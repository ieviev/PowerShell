// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public sealed class PSVariableIntrinsics
    {
        #region Constructors

        
        private PSVariableIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        
        internal PSVariableIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        
        public PSVariable Get(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            // Null is returned whenever the requested variable is string.Empty.
            // As per Powershell V1 implementation:
            // 1. If the requested variable exists in the session scope, the variable value is returned.
            // 2. If the requested variable is not null and does not exist in the session scope, then a null value is returned to the pipeline.
            // 3. If the requested variable is null then an NewArgumentNullException is thrown.
            // PowerShell V3 has the similar experience.
            if (name != null && name.Equals(string.Empty))
            {
                return null;
            }

            return _sessionState.GetVariable(name);
        }

        
        internal PSVariable GetAtScope(string name, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetVariableAtScope(name, scope);
        }

        
        public object GetValue(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetVariableValue(name);
        }

        
        public object GetValue(string name, object defaultValue)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetVariableValue(name) ?? defaultValue;
        }

        
        internal object GetValueAtScope(string name, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetVariableValueAtScope(name, scope);
        }

        
        public void Set(string name, object value)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.SetVariableValue(name, value, CommandOrigin.Internal);
        }

        
        public void Set(PSVariable variable)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.SetVariable(variable, false, CommandOrigin.Internal);
        }

        
        public void Remove(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveVariable(name);
        }

        
        public void Remove(PSVariable variable)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveVariable(variable);
        }

        
        internal void RemoveAtScope(string name, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveVariableAtScope(name, scope);
        }

        
        internal void RemoveAtScope(PSVariable variable, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveVariableAtScope(variable, scope);
        }

        #endregion Public methods

        #region private data

        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }
}
