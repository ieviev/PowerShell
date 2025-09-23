// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Text;
using Dbg = System.Management.Automation.Diagnostics;

//
// Now define the set of commands for manipulating modules.
//

namespace Microsoft.PowerShell.Commands
{
    #region New-ModuleManifest
    
    [Cmdlet(VerbsCommon.New, "ModuleManifest", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096487")]
    [OutputType(typeof(string))]
    public sealed class NewModuleManifestCommand : PSCmdlet
    {
        
        [Parameter(Mandatory = true, Position = 0)]
        public string Path
        {
            get { return _path; }

            set { _path = value; }
        }

        private string _path;

        
        [Parameter]
        [AllowEmptyCollection]
        public object[] NestedModules
        {
            get { return _nestedModules; }

            set { _nestedModules = value; }
        }

        private object[] _nestedModules;

        
        [Parameter]
        public Guid Guid
        {
            get { return _guid; }

            set { _guid = value; }
        }

        private Guid _guid = Guid.NewGuid();

        
        [Parameter]
        [AllowEmptyString]
        public string Author
        {
            get { return _author; }

            set { _author = value; }
        }

        private string _author;

        
        [Parameter]
        [AllowEmptyString]
        public string CompanyName
        {
            get { return _companyName; }

            set { _companyName = value; }
        }

        private string _companyName = string.Empty;

        
        [Parameter]
        [AllowEmptyString]
        public string Copyright
        {
            get { return _copyright; }

            set { _copyright = value; }
        }

        private string _copyright;

        
        [Parameter]
        [AllowEmptyString]
        [Alias("ModuleToProcess")]
        public string RootModule
        {
            get { return _rootModule; }

            set { _rootModule = value; }
        }

        private string _rootModule = null;

        
        [Parameter]
        [ValidateNotNull]
        public Version ModuleVersion
        {
            get { return _moduleVersion; }

            set { _moduleVersion = value; }
        }

        private Version _moduleVersion = new Version(0, 0, 1);

        
        [Parameter]
        [AllowEmptyString]
        public string Description
        {
            get { return _description; }

            set { _description = value; }
        }

        private string _description;

        
        [Parameter]
        public ProcessorArchitecture ProcessorArchitecture
        {
            get { return _processorArchitecture ?? ProcessorArchitecture.None; }

            set { _processorArchitecture = value; }
        }

        private ProcessorArchitecture? _processorArchitecture = null;

        
        [Parameter]
        public Version PowerShellVersion
        {
            get { return _powerShellVersion; }

            set { _powerShellVersion = value; }
        }

        private Version _powerShellVersion = null;

        
        [Parameter]
        public Version ClrVersion
        {
            get { return _ClrVersion; }

            set { _ClrVersion = value; }
        }

        private Version _ClrVersion = null;

        
        [Parameter]
        public Version DotNetFrameworkVersion
        {
            get { return _DotNetFrameworkVersion; }

            set { _DotNetFrameworkVersion = value; }
        }

        private Version _DotNetFrameworkVersion = null;

        
        [Parameter]
        public string PowerShellHostName
        {
            get { return _PowerShellHostName; }

            set { _PowerShellHostName = value; }
        }

        private string _PowerShellHostName = null;

        
        [Parameter]
        public Version PowerShellHostVersion
        {
            get { return _PowerShellHostVersion; }

            set { _PowerShellHostVersion = value; }
        }

        private Version _PowerShellHostVersion = null;

        
        [Parameter]
        [ArgumentTypeConverter(typeof(ModuleSpecification[]))]
        public object[] RequiredModules
        {
            get { return _requiredModules; }

            set { _requiredModules = value; }
        }

        private object[] _requiredModules;

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] TypesToProcess
        {
            get { return _types; }

            set { _types = value; }
        }

        private string[] _types;

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] FormatsToProcess
        {
            get { return _formats; }

            set { _formats = value; }
        }

        private string[] _formats;

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] ScriptsToProcess
        {
            get { return _scripts; }

            set { _scripts = value; }
        }

        private string[] _scripts;

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] RequiredAssemblies
        {
            get { return _requiredAssemblies; }

            set { _requiredAssemblies = value; }
        }

        private string[] _requiredAssemblies;

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] FileList
        {
            get { return _miscFiles; }

            set { _miscFiles = value; }
        }

        private string[] _miscFiles;

        
        [Parameter]
        [AllowEmptyCollection]
        [ArgumentTypeConverter(typeof(ModuleSpecification[]))]
        public object[] ModuleList
        {
            get { return _moduleList; }

            set { _moduleList = value; }
        }

        private object[] _moduleList;

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] FunctionsToExport
        {
            get { return _exportedFunctions; }

            set { _exportedFunctions = value; }
        }

        private string[] _exportedFunctions;

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] AliasesToExport
        {
            get { return _exportedAliases; }

            set { _exportedAliases = value; }
        }

        private string[] _exportedAliases;

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] VariablesToExport
        {
            get { return _exportedVariables; }

            set { _exportedVariables = value; }
        }

        private string[] _exportedVariables = new string[] { "*" };

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] CmdletsToExport
        {
            get { return _exportedCmdlets; }

            set { _exportedCmdlets = value; }
        }

        private string[] _exportedCmdlets;

        
        [Parameter]
        [AllowEmptyCollection]
        public string[] DscResourcesToExport
        {
            get { return _dscResourcesToExport; }

            set { _dscResourcesToExport = value; }
        }

        private string[] _dscResourcesToExport;

        
        [Parameter]
        [AllowEmptyCollection]
        [ValidateSet("Desktop", "Core")]
        public string[] CompatiblePSEditions
        {
            get { return _compatiblePSEditions; }

            set { _compatiblePSEditions = value; }
        }

        private string[] _compatiblePSEditions;

        
        [Parameter(Mandatory = false)]
        [AllowNull]
        public object PrivateData
        {
            get { return _privateData; }

            set { _privateData = value; }
        }

        private object _privateData;

        
        [Parameter(Mandatory = false)]
        [ValidateNotNullOrEmpty]
        public string[] Tags { get; set; }

        
        [Parameter(Mandatory = false)]
        [ValidateNotNullOrEmpty]
        public Uri ProjectUri { get; set; }

        
        [Parameter(Mandatory = false)]
        [ValidateNotNullOrEmpty]
        public Uri LicenseUri { get; set; }

        
        [Parameter(Mandatory = false)]
        [ValidateNotNullOrEmpty]
        public Uri IconUri { get; set; }

        
        [Parameter(Mandatory = false)]
        [ValidateNotNullOrEmpty]
        public string ReleaseNotes { get; set; }

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Prerelease { get; set; }

        
        [Parameter]
        public SwitchParameter RequireLicenseAcceptance { get; set; }

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[] ExternalModuleDependencies { get; set; }

        
        [Parameter]
        [AllowNull]
        public string HelpInfoUri
        {
            get { return _helpInfoUri; }

            set { _helpInfoUri = value; }
        }

        private string _helpInfoUri;

        
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return (SwitchParameter)_passThru; }

            set { _passThru = value; }
        }

        private bool _passThru;

        
        [Parameter]
        [AllowNull]
        public string DefaultCommandPrefix
        {
            get { return _defaultCommandPrefix; }

            set { _defaultCommandPrefix = value; }
        }

        private string _defaultCommandPrefix;

        private string _indent = string.Empty;

        
        private static string QuoteName(string name)
        {
            if (name == null)
                return "''";
            return ("'" + name.Replace("'", "''") + "'");
        }

        
        private static string QuoteName(Uri name)
        {
            if (name == null)
                return "''";
            return QuoteName(name.AbsoluteUri);
        }

        
        private static string QuoteName(Version name)
        {
            if (name == null)
                return "''";
            return QuoteName(name.ToString());
        }

        
        private static string QuoteNames(IEnumerable names, StreamWriter streamWriter)
        {
            if (names == null)
                return "@()";

            StringBuilder result = new StringBuilder();

            int offset = 15;
            bool first = true;
            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        result.Append(", ");
                    }

                    string quotedString = QuoteName(name);
                    offset += quotedString.Length;
                    if (offset > 80)
                    {
                        result.Append(streamWriter.NewLine);
                        result.Append("               ");
                        offset = 15 + quotedString.Length;
                    }

                    result.Append(quotedString);
                }
            }

            if (result.Length == 0)
                return "@()";

            return result.ToString();
        }

        
        private static IEnumerable PreProcessModuleSpec(IEnumerable moduleSpecs)
        {
            if (moduleSpecs != null)
            {
                foreach (object spec in moduleSpecs)
                {
                    if (spec is not Hashtable)
                    {
                        yield return spec.ToString();
                    }
                    else
                    {
                        yield return spec;
                    }
                }
            }
        }

        
        private static string QuoteModules(IEnumerable moduleSpecs, StreamWriter streamWriter)
        {
            StringBuilder result = new StringBuilder();
            result.Append("@(");

            if (moduleSpecs != null)
            {
                bool firstModule = true;
                foreach (object spec in moduleSpecs)
                {
                    if (spec == null)
                    {
                        continue;
                    }

                    ModuleSpecification moduleSpecification = (ModuleSpecification)LanguagePrimitives.ConvertTo(
                            spec,
                            typeof(ModuleSpecification),
                            CultureInfo.InvariantCulture);

                    if (!firstModule)
                    {
                        result.Append(", ");
                        result.Append(streamWriter.NewLine);
                        result.Append("               ");
                    }

                    firstModule = false;

                    if ((moduleSpecification.Guid == null) && (moduleSpecification.Version == null) && (moduleSpecification.MaximumVersion == null) && (moduleSpecification.RequiredVersion == null))
                    {
                        result.Append(QuoteName(moduleSpecification.Name));
                    }
                    else
                    {
                        result.Append("@{");

                        result.Append("ModuleName = ");
                        result.Append(QuoteName(moduleSpecification.Name));
                        result.Append("; ");

                        if (moduleSpecification.Guid != null)
                        {
                            result.Append("GUID = ");
                            result.Append(QuoteName(moduleSpecification.Guid.ToString()));
                            result.Append("; ");
                        }

                        if (moduleSpecification.Version != null)
                        {
                            result.Append("ModuleVersion = ");
                            result.Append(QuoteName(moduleSpecification.Version.ToString()));
                            result.Append("; ");
                        }

                        if (moduleSpecification.MaximumVersion != null)
                        {
                            result.Append("MaximumVersion = ");
                            result.Append(QuoteName(moduleSpecification.MaximumVersion));
                            result.Append("; ");
                        }

                        if (moduleSpecification.RequiredVersion != null)
                        {
                            result.Append("RequiredVersion = ");
                            result.Append(QuoteName(moduleSpecification.RequiredVersion.ToString()));
                            result.Append("; ");
                        }

                        result.Append('}');
                    }
                }
            }

            result.Append(')');
            return result.ToString();
        }

        
        private string QuoteFiles(IEnumerable names, StreamWriter streamWriter)
        {
            List<string> resolvedPaths = new List<string>();

            if (names != null)
            {
                foreach (string name in names)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        foreach (string path in TryResolveFilePath(name))
                        {
                            resolvedPaths.Add(path);
                        }
                    }
                }
            }

            return QuoteNames(resolvedPaths, streamWriter);
        }

        //
        // private string QuoteFilesWithWildcard(string basePath, IEnumerable names, string allowedExtension, StreamWriter streamWriter, string item)
        // {
        //    if (names != null)
        //    {
        //        foreach (string name in names)
        //        {
        //            if (string.IsNullOrEmpty(name))
        //                continue;

        //            string fileName = name;

        //            string extension = System.IO.Path.GetExtension(fileName);
        //            if (string.Equals(extension, allowedExtension, StringComparison.OrdinalIgnoreCase))
        //            {
        //                string drive = string.Empty;
        //                if (!SessionState.Path.IsPSAbsolute(fileName, out drive) && !System.IO.Path.IsPathRooted(fileName))
        //                {
        //                    fileName = SessionState.Path.Combine(SessionState.Path.CurrentLocation.ProviderPath, fileName);
        //                }

        //                string basePathDir = System.IO.Path.GetDirectoryName(SessionState.Path.GetUnresolvedProviderPathFromPSPath(basePath));
        //                if (basePathDir[basePathDir.Length - 1] != StringLiterals.DefaultPathSeparator)
        //                {
        //                    basePathDir += StringLiterals.DefaultPathSeparator;
        //                }
        //                string fileDir = null;

        //                // Call to SessionState.Path.GetUnresolvedProviderPathFromPSPath throws an exception
        //                // when the drive in the path does not exist.
        //                // Based on the exception it is obvious that the path is outside the basePath, because
        //                // basePath must always exist.
        //                try
        //                {
        //                    fileDir = System.IO.Path.GetDirectoryName(SessionState.Path.GetUnresolvedProviderPathFromPSPath(fileName));
        //                    if (fileDir[fileDir.Length - 1] != StringLiterals.DefaultPathSeparator)
        //                    {
        //                        fileDir += StringLiterals.DefaultPathSeparator;
        //                    }
        //                }
        //                catch
        //                {
        //                }

        //                if (fileDir == null
        //                    || !fileDir.StartsWith(basePathDir, StringComparison.OrdinalIgnoreCase))
        //                {
        //                    WriteWarning(StringUtil.Format(Modules.IncludedItemPathFallsOutsideSaveTree, name,
        //                        fileDir ?? name, item));
        //                }
        //            }
        //            else
        //            {
        //                string message = StringUtil.Format(Modules.InvalidWorkflowExtension);
        //                InvalidOperationException invalidOp = new InvalidOperationException(message);
        //                ErrorRecord er = new ErrorRecord(invalidOp, "Modules_InvalidWorkflowExtension",
        //                    ErrorCategory.InvalidOperation, null);
        //                ThrowTerminatingError(er);
        //            }
        //        }
        //    }

        //    return QuoteNames(names, streamWriter);
        // }

        
        private List<string> TryResolveFilePath(string filePath)
        {
            List<string> result = new List<string>();
            ProviderInfo provider = null;
            SessionState sessionState = Context.SessionState;
            try
            {
                Collection<string> filePaths =
                    sessionState.Path.GetResolvedProviderPathFromPSPath(filePath, out provider);

                // If the name doesn't resolve to something we can use, just return the unresolved name...
                if (!provider.NameEquals(this.Context.ProviderNames.FileSystem) || filePaths == null || filePaths.Count < 1)
                {
                    result.Add(filePath);
                    return result;
                }

                // Otherwise get the relative resolved path and trim the .\ or ./ because
                // modules are always loaded relative to the manifest base directory.
                foreach (string path in filePaths)
                {
                    string adjustedPath = SessionState.Path.NormalizeRelativePath(path,
                        SessionState.Path.CurrentLocation.ProviderPath);
                    if (adjustedPath.StartsWith(".\\", StringComparison.OrdinalIgnoreCase) ||
                        adjustedPath.StartsWith("./", StringComparison.OrdinalIgnoreCase))
                    {
                        adjustedPath = adjustedPath.Substring(2);
                    }

                    result.Add(adjustedPath);
                }
            }
            catch (ItemNotFoundException)
            {
                result.Add(filePath);
            }

            return result;
        }

        
        private string ManifestFragment(string key, string resourceString, string value, StreamWriter streamWriter)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}# {1}{2}{0}{3:19} = {4}{2}{2}", _indent, resourceString, streamWriter.NewLine, key, value);
        }

        private string ManifestFragmentForNonSpecifiedManifestMember(string key, string resourceString, string value, StreamWriter streamWriter)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}# {1}{2}{0}# {3:19} = {4}{2}{2}", _indent, resourceString, streamWriter.NewLine, key, value);
        }

        private static string ManifestComment(string insert, StreamWriter streamWriter)
        {
            // Prefix a non-empty string with a space for formatting reasons...
            if (!string.IsNullOrEmpty(insert))
            {
                insert = " " + insert;
            }

            return string.Format(CultureInfo.InvariantCulture, "#{0}{1}", insert, streamWriter.NewLine);
        }

        
        protected override void EndProcessing()
        {
            // Win8: 264471 - Error message for New-ModuleManifest -ProcessorArchitecture is obsolete.
            // If an undefined value is passed for the ProcessorArchitecture parameter, the error message from parameter binder includes all the values from the enum.
            // The value 'IA64' for ProcessorArchitecture is not supported. But since we do not own the enum System.Reflection.ProcessorArchitecture, we cannot control the values in it.
            // So, we add a separate check in our code to give an error if user specifies IA64
            if (ProcessorArchitecture == ProcessorArchitecture.IA64)
            {
                string message = StringUtil.Format(Modules.InvalidProcessorArchitectureInManifest, ProcessorArchitecture);
                InvalidOperationException ioe = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(ioe, "Modules_InvalidProcessorArchitectureInManifest",
                    ErrorCategory.InvalidArgument, ProcessorArchitecture);
                ThrowTerminatingError(er);
            }

            ProviderInfo provider = null;
            PSDriveInfo drive;
            string filePath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(_path, out provider, out drive);

            if (!provider.NameEquals(Context.ProviderNames.FileSystem) || !filePath.EndsWith(StringLiterals.PowerShellDataFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                string message = StringUtil.Format(Modules.InvalidModuleManifestPath, _path);
                InvalidOperationException ioe = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(ioe, "Modules_InvalidModuleManifestPath",
                    ErrorCategory.InvalidArgument, _path);
                ThrowTerminatingError(er);
            }

            // By default, we want to generate a module manifest the encourages the best practice of explicitly specifying
            // the commands exported (even if it's an empty array.) Unfortunately, changing the default breaks automation
            // (however unlikely, this cmdlet isn't really meant for automation). Instead of trying to detect interactive
            // use (which is quite hard), we infer interactive use if none of RootModule/NestedModules/RequiredModules is
            // specified - because the manifest needs to be edited to actually be of use in those cases.
            //
            // If one of these parameters has been specified, default back to the old behavior by specifying
            // wildcards for exported commands that weren't specified on the command line.
            if (_rootModule != null || _nestedModules != null || _requiredModules != null)
            {
                _exportedAliases ??= new string[] { "*" };
                _exportedCmdlets ??= new string[] { "*" };
                _exportedFunctions ??= new string[] { "*" };
            }

            ValidateUriParameterValue(ProjectUri, "ProjectUri");
            ValidateUriParameterValue(LicenseUri, "LicenseUri");
            ValidateUriParameterValue(IconUri, "IconUri");
            if (_helpInfoUri != null)
            {
                ValidateUriParameterValue(new Uri(_helpInfoUri), "HelpInfoUri");
            }

            if (CompatiblePSEditions != null && (CompatiblePSEditions.Distinct(StringComparer.OrdinalIgnoreCase).Count() != CompatiblePSEditions.Length))
            {
                string message = StringUtil.Format(Modules.DuplicateEntriesInCompatiblePSEditions, string.Join(',', CompatiblePSEditions));
                var ioe = new InvalidOperationException(message);
                var er = new ErrorRecord(ioe, "Modules_DuplicateEntriesInCompatiblePSEditions", ErrorCategory.InvalidArgument, CompatiblePSEditions);
                ThrowTerminatingError(er);
            }

            string action = StringUtil.Format(Modules.CreatingModuleManifestFile, filePath);

            if (ShouldProcess(filePath, action))
            {
                if (string.IsNullOrEmpty(_author))
                {
                    _author = Environment.UserName;
                }

                if (string.IsNullOrEmpty(_companyName))
                {
                    _companyName = Modules.DefaultCompanyName;
                }

                if (string.IsNullOrEmpty(_copyright))
                {
                    _copyright = StringUtil.Format(Modules.DefaultCopyrightMessage, _author);
                }

                FileStream fileStream;
                StreamWriter streamWriter;
                FileInfo readOnlyFileInfo;

                // Now open the output file...
                PathUtils.MasterStreamOpen(
                    cmdlet: this,
                    filePath: filePath,
                    resolvedEncoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    defaultEncoding: false,
                    Append: false,
                    Force: false,
                    NoClobber: false,
                    fileStream: out fileStream,
                    streamWriter: out streamWriter,
                    readOnlyFileInfo: out readOnlyFileInfo,
                    isLiteralPath: false
                );

                try
                {
                    StringBuilder result = new StringBuilder();

                    // Insert the formatted manifest header...
                    result.Append(ManifestComment(string.Empty, streamWriter));
                    result.Append(ManifestComment(StringUtil.Format(Modules.ManifestHeaderLine1, System.IO.Path.GetFileNameWithoutExtension(filePath)),
                            streamWriter));
                    result.Append(ManifestComment(string.Empty, streamWriter));
                    result.Append(ManifestComment(StringUtil.Format(Modules.ManifestHeaderLine2, _author),
                            streamWriter));
                    result.Append(ManifestComment(string.Empty, streamWriter));
                    result.Append(ManifestComment(StringUtil.Format(Modules.ManifestHeaderLine3, DateTime.Now.ToString("d", CultureInfo.CurrentCulture)),
                            streamWriter));
                    result.Append(ManifestComment(string.Empty, streamWriter));
                    result.Append(streamWriter.NewLine);
                    result.Append("@{");
                    result.Append(streamWriter.NewLine);
                    result.Append(streamWriter.NewLine);

                    _rootModule ??= string.Empty;

                    BuildModuleManifest(result, nameof(RootModule), Modules.RootModule, !string.IsNullOrEmpty(_rootModule), () => QuoteName(_rootModule), streamWriter);

                    BuildModuleManifest(result, nameof(ModuleVersion), Modules.ModuleVersion, _moduleVersion != null && !string.IsNullOrEmpty(_moduleVersion.ToString()), () => QuoteName(_moduleVersion), streamWriter);

                    BuildModuleManifest(result, nameof(CompatiblePSEditions), Modules.CompatiblePSEditions, _compatiblePSEditions != null && _compatiblePSEditions.Length > 0, () => QuoteNames(_compatiblePSEditions, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(Modules.GUID), Modules.GUID, !string.IsNullOrEmpty(_guid.ToString()), () => QuoteName(_guid.ToString()), streamWriter);

                    BuildModuleManifest(result, nameof(Author), Modules.Author, !string.IsNullOrEmpty(_author), () => QuoteName(Author), streamWriter);

                    BuildModuleManifest(result, nameof(CompanyName), Modules.CompanyName, !string.IsNullOrEmpty(_companyName), () => QuoteName(_companyName), streamWriter);

                    BuildModuleManifest(result, nameof(Copyright), Modules.Copyright, !string.IsNullOrEmpty(_copyright), () => QuoteName(_copyright), streamWriter);

                    BuildModuleManifest(result, nameof(Description), Modules.Description, !string.IsNullOrEmpty(_description), () => QuoteName(_description), streamWriter);

                    BuildModuleManifest(result, nameof(PowerShellVersion), Modules.PowerShellVersion, _powerShellVersion != null && !string.IsNullOrEmpty(_powerShellVersion.ToString()), () => QuoteName(_powerShellVersion), streamWriter);

                    BuildModuleManifest(result, nameof(PowerShellHostName), Modules.PowerShellHostName, !string.IsNullOrEmpty(_PowerShellHostName), () => QuoteName(_PowerShellHostName), streamWriter);

                    BuildModuleManifest(result, nameof(PowerShellHostVersion), Modules.PowerShellHostVersion, _PowerShellHostVersion != null && !string.IsNullOrEmpty(_PowerShellHostVersion.ToString()), () => QuoteName(_PowerShellHostVersion), streamWriter);

                    BuildModuleManifest(result, nameof(DotNetFrameworkVersion), StringUtil.Format(Modules.DotNetFrameworkVersion, Modules.PrerequisiteForDesktopEditionOnly), _DotNetFrameworkVersion != null && !string.IsNullOrEmpty(_DotNetFrameworkVersion.ToString()), () => QuoteName(_DotNetFrameworkVersion), streamWriter);

                    BuildModuleManifest(result, nameof(ClrVersion), StringUtil.Format(Modules.CLRVersion, Modules.PrerequisiteForDesktopEditionOnly), _ClrVersion != null && !string.IsNullOrEmpty(_ClrVersion.ToString()), () => QuoteName(_ClrVersion), streamWriter);

                    BuildModuleManifest(result, nameof(ProcessorArchitecture), Modules.ProcessorArchitecture, _processorArchitecture.HasValue, () => QuoteName(_processorArchitecture.ToString()), streamWriter);

                    BuildModuleManifest(result, nameof(RequiredModules), Modules.RequiredModules, _requiredModules != null && _requiredModules.Length > 0, () => QuoteModules(_requiredModules, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(RequiredAssemblies), Modules.RequiredAssemblies, _requiredAssemblies != null, () => QuoteFiles(_requiredAssemblies, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(ScriptsToProcess), Modules.ScriptsToProcess, _scripts != null, () => QuoteFiles(_scripts, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(TypesToProcess), Modules.TypesToProcess, _types != null, () => QuoteFiles(_types, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(FormatsToProcess), Modules.FormatsToProcess, _formats != null, () => QuoteFiles(_formats, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(NestedModules), Modules.NestedModules, _nestedModules != null, () => QuoteModules(PreProcessModuleSpec(_nestedModules), streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(FunctionsToExport), Modules.FunctionsToExport, true, () => QuoteNames(_exportedFunctions, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(CmdletsToExport), Modules.CmdletsToExport, true, () => QuoteNames(_exportedCmdlets, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(VariablesToExport), Modules.VariablesToExport, _exportedVariables != null && _exportedVariables.Length > 0, () => QuoteNames(_exportedVariables, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(AliasesToExport), Modules.AliasesToExport, true, () => QuoteNames(_exportedAliases, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(DscResourcesToExport), Modules.DscResourcesToExport, _dscResourcesToExport != null && _dscResourcesToExport.Length > 0, () => QuoteNames(_dscResourcesToExport, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(ModuleList), Modules.ModuleList, _moduleList != null, () => QuoteModules(_moduleList, streamWriter), streamWriter);

                    BuildModuleManifest(result, nameof(FileList), Modules.FileList, _miscFiles != null, () => QuoteFiles(_miscFiles, streamWriter), streamWriter);

                    BuildPrivateDataInModuleManifest(result, streamWriter);

                    BuildModuleManifest(result, nameof(Modules.HelpInfoURI), Modules.HelpInfoURI, !string.IsNullOrEmpty(_helpInfoUri), () => QuoteName((_helpInfoUri != null) ? new Uri(_helpInfoUri) : null), streamWriter);

                    BuildModuleManifest(result, nameof(DefaultCommandPrefix), Modules.DefaultCommandPrefix, !string.IsNullOrEmpty(_defaultCommandPrefix), () => QuoteName(_defaultCommandPrefix), streamWriter);

                    result.Append('}');
                    result.Append(streamWriter.NewLine);
                    result.Append(streamWriter.NewLine);
                    string strResult = result.ToString();

                    if (_passThru)
                    {
                        WriteObject(strResult);
                    }

                    streamWriter.Write(strResult);
                }
                finally
                {
                    streamWriter.Dispose();
                }
            }
        }

        private void BuildModuleManifest(StringBuilder result, string key, string keyDescription, bool hasValue, Func<string> action, StreamWriter streamWriter)
        {
            if (hasValue)
            {
                result.Append(ManifestFragment(key, keyDescription, action(), streamWriter));
            }
            else
            {
                result.Append(ManifestFragmentForNonSpecifiedManifestMember(key, keyDescription, action(), streamWriter));
            }
        }

        // PrivateData format in manifest file when PrivateData value is a HashTable or not specified.
        // <#
        // # Private data to pass to the module specified in RootModule/ModuleToProcess
        // PrivateData = @{
        //
        // PSData = @{
        // # Tags of this module
        // Tags = @()
        // # LicenseUri of this module
        // LicenseUri = ''
        // # ProjectUri of this module
        // ProjectUri = ''
        // # IconUri of this module
        // IconUri = ''
        // # ReleaseNotes of this module
        // ReleaseNotes = ''
        // }# end of PSData hashtable
        //
        // # User's private data keys
        //
        // }# end of PrivateData hashtable
        // #>
        private void BuildPrivateDataInModuleManifest(StringBuilder result, StreamWriter streamWriter)
        {
            var privateDataHashTable = PrivateData as Hashtable;
            bool specifiedPSDataProperties = !(Tags == null && ReleaseNotes == null && ProjectUri == null && IconUri == null && LicenseUri == null);

            if (_privateData != null && privateDataHashTable == null)
            {
                if (specifiedPSDataProperties)
                {
                    var ioe = new InvalidOperationException(Modules.PrivateDataValueTypeShouldBeHashTableError);
                    var er = new ErrorRecord(ioe, "PrivateDataValueTypeShouldBeHashTable", ErrorCategory.InvalidArgument, _privateData);
                    ThrowTerminatingError(er);
                }
                else
                {
                    WriteWarning(Modules.PrivateDataValueTypeShouldBeHashTableWarning);

                    BuildModuleManifest(result, nameof(PrivateData), Modules.PrivateData, _privateData != null,
                        () => QuoteName((string)LanguagePrimitives.ConvertTo(_privateData, typeof(string), CultureInfo.InvariantCulture)),
                        streamWriter);
                }
            }
            else
            {
                result.Append(ManifestComment(Modules.PrivateData, streamWriter));
                result.Append("PrivateData = @{");
                result.Append(streamWriter.NewLine);

                result.Append(streamWriter.NewLine);
                result.Append("    PSData = @{");
                result.Append(streamWriter.NewLine);
                result.Append(streamWriter.NewLine);

                _indent = "        ";

                BuildModuleManifest(result, nameof(Tags), Modules.Tags, Tags != null && Tags.Length > 0, () => QuoteNames(Tags, streamWriter), streamWriter);
                BuildModuleManifest(result, nameof(LicenseUri), Modules.LicenseUri, LicenseUri != null, () => QuoteName(LicenseUri), streamWriter);
                BuildModuleManifest(result, nameof(ProjectUri), Modules.ProjectUri, ProjectUri != null, () => QuoteName(ProjectUri), streamWriter);
                BuildModuleManifest(result, nameof(IconUri), Modules.IconUri, IconUri != null, () => QuoteName(IconUri), streamWriter);
                BuildModuleManifest(result, nameof(ReleaseNotes), Modules.ReleaseNotes, !string.IsNullOrEmpty(ReleaseNotes), () => QuoteName(ReleaseNotes), streamWriter);
                BuildModuleManifest(result, nameof(Prerelease), Modules.Prerelease, !string.IsNullOrEmpty(Prerelease), () => QuoteName(Prerelease), streamWriter);
                BuildModuleManifest(result, nameof(RequireLicenseAcceptance), Modules.RequireLicenseAcceptance, RequireLicenseAcceptance.IsPresent, () => RequireLicenseAcceptance.IsPresent ? "$true" : "$false", streamWriter);
                BuildModuleManifest(result, nameof(ExternalModuleDependencies), Modules.ExternalModuleDependencies, ExternalModuleDependencies != null && ExternalModuleDependencies.Length > 0, () => QuoteNames(ExternalModuleDependencies, streamWriter), streamWriter);

                result.Append("    } ");
                result.Append(ManifestComment(StringUtil.Format(Modules.EndOfManifestHashTable, "PSData"), streamWriter));
                result.Append(streamWriter.NewLine);

                _indent = "    ";
                if (privateDataHashTable != null)
                {
                    result.Append(streamWriter.NewLine);

                    foreach (DictionaryEntry entry in privateDataHashTable)
                    {
                        result.Append(ManifestFragment(entry.Key.ToString(), entry.Key.ToString(), QuoteName((string)LanguagePrimitives.ConvertTo(entry.Value, typeof(string), CultureInfo.InvariantCulture)), streamWriter));
                    }
                }

                result.Append("} ");
                result.Append(ManifestComment(StringUtil.Format(Modules.EndOfManifestHashTable, "PrivateData"), streamWriter));

                _indent = string.Empty;

                result.Append(streamWriter.NewLine);
            }
        }

        private void ValidateUriParameterValue(Uri uri, string parameterName)
        {
            Dbg.Assert(!string.IsNullOrWhiteSpace(parameterName), "parameterName should not be null or whitespace");

            if (uri != null && !Uri.IsWellFormedUriString(uri.AbsoluteUri, UriKind.Absolute))
            {
                var message = StringUtil.Format(Modules.InvalidParameterValue, uri);
                var ioe = new InvalidOperationException(message);
                var er = new ErrorRecord(ioe, "Modules_InvalidUri",
                    ErrorCategory.InvalidArgument, parameterName);
                ThrowTerminatingError(er);
            }
        }
    }

    #endregion
}
