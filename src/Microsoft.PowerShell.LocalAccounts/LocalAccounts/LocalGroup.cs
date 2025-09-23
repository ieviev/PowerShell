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

        
        /// <param name="name">Name of the new LocalGroup.</param>
        public LocalGroup(string name)
          : base(name)
        {
            ObjectClass = Strings.ObjectClassGroup;
        }

        
        /// <param name="other"></param>
        private LocalGroup(LocalGroup other)
          : this(other.Name)
        {
            Description = other.Description;
        }
        #endregion Construction

        #region Public Methods
        
        /// <returns>
        /// A string containing the Group Name.
        /// </returns>
        public override string ToString()
        {
            return Name ?? SID.ToString();
        }

        
        /// <returns>
        /// A new LocalGroup object with the same property values as this one.
        /// </returns>
        public LocalGroup Clone()
        {
            return new LocalGroup(this);
        }
        #endregion Public Methods
    }
}
