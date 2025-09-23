// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

using Microsoft.PowerShell.Commands;
using Microsoft.Win32;

namespace System.Management.Automation.Help
{
    
    internal class UpdatableHelpSystemException : Exception
    {
        
        internal UpdatableHelpSystemException(string errorId, string message, ErrorCategory cat, object targetObject, Exception innerException)
            : base(message, innerException)
        {
            FullyQualifiedErrorId = errorId;
            ErrorCategory = cat;
            TargetObject = targetObject;
        }

#if !CORECLR
        
        protected UpdatableHelpSystemException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
#endif

        
        internal string FullyQualifiedErrorId { get; }

        
        internal ErrorCategory ErrorCategory { get; }

        
        internal object TargetObject { get; }
    }

    
    internal class UpdatableHelpExceptionContext
    {
        
        internal UpdatableHelpExceptionContext(UpdatableHelpSystemException exception)
        {
            Exception = exception;
            Modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Cultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        
        internal HashSet<string> Modules { get; set; }

        
        internal HashSet<string> Cultures { get; set; }

        
        internal UpdatableHelpSystemException Exception { get; }

        
        internal ErrorRecord CreateErrorRecord(UpdatableHelpCommandType commandType)
        {
            Debug.Assert(Modules.Count != 0);

            return new ErrorRecord(new Exception(GetExceptionMessage(commandType)), Exception.FullyQualifiedErrorId, Exception.ErrorCategory,
                Exception.TargetObject);
        }

        
        internal string GetExceptionMessage(UpdatableHelpCommandType commandType)
        {
            string message = string.Empty;
            SortedSet<string> sortedModules = new SortedSet<string>(Modules, StringComparer.CurrentCultureIgnoreCase);
            SortedSet<string> sortedCultures = new SortedSet<string>(Cultures, StringComparer.CurrentCultureIgnoreCase);
            string modules = string.Join(", ", sortedModules);
            string cultures = string.Join(", ", sortedCultures);

            if (commandType == UpdatableHelpCommandType.UpdateHelpCommand)
            {
                if (Cultures.Count == 0)
                {
                    message = StringUtil.Format(HelpDisplayStrings.FailedToUpdateHelpForModule, modules, Exception.Message);
                }
                else
                {
                    message = StringUtil.Format(HelpDisplayStrings.FailedToUpdateHelpForModuleWithCulture, modules, cultures, Exception.Message);
                }
            }
            else
            {
                if (Cultures.Count == 0)
                {
                    message = StringUtil.Format(HelpDisplayStrings.FailedToSaveHelpForModule, modules, Exception.Message);
                }
                else
                {
                    message = StringUtil.Format(HelpDisplayStrings.FailedToSaveHelpForModuleWithCulture, modules, cultures, Exception.Message);
                }
            }

            return message;
        }
    }

    
    internal enum UpdatableHelpCommandType
    {
        UnknownCommand = 0,
        UpdateHelpCommand = 1,
        SaveHelpCommand = 2
    }

    
    internal class UpdatableHelpProgressEventArgs : EventArgs
    {
        
        internal UpdatableHelpProgressEventArgs(string moduleName, string status, int percent)
        {
            Debug.Assert(!string.IsNullOrEmpty(status));

            CommandType = UpdatableHelpCommandType.UnknownCommand;
            ProgressStatus = status;
            ProgressPercent = percent;
            ModuleName = moduleName;
        }

        
        internal UpdatableHelpProgressEventArgs(string moduleName, UpdatableHelpCommandType type, string status, int percent)
        {
            Debug.Assert(!string.IsNullOrEmpty(status));

            CommandType = type;
            ProgressStatus = status;
            ProgressPercent = percent;
            ModuleName = moduleName;
        }

        
        internal string ProgressStatus { get; }

        
        internal int ProgressPercent { get; }

        
        internal string ModuleName { get; }

        
        internal UpdatableHelpCommandType CommandType { get; set; }
    }

    
    internal class UpdatableHelpSystem : IDisposable
    {
        private readonly TimeSpan _defaultTimeout;
        private readonly Collection<UpdatableHelpProgressEventArgs> _progressEvents;
        private bool _stopping;
        private readonly object _syncObject;
        private readonly UpdatableHelpCommandBase _cmdlet;
        private readonly CancellationTokenSource _cancelTokenSource;

        internal HttpClient HttpClient { get; }

        internal bool UseDefaultCredentials;

        internal string CurrentModule { get; set; }

        
        internal UpdatableHelpSystem(UpdatableHelpCommandBase cmdlet, bool useDefaultCredentials)
        {
            HttpClient = new HttpClient();
            _defaultTimeout = new TimeSpan(0, 0, 30);
            _progressEvents = new Collection<UpdatableHelpProgressEventArgs>();
            Errors = new Collection<Exception>();
            _stopping = false;
            _syncObject = new object();
            _cmdlet = cmdlet;
            _cancelTokenSource = new CancellationTokenSource();

            UseDefaultCredentials = useDefaultCredentials;

#if !CORECLR
            WebClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(HandleDownloadProgressChanged);
            WebClient.DownloadFileCompleted += new AsyncCompletedEventHandler(HandleDownloadFileCompleted);
#endif
        }

        
        public void Dispose()
        {
#if !CORECLR
            _completionEvent.Dispose();
#endif
            _cancelTokenSource.Dispose();
            HttpClient.Dispose();
            GC.SuppressFinalize(this);
        }

        
        internal Collection<Exception> Errors { get; }

        
        internal IEnumerable<string> GetCurrentUICulture()
        {
            CultureInfo culture = CultureInfo.CurrentUICulture;

            // Allow tests to override system culture
            if (InternalTestHooks.CurrentUICulture != null)
            {
                culture = InternalTestHooks.CurrentUICulture;
            }

            return CultureSpecificUpdatableHelp.GetCultureFallbackChain(culture);
        }

        #region Help Metadata Retrieval

        
        internal UpdatableHelpUri GetHelpInfoUri(UpdatableHelpModuleInfo module, CultureInfo culture)
        {
            return new UpdatableHelpUri(module.ModuleName, module.ModuleGuid, culture, ResolveUri(module.HelpInfoUri, false));
        }

        
        internal UpdatableHelpInfo GetHelpInfo(UpdatableHelpCommandType commandType, string uri, string moduleName, Guid moduleGuid, string culture)
        {
            try
            {
                OnProgressChanged(this, new UpdatableHelpProgressEventArgs(CurrentModule, commandType, StringUtil.Format(
                    HelpDisplayStrings.UpdateProgressLocating), 0));

                string xml;
                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.UseDefaultCredentials = UseDefaultCredentials;
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.Timeout = _defaultTimeout;
                        Task<string> responseBody = client.GetStringAsync(uri);
                        xml = responseBody.Result;
                        if (responseBody.Exception != null)
                        {
                            return null;
                        }
                    }
                }

                UpdatableHelpInfo helpInfo = CreateHelpInfo(xml, moduleName, moduleGuid,
                                                            currentCulture: culture, pathOverride: null, verbose: true,
                                                            shouldResolveUri: true, ignoreValidationException: false);

                return helpInfo;
            }
#if !CORECLR
            catch (WebException)
            {
                return null;
            }
#endif
            finally
            {
                OnProgressChanged(this, new UpdatableHelpProgressEventArgs(CurrentModule, commandType, StringUtil.Format(
                    HelpDisplayStrings.UpdateProgressLocating), 100));
            }
        }

        
        private string ResolveUri(string baseUri, bool verbose)
        {
            Debug.Assert(!string.IsNullOrEmpty(baseUri));

            // Directory.Exists checks if baseUri is a network drive or
            // a local directory. If baseUri is local, we don't need to resolve it.
            //
            // The / check works because all of our fwlinks must resolve
            // to a remote virtual directory. I think HTTP always appends /
            // in reference to a directory.
            // Like if you send a request to www.technet.com/powershell you will get
            // a 301/203 response with the response URI set to www.technet.com/powershell/
            //
            if (Directory.Exists(baseUri) || baseUri.EndsWith('/'))
            {
                if (verbose)
                {
                    _cmdlet.WriteVerbose(StringUtil.Format(RemotingErrorIdStrings.URIRedirectWarningToHost, baseUri));
                }

                return baseUri;
            }

            if (verbose)
            {
                _cmdlet.WriteVerbose(StringUtil.Format(HelpDisplayStrings.UpdateHelpResolveUriVerbose, baseUri));
            }

            string uri = baseUri;

            try
            {
                // We only allow 10 redirections
                for (int i = 0; i < 10; i++)
                {
                    if (_stopping)
                    {
                        return uri;
                    }

                    using (HttpClientHandler handler = new HttpClientHandler())
                    {
                        handler.AllowAutoRedirect = false;
                        handler.UseDefaultCredentials = UseDefaultCredentials;
                        using (HttpClient client = new HttpClient(handler))
                        {
                            client.Timeout = new TimeSpan(0, 0, 30); // Set 30 second timeout
                            Task<HttpResponseMessage> responseMessage = client.GetAsync(uri);
                            using (HttpResponseMessage response = responseMessage.Result)
                            {
                                if (response.StatusCode == HttpStatusCode.Found ||
                                    response.StatusCode == HttpStatusCode.Redirect ||
                                    response.StatusCode == HttpStatusCode.Moved ||
                                    response.StatusCode == HttpStatusCode.MovedPermanently)
                                {
                                    Uri responseUri = response.Headers.Location;

                                    if (responseUri.IsAbsoluteUri)
                                    {
                                        uri = responseUri.ToString();
                                    }
                                    else
                                    {
                                        Uri originalAbs = new Uri(uri);
                                        uri = uri.Replace(originalAbs.AbsolutePath, responseUri.ToString());
                                    }

                                    uri = uri.Trim();

                                    if (verbose)
                                    {
                                        _cmdlet.WriteVerbose(StringUtil.Format(RemotingErrorIdStrings.URIRedirectWarningToHost, uri));
                                    }

                                    if (uri.EndsWith('/'))
                                    {
                                        return uri;
                                    }
                                }
                                else if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    if (uri.EndsWith('/'))
                                    {
                                        return uri;
                                    }
                                    else
                                    {
                                        throw new UpdatableHelpSystemException("InvalidHelpInfoUri", StringUtil.Format(HelpDisplayStrings.InvalidHelpInfoUri, uri),
                                            ErrorCategory.InvalidOperation, null, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (UriFormatException e)
            {
                throw new UpdatableHelpSystemException("InvalidUriFormat", e.Message, ErrorCategory.InvalidData, null, e);
            }

            throw new UpdatableHelpSystemException("TooManyRedirections", StringUtil.Format(HelpDisplayStrings.TooManyRedirections),
                ErrorCategory.InvalidOperation, null, null);
        }

        
        private const string HelpInfoXmlSchema = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <xs:schema attributeFormDefault=""unqualified"" elementFormDefault=""qualified""
                targetNamespace=""http://schemas.microsoft.com/powershell/help/2010/05"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
                <xs:element name=""HelpInfo"">
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name=""HelpContentURI"" type=""xs:anyURI"" minOccurs=""1"" maxOccurs=""1"" />
                            <xs:element name=""SupportedUICultures"" minOccurs=""1"" maxOccurs=""1"">
                                <xs:complexType>
                                    <xs:sequence>
                                        <xs:element name=""UICulture"" minOccurs=""1"" maxOccurs=""unbounded"">
                                            <xs:complexType>
                                                <xs:sequence>
                                                    <xs:element name=""UICultureName"" type=""xs:language"" minOccurs=""1"" maxOccurs=""1"" />
                                                    <xs:element name=""UICultureVersion"" type=""xs:string"" minOccurs=""1"" maxOccurs=""1"" />
                                                </xs:sequence>
                                            </xs:complexType>
                                        </xs:element>
                                    </xs:sequence>
                                </xs:complexType>
                            </xs:element>
                        </xs:sequence>
                    </xs:complexType>
                </xs:element>
            </xs:schema>";

        private const string HelpInfoXmlNamespace = "http://schemas.microsoft.com/powershell/help/2010/05";
        private const string HelpInfoXmlValidationFailure = "HelpInfoXmlValidationFailure";
        private const string MamlXmlNamespace = "http://schemas.microsoft.com/maml/2004/10";
        private const string CommandXmlNamespace = "http://schemas.microsoft.com/maml/dev/command/2004/10";
        private const string DscResourceXmlNamespace = "http://schemas.microsoft.com/maml/dev/dscResource/2004/10";

        
        internal UpdatableHelpInfo CreateHelpInfo(string xml, string moduleName, Guid moduleGuid,
            string currentCulture, string pathOverride, bool verbose, bool shouldResolveUri, bool ignoreValidationException)
        {
            XmlDocument document = null;
            try
            {
                document = CreateValidXmlDocument(xml, HelpInfoXmlNamespace, HelpInfoXmlSchema,
                    new ValidationEventHandler(HelpInfoValidationHandler),
                    true);
            }
            catch (UpdatableHelpSystemException e)
            {
                if (ignoreValidationException && HelpInfoXmlValidationFailure.Equals(e.FullyQualifiedErrorId, StringComparison.Ordinal))
                {
                    return null;
                }

                throw;
            }
            catch (XmlException e)
            {
                if (ignoreValidationException)
                {
                    return null;
                }

                throw new UpdatableHelpSystemException(HelpInfoXmlValidationFailure,
                    e.Message, ErrorCategory.InvalidData, null, e);
            }

            string uri = pathOverride;
            string unresolvedUri = document["HelpInfo"]["HelpContentURI"].InnerText;

            if (string.IsNullOrEmpty(pathOverride))
            {
                if (shouldResolveUri)
                {
                    uri = ResolveUri(unresolvedUri, verbose);
                }
                else
                {
                    uri = unresolvedUri;
                }
            }

            XmlNodeList cultures = document["HelpInfo"]["SupportedUICultures"].ChildNodes;

            CultureSpecificUpdatableHelp[] updatableHelpItem = new CultureSpecificUpdatableHelp[cultures.Count];

            for (int i = 0; i < cultures.Count; i++)
            {
                updatableHelpItem[i] = new CultureSpecificUpdatableHelp(
                    new CultureInfo(cultures[i]["UICultureName"].InnerText),
                    new Version(cultures[i]["UICultureVersion"].InnerText));
            }

            UpdatableHelpInfo helpInfo = new UpdatableHelpInfo(unresolvedUri, updatableHelpItem);

            if (!string.IsNullOrEmpty(currentCulture))
            {
                for (int i = 0; i < updatableHelpItem.Length; i++)
                {
                    if (updatableHelpItem[i].IsCultureSupported(currentCulture))
                    {
                        helpInfo.HelpContentUriCollection.Add(new UpdatableHelpUri(moduleName, moduleGuid, updatableHelpItem[i].Culture, uri));
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentCulture) && helpInfo.HelpContentUriCollection.Count == 0)
            {
                // throw exception
                throw new UpdatableHelpSystemException("HelpCultureNotSupported",
                    StringUtil.Format(HelpDisplayStrings.HelpCultureNotSupported,
                        currentCulture, helpInfo.GetSupportedCultures()), ErrorCategory.InvalidOperation, null, null);
            }

            return helpInfo;
        }

        
        private static XmlDocument CreateValidXmlDocument(string xml, string ns, string schema, ValidationEventHandler handler,
            bool helpInfo)
        {
            XmlReaderSettings settings = new XmlReaderSettings();

            settings.Schemas.Add(ns, new XmlTextReader(new StringReader(schema)));
            settings.ValidationType = ValidationType.Schema;

            XmlReader reader = XmlReader.Create(new StringReader(xml), settings);
            XmlDocument document = new XmlDocument();

            try
            {
                document.Load(reader);
                document.Validate(handler);
            }
            catch (XmlSchemaValidationException e)
            {
                if (helpInfo)
                {
                    throw new UpdatableHelpSystemException(HelpInfoXmlValidationFailure,
                        StringUtil.Format(HelpDisplayStrings.HelpInfoXmlValidationFailure, e.Message),
                        ErrorCategory.InvalidData, null, e);
                }
                else
                {
                    throw new UpdatableHelpSystemException("HelpContentXmlValidationFailure",
                        StringUtil.Format(HelpDisplayStrings.HelpContentXmlValidationFailure, e.Message),
                        ErrorCategory.InvalidData, null, e);
                }
            }

            return document;
        }

        
        private void HelpInfoValidationHandler(object sender, ValidationEventArgs arg)
        {
            switch (arg.Severity)
            {
                case XmlSeverityType.Error:
                    {
                        throw new UpdatableHelpSystemException(HelpInfoXmlValidationFailure,
                            StringUtil.Format(HelpDisplayStrings.HelpInfoXmlValidationFailure),
                            ErrorCategory.InvalidData, null, arg.Exception);
                    }
                case XmlSeverityType.Warning:
                    break;
            }
        }

        
        private void HelpContentValidationHandler(object sender, ValidationEventArgs arg)
        {
            switch (arg.Severity)
            {
                case XmlSeverityType.Error:
                    {
                        throw new UpdatableHelpSystemException("HelpContentXmlValidationFailure",
                            StringUtil.Format(HelpDisplayStrings.HelpContentXmlValidationFailure),
                            ErrorCategory.InvalidData, null, arg.Exception);
                    }
                case XmlSeverityType.Warning:
                    break;
            }
        }

        #endregion

        #region Help Content Retrieval

        
        internal void CancelDownload()
        {
            _cancelTokenSource.Cancel();
            _stopping = true;
        }

        
        internal bool DownloadAndInstallHelpContent(UpdatableHelpCommandType commandType, ExecutionContext context, Collection<string> destPaths,
            string fileName, CultureInfo culture, string helpContentUri, string xsdPath, out Collection<string> installed)
        {
            if (_stopping)
            {
                installed = new Collection<string>();
                return false;
            }

            string cache = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));

            if (!DownloadHelpContent(commandType, cache, helpContentUri, fileName, culture.Name))
            {
                installed = new Collection<string>();
                return false;
            }

            InstallHelpContent(commandType, context, cache, destPaths, fileName, cache, culture, xsdPath, out installed);

            return true;
        }

        
        internal bool DownloadHelpContent(UpdatableHelpCommandType commandType, string path, string helpContentUri, string fileName, string culture)
        {
            if (_stopping)
            {
                return false;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            OnProgressChanged(this, new UpdatableHelpProgressEventArgs(CurrentModule, commandType, StringUtil.Format(
                HelpDisplayStrings.UpdateProgressConnecting), 0));

            string uri = helpContentUri + fileName;

            return DownloadHelpContentHttpClient(uri, Path.Combine(path, fileName), commandType);
        }

        
        private bool DownloadHelpContentHttpClient(string uri, string fileName, UpdatableHelpCommandType commandType)
        {
            // TODO: Was it intentional for them to remove IDisposable from Task?
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.AllowAutoRedirect = false;
                handler.UseDefaultCredentials = UseDefaultCredentials;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = _defaultTimeout;
                    Task<HttpResponseMessage> responseMsg = client.GetAsync(new Uri(uri), _cancelTokenSource.Token);

                    // TODO: Should I use a continuation to write the stream to a file?
                    responseMsg.Wait();

                    if (_stopping)
                    {
                        return true;
                    }

                    if (!responseMsg.IsCanceled)
                    {
                        if (responseMsg.Exception != null)
                        {
                            Errors.Add(new UpdatableHelpSystemException("HelpContentNotFound",
                                StringUtil.Format(HelpDisplayStrings.HelpContentNotFound),
                                ErrorCategory.ResourceUnavailable, null, responseMsg.Exception));
                        }
                        else
                        {
                            lock (_syncObject)
                            {
                                _progressEvents.Add(new UpdatableHelpProgressEventArgs(CurrentModule, StringUtil.Format(
                                    HelpDisplayStrings.UpdateProgressDownloading), 100));
                            }

                            // Write the stream to the specified file to achieve functional parity with WebClient.DownloadFileAsync().
                            HttpResponseMessage response = responseMsg.Result;
                            if (response.IsSuccessStatusCode)
                            {
                                WriteResponseToFile(response, fileName);
                            }
                            else
                            {
                                Errors.Add(new UpdatableHelpSystemException("HelpContentNotFound",
                                    StringUtil.Format(HelpDisplayStrings.HelpContentNotFound),
                                    ErrorCategory.ResourceUnavailable, null, responseMsg.Exception));
                            }
                        }
                    }

                    SendProgressEvents(commandType);
                }
            }

            return (Errors.Count == 0);
        }

        
        private void WriteResponseToFile(HttpResponseMessage response, string fileName)
        {
            // TODO: Settings to use? FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite
            using (FileStream downloadedFileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                Task copyStreamOp = response.Content.CopyToAsync(downloadedFileStream);
                copyStreamOp.Wait();
                if (copyStreamOp.Exception != null)
                {
                    Errors.Add(copyStreamOp.Exception);
                }
            }
        }

        private void SendProgressEvents(UpdatableHelpCommandType commandType)
        {
            // Send progress events
            lock (_syncObject)
            {
                if (_progressEvents.Count > 0)
                {
                    foreach (UpdatableHelpProgressEventArgs evt in _progressEvents)
                    {
                        evt.CommandType = commandType;

                        OnProgressChanged(this, evt);
                    }

                    _progressEvents.Clear();
                }
            }
        }

        
        internal void GenerateHelpInfo(string moduleName, Guid moduleGuid, string contentUri, string culture, Version version, string destPath, string fileName, bool force)
        {
            Debug.Assert(Directory.Exists(destPath));

            if (_stopping)
            {
                return;
            }

            string destHelpInfo = Path.Combine(destPath, fileName);

            if (force)
            {
                RemoveReadOnly(destHelpInfo);
            }

            UpdatableHelpInfo oldHelpInfo = null;
            string xml = UpdatableHelpSystem.LoadStringFromPath(_cmdlet, destHelpInfo, null);

            if (xml != null)
            {
                // constructing the helpinfo object from previous update help log xml..
                // no need to resolve the uri's in this case.
                oldHelpInfo = CreateHelpInfo(xml, moduleName, moduleGuid, currentCulture: null, pathOverride: null,
                                             verbose: false, shouldResolveUri: false, ignoreValidationException: force);
            }

            using (FileStream file = new FileStream(destHelpInfo, FileMode.Create, FileAccess.Write))
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = Encoding.UTF8;
                settings.Indent = true; // Default indentation is two spaces
                using (XmlWriter writer = XmlWriter.Create(file, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("HelpInfo", "http://schemas.microsoft.com/powershell/help/2010/05");

                    writer.WriteStartElement("HelpContentURI");
                    writer.WriteValue(contentUri);
                    writer.WriteEndElement();

                    writer.WriteStartElement("SupportedUICultures");

                    bool found = false;

                    if (oldHelpInfo != null)
                    {
                        foreach (CultureSpecificUpdatableHelp oldInfo in oldHelpInfo.UpdatableHelpItems)
                        {
                            if (oldInfo.Culture.Name.Equals(culture, StringComparison.OrdinalIgnoreCase))
                            {
                                if (oldInfo.Version.Equals(version))
                                {
                                    writer.WriteStartElement("UICulture");
                                    writer.WriteStartElement("UICultureName");
                                    writer.WriteValue(oldInfo.Culture.Name);
                                    writer.WriteEndElement();
                                    writer.WriteStartElement("UICultureVersion");
                                    writer.WriteValue(oldInfo.Version.ToString());
                                    writer.WriteEndElement();
                                    writer.WriteEndElement();
                                }
                                else
                                {
                                    writer.WriteStartElement("UICulture");
                                    writer.WriteStartElement("UICultureName");
                                    writer.WriteValue(culture);
                                    writer.WriteEndElement();
                                    writer.WriteStartElement("UICultureVersion");
                                    writer.WriteValue(version.ToString());
                                    writer.WriteEndElement();
                                    writer.WriteEndElement();
                                }

                                found = true;
                            }
                            else
                            {
                                writer.WriteStartElement("UICulture");
                                writer.WriteStartElement("UICultureName");
                                writer.WriteValue(oldInfo.Culture.Name);
                                writer.WriteEndElement();
                                writer.WriteStartElement("UICultureVersion");
                                writer.WriteValue(oldInfo.Version.ToString());
                                writer.WriteEndElement();
                                writer.WriteEndElement();
                            }
                        }
                    }

                    if (!found)
                    {
                        writer.WriteStartElement("UICulture");
                        writer.WriteStartElement("UICultureName");
                        writer.WriteValue(culture);
                        writer.WriteEndElement();
                        writer.WriteStartElement("UICultureVersion");
                        writer.WriteValue(version.ToString());
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }

        
        private static void RemoveReadOnly(string path)
        {
            if (File.Exists(path))
            {
                FileAttributes attributes = File.GetAttributes(path);

                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    attributes &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(path, attributes);
                }
            }
        }

        
        internal void InstallHelpContent(UpdatableHelpCommandType commandType, ExecutionContext context, string sourcePath,
            Collection<string> destPaths, string fileName, string tempPath, CultureInfo culture, string xsdPath,
            out Collection<string> installed)
        {
            installed = new Collection<string>();

            if (_stopping)
            {
                installed = new Collection<string>();
                return;
            }

            // These paths must exist
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            Debug.Assert(destPaths.Count > 0);

            try
            {
                OnProgressChanged(this, new UpdatableHelpProgressEventArgs(CurrentModule, commandType, StringUtil.Format(
                    HelpDisplayStrings.UpdateProgressInstalling), 0));

                string combinedSourcePath = GetFilePath(Path.Combine(sourcePath, fileName));

                if (string.IsNullOrEmpty(combinedSourcePath))
                {
                    throw new UpdatableHelpSystemException("HelpContentNotFound", StringUtil.Format(HelpDisplayStrings.HelpContentNotFound),
                        ErrorCategory.ResourceUnavailable, null, null);
                }

                string combinedTempPath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(fileName));

                if (Directory.Exists(combinedTempPath))
                {
                    Directory.Delete(combinedTempPath, true);
                }

                bool needToCopy = true;
                UnzipHelpContent(context, combinedSourcePath, combinedTempPath, out needToCopy);
                if (needToCopy)
                {
                    ValidateAndCopyHelpContent(combinedTempPath, destPaths, culture.Name, xsdPath, out installed);
                }
            }
            finally
            {
                OnProgressChanged(this, new UpdatableHelpProgressEventArgs(CurrentModule, commandType, StringUtil.Format(
                    HelpDisplayStrings.UpdateProgressInstalling), 100));

                try
                {
                    if (Directory.Exists(tempPath))
                    {
                        Directory.Delete(tempPath);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (ArgumentException) { }
            }
        }

#if UNIX
        private static bool ExpandArchive(string source, string destination)
        {
            bool successfulDecompression = false;

            try
            {
                using (ZipArchive zipArchive = ZipFile.Open(source, ZipArchiveMode.Read))
                {
                    zipArchive.ExtractToDirectory(destination);
                    successfulDecompression = true;
                }
            }
            catch (ArgumentException) { }
            catch (InvalidDataException) { }
            catch (NotSupportedException) { }
            catch (FileNotFoundException) { }
            catch (IOException) { }
            catch (SecurityException) { }
            catch (UnauthorizedAccessException) { }
            catch (ObjectDisposedException) { }

            return successfulDecompression;
        }
#endif

        
        private static void UnzipHelpContent(ExecutionContext context, string srcPath, string destPath, out bool needToCopy)
        {
            needToCopy = true;

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            string sourceDirectory = Path.GetDirectoryName(srcPath);
            bool successfulDecompression = false;
#if UNIX
            successfulDecompression = ExpandArchive(Path.Combine(sourceDirectory, Path.GetFileName(srcPath)), destPath);
#else
            // Cabinet API doesn't handle the trailing back slash
            if (!sourceDirectory.EndsWith('\\'))
            {
                sourceDirectory += "\\";
            }

            if (!destPath.EndsWith('\\'))
            {
                destPath += "\\";
            }

            successfulDecompression = CabinetExtractorFactory.GetCabinetExtractor().Extract(Path.GetFileName(srcPath), sourceDirectory, destPath);
#endif
            if (!successfulDecompression)
            {
                throw new UpdatableHelpSystemException("UnableToExtract", StringUtil.Format(HelpDisplayStrings.UnzipFailure),
                    ErrorCategory.InvalidOperation, null, null);
            }

            string[] files = Directory.GetFiles(destPath);
            if (files.Length == 1)
            {
                // If there is a single file
                string file = Path.GetFileName(files[0]);
                if (!string.IsNullOrEmpty(file) && file.Equals("placeholder.txt", StringComparison.OrdinalIgnoreCase))
                {
                    // And that single file is named "placeholder.txt"
                    var fileInfo = new FileInfo(files[0]);
                    if (fileInfo.Length == 0)
                    {
                        // And if that single file has length 0, then we delete that file and no longer install help
                        needToCopy = false;
                        try
                        {
                            File.Delete(files[0]);
                            string directory = Path.GetDirectoryName(files[0]);
                            if (!string.IsNullOrEmpty(directory))
                            {
                                Directory.Delete(directory);
                            }
                        }
                        catch (FileNotFoundException)
                        { }
                        catch (DirectoryNotFoundException)
                        { }
                        catch (UnauthorizedAccessException)
                        { }
                        catch (System.Security.SecurityException)
                        { }
                        catch (ArgumentNullException)
                        { }
                        catch (ArgumentException)
                        { }
                        catch (PathTooLongException)
                        { }
                        catch (NotSupportedException)
                        { }
                        catch (IOException)
                        { }
                    }
                }
            }
            else
            {
                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        FileInfo fInfo = new FileInfo(file);
                        if ((fInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            // Clear the read-only attribute
                            fInfo.Attributes &= ~(FileAttributes.ReadOnly);
                        }
                    }
                }
            }
        }

        
        private void ValidateAndCopyHelpContent(string sourcePath, Collection<string> destPaths, string culture, string xsdPath,
            out Collection<string> installed)
        {
            installed = new Collection<string>();

            string xsd = LoadStringFromPath(_cmdlet, xsdPath, null);

            // We only accept txt files and xml files
            foreach (string file in Directory.GetFiles(sourcePath))
            {
                if (!string.Equals(Path.GetExtension(file), ".xml", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(Path.GetExtension(file), ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    throw new UpdatableHelpSystemException("HelpContentContainsInvalidFiles",
                        StringUtil.Format(HelpDisplayStrings.HelpContentContainsInvalidFiles), ErrorCategory.InvalidData,
                        null, null);
                }
            }

            // xml validation
            foreach (string file in Directory.GetFiles(sourcePath))
            {
                if (string.Equals(Path.GetExtension(file), ".xml", StringComparison.OrdinalIgnoreCase))
                {
                    if (xsd == null)
                    {
                        throw new ItemNotFoundException(StringUtil.Format(HelpDisplayStrings.HelpContentXsdNotFound, xsdPath));
                    }
                    else
                    {
                        string xml = LoadStringFromPath(_cmdlet, file, null);

                        XmlReader documentReader = XmlReader.Create(new StringReader(xml));
                        XmlDocument contentDocument = new XmlDocument();

                        contentDocument.Load(documentReader);

                        if (contentDocument.ChildNodes.Count != 1 && contentDocument.ChildNodes.Count != 2)
                        {
                            throw new UpdatableHelpSystemException("HelpContentXmlValidationFailure",
                                StringUtil.Format(HelpDisplayStrings.HelpContentXmlValidationFailure, HelpDisplayStrings.RootElementMustBeHelpItems),
                                ErrorCategory.InvalidData, null, null);
                        }

                        XmlNode helpItemsNode = null;

                        if (contentDocument.DocumentElement != null &&
                            contentDocument.DocumentElement.LocalName.Equals("providerHelp", StringComparison.OrdinalIgnoreCase))
                        {
                            helpItemsNode = contentDocument;
                        }
                        else
                        {
                            if (contentDocument.ChildNodes.Count == 1)
                            {
                                if (!contentDocument.ChildNodes[0].LocalName.Equals("helpItems", StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new UpdatableHelpSystemException("HelpContentXmlValidationFailure",
                                        StringUtil.Format(HelpDisplayStrings.HelpContentXmlValidationFailure, HelpDisplayStrings.RootElementMustBeHelpItems),
                                        ErrorCategory.InvalidData, null, null);
                                }
                                else
                                {
                                    helpItemsNode = contentDocument.ChildNodes[0];
                                }
                            }
                            else if (contentDocument.ChildNodes.Count == 2)
                            {
                                if (!contentDocument.ChildNodes[1].LocalName.Equals("helpItems", StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new UpdatableHelpSystemException("HelpContentXmlValidationFailure",
                                        StringUtil.Format(HelpDisplayStrings.HelpContentXmlValidationFailure, HelpDisplayStrings.RootElementMustBeHelpItems),
                                        ErrorCategory.InvalidData, null, null);
                                }
                                else
                                {
                                    helpItemsNode = contentDocument.ChildNodes[1];
                                }
                            }
                        }

                        Debug.Assert(helpItemsNode != null, "helpItemsNode must not be null");

                        foreach (XmlNode node in helpItemsNode.ChildNodes)
                        {
                            if (node.NodeType == XmlNodeType.Element)
                            {
                                if (!node.LocalName.Equals("providerHelp", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (node.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!node.NamespaceURI.Equals(MamlXmlNamespace, StringComparison.OrdinalIgnoreCase))
                                        {
                                            throw new UpdatableHelpSystemException("HelpContentXmlValidationFailure",
                                                StringUtil.Format(HelpDisplayStrings.HelpContentXmlValidationFailure,
                                                StringUtil.Format(HelpDisplayStrings.HelpContentMustBeInTargetNamespace, MamlXmlNamespace)), ErrorCategory.InvalidData, null, null);
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    if (!node.NamespaceURI.Equals(CommandXmlNamespace, StringComparison.OrdinalIgnoreCase) &&
                                        !node.NamespaceURI.Equals(DscResourceXmlNamespace, StringComparison.OrdinalIgnoreCase))
                                    {
                                        throw new UpdatableHelpSystemException("HelpContentXmlValidationFailure",
                                            StringUtil.Format(HelpDisplayStrings.HelpContentXmlValidationFailure,
                                            StringUtil.Format(HelpDisplayStrings.HelpContentMustBeInTargetNamespace, MamlXmlNamespace)), ErrorCategory.InvalidData, null, null);
                                    }
                                }

                                CreateValidXmlDocument(node.OuterXml, MamlXmlNamespace, xsd,
                                    new ValidationEventHandler(HelpContentValidationHandler),
                                    false);
                            }
                        }
                    }
                }
                else if (string.Equals(Path.GetExtension(file), ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);

                    if (fileStream.Length > 2)
                    {
                        byte[] firstTwoBytes = new byte[2];

                        fileStream.Read(firstTwoBytes, 0, 2);

                        // Check for Mark Zbikowski's magic initials
                        if (firstTwoBytes[0] == 'M' && firstTwoBytes[1] == 'Z')
                        {
                            throw new UpdatableHelpSystemException("HelpContentContainsInvalidFiles",
                                StringUtil.Format(HelpDisplayStrings.HelpContentContainsInvalidFiles), ErrorCategory.InvalidData,
                                null, null);
                        }
                    }
                }

                foreach (string path in destPaths)
                {
                    Debug.Assert(Directory.Exists(path));

                    string combinedPath = Path.Combine(path, culture);

                    if (!Directory.Exists(combinedPath))
                    {
                        Directory.CreateDirectory(combinedPath);
                    }

                    string destPath = Path.Combine(combinedPath, Path.GetFileName(file));

                    // Make the destpath writeable if force is used
                    FileAttributes? originalFileAttributes = null;
                    try
                    {
                        if (File.Exists(destPath) && (_cmdlet.Force))
                        {
                            FileInfo fInfo = new FileInfo(destPath);
                            if ((fInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                            {
                                // remember to reset the read-only attribute later
                                originalFileAttributes = fInfo.Attributes;
                                // Clear the read-only attribute
                                fInfo.Attributes &= ~(FileAttributes.ReadOnly);
                            }
                        }

                        File.Copy(file, destPath, true);
                    }
                    finally
                    {
                        if (originalFileAttributes.HasValue)
                        {
                            File.SetAttributes(destPath, originalFileAttributes.Value);
                        }
                    }

                    installed.Add(destPath);
                }
            }
        }

        
        internal static string LoadStringFromPath(PSCmdlet cmdlet, string path, PSCredential credential)
        {
            Debug.Assert(path != null);

            if (credential != null)
            {
                // New PSDrive

                using (UpdatableHelpSystemDrive drive = new UpdatableHelpSystemDrive(cmdlet, Path.GetDirectoryName(path), credential))
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));

                    if (!cmdlet.InvokeProvider.Item.Exists(Path.Combine(drive.DriveName, Path.GetFileName(path))))
                    {
                        return null;
                    }

                    cmdlet.InvokeProvider.Item.Copy(new string[1] { Path.Combine(drive.DriveName, Path.GetFileName(path)) }, tempPath,
                        false, CopyContainers.CopyTargetContainer, true, true);

                    path = tempPath;
                }
            }

            string filePath = GetFilePath(path);
            if (!string.IsNullOrEmpty(filePath))
            {
                using (FileStream currentHelpInfoFile = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    StreamReader reader = new StreamReader(currentHelpInfoFile);

                    return reader.ReadToEnd();
                }
            }

            return null;
        }

        
        internal static string GetFilePath(string path)
        {
            FileInfo item = new FileInfo(path);

            // We use 'FileInfo.Attributes' (not 'FileInfo.Exist')
            // because we want to get exceptions
            // like UnauthorizedAccessException or IOException.
            if ((int)item.Attributes != -1)
            {
                return path;
            }
#if UNIX
            // On Linux, file paths are case sensitive.
            // The user does not have control over the files (HelpInfo.xml, .zip, and cab) that are generated by the Publishing team.
            // The logic below is to support updating help content via sourcepath parameter for case insensitive files.
            var dirInfo = item.Directory;
            string fileName = item.Name;

            // Prerequisite: The directory in the given path must exist and it is case sensitive.
            if (dirInfo.Exists)
            {
                // Get the list of files in the directory.
                FileInfo[] fileList = dirInfo.GetFiles(searchPattern: fileName, new System.IO.EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive });

                if (fileList.Length > 0)
                {
                    return fileList[0].FullName;
                }
            }
#endif
            return null;
        }

        
        internal string GetDefaultSourcePath()
        {
            return null;
        }

        #endregion

        #region Events

        internal event EventHandler<UpdatableHelpProgressEventArgs> OnProgressChanged;

#if !CORECLR

        
        private void HandleDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (_stopping)
            {
                return;
            }

            if (!e.Cancelled)
            {
                if (e.Error != null)
                {
                    if (e.Error is WebException)
                    {
                        Errors.Add(new UpdatableHelpSystemException("HelpContentNotFound", StringUtil.Format(HelpDisplayStrings.HelpContentNotFound, e.UserState.ToString()),
                            ErrorCategory.ResourceUnavailable, null, null));
                    }
                    else
                    {
                        Errors.Add(e.Error);
                    }
                }
                else
                {
                    lock (_syncObject)
                    {
                        _progressEvents.Add(new UpdatableHelpProgressEventArgs(CurrentModule, StringUtil.Format(
                            HelpDisplayStrings.UpdateProgressDownloading), 100));
                    }
                }

                _completed = true;
                _completionEvent.Set();
            }
        }

        
        private void HandleDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (_stopping)
            {
                return;
            }

            lock (_syncObject)
            {
                _progressEvents.Add(new UpdatableHelpProgressEventArgs(CurrentModule, StringUtil.Format(
                        HelpDisplayStrings.UpdateProgressDownloading), e.ProgressPercentage));
            }

            _completionEvent.Set();
        }
#endif

        #endregion
    }

    
    internal class UpdatableHelpSystemDrive : IDisposable
    {
        private readonly string _driveName;
        private readonly PSCmdlet _cmdlet;

        
        internal string DriveName
        {
            get
            {
                return _driveName + ":\\";
            }
        }

        
        internal UpdatableHelpSystemDrive(PSCmdlet cmdlet, string path, PSCredential credential)
        {
            for (int i = 0; i < 6; i++)
            {
                _driveName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                _cmdlet = cmdlet;

                // Need to get rid of the trailing \, otherwise New-PSDrive will not work...
                if (path.EndsWith('\\') || path.EndsWith('/'))
                {
                    path = path.Remove(path.Length - 1);
                }

                PSDriveInfo mappedDrive = cmdlet.SessionState.Drive.GetAtScope(_driveName, "local");

                if (mappedDrive != null)
                {
                    if (mappedDrive.Root.Equals(path))
                    {
                        return;
                    }

                    // Remove the drive after 5 tries
                    if (i < 5)
                    {
                        continue;
                    }

                    cmdlet.SessionState.Drive.Remove(_driveName, true, "local");
                }

                mappedDrive = new PSDriveInfo(_driveName, cmdlet.SessionState.Internal.GetSingleProvider("FileSystem"),
                    path, string.Empty, credential);

                cmdlet.SessionState.Drive.New(mappedDrive, "local");

                break;
            }
        }

        
        public void Dispose()
        {
            PSDriveInfo mappedDrive = _cmdlet.SessionState.Drive.GetAtScope(_driveName, "local");

            if (mappedDrive != null)
            {
                _cmdlet.SessionState.Drive.Remove(_driveName, true, "local");
            }

            GC.SuppressFinalize(this);
        }
    }
}
