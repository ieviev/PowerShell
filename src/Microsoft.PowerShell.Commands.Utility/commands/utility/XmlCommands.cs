// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Security;
using System.Text;
using System.Xml;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsData.Export, "Clixml", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096926")]
    public sealed class ExportClixmlCommand : PSCmdlet, IDisposable
    {
        #region Command Line Parameters

        // If a Passthru parameter is added, the SupportsShouldProcess
        // implementation will need to be modified.

        
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int Depth { get; set; }

        
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByPath")]
        public string Path { get; set; }

        
        [Parameter(Mandatory = true, ParameterSetName = "ByLiteralPath")]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get
            {
                return Path;
            }

            set
            {
                Path = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        
        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        [AllowNull]
        public PSObject InputObject { get; set; }

        
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        private bool _force;

        
        [Parameter]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber
        {
            get
            {
                return _noclobber;
            }

            set
            {
                _noclobber = value;
            }
        }

        private bool _noclobber;

        
        [Parameter]
        [ArgumentToEncodingTransformation]
        [ArgumentEncodingCompletions]
        [ValidateNotNullOrEmpty]
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                EncodingConversion.WarnIfObsolete(this, value);
                _encoding = value;
            }
        }

        private Encoding _encoding = Encoding.Default;

        #endregion Command Line Parameters

        #region Overrides

        
        protected override
        void
        BeginProcessing()
        {
            CreateFileStream();
        }

        
        protected override
        void
        ProcessRecord()
        {
            if (_serializer != null)
            {
                _serializer.Serialize(InputObject);
                _xw.Flush();
            }
        }

        
        protected override
        void
        EndProcessing()
        {
            if (_serializer != null)
            {
                _serializer.Done();
                _serializer = null;
            }

            CleanUp();
        }

        
        protected override void StopProcessing()
        {
            base.StopProcessing();
            _serializer.Stop();
        }

        #endregion Overrides

        #region file

        
        private FileStream _fs;

        
        private XmlWriter _xw;

        
        private Serializer _serializer;

        
        private FileInfo _readOnlyFileInfo = null;

        private void CreateFileStream()
        {
            Dbg.Assert(Path != null, "FileName is mandatory parameter");

            if (!ShouldProcess(Path))
            {
                return;
            }

            StreamWriter sw;
            PathUtils.MasterStreamOpen(
                this,
                this.Path,
                this.Encoding,
                false, // default encoding
                false, // append
                this.Force,
                this.NoClobber,
                out _fs,
                out sw,
                out _readOnlyFileInfo,
                _isLiteralPath
                );

            // create xml writer
            XmlWriterSettings xmlSettings = new();
            xmlSettings.CloseOutput = true;
            xmlSettings.Encoding = sw.Encoding;
            xmlSettings.Indent = true;
            xmlSettings.OmitXmlDeclaration = true;
            _xw = XmlWriter.Create(sw, xmlSettings);
            if (Depth == 0)
            {
                _serializer = new Serializer(_xw);
            }
            else
            {
                _serializer = new Serializer(_xw, Depth, true);
            }
        }

        private
        void
        CleanUp()
        {
            if (_fs != null)
            {
                if (_xw != null)
                {
                    _xw.Dispose();
                    _xw = null;
                }

                _fs.Dispose();
                _fs = null;
            }
        }

        #endregion file

        #region IDisposable Members

        
        private bool _disposed;

        
        public
        void
        Dispose()
        {
            if (!_disposed)
            {
                CleanUp();
            }

            _disposed = true;
        }

        #endregion IDisposable Members
    }

    
    [Cmdlet(VerbsData.Import, "Clixml", SupportsPaging = true, DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096618")]
    public sealed class ImportClixmlCommand : PSCmdlet, IDisposable
    {
        #region Command Line Parameters

        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByPath")]
        public string[] Path { get; set; }

        
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByLiteralPath")]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get
            {
                return Path;
            }

            set
            {
                Path = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        #endregion Command Line Parameters

        #region IDisposable Members

        private bool _disposed = false;

        
        public void Dispose()
        {
            if (!_disposed)
            {
                GC.SuppressFinalize(this);
                if (_helper != null)
                {
                    _helper.Dispose();
                    _helper = null;
                }

                _disposed = true;
            }
        }

        #endregion

        private ImportXmlHelper _helper;

        
        protected override void ProcessRecord()
        {
            if (Path != null)
            {
                foreach (string path in Path)
                {
                    _helper = new ImportXmlHelper(path, this, _isLiteralPath);
                    _helper.Import();
                }
            }
        }

        
        protected override void StopProcessing()
        {
            base.StopProcessing();
            _helper.Stop();
        }
    }

    
    [Cmdlet(VerbsData.ConvertTo, "Xml", SupportsShouldProcess = false,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096603", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(XmlDocument), typeof(string))]
    public sealed class ConvertToXmlCommand : PSCmdlet, IDisposable
    {
        #region Command Line Parameters

        
        [Parameter(HelpMessage = "Specifies how many levels of contained objects should be included in the XML representation")]
        [ValidateRange(1, int.MaxValue)]
        public int Depth { get; set; }

        
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true)]
        [AllowNull]
        public PSObject InputObject { get; set; }

        
        [Parameter(HelpMessage = "Specifies not to include the Type information in the XML representation")]
        public SwitchParameter NoTypeInformation
        {
            get
            {
                return _notypeinformation;
            }

            set
            {
                _notypeinformation = value;
            }
        }

        private bool _notypeinformation;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [ValidateSet("Stream", "String", "Document")]
        public string As { get; set; } = "Document";

        #endregion Command Line Parameters

        #region Overrides

        
        protected override void BeginProcessing()
        {
            if (!As.Equals("Stream", StringComparison.OrdinalIgnoreCase))
            {
                CreateMemoryStream();
            }
            else
            {
                WriteObject(string.Create(CultureInfo.InvariantCulture, $"<?xml version=\"1.0\" encoding=\"{Encoding.UTF8.WebName}\"?>"));
                WriteObject("<Objects>");
            }
        }

        
        protected override void ProcessRecord()
        {
            if (As.Equals("Stream", StringComparison.OrdinalIgnoreCase))
            {
                CreateMemoryStream();

                _serializer?.SerializeAsStream(InputObject);

                if (_serializer != null)
                {
                    _serializer.DoneAsStream();
                    _serializer = null;
                }
                // Loading to the XML Document
                _ms.Position = 0;
                StreamReader read = new(_ms);
                string data = read.ReadToEnd();
                WriteObject(data);

                // Cleanup
                CleanUp();
            }
            else
            {
                _serializer?.Serialize(InputObject);
            }
        }

        
        protected override void EndProcessing()
        {
            if (_serializer != null)
            {
                _serializer.Done();
                _serializer = null;
            }

            if (As.Equals("Stream", StringComparison.OrdinalIgnoreCase))
            {
                WriteObject("</Objects>");
            }
            else
            {
                // Loading to the XML Document
                _ms.Position = 0;
                if (As.Equals("Document", StringComparison.OrdinalIgnoreCase))
                {
                    // this is a trusted xml doc - the cmdlet generated the doc into a private memory stream
                    XmlDocument xmldoc = new();
                    xmldoc.Load(_ms);
                    WriteObject(xmldoc);
                }
                else if (As.Equals("String", StringComparison.OrdinalIgnoreCase))
                {
                    StreamReader read = new(_ms);
                    string data = read.ReadToEnd();
                    WriteObject(data);
                }
            }

            // Cleaning up
            CleanUp();
        }

        
        protected override void StopProcessing()
        {
            _serializer.Stop();
        }

        #endregion Overrides

        #region memory

        
        private XmlWriter _xw;

        
        private CustomSerialization _serializer;

        
        private MemoryStream _ms;

        private void CreateMemoryStream()
        {
            // Memory Stream
            _ms = new MemoryStream();

            // We use XmlTextWriter originally:
            //     _xw = new XmlTextWriter(_ms, null);
            //     _xw.Formatting = Formatting.Indented;
            // This implies the following settings:
            //  - Encoding is null -> use the default encoding 'UTF-8' when creating the writer from the stream;
            //  - XmlTextWriter closes the underlying stream / writer when 'Close/Dispose' is called on it;
            //  - Use the default indentation setting -- two space characters.
            //
            // We configure the same settings in XmlWriterSettings when refactoring this code to use XmlWriter:
            //  - The default encoding used by XmlWriterSettings is 'UTF-8', but we call it out explicitly anyway;
            //  - Set CloseOutput to true;
            //  - Set Indent to true, and by default, IndentChars is two space characters.
            //
            // We use XmlWriterSettings.OmitXmlDeclaration instead of XmlWriter.WriteStartDocument because the
            // xml writer created by calling XmlWriter.Create(Stream, XmlWriterSettings) will write out the xml
            // declaration even without calling WriteStartDocument().

            var xmlSettings = new XmlWriterSettings();
            xmlSettings.Encoding = Encoding.UTF8;
            xmlSettings.CloseOutput = true;
            xmlSettings.Indent = true;

            if (As.Equals("Stream", StringComparison.OrdinalIgnoreCase))
            {
                // Omit xml declaration in this case because we will write out the declaration string in BeginProcess.
                xmlSettings.OmitXmlDeclaration = true;
            }

            _xw = XmlWriter.Create(_ms, xmlSettings);

            if (Depth == 0)
            {
                _serializer = new CustomSerialization(_xw, NoTypeInformation);
            }
            else
            {
                _serializer = new CustomSerialization(_xw, NoTypeInformation, Depth);
            }
        }

        
        private void CleanUp()
        {
            if (_ms != null)
            {
                if (_xw != null)
                {
                    _xw.Dispose();
                    _xw = null;
                }

                _ms.Dispose();
                _ms = null;
            }
        }

        #endregion memory

        #region IDisposable Members

        
        private bool _disposed;

        
        public
        void
        Dispose()
        {
            if (!_disposed)
            {
                CleanUp();
            }

            _disposed = true;
        }

        #endregion IDisposable Members
    }

    
    [Cmdlet(VerbsData.ConvertTo, "CliXml", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2280866")]
    [OutputType(typeof(string))]
    public sealed class ConvertToClixmlCommand : PSCmdlet
    {
        #region Parameters

        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int Depth { get; set; } = 2;

        #endregion Parameters

        #region Private Members

        private readonly List<object> _inputObjectBuffer = new();

        #endregion Private Members

        #region Overrides

        
        protected override void ProcessRecord()
        {
            _inputObjectBuffer.Add(InputObject);
        }

        
        protected override void EndProcessing()
        {
            WriteObject(PSSerializer.Serialize(_inputObjectBuffer, Depth, enumerate: true));
        }

        #endregion Overrides
    }

    
    [Cmdlet(VerbsData.ConvertFrom, "CliXml", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2280770")]
    public sealed class ConvertFromClixmlCommand : PSCmdlet
    {
        #region Parameters

        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string InputObject { get; set; }

        #endregion Parameters

        #region Overrides

        
        protected override void ProcessRecord()
        {
            WriteObject(PSSerializer.Deserialize(InputObject));
        }

        #endregion Overrides
    }

    
    internal sealed class ImportXmlHelper : IDisposable
    {
        #region constructor

        
        private readonly string _path;

        
        private readonly PSCmdlet _cmdlet;
        private readonly bool _isLiteralPath;

        internal ImportXmlHelper(string fileName, PSCmdlet cmdlet, bool isLiteralPath)
        {
            Dbg.Assert(fileName != null, "filename is mandatory");
            Dbg.Assert(cmdlet != null, "cmdlet is mandatory");
            _path = fileName;
            _cmdlet = cmdlet;
            _isLiteralPath = isLiteralPath;
        }

        #endregion constructor

        #region file

        
        internal FileStream _fs;

        
        internal XmlReader _xr;

        private static XmlReader CreateXmlReader(Stream stream)
        {
            TextReader textReader = new StreamReader(stream);

            // skip #< CLIXML directive
            const string cliXmlDirective = "#< CLIXML";
            if (textReader.Peek() == (int)cliXmlDirective[0])
            {
                string line = textReader.ReadLine();
                if (!line.Equals(cliXmlDirective, StringComparison.Ordinal))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }

            return XmlReader.Create(textReader, InternalDeserializer.XmlReaderSettingsForCliXml);
        }

        internal void CreateFileStream()
        {
            _fs = PathUtils.OpenFileStream(_path, _cmdlet, _isLiteralPath);
            _xr = CreateXmlReader(_fs);
        }

        private void CleanUp()
        {
            if (_fs != null)
            {
                _fs.Dispose();
                _fs = null;
            }
        }

        #endregion file

        #region IDisposable Members

        
        private bool _disposed;

        
        public void Dispose()
        {
            if (!_disposed)
            {
                CleanUp();
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Members

        private Deserializer _deserializer;

        internal void Import()
        {
            CreateFileStream();
            _deserializer = new Deserializer(_xr);
            // If total count has been requested, return a dummy object with zero confidence
            if (_cmdlet.PagingParameters.IncludeTotalCount)
            {
                PSObject totalCount = _cmdlet.PagingParameters.NewTotalCount(0, 0);
                _cmdlet.WriteObject(totalCount);
            }

            ulong skip = _cmdlet.PagingParameters.Skip;
            ulong first = _cmdlet.PagingParameters.First;

            // if paging is not specified then keep the old V2 behavior
            if (skip == 0 && first == ulong.MaxValue)
            {
                while (!_deserializer.Done())
                {
                    object result = _deserializer.Deserialize();
                    _cmdlet.WriteObject(result);
                }
            }
            // else try to flatten the output if possible
            else
            {
                ulong skipped = 0;
                ulong count = 0;
                while (!_deserializer.Done() && count < first)
                {
                    object result = _deserializer.Deserialize();

                    if (result is PSObject psObject)
                    {
                        if (psObject.BaseObject is ICollection c)
                        {
                            foreach (object o in c)
                            {
                                if (count >= first)
                                    break;

                                if (skipped++ >= skip)
                                {
                                    count++;
                                    _cmdlet.WriteObject(o);
                                }
                            }
                        }
                        else
                        {
                            if (skipped++ >= skip)
                            {
                                count++;
                                _cmdlet.WriteObject(result);
                            }
                        }
                    }
                    else if (skipped++ >= skip)
                    {
                        count++;
                        _cmdlet.WriteObject(result);
                        continue;
                    }
                }
            }
        }

        internal void Stop() => _deserializer?.Stop();
    }

    #region Select-Xml
    
    [Cmdlet(VerbsCommon.Select, "Xml", DefaultParameterSetName = "Xml", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097031")]
    [OutputType(typeof(SelectXmlInfo))]
    public class SelectXmlCommand : PSCmdlet
    {
        #region parameters
        
        [Parameter(Position = 1, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string[] Path { get; set; }

        
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "LiteralPath")]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get
            {
                return Path;
            }

            set
            {
                Path = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true,
                   ParameterSetName = "Xml")]
        [ValidateNotNullOrEmpty]
        [Alias("Node")]
        public System.Xml.XmlNode[] Xml { get; set; }

        
        [Parameter(Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = "Content")]
        [ValidateNotNullOrEmpty]
        public string[] Content { get; set; }

        
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string XPath { get; set; }

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public Hashtable Namespace { get; set; }

        #endregion parameters

        #region private

        private void WriteResults(XmlNodeList foundXmlNodes, string filePath)
        {
            Dbg.Assert(foundXmlNodes != null, "Caller should verify foundNodes != null");

            foreach (XmlNode foundXmlNode in foundXmlNodes)
            {
                SelectXmlInfo selectXmlInfo = new();
                selectXmlInfo.Node = foundXmlNode;
                selectXmlInfo.Pattern = XPath;
                selectXmlInfo.Path = filePath;

                this.WriteObject(selectXmlInfo);
            }
        }

        private void ProcessXmlNode(XmlNode xmlNode, string filePath)
        {
            Dbg.Assert(xmlNode != null, "Caller should verify xmlNode != null");

            XmlNodeList xList;
            if (Namespace != null)
            {
                XmlNamespaceManager xmlns = AddNameSpaceTable(this.ParameterSetName, xmlNode as XmlDocument, Namespace);
                xList = xmlNode.SelectNodes(XPath, xmlns);
            }
            else
            {
                xList = xmlNode.SelectNodes(XPath);
            }

            this.WriteResults(xList, filePath);
        }

        private void ProcessXmlFile(string filePath)
        {
            // Cannot use ImportXMLHelper because it will throw terminating error which will
            // not be inline with Select-String
            // So doing self processing of the file.
            try
            {
                XmlDocument xmlDocument = InternalDeserializer.LoadUnsafeXmlDocument(
                    new FileInfo(filePath),
                    true, 
                    null); 

                this.ProcessXmlNode(xmlDocument, filePath);
            }
            catch (NotSupportedException notSupportedException)
            {
                this.WriteFileReadError(filePath, notSupportedException);
            }
            catch (IOException ioException)
            {
                this.WriteFileReadError(filePath, ioException);
            }
            catch (SecurityException securityException)
            {
                this.WriteFileReadError(filePath, securityException);
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                this.WriteFileReadError(filePath, unauthorizedAccessException);
            }
            catch (XmlException xmlException)
            {
                this.WriteFileReadError(filePath, xmlException);
            }
            catch (InvalidOperationException invalidOperationException)
            {
                this.WriteFileReadError(filePath, invalidOperationException);
            }
        }

        private void WriteFileReadError(string filePath, Exception exception)
        {
            string errorMessage = string.Format(
                CultureInfo.InvariantCulture,
                // filePath is culture invariant, exception message is to be copied verbatim
                UtilityCommonStrings.FileReadError,
                filePath,
                exception.Message);

            ArgumentException argumentException = new(errorMessage, exception);
            ErrorRecord errorRecord = new(argumentException, "ProcessingFile", ErrorCategory.InvalidArgument, filePath);

            this.WriteError(errorRecord);
        }

        private XmlNamespaceManager AddNameSpaceTable(string parametersetname, XmlDocument xDoc, Hashtable namespacetable)
        {
            XmlNamespaceManager xmlns;
            if (parametersetname.Equals("Xml"))
            {
                XmlNameTable xmlnt = new NameTable();
                xmlns = new XmlNamespaceManager(xmlnt);
            }
            else
            {
                xmlns = new XmlNamespaceManager(xDoc.NameTable);
            }

            foreach (DictionaryEntry row in namespacetable)
            {
                try
                {
                    xmlns.AddNamespace(row.Key.ToString(), row.Value.ToString());
                }
                catch (NullReferenceException)
                {
                    string message = StringUtil.Format(UtilityCommonStrings.SearchXMLPrefixNullError);
                    InvalidOperationException e = new(message);
                    ErrorRecord er = new(e, "PrefixError", ErrorCategory.InvalidOperation, namespacetable);
                    WriteError(er);
                }
                catch (ArgumentNullException)
                {
                    string message = StringUtil.Format(UtilityCommonStrings.SearchXMLPrefixNullError);
                    InvalidOperationException e = new(message);
                    ErrorRecord er = new(e, "PrefixError", ErrorCategory.InvalidOperation, namespacetable);
                    WriteError(er);
                }
            }

            return xmlns;
        }

        #endregion private

        #region override

        
        protected override void ProcessRecord()
        {
            if (ParameterSetName.Equals("Xml", StringComparison.OrdinalIgnoreCase))
            {
                foreach (XmlNode xmlNode in this.Xml)
                {
                    ProcessXmlNode(xmlNode, string.Empty);
                }
            }
            else if (
                (ParameterSetName.Equals("Path", StringComparison.OrdinalIgnoreCase) ||
                (ParameterSetName.Equals("LiteralPath", StringComparison.OrdinalIgnoreCase))))
            {
                // If any file not resolved, execution stops. this is to make consistent with select-string.
                List<string> fullresolvedPaths = new();
                foreach (string fpath in Path)
                {
                    if (_isLiteralPath)
                    {
                        string resolvedPath = GetUnresolvedProviderPathFromPSPath(fpath);
                        fullresolvedPaths.Add(resolvedPath);
                    }
                    else
                    {
                        ProviderInfo provider;
                        Collection<string> resolvedPaths = GetResolvedProviderPathFromPSPath(fpath, out provider);
                        if (!provider.NameEquals(this.Context.ProviderNames.FileSystem))
                        {
                            // Cannot open File error
                            string message = StringUtil.Format(UtilityCommonStrings.FileOpenError, provider.FullName);
                            InvalidOperationException e = new(message);
                            ErrorRecord er = new(e, "ProcessingFile", ErrorCategory.InvalidOperation, fpath);
                            WriteError(er);
                            continue;
                        }

                        fullresolvedPaths.AddRange(resolvedPaths);
                    }
                }

                foreach (string file in fullresolvedPaths)
                {
                    ProcessXmlFile(file);
                }
            }
            else if (ParameterSetName.Equals("Content", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string xmlstring in Content)
                {
                    XmlDocument xmlDocument;
                    try
                    {
                        xmlDocument = (XmlDocument)LanguagePrimitives.ConvertTo(xmlstring, typeof(XmlDocument), CultureInfo.InvariantCulture);
                    }
                    catch (PSInvalidCastException invalidCastException)
                    {
                        this.WriteError(invalidCastException.ErrorRecord);
                        continue;
                    }

                    this.ProcessXmlNode(xmlDocument, string.Empty);
                }
            }
            else
            {
                Dbg.Assert(false, "Unrecognized parameterset");
            }
        }
        #endregion overrides
    }

    
    public sealed class SelectXmlInfo
    {
        
        private const string inputStream = "InputStream";
        private const string MatchFormat = "{0}:{1}";
        private const string SimpleFormat = "{0}";

        
        public XmlNode Node { get; set; }

        
        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _path = inputStream;
                }
                else
                {
                    _path = value;
                }
            }
        }

        private string _path;

        
        public string Pattern { get; set; }

        
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(null);
        }

        
        /// <param name="directory"></param>
        /// <returns></returns>
        private string ToString(string directory)
        {
            string displayPath = (directory != null) ? RelativePath(directory) : _path;
            return FormatLine(GetNodeText(), displayPath);
        }

        
        /// <returns></returns>
        internal string GetNodeText()
        {
            string nodeText = string.Empty;
            if (Node != null)
            {
                if (Node.Value != null)
                {
                    nodeText = Node.Value.Trim();
                }
                else
                {
                    nodeText = Node.InnerXml.Trim();
                }
            }

            return nodeText;
        }

        
        /// <param name="directory">The directory base the truncation on.</param>
        /// <returns>The relative path that was produced.</returns>
        private string RelativePath(string directory)
        {
            string relPath = _path;
            if (!relPath.Equals(inputStream))
            {
                if (relPath.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
                {
                    int offset = directory.Length;
                    if (offset < relPath.Length)
                    {
                        if (directory[offset - 1] == '\\' || directory[offset - 1] == '/')
                            relPath = relPath.Substring(offset);
                        else if (relPath[offset] == '\\' || relPath[offset] == '/')
                            relPath = relPath.Substring(offset + 1);
                    }
                }
            }

            return relPath;
        }

        
        /// <param name="text"></param>
        /// <param name="displaypath"></param>
        /// <returns></returns>
        private string FormatLine(string text, string displaypath)
        {
            if (_path.Equals(inputStream))
            {
                return StringUtil.Format(SimpleFormat, text);
            }
            else
            {
                return StringUtil.Format(MatchFormat, text, displaypath);
            }
        }
    }
    #endregion Select-Xml
}
