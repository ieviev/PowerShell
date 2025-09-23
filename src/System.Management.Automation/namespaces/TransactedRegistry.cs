// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//
// NOTE: A vast majority of this code was copied from BCL in
// ndp\clr\src\BCL\Microsoft\Win32\Registry.cs.
// Namespace: Microsoft.Win32
//

using BCLDebug = System.Diagnostics.Debug;

namespace Microsoft.PowerShell.Commands.Internal
{
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.CodeAnalysis;

    
    // This class contains only static members and does not need to be serializable.
    [ComVisible(true)]
    // Suppressed because these objects need to be accessed from CmdLets.
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal static class TransactedRegistry
    {
        private const string resBaseName = "RegistryProviderStrings";
        
        
        // The TransactedRegistryKey's members cannot be changed.
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        internal static readonly TransactedRegistryKey CurrentUser = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_CURRENT_USER);

        
        
        // The TransactedRegistryKey's members cannot be changed.
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        internal static readonly TransactedRegistryKey LocalMachine = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_LOCAL_MACHINE);

        
        
        // The TransactedRegistryKey's members cannot be changed.
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        internal static readonly TransactedRegistryKey ClassesRoot = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_CLASSES_ROOT);

        
        
        // The TransactedRegistryKey's members cannot be changed.
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        internal static readonly TransactedRegistryKey Users = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_USERS);

        
        
        // The TransactedRegistryKey's members cannot be changed.
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        internal static readonly TransactedRegistryKey CurrentConfig = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_CURRENT_CONFIG);
    }
}
