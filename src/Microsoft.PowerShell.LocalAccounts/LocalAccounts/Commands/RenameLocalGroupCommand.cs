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
    
    [Cmdlet(VerbsCommon.Rename, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717978")]
    [Alias("rnlg")]
    public class RenameLocalGroupCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNullOrEmpty]
        public Microsoft.PowerShell.Commands.LocalGroup InputObject
        {
            get { return this.inputobject;}

            set { this.inputobject = value; }
        }

        private Microsoft.PowerShell.Commands.LocalGroup inputobject;

        
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get { return this.name;}

            set { this.name = value; }
        }

        private string name;

        
        [Parameter(Mandatory = true,
                   Position = 1)]
        [ValidateNotNullOrEmpty]
        public string NewName
        {
            get { return this.newname;}

            set { this.newname = value; }
        }

        private string newname;

        
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNullOrEmpty]
        public System.Security.Principal.SecurityIdentifier SID
        {
            get { return this.sid;}

            set { this.sid = value; }
        }

        private System.Security.Principal.SecurityIdentifier sid;
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
                ProcessGroup();
                ProcessName();
                ProcessSid();
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
        
        private void ProcessName()
        {
            if (Name != null)
            {
                try
                {
                    if (CheckShouldProcess(Name, NewName))
                        sam.RenameLocalGroup(sam.GetLocalGroup(Name), NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        
        private void ProcessSid()
        {
            if (SID != null)
            {
                try
                {
                    if (CheckShouldProcess(SID.ToString(), NewName))
                        sam.RenameLocalGroup(SID, NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        
        private void ProcessGroup()
        {
            if (InputObject != null)
            {
                try
                {
                    if (CheckShouldProcess(InputObject.Name, NewName))
                        sam.RenameLocalGroup(InputObject, NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        
        private bool CheckShouldProcess(string groupName, string newName)
        {
            string msg = StringUtil.Format(Strings.ActionRenameGroup, newName);

            return ShouldProcess(groupName, msg);
        }
        #endregion Private Methods
    }

}
