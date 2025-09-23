// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using System;
using Dbg = System.Management.Automation;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Security;

namespace Microsoft.PowerShell.Commands
{
    
    public abstract class SessionStateProviderBase : ContainerCmdletProvider, IContentCmdletProvider
    {
        #region tracer

        
        [Dbg.TraceSource(
             "SessionStateProvider",
             "Providers that produce a view of session state data.")]
        private static readonly Dbg.PSTraceSource s_tracer =
            Dbg.PSTraceSource.GetTracer("SessionStateProvider",
             "Providers that produce a view of session state data.");

        #endregion tracer

        #region protected members

        
        internal abstract object GetSessionStateItem(string name);

        
        internal abstract void SetSessionStateItem(string name, object value, bool writeItem);

        
        internal abstract void RemoveSessionStateItem(string name);

        
        internal abstract IDictionary GetSessionStateTable();

        
        internal virtual object GetValueOfItem(object item)
        {
            Dbg.Diagnostics.Assert(
                item != null,
                "Caller should verify the item parameter");

            object value = item;

            if (item is DictionaryEntry)
            {
                value = ((DictionaryEntry)item).Value;
            }

            return value;
        }

        
        internal virtual bool CanRenameItem(object item)
        {
            return true;
        }

        #endregion protected members

        #region ItemCmdletProvider overrides

        
        protected override void GetItem(string name)
        {
            bool isContainer = false;
            object item = null;

            IDictionary table = GetSessionStateTable();
            if (table != null)
            {
                if (string.IsNullOrEmpty(name))
                {
                    isContainer = true;
                    item = table.Values;
                }
                else
                {
                    item = table[name];
                }
            }

            if (item != null)
            {
                if (SessionState.IsVisible(this.Context.Origin, item))
                {
                    WriteItemObject(item, name, isContainer);
                }
            }
        }

        
        protected override void SetItem(
            string name,
            object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                WriteError(new ErrorRecord(
                    PSTraceSource.NewArgumentNullException(nameof(name)),
                    "SetItemNullName",
                    ErrorCategory.InvalidArgument,
                    name));
                return;
            }

            try
            {
                // Confirm the set item with the user

                string action = SessionStateProviderBaseStrings.SetItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.SetItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        name,
                        value);

                if (ShouldProcess(resource, action))
                {
                    SetSessionStateItem(name, value, true);
                }
            }
            catch (SessionStateException e)
            {
                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
            }
            catch (PSArgumentException argException)
            {
                WriteError(
                    new ErrorRecord(
                        argException.ErrorRecord,
                        argException));
            }
        }

        
        protected override void ClearItem(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                WriteError(new ErrorRecord(
                    PSTraceSource.NewArgumentNullException(nameof(path)),
                    "ClearItemNullPath",
                    ErrorCategory.InvalidArgument,
                    path));
                return;
            }

            try
            {
                // Confirm the clear item with the user

                string action = SessionStateProviderBaseStrings.ClearItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.ClearItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path);

                if (ShouldProcess(resource, action))
                {
                    SetSessionStateItem(path, null, false);
                }
            }
            catch (SessionStateException e)
            {
                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
            }
            catch (PSArgumentException argException)
            {
                WriteError(
                    new ErrorRecord(
                        argException.ErrorRecord,
                        argException));
            }
        }

        #endregion ItemCmdletProvider overrides

        #region ContainerCmdletProvider overrides

        
        protected override void GetChildItems(string path, bool recurse)
        {
            CommandOrigin origin = this.Context.Origin;
            if (string.IsNullOrEmpty(path))
            {
                IDictionary dictionary = null;

                try
                {
                    dictionary = GetSessionStateTable();
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "GetTableSecurityException",
                            ErrorCategory.ReadError,
                            path));
                    return;
                }

                // bug Windows7 #300974 says that we should sort
                List<DictionaryEntry> sortedEntries = new List<DictionaryEntry>(dictionary.Count + 1);
                foreach (DictionaryEntry entry in dictionary)
                {
                    sortedEntries.Add(entry);
                }

                sortedEntries.Sort(
                    (DictionaryEntry left, DictionaryEntry right) =>
                    {
                        string leftKey = (string)left.Key;
                        string rightKey = (string)right.Key;
                        IComparer<string> stringComparer = StringComparer.CurrentCultureIgnoreCase;
                        return stringComparer.Compare(leftKey, rightKey);
                    });

                // Now write out each object
                foreach (DictionaryEntry entry in sortedEntries)
                {
                    try
                    {
                        if (SessionState.IsVisible(origin, entry.Value))
                        {
                            WriteItemObject(entry.Value, (string)entry.Key, false);
                        }
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));

                        return;
                    }
                    catch (SecurityException securityException)
                    {
                        WriteError(
                            new ErrorRecord(
                                securityException,
                                "GetItemSecurityException",
                                ErrorCategory.PermissionDenied,
                                (string)entry.Key));
                        return;
                    }
                }
            }
            else
            {
                object item = null;

                try
                {
                    item = GetSessionStateItem(path);
                }
                catch (PSArgumentException argException)
                {
                    WriteError(
                        new ErrorRecord(
                            argException.ErrorRecord,
                            argException));

                    return;
                }
                catch (SecurityException securityException)
                {
                    WriteError(
                        new ErrorRecord(
                            securityException,
                            "GetItemSecurityException",
                            ErrorCategory.PermissionDenied,
                            path));
                    return;
                }

                if (item != null)
                {
                    if (SessionState.IsVisible(origin, item))
                    {
                        WriteItemObject(item, path, false);
                    }
                }
            }
        }

        
        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            CommandOrigin origin = this.Context.Origin;
            if (string.IsNullOrEmpty(path))
            {
                IDictionary dictionary = null;

                try
                {
                    dictionary = GetSessionStateTable();
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "GetChildNamesSecurityException",
                            ErrorCategory.ReadError,
                            path));
                    return;
                }

                // Now write out each object's key...

                foreach (DictionaryEntry entry in dictionary)
                {
                    try
                    {
                        if (SessionState.IsVisible(origin, entry.Value))
                        {
                            WriteItemObject(entry.Key, (string)entry.Key, false);
                        }
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));

                        return;
                    }
                    catch (SecurityException securityException)
                    {
                        WriteError(
                            new ErrorRecord(
                                securityException,
                                "GetItemSecurityException",
                                ErrorCategory.PermissionDenied,
                                (string)entry.Key));
                        return;
                    }
                }
            }
            else
            {
                object item = null;

                try
                {
                    item = GetSessionStateItem(path);
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "GetChildNamesSecurityException",
                            ErrorCategory.ReadError,
                            path));
                    return;
                }

                if (item != null)
                {
                    if (SessionState.IsVisible(origin, item))
                    {
                        WriteItemObject(path, path, false);
                    }
                }
            }
        }

        
        protected override bool HasChildItems(string path)
        {
            bool result = false;

            if (string.IsNullOrEmpty(path))
            {
                try
                {
                    if (GetSessionStateTable().Count > 0)
                    {
                        result = true;
                    }
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "HasChildItemsSecurityException",
                            ErrorCategory.ReadError,
                            path));
                }
            }

            return result;
        }

        
        protected override bool ItemExists(string path)
        {
            bool result = false;

            if (string.IsNullOrEmpty(path))
            {
                result = true;
            }
            else
            {
                object item = null;

                try
                {
                    item = GetSessionStateItem(path);
                }
                catch (SecurityException e)
                {
                    WriteError(
                        new ErrorRecord(
                            e,
                            "ItemExistsSecurityException",
                            ErrorCategory.ReadError,
                            path));
                }

                if (item != null)
                {
                    result = true;
                }
            }

            return result;
        }

        
        protected override bool IsValidPath(string path)
        {
            return !string.IsNullOrEmpty(path);
        }

        
        protected override void RemoveItem(string path, bool recurse)
        {
            if (string.IsNullOrEmpty(path))
            {
                Exception e =
                    PSTraceSource.NewArgumentException(nameof(path));
                WriteError(new ErrorRecord(
                    e,
                    "RemoveItemNullPath",
                    ErrorCategory.InvalidArgument,
                    path));
            }
            else
            {
                // Confirm the remove item with the user

                string action = SessionStateProviderBaseStrings.RemoveItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.RemoveItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path);

                if (ShouldProcess(resource, action))
                {
                    try
                    {
                        RemoveSessionStateItem(path);
                    }
                    catch (SessionStateException e)
                    {
                        WriteError(
                            new ErrorRecord(
                                e.ErrorRecord,
                                e));
                        return;
                    }
                    catch (SecurityException securityException)
                    {
                        WriteError(
                            new ErrorRecord(
                                securityException,
                                "RemoveItemSecurityException",
                                ErrorCategory.PermissionDenied,
                                path));
                        return;
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));
                        return;
                    }
                }
            }
        }

        
        protected override void NewItem(string path, string type, object newItem)
        {
            if (string.IsNullOrEmpty(path))
            {
                Exception e =
                    PSTraceSource.NewArgumentException(nameof(path));
                WriteError(new ErrorRecord(
                    e,
                    "NewItemNullPath",
                    ErrorCategory.InvalidArgument,
                    path));
                return;
            }

            if (newItem == null)
            {
                ArgumentNullException argException =
                    PSTraceSource.NewArgumentNullException("value");

                WriteError(
                    new ErrorRecord(
                        argException,
                        "NewItemValueNotSpecified",
                        ErrorCategory.InvalidArgument,
                        path));
                return;
            }

            if (ItemExists(path) && !Force)
            {
                PSArgumentException e =
                    (PSArgumentException)PSTraceSource.NewArgumentException(
                        nameof(path),
                        SessionStateStrings.NewItemAlreadyExists,
                        path);

                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
                return;
            }
            else
            {
                // Confirm the new item with the user

                string action = SessionStateProviderBaseStrings.NewItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.NewItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path,
                        type,
                        newItem);

                if (ShouldProcess(resource, action))
                {
                    SetItem(path, newItem);
                }
            }
        }

        
        protected override void CopyItem(string path, string copyPath, bool recurse)
        {
            if (string.IsNullOrEmpty(path))
            {
                Exception e =
                    PSTraceSource.NewArgumentException(nameof(path));
                WriteError(new ErrorRecord(
                    e,
                    "CopyItemNullPath",
                    ErrorCategory.InvalidArgument,
                    path));
                return;
            }

            // If copyPath is null or empty, that means we are trying to copy
            // the item to itself so it should be a no-op.

            if (string.IsNullOrEmpty(copyPath))
            {
                // Just get the item for -passthru
                GetItem(path);
                return;
            }

            object item = null;

            try
            {
                item = GetSessionStateItem(path);
            }
            catch (SecurityException e)
            {
                WriteError(
                    new ErrorRecord(
                        e,
                        "CopyItemSecurityException",
                        ErrorCategory.ReadError,
                        path));
                return;
            }

            if (item != null)
            {
                // Confirm the new item with the user

                string action = SessionStateProviderBaseStrings.CopyItemAction;

                string resourceTemplate = SessionStateProviderBaseStrings.CopyItemResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path,
                        copyPath);

                if (ShouldProcess(resource, action))
                {
                    try
                    {
                        SetSessionStateItem(copyPath, GetValueOfItem(item), true);
                    }
                    catch (SessionStateException e)
                    {
                        WriteError(
                            new ErrorRecord(
                                e.ErrorRecord,
                                e));
                        return;
                    }
                    catch (PSArgumentException argException)
                    {
                        WriteError(
                            new ErrorRecord(
                                argException.ErrorRecord,
                                argException));
                        return;
                    }
                }
            }
            else
            {
                PSArgumentException e =
                    (PSArgumentException)PSTraceSource.NewArgumentException(
                        nameof(path),
                        SessionStateStrings.CopyItemDoesntExist,
                        path);

                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
                return;
            }
        }

        
        protected override void RenameItem(string name, string newName)
        {
            if (string.IsNullOrEmpty(name))
            {
                Exception e =
                    PSTraceSource.NewArgumentException(nameof(name));
                WriteError(new ErrorRecord(
                    e,
                    "RenameItemNullPath",
                    ErrorCategory.InvalidArgument,
                    name));
                return;
            }

            object item = null;

            try
            {
                item = GetSessionStateItem(name);
            }
            catch (SecurityException e)
            {
                WriteError(
                    new ErrorRecord(
                        e,
                        "RenameItemSecurityException",
                        ErrorCategory.ReadError,
                        name));
                return;
            }

            if (item != null)
            {
                if (ItemExists(newName) && !Force)
                {
                    PSArgumentException e =
                        (PSArgumentException)PSTraceSource.NewArgumentException(
                            nameof(newName),
                            SessionStateStrings.NewItemAlreadyExists,
                            newName);

                    WriteError(
                        new ErrorRecord(
                            e.ErrorRecord,
                            e));
                    return;
                }
                else
                {
                    try
                    {
                        if (CanRenameItem(item))
                        {
                            // Confirm the new item with the user

                            string action = SessionStateProviderBaseStrings.RenameItemAction;

                            string resourceTemplate = SessionStateProviderBaseStrings.RenameItemResourceTemplate;

                            string resource =
                                string.Format(
                                    Host.CurrentCulture,
                                    resourceTemplate,
                                    name,
                                    newName);

                            if (ShouldProcess(resource, action))
                            {
                                if (string.Equals(name, newName, StringComparison.OrdinalIgnoreCase))
                                {
                                    // This is a no-op. Just get the item for -passthru
                                    GetItem(newName);
                                    return;
                                }

                                try
                                {
                                    SetSessionStateItem(newName, item, true);
                                    RemoveSessionStateItem(name);
                                }
                                catch (SessionStateException e)
                                {
                                    WriteError(
                                        new ErrorRecord(
                                            e.ErrorRecord,
                                            e));
                                    return;
                                }
                                catch (PSArgumentException argException)
                                {
                                    WriteError(
                                        new ErrorRecord(
                                            argException.ErrorRecord,
                                            argException));
                                    return;
                                }
                                catch (SecurityException securityException)
                                {
                                    WriteError(
                                        new ErrorRecord(
                                            securityException,
                                            "RenameItemSecurityException",
                                            ErrorCategory.PermissionDenied,
                                            name));
                                    return;
                                }
                            }
                        }
                    }
                    catch (SessionStateException e)
                    {
                        WriteError(
                            new ErrorRecord(
                                e.ErrorRecord,
                                e));
                        return;
                    }
                }
            }
            else
            {
                PSArgumentException e =
                    (PSArgumentException)PSTraceSource.NewArgumentException(
                        nameof(name),
                        SessionStateStrings.RenameItemDoesntExist,
                        name);

                WriteError(
                    new ErrorRecord(
                        e.ErrorRecord,
                        e));
                return;
            }
        }

        #endregion ContainerCmdletProvider overrides

        #region IContentCmdletProvider methods

        
        public IContentReader GetContentReader(string path)
        {
            return new SessionStateProviderBaseContentReaderWriter(path, this);
        }

        
        public IContentWriter GetContentWriter(string path)
        {
            return new SessionStateProviderBaseContentReaderWriter(path, this);
        }

        
        public void ClearContent(string path)
        {
            throw
                PSTraceSource.NewNotSupportedException(
                    SessionStateStrings.IContent_Clear_NotSupported);
        }

        #region dynamic parameters

        // For now, none of the derived providers need dynamic parameters
        // so these methods just return null

        
        public object GetContentReaderDynamicParameters(string path) { return null; }

        
        public object GetContentWriterDynamicParameters(string path) { return null; }

        
        public object ClearContentDynamicParameters(string path) { return null; }

        #endregion
        #endregion
    }

    
    public class SessionStateProviderBaseContentReaderWriter : IContentReader, IContentWriter
    {
        
        internal SessionStateProviderBaseContentReaderWriter(string path, SessionStateProviderBase provider)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (provider == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(provider));
            }

            _path = path;
            _provider = provider;
        }

        private readonly string _path;
        private readonly SessionStateProviderBase _provider;

        
        public IList Read(long readCount)
        {
            IList result = null;

            if (!_contentRead)
            {
                object item = _provider.GetSessionStateItem(_path);

                if (item != null)
                {
                    object getItemValueResult = _provider.GetValueOfItem(item);

                    if (getItemValueResult != null)
                    {
                        result = getItemValueResult as IList ?? new object[] { getItemValueResult };
                    }

                    _contentRead = true;
                }
            }

            return result;
        }

        private bool _contentRead;

        
        public IList Write(IList content)
        {
            if (content == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(content));
            }

            // Unravel the IList if there is only one value
            object valueToSet = content;
            if (content.Count == 1)
            {
                valueToSet = content[0];
            }

            _provider.SetSessionStateItem(_path, valueToSet, false);

            return content;
        }

        
        public void Seek(long offset, SeekOrigin origin)
        {
            throw
                PSTraceSource.NewNotSupportedException(
                    SessionStateStrings.IContent_Seek_NotSupported);
        }

        
        public void Close() { }

        
        public void Dispose() { Close(); GC.SuppressFinalize(this); }
    }
}

#pragma warning restore 56506
