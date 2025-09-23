// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.New, "ItemProperty", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096813")]
    public class NewItemPropertyCommand : ItemPropertyCommandBase
    {
        #region Parameters

        
        [Parameter(Position = 0, ParameterSetName = "Path", Mandatory = true)]
        public string[] Path
        {
            get
            {
                return paths;
            }

            set
            {
                paths = value;
            }
        }

        
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get
            {
                return paths;
            }

            set
            {
                base.SuppressWildcardExpansion = true;
                paths = value;
            }
        }

        
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        [Alias("PSProperty")]
        public string Name { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Type")]
#if !UNIX
        [ArgumentCompleter(typeof(PropertyTypeArgumentCompleter))]
#endif
        public string PropertyType { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public object Value { get; set; }

        
        [Parameter]
        public override SwitchParameter Force
        {
            get
            {
                return base.Force;
            }

            set
            {
                base.Force = value;
            }
        }

        
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.NewPropertyDynamicParameters(Path[0], Name, PropertyType, Value, context);
            }

            return InvokeProvider.Property.NewPropertyDynamicParameters(".", Name, PropertyType, Value, context);
        }

        #endregion Parameters

        #region parameter data

        #endregion parameter data

        #region Command code

        
        protected override void ProcessRecord()
        {
            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Property.New(path, Name, PropertyType, Value, CmdletProviderContext);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                    continue;
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                    continue;
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                    continue;
                }
                catch (ItemNotFoundException pathNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                    continue;
                }
            }
        }
        #endregion Command code

    }

#if !UNIX
    
    public class PropertyTypeArgumentCompleter : IArgumentCompleter
    {
        private static readonly CompletionHelpers.CompletionDisplayInfoMapper RegistryPropertyTypeDisplayInfoMapper = registryPropertyType => registryPropertyType switch
        {
            "String" => (
                ToolTip: TabCompletionStrings.RegistryStringToolTip,
                ListItemText: "String"),
            "ExpandString" => (
                ToolTip: TabCompletionStrings.RegistryExpandStringToolTip,
                ListItemText: "ExpandString"),
            "Binary" => (
                ToolTip: TabCompletionStrings.RegistryBinaryToolTip,
                ListItemText: "Binary"),
            "DWord" => (
                ToolTip: TabCompletionStrings.RegistryDWordToolTip,
                ListItemText: "DWord"),
            "MultiString" => (
                ToolTip: TabCompletionStrings.RegistryMultiStringToolTip,
                ListItemText: "MultiString"),
            "QWord" => (
                ToolTip: TabCompletionStrings.RegistryQWordToolTip,
                ListItemText: "QWord"),
            _ => (
                ToolTip: TabCompletionStrings.RegistryUnknownToolTip,
                ListItemText: "Unknown"),
        };

        private static readonly IReadOnlyList<string> s_RegistryPropertyTypes = new List<string>(capacity: 7)
        {
            "String",
            "ExpandString",
            "Binary",
            "DWord",
            "MultiString",
            "QWord",
            "Unknown"
        };

        
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
                => IsRegistryProvider(fakeBoundParameters)
                    ? CompletionHelpers.GetMatchingResults(
                        wordToComplete,
                        possibleCompletionValues: s_RegistryPropertyTypes,
                        displayInfoMapper: RegistryPropertyTypeDisplayInfoMapper,
                        resultType: CompletionResultType.ParameterValue)
                    : [];

        
        private static bool IsRegistryProvider(IDictionary fakeBoundParameters)
        {
            Collection<PathInfo> paths;

            if (fakeBoundParameters.Contains("Path"))
            {
                paths = ResolvePath(fakeBoundParameters["Path"], isLiteralPath: false);
            }
            else if (fakeBoundParameters.Contains("LiteralPath"))
            {
                paths = ResolvePath(fakeBoundParameters["LiteralPath"], isLiteralPath: true);
            }
            else
            {
                paths = ResolvePath(@".\", isLiteralPath: false);
            }

            return paths.Count > 0 && paths[0].Provider.NameEquals("Registry");
        }

        
        private static Collection<PathInfo> ResolvePath(object path, bool isLiteralPath)
        {
            using var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);

            ps.AddCommand("Microsoft.PowerShell.Management\\Resolve-Path");
            ps.AddParameter(isLiteralPath ? "LiteralPath" : "Path", path);

            Collection<PathInfo> output = ps.Invoke<PathInfo>();

            return output;
        }
    }
#endif
}
