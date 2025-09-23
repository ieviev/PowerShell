// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Management.Automation;

using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;

using Microsoft.PowerShell.LocalAccounts;
#endregion

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.New, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri ="https://go.microsoft.com/fwlink/?LinkId=717990")]
    [Alias("nlg")]
    public class NewLocalGroupCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string Description
        {
            get { return this.description;}

            set { this.description = value; }
        }

        private string description;

        
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [ValidateLength(1, 256)]
        public string Name
        {
            get { return this.name;}

            set { this.name = value; }
        }

        private string name;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        
        protected override void BeginProcessing()
        {
            sam = new Sam();
        }

        
        protected override void ProcessRecord()
        {
            try
            {
                if (CheckShouldProcess(Name))
                {
                    var group = sam.CreateLocalGroup(new LocalGroup
                                                        {
                                                            Description = Description,
                                                            Name = Name
                                                        });

                    WriteObject(group);
                }
            }
            catch (Exception ex)
            {
                WriteError(ex.MakeErrorRecord());
            }
        }

        
        protected override void EndProcessing()
        {
            if (sam != null)
            {
                sam.Dispose();
                sam = null;
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionNewGroup);
        }
        #endregion Private Methods
    }

}
