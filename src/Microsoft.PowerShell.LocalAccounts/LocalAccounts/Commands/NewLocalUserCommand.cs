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
    
    [Cmdlet(VerbsCommon.New, "LocalUser",
            DefaultParameterSetName = "Password",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717981")]
    [Alias("nlu")]
    public class NewLocalUserCommand : PSCmdlet
    {
        #region Static Data
        // Names of object- and boolean-type parameters.
        // Switch parameters don't need to be included.
        private static string[] parameterNames = new string[]
            {
                "AccountExpires",
                "Description",
                "Disabled",
                "FullName",
                "Password",
                "UserMayNotChangePassword"
            };
        #endregion Static Data

        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.DateTime AccountExpires
        {
            get { return this.accountexpires;}

            set { this.accountexpires = value; }
        }

        private System.DateTime accountexpires;

        // This parameter added by hand (copied from SetLocalUserCommand), not by Cmdlet Designer
        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter AccountNeverExpires
        {
            get { return this.accountneverexpires;}

            set { this.accountneverexpires = value; }
        }

        private System.Management.Automation.SwitchParameter accountneverexpires;

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string Description
        {
            get { return this.description;}

            set { this.description = value; }
        }

        private string description;

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter Disabled
        {
            get { return this.disabled;}

            set { this.disabled = value; }
        }

        private System.Management.Automation.SwitchParameter disabled;

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string FullName
        {
            get { return this.fullname;}

            set { this.fullname = value; }
        }

        private string fullname;

        
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [ValidateLength(1, 20)]
        public string Name
        {
            get { return this.name;}

            set { this.name = value; }
        }

        private string name;

        
        [Parameter(Mandatory = true,
                   ParameterSetName = "Password",
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public System.Security.SecureString Password
        {
            get { return this.password;}

            set { this.password = value; }
        }

        private System.Security.SecureString password;

        
        [Parameter(Mandatory = true,
                   ParameterSetName = "NoPassword",
                   ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter NoPassword
        {
            get { return this.nopassword; }

            set { this.nopassword = value; }
        }

        private System.Management.Automation.SwitchParameter nopassword;

        
        [Parameter(ParameterSetName = "Password",
                   ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter PasswordNeverExpires
        {
            get { return this.passwordneverexpires; }

            set { this.passwordneverexpires = value; }
        }

        private System.Management.Automation.SwitchParameter passwordneverexpires;

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.SwitchParameter UserMayNotChangePassword
        {
            get { return this.usermaynotchangepassword;}

            set { this.usermaynotchangepassword = value; }
        }

        private System.Management.Automation.SwitchParameter usermaynotchangepassword;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        
        protected override void BeginProcessing()
        {
            if (this.HasParameter("AccountExpires") && AccountNeverExpires.IsPresent)
            {
                InvalidParametersException ex = new InvalidParametersException("AccountExpires", "AccountNeverExpires");
                ThrowTerminatingError(ex.MakeErrorRecord());
            }

            sam = new Sam();
        }

        
        protected override void ProcessRecord()
        {
            try
            {
                if (CheckShouldProcess(Name))
                {
                    var user = new LocalUser
                                {
                                    Name = Name,
                                    Description = Description,
                                    Enabled = true,
                                    FullName = FullName,
                                    UserMayChangePassword = true
                                };

                    foreach (var paramName in parameterNames)
                    {
                        if (this.HasParameter(paramName))
                        {
                            switch (paramName)
                            {
                                case "AccountExpires":
                                    user.AccountExpires = AccountExpires;
                                    break;

                                case "Disabled":
                                    user.Enabled = !Disabled;
                                    break;

                                case "UserMayNotChangePassword":
                                    user.UserMayChangePassword = !UserMayNotChangePassword;
                                    break;
                            }
                        }
                    }

                    if (AccountNeverExpires.IsPresent)
                        user.AccountExpires = null;

                    // Password will be null if NoPassword was given
                    user = sam.CreateLocalUser(user, Password, PasswordNeverExpires.IsPresent);

                    WriteObject(user);
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
            return ShouldProcess(target, Strings.ActionNewUser);
        }
        #endregion Private Methods
    }

}
