// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Provider
{
    #region ContainerCmdletProvider

    
    public abstract class ContainerCmdletProvider : ItemCmdletProvider
    {
        #region Internal methods

        
        internal void GetChildItems(
            string path,
            bool recurse,
            uint depth,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            GetChildItems(path, recurse, depth);
        }

        
        internal object GetChildItemsDynamicParameters(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;
            return GetChildItemsDynamicParameters(path, recurse);
        }

        
        internal void GetChildNames(
            string path,
            ReturnContainers returnContainers,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method
            GetChildNames(path, returnContainers);
        }

        
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#")]
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#")]
        internal virtual bool ConvertPath(
            string path,
            string filter,
            ref string updatedPath,
            ref string updatedFilter,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method
            return ConvertPath(path, filter, ref updatedPath, ref updatedFilter);
        }

        
        internal object GetChildNamesDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Context = context;
            return GetChildNamesDynamicParameters(path);
        }

        
        internal void RenameItem(
            string path,
            string newName,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            RenameItem(path, newName);
        }

        
        internal object RenameItemDynamicParameters(
            string path,
            string newName,
            CmdletProviderContext context)
        {
            Context = context;
            return RenameItemDynamicParameters(path, newName);
        }

        
        internal void NewItem(
            string path,
            string type,
            object newItemValue,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            NewItem(path, type, newItemValue);
        }

        
        internal object NewItemDynamicParameters(
            string path,
            string type,
            object newItemValue,
            CmdletProviderContext context)
        {
            Context = context;
            return NewItemDynamicParameters(path, type, newItemValue);
        }

        
        internal void RemoveItem(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            RemoveItem(path, recurse);
        }

        
        internal object RemoveItemDynamicParameters(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;
            return RemoveItemDynamicParameters(path, recurse);
        }

        
        internal bool HasChildItems(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return HasChildItems(path);
        }

        
        internal void CopyItem(
            string path,
            string copyPath,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            CopyItem(path, copyPath, recurse);
        }

        
        internal object CopyItemDynamicParameters(
            string path,
            string destination,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;
            return CopyItemDynamicParameters(path, destination, recurse);
        }

        #endregion Internal members

        #region Protected methods

        
        protected virtual void GetChildItems(
            string path,
            bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual void GetChildItems(
            string path,
            bool recurse,
            uint depth)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (depth == uint.MaxValue)
                {
                    this.GetChildItems(path, recurse);
                }
                else
                {
                    throw
                        PSTraceSource.NewNotSupportedException(
                            SessionStateStrings.CmdletProvider_NotSupportedRecursionDepth);
                }
            }
        }

        
        protected virtual object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual void GetChildNames(
            string path,
            ReturnContainers returnContainers)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#")]
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#")]
        protected virtual bool ConvertPath(
            string path,
            string filter,
            ref string updatedPath,
            ref string updatedFilter)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return false;
            }
        }

        
        protected virtual object GetChildNamesDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual void RenameItem(
            string path,
            string newName)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object RenameItemDynamicParameters(string path, string newName)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual void NewItem(
            string path,
            string itemTypeName,
            object newItemValue)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object NewItemDynamicParameters(
            string path,
            string itemTypeName,
            object newItemValue)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual void RemoveItem(
            string path,
            bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object RemoveItemDynamicParameters(
            string path,
            bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual bool HasChildItems(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual void CopyItem(
            string path,
            string copyPath,
            bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object CopyItemDynamicParameters(
            string path,
            string destination,
            bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        #endregion Protected members
    }

    #endregion ContainerCmdletProvider
}
