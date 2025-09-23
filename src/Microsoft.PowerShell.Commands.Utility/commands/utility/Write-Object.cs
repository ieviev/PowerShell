// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    #region WriteOutputCommand
    
    [Cmdlet(VerbsCommunications.Write, "Output", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097117", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteOutputCommand : PSCmdlet
    {
        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromRemainingArguments = true)]
        [AllowNull]
        [AllowEmptyCollection]
        public PSObject InputObject { get; set; }

        
        [Parameter]
        public SwitchParameter NoEnumerate { get; set; }

        
        protected override void ProcessRecord()
        {
            if (InputObject == null)
            {
                WriteObject(InputObject);
                return;
            }

            WriteObject(InputObject, !NoEnumerate.IsPresent);
        }
    }
    #endregion
}
