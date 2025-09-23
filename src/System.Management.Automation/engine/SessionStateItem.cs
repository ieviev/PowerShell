// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Provider;

using Dbg = System.Management.Automation;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings
#pragma warning disable 56500

namespace System.Management.Automation
{
    
    internal sealed partial class SessionStateInternal
    {
        #region ItemCmdletProvider accessors

        #region GetItem

        
        internal Collection<PSObject> GetItem(string[] paths, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            GetItem(paths, context);

            context.ThrowFirstErrorOrDoNothing();

            // Since there was not errors return the accumulated objects

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        
        internal void GetItem(
            string[] paths,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        false,
                        context,
                        out provider,
                        out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    GetItemPrivate(providerInstance, providerPath, context);
                }
            }
        }

        
        private void GetItemPrivate(
            CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            try
            {
                itemCmdletProvider.GetItem(path, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "GetItemProviderException",
                    SessionStateStrings.GetItemProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object GetItemDynamicParameters(string path, CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            CmdletProviderContext newContext =
                new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            if (providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path

                return GetItemDynamicParameters(providerInstance, providerPaths[0], newContext);
            }

            return null;
        }

        
        private object GetItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = itemCmdletProvider.GetItemDynamicParameters(path, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "GetItemDynamicParametersProviderException",
                    SessionStateStrings.GetItemDynamicParametersProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetItem

        #region SetItem

        
        internal Collection<PSObject> SetItem(string[] paths, object value, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            SetItem(paths, value, context);

            context.ThrowFirstErrorOrDoNothing();

            // Since there was no errors return the accumulated objects

            return context.GetAccumulatedObjects();
        }

        
        internal void SetItem(
            string[] paths,
            object value,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                ProviderInfo provider = null;
                CmdletProvider providerInstance = null;

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        true,
                        context,
                        out provider,
                        out providerInstance);

                if (providerPaths != null)
                {
                    foreach (string providerPath in providerPaths)
                    {
                        SetItem(providerInstance, providerPath, value, context);
                    }
                }
            }
        }

        
        private void SetItem(
            CmdletProvider providerInstance,
            string path,
            object value,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            try
            {
                itemCmdletProvider.SetItem(path, value, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "SetItemProviderException",
                    SessionStateStrings.SetItemProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object SetItemDynamicParameters(string path, object value, CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            CmdletProviderContext newContext =
               new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths =
                 Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            if (providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path

                return SetItemDynamicParameters(providerInstance, providerPaths[0], value, newContext);
            }

            return null;
        }

        
        private object SetItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            object value,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = itemCmdletProvider.SetItemDynamicParameters(path, value, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "SetItemDynamicParametersProviderException",
                    SessionStateStrings.SetItemDynamicParametersProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion SetItem

        #region ClearItem

        
        internal Collection<PSObject> ClearItem(string[] paths, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            ClearItem(paths, context);

            context.ThrowFirstErrorOrDoNothing();

            return context.GetAccumulatedObjects();
        }

        
        internal void ClearItem(
            string[] paths,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        false,
                        context,
                        out provider,
                        out providerInstance);

                if (providerPaths != null)
                {
                    foreach (string providerPath in providerPaths)
                    {
                        ClearItemPrivate(providerInstance, providerPath, context);
                    }
                }
            }
        }

        
        private void ClearItemPrivate(
            CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            try
            {
                itemCmdletProvider.ClearItem(path, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "ClearItemProviderException",
                    SessionStateStrings.ClearItemProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object ClearItemDynamicParameters(string path, CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            CmdletProviderContext newContext =
               new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths =
                 Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            if (providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path

                return ClearItemDynamicParameters(providerInstance, providerPaths[0], newContext);
            }

            return null;
        }

        
        private object ClearItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = itemCmdletProvider.ClearItemDynamicParameters(path, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "ClearItemProviderException",
                    SessionStateStrings.ClearItemProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion ClearItem

        #region InvokeDefaultAction

        
        internal void InvokeDefaultAction(string[] paths, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.SuppressWildcardExpansion = literalPath;

            InvokeDefaultAction(paths, context);

            context.ThrowFirstErrorOrDoNothing();
        }

        
        internal void InvokeDefaultAction(
            string[] paths,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        false,
                        context,
                        out provider,
                        out providerInstance);

                if (providerPaths != null)
                {
                    foreach (string providerPath in providerPaths)
                    {
                        InvokeDefaultActionPrivate(providerInstance, providerPath, context);
                    }
                }
            }
        }

        
        private void InvokeDefaultActionPrivate(
            CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            try
            {
                itemCmdletProvider.InvokeDefaultAction(path, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "InvokeDefaultActionProviderException",
                    SessionStateStrings.InvokeDefaultActionProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object InvokeDefaultActionDynamicParameters(string path, CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            CmdletProviderContext newContext =
               new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths =
                 Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            if (providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path

                return InvokeDefaultActionDynamicParameters(providerInstance, providerPaths[0], newContext);
            }

            return null;
        }

        
        private object InvokeDefaultActionDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = itemCmdletProvider.InvokeDefaultActionDynamicParameters(path, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "InvokeDefaultActionDynamicParametersProviderException",
                    SessionStateStrings.InvokeDefaultActionDynamicParametersProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion InvokeDefaultAction

        #endregion ItemCmdletProvider accessors
    }
}

#pragma warning restore 56500
