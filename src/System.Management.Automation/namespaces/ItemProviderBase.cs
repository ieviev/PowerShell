// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;

namespace System.Management.Automation.Provider
{
    #region ItemCmdletProvider

    
    public abstract class ItemCmdletProvider : DriveCmdletProvider
    {
        #region internal methods

        
        internal void GetItem(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            GetItem(path);
        }

        
        internal object GetItemDynamicParameters(string path, CmdletProviderContext context)
        {
            Context = context;
            return GetItemDynamicParameters(path);
        }

        
        internal void SetItem(
            string path,
            object value,
            CmdletProviderContext context)
        {
            providerBaseTracer.WriteLine("ItemCmdletProvider.SetItem");

            Context = context;

            // Call virtual method

            SetItem(path, value);
        }

        
        internal object SetItemDynamicParameters(
            string path,
            object value,
            CmdletProviderContext context)
        {
            Context = context;
            return SetItemDynamicParameters(path, value);
        }

        
        internal void ClearItem(
            string path,
            CmdletProviderContext context)
        {
            providerBaseTracer.WriteLine("ItemCmdletProvider.ClearItem");

            Context = context;

            // Call virtual method

            ClearItem(path);
        }

        
        internal object ClearItemDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Context = context;
            return ClearItemDynamicParameters(path);
        }

        
        internal void InvokeDefaultAction(
            string path,
            CmdletProviderContext context)
        {
            providerBaseTracer.WriteLine("ItemCmdletProvider.InvokeDefaultAction");

            Context = context;

            // Call virtual method

            InvokeDefaultAction(path);
        }

        
        internal object InvokeDefaultActionDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Context = context;
            return InvokeDefaultActionDynamicParameters(path);
        }

        
        internal bool ItemExists(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            bool itemExists = false;
            try
            {
                // Some providers don't expect non-valid path elements, and instead
                // throw an exception here.
                itemExists = ItemExists(path);
            }
            catch (Exception)
            {
            }

            return itemExists;
        }

        
        internal object ItemExistsDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Context = context;
            return ItemExistsDynamicParameters(path);
        }

        
        internal bool IsValidPath(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return IsValidPath(path);
        }

        
        internal string[] ExpandPath(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method
            return ExpandPath(path);
        }

        #endregion internal methods

        #region Protected methods

        
        protected virtual void GetItem(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object GetItemDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual void SetItem(
            string path,
            object value)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object SetItemDynamicParameters(string path, object value)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual void ClearItem(
            string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object ClearItemDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual void InvokeDefaultAction(
            string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object InvokeDefaultActionDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual bool ItemExists(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        
        protected virtual object ItemExistsDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected abstract bool IsValidPath(string path);

        
        protected virtual string[] ExpandPath(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return new string[] { path };
            }
        }

        #endregion Protected methods
    }

    #endregion ItemCmdletProvider
}
