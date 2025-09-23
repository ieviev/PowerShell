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
        #region IDynamicPropertyCmdletProvider accessors

        #region NewProperty

        
        internal Collection<PSObject> NewProperty(
            string[] paths,
            string property,
            string type,
            object value,
            bool force,
            bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (property == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(property));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            NewProperty(paths, property, type, value, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        
        internal void NewProperty(
            string[] paths,
            string property,
            string type,
            object value,
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
                    NewProperty(providerInstance, providerPath, property, type, value, context);
                }
            }
        }

        
        private void NewProperty(
            CmdletProvider providerInstance,
            string path,
            string property,
            string type,
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
                property != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.NewProperty(path, property, type, value, context);
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
                    "NewPropertyProviderException",
                    SessionStateStrings.NewPropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object NewPropertyDynamicParameters(
             string path,
            string propertyName,
            string type,
            object value,
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

                return NewPropertyDynamicParameters(providerInstance, providerPaths[0], propertyName, type, value, newContext);
            }

            return null;
        }

        
        private object NewPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string propertyName,
            string type,
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

            object result = null;
            try
            {
                result = providerInstance.NewPropertyDynamicParameters(path, propertyName, type, value, context);
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
                    "NewPropertyDynamicParametersProviderException",
                    SessionStateStrings.NewPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion NewProperty

        #region RemoveProperty

        
        internal void RemoveProperty(string[] paths, string property, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (property == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(property));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            RemoveProperty(paths, property, context);

            context.ThrowFirstErrorOrDoNothing();
        }

        
        internal void RemoveProperty(
            string[] paths,
            string property,
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

                foreach (string providerPath in providerPaths)
                {
                    RemoveProperty(providerInstance, providerPath, property, context);
                }
            }
        }

        
        private void RemoveProperty(
            CmdletProvider providerInstance,
            string path,
            string property,
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
                "Caller should validate property before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.RemoveProperty(path, property, context);
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
                    "RemovePropertyProviderException",
                    SessionStateStrings.RemovePropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object RemovePropertyDynamicParameters(
             string path,
            string propertyName,
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

                return RemovePropertyDynamicParameters(providerInstance, providerPaths[0], propertyName, newContext);
            }

            return null;
        }

        
        private object RemovePropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string propertyName,
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
                result = providerInstance.RemovePropertyDynamicParameters(path, propertyName, context);
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
                    "RemovePropertyDynamicParametersProviderException",
                    SessionStateStrings.RemovePropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion RemoveProperty

        #region CopyProperty

        
        internal Collection<PSObject> CopyProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            bool force,
            bool literalPath)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            CopyProperty(sourcePaths, sourceProperty, destinationPath, destinationProperty, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        
        internal void CopyProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            foreach (string sourcePath in sourcePaths)
            {
                if (sourcePath == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
                }

                ProviderInfo provider = null;
                CmdletProvider providerInstance = null;

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        sourcePath,
                        false,
                        context,
                        out provider,
                        out providerInstance);

                if (providerPaths.Count > 0)
                {
                    // Save off the original filters
                    Collection<string> includeFilters = context.Include;
                    Collection<string> excludeFilters = context.Exclude;
                    string filterString = context.Filter;

                    // now modify the filters so that the destination isn't filtered

                    context.SetFilters(
                        new Collection<string>(),
                        new Collection<string>(),
                        null);

                    Collection<string> providerDestinationPaths =
                        Globber.GetGlobbedProviderPathsFromMonadPath(
                            destinationPath,
                            false,
                            context,
                            out provider,
                            out providerInstance);

                    // Now reapply the filters

                    context.SetFilters(
                        includeFilters,
                        excludeFilters,
                        filterString);

                    foreach (string providerPath in providerPaths)
                    {
                        foreach (string providerDestinationPath in providerDestinationPaths)
                        {
                            CopyProperty(providerInstance, providerPath, sourceProperty, providerDestinationPath, destinationProperty, context);
                        }
                    }
                }
            }
        }

        
        private void CopyProperty(
            CmdletProvider providerInstance,
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                sourcePath != null,
                "Caller should validate sourcePath before calling this method");

            Dbg.Diagnostics.Assert(
                sourceProperty != null,
                "Caller should validate sourceProperty before calling this method");

            Dbg.Diagnostics.Assert(
                destinationPath != null,
                "Caller should validate destinationPath before calling this method");

            Dbg.Diagnostics.Assert(
                destinationProperty != null,
                "Caller should validate destinationProperty before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.CopyProperty(sourcePath, sourceProperty, destinationPath, destinationProperty, context);
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
                    "CopyPropertyProviderException",
                    SessionStateStrings.CopyPropertyProviderException,
                    providerInstance.ProviderInfo,
                    sourcePath,
                    e);
            }
        }

        
        internal object CopyPropertyDynamicParameters(
             string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
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

                return CopyPropertyDynamicParameters(
                    providerInstance,
                    providerPaths[0],
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    newContext);
            }

            return null;
        }

        
        private object CopyPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
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
                result = providerInstance.CopyPropertyDynamicParameters(
                    path,
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    context);
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
                    "CopyPropertyDynamicParametersProviderException",
                    SessionStateStrings.CopyPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion CopyProperty

        #region MoveProperty

        
        internal Collection<PSObject> MoveProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            bool force,
            bool literalPath)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            MoveProperty(sourcePaths, sourceProperty, destinationPath, destinationProperty, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        
        internal void MoveProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            // We shouldn't be filtering the destination path

            CmdletProviderContext destinationContext = new CmdletProviderContext(context);

            destinationContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> destinationProviderPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    destinationPath,
                    false,
                    destinationContext,
                    out provider,
                    out providerInstance);

            if (destinationProviderPaths.Count > 1)
            {
                ArgumentException argException =
                    PSTraceSource.NewArgumentException(
                        nameof(destinationPath),
                        SessionStateStrings.MovePropertyDestinationResolveToSingle);

                context.WriteError(new ErrorRecord(argException, argException.GetType().FullName, ErrorCategory.InvalidArgument, destinationProviderPaths));
            }
            else
            {
                foreach (string sourcePath in sourcePaths)
                {
                    if (sourcePath == null)
                    {
                        throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
                    }

                    Collection<string> providerPaths =
                        Globber.GetGlobbedProviderPathsFromMonadPath(
                            sourcePath,
                            false,
                            context,
                            out provider,
                            out providerInstance);

                    foreach (string providerPath in providerPaths)
                    {
                        MoveProperty(providerInstance, providerPath, sourceProperty, destinationProviderPaths[0], destinationProperty, context);
                    }
                }
            }
        }

        
        private void MoveProperty(
            CmdletProvider providerInstance,
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                sourcePath != null,
                "Caller should validate sourcePath before calling this method");

            Dbg.Diagnostics.Assert(
                sourceProperty != null,
                "Caller should validate sourceProperty before calling this method");

            Dbg.Diagnostics.Assert(
                destinationPath != null,
                "Caller should validate destinationPath before calling this method");

            Dbg.Diagnostics.Assert(
                destinationProperty != null,
                "Caller should validate destinationProperty before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.MoveProperty(sourcePath, sourceProperty, destinationPath, destinationProperty, context);
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
                    "MovePropertyProviderException",
                    SessionStateStrings.MovePropertyProviderException,
                    providerInstance.ProviderInfo,
                    sourcePath,
                    e);
            }
        }

        
        internal object MovePropertyDynamicParameters(
             string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
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

                return MovePropertyDynamicParameters(
                    providerInstance,
                    providerPaths[0],
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    newContext);
            }

            return null;
        }

        
        private object MovePropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
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
                result = providerInstance.MovePropertyDynamicParameters(
                    path,
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    context);
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
                    "MovePropertyDynamicParametersProviderException",
                    SessionStateStrings.MovePropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion MoveProperty

        #region RenameProperty

        
        internal Collection<PSObject> RenameProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationProperty,
            bool force,
            bool literalPath)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            RenameProperty(sourcePaths, sourceProperty, destinationProperty, context);

            context.ThrowFirstErrorOrDoNothing();
            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        
        internal void RenameProperty(
            string[] paths,
            string sourceProperty,
            string destinationProperty,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
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
                    RenameProperty(providerInstance, providerPath, sourceProperty, destinationProperty, context);
                }
            }
        }

        
        private void RenameProperty(
            CmdletProvider providerInstance,
            string sourcePath,
            string sourceProperty,
            string destinationProperty,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                sourcePath != null,
                "Caller should validate sourcePath before calling this method");

            Dbg.Diagnostics.Assert(
                sourceProperty != null,
                "Caller should validate sourceProperty before calling this method");

            Dbg.Diagnostics.Assert(
                destinationProperty != null,
                "Caller should validate destinationProperty before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.RenameProperty(sourcePath, sourceProperty, destinationProperty, context);
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
                    "RenamePropertyProviderException",
                    SessionStateStrings.RenamePropertyProviderException,
                    providerInstance.ProviderInfo,
                    sourcePath,
                    e);
            }
        }

        
        internal object RenamePropertyDynamicParameters(
             string path,
            string sourceProperty,
            string destinationProperty,
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

                return RenamePropertyDynamicParameters(
                    providerInstance,
                    providerPaths[0],
                    sourceProperty,
                    destinationProperty,
                    newContext);
            }

            return null;
        }

        
        private object RenamePropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string sourceProperty,
            string destinationProperty,
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
                result = providerInstance.RenamePropertyDynamicParameters(
                    path,
                    sourceProperty,
                    destinationProperty,
                    context);
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
                    "RenamePropertyDynamicParametersProviderException",
                    SessionStateStrings.RenamePropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion RenameProperty

        #endregion IDynamicPropertyCmdletProvider accessors
    }
}

#pragma warning restore 56500
