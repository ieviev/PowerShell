// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
using System.Security.AccessControl;

namespace System.Management.Automation.Provider
{
    #region ISecurityDescriptorCmdletProvider

    
    public interface ISecurityDescriptorCmdletProvider
    {
        
        void GetSecurityDescriptor(
            string path,
            AccessControlSections includeSections);

        
        void SetSecurityDescriptor(
            string path,
            ObjectSecurity securityDescriptor);

        
        ObjectSecurity NewSecurityDescriptorFromPath(
            string path,
            AccessControlSections includeSections);

        
        ObjectSecurity NewSecurityDescriptorOfType(
            string type,
            AccessControlSections includeSections);
    }

    #endregion ISecurityDescriptorCmdletProvider
}
