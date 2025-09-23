// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Rename, "ItemProperty", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097152")]
    public class RenameItemPropertyCommand : PassThroughItemPropertyCommandBase
    {
        #region Parameters

        
        [Parameter(Position = 0, ParameterSetName = "Path",
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
            }
        }

        
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get
            {
                return _path;
            }

            set
            {
                base.SuppressWildcardExpansion = true;
                _path = value;
            }
        }

        
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        [Alias("PSProperty")]
        public string Name { get; set; }

        
        [Parameter(Mandatory = true, Position = 2, ValueFromPipelineByPropertyName = true)]
        public string NewName { get; set; }

        
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null)
            {
                return InvokeProvider.Property.RenamePropertyDynamicParameters(Path, Name, NewName, context);
            }

            return InvokeProvider.Property.RenamePropertyDynamicParameters(".", Name, NewName, context);
        }

        #endregion Parameters

        #region parameter data

        
        private string _path;

        #endregion parameter data

        #region Command code

        
        protected override void ProcessRecord()
        {
            try
            {
                CmdletProviderContext currentContext = CmdletProviderContext;
                currentContext.PassThru = PassThru;

                InvokeProvider.Property.Rename(_path, Name, NewName, currentContext);
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
        #endregion Command code

    }
}
