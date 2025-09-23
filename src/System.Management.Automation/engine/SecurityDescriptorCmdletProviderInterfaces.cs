// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Security.AccessControl;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public sealed class SecurityDescriptorCmdletProviderIntrinsics
    {
        #region Constructors

        
        private SecurityDescriptorCmdletProviderIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        
        internal SecurityDescriptorCmdletProviderIntrinsics(Cmdlet cmdlet)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            _cmdlet = cmdlet;
            _sessionState = cmdlet.Context.EngineSessionState;
        }

        
        internal SecurityDescriptorCmdletProviderIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        #region GetSecurityDescriptor

        
        public Collection<PSObject> Get(string path, AccessControlSections includeSections)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object
            return _sessionState.GetSecurityDescriptor(path, includeSections);
        }

        
        internal void Get(string path,
                        AccessControlSections includeSections,
                        CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object
            _sessionState.GetSecurityDescriptor(path, includeSections, context);
        }

        #endregion GetSecurityDescriptor

        #region SetSecurityDescriptor

        
        public Collection<PSObject> Set(string path, ObjectSecurity sd)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object
            Collection<PSObject> result = _sessionState.SetSecurityDescriptor(path, sd);

            return result;
        }

        
        internal void Set(string path, ObjectSecurity sd, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.SetSecurityDescriptor(path, sd, context);
        }

        #endregion SetSecurityDescriptor

        #region NewSecurityDescriptor

        
        public ObjectSecurity NewFromPath(string path, AccessControlSections includeSections)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object
            return _sessionState.NewSecurityDescriptorFromPath(path, includeSections);
        }

        
        public ObjectSecurity NewOfType(string providerId, string type, AccessControlSections includeSections)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewSecurityDescriptorOfType(providerId,
                                                            type,
                                                            includeSections);
        }

        #endregion NewSecurityDescriptor

        #endregion Public methods

        #region private data

        private readonly Cmdlet _cmdlet;
        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }
}
