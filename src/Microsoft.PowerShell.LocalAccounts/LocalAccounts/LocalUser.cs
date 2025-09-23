// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

using Microsoft.PowerShell.LocalAccounts;

namespace Microsoft.PowerShell.Commands
{
    
    public class LocalUser : LocalPrincipal
    {
        #region Public Properties
        
        public DateTime? AccountExpires { get; set; }

        
        public string Description { get; set; }

        
        public bool Enabled { get; set; }

        
        public string FullName { get; set; }

        
        public DateTime? PasswordChangeableDate { get; set; }

        
        public DateTime? PasswordExpires { get; set; }

        
        public bool UserMayChangePassword { get; set; }

        
        public bool PasswordRequired { get; set; }

        
        public DateTime? PasswordLastSet { get; set; }

        
        public DateTime? LastLogon { get; set; }
        #endregion Public Properties

        #region Construction
        
        public LocalUser()
        {
            ObjectClass = Strings.ObjectClassUser;
        }

        
        /// <param name="name">Name of the new LocalUser.</param>
        public LocalUser(string name)
          : base(name)
        {
            ObjectClass = Strings.ObjectClassUser;
        }

        
        /// <param name="other">The LocalUser object to copy.</param>
        private LocalUser(LocalUser other)
          : this(other.Name)
        {
            SID = other.SID;
            PrincipalSource = other.PrincipalSource;
            ObjectClass = other.ObjectClass;

            AccountExpires = other.AccountExpires;
            Description = other.Description;
            Enabled = other.Enabled;
            FullName = other.FullName;
            PasswordChangeableDate = other.PasswordChangeableDate;
            PasswordExpires = other.PasswordExpires;
            UserMayChangePassword = other.UserMayChangePassword;

            PasswordRequired = other.PasswordRequired;
            PasswordLastSet = other.PasswordLastSet;
            LastLogon = other.LastLogon;
        }
        #endregion Construction

        #region Public Methods
        
        /// <returns>
        /// A string containing the User Name.
        /// </returns>
        public override string ToString()
        {
            return Name ?? SID.ToString();
        }

        
        /// <returns>
        /// A new LocalUser object with the same property values as this one.
        /// </returns>
        public LocalUser Clone()
        {
            return new LocalUser(this);
        }
        #endregion Public Methods
    }
}
