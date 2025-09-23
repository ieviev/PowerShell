// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsData.ConvertFrom, "SddlString", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=623636", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(SecurityDescriptorInfo))]
    public sealed class ConvertFromSddlStringCommand : PSCmdlet
    {
        
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public string Sddl { get; set; }

        
        [Parameter]
        public AccessRightTypeNames Type
        {
            get
            {
                return _type;
            }

            set
            {
                _isTypeSet = true;
                _type = value;
            }
        }

        private AccessRightTypeNames _type;
        private bool _isTypeSet = false;

        private static string ConvertToNTAccount(SecurityIdentifier securityIdentifier)
        {
            try
            {
                return securityIdentifier?.Translate(typeof(NTAccount)).Value;
            }
            catch
            {
                return null;
            }
        }

        private static List<string> GetApplicableAccessRights(int accessMask, AccessRightTypeNames? typeName)
        {
            List<Type> typesToExamine = new();
            List<string> foundAccessRightNames = new();
            HashSet<int> foundAccessRightValues = new();

            if (typeName != null)
            {
                typesToExamine.Add(GetRealAccessRightType(typeName.Value));
            }
            else
            {
                foreach (AccessRightTypeNames member in Enum.GetValues<AccessRightTypeNames>())
                {
                    typesToExamine.Add(GetRealAccessRightType(member));
                }
            }

            foreach (Type accessRightType in typesToExamine)
            {
                foreach (string memberName in Enum.GetNames(accessRightType))
                {
                    int memberValue = (int)Enum.Parse(accessRightType, memberName);
                    if (foundAccessRightValues.Add(memberValue))
                    {
                        if ((accessMask & memberValue) == memberValue)
                        {
                            foundAccessRightNames.Add(memberName);
                        }
                    }
                }
            }

            foundAccessRightNames.Sort(StringComparer.OrdinalIgnoreCase);
            return foundAccessRightNames;
        }

        private static Type GetRealAccessRightType(AccessRightTypeNames typeName)
        {
            switch (typeName)
            {
                case AccessRightTypeNames.FileSystemRights:
                    return typeof(FileSystemRights);
                case AccessRightTypeNames.RegistryRights:
                    return typeof(RegistryRights);
                case AccessRightTypeNames.ActiveDirectoryRights:
                    return typeof(System.DirectoryServices.ActiveDirectoryRights);
                case AccessRightTypeNames.MutexRights:
                    return typeof(MutexRights);
                case AccessRightTypeNames.SemaphoreRights:
                    return typeof(SemaphoreRights);
                case AccessRightTypeNames.EventWaitHandleRights:
                    return typeof(EventWaitHandleRights);
                default:
                    throw new InvalidOperationException();
            }
        }

        private static string[] ConvertAccessControlListToStrings(CommonAcl acl, AccessRightTypeNames? typeName)
        {
            if (acl == null || acl.Count == 0)
            {
                return Array.Empty<string>();
            }

            List<string> aceStringList = new(acl.Count);
            foreach (CommonAce ace in acl)
            {
                StringBuilder aceString = new();
                string ntAccount = ConvertToNTAccount(ace.SecurityIdentifier);
                aceString.Append($"{ntAccount}: {ace.AceQualifier}");

                if (ace.AceFlags != AceFlags.None)
                {
                    aceString.Append($" {ace.AceFlags}");
                }

                List<string> accessRightList = GetApplicableAccessRights(ace.AccessMask, typeName);
                if (accessRightList.Count > 0)
                {
                    string accessRights = string.Join(", ", accessRightList);
                    aceString.Append($" ({accessRights})");
                }

                aceStringList.Add(aceString.ToString());
            }

            return aceStringList.ToArray();
        }

        
        protected override void ProcessRecord()
        {
            CommonSecurityDescriptor rawSecurityDescriptor = null;
            try
            {
                rawSecurityDescriptor = new CommonSecurityDescriptor(isContainer: false, isDS: false, Sddl);
            }
            catch (Exception e)
            {
                var ioe = PSTraceSource.NewInvalidOperationException(e, UtilityCommonStrings.InvalidSDDL, e.Message);
                ThrowTerminatingError(new ErrorRecord(ioe, "InvalidSDDL", ErrorCategory.InvalidArgument, Sddl));
            }

            string owner = ConvertToNTAccount(rawSecurityDescriptor.Owner);
            string group = ConvertToNTAccount(rawSecurityDescriptor.Group);

            AccessRightTypeNames? typeToUse = _isTypeSet ? _type : (AccessRightTypeNames?)null;
            string[] discretionaryAcl = ConvertAccessControlListToStrings(rawSecurityDescriptor.DiscretionaryAcl, typeToUse);
            string[] systemAcl = ConvertAccessControlListToStrings(rawSecurityDescriptor.SystemAcl, typeToUse);

            var outObj = new SecurityDescriptorInfo(owner, group, discretionaryAcl, systemAcl, rawSecurityDescriptor);
            WriteObject(outObj);
        }

        
        public enum AccessRightTypeNames
        {
            
            FileSystemRights,

            
            RegistryRights,

            
            ActiveDirectoryRights,

            
            MutexRights,

            
            SemaphoreRights,

            // We have 'CryptoKeyRights' in the list for Windows PowerShell, but that type is not available in .NET Core.
            // CryptoKeyRights,

            
            EventWaitHandleRights
        }
    }

    
    public sealed class SecurityDescriptorInfo
    {
        internal SecurityDescriptorInfo(
            string owner,
            string group,
            string[] discretionaryAcl,
            string[] systemAcl,
            CommonSecurityDescriptor rawDescriptor)
        {
            Owner = owner;
            Group = group;
            DiscretionaryAcl = discretionaryAcl;
            SystemAcl = systemAcl;
            RawDescriptor = rawDescriptor;
        }

        
        public readonly string Owner;

        
        public readonly string Group;

        
        public readonly string[] DiscretionaryAcl;

        
        public readonly string[] SystemAcl;

        
        public readonly CommonSecurityDescriptor RawDescriptor;
    }
}

#endif
