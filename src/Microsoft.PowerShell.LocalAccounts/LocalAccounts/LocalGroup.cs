// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

using Microsoft.PowerShell.LocalAccounts;

namespace Microsoft.PowerShell.Commands
{
    
    public class LocalGroup : LocalPrincipal
    {
        #region Public Properties
        
        public string Description { get; set; }
        #endregion Public Properties

        #region Construction
        
        public LocalGroup()
        {
            ObjectClass = Strings.ObjectClassGroup;
        }

        
        public LocalGroup(string name)
          : base(name)
        {
            ObjectClass = Strings.ObjectClassGroup;
        }

        
        private LocalGroup(LocalGroup other)
          : this(other.Name)
        {
            Description = other.Description;
        }
        #endregion Construction

        #region Public Methods
        
        public override string ToString()
        {
            return Name ?? SID.ToString();
        }

        
        public LocalGroup Clone()
        {
            return new LocalGroup(this);
        }
        #endregion Public Methods
    }
}
