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

        
        /// <param name="identity">The user or group the rule applies to. Must be of type SecurityIdentifier or a type such as
        /// NTAccount that can be converted to type SecurityIdentifier.</param>
        /// <param name="registryRights">A bitwise combination of Microsoft.Win32.RegistryRights values indicating the rights allowed or denied.</param>
        /// <param name="type">One of the AccessControlType values indicating whether the rights are allowed or denied.</param>
        internal TransactedRegistryAccessRule(IdentityReference identity, RegistryRights registryRights, AccessControlType type)
            : this(identity, (int)registryRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        
        /// <param name="identity">The name of the user or group the rule applies to.</param>
        /// <param name="registryRights">A bitwise combination of Microsoft.Win32.RegistryRights values indicating the rights allowed or denied.</param>
        /// <param name="type">One of the AccessControlType values indicating whether the rights are allowed or denied.</param>
        internal TransactedRegistryAccessRule(string identity, RegistryRights registryRights, AccessControlType type)
            : this(new NTAccount(identity), (int)registryRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        
        /// <param name="identity">The user or group the rule applies to. Must be of type SecurityIdentifier or a type such as
        /// NTAccount that can be converted to type SecurityIdentifier.</param>
        /// <param name="registryRights">A bitwise combination of Microsoft.Win32.RegistryRights values indicating the rights allowed or denied.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags flags specifying how access rights are inherited from other objects.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags flags specifying how access rights are propagated to other objects.</param>
        /// <param name="type">One of the AccessControlType values indicating whether the rights are allowed or denied.</param>
        public TransactedRegistryAccessRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : this(identity, (int)registryRights, false, inheritanceFlags, propagationFlags, type)
        {
        }

        
        /// <param name="identity">The name of the user or group the rule applies to.</param>
        /// <param name="registryRights">A bitwise combination of Microsoft.Win32.RegistryRights values indicating the rights allowed or denied.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags flags specifying how access rights are inherited from other objects.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags flags specifying how access rights are propagated to other objects.</param>
        /// <param name="type">One of the AccessControlType values indicating whether the rights are allowed or denied.</param>
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
        
        /// <param name="identity">The user or group the rule applies to. Must be of type SecurityIdentifier or a type such as
        /// NTAccount that can be converted to type SecurityIdentifier.</param>
        /// <param name="registryRights">A bitwise combination of RegistryRights values specifying the kinds of access to audit.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags values specifying whether the audit rule applies to subkeys of the current key.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags values that affect the way an inherited audit rule is propagated to subkeys of the current key.</param>
        /// <param name="flags">A bitwise combination of AuditFlags values specifying whether to audit success, failure, or both.</param>
        internal TransactedRegistryAuditRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : this(identity, (int)registryRights, false, inheritanceFlags, propagationFlags, flags)
        {
        }

        
        /// <param name="identity">The name of the user or group the rule applies to.</param>
        /// <param name="registryRights">A bitwise combination of RegistryRights values specifying the kinds of access to audit.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags values specifying whether the audit rule applies to subkeys of the current key.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags values that affect the way an inherited audit rule is propagated to subkeys of the current key.</param>
        /// <param name="flags">A bitwise combination of AuditFlags values specifying whether to audit success, failure, or both.</param>
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

        
        /// <returns>A TransactedRegistryAccessRule object representing the specified rights for the specified user.</returns>
        /// <param name="identityReference">An IdentityReference that identifies the user or group the rule applies to.</param>
        /// <param name="accessMask">A bitwise combination of RegistryRights values specifying the access rights to allow or deny, cast to an integer.</param>
        /// <param name="isInherited">A Boolean value specifying whether the rule is inherited.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags values specifying how the rule is inherited by subkeys.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags values that modify the way the rule is inherited by subkeys. Meaningless if the value of inheritanceFlags is InheritanceFlags.None.</param>
        /// <param name="type">One of the AccessControlType values specifying whether the rights are allowed or denied.</param>
        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
        {
            return new TransactedRegistryAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
        }

        
        /// <returns>A TransactedRegistryAuditRule object representing the specified audit rule for the specified user, with the specified flags.
        /// The return type of the method is the base class, AuditRule, but the return value can be cast safely to the derived class.</returns>
        /// <param name="identityReference">An IdentityReference that identifies the user or group the rule applies to.</param>
        /// <param name="accessMask">A bitwise combination of RegistryRights values specifying the access rights to audit, cast to an integer.</param>
        /// <param name="isInherited">A Boolean value specifying whether the rule is inherited.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags values specifying how the rule is inherited by subkeys.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags values that modify the way the rule is inherited by subkeys. Meaningless if the value of inheritanceFlags is InheritanceFlags.None.</param>
        /// <param name="flags">A bitwise combination of AuditFlags values specifying whether to audit successful access, failed access, or both.</param>
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

        
        /// <param name="rule">The access control rule to add.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void AddAccessRule(TransactedRegistryAccessRule rule)
        {
            base.AddAccessRule(rule);
        }

        
        /// <param name="rule">The TransactedRegistryAccessRule to add. The user and AccessControlType of this rule determine the rules to remove before this rule is added.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void SetAccessRule(TransactedRegistryAccessRule rule)
        {
            base.SetAccessRule(rule);
        }

        
        /// <param name="rule">The TransactedRegistryAccessRule to add. The user specified by this rule determines the rules to remove before this rule is added.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void ResetAccessRule(TransactedRegistryAccessRule rule)
        {
            base.ResetAccessRule(rule);
        }

        
        /// <param name="rule">A TransactedRegistryAccessRule that specifies the user and AccessControlType to search for, and a set of inheritance
        /// and propagation flags that a matching rule, if found, must be compatible with. Specifies the rights to remove from the compatible rule, if found.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public bool RemoveAccessRule(TransactedRegistryAccessRule rule)
        {
            return base.RemoveAccessRule(rule);
        }

        
        /// <param name="rule">A TransactedRegistryAccessRule that specifies the user and AccessControlType to search for. Any rights, inheritance flags, or
        /// propagation flags specified by this rule are ignored.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAccessRuleAll(TransactedRegistryAccessRule rule)
        {
            base.RemoveAccessRuleAll(rule);
        }

        
        /// <param name="rule">The TransactedRegistryAccessRule to remove.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAccessRuleSpecific(TransactedRegistryAccessRule rule)
        {
            base.RemoveAccessRuleSpecific(rule);
        }

        
        /// <param name="rule">The audit rule to add. The user specified by this rule determines the search.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void AddAuditRule(TransactedRegistryAuditRule rule)
        {
            base.AddAuditRule(rule);
        }

        
        /// <param name="rule">The TransactedRegistryAuditRule to add. The user specified by this rule determines the rules to remove before this rule is added.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void SetAuditRule(TransactedRegistryAuditRule rule)
        {
            base.SetAuditRule(rule);
        }

        
        /// <param name="rule">A TransactedRegistryAuditRule that specifies the user to search for, and a set of inheritance and propagation flags that
        /// a matching rule, if found, must be compatible with. Specifies the rights to remove from the compatible rule, if found.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public bool RemoveAuditRule(TransactedRegistryAuditRule rule)
        {
            return base.RemoveAuditRule(rule);
        }

        
        /// <param name="rule">A TransactedRegistryAuditRule that specifies the user to search for. Any rights, inheritance
        /// flags, or propagation flags specified by this rule are ignored.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAuditRuleAll(TransactedRegistryAuditRule rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        
        /// <param name="rule">The TransactedRegistryAuditRule to be removed.</param>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAuditRuleSpecific(TransactedRegistryAuditRule rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }

        
        /// <returns>A Type object representing the RegistryRights enumeration.</returns>
        public override Type AccessRightType
        {
            get { return typeof(RegistryRights); }
        }

        
        /// <returns>A Type object representing the TransactedRegistryAccessRule class.</returns>
        public override Type AccessRuleType
        {
            get { return typeof(TransactedRegistryAccessRule); }
        }

        
        /// <returns>A Type object representing the TransactedRegistryAuditRule class.</returns>
        public override Type AuditRuleType
        {
            get { return typeof(TransactedRegistryAuditRule); }
        }
    }
}
