// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Provider;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    internal sealed partial class SessionStateInternal
    {
        #region Current working directory/drive

        
        internal PathInfo CurrentLocation
        {
            get
            {
                if (CurrentDrive == null)
                {
                    // We need the error handling, and moving to a method would be
                    // a breaking change
#pragma warning suppress 56503
                    throw PSTraceSource.NewInvalidOperationException();
                }

                PathInfo result =
                    new PathInfo(
                        CurrentDrive,
                        CurrentDrive.Provider,
                        CurrentDrive.CurrentLocation,
                        new SessionState(this));

                return result;
            }
        }

        
        internal PathInfo GetNamespaceCurrentLocation(string namespaceID)
        {
            if (namespaceID == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(namespaceID));
            }

            // If namespace ID is empty, we will use the current working drive
            PSDriveInfo drive = null;

            if (namespaceID.Length == 0)
            {
                ProvidersCurrentWorkingDrive.TryGetValue(CurrentDrive.Provider, out drive);
            }
            else
            {
                // First check to see if the provider exists
                ProvidersCurrentWorkingDrive.TryGetValue(GetSingleProvider(namespaceID), out drive);
            }

            if (drive == null)
            {
                DriveNotFoundException e =
                    new DriveNotFoundException(
                        namespaceID,
                        "DriveNotFound",
                        SessionStateStrings.DriveNotFound);
                throw e;
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Drive = drive;

            // Now make the namespace specific path
            string path = null;

            if (drive.Hidden)
            {
                if (LocationGlobber.IsProviderDirectPath(drive.CurrentLocation))
                {
                    path = drive.CurrentLocation;
                }
                else
                {
                    path = LocationGlobber.GetProviderQualifiedPath(drive.CurrentLocation, drive.Provider);
                }
            }
            else
            {
                path = LocationGlobber.GetDriveQualifiedPath(drive.CurrentLocation, drive);
            }

            return new PathInfo(drive, drive.Provider, path, new SessionState(this));
        }

        
        internal PathInfo SetLocation(string path)
        {
            return SetLocation(path, null);
        }

        
        internal PathInfo SetLocation(string path, CmdletProviderContext context)
        {
            return SetLocation(path, context, literalPath: false);
        }

        
        internal PathInfo SetLocation(string path, CmdletProviderContext context, bool literalPath)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            PathInfo current = CurrentLocation;
            string originalPath = path;
            string driveName = null;
            ProviderInfo provider = null;
            string providerId = null;

            switch (originalPath)
            {
                case string originalPathSwitch when !literalPath && originalPathSwitch.Equals("-", StringComparison.Ordinal):
                    if (_setLocationHistory.UndoCount <= 0)
                    {
                        throw new InvalidOperationException(SessionStateStrings.LocationUndoStackIsEmpty);
                    }

                    path = _setLocationHistory.Undo(this.CurrentLocation).Path;
                    break;
                case string originalPathSwitch when !literalPath && originalPathSwitch.Equals("+", StringComparison.Ordinal):
                    if (_setLocationHistory.RedoCount <= 0)
                    {
                        throw new InvalidOperationException(SessionStateStrings.LocationRedoStackIsEmpty);
                    }

                    path = _setLocationHistory.Redo(this.CurrentLocation).Path;
                    break;
                default:
                    var pushPathInfo = GetNewPushPathInfo();
                    _setLocationHistory.Push(pushPathInfo);
                    break;
            }

            PSDriveInfo previousWorkingDrive = CurrentDrive;

            // First check to see if the path is a home path
            if (LocationGlobber.IsHomePath(path))
            {
                path = Globber.GetHomeRelativePath(path);
            }

            if (LocationGlobber.IsProviderDirectPath(path))
            {
                // The path is a provider-direct path so use the current
                // provider and its hidden drive but don't modify the path
                // at all.
                provider = CurrentLocation.Provider;
                CurrentDrive = provider.HiddenDrive;
            }
            else if (LocationGlobber.IsProviderQualifiedPath(path, out providerId))
            {
                provider = GetSingleProvider(providerId);
                CurrentDrive = provider.HiddenDrive;
            }
            else
            {
                // See if the path is a relative or absolute
                // path.
                if (Globber.IsAbsolutePath(path, out driveName))
                {
                    // Since the path is an absolute path
                    // we need to change the current working
                    // drive
                    PSDriveInfo newWorkingDrive = GetDrive(driveName);
                    CurrentDrive = newWorkingDrive;

                    // If the path is simply a colon-terminated drive,
                    // not a slash-terminated path to the root of a drive,
                    // set the path to the current working directory of that drive.
                    string colonTerminatedVolume = CurrentDrive.Name + ':';
                    if (CurrentDrive.VolumeSeparatedByColon && (path.Length == colonTerminatedVolume.Length))
                    {
                        path = Path.Combine(colonTerminatedVolume + Path.DirectorySeparatorChar, CurrentDrive.CurrentLocation);
                    }

                    // Now that the current working drive is set,
                    // process the rest of the path as a relative path.
                }
            }

            context ??= new CmdletProviderContext(this.ExecutionContext);

            if (CurrentDrive != null)
            {
                context.Drive = CurrentDrive;
            }

            CmdletProvider providerInstance = null;

            Collection<PathInfo> workingPath = null;

            try
            {
                workingPath =
                    Globber.GetGlobbedMonadPathsFromMonadPath(
                        path,
                        false,
                        context,
                        out providerInstance);
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
            catch (Exception)
            {
                // Reset the drive to the previous drive and
                // then rethrow the error
                CurrentDrive = previousWorkingDrive;
                throw;
            }

            if (workingPath.Count == 0)
            {
                // Set the current working drive back to the previous
                // one in case it was changed.
                CurrentDrive = previousWorkingDrive;

                throw
                    new ItemNotFoundException(
                        path,
                        "PathNotFound",
                        SessionStateStrings.PathNotFound);
            }

            // We allow globbing the location as long as it only resolves a single container.
            bool foundContainer = false;
            bool pathIsContainer = false;
            bool pathIsProviderQualifiedPath = false;
            bool currentPathisProviderQualifiedPath = false;

            for (int index = 0; index < workingPath.Count; ++index)
            {
                CmdletProviderContext normalizePathContext =
                    new CmdletProviderContext(context);

                PathInfo resolvedPath = workingPath[index];
                string currentPath = path;
                try
                {
                    string providerName = null;
                    currentPathisProviderQualifiedPath = LocationGlobber.IsProviderQualifiedPath(resolvedPath.Path, out providerName);
                    if (currentPathisProviderQualifiedPath)
                    {
                        // The path should be the provider-qualified path without the provider ID
                        // or ::
                        string providerInternalPath = LocationGlobber.RemoveProviderQualifier(resolvedPath.Path);

                        try
                        {
                            currentPath = NormalizeRelativePath(GetSingleProvider(providerName), providerInternalPath, string.Empty, normalizePathContext);
                        }
                        catch (NotSupportedException)
                        {
                            // Since the provider does not support normalizing the path, just
                            // use the path we currently have.
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
                        catch (Exception)
                        {
                            // Reset the drive to the previous drive and
                            // then rethrow the error
                            CurrentDrive = previousWorkingDrive;
                            throw;
                        }
                    }
                    else
                    {
                        try
                        {
                            currentPath = NormalizeRelativePath(resolvedPath.Path, CurrentDrive.Root, normalizePathContext);
                        }
                        catch (NotSupportedException)
                        {
                            // Since the provider does not support normalizing the path, just
                            // use the path we currently have.
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
                        catch (Exception)
                        {
                            // Reset the drive to the previous drive and
                            // then rethrow the error
                            CurrentDrive = previousWorkingDrive;
                            throw;
                        }
                    }

                    // Now see if there was errors while normalizing the path
                    if (normalizePathContext.HasErrors())
                    {
                        // Set the current working drive back to the previous
                        // one in case it was changed.
                        CurrentDrive = previousWorkingDrive;

                        normalizePathContext.ThrowFirstErrorOrDoNothing();
                    }
                }
                finally
                {
                    normalizePathContext.RemoveStopReferral();
                }

                bool isContainer = false;

                CmdletProviderContext itemContainerContext =
                    new CmdletProviderContext(context);
                itemContainerContext.SuppressWildcardExpansion = true;

                try
                {
                    isContainer =
                        IsItemContainer(
                            resolvedPath.Path,
                            itemContainerContext);

                    if (itemContainerContext.HasErrors())
                    {
                        // Set the current working drive back to the previous
                        // one in case it was changed.
                        CurrentDrive = previousWorkingDrive;

                        itemContainerContext.ThrowFirstErrorOrDoNothing();
                    }
                }
                catch (NotSupportedException)
                {
                    if (currentPath.Length == 0)
                    {
                        // Treat this as a container because providers that only
                        // support the ContainerCmdletProvider interface are really
                        // containers at their root.
                        isContainer = true;
                    }
                }
                finally
                {
                    itemContainerContext.RemoveStopReferral();
                }

                if (isContainer)
                {
                    if (foundContainer)
                    {
                        // The path resolved to more than one container
                        // Set the current working drive back to the previous
                        // one in case it was changed.
                        CurrentDrive = previousWorkingDrive;

                        throw
                            PSTraceSource.NewArgumentException(
                                nameof(path),
                                SessionStateStrings.PathResolvedToMultiple,
                                originalPath);
                    }
                    else
                    {
                        // Set the path to use
                        path = currentPath;

                        // Mark it as a container
                        pathIsContainer = true;

                        // Mark whether or not it was provider-qualified
                        pathIsProviderQualifiedPath = currentPathisProviderQualifiedPath;

                        // Mark that we have already found one container. Finding additional
                        // should be an error
                        foundContainer = true;
                    }
                }
            }

            if (pathIsContainer)
            {
                // Remove the root slash since it is implied that the
                // current working directory is relative to the root.
                if (!LocationGlobber.IsProviderDirectPath(path) &&
                    path.StartsWith(StringLiterals.DefaultPathSeparator) &&
                    !pathIsProviderQualifiedPath)
                {
                    path = path.Substring(1);
                }

                s_tracer.WriteLine(
                    "New working path = {0}",
                    path);

                CurrentDrive.CurrentLocation = path;
            }
            else
            {
                // Set the current working drive back to the previous
                // one in case it was changed.
                CurrentDrive = previousWorkingDrive;

                throw
                    new ItemNotFoundException(
                        originalPath,
                        "PathNotFound",
                        SessionStateStrings.PathNotFound);
            }

            // Now make sure the current drive is set in the provider's
            // current working drive hashtable
            ProvidersCurrentWorkingDrive[CurrentDrive.Provider] =
                CurrentDrive;

            // Set the $PWD variable to the new location
            this.SetVariable(SpecialVariables.PWDVarPath, this.CurrentLocation, false, true, CommandOrigin.Internal);

            // If an action has been defined for location changes, invoke it now.
            if (PublicSessionState.InvokeCommand.LocationChangedAction != null)
            {
                var eventArgs = new LocationChangedEventArgs(PublicSessionState, current, CurrentLocation);
                PublicSessionState.InvokeCommand.LocationChangedAction.Invoke(ExecutionContext.CurrentRunspace, eventArgs);
                s_tracer.WriteLine("Invoked LocationChangedAction");
            }

            return this.CurrentLocation;
        }

        
        internal bool IsCurrentLocationOrAncestor(string path, CmdletProviderContext context)
        {
            bool result = false;

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            PSDriveInfo drive = null;
            ProviderInfo provider = null;

            string providerSpecificPath =
                Globber.GetProviderPath(
                    path,
                    context,
                    out provider,
                    out drive);

            if (drive != null)
            {
                s_tracer.WriteLine("Tracing drive");
                drive.Trace();
            }

            Dbg.Diagnostics.Assert(
                providerSpecificPath != null,
                "There should always be a way to generate a provider path for a " +
                "given path");

            if (drive != null)
            {
                context.Drive = drive;
            }

            // Check to see if the path that was specified is within the current
            // working drive
            if (drive == CurrentDrive)
            {
                // The path needs to be normalized to get rid of relative path tokens
                // so they don't interfere with our path comparisons below
                CmdletProviderContext normalizePathContext
                    = new CmdletProviderContext(context);

                try
                {
                    providerSpecificPath = NormalizeRelativePath(path, null, normalizePathContext);
                }
                catch (NotSupportedException)
                {
                    // Since the provider does not support normalizing the path, just
                    // use the path we currently have.
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
                finally
                {
                    normalizePathContext.RemoveStopReferral();
                }

                if (normalizePathContext.HasErrors())
                {
                    normalizePathContext.ThrowFirstErrorOrDoNothing();
                }

                s_tracer.WriteLine("Provider path = {0}", providerSpecificPath);

                // Get the current working directory provider specific path
                PSDriveInfo currentWorkingDrive = null;
                ProviderInfo currentDriveProvider = null;

                string currentWorkingPath =
                    Globber.GetProviderPath(
                        ".",
                        context,
                        out currentDriveProvider,
                        out currentWorkingDrive);

                Dbg.Diagnostics.Assert(
                    currentWorkingDrive == CurrentDrive,
                    "The current working drive should be the CurrentDrive.");

                s_tracer.WriteLine(
                    "Current working path = {0}",
                    currentWorkingPath);

                // See if the path is the current working directory or a parent
                // of the current working directory
                s_tracer.WriteLine(
                    "Comparing {0} to {1}",
                    providerSpecificPath,
                    currentWorkingPath);

                if (string.Equals(providerSpecificPath, currentWorkingPath, StringComparison.OrdinalIgnoreCase))
                {
                    // The path is the current working directory so
                    // return true
                    s_tracer.WriteLine("The path is the current working directory");

                    result = true;
                }
                else
                {
                    // Check to see if the specified path is a parent
                    // of the current working directory
                    string lockedDirectory = currentWorkingPath;

                    while (lockedDirectory.Length > 0)
                    {
                        // We need to allow the provider to go as far up the tree
                        // as it can even if that means it has to traverse higher
                        // than the mount point for this drive. That is
                        // why we are passing the empty string as the root here.
                        lockedDirectory =
                            GetParentPath(
                                drive.Provider,
                                lockedDirectory,
                                string.Empty,
                                context);

                        s_tracer.WriteLine(
                            "Comparing {0} to {1}",
                            lockedDirectory,
                            providerSpecificPath);

                        if (string.Equals(lockedDirectory, providerSpecificPath, StringComparison.OrdinalIgnoreCase))
                        {
                            // The path is a parent of the current working
                            // directory
                            s_tracer.WriteLine(
                                "The path is a parent of the current working directory: {0}",
                                lockedDirectory);

                            result = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                s_tracer.WriteLine("Drives are not the same");
            }

            return result;
        }

        #endregion Current working directory/drive

        #region push-Pop current working directory

        
        private readonly HistoryStack<PathInfo> _setLocationHistory;

        
        private readonly Dictionary<string, Stack<PathInfo>> _workingLocationStack;

        private const string startingDefaultStackName = "default";
        
        private string _defaultStackName = startingDefaultStackName;

        
        internal void PushCurrentLocation(string stackName)
        {
            if (string.IsNullOrEmpty(stackName))
            {
                stackName = _defaultStackName;
            }

            // Get the location stack from the hashtable
            Stack<PathInfo> locationStack = null;

            if (!_workingLocationStack.TryGetValue(stackName, out locationStack))
            {
                locationStack = new Stack<PathInfo>();
                _workingLocationStack[stackName] = locationStack;
            }

            // Push the directory/drive pair onto the stack
            var pushPathInfo = GetNewPushPathInfo();
            locationStack.Push(pushPathInfo);
        }

        private PathInfo GetNewPushPathInfo()
        {
            // Create a new instance of the directory/drive pair
            ProviderInfo provider = CurrentDrive.Provider;
            string mshQualifiedPath =
                LocationGlobber.GetMshQualifiedPath(CurrentDrive.CurrentLocation, CurrentDrive);

            PathInfo newPushLocation =
                new PathInfo(
                    CurrentDrive,
                    provider,
                    mshQualifiedPath,
                    new SessionState(this));

            s_tracer.WriteLine(
                "Pushing drive: {0} directory: {1}",
                CurrentDrive.Name,
                mshQualifiedPath);

            return newPushLocation;
        }

        
        internal PathInfo PopLocation(string stackName)
        {
            if (string.IsNullOrEmpty(stackName))
            {
                stackName = _defaultStackName;
            }

            if (WildcardPattern.ContainsWildcardCharacters(stackName))
            {
                // Need to glob the stack name, but it can only glob to a single.
                bool haveMatch = false;

                WildcardPattern stackNamePattern =
                    WildcardPattern.Get(stackName, WildcardOptions.IgnoreCase);

                foreach (string key in _workingLocationStack.Keys)
                {
                    if (stackNamePattern.IsMatch(key))
                    {
                        if (haveMatch)
                        {
                            throw
                                PSTraceSource.NewArgumentException(
                                    nameof(stackName),
                                    SessionStateStrings.StackNameResolvedToMultiple,
                                    stackName);
                        }

                        haveMatch = true;
                        stackName = key;
                    }
                }
            }

            PathInfo result = CurrentLocation;

            try
            {
                Stack<PathInfo> locationStack = null;
                if (!_workingLocationStack.TryGetValue(stackName, out locationStack))
                {
                    if (!string.Equals(stackName, startingDefaultStackName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw
                            PSTraceSource.NewArgumentException(
                                nameof(stackName),
                                SessionStateStrings.StackNotFound,
                                stackName);
                    }

                    return null;
                }

                PathInfo poppedWorkingDirectory = locationStack.Pop();

                Dbg.Diagnostics.Assert(
                    poppedWorkingDirectory != null,
                    "All items in the workingLocationStack should be " +
                    "of type PathInfo");

                string newPath =
                    LocationGlobber.GetMshQualifiedPath(
                        WildcardPattern.Escape(poppedWorkingDirectory.Path),
                        poppedWorkingDirectory.GetDrive());

                result = SetLocation(newPath);

                if (locationStack.Count == 0 &&
                    !string.Equals(stackName, startingDefaultStackName, StringComparison.OrdinalIgnoreCase))
                {
                    // Remove the stack from the stack list if it
                    // no longer contains any paths.
                    _workingLocationStack.Remove(stackName);
                }
            }
            catch (InvalidOperationException)
            {
                // This is a no-op. We stay with the current working
                // directory.
            }

            return result;
        }

        
        internal PathInfoStack LocationStack(string stackName)
        {
            if (string.IsNullOrEmpty(stackName))
            {
                stackName = _defaultStackName;
            }

            Stack<PathInfo> locationStack = null;

            if (!_workingLocationStack.TryGetValue(stackName, out locationStack))
            {
                // If the request was for the default stack, but it doesn't
                // yet exist, create a dummy stack and return it.
                if (string.Equals(
                        stackName,
                        startingDefaultStackName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    locationStack = new Stack<PathInfo>();
                }
                else
                {
                    throw PSTraceSource.NewArgumentException(nameof(stackName));
                }
            }

            PathInfoStack result = new PathInfoStack(stackName, locationStack);

            return result;
        }

        
        internal PathInfoStack SetDefaultLocationStack(string stackName)
        {
            if (string.IsNullOrEmpty(stackName))
            {
                stackName = startingDefaultStackName;
            }

            if (!_workingLocationStack.ContainsKey(stackName))
            {
                if (string.Equals(stackName, startingDefaultStackName, StringComparison.OrdinalIgnoreCase))
                {
                    // Since the "default" stack must always exist, create it here
                    return new PathInfoStack(startingDefaultStackName, new Stack<PathInfo>());
                }

                ItemNotFoundException itemNotFound =
                    new ItemNotFoundException(
                        stackName,
                        "StackNotFound",
                        SessionStateStrings.PathNotFound);

                throw itemNotFound;
            }

            _defaultStackName = stackName;

            Stack<PathInfo> locationStack = _workingLocationStack[_defaultStackName];

            if (locationStack != null)
            {
                return new PathInfoStack(_defaultStackName, locationStack);
            }

            return null;
        }

        #endregion push-Pop current working directory
    }

    
    public class LocationChangedEventArgs : EventArgs
    {
        
        internal LocationChangedEventArgs(SessionState sessionState, PathInfo oldPath, PathInfo newPath)
        {
            SessionState = sessionState;
            OldPath = oldPath;
            NewPath = newPath;
        }

        
        public PathInfo OldPath { get; internal set; }

        
        public PathInfo NewPath { get; internal set; }

        
        public SessionState SessionState { get; internal set; }
    }
}
