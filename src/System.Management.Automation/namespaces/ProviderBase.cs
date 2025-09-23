// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Internal;
using System.Management.Automation.Host;
using System.Resources;
using System.Diagnostics.CodeAnalysis; // for fxcop
using System.Security.AccessControl;

namespace System.Management.Automation.Provider
{

    
#nullable enable
    public interface ICmdletProviderSupportsHelp
    {
        
        /// <param name="helpItemName">
        /// Name of command that the help is requested for.
        /// </param>
        /// <param name="path">
        /// Full path to the current location of the user or the full path to
        /// the location of the property that the user needs help about.
        /// </param>
        /// <returns>
        /// The MAML help XML that should be presented to the user.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Maml", Justification = "Maml is an acronym.")]
        string GetHelpMaml(string helpItemName, string path);
    }
#nullable restore
    #region CmdletProvider

    
    /// <remarks>
    /// Although it is possible to derive from this base class to implement a Cmdlet Provider, in most
    /// cases one should derive from <see cref="System.Management.Automation.Provider.ItemCmdletProvider"/>,
    /// <see cref="System.Management.Automation.Provider.ContainerCmdletProvider"/>, or
    /// <see cref ="System.Management.Automation.Provider.NavigationCmdletProvider"/>
    /// </remarks>
    public abstract partial class CmdletProvider : IResourceSupplier
    {
        #region private data

        
        private CmdletProviderContext _contextBase = null;

        
        private ProviderInfo _providerInformation = null;

        #endregion private data

        #region internal members

        #region Trace object

        
        [TraceSource(
             "CmdletProviderClasses",
             "The namespace provider base classes tracer")]
        internal static readonly PSTraceSource providerBaseTracer = PSTraceSource.GetTracer(
                                                               "CmdletProviderClasses",
                                                               "The namespace provider base classes tracer");

        #endregion Trace object

        
        /// <param name="providerInfoToSet">
        /// The provider information that is stored by the Monad engine.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="providerInformation"/> is null.
        /// </exception>
        internal void SetProviderInformation(ProviderInfo providerInfoToSet)
        {
            if (providerInfoToSet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(providerInfoToSet));
            }

            _providerInformation = providerInfoToSet;
        }

        
        /// <returns>
        /// Whether the filter of the provider is set.
        /// </returns>
        internal virtual bool IsFilterSet()
        {
            bool filterSet = !string.IsNullOrEmpty(Filter);
            return filterSet;
        }

        #region CmdletProvider method wrappers

        
        /// <exception cref="NotSupportedException">
        /// On set, if the context contains credentials and the provider
        /// doesn't support credentials, or if the context contains a filter
        /// parameter and the provider does not support filters.
        /// </exception>
        internal CmdletProviderContext Context
        {
            get
            {
                return _contextBase;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                // Check that the provider supports the use of credentials
                if (value.Credential != null &&
                    value.Credential != PSCredential.Empty &&
                    !CmdletProviderManagementIntrinsics.CheckProviderCapabilities(ProviderCapabilities.Credentials, _providerInformation))
                {
                    throw PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.Credentials_NotSupported);
                }

                // Supplying Credentials for the FileSystemProvider is supported only for New-PSDrive Command.
                if (_providerInformation != null && !string.IsNullOrEmpty(_providerInformation.Name) && _providerInformation.Name.Equals("FileSystem") &&
                    value.Credential != null &&
                    value.Credential != PSCredential.Empty &&
                    !value.ExecutionContext.CurrentCommandProcessor.Command.GetType().Name.Equals("NewPSDriveCommand"))
                {
                    throw PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.FileSystemProviderCredentials_NotSupported);
                }

                // Check that the provider supports the use of filters
                if ((!string.IsNullOrEmpty(value.Filter)) &&
                    (!CmdletProviderManagementIntrinsics.CheckProviderCapabilities(ProviderCapabilities.Filter, _providerInformation)))
                {
                    throw PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.Filter_NotSupported);
                }

                // Check that the provider supports the use of transactions if the command
                // requested it
                if ((value.UseTransaction) &&
                   (!CmdletProviderManagementIntrinsics.CheckProviderCapabilities(ProviderCapabilities.Transactions, _providerInformation)))
                {
                    throw PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.Transactions_NotSupported);
                }

                _contextBase = value;
                _contextBase.ProviderInstance = this;
            }
        }

        
        /// <param name="providerInfo">
        /// The information about the provider.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        internal ProviderInfo Start(ProviderInfo providerInfo, CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;
            return Start(providerInfo);
        }

        
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object StartDynamicParameters(CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            return StartDynamicParameters();
        }

        
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        internal void Stop(CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;
            Stop();
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.StopProcessing" />
        protected internal virtual void StopProcessing()
        {
        }

        #endregion CmdletProvider method wrappers

        #region IPropertyCmdletProvider method wrappers

        
        /// <param name="path">
        /// The path to the item to retrieve properties from.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of properties that should be retrieved. If this parameter is null
        /// or empty, all properties should be retrieved.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        internal void GetProperty(
            string path,
            Collection<string> providerSpecificPickList,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IPropertyCmdletProvider propertyProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IPropertyCmdletProvider_NotSupported);
            }

            // Call interface method

            propertyProvider.GetProperty(path, providerSpecificPickList);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of properties that should be retrieved. If this parameter is null
        /// or empty, all properties should be retrieved.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object GetPropertyDynamicParameters(
            string path,
            Collection<string> providerSpecificPickList,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IPropertyCmdletProvider propertyProvider))
            {
                return null;
            }

            return propertyProvider.GetPropertyDynamicParameters(path, providerSpecificPickList);
        }

        
        /// <param name="path">
        /// The path to the item to set the properties on.
        /// </param>
        /// <param name="propertyValue">
        /// A PSObject which contains a collection of the name, type, value
        /// of the properties to be set.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        internal void SetProperty(
            string path,
            PSObject propertyValue,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IPropertyCmdletProvider propertyProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IPropertyCmdletProvider_NotSupported);
            }

            // Call interface method

            propertyProvider.SetProperty(path, propertyValue);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="propertyValue">
        /// A PSObject which contains a collection of the name, type, value
        /// of the properties to be set.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object SetPropertyDynamicParameters(
            string path,
            PSObject propertyValue,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IPropertyCmdletProvider propertyProvider))
            {
                return null;
            }

            return propertyProvider.SetPropertyDynamicParameters(path, propertyValue);
        }

        
        /// <param name="path">
        /// The path to the item from which the property should be cleared.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be cleared.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <remarks>
        /// Implement this method when you are providing access to a data store
        /// that allows dynamic clearing of properties.
        /// </remarks>
        internal void ClearProperty(
            string path,
            Collection<string> propertyName,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IPropertyCmdletProvider propertyProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IPropertyCmdletProvider_NotSupported);
            }

            // Call interface method

            propertyProvider.ClearProperty(path, propertyName);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of properties that should be cleared. If this parameter is null
        /// or empty, all properties should be cleared.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object ClearPropertyDynamicParameters(
            string path,
            Collection<string> providerSpecificPickList,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IPropertyCmdletProvider propertyProvider))
            {
                return null;
            }

            return propertyProvider.ClearPropertyDynamicParameters(path, providerSpecificPickList);
        }

        #endregion IPropertyCmdletProvider

        #region IDynamicPropertyCmdletProvider

        
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
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <remarks>
        /// Implement this method when you are providing access to a data store
        /// that allows dynamic creation of properties.
        /// </remarks>
        internal void NewProperty(
            string path,
            string propertyName,
            string propertyTypeName,
            object value,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IDynamicPropertyCmdletProvider_NotSupported);
            }

            // Call interface method

            propertyProvider.NewProperty(path, propertyName, propertyTypeName, value);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
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
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object NewPropertyDynamicParameters(
            string path,
            string propertyName,
            string propertyTypeName,
            object value,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                return null;
            }

            return propertyProvider.NewPropertyDynamicParameters(path, propertyName, propertyTypeName, value);
        }

        
        /// <param name="path">
        /// The path to the item on which the property should be removed.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property to be removed
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <remarks>
        /// Implement this method when you are providing access to a data store
        /// that allows dynamic removal of properties.
        /// </remarks>
        internal void RemoveProperty(
            string path,
            string propertyName,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IDynamicPropertyCmdletProvider_NotSupported);
            }

            // Call interface method

            propertyProvider.RemoveProperty(path, propertyName);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be removed.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object RemovePropertyDynamicParameters(
            string path,
            string propertyName,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                return null;
            }

            return propertyProvider.RemovePropertyDynamicParameters(path, propertyName);
        }

        
        /// <param name="path">
        /// The path to the item on which the property should be renamed.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be renamed.
        /// </param>
        /// <param name="newPropertyName">
        /// The new name for the property.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <remarks>
        /// Implement this method when you are providing access to a data store
        /// that allows dynamic renaming of properties.
        /// </remarks>
        internal void RenameProperty(
                    string path,
            string propertyName,
            string newPropertyName,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IDynamicPropertyCmdletProvider_NotSupported);
            }

            // Call interface method

            propertyProvider.RenameProperty(path, propertyName, newPropertyName);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property that should be renamed.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to rename it to.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object RenamePropertyDynamicParameters(
            string path,
            string sourceProperty,
            string destinationProperty,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                return null;
            }

            return propertyProvider.RenamePropertyDynamicParameters(path, sourceProperty, destinationProperty);
        }

        
        /// <param name="sourcePath">
        /// The path to the item from which the property should be copied.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property that should be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to which the property should be copied.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property that should be copied to.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <remarks>
        /// Implement this method when you are providing access to a data store
        /// that allows dynamic copying of properties.
        /// </remarks>
        internal void CopyProperty(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IDynamicPropertyCmdletProvider_NotSupported);
            }

            // Call interface method

            propertyProvider.CopyProperty(sourcePath, sourceProperty, destinationPath, destinationProperty);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property that should be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to which the property should be copied.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property that should be copied to.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object CopyPropertyDynamicParameters(
            string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                return null;
            }

            return propertyProvider.CopyPropertyDynamicParameters(path, sourceProperty, destinationPath, destinationProperty);
        }

        
        /// <param name="sourcePath">
        /// The path to the item from which the property should be moved.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property that should be moved.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to which the property should be moved.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property that should be moved to.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <remarks>
        /// Implement this method when you are providing access to a data store
        /// that allows dynamic moving of properties.
        /// </remarks>
        internal void MoveProperty(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IDynamicPropertyCmdletProvider_NotSupported);
            }

            // Call interface method

            propertyProvider.MoveProperty(sourcePath, sourceProperty, destinationPath, destinationProperty);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property that should be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to which the property should be copied.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property that should be copied to.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object MovePropertyDynamicParameters(
            string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IDynamicPropertyCmdletProvider propertyProvider))
            {
                return null;
            }

            return propertyProvider.MovePropertyDynamicParameters(path, sourceProperty, destinationPath, destinationProperty);
        }

        #endregion IDynamicPropertyCmdletProvider method wrappers

        #region IContentCmdletProvider method wrappers

        
        /// <param name="path">
        /// The path to the item to retrieve content from.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An instance of the IContentReader for the specified path.
        /// </returns>
        internal IContentReader GetContentReader(
            string path,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IContentCmdletProvider contentProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IContentCmdletProvider_NotSupported);
            }

            // Call interface method

            return contentProvider.GetContentReader(path);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object GetContentReaderDynamicParameters(
            string path,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IContentCmdletProvider contentProvider))
            {
                return null;
            }

            return contentProvider.GetContentReaderDynamicParameters(path);
        }

        
        /// <param name="path">
        /// The path to the item to set content on.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An instance of the IContentWriter for the specified path.
        /// </returns>
        internal IContentWriter GetContentWriter(
            string path,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IContentCmdletProvider contentProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IContentCmdletProvider_NotSupported);
            }

            // Call interface method

            return contentProvider.GetContentWriter(path);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object GetContentWriterDynamicParameters(
            string path,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IContentCmdletProvider contentProvider))
            {
                return null;
            }

            return contentProvider.GetContentWriterDynamicParameters(path);
        }

        
        /// <param name="path">
        /// The path to the item to clear the content from.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        internal void ClearContent(
            string path,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IContentCmdletProvider contentProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.IContentCmdletProvider_NotSupported);
            }

            // Call interface method

            contentProvider.ClearContent(path);
        }

        
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="cmdletProviderContext">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object ClearContentDynamicParameters(
            string path,
            CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            if (!(this is IContentCmdletProvider contentProvider))
            {
                return null;
            }

            return contentProvider.ClearContentDynamicParameters(path);
        }

        #endregion IContentCmdletProvider method wrappers

        #endregion internal members

        #region protected members

        
        /// <param name="providerInfo">
        /// The information about the provider that is being started.
        /// </param>
        /// <remarks>
        /// The default implementation returns the ProviderInfo instance that
        /// was passed.
        ///
        /// To have session state maintain persisted data on behalf of the provider,
        /// the provider should derive from <see cref="System.Management.Automation.ProviderInfo"/>
        /// and add any properties or
        /// methods for the data it wishes to persist.  When Start gets called the
        /// provider should construct an instance of its derived ProviderInfo using the
        /// providerInfo that is passed in and return that new instance.
        /// </remarks>
        protected virtual ProviderInfo Start(ProviderInfo providerInfo)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return providerInfo;
            }
        }

        
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object StartDynamicParameters()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        /// <remarks>
        /// A provider should override this method to free up any resources that the provider
        /// was using.
        ///
        /// The default implementation does nothing.
        /// </remarks>
        protected virtual void Stop()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
            }
        }

        
        public bool Stopping
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return Context.Stopping;
                }
            }
        }

        
        public SessionState SessionState
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return new SessionState(Context.ExecutionContext.EngineSessionState);
                }
            }
        }

        
        public ProviderIntrinsics InvokeProvider
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return new ProviderIntrinsics(Context.ExecutionContext.EngineSessionState);
                }
            }
        }

        
        public CommandInvocationIntrinsics InvokeCommand
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return new CommandInvocationIntrinsics(Context.ExecutionContext);
                }
            }
        }

        
        public PSCredential Credential
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return Context.Credential;
                }
            }
        }

        
        /// <remarks>
        /// If a derived type of ProviderInfo was returned from the Start method, it
        /// will be set here in all subsequent calls to the provider.
        /// </remarks>
        protected internal ProviderInfo ProviderInfo
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return _providerInformation;
                }
            }
        }

        
        protected PSDriveInfo PSDriveInfo
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return Context.Drive;
                }
            }
        }

        
        protected object DynamicParameters
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return Context.DynamicParameters;
                }
            }
        }

        
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        public SwitchParameter Force
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return Context.Force;
                }
            }
        }

        
        public string Filter
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return Context.Filter;
                }
            }
        }

        
        public Collection<string> Include
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return Context.Include;
                }
            }
        }

        
        public Collection<string> Exclude
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return Context.Exclude;
                }
            }
        }

        
        public PSHost Host
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    Diagnostics.Assert(
                        Context != null,
                        "The context should always be set");

                    return Context.ExecutionContext.EngineHostInterface;
                }
            }
        }

        
        public virtual char ItemSeparator => Path.DirectorySeparatorChar;

        
        public virtual char AltItemSeparator =>
#if UNIX
            '\\';
#else
            Path.AltDirectorySeparatorChar;
#endif

        #region IResourceSupplier
        
        /// <param name="baseName">
        /// the base resource name
        /// </param>
        /// <param name="resourceId">
        /// the resource id
        /// </param>
        /// <returns>
        /// the resource string corresponding to baseName and resourceId
        /// </returns>
        /// <remarks>
        /// When overriding this method, the resource string for the specified
        /// resource should be retrieved from a localized resource assembly.
        /// </remarks>
        public virtual string GetResourceString(string baseName, string resourceId)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (string.IsNullOrEmpty(baseName))
                {
                    throw PSTraceSource.NewArgumentException(nameof(baseName));
                }

                if (string.IsNullOrEmpty(resourceId))
                {
                    throw PSTraceSource.NewArgumentException(nameof(resourceId));
                }

                ResourceManager manager =
                    ResourceManagerCache.GetResourceManager(
                        this.GetType().Assembly,
                        baseName);

                string retValue = null;

                try
                {
                    retValue = manager.GetString(resourceId,
                                                  System.Globalization.CultureInfo.CurrentUICulture);
                }
                catch (MissingManifestResourceException)
                {
                    throw PSTraceSource.NewArgumentException(nameof(baseName), GetErrorText.ResourceBaseNameFailure, baseName);
                }

                if (retValue == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(resourceId), GetErrorText.ResourceIdFailure, resourceId);
                }

                return retValue;
            }
        }
        #endregion IResourceSupplier

        #region ThrowTerminatingError
        /// <Content contentref="System.Management.Automation.Cmdlet.ThrowTerminatingError" />
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (errorRecord == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(errorRecord));
                }

                if (errorRecord.ErrorDetails != null
                    && errorRecord.ErrorDetails.TextLookupError != null)
                {
                    Exception textLookupError = errorRecord.ErrorDetails.TextLookupError;
                    errorRecord.ErrorDetails.TextLookupError = null;
                    MshLog.LogProviderHealthEvent(
                        this.Context.ExecutionContext,
                        ProviderInfo.Name,
                        textLookupError,
                        Severity.Warning);
                }

                // We can't play the same game as Cmdlet.ThrowTerminatingError
                //  and save the exception in the "pipeline".  We need to pass
                //  the actual exception as a thrown exception.  So, we wrap
                //  it in ProviderInvocationException.

                ProviderInvocationException providerInvocationException =
                    new ProviderInvocationException(ProviderInfo, errorRecord);

                // Log a provider health event

                MshLog.LogProviderHealthEvent(
                    this.Context.ExecutionContext,
                    ProviderInfo.Name,
                    providerInvocationException,
                    Severity.Warning);

                throw providerInvocationException;
            }
        }
        #endregion ThrowTerminatingError

        #region User feedback mechanisms

        /// <Content contentref="System.Management.Automation.Cmdlet.ShouldProcess" />
        public bool ShouldProcess(
            string target)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                return Context.ShouldProcess(target);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.ShouldProcess" />
        public bool ShouldProcess(
            string target,
            string action)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                return Context.ShouldProcess(target, action);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.ShouldProcess" />
        public bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                return Context.ShouldProcess(
                    verboseDescription,
                    verboseWarning,
                    caption);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.ShouldProcess" />
        public bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption,
            out ShouldProcessReason shouldProcessReason)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                return Context.ShouldProcess(
                    verboseDescription,
                    verboseWarning,
                    caption,
                    out shouldProcessReason);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.ShouldContinue" />
        public bool ShouldContinue(
            string query,
            string caption)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                return Context.ShouldContinue(query, caption);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.ShouldContinue" />
        public bool ShouldContinue(
            string query,
            string caption,
            ref bool yesToAll,
            ref bool noToAll)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                return Context.ShouldContinue(
                    query, caption, ref yesToAll, ref noToAll);
            }
        }

        #region Transaction Support

        
        public bool TransactionAvailable()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (Context == null)
                    return false;
                else
                    return Context.TransactionAvailable();
            }
        }

        
        public PSTransactionContext CurrentPSTransaction
        {
            get
            {
                if (Context == null)
                    return null;
                else
                    return Context.CurrentPSTransaction;
            }
        }
        #endregion Transaction Support

        /// <Content contentref="System.Management.Automation.Cmdlet.WriteVerbose" />
        public void WriteVerbose(string text)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                Context.WriteVerbose(text);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.WriteWarning" />
        public void WriteWarning(string text)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                Context.WriteWarning(text);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.WriteProgress" />
        public void WriteProgress(ProgressRecord progressRecord)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                if (progressRecord == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(progressRecord));
                }

                Context.WriteProgress(progressRecord);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.WriteDebug" />
        public void WriteDebug(string text)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                Context.WriteDebug(text);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.WriteInformation" />
        public void WriteInformation(InformationRecord record)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                Context.WriteInformation(record);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.WriteInformation" />
        public void WriteInformation(object messageData, string[] tags)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                Context.WriteInformation(messageData, tags);
            }
        }

        
        /// <param name="item">
        /// The item being written out.
        /// </param>
        /// <param name="path">
        /// The path of the item being written out.
        /// </param>
        /// <param name="isContainer">
        /// True if the item is a container, false otherwise.
        /// </param>
        private void WriteObject(
            object item,
            string path,
            bool isContainer)
        {
            PSObject result = WrapOutputInPSObject(item, path);

            // Now add the IsContainer

            result.AddOrSetProperty("PSIsContainer", isContainer ? Boxed.True : Boxed.False);
            providerBaseTracer.WriteLine("Attaching {0} = {1}", "PSIsContainer", isContainer);

            Diagnostics.Assert(
                Context != null,
                "The context should always be set");

            Context.WriteObject(result);
        }

        
        /// <param name="item">
        /// The item being written out.
        /// </param>
        /// <param name="path">
        /// The path of the item being written out.
        /// </param>
        private void WriteObject(
            object item,
            string path)
        {
            PSObject result = WrapOutputInPSObject(item, path);

            Diagnostics.Assert(
                Context != null,
                "The context should always be set");

            Context.WriteObject(result);
        }

        
        /// <param name="item">
        /// The item to be wrapped.
        /// </param>
        /// <param name="path">
        /// The path to the item.
        /// </param>
        /// <returns>
        /// A PSObject that wraps the item and has path information attached
        /// as notes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// if <paramref name="item"/> is null.
        /// </exception>
        private PSObject WrapOutputInPSObject(
            object item,
            string path)
        {
            if (item == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(item));
            }

            PSObject result = new PSObject(item);

            Diagnostics.Assert(
                ProviderInfo != null,
                "The ProviderInfo should always be set");

            // Move the TypeNames to the wrapping object if the wrapped object
            // was an PSObject

            PSObject mshObj = item as PSObject;
            if (mshObj != null)
            {
                result.InternalTypeNames = new ConsolidatedString(mshObj.InternalTypeNames);
            }

            // Construct a provider qualified path as the Path note

            string providerQualifiedPath =
                LocationGlobber.GetProviderQualifiedPath(path, ProviderInfo);

            result.AddOrSetProperty("PSPath", providerQualifiedPath);
            providerBaseTracer.WriteLine("Attaching {0} = {1}", "PSPath", providerQualifiedPath);

            // Now get the parent path and child name

            NavigationCmdletProvider navProvider = this as NavigationCmdletProvider;
            if (navProvider != null && path != null)
            {
                // Get the parent path

                string parentPath = null;

                if (PSDriveInfo != null)
                {
                    parentPath = navProvider.GetParentPath(path, PSDriveInfo.Root, Context);
                }
                else
                {
                    parentPath = navProvider.GetParentPath(path, string.Empty, Context);
                }

                string providerQualifiedParentPath = string.Empty;

                if (!string.IsNullOrEmpty(parentPath))
                {
                    providerQualifiedParentPath =
                        LocationGlobber.GetProviderQualifiedPath(parentPath, ProviderInfo);
                }

                result.AddOrSetProperty("PSParentPath", providerQualifiedParentPath);
                providerBaseTracer.WriteLine("Attaching {0} = {1}", "PSParentPath", providerQualifiedParentPath);

                // Get the child name

                string childName = navProvider.GetChildName(path, Context);

                result.AddOrSetProperty("PSChildName", childName);
                providerBaseTracer.WriteLine("Attaching {0} = {1}", "PSChildName", childName);
#if UNIX

                // Add a commonstat structure to file system objects
                if (ProviderInfo.ImplementingType == typeof(Microsoft.PowerShell.Commands.FileSystemProvider))
                {
                    try
                    {
                        // Use LStat because if you get a link, you want the information about the
                        // link, not the file.
                        var commonStat = Platform.Unix.GetLStat(path);
                        result.AddOrSetProperty("UnixStat", commonStat);
                    }
                    catch
                    {
                        // If there is *any* problem in retrieving the stat information
                        // set the property to null. There is no specific exception which
                        // would result in different behavior.
                        result.AddOrSetProperty("UnixStat", value: null);
                    }
                }
#endif
            }

            // PSDriveInfo

            if (PSDriveInfo != null)
            {
                result.AddOrSetProperty(this.PSDriveInfo.GetNotePropertyForProviderCmdlets("PSDrive"));
                providerBaseTracer.WriteLine("Attaching {0} = {1}", "PSDrive", this.PSDriveInfo);
            }

            // ProviderInfo

            result.AddOrSetProperty(this.ProviderInfo.GetNotePropertyForProviderCmdlets("PSProvider"));
            providerBaseTracer.WriteLine("Attaching {0} = {1}", "PSProvider", this.ProviderInfo);

            return result;
        }

        
        /// <param name="item">
        /// The item to be written.
        /// </param>
        /// <param name="path">
        /// The path of the item being written.
        /// </param>
        /// <param name="isContainer">
        /// True if the item is a container, false otherwise.
        /// </param>
        /// 
        public void WriteItemObject(
            object item,
            string path,
            bool isContainer)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                WriteObject(item, path, isContainer);
            }
        }

        
        /// <param name="propertyValue">
        /// The properties to be written.
        /// </param>
        /// <param name="path">
        /// The path of the item being written.
        /// </param>
        /// 
        public void WritePropertyObject(
            object propertyValue,
            string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                WriteObject(propertyValue, path);
            }
        }

        
        /// <param name="securityDescriptor">
        /// The Security Descriptor to be written.
        /// </param>
        /// <param name="path">
        /// The path of the item from which the Security Descriptor was retrieved.
        /// </param>
        /// 
        public void WriteSecurityDescriptorObject(
            ObjectSecurity securityDescriptor,
            string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                WriteObject(securityDescriptor, path);
            }
        }

        /// <Content contentref="System.Management.Automation.Cmdlet.WriteError" />
        public void WriteError(ErrorRecord errorRecord)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                Diagnostics.Assert(
                    Context != null,
                    "The context should always be set");

                if (errorRecord == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(errorRecord));
                }

                if (errorRecord.ErrorDetails != null
                    && errorRecord.ErrorDetails.TextLookupError != null)
                {
                    MshLog.LogProviderHealthEvent(
                        this.Context.ExecutionContext,
                        ProviderInfo.Name,
                        errorRecord.ErrorDetails.TextLookupError,
                        Severity.Warning);
                }

                Context.WriteError(errorRecord);
            }
        }

        #endregion User feedback mechanisms

        #endregion protected members
    }

    #endregion CmdletProvider
}

#pragma warning restore 56506
