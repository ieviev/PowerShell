// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "PSCallStack", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096705")]
    [OutputType(typeof(CallStackFrame))]
    public class GetPSCallStackCommand : PSCmdlet
    {
        
        protected override void ProcessRecord()
        {
            foreach (CallStackFrame frame in Context.Debugger.GetCallStack())
            {
                WriteObject(frame);
            }
        }
    }
}
