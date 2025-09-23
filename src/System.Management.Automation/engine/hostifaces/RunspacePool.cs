// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces.Internal;
using System.Runtime.Serialization;
using System.Threading;

using PSHost = System.Management.Automation.Host.PSHost;

namespace System.Management.Automation.Runspaces
{
    #region Exceptions
    
    public class InvalidRunspacePoolStateException : SystemException
    {
        
        public InvalidRunspacePoolStateException()
        : base
        (
            StringUtil.Format(RunspacePoolStrings.InvalidRunspacePoolStateGeneral)
        )
        {
        }

        
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public InvalidRunspacePoolStateException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// </param>
        public InvalidRunspacePoolStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="message">The message that describes the error.</param>
        /// <param name="currentState">Current state of runspace pool.</param>
        /// <param name="expectedState">Expected state of the runspace pool.</param>
        internal InvalidRunspacePoolStateException
        (
            string message,
            RunspacePoolState currentState,
            RunspacePoolState expectedState
        )
            : base(message)
        {
            _expectedState = expectedState;
            _currentState = currentState;
        }

        #region ISerializable Members

        // No need to implement GetObjectData
        // if all fields are static or [NonSerialized]

        
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> that holds
        /// the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The <see cref="StreamingContext"/> that contains
        /// contextual information about the source or destination.
        /// </param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected
        InvalidRunspacePoolStateException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion

        
        /// <remarks>
        /// This is the state of the runspace pool when exception was thrown.
        /// </remarks>
        public RunspacePoolState CurrentState
        {
            get
            {
                return _currentState;
            }
        }

        
        public RunspacePoolState ExpectedState
        {
            get
            {
                return _expectedState;
            }
        }

        
        internal InvalidRunspaceStateException ToInvalidRunspaceStateException()
        {
            InvalidRunspaceStateException exception = new InvalidRunspaceStateException(
                RunspaceStrings.InvalidRunspaceStateGeneral,
                this);
            exception.CurrentState = RunspacePoolStateToRunspaceState(this.CurrentState);
            exception.ExpectedState = RunspacePoolStateToRunspaceState(this.ExpectedState);
            return exception;
        }

        
        private static RunspaceState RunspacePoolStateToRunspaceState(RunspacePoolState state)
        {
            switch (state)
            {
                case RunspacePoolState.BeforeOpen:
                    return RunspaceState.BeforeOpen;

                case RunspacePoolState.Opening:
                    return RunspaceState.Opening;

                case RunspacePoolState.Opened:
                    return RunspaceState.Opened;

                case RunspacePoolState.Closed:
                    return RunspaceState.Closed;

                case RunspacePoolState.Closing:
                    return RunspaceState.Closing;

                case RunspacePoolState.Broken:
                    return RunspaceState.Broken;

                case RunspacePoolState.Disconnecting:
                    return RunspaceState.Disconnecting;

                case RunspacePoolState.Disconnected:
                    return RunspaceState.Disconnected;

                case RunspacePoolState.Connecting:
                    return RunspaceState.Connecting;

                default:
                    Diagnostics.Assert(false, "Unexpected RunspacePoolState");
                    return 0;
            }
        }

        
        [NonSerialized]
        private readonly RunspacePoolState _currentState = 0;

        
        [NonSerialized]
        private readonly RunspacePoolState _expectedState = 0;
    }
    #endregion

    #region State
    
    public enum RunspacePoolState
    {
        
        BeforeOpen = 0,
        
        Opening = 1,
        
        Opened = 2,
        
        Closed = 3,
        
        Closing = 4,
        
        Broken = 5,

        
        Disconnecting = 6,

        
        Disconnected = 7,

        
        Connecting = 8,
    }

    
    public sealed class RunspacePoolStateChangedEventArgs : EventArgs
    {
        #region Constructors

        
        /// <param name="state">
        /// state to raise the event with.
        /// </param>
        internal RunspacePoolStateChangedEventArgs(RunspacePoolState state)
        {
            RunspacePoolStateInfo = new RunspacePoolStateInfo(state, null);
        }

        
        /// <param name="stateInfo"></param>
        internal RunspacePoolStateChangedEventArgs(RunspacePoolStateInfo stateInfo)
        {
            RunspacePoolStateInfo = stateInfo;
        }

        #endregion

        #region Public Properties

        
        public RunspacePoolStateInfo RunspacePoolStateInfo { get; }

        #endregion

        #region Private Data

        #endregion
    }

    
    internal sealed class RunspaceCreatedEventArgs : EventArgs
    {
        #region Private Data

        #endregion

        #region Constructors

        
        /// <param name="runspace"></param>
        internal RunspaceCreatedEventArgs(Runspace runspace)
        {
            Runspace = runspace;
        }

        #endregion

        #region Internal Properties

        internal Runspace Runspace { get; }

        #endregion
    }

    #endregion

    #region RunspacePool Availability

    
    public enum RunspacePoolAvailability
    {
        
        None = 0,

        
        Available = 1,

        
        Busy = 2
    }

    #endregion

    #region RunspacePool Capabilities

    
    public enum RunspacePoolCapability
    {
        
        Default = 0x0,

        
        SupportsDisconnect = 0x1
    }

    #endregion

    #region AsyncResult

    
    internal sealed class RunspacePoolAsyncResult : AsyncResult
    {
        #region Private Data

        #endregion

        #region Constructor

        
        /// <param name="ownerId">
        /// Instance Id of the pool creating this instance
        /// </param>
        /// <param name="callback">
        /// Callback to call when the async operation completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the "callback" with.
        /// </param>
        /// <param name="isCalledFromOpenAsync">
        /// true if AsyncResult monitors Async Open.
        /// false otherwise
        /// </param>
        internal RunspacePoolAsyncResult(Guid ownerId, AsyncCallback callback, object state,
            bool isCalledFromOpenAsync)
            : base(ownerId, callback, state)
        {
            IsAssociatedWithAsyncOpen = isCalledFromOpenAsync;
        }

        #endregion

        #region Internal Properties

        
        internal bool IsAssociatedWithAsyncOpen { get; }

        #endregion
    }

    
    internal sealed class GetRunspaceAsyncResult : AsyncResult
    {
        #region Private Data

        private bool _isActive;

        #endregion

        #region Constructor

        
        /// <param name="ownerId">
        /// Instance Id of the pool creating this instance
        /// </param>
        /// <param name="callback">
        /// Callback to call when the async operation completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the "callback" with.
        /// </param>
        internal GetRunspaceAsyncResult(Guid ownerId, AsyncCallback callback, object state)
            : base(ownerId, callback, state)
        {
            _isActive = true;
        }

        #endregion

        #region Internal Methods/Properties

        
        /// <remarks>
        /// This can be null if the async Get operation is not completed.
        /// </remarks>
        internal Runspace Runspace { get; set; }

        
        internal bool IsActive
        {
            get
            {
                lock (SyncObject)
                {
                    return _isActive;
                }
            }

            set
            {
                lock (SyncObject)
                {
                    _isActive = value;
                }
            }
        }

        
        /// <param name="state">
        /// This is not used
        /// </param>
        /// <remarks>
        /// This method is called from a thread pool thread to release
        /// the async operation.
        /// </remarks>
        internal void DoComplete(object state)
        {
            SetAsCompleted(null);
        }

        #endregion
    }

    #endregion

    #region RunspacePool

    
    public sealed class RunspacePool : IDisposable
    {
        #region Private Data

        private readonly RunspacePoolInternal _internalPool;
        private readonly object _syncObject = new object();

        private event EventHandler<RunspacePoolStateChangedEventArgs> InternalStateChanged = null;

        private event EventHandler<PSEventArgs> InternalForwardEvent = null;

        private event EventHandler<RunspaceCreatedEventArgs> InternalRunspaceCreated = null;

        #endregion

        #region Internal Constructor

        
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="host">
        /// The explicit PSHost implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Host is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Maximum runspaces is less than 1.
        /// Minimum runspaces is less than 1.
        /// </exception>
        internal RunspacePool(int minRunspaces, int maxRunspaces, PSHost host)
        {
            // Currently we support only Local Runspace Pool..
            // this needs to be changed once remote runspace pool
            // is implemented

            _internalPool = new RunspacePoolInternal(minRunspaces, maxRunspaces, host);
        }

        
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="initialSessionState">
        /// InitialSessionState object to use when creating a new Runspace.
        /// </param>
        /// <param name="host">
        /// The explicit PSHost implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// initialSessionState is null.
        /// Host is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Maximum runspaces is less than 1.
        /// Minimum runspaces is less than 1.
        /// </exception>
        internal RunspacePool(int minRunspaces, int maxRunspaces,
            InitialSessionState initialSessionState, PSHost host)
        {
            // Currently we support only Local Runspace Pool..
            // this needs to be changed once remote runspace pool
            // is implemented

            _internalPool = new RunspacePoolInternal(minRunspaces,
                maxRunspaces, initialSessionState, host);
        }

        
        /// <param name="minRunspaces">Min runspaces.</param>
        /// <param name="maxRunspaces">Max runspaces.</param>
        /// <param name="typeTable">TypeTable.</param>
        /// <param name="host">Host.</param>
        /// <param name="applicationArguments">App arguments.</param>
        /// <param name="connectionInfo">Connection information.</param>
        /// <param name="name">Session name.</param>
        internal RunspacePool(
            int minRunspaces,
            int maxRunspaces,
            TypeTable typeTable,
            PSHost host,
            PSPrimitiveDictionary applicationArguments,
            RunspaceConnectionInfo connectionInfo,
            string name = null)
        {
            _internalPool = new RemoteRunspacePoolInternal(
                minRunspaces,
                maxRunspaces,
                typeTable,
                host,
                applicationArguments,
                connectionInfo,
                name);

            IsRemote = true;
        }

        
        /// <param name="isDisconnected">Indicates whether the shell/runspace pool is disconnected.</param>
        /// <param name="instanceId">Identifies a remote runspace pool session to connect to.</param>
        /// <param name="name">Friendly name for runspace pool.</param>
        /// <param name="connectCommands">Runspace pool running commands information.</param>
        /// <param name="connectionInfo">Connection information of remote server.</param>
        /// <param name="host">PSHost object.</param>
        /// <param name="typeTable">TypeTable used for serialization/deserialization of remote objects.</param>
        internal RunspacePool(
            bool isDisconnected,
            Guid instanceId,
            string name,
            ConnectCommandInfo[] connectCommands,
            RunspaceConnectionInfo connectionInfo,
            PSHost host,
            TypeTable typeTable)
        {
            // Disconnect-Connect semantics are currently only supported in WSMan transport.
            if (connectionInfo is not WSManConnectionInfo)
            {
                throw new NotSupportedException();
            }

            _internalPool = new RemoteRunspacePoolInternal(instanceId, name, isDisconnected, connectCommands,
                connectionInfo, host, typeTable);

            IsRemote = true;
        }

        #endregion

        #region Public Properties

        
        public Guid InstanceId
        {
            get
            {
                return _internalPool.InstanceId;
            }
        }

        
        public bool IsDisposed
        {
            get
            {
                return _internalPool.IsDisposed;
            }
        }

        
        public RunspacePoolStateInfo RunspacePoolStateInfo
        {
            get
            {
                return _internalPool.RunspacePoolStateInfo;
            }
        }

        
        public InitialSessionState InitialSessionState
        {
            get
            {
                return _internalPool.InitialSessionState;
            }
        }

        
        public RunspaceConnectionInfo ConnectionInfo
        {
            get
            {
                return _internalPool.ConnectionInfo;
            }
        }

        
        public TimeSpan CleanupInterval
        {
            get { return _internalPool.CleanupInterval; }

            set { _internalPool.CleanupInterval = value; }
        }

        
        public RunspacePoolAvailability RunspacePoolAvailability
        {
            get { return _internalPool.RunspacePoolAvailability; }
        }

        #endregion

        #region events

        
        public event EventHandler<RunspacePoolStateChangedEventArgs> StateChanged
        {
            add
            {
                lock (_syncObject)
                {
                    bool firstEntry = (InternalStateChanged == null);
                    InternalStateChanged += value;
                    if (firstEntry)
                    {
                        // call any event handlers on this object, replacing the
                        // internalPool sender with 'this' since receivers
                        // are expecting a RunspacePool.
                        _internalPool.StateChanged += OnStateChanged;
                    }
                }
            }

            remove
            {
                lock (_syncObject)
                {
                    InternalStateChanged -= value;
                    if (InternalStateChanged == null)
                    {
                        _internalPool.StateChanged -= OnStateChanged;
                    }
                }
            }
        }

        
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void OnStateChanged(object source, RunspacePoolStateChangedEventArgs args)
        {
            if (ConnectionInfo is NewProcessConnectionInfo)
            {
                NewProcessConnectionInfo connectionInfo = ConnectionInfo as NewProcessConnectionInfo;
                if (connectionInfo.Process != null &&
                    (args.RunspacePoolStateInfo.State == RunspacePoolState.Opened ||
                     args.RunspacePoolStateInfo.State == RunspacePoolState.Broken))
                {
                    connectionInfo.Process.RunspacePool = this;
                }
            }

            // call any event handlers on this, replacing the
            // internalPool sender with 'this' since receivers
            // are expecting a RunspacePool
            InternalStateChanged.SafeInvoke(this, args);
        }

        
        internal event EventHandler<PSEventArgs> ForwardEvent
        {
            add
            {
                lock (_syncObject)
                {
                    bool firstEntry = InternalForwardEvent == null;

                    InternalForwardEvent += value;

                    if (firstEntry)
                    {
                        _internalPool.ForwardEvent += OnInternalPoolForwardEvent;
                    }
                }
            }

            remove
            {
                lock (_syncObject)
                {
                    InternalForwardEvent -= value;

                    if (InternalForwardEvent == null)
                    {
                        _internalPool.ForwardEvent -= OnInternalPoolForwardEvent;
                    }
                }
            }
        }

        
        private void OnInternalPoolForwardEvent(object sender, PSEventArgs e)
        {
            OnEventForwarded(e);
        }

        
        private void OnEventForwarded(PSEventArgs e)
        {
            InternalForwardEvent?.Invoke(this, e);
        }

        
        internal event EventHandler<RunspaceCreatedEventArgs> RunspaceCreated
        {
            add
            {
                lock (_syncObject)
                {
                    bool firstEntry = (InternalRunspaceCreated == null);
                    InternalRunspaceCreated += value;
                    if (firstEntry)
                    {
                        // call any event handlers on this object, replacing the
                        // internalPool sender with 'this' since receivers
                        // are expecting a RunspacePool.
                        _internalPool.RunspaceCreated += OnRunspaceCreated;
                    }
                }
            }

            remove
            {
                lock (_syncObject)
                {
                    InternalRunspaceCreated -= value;
                    if (InternalRunspaceCreated == null)
                    {
                        _internalPool.RunspaceCreated -= OnRunspaceCreated;
                    }
                }
            }
        }

        
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void OnRunspaceCreated(object source, RunspaceCreatedEventArgs args)
        {
            // call any event handlers on this, replacing the
            // internalPool sender with 'this' since receivers
            // are expecting a RunspacePool
            InternalRunspaceCreated.SafeInvoke(this, args);
        }

        #endregion events

        #region Public static methods.

        
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <returns>Array of RunspacePool objects each in the Disconnected state.</returns>
        public static RunspacePool[] GetRunspacePools(RunspaceConnectionInfo connectionInfo)
        {
            return GetRunspacePools(connectionInfo, null, null);
        }

        
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <param name="host">Client host object.</param>
        /// <returns>Array of RunspacePool objects each in the Disconnected state.</returns>
        public static RunspacePool[] GetRunspacePools(RunspaceConnectionInfo connectionInfo, PSHost host)
        {
            return GetRunspacePools(connectionInfo, host, null);
        }

        
        /// <param name="connectionInfo">Connection object for the target server.</param>
        /// <param name="host">Client host object.</param>
        /// <param name="typeTable">TypeTable object.</param>
        /// <returns>Array of RunspacePool objects each in the Disconnected state.</returns>
        public static RunspacePool[] GetRunspacePools(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable)
        {
            return RemoteRunspacePoolInternal.GetRemoteRunspacePools(connectionInfo, host, typeTable);
        }

        #endregion

        #region Public Disconnect-Connect API

        
        public void Disconnect()
        {
            _internalPool.Disconnect();
        }

        
        /// <param name="callback">An AsyncCallback to call once the BeginClose completes.</param>
        /// <param name="state">A user supplied state to call the callback with.</param>
        public IAsyncResult BeginDisconnect(AsyncCallback callback, object state)
        {
            return _internalPool.BeginDisconnect(callback, state);
        }

        
        /// <param name="asyncResult">Asynchronous call result object.</param>
        public void EndDisconnect(IAsyncResult asyncResult)
        {
            _internalPool.EndDisconnect(asyncResult);
        }

        
        public void Connect()
        {
            _internalPool.Connect();
        }

        
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public IAsyncResult BeginConnect(AsyncCallback callback, object state)
        {
            return _internalPool.BeginConnect(callback, state);
        }

        
        /// <param name="asyncResult">Asynchronous call result object.</param>
        public void EndConnect(IAsyncResult asyncResult)
        {
            _internalPool.EndConnect(asyncResult);
        }

        
        /// <returns></returns>
        public Collection<PowerShell> CreateDisconnectedPowerShells()
        {
            return _internalPool.CreateDisconnectedPowerShells(this);
        }

        
        /// <returns>RunspacePoolCapability.</returns>
        public RunspacePoolCapability GetCapabilities()
        {
            return _internalPool.GetCapabilities();
        }

        #endregion

        #region Public API

        
        /// <param name="maxRunspaces">
        /// The maximum number of runspaces in the pool.
        /// </param>
        /// <returns>
        /// true if the change is successful; otherwise, false.
        /// </returns>
        /// <remarks>
        /// You cannot set the number of runspaces to a number smaller than
        /// the minimum runspaces.
        /// </remarks>
        public bool SetMaxRunspaces(int maxRunspaces)
        {
            return _internalPool.SetMaxRunspaces(maxRunspaces);
        }

        
        /// <returns>
        /// The maximum number of runspaces in the pool
        /// </returns>
        public int GetMaxRunspaces()
        {
            return _internalPool.GetMaxRunspaces();
        }

        
        /// <param name="minRunspaces">
        /// The minimum number of runspaces in the pool.
        /// </param>
        /// <returns>
        /// true if the change is successful; otherwise, false.
        /// </returns>
        /// <remarks>
        /// You cannot set the number of idle runspaces to a number smaller than
        /// 1 or greater than maximum number of active runspaces.
        /// </remarks>
        public bool SetMinRunspaces(int minRunspaces)
        {
            return _internalPool.SetMinRunspaces(minRunspaces);
        }

        
        /// <returns>
        /// The minimum number of runspaces in the pool
        /// </returns>
        public int GetMinRunspaces()
        {
            return _internalPool.GetMinRunspaces();
        }

        
        /// <returns>
        /// The number of available runspace in the pool.
        /// </returns>
        public int GetAvailableRunspaces()
        {
            return _internalPool.GetAvailableRunspaces();
        }

        
        /// <exception cref="InvalidRunspacePoolStateException">
        /// RunspacePoolState is not BeforeOpen
        /// </exception>
        public void Open()
        {
            _internalPool.Open();
        }

        
        /// <param name="callback">
        /// A AsyncCallback to call once the BeginOpen completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// An AsyncResult object to monitor the state of the async
        /// operation.
        /// </returns>
        public IAsyncResult BeginOpen(AsyncCallback callback, object state)
        {
            return _internalPool.BeginOpen(callback, state);
        }

        
        /// <exception cref="ArgumentNullException">
        /// asyncResult is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// asyncResult object was not created by calling BeginOpen
        /// on this runspacepool instance.
        /// </exception>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// RunspacePoolState is not BeforeOpen.
        /// </exception>
        /// <remarks>
        /// TODO: Behavior if EndOpen is called multiple times.
        /// </remarks>
        public void EndOpen(IAsyncResult asyncResult)
        {
            _internalPool.EndOpen(asyncResult);
        }

        
        /// <exception cref="InvalidRunspacePoolStateException">
        /// Cannot close the RunspacePool because RunspacePool is
        /// in Closing state.
        /// </exception>
        public void Close()
        {
            _internalPool.Close();
        }

        
        /// <param name="callback">
        /// A AsyncCallback to call once the BeginClose completes.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// An AsyncResult object to monitor the state of the async
        /// operation.
        /// </returns>
        public IAsyncResult BeginClose(AsyncCallback callback, object state)
        {
            return _internalPool.BeginClose(callback, state);
        }

        
        /// <exception cref="ArgumentNullException">
        /// asyncResult is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// asyncResult object was not created by calling BeginClose
        /// on this runspacepool instance.
        /// </exception>
        public void EndClose(IAsyncResult asyncResult)
        {
            _internalPool.EndClose(asyncResult);
        }

        
        public void Dispose()
        {
            _internalPool.Dispose();
        }

        
        public PSPrimitiveDictionary GetApplicationPrivateData()
        {
            return _internalPool.GetApplicationPrivateData();
        }

        #endregion

        #region Internal API

        
        /// <remarks>
        /// Any updates to the value of this property must be done before the RunspacePool is opened
        /// </remarks>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// An attempt to change this property was made after opening the RunspacePool
        /// </exception>
        public PSThreadOptions ThreadOptions
        {
            get
            {
                return _internalPool.ThreadOptions;
            }

            set
            {
                if (this.RunspacePoolStateInfo.State != RunspacePoolState.BeforeOpen)
                {
                    throw new InvalidRunspacePoolStateException(RunspacePoolStrings.ChangePropertyAfterOpen);
                }

                _internalPool.ThreadOptions = value;
            }
        }

        
        /// <remarks>
        /// Any updates to the value of this property must be done before the RunspacePool is opened
        /// </remarks>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// An attempt to change this property was made after opening the RunspacePool
        /// </exception>
        public ApartmentState ApartmentState
        {
            get
            {
                return _internalPool.ApartmentState;
            }

            set
            {
                if (this.RunspacePoolStateInfo.State != RunspacePoolState.BeforeOpen)
                {
                    throw new InvalidRunspacePoolStateException(RunspacePoolStrings.ChangePropertyAfterOpen);
                }

                _internalPool.ApartmentState = value;
            }
        }

        
        /// <param name="callback">
        /// A AsyncCallback to call once the runspace is available.
        /// </param>
        /// <param name="state">
        /// A user supplied state to call the <paramref name="callback"/>
        /// with.
        /// </param>
        /// <returns>
        /// An IAsyncResult object to track the status of the Async operation.
        /// </returns>
        internal IAsyncResult BeginGetRunspace(
            AsyncCallback callback, object state)
        {
            return _internalPool.BeginGetRunspace(callback, state);
        }

        
        /// <param name="asyncResult">
        /// </param>
        internal void CancelGetRunspace(IAsyncResult asyncResult)
        {
            _internalPool.CancelGetRunspace(asyncResult);
        }

        
        /// <param name="asyncResult">
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// asyncResult is a null reference.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// asyncResult object was not created by calling BeginGetRunspace
        /// on this runspacepool instance.
        /// </exception>
        /// <exception cref="InvalidRunspacePoolStateException">
        /// RunspacePoolState is not BeforeOpen.
        /// </exception>
        internal Runspace EndGetRunspace(IAsyncResult asyncResult)
        {
            return _internalPool.EndGetRunspace(asyncResult);
        }

        
        /// <param name="runspace">
        /// Runspace to release to the pool.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="runspace"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Cannot release the runspace to this pool as the runspace
        /// doesn't belong to this pool.
        /// </exception>
        /// <exception cref="InvalidRunspaceStateException">
        /// Only opened runspaces can be released back to the pool.
        /// </exception>
        internal void ReleaseRunspace(Runspace runspace)
        {
            _internalPool.ReleaseRunspace(runspace);
        }

        
        internal bool IsRemote { get; } = false;

        
        internal RemoteRunspacePoolInternal RemoteRunspacePoolInternal
        {
            get
            {
                if (_internalPool is RemoteRunspacePoolInternal)
                {
                    return (RemoteRunspacePoolInternal)_internalPool;
                }
                else
                {
                    return null;
                }
            }
        }

        internal void AssertPoolIsOpen()
        {
            _internalPool.AssertPoolIsOpen();
        }

        #endregion
    }

    #endregion
}
