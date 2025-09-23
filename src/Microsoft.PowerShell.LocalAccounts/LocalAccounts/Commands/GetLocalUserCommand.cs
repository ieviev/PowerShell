// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;
#endregion

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "LocalUser",
            DefaultParameterSetName = "Default",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717980")]
    [Alias("glu")]
    public class GetLocalUserCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
        #endregion Instance Data

        #region Parameter Properties
        
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get { return this.name; }

            set { this.name = value; }
        }

        private string[] name;

        
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public System.Security.Principal.SecurityIdentifier[] SID
        {
            get { return this.sid; }

            set { this.sid = value; }
        }

        private System.Security.Principal.SecurityIdentifier[] sid;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        
        protected override void BeginProcessing()
        {
            sam = new Sam();
        }

        
        protected override void ProcessRecord()
        {
            if (Name == null && SID == null)
            {
                foreach (var user in sam.GetAllLocalUsers())
                    WriteObject(user);

                return;
            }

            ProcessNames();
            ProcessSids();
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
        /// All arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// Users may be specified using wildcards.
        /// </remarks>
        private void ProcessNames()
        {
            if (Name != null)
            {
                foreach (var nm in Name)
                {
                    try
                    {
                        if (WildcardPattern.ContainsWildcardCharacters(nm))
                        {
                            var pattern = new WildcardPattern(nm, WildcardOptions.Compiled
                                                                | WildcardOptions.IgnoreCase);

                            foreach (var user in sam.GetMatchingLocalUsers(n => pattern.IsMatch(n)))
                                WriteObject(user);
                        }
                        else
                        {
                            WriteObject(sam.GetLocalUser(nm));
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }

        
        private void ProcessSids()
        {
            if (SID != null)
            {
                foreach (var s in SID)
                {
                    try
                    {
                        WriteObject(sam.GetLocalUser(s));
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }
        #endregion Private Methods
    }

}
