// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    public class PassThroughContentCommandBase : ContentCommandBase
    {
        #region Parameters

        
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThrough;
            }

            set
            {
                _passThrough = value;
            }
        }

        
        protected override bool ProviderSupportsShouldProcess
        {
            get
            {
                return base.DoesProviderSupportShouldProcess(base.Path);
            }
        }

        #endregion Parameters

        #region parameter data

        
        private bool _passThrough;

        #endregion parameter data

        #region protected members

        
        internal CmdletProviderContext GetCurrentContext()
        {
            CmdletProviderContext currentCommandContext = CmdletProviderContext;
            currentCommandContext.PassThru = PassThru;
            return currentCommandContext;
        }

        #endregion protected members
    }
}
