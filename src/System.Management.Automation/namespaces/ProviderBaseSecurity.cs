// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.AccessControl;

namespace System.Management.Automation.Provider
{
    
    public abstract partial class CmdletProvider
    {
        #region ISecurityDescriptorCmdletProvider method wrappers

        
        internal void GetSecurityDescriptor(
            string path,
            AccessControlSections sections,
            CmdletProviderContext context)
        {
            Context = context;

            ISecurityDescriptorCmdletProvider permissionProvider = this as ISecurityDescriptorCmdletProvider;

            //
            // if this is not supported, the fn will throw
            //
            CheckIfSecurityDescriptorInterfaceIsSupported(permissionProvider);

            // Call interface method

            permissionProvider.GetSecurityDescriptor(path, sections);
        }

        
        internal void SetSecurityDescriptor(
            string path,
            ObjectSecurity securityDescriptor,
            CmdletProviderContext context)
        {
            Context = context;

            ISecurityDescriptorCmdletProvider permissionProvider = this as ISecurityDescriptorCmdletProvider;

            //
            // if this is not supported, the fn will throw
            //
            CheckIfSecurityDescriptorInterfaceIsSupported(permissionProvider);

            // Call interface method

            permissionProvider.SetSecurityDescriptor(path, securityDescriptor);
        }

        private static void CheckIfSecurityDescriptorInterfaceIsSupported(ISecurityDescriptorCmdletProvider permissionProvider)
        {
            if (permissionProvider == null)
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        ProviderBaseSecurity.ISecurityDescriptorCmdletProvider_NotSupported);
            }
        }
        #endregion ISecurityDescriptorCmdletProvider method wrappers
    }
}
