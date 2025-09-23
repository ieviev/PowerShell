// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//
// NOTE: A vast majority of this code was copied from BCL in
// ndp\clr\src\BCL\System\Security\AccessControl\RegistrySecurity.cs.
// Namespace: System.Security.AccessControl
//


using System;
using System.Security.Permissions;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.AccessControl;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.Commands.Internal
{
    
    // Suppressed because these are needed to manipulate TransactedRegistryKey, which is written to the pipeline.
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public sealed class TransactedRegistryAccessRule : AccessRule
    {
        // Constructor for creating access rules for registry objects

        
        internal TransactedRegistryAccessRule(IdentityReference identity, RegistryRights registryRights, AccessControlType type)
            : this(identity, (int)registryRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        
        internal TransactedRegistryAccessRule(string identity, RegistryRights registryRights, AccessControlType type)
            : this(new NTAccount(identity), (int)registryRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        
        public TransactedRegistryAccessRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : this(identity, (int)registryRights, false, inheritanceFlags, propagationFlags, type)
        {
        }

        
        internal TransactedRegistryAccessRule(string identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : this(new NTAccount(identity), (int)registryRights, false, inheritanceFlags, propagationFlags, type)
        {
        }

        //
        // Internal constructor to be called by public constructors
        // and the access rule factory methods of {File|Folder}Security
        //
        internal TransactedRegistryAccessRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
            : base(
                identity,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                type)
        {
        }

        
        public RegistryRights RegistryRights
        {
            get { return (RegistryRights)base.AccessMask; }
        }
    }

    
    // Suppressed because these are needed to manipulate TransactedRegistryKey, which is written to the pipeline.
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public sealed class TransactedRegistryAuditRule : AuditRule
    {
        
        internal TransactedRegistryAuditRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : this(identity, (int)registryRights, false, inheritanceFlags, propagationFlags, flags)
        {
        }

        
        internal TransactedRegistryAuditRule(string identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : this(new NTAccount(identity), (int)registryRights, false, inheritanceFlags, propagationFlags, flags)
        {
        }

        internal TransactedRegistryAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
        {
        }

        
        public RegistryRights RegistryRights
        {
            get { return (RegistryRights)base.AccessMask; }
        }
    }

    
    // Suppressed because these are needed to manipulate TransactedRegistryKey, which is written to the pipeline.
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public sealed class TransactedRegistrySecurity : NativeObjectSecurity
    {
        private const string resBaseName = "RegistryProviderStrings";

        
        public TransactedRegistrySecurity()
            : base(true, ResourceType.RegistryKey)
        {
        }

        

        // Suppressed because the passed name and hkey won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        internal TransactedRegistrySecurity(SafeRegistryHandle hKey, string name, AccessControlSections includeSections)
            : base(true, ResourceType.RegistryKey, hKey, includeSections, _HandleErrorCode, null)
        {
            new RegistryPermission(RegistryPermissionAccess.NoAccess, AccessControlActions.View, name).Demand();
        }

        private static Exception _HandleErrorCode(int errorCode, string name, SafeHandle handle, object context)
        {
            System.Exception exception = null;

            switch (errorCode)
            {
                case Win32Native.ERROR_FILE_NOT_FOUND:
                    exception = new IOException(RegistryProviderStrings.Arg_RegKeyNotFound);
                    break;

                case Win32Native.ERROR_INVALID_NAME:
                    exception = new ArgumentException(RegistryProviderStrings.Arg_RegInvalidKeyName);
                    break;

                case Win32Native.ERROR_INVALID_HANDLE:
                    exception = new ArgumentException(RegistryProviderStrings.AccessControl_InvalidHandle);
                    break;

                default:
                    break;
            }

            return exception;
        }

        
        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
        {
            return new TransactedRegistryAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
        }

        
        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
        {
            return new TransactedRegistryAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
        }

        internal AccessControlSections GetAccessControlSectionsFromChanges()
        {
            AccessControlSections persistRules = AccessControlSections.None;
            if (AccessRulesModified)
                persistRules = AccessControlSections.Access;
            if (AuditRulesModified)
                persistRules |= AccessControlSections.Audit;
            if (OwnerModified)
                persistRules |= AccessControlSections.Owner;
            if (GroupModified)
                persistRules |= AccessControlSections.Group;
            return persistRules;
        }

        // Suppressed because the passed keyName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        internal void Persist(SafeRegistryHandle hKey, string keyName)
        {
            new RegistryPermission(RegistryPermissionAccess.NoAccess, AccessControlActions.Change, keyName).Demand();

            WriteLock();

            try
            {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                if (persistRules == AccessControlSections.None)
                    return;  // Don't need to persist anything.

                base.Persist(hKey, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void AddAccessRule(TransactedRegistryAccessRule rule)
        {
            base.AddAccessRule(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void SetAccessRule(TransactedRegistryAccessRule rule)
        {
            base.SetAccessRule(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void ResetAccessRule(TransactedRegistryAccessRule rule)
        {
            base.ResetAccessRule(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public bool RemoveAccessRule(TransactedRegistryAccessRule rule)
        {
            return base.RemoveAccessRule(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAccessRuleAll(TransactedRegistryAccessRule rule)
        {
            base.RemoveAccessRuleAll(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAccessRuleSpecific(TransactedRegistryAccessRule rule)
        {
            base.RemoveAccessRuleSpecific(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void AddAuditRule(TransactedRegistryAuditRule rule)
        {
            base.AddAuditRule(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void SetAuditRule(TransactedRegistryAuditRule rule)
        {
            base.SetAuditRule(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public bool RemoveAuditRule(TransactedRegistryAuditRule rule)
        {
            return base.RemoveAuditRule(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAuditRuleAll(TransactedRegistryAuditRule rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAuditRuleSpecific(TransactedRegistryAuditRule rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }

        
        public override Type AccessRightType
        {
            get { return typeof(RegistryRights); }
        }

        
        public override Type AccessRuleType
        {
            get { return typeof(TransactedRegistryAccessRule); }
        }

        
        public override Type AuditRuleType
        {
            get { return typeof(TransactedRegistryAuditRule); }
        }
    }
}
