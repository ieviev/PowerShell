// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace System.Management.Automation.Internal
{
    
    /// <remarks>
    /// Only use <see cref="System.Management.Automation.Internal.InternalCommand"/>
    /// as a subclass of
    /// <see cref="System.Management.Automation.Cmdlet"/>.
    /// Do not attempt to create instances of
    /// <see cref="System.Management.Automation.Internal.InternalCommand"/>
    /// independently, or to derive other classes than
    /// <see cref="System.Management.Automation.Cmdlet"/> from
    /// <see cref="System.Management.Automation.Internal.InternalCommand"/>.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Cmdlet"/>
    /// 
    [DebuggerDisplay("Command = {_commandInfo}")]
    public abstract class InternalCommand
    {
        #region private_members

        internal ICommandRuntime commandRuntime;

        #endregion private_members

        #region ctor

        
        /// <remarks>
        /// The only constructor is internal, so outside users cannot create
        /// an instance of this class.
        /// </remarks>
        internal InternalCommand()
        {
            this.CommandInfo = null;
        }

        #endregion ctor

        #region internal_members

        
        /// <value></value>
        internal IScriptExtent InvocationExtent { get; set; }

        private InvocationInfo _myInvocation = null;
        
        /// <value>The invocation object for this command.</value>
        internal InvocationInfo MyInvocation
        {
            get { return _myInvocation ??= new InvocationInfo(this); }
        }

        
        internal PSObject currentObjectInPipeline = AutomationNull.Value;

        
        internal PSObject CurrentPipelineObject
        {
            get
            {
                return currentObjectInPipeline;
            }

            set
            {
                currentObjectInPipeline = value;
            }
        }

        
        internal PSHost PSHostInternal
        {
            get { return _CBhost; }
        }

        private PSHost _CBhost;

        
        internal SessionState InternalState
        {
            get { return _state; }
        }

        private SessionState _state;

        
        internal bool IsStopping
        {
            get
            {
                MshCommandRuntime mcr = this.commandRuntime as MshCommandRuntime;
                return (mcr != null && mcr.IsStopping);
            }
        }

        
        internal CancellationToken StopToken => commandRuntime is MshCommandRuntime mcr
            ? mcr.PipelineProcessor.PipelineStopToken
            : default;

        
        private CommandInfo _commandInfo;
        
        internal CommandInfo CommandInfo
        {
            get { return _commandInfo; }

            set { _commandInfo = value; }
        }

        #endregion internal_members

        #region public_properties

        
        /// <exception cref="System.ArgumentNullException">
        /// may not be set to null
        /// </exception>
        internal ExecutionContext Context
        {
            get
            {
                return _context;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("Context");
                }

                _context = value;
                Diagnostics.Assert(_context.EngineHostInterface is InternalHost, "context.EngineHostInterface is not an InternalHost");
                _CBhost = (InternalHost)_context.EngineHostInterface;

                // Construct the session state API set from the new context

                _state = new SessionState(_context.EngineSessionState);
            }
        }

        private ExecutionContext _context;

        
        public CommandOrigin CommandOrigin
        {
            get { return CommandOriginInternal; }
        }

        internal CommandOrigin CommandOriginInternal = CommandOrigin.Internal;

        #endregion public_properties

        #region Override

        
        internal virtual void DoBeginProcessing()
        {
        }

        
        internal virtual void DoProcessRecord()
        {
        }

        
        internal virtual void DoEndProcessing()
        {
        }

        
        internal virtual void DoStopProcessing()
        {
        }

        
        internal virtual void DoCleanResource()
        {
        }

        #endregion Override

        
        /// <exception cref="System.Management.Automation.PipelineStoppedException"></exception>
        internal void ThrowIfStopping()
        {
            if (IsStopping)
                throw new PipelineStoppedException();
        }

        #region Dispose

        
        /// <remarks>
        /// Using InternalDispose instead of Dispose pattern because this
        /// interface was shipped in PowerShell V1 and 3rd cmdlets indirectly
        /// derive from this interface. If we depend on Dispose() and 3rd
        /// party cmdlets do not call base.Dispose (which is the case), we
        /// will still end up having this leak.
        /// </remarks>
        internal void InternalDispose(bool isDisposing)
        {
            _myInvocation = null;
            _state = null;
            _commandInfo = null;
            _context = null;
        }

        #endregion
    }
}

namespace System.Management.Automation
{
    #region NativeArgumentPassingStyle
    
    public enum NativeArgumentPassingStyle
    {
        
        Legacy = 0,

        
        Standard = 1,

        
        Windows = 2
    }
    #endregion NativeArgumentPassingStyle

    #region ErrorView
    
    public enum ErrorView
    {
        
        NormalView = 0,

        
        CategoryView = 1,

        
        ConciseView = 2,

        
        DetailedView = 3,
    }
    #endregion ErrorView

    #region ActionPreference
    
    public enum ActionPreference
    {
        
        SilentlyContinue = 0,

        
        Stop = 1,

        
        Continue = 2,

        
        Inquire = 3,

        
        Ignore = 4,

        
        Suspend = 5,

        
        Break = 6,
    } // enum ActionPreference
    #endregion ActionPreference

    #region ConfirmImpact
    
    public enum ConfirmImpact
    {
        
        None,
        
        Low,
        
        Medium,
        
        High,
    }
    #endregion ConfirmImpact

    
    /// <remarks>
    /// There are two ways to create a Cmdlet: by deriving from the Cmdlet base class, and by
    /// deriving from the PSCmdlet base class.  The Cmdlet base class is the primary means by
    /// which users create their own Cmdlets.  Extending this class provides support for the most
    /// common functionality, including object output and record processing.
    /// If your Cmdlet requires access to the PowerShell Runtime (for example, variables in the session state,
    /// access to the host, or information about the current Cmdlet Providers,) then you should instead
    /// derive from the PSCmdlet base class.
    /// The public members defined by the PSCmdlet class are not designed to be overridden; instead, they
    /// provided access to different aspects of the PowerShell runtime.
    /// In both cases, users should first develop and implement an object model to accomplish their
    /// task, extending the Cmdlet or PSCmdlet classes only as a thin management layer.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Internal.InternalCommand"/>
    public abstract partial class PSCmdlet : Cmdlet
    {
        #region private_members

        private ProviderIntrinsics _invokeProvider = null;

        #endregion private_members

        #region public_properties

        
        public PSHost Host
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return PSHostInternal;
                }
            }
        }

        
        public SessionState SessionState
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return this.InternalState;
                }
            }
        }

        
        public PSEventManager Events
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return this.Context.Events;
                }
            }
        }

        
        public JobRepository JobRepository
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return ((LocalRunspace)this.Context.CurrentRunspace).JobRepository;
                }
            }
        }

        
        public JobManager JobManager
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return ((LocalRunspace)this.Context.CurrentRunspace).JobManager;
                }
            }
        }

        
        internal RunspaceRepository RunspaceRepository
        {
            get
            {
                return ((LocalRunspace)this.Context.CurrentRunspace).RunspaceRepository;
            }
        }

        
        public ProviderIntrinsics InvokeProvider
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return _invokeProvider ??= new ProviderIntrinsics(this);
                }
            }
        }

        #region Provider wrappers

        /// <Content contentref="System.Management.Automation.PathIntrinsics.CurrentProviderLocation" />
        public PathInfo CurrentProviderLocation(string providerId)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (providerId == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(providerId));
                }

                PathInfo result = SessionState.Path.CurrentProviderLocation(providerId);

                Diagnostics.Assert(result != null, "DataStoreAdapterCollection.GetNamespaceCurrentLocation() should " + "throw an exception, not return null");
                return result;
            }
        }
        /// <Content contentref="System.Management.Automation.PathIntrinsics.GetUnresolvedProviderPathFromPSPath" />
        public string GetUnresolvedProviderPathFromPSPath(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
            }
        }

        /// <Content contentref="System.Management.Automation.PathIntrinsics.GetResolvedProviderPathFromPSPath" />
        public Collection<string> GetResolvedProviderPathFromPSPath(string path, out ProviderInfo provider)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return SessionState.Path.GetResolvedProviderPathFromPSPath(path, out provider);
            }
        }
        #endregion Provider wrappers

        #endregion internal_members

        #region ctor

        
        /// <remarks>
        /// Only subclasses of <see cref="System.Management.Automation.Cmdlet"/>
        /// can be created.
        /// </remarks>
        protected PSCmdlet()
        {
        }

        #endregion ctor

        #region public_methods

        #region PSVariable APIs

        /// <Content contentref="System.Management.Automation.VariableIntrinsics.GetValue" />
        public object GetVariableValue(string name)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return this.SessionState.PSVariable.GetValue(name);
            }
        }

        /// <Content contentref="System.Management.Automation.VariableIntrinsics.GetValue" />
        public object GetVariableValue(string name, object defaultValue)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return this.SessionState.PSVariable.GetValue(name, defaultValue);
            }
        }

        #endregion PSVariable APIs

        #region Parameter methods

        #endregion Parameter methods

        #endregion public_methods
    }
}
