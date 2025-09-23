// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    public class WriteAliasCommandBase : PSCmdlet
    {
        #region Parameters

        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Value { get; set; }

        
        [Parameter]
        public string Description { get; set; } = string.Empty;

        
        [Parameter]
        public ScopedItemOptions Option { get; set; } = ScopedItemOptions.None;

        
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }

            set
            {
                _passThru = value;
            }
        }

        private bool _passThru;

        
        [Parameter]
        [ArgumentCompleter(typeof(ScopeArgumentCompleter))]
        public string Scope { get; set; }

        
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        private bool _force;
        #endregion Parameters
    }
}
