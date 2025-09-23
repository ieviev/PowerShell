// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Remove, "PSBreakpoint", SupportsShouldProcess = true, DefaultParameterSetName = BreakpointParameterSetName,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097134")]
    public class RemovePSBreakpointCommand : PSBreakpointUpdaterCommandBase
    {
        #region overrides

        
        protected override void ProcessBreakpoint(Breakpoint breakpoint)
        {
            Runspace.Debugger.RemoveBreakpoint(breakpoint);
        }

        #endregion overrides
    }
}
