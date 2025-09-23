// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Help;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Net;

namespace Microsoft.PowerShell.Commands
{
    
    public class UpdatableHelpCommandBase : PSCmdlet
    {
        internal const string PathParameterSetName = "Path";
        internal const string LiteralPathParameterSetName = "LiteralPath";

        internal UpdatableHelpCommandType _commandType;
        internal UpdatableHelpSystem _helpSystem;
        internal bool _stopping;

        internal int activityId;
        private readonly Dictionary<string, UpdatableHelpExceptionContext> _exceptions;

        #region Parameters

        
        [Parameter(Position = 2)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public CultureInfo[] UICulture
        {
            get
            {
                CultureInfo[] result = null;
                if (_language != null)
                {
                    result = new CultureInfo[_language.Length];
                    for (int index = 0; index < _language.Length; index++)
                    {
                        result[index] = new CultureInfo(_language[index]);
                    }
                }

                return result;
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                _language = new string[value.Length];
                for (int index = 0; index < value.Length; index++)
                {
                    _language[index] = value[index].Name;
                }
            }
        }

        internal string[] _language;

        
        [Parameter]
        [Credential]
        public PSCredential Credential
        {
            get { return _credential; }

            set { _credential = value; }
        }

        internal PSCredential _credential;

        
        [Parameter]
        public SwitchParameter UseDefaultCredentials
        {
            get
            {
                return _useDefaultCredentials;
            }

            set
            {
                _useDefaultCredentials = value;
            }
        }

        private bool _useDefaultCredentials = false;

        
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

        internal bool _force;

        
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public UpdateHelpScope Scope
        {
            get;
            set;
        }

        #endregion

        #region Events

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void HandleProgressChanged(object sender, UpdatableHelpProgressEventArgs e)
        {
            Debug.Assert(e.CommandType == UpdatableHelpCommandType.UpdateHelpCommand
                || e.CommandType == UpdatableHelpCommandType.SaveHelpCommand);

            string activity = (e.CommandType == UpdatableHelpCommandType.UpdateHelpCommand) ?
                HelpDisplayStrings.UpdateProgressActivityForModule : HelpDisplayStrings.SaveProgressActivityForModule;

            ProgressRecord progress = new ProgressRecord(activityId, StringUtil.Format(activity, e.ModuleName), e.ProgressStatus);

            progress.PercentComplete = e.ProgressPercent;

            WriteProgress(progress);
        }

        #endregion

        #region Constructor

        private static readonly Dictionary<string, string> s_metadataCache;

        
        static UpdatableHelpCommandBase()
        {
            s_metadataCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // NOTE: The HelpInfoUri must be updated with each release.

            s_metadataCache.Add("Microsoft.PowerShell.Diagnostics", "https://aka.ms/powershell75-help");
            s_metadataCache.Add("Microsoft.PowerShell.Core", "https://aka.ms/powershell75-help");
            s_metadataCache.Add("Microsoft.PowerShell.Utility", "https://aka.ms/powershell75-help");
            s_metadataCache.Add("Microsoft.PowerShell.Host", "https://aka.ms/powershell75-help");
            s_metadataCache.Add("Microsoft.PowerShell.Management", "https://aka.ms/powershell75-help");
            s_metadataCache.Add("Microsoft.PowerShell.Security", "https://aka.ms/powershell75-help");
            s_metadataCache.Add("Microsoft.WSMan.Management", "https://aka.ms/powershell75-help");
        }

        
        /// <param name="module">Module name.</param>
        /// <returns>True if system module, false if not.</returns>
        internal static bool IsSystemModule(string module)
        {
            return s_metadataCache.ContainsKey(module);
        }

        
        /// <param name="commandType">Command type.</param>
        internal UpdatableHelpCommandBase(UpdatableHelpCommandType commandType)
        {
            _commandType = commandType;
            _helpSystem = new UpdatableHelpSystem(this, _useDefaultCredentials);
            _exceptions = new Dictionary<string, UpdatableHelpExceptionContext>();
            _helpSystem.OnProgressChanged += HandleProgressChanged;

            activityId = Random.Shared.Next();
        }

        #endregion

        #region Implementation

        private void ProcessSingleModuleObject(PSModuleInfo module, ExecutionContext context, Dictionary<Tuple<string, Version>, UpdatableHelpModuleInfo> helpModules, bool noErrors)
        {
            if (InitialSessionState.IsEngineModule(module.Name) && !InitialSessionState.IsNestedEngineModule(module.Name))
            {
                WriteDebug(StringUtil.Format("Found engine module: {0}, {1}.", module.Name, module.Guid));

                var keyTuple = new Tuple<string, Version>(module.Name, module.Version);
                if (!helpModules.ContainsKey(keyTuple))
                {
                    helpModules.Add(keyTuple, new UpdatableHelpModuleInfo(module.Name, module.Guid,
                        Utils.GetApplicationBase(context.ShellID), s_metadataCache[module.Name]));
                }

                return;
            }
            else if (InitialSessionState.IsNestedEngineModule(module.Name))
            {
                return;
            }

            if (string.IsNullOrEmpty(module.HelpInfoUri))
            {
                if (!noErrors)
                {
                    ProcessException(module.Name, null, new UpdatableHelpSystemException(
                        "HelpInfoUriNotFound", StringUtil.Format(HelpDisplayStrings.HelpInfoUriNotFound),
                        ErrorCategory.NotSpecified, new Uri("HelpInfoUri", UriKind.Relative), null));
                }

                return;
            }

            if (!(module.HelpInfoUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || module.HelpInfoUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                if (!noErrors)
                {
                    ProcessException(module.Name, null, new UpdatableHelpSystemException(
                        "InvalidHelpInfoUriFormat", StringUtil.Format(HelpDisplayStrings.InvalidHelpInfoUriFormat, module.HelpInfoUri),
                        ErrorCategory.NotSpecified, new Uri("HelpInfoUri", UriKind.Relative), null));
                }

                return;
            }

            var keyTuple2 = new Tuple<string, Version>(module.Name, module.Version);
            if (!helpModules.ContainsKey(keyTuple2))
            {
                helpModules.Add(keyTuple2, new UpdatableHelpModuleInfo(module.Name, module.Guid, module.ModuleBase, module.HelpInfoUri));
            }
        }

        
        /// <param name="context">Execution context.</param>
        /// <param name="pattern">Pattern to search.</param>
        /// <param name="fullyQualifiedName">Module Specification.</param>
        /// <param name="noErrors">Do not generate errors for modules without HelpInfoUri.</param>
        /// <returns>A list of modules.</returns>
        private Dictionary<Tuple<string, Version>, UpdatableHelpModuleInfo> GetModuleInfo(ExecutionContext context, string pattern, ModuleSpecification fullyQualifiedName, bool noErrors)
        {
            List<PSModuleInfo> modules = null;
            string moduleNamePattern = null;

            if (pattern != null)
            {
                moduleNamePattern = pattern;
                modules = Utils.GetModules(pattern, context);
            }
            else if (fullyQualifiedName != null)
            {
                moduleNamePattern = fullyQualifiedName.Name;
                modules = Utils.GetModules(fullyQualifiedName, context);
            }

            var helpModules = new Dictionary<Tuple<string, Version>, UpdatableHelpModuleInfo>();
            if (modules != null)
            {
                foreach (PSModuleInfo module in modules)
                {
                    ProcessSingleModuleObject(module, context, helpModules, noErrors);
                }
            }

            IEnumerable<WildcardPattern> patternList = SessionStateUtilities.CreateWildcardsFromStrings(
                globPatterns: new[] { moduleNamePattern },
                options: WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);

            foreach (KeyValuePair<string, string> name in s_metadataCache)
            {
                if (SessionStateUtilities.MatchesAnyWildcardPattern(name.Key, patternList, true))
                {
                    // For core snapin, there are no GUIDs. So, we need to construct the HelpInfo slightly differently
                    if (!name.Key.Equals(InitialSessionState.CoreSnapin, StringComparison.OrdinalIgnoreCase))
                    {
                        var keyTuple = new Tuple<string, Version>(name.Key, new Version("1.0"));
                        if (!helpModules.ContainsKey(keyTuple))
                        {
                            List<PSModuleInfo> availableModules = Utils.GetModules(name.Key, context);
                            if (availableModules != null)
                            {
                                foreach (PSModuleInfo module in availableModules)
                                {
                                    keyTuple = new Tuple<string, Version>(module.Name, module.Version);
                                    if (!helpModules.ContainsKey(keyTuple))
                                    {
                                        WriteDebug(StringUtil.Format("Found engine module: {0}, {1}.", module.Name, module.Guid));

                                        helpModules.Add(keyTuple, new UpdatableHelpModuleInfo(module.Name,
                                            module.Guid, Utils.GetApplicationBase(context.ShellID), s_metadataCache[module.Name]));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var keyTuple2 = new Tuple<string, Version>(name.Key, new Version("1.0"));
                        if (!helpModules.ContainsKey(keyTuple2))
                        {
                            helpModules.Add(keyTuple2,
                                            new UpdatableHelpModuleInfo(name.Key, Guid.Empty,
                                                                        Utils.GetApplicationBase(context.ShellID),
                                                                        name.Value));
                        }
                    }
                }
            }

            return helpModules;
        }

        
        protected override void StopProcessing()
        {
            _stopping = true;
            _helpSystem.CancelDownload();
        }

        
        protected override void EndProcessing()
        {
            foreach (UpdatableHelpExceptionContext exception in _exceptions.Values)
            {
                UpdatableHelpExceptionContext e = exception;

                if ((exception.Exception.FullyQualifiedErrorId == "HelpCultureNotSupported") &&
                    ((exception.Cultures != null && exception.Cultures.Count > 1) ||
                    (exception.Modules != null && exception.Modules.Count > 1)))
                {
                    // Win8: 744749 Rewriting the error message only in the case where either
                    // multiple cultures or multiple modules are involved.
                    e = new UpdatableHelpExceptionContext(new UpdatableHelpSystemException(
                        "HelpCultureNotSupported", StringUtil.Format(HelpDisplayStrings.CannotMatchUICulturePattern,
                        string.Join(", ", exception.Cultures)),
                        ErrorCategory.InvalidArgument, exception.Cultures, null));
                    e.Modules = exception.Modules;
                    e.Cultures = exception.Cultures;
                }

                WriteError(e.CreateErrorRecord(_commandType));

                LogContext context = MshLog.GetLogContext(Context, MyInvocation);

                context.Severity = "Error";

                
            }
        }

        
        /// <param name="moduleNames">Module names given by the user.</param>
        /// <param name="fullyQualifiedNames">FullyQualifiedNames.</param>
        internal void Process(IEnumerable<string> moduleNames, IEnumerable<ModuleSpecification> fullyQualifiedNames)
        {
            _helpSystem.UseDefaultCredentials = _useDefaultCredentials;

            if (moduleNames != null)
            {
                foreach (string name in moduleNames)
                {
                    if (_stopping)
                    {
                        break;
                    }

                    ProcessModuleWithGlobbing(name);
                }
            }
            else if (fullyQualifiedNames != null)
            {
                foreach (var fullyQualifiedName in fullyQualifiedNames)
                {
                    if (_stopping)
                    {
                        break;
                    }

                    ProcessModuleWithGlobbing(fullyQualifiedName);
                }
            }
            else
            {
                foreach (KeyValuePair<Tuple<string, Version>, UpdatableHelpModuleInfo> module in GetModuleInfo("*", null, true))
                {
                    if (_stopping)
                    {
                        break;
                    }

                    ProcessModule(module.Value);
                }
            }
        }

        
        /// <param name="modules">Module objects given by the user.</param>
        internal void Process(IEnumerable<PSModuleInfo> modules)
        {
            if (modules == null || !modules.Any())
            {
                return;
            }

            var helpModules = new Dictionary<Tuple<string, Version>, UpdatableHelpModuleInfo>();

            foreach (PSModuleInfo module in modules)
            {
                ProcessSingleModuleObject(module, Context, helpModules, false);
            }

            foreach (KeyValuePair<Tuple<string, Version>, UpdatableHelpModuleInfo> helpModule in helpModules)
            {
                ProcessModule(helpModule.Value);
            }
        }

        
        /// <param name="name">Module name with globbing.</param>
        private void ProcessModuleWithGlobbing(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                PSArgumentException e = new PSArgumentException(StringUtil.Format(HelpDisplayStrings.ModuleNameNullOrEmpty));
                WriteError(e.ErrorRecord);
                return;
            }

            foreach (KeyValuePair<Tuple<string, Version>, UpdatableHelpModuleInfo> module in GetModuleInfo(name, null, false))
            {
                ProcessModule(module.Value);
            }
        }

        
        /// <param name="fullyQualifiedName">ModuleSpecification.</param>
        private void ProcessModuleWithGlobbing(ModuleSpecification fullyQualifiedName)
        {
            foreach (KeyValuePair<Tuple<string, Version>, UpdatableHelpModuleInfo> module in GetModuleInfo(null, fullyQualifiedName, false))
            {
                ProcessModule(module.Value);
            }
        }

        
        /// <param name="module">Module to process.</param>
        private void ProcessModule(UpdatableHelpModuleInfo module)
        {
            _helpSystem.CurrentModule = module.ModuleName;

            if (this is UpdateHelpCommand && !Directory.Exists(module.ModuleBase))
            {
                ProcessException(module.ModuleName, null,
                    new UpdatableHelpSystemException("ModuleBaseMustExist",
                        StringUtil.Format(HelpDisplayStrings.ModuleBaseMustExist),
                        ErrorCategory.InvalidOperation, null, null));
                return;
            }

            // Win8: 572882 When the system locale is English and the UI is JPN,
            // running "update-help" still downs English help content.
            var cultures = _language ?? _helpSystem.GetCurrentUICulture();
            UpdatableHelpSystemException implicitCultureNotSupported = null;

            foreach (string culture in cultures)
            {
                bool installed = true;

                if (_stopping)
                {
                    break;
                }

                try
                {
                    ProcessModuleWithCulture(module, culture);
                }
                catch (IOException e)
                {
                    ProcessException(module.ModuleName, culture, new UpdatableHelpSystemException("FailedToCopyFile",
                        e.Message, ErrorCategory.InvalidOperation, null, e));
                }
                catch (UnauthorizedAccessException e)
                {
                    ProcessException(module.ModuleName, culture, new UpdatableHelpSystemException("AccessIsDenied",
                        e.Message, ErrorCategory.PermissionDenied, null, e));
                }
#if !CORECLR
                catch (WebException e)
                {
                    if (e.InnerException != null && e.InnerException is UnauthorizedAccessException)
                    {
                        ProcessException(module.ModuleName, culture, new UpdatableHelpSystemException("AccessIsDenied",
                            e.InnerException.Message, ErrorCategory.PermissionDenied, null, e));
                    }
                    else
                    {
                        ProcessException(module.ModuleName, culture, e);
                    }
                }
#endif
                catch (UpdatableHelpSystemException e)
                {
                    if (e.FullyQualifiedErrorId == "HelpCultureNotSupported"
                            || e.FullyQualifiedErrorId == "UnableToRetrieveHelpInfoXml")
                    {
                        installed = false;

                        if (_language != null)
                        {
                            // Display the error message only if we are not using the fallback chain
                            ProcessException(module.ModuleName, culture, e);
                        }
                        else
                        {
                            // Hold first exception, it will be displayed if fallback chain fails
                            WriteVerbose(StringUtil.Format(HelpDisplayStrings.HelpCultureNotSupportedFallback, e.Message));
                            implicitCultureNotSupported ??= e;
                        }
                    }
                    else
                    {
                        ProcessException(module.ModuleName, culture, e);
                    }
                }
                catch (Exception e)
                {
                    ProcessException(module.ModuleName, culture, e);
                }
                finally
                {
                    if (_helpSystem.Errors.Count != 0)
                    {
                        foreach (Exception error in _helpSystem.Errors)
                        {
                            ProcessException(module.ModuleName, culture, error);
                        }

                        _helpSystem.Errors.Clear();
                    }
                }

                // If -UICulture is not specified, we only install
                // one culture from the fallback chain
                if (_language == null && installed)
                {
                    return;
                }
            }

            // If the exception is not null and did not return early, then all of the fallback chain failed
            if (implicitCultureNotSupported != null)
            {
                ProcessException(module.ModuleName, cultures.First(), implicitCultureNotSupported);
            }
        }

        
        /// <param name="module">Module to process.</param>
        /// <param name="culture">Culture to use.</param>
        /// <returns>True if the module has been processed, false if not.</returns>
        internal virtual bool ProcessModuleWithCulture(UpdatableHelpModuleInfo module, string culture)
        {
            return false;
        }

        #endregion

        #region Common methods

        
        /// <param name="pattern">Pattern to match.</param>
        /// <param name="fullyQualifiedName">ModuleSpecification.</param>
        /// <param name="noErrors">Skip errors.</param>
        /// <returns>A list of modules.</returns>
        internal Dictionary<Tuple<string, Version>, UpdatableHelpModuleInfo> GetModuleInfo(string pattern, ModuleSpecification fullyQualifiedName, bool noErrors)
        {
            Dictionary<Tuple<string, Version>, UpdatableHelpModuleInfo> modules = GetModuleInfo(Context, pattern, fullyQualifiedName, noErrors);

            if (modules.Count == 0 && _exceptions.Count == 0 && !noErrors)
            {
                var errorMessage = fullyQualifiedName != null ? StringUtil.Format(HelpDisplayStrings.ModuleNotFoundWithFullyQualifiedName, fullyQualifiedName)
                                                              : StringUtil.Format(HelpDisplayStrings.CannotMatchModulePattern, pattern);

                ErrorRecord errorRecord = new ErrorRecord(new Exception(errorMessage),
                    "ModuleNotFound", ErrorCategory.InvalidArgument, pattern);

                WriteError(errorRecord);
            }

            return modules;
        }

        
        /// <param name="module">ModuleInfo.</param>
        /// <param name="currentHelpInfo">Current HelpInfo.xml.</param>
        /// <param name="newHelpInfo">New HelpInfo.xml.</param>
        /// <param name="culture">Current culture.</param>
        /// <param name="force">Force update.</param>
        /// <returns>True if it is necessary to update help, false if not.</returns>
        internal bool IsUpdateNecessary(UpdatableHelpModuleInfo module, UpdatableHelpInfo currentHelpInfo,
            UpdatableHelpInfo newHelpInfo, CultureInfo culture, bool force)
        {
            Debug.Assert(module != null);

            if (newHelpInfo == null)
            {
                throw new UpdatableHelpSystemException("UnableToRetrieveHelpInfoXml",
                    StringUtil.Format(HelpDisplayStrings.UnableToRetrieveHelpInfoXml, culture.Name), ErrorCategory.ResourceUnavailable,
                    null, null);
            }

            // Culture check
            if (!newHelpInfo.IsCultureSupported(culture.Name))
            {
                throw new UpdatableHelpSystemException("HelpCultureNotSupported",
                    StringUtil.Format(HelpDisplayStrings.HelpCultureNotSupported,
                    culture.Name, newHelpInfo.GetSupportedCultures()), ErrorCategory.InvalidOperation, null, null);
            }

            // Version check
            if (!force && currentHelpInfo != null && !currentHelpInfo.IsNewerVersion(newHelpInfo, culture))
            {
                return false;
            }

            return true;
        }

        
        /// <param name="moduleName">Module name.</param>
        /// <param name="path">Path to help info.</param>
        /// <param name="filename">Help info file name.</param>
        /// <param name="time">Current time (UTC).</param>
        /// <param name="force">If -Force is specified.</param>
        /// <returns>True if we are okay to update, false if not.</returns>
        internal bool CheckOncePerDayPerModule(string moduleName, string path, string filename, DateTime time, bool force)
        {
            // Update if -Force is specified
            if (force)
            {
                return true;
            }

            string helpInfoFilePath = SessionState.Path.Combine(path, filename);

            // No HelpInfo.xml
            if (!File.Exists(helpInfoFilePath))
            {
                return true;
            }

            DateTime lastModified = File.GetLastWriteTimeUtc(helpInfoFilePath);
            TimeSpan difference = time - lastModified;

            if (difference.Days >= 1)
            {
                return true;
            }

            if (_commandType == UpdatableHelpCommandType.UpdateHelpCommand)
            {
                WriteVerbose(StringUtil.Format(HelpDisplayStrings.UseForceToUpdateHelp, moduleName));
            }
            else if (_commandType == UpdatableHelpCommandType.SaveHelpCommand)
            {
                WriteVerbose(StringUtil.Format(HelpDisplayStrings.UseForceToSaveHelp, moduleName));
            }

            return false;
        }

        
        /// <param name="path">Path to resolve.</param>
        /// <param name="recurse">Resolve recursively?</param>
        /// <param name="isLiteralPath">Treat the path / start path as a literal path?</param>///
        /// <returns>A list of directories.</returns>
        internal IEnumerable<string> ResolvePath(string path, bool recurse, bool isLiteralPath)
        {
            List<string> resolvedPaths = new List<string>();

            if (isLiteralPath)
            {
                string newPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);

                if (!Directory.Exists(newPath))
                {
                    throw new UpdatableHelpSystemException("PathMustBeValidContainers",
                        StringUtil.Format(HelpDisplayStrings.PathMustBeValidContainers, path), ErrorCategory.InvalidArgument,
                        null, new ItemNotFoundException());
                }

                resolvedPaths.Add(newPath);
            }
            else
            {
                Collection<PathInfo> resolvedPathInfos = SessionState.Path.GetResolvedPSPathFromPSPath(path);
                foreach (PathInfo resolvedPath in resolvedPathInfos)
                {
                    ValidatePathProvider(resolvedPath);

                    resolvedPaths.Add(resolvedPath.ProviderPath);
                }
            }

            foreach (string resolvedPath in resolvedPaths)
            {
                if (recurse)
                {
                    foreach (string innerResolvedPath in RecursiveResolvePathHelper(resolvedPath))
                    {
                        yield return innerResolvedPath;
                    }
                }
                else
                {
                    // Win8: 566738
                    CmdletProviderContext context = new CmdletProviderContext(this.Context);
                    // resolvedPath is already resolved..so no need to expand wildcards anymore
                    context.SuppressWildcardExpansion = true;
                    if (isLiteralPath || InvokeProvider.Item.IsContainer(resolvedPath, context))
                    {
                        yield return resolvedPath;
                    }
                }
            }

            yield break;
        }

        
        /// <param name="path">Path to resolve.</param>
        /// <returns>A list of directories.</returns>
        private static IEnumerable<string> RecursiveResolvePathHelper(string path)
        {
            if (System.IO.Directory.Exists(path))
            {
                yield return path;

                foreach (string subDirectory in Directory.EnumerateDirectories(path))
                {
                    foreach (string subDirectory2 in RecursiveResolvePathHelper(subDirectory))
                    {
                        yield return subDirectory2;
                    }
                }
            }

            yield break;
        }

        #endregion

        #region Static methods

        
        /// <param name="path">Path to validate.</param>
        internal void ValidatePathProvider(PathInfo path)
        {
            if (path.Provider == null || path.Provider.Name != FileSystemProvider.ProviderName)
            {
                throw new PSArgumentException(StringUtil.Format(HelpDisplayStrings.ProviderIsNotFileSystem,
                    path.Path));
            }
        }

        #endregion

        #region Logging

        
        /// <param name="message">Message to log.</param>
        internal void LogMessage(string message)
        {
            List<string> details = new List<string>() { message };
            
        }

        #endregion

        #region Exception processing

        
        /// <param name="moduleName">Module name.</param>
        /// <param name="culture">Culture info.</param>
        /// <param name="e">Exception to check.</param>
        internal void ProcessException(string moduleName, string culture, Exception e)
        {
            UpdatableHelpSystemException except = null;

            if (e is UpdatableHelpSystemException)
            {
                except = (UpdatableHelpSystemException)e;
            }
#if !CORECLR
            else if (e is WebException)
            {
                except = new UpdatableHelpSystemException("UnableToConnect",
                    StringUtil.Format(HelpDisplayStrings.UnableToConnect), ErrorCategory.InvalidOperation, null, e);
            }
#endif
            else if (e is PSArgumentException)
            {
                except = new UpdatableHelpSystemException("InvalidArgument",
                    e.Message, ErrorCategory.InvalidArgument, null, e);
            }
            else
            {
                except = new UpdatableHelpSystemException("UnknownErrorId",
                    e.Message, ErrorCategory.InvalidOperation, null, e);
            }

            if (!_exceptions.ContainsKey(except.FullyQualifiedErrorId))
            {
                _exceptions.Add(except.FullyQualifiedErrorId, new UpdatableHelpExceptionContext(except));
            }

            _exceptions[except.FullyQualifiedErrorId].Modules.Add(moduleName);

            if (culture != null)
            {
                _exceptions[except.FullyQualifiedErrorId].Cultures.Add(culture);
            }
        }

        #endregion
    }

    
    public enum UpdateHelpScope
    {
        
        CurrentUser,

        
        AllUsers
    }
}
