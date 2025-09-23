// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "Random", DefaultParameterSetName = GetRandomCommandBase.RandomNumberParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097016", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(int), typeof(long), typeof(double))]
    public sealed class GetRandomCommand : GetRandomCommandBase
    {
        
        [Parameter]
        [ValidateNotNull]
        public int? SetSeed { get; set; }

        
        protected override void BeginProcessing()
        {
            if (SetSeed.HasValue)
            {
                Generator = new PolymorphicRandomNumberGenerator(SetSeed.Value);
            }

            base.BeginProcessing();
        }
    }
}
