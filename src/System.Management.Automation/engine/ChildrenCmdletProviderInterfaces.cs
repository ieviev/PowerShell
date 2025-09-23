// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public sealed class ChildItemCmdletProviderIntrinsics
    {
        #region Constructors

        
        private ChildItemCmdletProviderIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        
        internal ChildItemCmdletProviderIntrinsics(Cmdlet cmdlet)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            _cmdlet = cmdlet;
            _sessionState = cmdlet.Context.EngineSessionState;
        }

        
        internal ChildItemCmdletProviderIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }
        #endregion Constructors

        #region Public methods

        #region GetChildItems

        
        public Collection<PSObject> Get(string path, bool recurse)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildItems(new string[] { path }, recurse, uint.MaxValue, false, false);
        }

        
        public Collection<PSObject> Get(string[] path, bool recurse, uint depth, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildItems(path, recurse, depth, force, literalPath);
        }

        
        public Collection<PSObject> Get(string[] path, bool recurse, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return this.Get(path, recurse, uint.MaxValue, force, literalPath);
        }

        
        internal void Get(
            string path,
            bool recurse,
            uint depth,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.GetChildItems(path, recurse, depth, context);
        }

        
        internal object GetChildItemsDynamicParameters(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildItemsDynamicParameters(path, recurse, context);
        }

        #endregion GetChildItems

        #region GetChildNames

        
        public Collection<string> GetNames(
            string path,
            ReturnContainers returnContainers,
            bool recurse)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildNames(new string[] { path }, returnContainers, recurse, uint.MaxValue, false, false);
        }

        
        public Collection<string> GetNames(
            string[] path,
            ReturnContainers returnContainers,
            bool recurse,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.GetChildNames(path, returnContainers, recurse, uint.MaxValue, force, literalPath);
        }

        
        public Collection<string> GetNames(
            string[] path,
            ReturnContainers returnContainers,
            bool recurse,
            uint depth,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.GetChildNames(path, returnContainers, recurse, depth, force, literalPath);
        }

        
        internal void GetNames(
            string path,
            ReturnContainers returnContainers,
            bool recurse,
            uint depth,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.GetChildNames(path, returnContainers, recurse, depth, context);
        }

        
        internal object GetChildNamesDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildNamesDynamicParameters(path, context);
        }

        #endregion GetChildNames

        #region HasChildItems

        
        public bool HasChild(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.HasChildItems(path, false, false);
        }

        
        public bool HasChild(string path, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.HasChildItems(path, force, literalPath);
        }

        
        internal bool HasChild(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.HasChildItems(path, context);
        }

        #endregion HasChildItems

        #endregion Public methods

        #region private data

        private readonly Cmdlet _cmdlet;
        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }

    
    public enum ReturnContainers
    {
        
        ReturnMatchingContainers,

        
        ReturnAllContainers
    }
}
