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
        #region NavigationCmdletProvider accessors

        #region GetParentPath

        
        internal string GetParentPath(string path, string root)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            string result = GetParentPath(path, root, context);

            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        
        internal string GetParentPath(
            string path,
            string root,
            CmdletProviderContext context)
        {
            return GetParentPath(path, root, context, false);
        }

        
        internal string GetParentPath(
            string path,
            string root,
            CmdletProviderContext context,
            bool useDefaultProvider)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext getProviderPathContext =
                new CmdletProviderContext(context);

            try
            {
                PSDriveInfo drive = null;
                ProviderInfo provider = null;

                try
                {
                    Globber.GetProviderPath(
                        path,
                        getProviderPathContext,
                        out provider,
                        out drive);
                }
                catch (DriveNotFoundException)
                {
                    // the path is sure to be drive_qualified and it is absolute path, otherwise the
                    // drive would be set to the current drive and the DriveNotFoundException will not happen
                    if (useDefaultProvider)
                    {
                        // the default provider is FileSystem
                        provider = PublicSessionState.Internal.GetSingleProvider(Microsoft.PowerShell.Commands.FileSystemProvider.ProviderName);
                    }
                    else
                    {
                        throw;
                    }
                }

                if (getProviderPathContext.HasErrors())
                {
                    getProviderPathContext.WriteErrorsToContext(context);
                    return null;
                }

                if (drive != null)
                {
                    context.Drive = drive;
                }

                bool isProviderQualified = false;
                bool isDriveQualified = false;
                string qualifier = null;
                string pathNoQualifier = RemoveQualifier(path, provider, out qualifier, out isProviderQualified, out isDriveQualified);

                string result = GetParentPath(provider, pathNoQualifier, root, context);

                if (!string.IsNullOrEmpty(qualifier) && !string.IsNullOrEmpty(result))
                {
                    result = AddQualifier(result, provider, qualifier, isProviderQualified, isDriveQualified);
                }

                return result;
            }
            finally
            {
                getProviderPathContext.RemoveStopReferral();
            }
        }

        private static string AddQualifier(string path, ProviderInfo provider, string qualifier, bool isProviderQualified, bool isDriveQualified)
        {
            string result = path;

            string formatString = "{1}";
            if (isProviderQualified)
            {
                formatString = "{0}::{1}";
            }
            else if (isDriveQualified)
            {
                // Porting note: on non-windows filesystem paths, there should be no colon in the path
                if (provider.VolumeSeparatedByColon)
                {
                    formatString = "{0}:{1}";
                }
                else
                {
                    formatString = "{0}{1}";
                }
            }

            result =
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    formatString,
                    qualifier,
                    path);

            return result;
        }

        
        private string RemoveQualifier(string path, ProviderInfo provider, out string qualifier, out bool isProviderQualified, out bool isDriveQualified)
        {
            Dbg.Diagnostics.Assert(
                path != null,
                "Path should be verified by the caller");

            string result = path;
            qualifier = null;
            isProviderQualified = false;
            isDriveQualified = false;

            if (LocationGlobber.IsProviderQualifiedPath(path, out qualifier))
            {
                isProviderQualified = true;

                int index = path.IndexOf("::", StringComparison.Ordinal);

                if (index != -1)
                {
                    // remove the qualifier
                    result = path.Substring(index + 2);
                }
            }
            else
            {
                if (Globber.IsAbsolutePath(path, out qualifier))
                {
                    isDriveQualified = true;

                    // Remove the drive name and colon, or just the drive name

                    // Porting note: on non-windows there is no colon for qualified paths
                    if (provider.VolumeSeparatedByColon)
                    {
                        result = path.Substring(qualifier.Length + 1);
                    }
                    else
                    {
                        result = path.Substring(qualifier.Length);
                    }
                }
            }

            return result;
        }

        
        internal string GetParentPath(
            ProviderInfo provider,
            string path,
            string root,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                provider != null,
                "Caller should validate provider before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                root != null,
                "Caller should validate root before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            CmdletProvider providerInstance = GetProviderInstance(provider);
            return GetParentPath(providerInstance, path, root, context);
        }

        
        internal string GetParentPath(
            CmdletProvider providerInstance,
            string path,
            string root,
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
                root != null,
                "Caller should validate root before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            NavigationCmdletProvider navigationCmdletProvider =
                GetNavigationProviderInstance(providerInstance, false);

            string result = null;

            try
            {
                result = navigationCmdletProvider.GetParentPath(path, root, context);
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
                    "GetParentPathProviderException",
                    SessionStateStrings.GetParentPathProviderException,
                    navigationCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetParentPath

        #region NormalizeRelativePath

        
        internal string NormalizeRelativePath(string path, string basePath)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            string result = NormalizeRelativePath(path, basePath, context);

            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        
        internal string NormalizeRelativePath(
            string path,
            string basePath,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext getProviderPathContext =
                new CmdletProviderContext(context);

            try
            {
                PSDriveInfo drive = null;
                ProviderInfo provider = null;

                string workingPath = Globber.GetProviderPath(
                     path,
                     getProviderPathContext,
                     out provider,
                     out drive);

                if (getProviderPathContext.HasErrors())
                {
                    getProviderPathContext.WriteErrorsToContext(context);
                    return null;
                }

                if (workingPath == null ||
                    provider == null)
                {
                    // Since the provider didn't write an error, and we didn't get any
                    // results ourselves, we need to write out our own error.

                    Exception e = PSTraceSource.NewArgumentException(nameof(path));
                    context.WriteError(new ErrorRecord(e, "NormalizePathNullResult", ErrorCategory.InvalidArgument, path));
                    return null;
                }

                if (basePath != null)
                {
                    PSDriveInfo baseDrive = null;
                    ProviderInfo baseProvider = null;

                    Globber.GetProviderPath(
                         basePath,
                         getProviderPathContext,
                         out baseProvider,
                         out baseDrive);

                    if (drive != null && baseDrive != null)
                    {
                        if (!drive.Name.Equals(baseDrive.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            // Make sure they are from physically different drives
                            // Doing StartsWith from both directions covers the following cases
                            // C:\ and C:\Temp
                            // C:\Temp and C:\
                            if (!(drive.Root.StartsWith(baseDrive.Root, StringComparison.OrdinalIgnoreCase) ||
                                (baseDrive.Root.StartsWith(drive.Root, StringComparison.OrdinalIgnoreCase))))
                            {
                                // In this case, no normalization is necessary
                                return path;
                            }
                        }
                    }
                }

                if (drive != null)
                {
                    context.Drive = drive;

                    // Detect if the original path was already a
                    // provider path. This happens when a drive doesn't
                    // have a rooted root -- such as HKEY_LOCAL_MACHINE instead of
                    // \\HKEY_LOCAL_MACHINE
                    if (
                        (GetProviderInstance(provider) is NavigationCmdletProvider) &&
                        (!string.IsNullOrEmpty(drive.Root)) &&
                        (path.StartsWith(drive.Root, StringComparison.OrdinalIgnoreCase)))
                    {
                        //
                        // If the drive root doesn't end with a path separator then there is a chance the
                        // path starts with the drive root name but doesn't actually refer to it.  For example,
                        // (see Win8 bug 922001) consider drive with root HKEY_LOCAL_MACHINE named
                        // HKEY_LOCAL_MACHINE_foo.  The path would start with the drive root but is not a provider
                        // path.
                        //
                        // We will remediate this by only considering this a provider path if
                        // 1.  The drive root ends with a path separator.
                        // OR
                        // 2.  The path starts with the drive root followed by a path separator
                        // OR
                        // 3.  The path exactly matches the drive root.
                        //

                        // 1. Test for the drive root ending with a path separator.
                        bool driveRootEndsWithPathSeparator = IsPathSeparator(drive.Root[drive.Root.Length - 1]);

                        // 2. Test for the path starting with the drive root followed by a path separator
                        int indexAfterDriveRoot = drive.Root.Length;
                        bool pathStartsWithDriveRootAndPathSeparator = indexAfterDriveRoot < path.Length && IsPathSeparator(path[indexAfterDriveRoot]);

                        // 3. Test for the drive root exactly matching the path.
                        //    Since we know the path starts with the drive root then they are equal if the lengths are equal.
                        bool pathEqualsDriveRoot = drive.Root.Length == path.Length;

                        if (driveRootEndsWithPathSeparator || pathStartsWithDriveRootAndPathSeparator || pathEqualsDriveRoot)
                        {
                            workingPath = path;
                        }
                    }
                }

                return NormalizeRelativePath(provider, workingPath, basePath, context);
            }
            finally
            {
                getProviderPathContext.RemoveStopReferral();
            }
        }

        
        private static bool IsPathSeparator(char c)
        {
            return c == StringLiterals.DefaultPathSeparator || c == StringLiterals.AlternatePathSeparator;
        }

        
        internal string NormalizeRelativePath(
            ProviderInfo provider,
            string path,
            string basePath,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                provider != null,
                "Caller should validate provider before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            // Get an instance of the provider

            Provider.CmdletProvider providerInstance = GetProviderInstance(provider);

            NavigationCmdletProvider navigationCmdletProvider = providerInstance as NavigationCmdletProvider;
            if (navigationCmdletProvider != null)
            {
                try
                {
                    path = navigationCmdletProvider.NormalizeRelativePath(path, basePath, context);
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
                        "NormalizeRelativePathProviderException",
                    SessionStateStrings.NormalizeRelativePathProviderException,
                        navigationCmdletProvider.ProviderInfo,
                        path,
                        e);
                }
            }
            else if (providerInstance is ContainerCmdletProvider)
            {
                // Do nothing and return the path as-is
            }
            else
            {
                throw PSTraceSource.NewNotSupportedException();
            }

            return path;
        }

        #endregion NormalizeRelativePath

        #region MakePath

        
        internal string MakePath(
            string parent,
            string child)
        {
            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            return MakePath(parent, child, context);
        }

        
        internal string MakePath(
            string parent,
            string child,
            CmdletProviderContext context)
        {
            string result = null;

            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(context));
            }

            if (parent == null &&
                child == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(parent));
            }

            // Set the drive data for the context

            ProviderInfo provider = null;
            if (CurrentDrive != null)
            {
                provider = CurrentDrive.Provider;
            }

            if (context.Drive == null)
            {
                bool isProviderQualified = LocationGlobber.IsProviderQualifiedPath(parent);
                bool isAbsolute = LocationGlobber.IsAbsolutePath(parent);
                if (isProviderQualified || isAbsolute)
                {
                    PSDriveInfo drive = null;

                    // Ignore the result. Just using this to get the providerId and drive
                    Globber.GetProviderPath(parent, context, out provider, out drive);

                    if (drive == null && isProviderQualified)
                    {
                        drive = provider.HiddenDrive;
                    }

                    context.Drive = drive;
                }
                else
                {
                    context.Drive = CurrentDrive;
                }

                result = MakePath(provider, parent, child, context);

                if (isAbsolute)
                {
                    result = LocationGlobber.GetDriveQualifiedPath(result, context.Drive);
                }
                else if (isProviderQualified)
                {
                    result = LocationGlobber.GetProviderQualifiedPath(result, provider);
                }
            }
            else
            {
                provider = context.Drive.Provider;
                result = MakePath(provider, parent, child, context);
            }

            return result;
        }

        
        internal string MakePath(
            ProviderInfo provider,
            string parent,
            string child,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                provider != null,
                "Caller should validate provider before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            // Get an instance of the provider

            Provider.CmdletProvider providerInstance = provider.CreateInstance();

            return MakePath(providerInstance, parent, child, context);
        }

        
        internal string MakePath(
            CmdletProvider providerInstance,
            string parent,
            string child,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            // Get an instance of the provider

            string result = null;

            NavigationCmdletProvider navigationCmdletProvider = providerInstance as NavigationCmdletProvider;

            if (navigationCmdletProvider != null)
            {
                try
                {
                    result = navigationCmdletProvider.MakePath(parent, child, context);
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
                        "MakePathProviderException",
                        SessionStateStrings.MakePathProviderException,
                        navigationCmdletProvider.ProviderInfo,
                        parent,
                        e);
                }
            }
            else if (providerInstance is ContainerCmdletProvider)
            {
                result = child;
            }
            else
            {
                throw PSTraceSource.NewNotSupportedException();
            }

            return result;
        }

        #endregion MakePath

        #region GetChildName

        
        internal string GetChildName(string path)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            string result = GetChildName(path, context);

            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        
        internal string GetChildName(
            string path,
            CmdletProviderContext context)
        {
            return GetChildName(path, context, false);
        }

        
        internal string GetChildName(
            string path,
            CmdletProviderContext context,
            bool useDefaultProvider)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            PSDriveInfo drive = null;
            ProviderInfo provider = null;
            string workingPath = null;

            try
            {
                workingPath = Globber.GetProviderPath(path, context, out provider, out drive);
            }
            catch (DriveNotFoundException)
            {
                // the path is sure to be drive_qualified and it is an absolute path, otherwise the
                // drive would be set to the current drive and the DriveNotFoundException will not happen.
                if (useDefaultProvider)
                {
                    // the default provider is FileSystem
                    provider = PublicSessionState.Internal.GetSingleProvider(Microsoft.PowerShell.Commands.FileSystemProvider.ProviderName);

                    workingPath = path.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);
                    workingPath = workingPath.TrimEnd(StringLiterals.DefaultPathSeparator);
                }
                else
                {
                    throw;
                }
            }

            Dbg.Diagnostics.Assert(
                workingPath != null,
                "There should always be a way to generate a UniversalResourceName for a " +
                "given path");

            Dbg.Diagnostics.Assert(
                provider != null,
            "There should always be a way to get the provider ID for a given path or else GetProviderPath should have thrown an exception");

            if (drive != null)
            {
                context.Drive = drive;
            }

            return GetChildName(provider, workingPath, context);
        }

        
        private string GetChildName(
            ProviderInfo provider,
            string path,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                provider != null,
                "Caller should validate provider before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before callin g this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            CmdletProvider providerInstance = provider.CreateInstance();

            return GetChildName(providerInstance, path, context, true);
        }

        
        private string GetChildName(
            CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context,
            bool acceptNonContainerProviders
            )
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

            string result = null;

            NavigationCmdletProvider navigationCmdletProvider =
                GetNavigationProviderInstance(providerInstance, acceptNonContainerProviders);

            if (navigationCmdletProvider == null)
                return path;

            try
            {
                result = navigationCmdletProvider.GetChildName(path, context);
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
                    "GetChildNameProviderException",
                    SessionStateStrings.GetChildNameProviderException,
                    navigationCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetChildName

        #region MoveItem

        
        internal Collection<PSObject> MoveItem(string[] paths, string destination, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            MoveItem(paths, destination, context);

            context.ThrowFirstErrorOrDoNothing();

            // Since there was no errors return the accumulated objects

            return context.GetAccumulatedObjects();
        }

        
        internal void MoveItem(
            string[] paths,
            string destination,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (destination == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destination));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            Collection<PathInfo> providerDestinationPaths =
                Globber.GetGlobbedMonadPathsFromMonadPath(
                    destination,
                    true,
                    context,
                    out providerInstance);

            if (providerDestinationPaths.Count > 1)
            {
                ArgumentException argException =
                    PSTraceSource.NewArgumentException(
                        nameof(destination),
                        SessionStateStrings.MoveItemOneDestination);

                context.WriteError(new ErrorRecord(argException, argException.GetType().FullName, ErrorCategory.InvalidArgument, destination));
            }
            else
            {
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

                    // Check to be sure we resolved at least one item to move and that the
                    // destination is a container.

                    if (providerPaths.Count > 1 &&
                        providerDestinationPaths.Count > 0 &&
                        !IsItemContainer(providerDestinationPaths[0].Path))
                    {
                        ArgumentException argException =
                            PSTraceSource.NewArgumentException(
                                "path",
                                SessionStateStrings.MoveItemPathMultipleDestinationNotContainer);

                        context.WriteError(new ErrorRecord(argException, argException.GetType().FullName, ErrorCategory.InvalidArgument, providerDestinationPaths[0]));
                    }
                    else
                    {
                        ProviderInfo destinationProvider = null;

                        CmdletProviderContext destinationContext = new CmdletProviderContext(this.ExecutionContext);

                        string destinationProviderInternalPath = null;

                        if (providerDestinationPaths.Count > 0)
                        {
                            destinationProviderInternalPath =
                                Globber.GetProviderPath(
                                    providerDestinationPaths[0].Path,
                                    destinationContext,
                                    out destinationProvider,
                                    out _);
                        }
                        else
                        {
                            // Since the path doesn't exist, just convert it to a
                            // provider path and continue.

                            destinationProviderInternalPath =
                                Globber.GetProviderPath(
                                    destination,
                                    destinationContext,
                                    out destinationProvider,
                                    out _);
                        }

                        // Now verify the providers are the same.

                        if (!string.Equals(
                                provider.FullName,
                                destinationProvider.FullName,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            ArgumentException argException =
                                PSTraceSource.NewArgumentException(
                                    nameof(destination),
                                    SessionStateStrings.MoveItemSourceAndDestinationNotSameProvider);

                            context.WriteError(new ErrorRecord(argException, argException.GetType().FullName, ErrorCategory.InvalidArgument, providerPaths));
                        }
                        else
                        {
                            foreach (string providerPath in providerPaths)
                            {
                                MoveItemPrivate(providerInstance, providerPath, destinationProviderInternalPath, context);
                            }
                        }
                    }
                }
            }
        }

        
        private void MoveItemPrivate(
            CmdletProvider providerInstance,
            string path,
            string destination,
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

            NavigationCmdletProvider navigationCmdletProvider =
                GetNavigationProviderInstance(providerInstance, false);

            try
            {
                navigationCmdletProvider.MoveItem(path, destination, context);
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
                    "MoveItemProviderException",
                    SessionStateStrings.MoveItemProviderException,
                    navigationCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        
        internal object MoveItemDynamicParameters(
            string path,
            string destination,
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

                return MoveItemDynamicParameters(providerInstance, providerPaths[0], destination, newContext);
            }

            return null;
        }

        
        private object MoveItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string destination,
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

            NavigationCmdletProvider navigationCmdletProvider =
                GetNavigationProviderInstance(providerInstance, false);

            object result = null;

            try
            {
                result = navigationCmdletProvider.MoveItemDynamicParameters(path, destination, context);
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
                    "MoveItemDynamicParametersProviderException",
                    SessionStateStrings.MoveItemDynamicParametersProviderException,
                    navigationCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion MoveItem

        #endregion NavigationCmdletProvider accessors
    }
}

#pragma warning restore 56500
