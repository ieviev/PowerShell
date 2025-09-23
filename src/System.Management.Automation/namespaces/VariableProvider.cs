// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [CmdletProvider(VariableProvider.ProviderName, ProviderCapabilities.ShouldProcess)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.SetItem)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.RenameItem)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.CopyItem)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.GetItem)]
    [OutputType(typeof(PSVariable), ProviderCmdlet = ProviderCmdlet.NewItem)]
    public sealed class VariableProvider : SessionStateProviderBase
    {
        
        public const string ProviderName = "Variable";

        #region Constructor

        
        public VariableProvider()
        {
        }

        #endregion Constructor

        #region DriveCmdletProvider overrides

        
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            string description = SessionStateStrings.VariableDriveDescription;

            PSDriveInfo variableDrive =
                new PSDriveInfo(
                    DriveNames.VariableDrive,
                    ProviderInfo,
                    string.Empty,
                    description,
                    null);

            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();
            drives.Add(variableDrive);
            return drives;
        }

        #endregion DriveCmdletProvider overrides

        #region protected members

        
        internal override object GetSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            return (PSVariable)SessionState.Internal.GetVariable(name, Context.Origin);
        }

        
        internal override void SetSessionStateItem(string name, object value, bool writeItem)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            PSVariable variable = null;

            if (value != null)
            {
                variable = value as PSVariable;
                if (variable == null)
                {
                    variable = new PSVariable(name, value);
                }
                else
                {
                    // ensure the name matches

                    if (!string.Equals(name, variable.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        PSVariable newVar = new PSVariable(name, variable.Value, variable.Options, variable.Attributes);
                        newVar.Description = variable.Description;
                        variable = newVar;
                    }
                }
            }
            else
            {
                variable = new PSVariable(name, null);
            }

            PSVariable item = SessionState.Internal.SetVariable(variable, Force, Context.Origin) as PSVariable;

            if (writeItem && item != null)
            {
                WriteItemObject(item, item.Name, false);
            }
        }

        
        internal override void RemoveSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            SessionState.Internal.RemoveVariable(name, Force);
        }

        
        internal override IDictionary GetSessionStateTable()
        {
            return (IDictionary)SessionState.Internal.GetVariableTable();
        }

        
        internal override object GetValueOfItem(object item)
        {
            Dbg.Diagnostics.Assert(
                item != null,
                "Caller should verify the item parameter");

            // Call the base class to unwrap the DictionaryEntry
            // if necessary

            object value = base.GetValueOfItem(item);

            PSVariable var = item as PSVariable;
            if (var != null)
            {
                value = var.Value;
            }

            return value;
        }

        
        internal override bool CanRenameItem(object item)
        {
            bool result = false;

            PSVariable variable = item as PSVariable;
            if (variable != null)
            {
                if ((variable.Options & ScopedItemOptions.Constant) != 0 ||
                    ((variable.Options & ScopedItemOptions.ReadOnly) != 0 && !Force))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                            variable.Name,
                            SessionStateCategory.Variable,
                            "CannotRenameVariable",
                            SessionStateStrings.CannotRenameVariable);

                    throw e;
                }

                result = true;
            }

            return result;
        }
        #endregion protected members

    }
}
