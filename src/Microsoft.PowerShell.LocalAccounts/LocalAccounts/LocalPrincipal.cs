// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Principal;

namespace Microsoft.PowerShell.Commands
{
    
    public enum PrincipalSource
    {
        
        Unknown = 0,

        
        Local,

        
        ActiveDirectory,

        
        AzureAD,

        
        MicrosoftAccount
    }

    
    public class LocalPrincipal
    {
        #region Public Properties
        
        public string Name { get; set; }

        
        public SecurityIdentifier SID { get; set; }

        
        public PrincipalSource? PrincipalSource { get; set; }

        
        public string ObjectClass { get; set; }
        #endregion Public Properties

        #region Construction
        
        public LocalPrincipal()
        {
        }

        
        public LocalPrincipal(string name)
        {
            Name = name;
        }
        #endregion Construction

        #region Public Methods
        
        public override string ToString()
        {
            return Name ?? SID.ToString();
        }
        #endregion Public Methods
    }
}
