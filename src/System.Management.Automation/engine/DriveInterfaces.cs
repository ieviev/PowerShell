// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public sealed class DriveManagementIntrinsics
    {
        #region Constructors

        
        private DriveManagementIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        
        internal DriveManagementIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        
        public PSDriveInfo Current
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _sessionState != null,
                    "The only constructor for this class should always set the sessionState field");

                return _sessionState.CurrentDrive;
            }
        }

        #region New

        
        public PSDriveInfo New(PSDriveInfo drive, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewDrive(drive, scope);
        }

        
        internal void New(
            PSDriveInfo drive,
            string scope,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.NewDrive(drive, scope, context);
        }

        
        internal object NewDriveDynamicParameters(
            string providerId,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewDriveDynamicParameters(providerId, context);
        }

        #endregion New

        #region Remove

        
        public void Remove(string driveName, bool force, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveDrive(driveName, force, scope);
        }

        
        internal void Remove(
            string driveName,
            bool force,
            string scope,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveDrive(driveName, force, scope, context);
        }

        #endregion Remove

        #region Get

        
        public PSDriveInfo Get(string driveName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetDrive(driveName);
        }

        
        public PSDriveInfo GetAtScope(string driveName, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetDrive(driveName, scope);
        }

        
        public Collection<PSDriveInfo> GetAll()
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.Drives(null);
        }

        
        public Collection<PSDriveInfo> GetAllAtScope(string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.Drives(scope);
        }

        
        public Collection<PSDriveInfo> GetAllForProvider(string providerName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetDrivesForProvider(providerName);
        }

        #endregion GetDrive

        #endregion Public methods

        #region private data

        // A private reference to the internal session state of the engine.
        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }
}
