// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsLifecycle.Enable, "PSBreakpoint", SupportsShouldProcess = true, DefaultParameterSetName = BreakpointParameterSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096700")]
    [OutputType(typeof(Breakpoint))]
    public class EnablePSBreakpointCommand : PSBreakpointUpdaterCommandBase
    {
        #region parameters

        
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        #endregion parameters

        #region overrides

        
        protected override void ProcessBreakpoint(Breakpoint breakpoint)
        {
            breakpoint = Runspace.Debugger.EnableBreakpoint(breakpoint);

            if (PassThru)
            {
                base.ProcessBreakpoint(breakpoint);
            }
        }

        #endregion overrides
    }
}
