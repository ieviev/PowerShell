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
        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Maml", Justification = "Maml is an acronym.")]
        string GetHelpMaml(string helpItemName, string path);
    }
#nullable restore
    #region CmdletProvider

    
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

        
        internal void SetProviderInformation(ProviderInfo providerInfoToSet)
        {
            if (providerInfoToSet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(providerInfoToSet));
            }

            _providerInformation = providerInfoToSet;
        }

        
        internal virtual bool IsFilterSet()
        {
            bool filterSet = !string.IsNullOrEmpty(Filter);
            return filterSet;
        }

        #region CmdletProvider method wrappers

        
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

        
        internal ProviderInfo Start(ProviderInfo providerInfo, CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;
            return Start(providerInfo);
        }

        
        internal object StartDynamicParameters(CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;

            return StartDynamicParameters();
        }

        
        internal void Stop(CmdletProviderContext cmdletProviderContext)
        {
            Context = cmdletProviderContext;
            Stop();
        }

        protected internal virtual void StopProcessing()
        {
        }

        #endregion CmdletProvider method wrappers

        #region IPropertyCmdletProvider method wrappers

        
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

        
        protected virtual ProviderInfo Start(ProviderInfo providerInfo)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return providerInfo;
            }
        }

        
        protected virtual object StartDynamicParameters()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
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

        
        public void WritePropertyObject(
            object propertyValue,
            string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                WriteObject(propertyValue, path);
            }
        }

        
        public void WriteSecurityDescriptorObject(
            ObjectSecurity securityDescriptor,
            string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                WriteObject(securityDescriptor, path);
            }
        }

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
