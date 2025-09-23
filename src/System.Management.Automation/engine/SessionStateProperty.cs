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
        #region IPropertyCmdletProvider accessors

        #region GetProperty

        
        internal Collection<PSObject> GetProperty(
            string[] paths,
            Collection<string> providerSpecificPickList,
            bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException("path");
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.SuppressWildcardExpansion = literalPath;

            GetProperty(paths, providerSpecificPickList, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        
        internal void GetProperty(
            string[] paths,
            Collection<string> providerSpecificPickList,
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
                        false,
                        context,
                        out provider,
                        out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    GetPropertyPrivate(
                        providerInstance,
                        providerPath,
                        providerSpecificPickList,
                        context);
                }
            }
        }

        
        private void GetPropertyPrivate(
            CmdletProvider providerInstance,
            string path,
            Collection<string> providerSpecificPickList,
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

            try
            {
                providerInstance.GetProperty(path, providerSpecificPickList, context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "GetPropertyProviderException",
                    SessionStateStrings.GetPropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object GetPropertyDynamicParameters(
            string path,
            Collection<string> providerSpecificPickList,
            CmdletProviderContext context)
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

                return GetPropertyDynamicParameters(providerInstance, providerPaths[0], providerSpecificPickList, newContext);
            }

            return null;
        }

        
        private object GetPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            Collection<string> providerSpecificPickList,
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

            object result = null;
            try
            {
                result = providerInstance.GetPropertyDynamicParameters(path, providerSpecificPickList, context);
            }
            catch (NotSupportedException)
            {
                throw;
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
                    "GetPropertyDynamicParametersProviderException",
                    SessionStateStrings.GetPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetProperty

        #region SetProperty

        
        internal Collection<PSObject> SetProperty(string[] paths, PSObject property, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (property == null)
            {
                throw PSTraceSource.NewArgumentNullException("properties");
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            SetProperty(paths, property, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        
        internal void SetProperty(
            string[] paths,
            PSObject property,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (property == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(property));
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
                        false,
                        context,
                        out provider,
                        out providerInstance);

                if (providerPaths != null)
                {
                    foreach (string providerPath in providerPaths)
                    {
                        SetPropertyPrivate(providerInstance, providerPath, property, context);
                    }
                }
            }
        }

        
        private void SetPropertyPrivate(
            CmdletProvider providerInstance,
            string path,
            PSObject property,
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
                property != null,
                "Caller should validate properties before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.SetProperty(path, property, context);
            }
            catch (NotSupportedException)
            {
                throw;
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
                    "SetPropertyProviderException",
                    SessionStateStrings.SetPropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object SetPropertyDynamicParameters(
            string path,
            PSObject propertyValue,
            CmdletProviderContext context)
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

                return SetPropertyDynamicParameters(providerInstance, providerPaths[0], propertyValue, newContext);
            }

            return null;
        }

        
        private object SetPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            PSObject propertyValue,
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

            object result = null;
            try
            {
                result = providerInstance.SetPropertyDynamicParameters(path, propertyValue, context);
            }
            catch (NotSupportedException)
            {
                throw;
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
                    "SetPropertyDynamicParametersProviderException",
                    SessionStateStrings.SetPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion SetProperty

        #region ClearProperty

        
        internal void ClearProperty(
            string[] paths,
            Collection<string> propertyToClear,
            bool force,
            bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (propertyToClear == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyToClear));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            ClearProperty(paths, propertyToClear, context);

            context.ThrowFirstErrorOrDoNothing();
        }

        
        internal void ClearProperty(
            string[] paths,
            Collection<string> propertyToClear,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (propertyToClear == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyToClear));
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
                        false,
                        context,
                        out provider,
                        out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    ClearPropertyPrivate(providerInstance, providerPath, propertyToClear, context);
                }
            }
        }

        
        private void ClearPropertyPrivate(
            CmdletProvider providerInstance,
            string path,
            Collection<string> propertyToClear,
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
                propertyToClear != null,
                "Caller should validate propertyToClear before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.ClearProperty(path, propertyToClear, context);
            }
            catch (NotSupportedException)
            {
                throw;
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
                    "ClearPropertyProviderException",
                    SessionStateStrings.ClearPropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object ClearPropertyDynamicParameters(
            string path,
            Collection<string> propertyToClear,
            CmdletProviderContext context)
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

                return ClearPropertyDynamicParameters(providerInstance, providerPaths[0], propertyToClear, newContext);
            }

            return null;
        }

        
        private object ClearPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            Collection<string> propertyToClear,
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

            object result = null;

            try
            {
                result = providerInstance.ClearPropertyDynamicParameters(path, propertyToClear, context);
            }
            catch (NotSupportedException)
            {
                throw;
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
                    "ClearPropertyDynamicParametersProviderException",
                    SessionStateStrings.ClearPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion ClearProperty

        #endregion IPropertyCmdletProvider accessors
    }
}

#pragma warning restore 56500
