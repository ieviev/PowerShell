// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Security.AccessControl;
using Microsoft.PowerShell.Commands.Internal;

namespace Microsoft.PowerShell.Commands
{
    
    public sealed partial class RegistryProvider :
        NavigationCmdletProvider,
        IPropertyCmdletProvider,
        IDynamicPropertyCmdletProvider,
        ISecurityDescriptorCmdletProvider
    {
        #region ISecurityDescriptorCmdletProvider members

        
        public void GetSecurityDescriptor(string path,
                                          AccessControlSections sections)
        {
            ObjectSecurity sd = null;
            IRegistryWrapper key = null;

            // Validate input first.
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if ((sections & ~AccessControlSections.All) != 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(sections));
            }

            path = NormalizePath(path);

            key = GetRegkeyForPathWriteIfError(path, false);

            if (key != null)
            {
                try
                {
                    sd = key.GetAccessControl(sections);
                }
                catch (System.Security.SecurityException e)
                {
                    WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }

                WriteSecurityDescriptorObject(sd, path);
            }
        }

        
        public void SetSecurityDescriptor(
            string path,
            ObjectSecurity securityDescriptor)
        {
            IRegistryWrapper key = null;

            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (securityDescriptor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(securityDescriptor));
            }

            path = NormalizePath(path);

            ObjectSecurity sd;
            if (TransactionAvailable())
            {
                sd = securityDescriptor as TransactedRegistrySecurity;

                if (sd == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(securityDescriptor));
                }
            }
            else
            {
                sd = securityDescriptor as RegistrySecurity;

                if (sd == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(securityDescriptor));
                }
            }

            key = GetRegkeyForPathWriteIfError(path, true);

            if (key != null)
            {
                //
                // the caller already checks for the following exceptions:
                // -- UnauthorizedAccessException
                // -- PrivilegeNotHeldException
                // -- NotSupportedException
                // -- SystemException
                //
                try
                {
                    key.SetAccessControl(sd);
                }
                catch (System.Security.SecurityException e)
                {
                    WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }
                catch (System.UnauthorizedAccessException e)
                {
                    WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }

                WriteSecurityDescriptorObject(sd, path);
            }
        }

        
        public ObjectSecurity NewSecurityDescriptorFromPath(
            string path,
            AccessControlSections sections)
        {
            if (TransactionAvailable())
            {
                return new TransactedRegistrySecurity();
            }
            else
            {
                return new RegistrySecurity(); // sections);
            }
        }

        
        public ObjectSecurity NewSecurityDescriptorOfType(
            string type,
            AccessControlSections sections)
        {
            if (TransactionAvailable())
            {
                return new TransactedRegistrySecurity();
            }
            else
            {
                return new RegistrySecurity(); // sections);
            }
        }

        #endregion ISecurityDescriptorCmdletProvider members
    }
}
#endif // !UNIX
