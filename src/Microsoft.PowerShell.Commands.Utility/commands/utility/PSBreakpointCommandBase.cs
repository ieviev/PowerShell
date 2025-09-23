// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    
    public abstract class PSBreakpointCommandBase : PSCmdlet
    {
        #region parameters

        
        [Parameter]
        [ValidateNotNull]
        [Runspace]
        public virtual Runspace Runspace { get; set; }

        #endregion parameters

        #region overrides

        
        protected override void BeginProcessing()
        {
            Runspace ??= Context.CurrentRunspace;
        }

        #endregion overrides

        #region protected methods

        
        protected virtual void ProcessBreakpoint(Breakpoint breakpoint)
        {
            if (Runspace != Context.CurrentRunspace)
            {
                var pso = new PSObject(breakpoint);
                pso.Properties.Add(new PSNoteProperty(RemotingConstants.RunspaceIdNoteProperty, Runspace.InstanceId));
                WriteObject(pso);
            }
            else
            {
                WriteObject(breakpoint);
            }
        }

        #endregion protected methods
    }
}
