// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using static System.Management.Automation.Verbs;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "Verb", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097026")]
    [OutputType(typeof(VerbInfo))]
    public class GetVerbCommand : Cmdlet
    {
        
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        [ArgumentCompleter(typeof(VerbArgumentCompleter))]
        public string[] Verb
        {
            get; set;
        }

        
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 1)]
        [ValidateSet("Common", "Communications", "Data", "Diagnostic", "Lifecycle", "Other", "Security")]
        public string[] Group
        {
            get; set;
        }

        
        protected override void ProcessRecord()
        {
            foreach (VerbInfo verb in FilterByVerbsAndGroups(Verb, Group))
            {
                WriteObject(verb);
            }
        }
    }
}
