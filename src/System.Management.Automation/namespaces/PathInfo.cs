// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    public sealed class PathInfo
    {
        
        public PSDriveInfo Drive
        {
            get
            {
                PSDriveInfo result = null;

                if (_drive != null &&
                    !_drive.Hidden)
                {
                    result = _drive;
                }

                return result;
            }
        }

        
        public ProviderInfo Provider
        {
            get
            {
                return _provider;
            }
        }

        
        internal PSDriveInfo GetDrive()
        {
            return _drive;
        }

        
        public string ProviderPath
        {
            get
            {
                if (_providerPath == null)
                {
                    // Construct the providerPath

                    LocationGlobber pathGlobber = _sessionState.Internal.ExecutionContext.LocationGlobber;
                    _providerPath = pathGlobber.GetProviderPath(Path);
                }

                return _providerPath;
            }
        }

        private string _providerPath;
        private readonly SessionState _sessionState;

        
        public string Path
        {
            get
            {
                return this.ToString();
            }
        }

        private readonly PSDriveInfo _drive;
        private readonly ProviderInfo _provider;
        private readonly string _path = string.Empty;

        
        public override string ToString()
        {
            string result = _path;

            if (_drive == null ||
                _drive.Hidden)
            {
                // For hidden drives just return the current location
                result =
                    LocationGlobber.GetProviderQualifiedPath(
                        _path,
                        _provider);
            }
            else
            {
                result = LocationGlobber.GetDriveQualifiedPath(_path, _drive);
            }

            return result;
        }

        
        internal PathInfo(PSDriveInfo drive, ProviderInfo provider, string path, SessionState sessionState)
        {
            if (provider == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(provider));
            }

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _drive = drive;
            _provider = provider;
            _path = path;
            _sessionState = sessionState;
        }
    }
}
