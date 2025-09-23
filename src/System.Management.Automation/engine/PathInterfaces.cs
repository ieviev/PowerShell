// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public sealed class PathIntrinsics
    {
        #region Constructors

        
        private PathIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        
        internal PathIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        
        public PathInfo CurrentLocation
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _sessionState != null,
                    "The only constructor for this class should always set the sessionState field");

                return _sessionState.CurrentLocation;
            }
        }

        
        public PathInfo CurrentProviderLocation(string providerName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetNamespaceCurrentLocation(providerName);
        }

        
        public PathInfo CurrentFileSystemLocation
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _sessionState != null,
                    "The only constructor for this class should always set the sessionState field");

                return CurrentProviderLocation(_sessionState.ExecutionContext.ProviderNames.FileSystem);
            }
        }

        
        public PathInfo SetLocation(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetLocation(path);
        }

        
        internal PathInfo SetLocation(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetLocation(path, context);
        }

        
        internal PathInfo SetLocation(string path, CmdletProviderContext context, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetLocation(path, context, literalPath);
        }

        
        internal bool IsCurrentLocationOrAncestor(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.IsCurrentLocationOrAncestor(path, context);
        }

        
        public void PushCurrentLocation(string stackName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            _sessionState.PushCurrentLocation(stackName);
        }

        
        public PathInfo PopLocation(string stackName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.PopLocation(stackName);
        }

        
        public PathInfoStack LocationStack(string stackName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.LocationStack(stackName);
        }

        
        public PathInfoStack SetDefaultLocationStack(string stackName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.SetDefaultLocationStack(stackName);
        }

        
        public Collection<PathInfo> GetResolvedPSPathFromPSPath(string path)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedMonadPathsFromMonadPath(path, false, out providerInstance);
        }

        
        internal Collection<PathInfo> GetResolvedPSPathFromPSPath(
            string path,
            CmdletProviderContext context)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedMonadPathsFromMonadPath(path, false, context, out providerInstance);
        }

        
        public Collection<string> GetResolvedProviderPathFromPSPath(
            string path,
            out ProviderInfo provider)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromMonadPath(path, false, out provider, out providerInstance);
        }

        internal Collection<string> GetResolvedProviderPathFromPSPath(
            string path,
            bool allowNonexistingPaths,
            out ProviderInfo provider)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromMonadPath(path, allowNonexistingPaths, out provider, out providerInstance);
        }

        
        internal Collection<string> GetResolvedProviderPathFromPSPath(
            string path,
            CmdletProviderContext context,
            out ProviderInfo provider)
        {
            // The parameters will be verified by the path resolver

            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromMonadPath(path, false, context, out provider, out providerInstance);
        }

        
        public Collection<string> GetResolvedProviderPathFromProviderPath(
            string path,
            string providerId)
        {
            // The parameters will be verified by the path resolver
            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromProviderPath(path, false, providerId, out providerInstance);
        }

        
        internal Collection<string> GetResolvedProviderPathFromProviderPath(
            string path,
            string providerId,
            CmdletProviderContext context)
        {
            // The parameters will be verified by the path resolver

            Provider.CmdletProvider providerInstance = null;
            return PathResolver.GetGlobbedProviderPathsFromProviderPath(path, false, providerId, context, out providerInstance);
        }

        
        public string GetUnresolvedProviderPathFromPSPath(string path)
        {
            // The parameters will be verified by the path resolver

            return PathResolver.GetProviderPath(path);
        }

        
        public string GetUnresolvedProviderPathFromPSPath(
            string path,
            out ProviderInfo provider,
            out PSDriveInfo drive)
        {
            CmdletProviderContext context = new CmdletProviderContext(_sessionState.ExecutionContext);

            // The parameters will be verified by the path resolver

            string result = PathResolver.GetProviderPath(path, context, out provider, out drive);

            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        
        internal string GetUnresolvedProviderPathFromPSPath(
            string path,
            CmdletProviderContext context,
            out ProviderInfo provider,
            out PSDriveInfo drive)
        {
            // The parameters will be verified by the path resolver

            return PathResolver.GetProviderPath(path, context, out provider, out drive);
        }

        
        public bool IsProviderQualified(string path)
        {
            // The parameters will be verified by the path resolver

            return LocationGlobber.IsProviderQualifiedPath(path);
        }

        
        public bool IsPSAbsolute(string path, out string driveName)
        {
            // The parameters will be verified by the path resolver

            return PathResolver.IsAbsolutePath(path, out driveName);
        }

        #region Combine

        
        public string Combine(string parent, string child)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.MakePath(parent, child);
        }

        
        internal string Combine(string parent, string child, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.MakePath(parent, child, context);
        }

        #endregion Combine

        #region ParseParent

        
        public string ParseParent(string path, string root)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetParentPath(path, root);
        }

        
        internal string ParseParent(
            string path,
            string root,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetParentPath(path, root, context, false);
        }

        
        internal string ParseParent(
            string path,
            string root,
            CmdletProviderContext context,
            bool useDefaultProvider)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetParentPath(path, root, context, useDefaultProvider);
        }

        #endregion ParseParent

        #region ParseChildName

        
        public string ParseChildName(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildName(path);
        }

        
        internal string ParseChildName(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildName(path, context, false);
        }

        
        internal string ParseChildName(
            string path,
            CmdletProviderContext context,
            bool useDefaultProvider)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildName(path, context, useDefaultProvider);
        }

        #endregion ParseChildName

        #region NormalizeRelativePath

        
        public string NormalizeRelativePath(string path, string basePath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NormalizeRelativePath(path, basePath);
        }

        
        internal string NormalizeRelativePath(
            string path,
            string basePath,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NormalizeRelativePath(path, basePath, context);
        }

        #endregion NormalizeRelativePath

        #region IsValid

        
        public bool IsValid(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.IsValidPath(path);
        }

        
        internal bool IsValid(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.IsValidPath(path, context);
        }

        #endregion IsValid

        #endregion Public methods

        #region private data

        private LocationGlobber PathResolver
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _sessionState != null,
                    "The only constructor for this class should always set the sessionState field");

                return _pathResolver ??= _sessionState.ExecutionContext.LocationGlobber;
            }
        }

        private LocationGlobber _pathResolver;
        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }
}
