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

        
        /// <param name="cmdlet">
        /// An instance of the cmdlet.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="cmdlet"/> is null.
        /// </exception>
        internal PropertyCmdletProviderIntrinsics(Cmdlet cmdlet)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            _cmdlet = cmdlet;
            _sessionState = cmdlet.Context.EngineSessionState;
        }

        
        /// <param name="sessionState">
        /// An instance of the "real" session state.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sessionState"/> is null.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item to get the properties from.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// The properties to get from the item(s). If this is empty, null, or "*" all
        /// properties should be returned.
        /// </param>
        /// <returns>
        /// A PSObject for each item that the path represents. Each PSObject should
        /// contain a property for those in the providerSpecificPickList.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path(s) to the item(s) to get the properties from.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// The properties to get from the item(s). If this is empty, null, or "*" all
        /// properties should be returned.
        /// </param>
        /// <returns>
        /// A PSObject for each item that the path represents. Each PSObject should
        /// contain a property for those in the providerSpecificPickList.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item to get the properties from.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// The properties to get from the item(s). If this is empty, null, or "*" all
        /// properties should be returned.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject for each item that the path represents is written
        /// to the context. Each PSObject should
        /// contain a property for those in the providerSpecificPickList.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// The properties to get from the item(s). If this is empty, null, or "*" all
        /// properties should be returned.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item to set the properties on.
        /// </param>
        /// <param name="propertyValue">
        /// The properties that are to be set on the item
        /// </param>
        /// <returns>
        /// A PSObject for each item that had the property set on it.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="property"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path(s) to the item(s) to set the properties on.
        /// </param>
        /// <param name="propertyValue">
        /// The properties that are to be set on the item
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// A PSObject for each item that had the property set on it.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="property"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item to set the properties on.
        /// </param>
        /// <param name="propertyValue">
        /// The properties that are to be set on the item
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject for the property that was set is written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="property"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyValue">
        /// The properties that are to be set on the item
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item to clear the properties from.
        /// </param>
        /// <param name="propertyToClear">
        /// The properties to clear from the item(s).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path(s) to the item(s) to clear the properties from.
        /// </param>
        /// <param name="propertyToClear">
        /// The properties to clear from the item(s).
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item to clear the properties from.
        /// </param>
        /// <param name="propertyToClear">
        /// The properties to clear from the item(s).
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyToClear">
        /// The properties to clear from the item(s).
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item on which the new property should be created.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="propertyTypeName">
        /// The type of the property that should be created.
        /// </param>
        /// <param name="value">
        /// The new value of the property that should be created.
        /// </param>
        /// <returns>
        /// A PSObject for each item that the property was created on. The PSObject
        /// contains the properties that were created.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path(s) to the item(s0 on which the new property should be created.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="propertyTypeName">
        /// The type of the property that should be created.
        /// </param>
        /// <param name="value">
        /// The new value of the property that should be created.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// A PSObject for each item that the property was created on. The PSObject
        /// contains the properties that were created.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item on which the new property should be created.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="type">
        /// The type of the property that should be created.
        /// </param>
        /// <param name="value">
        /// The new value of the property that should be created.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject for each item that the property was created on
        /// is written to the context. Each PSObject
        /// contains the properties that were created.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="type">
        /// The type of the property that should be created.
        /// </param>
        /// <param name="value">
        /// The new value of the property that should be created.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item(s) on which the property should be removed.
        /// </param>
        /// <param name="propertyName">
        /// The property name that should be removed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="property"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public void Remove(string path, string propertyName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveProperty(new string[] { path }, propertyName, false, false);
        }

        
        /// <param name="path">
        /// The path(s) to the item(s) on which the property should be removed.
        /// </param>
        /// <param name="propertyName">
        /// The property name that should be removed.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="property"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public void Remove(string[] path, string propertyName, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveProperty(path, propertyName, force, literalPath);
        }

        
        /// <param name="path">
        /// The path to the item(s) on which the property should be removed.
        /// </param>
        /// <param name="propertyName">
        /// The property name that should be removed.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="property"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be removed.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item(s) on which the property should be renamed.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be renamed.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <returns>
        /// A PSObject for each item that is the new property after the rename.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/>, <paramref name="sourceProperty"/>,
        /// or <paramref name="destinationProperty"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path(s) to the item(s) on which the property should be renamed.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be renamed.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// A PSObject for each item that is the new property after the rename.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/>, <paramref name="sourceProperty"/>,
        /// or <paramref name="destinationProperty"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item(s) on which the property should be renamed.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be renamed.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject for each item that the property is renamed on is
        /// written to the context. The Shellobject contains the new property after the rename.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/>, <paramref name="sourceProperty"/>,
        /// or <paramref name="destinationProperty"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be renamed.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="sourcePath">
        /// The path to the item(s) on which the property should be copied.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item(s) to copy the property to. It can be the same
        /// as the sourcePath as long as the destinationProperty is different.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <returns>
        /// A PSObject for each item that is the new property after the copy.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="sourcePath">
        /// The path(s) to the item(s) on which the property should be copied.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item(s) to copy the property to. It can be the same
        /// as the sourcePath as long as the destinationProperty is different.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// A PSObject for each item that is the new property after the copy.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="sourcePath">
        /// The path to the item(s) on which the property should be copied.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item(s) to copy the property to. It can be the same
        /// as the sourcePath as long as the destinationProperty is different.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject for each item that the new property was copied to is
        /// written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item(s) to copy the property to. It can be the same
        /// as the sourcePath as long as the destinationProperty is different.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="sourcePath">
        /// The path to the item(s) on which the property should be moved.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be moved.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item(s) to move the property to. It can be the same
        /// as the sourcePath as long as the destinationProperty is different.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <returns>
        /// A PSObject for each item that is the new property after the move.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="destinationPath"/> resolves to more than one item.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="sourcePath">
        /// The path(s) to the item(s) on which the property should be moved.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be moved.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item(s) to move the property to. It can be the same
        /// as the sourcePath as long as the destinationProperty is different.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// A PSObject for each item that is the new property after the move.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="destinationPath"/> resolves to more than one item.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="sourcePath">
        /// The path to the item(s) on which the property should be moved.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be moved.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item(s) to move the property to. It can be the same
        /// as the sourcePath as long as the destinationProperty is different.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject for each item that the property was moved to is written
        /// to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="destinationPath"/> resolves to more than one item.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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

        
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The source name of the property to be moved.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item(s) to move the property to. It can be the same
        /// as the sourcePath as long as the destinationProperty is different.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
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
