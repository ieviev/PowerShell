// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Clear, "ItemProperty", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096903")]
    public class ClearItemPropertyCommand : PassThroughItemPropertyCommandBase
    {
        #region Parameters

        
        [Parameter(Position = 0, ParameterSetName = "Path",
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
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

        
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name
        {
            get
            {
                return _property;
            }

            set
            {
                _property = value;
            }
        }

        
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            Collection<string> propertyCollection = new();
            propertyCollection.Add(_property);

            if (Path != null && Path.Length > 0)
            {
                // Go ahead and let any exception terminate the pipeline.

                return InvokeProvider.Property.ClearPropertyDynamicParameters(
                    Path[0],
                    propertyCollection,
                    context);
            }

            return InvokeProvider.Property.ClearPropertyDynamicParameters(
                ".",
                propertyCollection,
                context);
        }

        #endregion Parameters

        #region parameter data

        
        private string _property;

        #endregion parameter data

        #region Command code

        
        protected override void ProcessRecord()
        {
            CmdletProviderContext currentContext = CmdletProviderContext;
            currentContext.PassThru = PassThru;

            Collection<string> propertyCollection = new();
            propertyCollection.Add(_property);

            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Property.Clear(
                        path,
                        propertyCollection,
                        currentContext);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                }
                catch (ItemNotFoundException pathNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                }
            }
        }
        #endregion Command code

    }
}
