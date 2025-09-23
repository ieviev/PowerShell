// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsLifecycle.Disable, "PSBreakpoint", SupportsShouldProcess = true, DefaultParameterSetName = BreakpointParameterSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096498")]
    [OutputType(typeof(Breakpoint))]
    public class DisablePSBreakpointCommand : PSBreakpointUpdaterCommandBase
    {
        #region parameters

        
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        #endregion parameters

        #region overrides

        
        protected override void ProcessBreakpoint(Breakpoint breakpoint)
        {
            breakpoint = Runspace.Debugger.DisableBreakpoint(breakpoint);

            if (PassThru)
            {
                base.ProcessBreakpoint(breakpoint);
            }
        }

        #endregion overrides
    }
}
