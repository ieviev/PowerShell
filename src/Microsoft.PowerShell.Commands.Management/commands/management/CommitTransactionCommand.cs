// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsLifecycle.Complete, "Transaction", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135200")]
    public class CompleteTransactionCommand : PSCmdlet
    {
        
        protected override void EndProcessing()
        {
            // Commit the transaction
            if (ShouldProcess(
                NavigationResources.TransactionResource,
                NavigationResources.CommitAction))
            {
                this.Context.TransactionManager.Commit();
            }
        }
    }
}
