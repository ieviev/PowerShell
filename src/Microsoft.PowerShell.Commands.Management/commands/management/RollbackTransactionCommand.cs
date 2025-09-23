// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Undo, "Transaction", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135268")]
    public class UndoTransactionCommand : PSCmdlet
    {
        
        protected override void EndProcessing()
        {
            // Rollback the transaction
            if (ShouldProcess(
                NavigationResources.TransactionResource,
                NavigationResources.RollbackAction))
            {
                this.Context.TransactionManager.Rollback();
            }
        }
    }
}
