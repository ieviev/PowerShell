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
    
    [Cmdlet(VerbsCommon.Rename, "LocalUser",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkID=717983")]
    [Alias("rnlu")]
    public class RenameLocalUserCommand : Cmdlet
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
        [ValidateNotNull]
        public Microsoft.PowerShell.Commands.LocalUser InputObject
        {
            get { return this.inputobject;}

            set { this.inputobject = value; }
        }

        private Microsoft.PowerShell.Commands.LocalUser inputobject;

        
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
        [ValidateNotNull]
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
                ProcessUser();
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
        
        /// <remarks>
        /// Arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// </remarks>
        private void ProcessName()
        {
            if (Name != null)
            {
                try
                {
                    if (CheckShouldProcess(Name, NewName))
                        sam.RenameLocalUser(sam.GetLocalUser(Name), NewName);
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
                        sam.RenameLocalUser(SID, NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        
        private void ProcessUser()
        {
            if (InputObject != null)
            {
                try
                {
                    if (CheckShouldProcess(InputObject.Name, NewName))
                        sam.RenameLocalUser(InputObject, NewName);
                }
                catch (Exception ex)
                {
                    WriteError(ex.MakeErrorRecord());
                }
            }
        }

        
        /// <param name="userName">
        /// Name of the user to rename.
        /// </param>
        /// <param name="newName">
        /// New name for the user.
        /// </param>
        /// <returns>
        /// True if the user should be processed, false otherwise.
        /// </returns>
        private bool CheckShouldProcess(string userName, string newName)
        {
            string msg = StringUtil.Format(Strings.ActionRenameUser, newName);

            return ShouldProcess(userName, msg);
        }
        #endregion Private Methods
    }

}
