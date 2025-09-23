// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "ItemProperty", DefaultParameterSetName = "Path", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096493")]
    public class GetItemPropertyCommand : ItemPropertyCommandBase
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

        
        [Parameter(Position = 1)]
        [Alias("PSProperty")]
        public string[] Name
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

        
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.GetPropertyDynamicParameters(
                    Path[0],
                    SessionStateUtilities.ConvertArrayToCollection<string>(_property), context);
            }

            return InvokeProvider.Property.GetPropertyDynamicParameters(
                ".",
                SessionStateUtilities.ConvertArrayToCollection<string>(_property), context);
        }

        #endregion Parameters

        #region parameter data

        
        private string[] _property;

        #endregion parameter data

        #region Command code

        
        protected override void ProcessRecord()
        {
            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Property.Get(
                        path,
                        SessionStateUtilities.ConvertArrayToCollection<string>(_property),
                        CmdletProviderContext);
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

    
    [Cmdlet(VerbsCommon.Get, "ItemPropertyValue", DefaultParameterSetName = "Path", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096906")]
    public sealed class GetItemPropertyValueCommand : ItemPropertyCommandBase
    {
        #region Parameters

        
        [Parameter(Position = 0, ParameterSetName = "Path", Mandatory = false, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
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

        
        [Parameter(ParameterSetName = "LiteralPath", Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
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

        
        [Parameter(Position = 1, Mandatory = true)]
        [Alias("PSProperty")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
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

        
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.GetPropertyDynamicParameters(
                    Path[0],
                    SessionStateUtilities.ConvertArrayToCollection<string>(_property), context);
            }

            return InvokeProvider.Property.GetPropertyDynamicParameters(
                ".",
                SessionStateUtilities.ConvertArrayToCollection<string>(_property), context);
        }

        #endregion Parameters

        #region parameter data

        
        private string[] _property;

        #endregion parameter data

        #region Command code

        
        protected override void ProcessRecord()
        {
            if (Path == null || Path.Length == 0)
            {
                paths = new string[] { "." };
            }

            foreach (string path in Path)
            {
                try
                {
                    Collection<PSObject> itemProperties = InvokeProvider.Property.Get(
                        new string[] { path },
                        SessionStateUtilities.ConvertArrayToCollection<string>(_property),
                        base.SuppressWildcardExpansion);

                    if (itemProperties != null)
                    {
                        foreach (PSObject currentItem in itemProperties)
                        {
                            if (this.Name != null)
                            {
                                foreach (string currentPropertyName in this.Name)
                                {
                                    if (currentItem.Properties != null &&
                                        currentItem.Properties[currentPropertyName] != null &&
                                        currentItem.Properties[currentPropertyName].Value != null)
                                    {
                                        CmdletProviderContext.WriteObject(currentItem.Properties[currentPropertyName].Value);
                                    }
                                }
                            }
                        }
                    }
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
}
