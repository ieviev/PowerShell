// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Provider;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public sealed class CmdletProviderManagementIntrinsics
    {
        #region Constructors

        
        private CmdletProviderManagementIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        
        internal CmdletProviderManagementIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        
        public Collection<ProviderInfo> Get(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetProvider(name);
        }

        
        public ProviderInfo GetOne(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetSingleProvider(name);
        }

        
        public IEnumerable<ProviderInfo> GetAll()
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.ProviderList;
        }

        #endregion Public methods

        #region Internal methods

        
        internal static bool CheckProviderCapabilities(
            ProviderCapabilities capability,
            ProviderInfo provider)
        {
            // Check the capability

            return (provider.Capabilities & capability) != 0;
        }

        
        internal int Count
        {
            get
            {
                return _sessionState.ProviderCount;
            }
        }

        #endregion Internal methods

        #region private data

        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }
}
