// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "Transaction", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135220")]
    [OutputType(typeof(PSTransaction))]
    public class GetTransactionCommand : PSCmdlet
    {
        
        protected override void EndProcessing()
        {
            WriteObject(this.Context.TransactionManager.GetCurrent());
        }
    }
}
