// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public sealed class PropertyCmdletProviderIntrinsics
    {
        #region Constructors

        
        private PropertyCmdletProviderIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        
        internal PropertyCmdletProviderIntrinsics(Cmdlet cmdlet)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            _cmdlet = cmdlet;
            _sessionState = cmdlet.Context.EngineSessionState;
        }

        
        internal PropertyCmdletProviderIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        #region GetProperty

        
        public Collection<PSObject> Get(
            string path,
            Collection<string> providerSpecificPickList)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetProperty(new string[] { path }, providerSpecificPickList, false);
        }

        
        public Collection<PSObject> Get(
            string[] path,
            Collection<string> providerSpecificPickList,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetProperty(path, providerSpecificPickList, literalPath);
        }

        
        internal void Get(
            string path,
            Collection<string> providerSpecificPickList,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.GetProperty(new string[] { path }, providerSpecificPickList, context);
        }

        
        internal object GetPropertyDynamicParameters(
            string path,
            Collection<string> providerSpecificPickList,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetPropertyDynamicParameters(path, providerSpecificPickList, context);
        }

        #endregion GetProperty

        #region SetProperty

        
        public Collection<PSObject> Set(
            string path,
            PSObject propertyValue)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetProperty(new string[] { path }, propertyValue, false, false);
        }

        
        public Collection<PSObject> Set(
            string[] path,
            PSObject propertyValue,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetProperty(path, propertyValue, force, literalPath);
        }

        
        internal void Set(
            string path,
            PSObject propertyValue,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.SetProperty(new string[] { path }, propertyValue, context);
        }

        
        internal object SetPropertyDynamicParameters(
            string path,
            PSObject propertyValue,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetPropertyDynamicParameters(path, propertyValue, context);
        }

        #endregion SetProperty

        #region ClearProperty

        
        public void Clear(
            string path,
            Collection<string> propertyToClear)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.ClearProperty(new string[] { path }, propertyToClear, false, false);
        }

        
        public void Clear(
            string[] path,
            Collection<string> propertyToClear,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.ClearProperty(path, propertyToClear, force, literalPath);
        }

        
        internal void Clear(
            string path,
            Collection<string> propertyToClear,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.ClearProperty(new string[] { path }, propertyToClear, context);
        }

        
        internal object ClearPropertyDynamicParameters(
            string path,
            Collection<string> propertyToClear,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.ClearPropertyDynamicParameters(path, propertyToClear, context);
        }

        #endregion ClearProperty

        #region NewProperty

        
        public Collection<PSObject> New(
            string path,
            string propertyName,
            string propertyTypeName,
            object value)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewProperty(new string[] { path }, propertyName, propertyTypeName, value, false, false);
        }

        
        public Collection<PSObject> New(
            string[] path,
            string propertyName,
            string propertyTypeName,
            object value,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewProperty(path, propertyName, propertyTypeName, value, force, literalPath);
        }

        
        internal void New(
            string path,
            string propertyName,
            string type,
            object value,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.NewProperty(new string[] { path }, propertyName, type, value, context);
        }

        
        internal object NewPropertyDynamicParameters(
            string path,
            string propertyName,
            string type,
            object value,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewPropertyDynamicParameters(path, propertyName, type, value, context);
        }

        #endregion NewProperty

        #region RemoveProperty

        
        public void Remove(string path, string propertyName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveProperty(new string[] { path }, propertyName, false, false);
        }

        
        public void Remove(string[] path, string propertyName, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveProperty(path, propertyName, force, literalPath);
        }

        
        internal void Remove(
            string path,
            string propertyName,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveProperty(new string[] { path }, propertyName, context);
        }

        
        internal object RemovePropertyDynamicParameters(
            string path,
            string propertyName,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.RemovePropertyDynamicParameters(path, propertyName, context);
        }

        #endregion RemoveProperty

        #region RenameProperty

        
        public Collection<PSObject> Rename(
            string path,
            string sourceProperty,
            string destinationProperty)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.RenameProperty(new string[] { path }, sourceProperty, destinationProperty, false, false);
        }

        
        public Collection<PSObject> Rename(
            string[] path,
            string sourceProperty,
            string destinationProperty,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.RenameProperty(path, sourceProperty, destinationProperty, force, literalPath);
        }

        
        internal void Rename(
            string path,
            string sourceProperty,
            string destinationProperty,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RenameProperty(new string[] { path }, sourceProperty, destinationProperty, context);
        }

        
        internal object RenamePropertyDynamicParameters(
            string path,
            string sourceProperty,
            string destinationProperty,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.RenamePropertyDynamicParameters(path, sourceProperty, destinationProperty, context);
        }

        #endregion RenameProperty

        #region CopyProperty

        
        public Collection<PSObject> Copy(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return
                _sessionState.CopyProperty(
                    new string[] { sourcePath },
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    false, false);
        }

        
        public Collection<PSObject> Copy(
            string[] sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return
                _sessionState.CopyProperty(
                    sourcePath,
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    force,
                    literalPath);
        }

        
        internal void Copy(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.CopyProperty(
                new string[] { sourcePath },
                sourceProperty,
                destinationPath,
                destinationProperty,
                context);
        }

        
        internal object CopyPropertyDynamicParameters(
            string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.CopyPropertyDynamicParameters(path, sourceProperty, destinationPath, destinationProperty, context);
        }

        #endregion CopyProperty

        #region MoveProperty

        
        public Collection<PSObject> Move(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return
                _sessionState.MoveProperty(
                    new string[] { sourcePath },
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    false,
                    false);
        }

        
        public Collection<PSObject> Move(
            string[] sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return
                _sessionState.MoveProperty(
                    sourcePath,
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    force,
                    literalPath);
        }

        
        internal void Move(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.MoveProperty(
                new string[] { sourcePath },
                sourceProperty,
                destinationPath,
                destinationProperty,
                context);
        }

        
        internal object MovePropertyDynamicParameters(
            string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.MovePropertyDynamicParameters(path, sourceProperty, destinationPath, destinationProperty, context);
        }

        #endregion MoveProperty

        #endregion Public methods

        #region private data

        private readonly Cmdlet _cmdlet;
        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }
}
