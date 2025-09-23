// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [CmdletProvider(FunctionProvider.ProviderName, ProviderCapabilities.ShouldProcess)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.SetItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.RenameItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.CopyItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.GetItem)]
    [OutputType(typeof(FunctionInfo), ProviderCmdlet = ProviderCmdlet.NewItem)]
    public sealed class FunctionProvider : SessionStateProviderBase
    {
        
        public const string ProviderName = "Function";

        #region Constructor

        
        public FunctionProvider()
        {
        }

        #endregion Constructor

        #region DriveCmdletProvider overrides

        
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            string description = SessionStateStrings.FunctionDriveDescription;

            PSDriveInfo functionDrive =
                new PSDriveInfo(
                    DriveNames.FunctionDrive,
                    ProviderInfo,
                    string.Empty,
                    description,
                    null);

            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();
            drives.Add(functionDrive);
            return drives;
        }

        #endregion DriveCmdletProvider overrides

        #region Dynamic Parameters

        
        protected override object NewItemDynamicParameters(string path, string type, object newItemValue)
        {
            return new FunctionProviderDynamicParameters();
        }

        
        protected override object SetItemDynamicParameters(string path, object value)
        {
            return new FunctionProviderDynamicParameters();
        }

        #endregion Dynamic Parameters

        #region protected members

        
        internal override object GetSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            CommandInfo function = SessionState.Internal.GetFunction(name, Context.Origin);

            return function;
        }

        
#pragma warning disable 0162
        internal override void SetSessionStateItem(string name, object value, bool writeItem)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            FunctionProviderDynamicParameters dynamicParameters =
                DynamicParameters as FunctionProviderDynamicParameters;

            CommandInfo modifiedItem = null;

            bool dynamicParametersSpecified = dynamicParameters != null && dynamicParameters.OptionsSet;

            if (value == null)
            {
                // If the value wasn't specified but the options were, just set the
                // options on the existing function.
                // If the options weren't specified, then remove the function

                if (dynamicParametersSpecified)
                {
                    modifiedItem = (CommandInfo)GetSessionStateItem(name);

                    if (modifiedItem != null)
                    {
                        SetOptions(modifiedItem, dynamicParameters.Options);
                    }
                }
                else
                {
                    RemoveSessionStateItem(name);
                }
            }
            else
            {
                do // false loop
                {
                    // Unwrap the PSObject before binding it as a scriptblock...
                    PSObject pso = value as PSObject;
                    if (pso != null)
                    {
                        value = pso.BaseObject;
                    }

                    ScriptBlock scriptBlockValue = value as ScriptBlock;
                    if (scriptBlockValue != null)
                    {
                        if (dynamicParametersSpecified)
                        {
                            modifiedItem = SessionState.Internal.SetFunction(name, scriptBlockValue,
                                null, dynamicParameters.Options, Force, Context.Origin);
                        }
                        else
                        {
                            modifiedItem = SessionState.Internal.SetFunction(name, scriptBlockValue, null, Force, Context.Origin);
                        }

                        break;
                    }

                    FunctionInfo function = value as FunctionInfo;
                    if (function != null)
                    {
                        ScopedItemOptions options = function.Options;

                        if (dynamicParametersSpecified)
                        {
                            options = dynamicParameters.Options;
                        }

                        modifiedItem = SessionState.Internal.SetFunction(name, function.ScriptBlock, function, options, Force, Context.Origin);
                        break;
                    }

                    string stringValue = value as string;
                    if (stringValue != null)
                    {
                        ScriptBlock scriptBlock = ScriptBlock.Create(Context.ExecutionContext, stringValue);

                        if (dynamicParametersSpecified)
                        {
                            modifiedItem = SessionState.Internal.SetFunction(name, scriptBlock, null, dynamicParameters.Options, Force, Context.Origin);
                        }
                        else
                        {
                            modifiedItem = SessionState.Internal.SetFunction(name, scriptBlock, null, Force, Context.Origin);
                        }

                        break;
                    }

                    throw PSTraceSource.NewArgumentException(nameof(value));
                } while (false);

                if (writeItem && modifiedItem != null)
                {
                    WriteItemObject(modifiedItem, modifiedItem.Name, false);
                }
            }
        }
#pragma warning restore 0162

        private static void SetOptions(CommandInfo function, ScopedItemOptions options)
        {
            ((FunctionInfo)function).Options = options;
        }

        
        internal override void RemoveSessionStateItem(string name)
        {
            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(name),
                "The caller should verify this parameter");

            SessionState.Internal.RemoveFunction(name, Force);
        }

        
        internal override object GetValueOfItem(object item)
        {
            Dbg.Diagnostics.Assert(
                item != null,
                "Caller should verify the item parameter");

            object value = item;

            FunctionInfo function = item as FunctionInfo;
            if (function != null)
            {
                value = function.ScriptBlock;
            }

            return value;
        }

        
        internal override IDictionary GetSessionStateTable()
        {
            return (IDictionary)SessionState.Internal.GetFunctionTable();
        }

        
        internal override bool CanRenameItem(object item)
        {
            bool result = false;

            FunctionInfo functionInfo = item as FunctionInfo;
            if (functionInfo != null)
            {
                if ((functionInfo.Options & ScopedItemOptions.Constant) != 0 ||
                    ((functionInfo.Options & ScopedItemOptions.ReadOnly) != 0 && !Force))
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                            functionInfo.Name,
                            SessionStateCategory.Function,
                            "CannotRenameFunction",
                            SessionStateStrings.CannotRenameFunction);

                    throw e;
                }

                result = true;
            }

            return result;
        }

        #endregion protected members
    }

    
    public class FunctionProviderDynamicParameters
    {
        
        [Parameter]
        public ScopedItemOptions Options
        {
            get
            {
                return _options;
            }

            set
            {
                _optionsSet = true;
                _options = value;
            }
        }

        private ScopedItemOptions _options = ScopedItemOptions.None;

        
        internal bool OptionsSet
        {
            get { return _optionsSet; }
        }

        private bool _optionsSet;
    }
}
