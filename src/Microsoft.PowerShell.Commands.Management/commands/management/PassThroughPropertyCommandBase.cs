// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    public class PassThroughItemPropertyCommandBase : ItemPropertyCommandBase
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

        
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
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

        #endregion Parameters

        #region parameter data

        
        private bool _passThrough;

        #endregion parameter data

        #region protected members

        
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess
        {
            get
            {
                return base.DoesProviderSupportShouldProcess(base.paths);
            }
        }

        
        /// <returns>
        /// A CmdletProviderContext instance initialized to the context of the current
        /// command.
        /// </returns>
        internal CmdletProviderContext GetCurrentContext()
        {
            CmdletProviderContext currentCommandContext = CmdletProviderContext;
            currentCommandContext.PassThru = PassThru;
            return currentCommandContext;
        }

        #endregion protected members
    }
}
