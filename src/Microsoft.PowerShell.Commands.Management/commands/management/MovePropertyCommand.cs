// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Move, "ItemProperty", SupportsShouldProcess = true, DefaultParameterSetName = "Path", SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096817")]
    public class MoveItemPropertyCommand : PassThroughItemPropertyCommandBase
    {
        #region Parameters

        
        [Parameter(Position = 0, ParameterSetName = "Path",
                   Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get { return paths; }

            set { paths = value; }
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

        
        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PSProperty")]
        public string[] Name
        {
            get
            {
                return _property;
            }

            set
            {
                value ??= Array.Empty<string>();

                _property = value;
            }
        }

        
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public string Destination { get; set; }

        
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            string propertyName = string.Empty;
            if (Name != null && Name.Length > 0)
            {
                propertyName = Name[0];
            }

            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.MovePropertyDynamicParameters(Path[0], propertyName, Destination, propertyName, context);
            }

            return InvokeProvider.Property.MovePropertyDynamicParameters(
                ".",
                propertyName,
                Destination,
                propertyName,
                context);
        }

        #endregion Parameters

        #region parameter data

        
        private string[] _property = Array.Empty<string>();

        #endregion parameter data

        #region Command code

        
        protected override void ProcessRecord()
        {
            foreach (string path in Path)
            {
                foreach (string propertyName in Name)
                {
                    try
                    {
                        InvokeProvider.Property.Move(path, propertyName, Destination, propertyName, GetCurrentContext());
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
        }
        #endregion Command code

    }
}
