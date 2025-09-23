// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Host;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Runspaces.Internal;
using System.Management.Automation.Tracing;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal
{
    
    internal sealed class ClientRunspacePoolDataStructureHandler : IDisposable
    {
        private bool _reconnecting = false;

        #region Constructors

        
        /// <param name="clientRunspacePool">Client runspace pool object.</param>
        /// <param name="typeTable">Typetable to use for serialization/deserialization.</param>
        internal ClientRunspacePoolDataStructureHandler(RemoteRunspacePoolInternal clientRunspacePool,
            TypeTable typeTable)
        {
            _clientRunspacePoolId = clientRunspacePool.InstanceId;
            _minRunspaces = clientRunspacePool.GetMinRunspaces();
            _maxRunspaces = clientRunspacePool.GetMaxRunspaces();
            _host = clientRunspacePool.Host;
            _applicationArguments = clientRunspacePool.ApplicationArguments;
            RemoteSession = CreateClientRemoteSession(clientRunspacePool);
            // TODO: Assign remote session name.. should be passed from clientRunspacePool
            _transportManager = RemoteSession.SessionDataStructureHandler.TransportManager;
            _transportManager.TypeTable = typeTable;
            RemoteSession.StateChanged += HandleClientRemoteSessionStateChanged;
            _reconnecting = false;

            _transportManager.RobustConnectionNotification += HandleRobustConnectionNotification;
            _transportManager.CreateCompleted += HandleSessionCreateCompleted;
        }

        #endregion Constructors

        #region Data Structure Handler Methods

        
        internal void CreateRunspacePoolAndOpenAsync()
        {
            // #1: Connect to remote session
            Dbg.Assert(RemoteSession.SessionDataStructureHandler.StateMachine.State == RemoteSessionState.Idle,
                "State of ClientRemoteSession is expected to be idle before connection is established");
            RemoteSession.CreateAsync();

            // #2: send the message for runspace pool creation
            // this is done in HandleClientRemoteSessionStateChanged
        }

        
        internal void CloseRunspacePoolAsync()
        {
            RemoteSession.CloseAsync();
        }

        
        internal void DisconnectPoolAsync()
        {
            // Prepare running commands for disconnect and start disconnect
            // when ready.
            PrepareForAndStartDisconnect();
        }

        
        internal void ReconnectPoolAsync()
        {
            // TODO: Integrate this into state machine
            _reconnecting = true;
            PrepareForConnect();
            RemoteSession.ReconnectAsync();
        }

        
        internal void ConnectPoolAsync()
        {
            PrepareForConnect();
            RemoteSession.ConnectAsync();
        }

        
        /// <param name="receivedData">Data received.</param>
        internal void ProcessReceivedData(RemoteDataObject<PSObject> receivedData)
        {
            // verify if this data structure handler is the intended recipient
            if (receivedData.RunspacePoolId != _clientRunspacePoolId)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.RunspaceIdsDoNotMatch,
                                receivedData.RunspacePoolId, _clientRunspacePoolId);
            }

            // take appropriate action based on the action type
            Dbg.Assert(receivedData.TargetInterface == RemotingTargetInterface.RunspacePool,
                "Target interface is expected to be RunspacePool");

            switch (receivedData.DataType)
            {
                case RemotingDataType.RemoteHostCallUsingRunspaceHost:
                    {
                        Dbg.Assert(RemoteHostCallReceived != null,
                            "RemoteRunspacePoolInternal should subscribe to all data structure handler events");

                        RemoteHostCall remoteHostCall = RemoteHostCall.Decode(receivedData.Data);
                        RemoteHostCallReceived.SafeInvoke(this, new RemoteDataEventArgs<RemoteHostCall>(remoteHostCall));
                    }

                    break;

                case RemotingDataType.RunspacePoolInitData:
                    {
                        RunspacePoolInitInfo initInfo = RemotingDecoder.GetRunspacePoolInitInfo(receivedData.Data);

                        Dbg.Assert(RSPoolInitInfoReceived != null,
                            "RemoteRunspacePoolInternal should subscribe to all data structure handler events");
                        RSPoolInitInfoReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<RunspacePoolInitInfo>(initInfo));
                    }

                    break;

                case RemotingDataType.RunspacePoolStateInfo:
                    {
                        RunspacePoolStateInfo stateInfo =
                            RemotingDecoder.GetRunspacePoolStateInfo(receivedData.Data);

                        Dbg.Assert(StateInfoReceived != null,
                            "RemoteRunspacePoolInternal should subscribe to all data structure handler events");
                        StateInfoReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<RunspacePoolStateInfo>(stateInfo));

                        NotifyAssociatedPowerShells(stateInfo);
                    }

                    break;

                case RemotingDataType.ApplicationPrivateData:
                    {
                        PSPrimitiveDictionary applicationPrivateData = RemotingDecoder.GetApplicationPrivateData(receivedData.Data);
                        Dbg.Assert(ApplicationPrivateDataReceived != null,
                            "RemoteRunspacePoolInternal should subscribe to all data structure handler events");
                        ApplicationPrivateDataReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<PSPrimitiveDictionary>(applicationPrivateData));
                    }

                    break;

                case RemotingDataType.RunspacePoolOperationResponse:
                    {
                        Dbg.Assert(SetMaxMinRunspacesResponseReceived != null,
                            "RemoteRunspacePoolInternal should subscribe to all data structure handler events");

                        SetMaxMinRunspacesResponseReceived.SafeInvoke(this, new RemoteDataEventArgs<PSObject>(receivedData.Data));
                    }

                    break;

                case RemotingDataType.PSEventArgs:
                    {
                        PSEventArgs psEventArgs = RemotingDecoder.GetPSEventArgs(receivedData.Data);

                        Dbg.Assert(PSEventArgsReceived != null,
                            "RemoteRunspacePoolInternal should subscribe to all data structure handler events");

                        PSEventArgsReceived.SafeInvoke(this, new RemoteDataEventArgs<PSEventArgs>(psEventArgs));
                    }

                    break;
            }
        }

        
        /// <param name="shell">Associated powershell.</param>
        /// <returns>PowerShell data structure handler object.</returns>
        internal ClientPowerShellDataStructureHandler CreatePowerShellDataStructureHandler(
            ClientRemotePowerShell shell)
        {
            BaseClientCommandTransportManager clientTransportMgr =
                RemoteSession.SessionDataStructureHandler.CreateClientCommandTransportManager(shell, shell.NoInput);

            return new ClientPowerShellDataStructureHandler(
                clientTransportMgr, _clientRunspacePoolId, shell.InstanceId);
        }

        
        /// <param name="shell">The client remote powershell.</param>
        internal void CreatePowerShellOnServerAndInvoke(ClientRemotePowerShell shell)
        {
            // add to associated powershell list and send request to server
            lock (_associationSyncObject)
            {
                _associatedPowerShellDSHandlers.Add(shell.InstanceId, shell.DataStructureHandler);
            }

            shell.DataStructureHandler.RemoveAssociation += HandleRemoveAssociation;

            // Find out if this is an invoke and disconnect operation and if so whether the endpoint
            // supports disconnect.  Throw exception if disconnect is not supported.
            bool invokeAndDisconnect = shell.Settings != null && shell.Settings.InvokeAndDisconnect;
            if (invokeAndDisconnect && !EndpointSupportsDisconnect)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.EndpointDoesNotSupportDisconnect);
            }

            if (RemoteSession == null)
            {
                throw new ObjectDisposedException("ClientRunspacePoolDataStructureHandler");
            }

            shell.DataStructureHandler.Start(RemoteSession.SessionDataStructureHandler.StateMachine, invokeAndDisconnect);
        }

        
        /// <param name="psShellInstanceId">PowerShell Instance Id.</param>
        /// <param name="psDSHandler">ClientPowerShellDataStructureHandler for PowerShell.</param>
        internal void AddRemotePowerShellDSHandler(Guid psShellInstanceId, ClientPowerShellDataStructureHandler psDSHandler)
        {
            lock (_associationSyncObject)
            {
                // Remove old DSHandler and replace with new.
                _associatedPowerShellDSHandlers[psShellInstanceId] = psDSHandler;
            }

            psDSHandler.RemoveAssociation += HandleRemoveAssociation;
        }

        
        /// <param name="rcvdData">Message received.</param>
        internal void DispatchMessageToPowerShell(RemoteDataObject<PSObject> rcvdData)
        {
            ClientPowerShellDataStructureHandler dsHandler =
                GetAssociatedPowerShellDataStructureHandler(rcvdData.PowerShellId);

            // if a data structure handler does not exist it means
            // the association has been removed -
            // discard messages
            dsHandler?.ProcessReceivedData(rcvdData);
        }

        
        /// <param name="hostResponse">Host response object to send.</param>
        internal void SendHostResponseToServer(RemoteHostResponse hostResponse)
        {
            SendDataAsync(hostResponse.Encode(), DataPriorityType.PromptResponse);
        }

        
        /// <param name="callId">Caller Id.</param>
        internal void SendResetRunspaceStateToServer(long callId)
        {
            RemoteDataObject message =
                RemotingEncoder.GenerateResetRunspaceState(_clientRunspacePoolId, callId);

            SendDataAsync(message);
        }

        
        /// <param name="maxRunspaces">New maxrunspaces to set.</param>
        /// <param name="callId">call id on which the calling method will
        /// be blocked on</param>
        internal void SendSetMaxRunspacesToServer(int maxRunspaces, long callId)
        {
            RemoteDataObject message =
                RemotingEncoder.GenerateSetMaxRunspaces(_clientRunspacePoolId, maxRunspaces, callId);

            SendDataAsync(message);
        }

        
        /// <param name="minRunspaces">New minrunspaces to set.</param>
        /// <param name="callId">call id on which the calling method will
        /// be blocked on</param>
        internal void SendSetMinRunspacesToServer(int minRunspaces, long callId)
        {
            RemoteDataObject message =
                RemotingEncoder.GenerateSetMinRunspaces(_clientRunspacePoolId, minRunspaces, callId);

            SendDataAsync(message);
        }

        
        /// <param name="callId">call id on which the calling method will
        /// be blocked on</param>
        internal void SendGetAvailableRunspacesToServer(long callId)
        {
            SendDataAsync(RemotingEncoder.GenerateGetAvailableRunspaces(_clientRunspacePoolId, callId));
        }

        #endregion Data Structure Handler Methods

        #region Data Structure Handler events

        
        internal event EventHandler<RemoteDataEventArgs<RemoteHostCall>> RemoteHostCallReceived;

        
        internal event EventHandler<RemoteDataEventArgs<RunspacePoolStateInfo>> StateInfoReceived;

        
        internal event EventHandler<RemoteDataEventArgs<RunspacePoolInitInfo>> RSPoolInitInfoReceived;

        
        internal event EventHandler<RemoteDataEventArgs<PSPrimitiveDictionary>> ApplicationPrivateDataReceived;

        
        internal event EventHandler<RemoteDataEventArgs<PSEventArgs>> PSEventArgsReceived;

        
        internal event EventHandler<RemoteDataEventArgs<Exception>> SessionClosed;

        
        internal event EventHandler<RemoteDataEventArgs<Exception>> SessionDisconnected;

        
        internal event EventHandler<RemoteDataEventArgs<Exception>> SessionReconnected;

        
        internal event EventHandler<RemoteDataEventArgs<Exception>> SessionClosing;

        
        internal event EventHandler<RemoteDataEventArgs<PSObject>> SetMaxMinRunspacesResponseReceived;

        
        internal event EventHandler<RemoteDataEventArgs<Uri>> URIRedirectionReported;

        
        internal event EventHandler<RemoteDataEventArgs<Exception>> SessionRCDisconnecting;

        
        internal event EventHandler<CreateCompleteEventArgs> SessionCreateCompleted;

        #endregion Data Structure Handler events

        #region Private Methods

        
        /// <param name="data">Data to send.</param>
        /// <remarks>This overload takes a RemoteDataObject and should be
        /// the one used within the code</remarks>
        private void SendDataAsync(RemoteDataObject data)
        {
            _transportManager.DataToBeSentCollection.Add<object>(data);
        }

        
        /// <typeparam name="T"></typeparam>
        /// <param name="data">Data to be sent to server.</param>
        /// <param name="priority">Priority with which to send data.</param>
        internal void SendDataAsync<T>(RemoteDataObject<T> data, DataPriorityType priority)
        {
            _transportManager.DataToBeSentCollection.Add<T>(data, priority);
        }

        
        /// <param name="data">Data object to send.</param>
        /// <param name="priority">Priority with which to send data.</param>
        internal void SendDataAsync(PSObject data, DataPriorityType priority)
        {
            RemoteDataObject<PSObject> dataToBeSent = RemoteDataObject<PSObject>.CreateFrom(RemotingDestination.Server,
                RemotingDataType.InvalidDataType, _clientRunspacePoolId, Guid.Empty, data);

            _transportManager.DataToBeSentCollection.Add<PSObject>(dataToBeSent);
        }

        
        /// <param name="rsPoolInternal">
        /// The RunspacePool object this session should map to.
        /// </param>
        private ClientRemoteSessionImpl CreateClientRemoteSession(
                    RemoteRunspacePoolInternal rsPoolInternal)
        {
            ClientRemoteSession.URIDirectionReported uriRedirectionHandler =
                new ClientRemoteSession.URIDirectionReported(HandleURIDirectionReported);
            return new ClientRemoteSessionImpl(rsPoolInternal,
                                               uriRedirectionHandler);
        }

        
        /// <param name="sender">Sender of this event.</param>
        /// <param name="e">Object describing this event.</param>
        private void HandleClientRemoteSessionStateChanged(
                        object sender, RemoteSessionStateEventArgs e)
        {
            // send create runspace request while sending negotiation packet. This will
            // save 1 network call to create a runspace on the server.
            if (e.SessionStateInfo.State == RemoteSessionState.NegotiationSending)
            {
                if (_createRunspaceCalled)
                {
                    return;
                }

                lock (_syncObject)
                {
                    // We are doing this check because Established event
                    // is raised more than once
                    if (_createRunspaceCalled)
                    {
                        // TODO: Put an assert here. NegotiationSending cannot
                        // occur multiple time in v2 remoting.
                        return;
                    }

                    _createRunspaceCalled = true;
                }

                // make client's PSVersionTable available to the server using applicationArguments
                PSPrimitiveDictionary argumentsWithVersionTable =
                    PSPrimitiveDictionary.CloneAndAddPSVersionTable(_applicationArguments);
                // send a message to the server..
                SendDataAsync(RemotingEncoder.GenerateCreateRunspacePool(
                    _clientRunspacePoolId, _minRunspaces, _maxRunspaces, RemoteSession.RemoteRunspacePoolInternal, _host,
                    argumentsWithVersionTable));
            }

            if (e.SessionStateInfo.State == RemoteSessionState.NegotiationSendingOnConnect)
            {
                // send connect message to the server.
                SendDataAsync(RemotingEncoder.GenerateConnectRunspacePool(
                    _clientRunspacePoolId, _minRunspaces, _maxRunspaces));
            }
            else if (e.SessionStateInfo.State == RemoteSessionState.ClosingConnection)
            {
                // use the first reason which caused the error
                Exception reason = _closingReason;
                if (reason == null)
                {
                    reason = e.SessionStateInfo.Reason;
                    _closingReason = reason;
                }

                // close transport managers of the associated commands
                List<ClientPowerShellDataStructureHandler> dsHandlers;
                lock (_associationSyncObject)
                {
                    dsHandlers = new List<ClientPowerShellDataStructureHandler>(_associatedPowerShellDSHandlers.Values);
                }

                foreach (ClientPowerShellDataStructureHandler dsHandler in dsHandlers)
                {
                    dsHandler.CloseConnectionAsync(_closingReason);
                }

                SessionClosing.SafeInvoke(this, new RemoteDataEventArgs<Exception>(reason));
            }
            else if (e.SessionStateInfo.State == RemoteSessionState.Closed)
            {
                // use the first reason which caused the error
                Exception reason = _closingReason;
                if (reason == null)
                {
                    reason = e.SessionStateInfo.Reason;
                    _closingReason = reason;
                }

                // if there is a reason associated, then most likely the
                // runspace pool has broken, so notify accordingly
                if (reason != null)
                {
                    NotifyAssociatedPowerShells(new RunspacePoolStateInfo(RunspacePoolState.Broken, reason));
                }
                else
                {
                    // notify the associated powershells that this
                    // runspace pool has closed
                    NotifyAssociatedPowerShells(new RunspacePoolStateInfo(RunspacePoolState.Closed, reason));
                }

                SessionClosed.SafeInvoke(this, new RemoteDataEventArgs<Exception>(reason));
            }
            else if (e.SessionStateInfo.State == RemoteSessionState.Connected)
            {
                // write a transfer event here
                
            }
            else if (e.SessionStateInfo.State == RemoteSessionState.Disconnected)
            {
                NotifyAssociatedPowerShells(new RunspacePoolStateInfo(
                    RunspacePoolState.Disconnected,
                    e.SessionStateInfo.Reason));
                SessionDisconnected.SafeInvoke(this, new RemoteDataEventArgs<Exception>(e.SessionStateInfo.Reason));
            }
            else if (_reconnecting && e.SessionStateInfo.State == RemoteSessionState.Established)
            {
                SessionReconnected.SafeInvoke(this, new RemoteDataEventArgs<Exception>(null));
                _reconnecting = false;
            }
            else if (e.SessionStateInfo.State == RemoteSessionState.RCDisconnecting)
            {
                SessionRCDisconnecting.SafeInvoke(this, new RemoteDataEventArgs<Exception>(null));
            }
            else
            {
                if (e.SessionStateInfo.Reason != null)
                {
                    _closingReason = e.SessionStateInfo.Reason;
                }
            }
        }

        
        /// <param name="newURI"></param>
        private void HandleURIDirectionReported(Uri newURI)
        {
            URIRedirectionReported.SafeInvoke(this, new RemoteDataEventArgs<Uri>(newURI));
        }

        
        /// <param name="stateInfo">state information that need to
        /// be notified</param>
        private void NotifyAssociatedPowerShells(RunspacePoolStateInfo stateInfo)
        {
            List<ClientPowerShellDataStructureHandler> dsHandlers;

            if (stateInfo.State == RunspacePoolState.Disconnected)
            {
                lock (_associationSyncObject)
                {
                    dsHandlers = new List<ClientPowerShellDataStructureHandler>(_associatedPowerShellDSHandlers.Values);
                }

                foreach (ClientPowerShellDataStructureHandler dsHandler in dsHandlers)
                {
                    dsHandler.ProcessDisconnect(stateInfo);
                }

                return;
            }

            // if the runspace pool is broken or closed then set all
            // associated powershells to stopped
            if (stateInfo.State == RunspacePoolState.Broken || stateInfo.State == RunspacePoolState.Closed)
            {
                lock (_associationSyncObject)
                {
                    dsHandlers = new List<ClientPowerShellDataStructureHandler>(_associatedPowerShellDSHandlers.Values);
                    _associatedPowerShellDSHandlers.Clear();
                }

                if (stateInfo.State == RunspacePoolState.Broken)
                {
                    // set the state to failed, outside the lock
                    foreach (ClientPowerShellDataStructureHandler dsHandler in dsHandlers)
                    {
                        dsHandler.SetStateToFailed(stateInfo.Reason);
                    }
                }
                else if (stateInfo.State == RunspacePoolState.Closed)
                {
                    foreach (ClientPowerShellDataStructureHandler dsHandler in dsHandlers)
                    {
                        dsHandler.SetStateToStopped(stateInfo.Reason);
                    }
                }

                return;
            }
        }

        
        /// <param name="clientPowerShellId">Id of the client remote powershell.</param>
        /// <returns>ClientPowerShellDataStructureHandler object.</returns>
        private ClientPowerShellDataStructureHandler GetAssociatedPowerShellDataStructureHandler
            (Guid clientPowerShellId)
        {
            ClientPowerShellDataStructureHandler dsHandler = null;

            lock (_associationSyncObject)
            {
                bool success = _associatedPowerShellDSHandlers.TryGetValue(clientPowerShellId, out dsHandler);

                if (!success)
                {
                    dsHandler = null;
                }
            }

            return dsHandler;
        }

        
        /// <param name="sender">Sender of this event.</param>
        /// <param name="e">Unused.</param>
        private void HandleRemoveAssociation(object sender, EventArgs e)
        {
            Dbg.Assert(sender is ClientPowerShellDataStructureHandler, @"sender of the event
                must be ClientPowerShellDataStructureHandler");

            ClientPowerShellDataStructureHandler dsHandler =
                sender as ClientPowerShellDataStructureHandler;

            lock (_associationSyncObject)
            {
                _associatedPowerShellDSHandlers.Remove(dsHandler.PowerShellId);
            }

            _transportManager.RemoveCommandTransportManager(dsHandler.PowerShellId);
        }

        
        private void PrepareForAndStartDisconnect()
        {
            bool startDisconnectNow;

            lock (_associationSyncObject)
            {
                if (_associatedPowerShellDSHandlers.Count == 0)
                {
                    // There are no running commands associated with this runspace pool.
                    startDisconnectNow = true;
                    _preparingForDisconnectList = null;
                }
                else
                {
                    // Delay starting the disconnect operation until all running commands are prepared.
                    startDisconnectNow = false;

                    // Create and fill list of active transportmanager objects to be disconnected.
                    // Add ready-for-disconnect callback handler to DSHandler transportmanager objects.
                    Dbg.Assert(_preparingForDisconnectList == null, "Cannot prepare for disconnect while disconnect is pending.");
                    _preparingForDisconnectList = new List<BaseClientCommandTransportManager>();
                    foreach (ClientPowerShellDataStructureHandler dsHandler in _associatedPowerShellDSHandlers.Values)
                    {
                        _preparingForDisconnectList.Add(dsHandler.TransportManager);
                        dsHandler.TransportManager.ReadyForDisconnect += HandleReadyForDisconnect;
                    }
                }
            }

            if (startDisconnectNow)
            {
                // Ok to start on this thread.
                StartDisconnectAsync(RemoteSession);
            }
            else
            {
                // Start preparation for disconnect.  The HandleReadyForDisconnect callback will be
                // called when a transportManager is ready for disconnect.
                List<ClientPowerShellDataStructureHandler> dsHandlers;
                lock (_associationSyncObject)
                {
                    dsHandlers = new List<ClientPowerShellDataStructureHandler>(_associatedPowerShellDSHandlers.Values);
                }

                foreach (ClientPowerShellDataStructureHandler dsHandler in dsHandlers)
                {
                    dsHandler.TransportManager.PrepareForDisconnect();
                }
            }
        }

        
        private void PrepareForConnect()
        {
            List<ClientPowerShellDataStructureHandler> dsHandlers;
            lock (_associationSyncObject)
            {
                dsHandlers = new List<ClientPowerShellDataStructureHandler>(_associatedPowerShellDSHandlers.Values);
            }

            foreach (ClientPowerShellDataStructureHandler dsHandler in dsHandlers)
            {
                dsHandler.TransportManager.ReadyForDisconnect -= HandleReadyForDisconnect;
                dsHandler.TransportManager.PrepareForConnect();
            }
        }

        
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleReadyForDisconnect(object sender, EventArgs args)
        {
            if (sender == null)
            {
                return;
            }

            BaseClientCommandTransportManager bcmdTM = (BaseClientCommandTransportManager)sender;

            lock (_associationSyncObject)
            {
                // Ignore extra event calls after disconnect is started.
                if (_preparingForDisconnectList == null)
                {
                    return;
                }

                _preparingForDisconnectList.Remove(bcmdTM);

                if (_preparingForDisconnectList.Count == 0)
                {
                    _preparingForDisconnectList = null;

                    // Start the asynchronous disconnect on a worker thread because we don't know
                    // what thread this callback is made from.  If it was made from a transport
                    // callback event then a deadlock may occur when DisconnectAsync is called on
                    // that same thread.
                    ThreadPool.QueueUserWorkItem(new WaitCallback(StartDisconnectAsync));
                }
            }
        }

        
        /// <param name="state"></param>
        private void StartDisconnectAsync(object state)
        {
            var remoteSession = RemoteSession;
            try
            {
                remoteSession?.DisconnectAsync();
            }
            catch
            {
                // remoteSession may have already been disposed resulting in unexpected exceptions.
            }
        }

        
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleRobustConnectionNotification(
            object sender,
            ConnectionStatusEventArgs e)
        {
            List<ClientPowerShellDataStructureHandler> dsHandlers;
            lock (_associationSyncObject)
            {
                dsHandlers = new List<ClientPowerShellDataStructureHandler>(_associatedPowerShellDSHandlers.Values);
            }

            foreach (ClientPowerShellDataStructureHandler dsHandler in dsHandlers)
            {
                dsHandler.ProcessRobustConnectionNotification(e);
            }
        }

        
        /// <param name="sender">Transport sender.</param>
        /// <param name="eventArgs">CreateCompleteEventArgs.</param>
        private void HandleSessionCreateCompleted(object sender, CreateCompleteEventArgs eventArgs)
        {
            SessionCreateCompleted.SafeInvoke<CreateCompleteEventArgs>(this, eventArgs);
        }

        #endregion Private Methods

        #region Private Members

        private readonly Guid _clientRunspacePoolId;
        private readonly object _syncObject = new object();
        private bool _createRunspaceCalled = false;
        private Exception _closingReason;
        private readonly int _minRunspaces;
        private readonly int _maxRunspaces;
        private readonly PSHost _host;
        private readonly PSPrimitiveDictionary _applicationArguments;

        private readonly Dictionary<Guid, ClientPowerShellDataStructureHandler> _associatedPowerShellDSHandlers
            = new Dictionary<Guid, ClientPowerShellDataStructureHandler>();

        // data structure handlers of all ClientRemotePowerShell which are
        // associated with this runspace pool
        private readonly object _associationSyncObject = new object();
        // object to synchronize operations to above
        private readonly BaseClientSessionTransportManager _transportManager;
        // session transport manager associated with this runspace

        private List<BaseClientCommandTransportManager> _preparingForDisconnectList;

        #endregion Private Members

        #region Internal Properties

        
        internal ClientRemoteSession RemoteSession { get; private set; }

        
        internal BaseClientSessionTransportManager TransportManager
        {
            get
            {
                if (RemoteSession != null)
                {
                    return RemoteSession.SessionDataStructureHandler.TransportManager;
                }
                else
                {
                    return null;
                }
            }
        }

        
        internal int MaxRetryConnectionTime
        {
            get
            {
                if (_transportManager != null &&
                    _transportManager is WSManClientSessionTransportManager)
                {
                    return ((WSManClientSessionTransportManager)(_transportManager)).MaxRetryConnectionTime;
                }

                return 0;
            }
        }

        
        internal bool EndpointSupportsDisconnect
        {
            get
            {
                WSManClientSessionTransportManager wsmanTransportManager = _transportManager as WSManClientSessionTransportManager;
                return wsmanTransportManager != null && wsmanTransportManager.SupportsDisconnect;
            }
        }

        #endregion Internal Properties

        #region IDisposable

        
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        
        /// <param name="disposing">If true, release all managed resources.</param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (RemoteSession != null)
                {
                    ((ClientRemoteSessionImpl)RemoteSession).Dispose();
                    RemoteSession = null;
                }
            }
        }

        #endregion IDisposable
    }

    
    internal sealed class ClientPowerShellDataStructureHandler
    {
        #region Data Structure Handler events

        
        internal event EventHandler RemoveAssociation;

        
        internal event EventHandler<RemoteDataEventArgs<PSInvocationStateInfo>> InvocationStateInfoReceived;

        
        internal event EventHandler<RemoteDataEventArgs<object>> OutputReceived;

        
        internal event EventHandler<RemoteDataEventArgs<ErrorRecord>> ErrorReceived;

        
        internal event EventHandler<RemoteDataEventArgs<InformationalMessage>> InformationalMessageReceived;

        
        internal event EventHandler<RemoteDataEventArgs<RemoteHostCall>> HostCallReceived;

        
        internal event EventHandler<RemoteDataEventArgs<Exception>> ClosedNotificationFromRunspacePool;

        
        /// <remarks>
        /// The eventhandler should make sure not to throw any exceptions.
        /// </remarks>
        internal event EventHandler<EventArgs> CloseCompleted;

        
        internal event EventHandler<RemoteDataEventArgs<Exception>> BrokenNotificationFromRunspacePool;

        
        internal event EventHandler<RemoteDataEventArgs<Exception>> ReconnectCompleted;

        
        internal event EventHandler<RemoteDataEventArgs<Exception>> ConnectCompleted;

        
        internal event EventHandler<ConnectionStatusEventArgs> RobustConnectionNotification;

        #endregion Data Structure Handler events

        #region Data Structure Handler Methods

        
        internal void Start(ClientRemoteSessionDSHandlerStateMachine stateMachine, bool inDisconnectMode)
        {
            // Add all callbacks to transport manager.
            SetupTransportManager(inDisconnectMode);
            TransportManager.CreateAsync();
        }

        private void HandleDelayStreamRequestProcessed(object sender, EventArgs e)
        {
            // client's request to start pipeline in disconnected mode has been successfully processed
            ProcessDisconnect(null);
        }

        internal void HandleReconnectCompleted(object sender, EventArgs args)
        {
            int currentState = Interlocked.CompareExchange(ref _connectionState, (int)connectionStates.Connected, (int)connectionStates.Reconnecting);

            ReconnectCompleted.SafeInvoke(this, new RemoteDataEventArgs<Exception>(null));
            return;
        }

        internal void HandleConnectCompleted(object sender, EventArgs args)
        {
            int currentState = Interlocked.CompareExchange(ref _connectionState, (int)connectionStates.Connected, (int)connectionStates.Connecting);

            ConnectCompleted.SafeInvoke(this, new RemoteDataEventArgs<Exception>(null));
            return;
        }

        
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void HandleTransportError(object sender, TransportErrorOccuredEventArgs e)
        {
            // notify associated powershell about the error and close transport manager
            PSInvocationStateInfo stateInfo = new PSInvocationStateInfo(PSInvocationState.Failed, e.Exception);
            InvocationStateInfoReceived.SafeInvoke(this, new RemoteDataEventArgs<PSInvocationStateInfo>(stateInfo));

            // The handler to InvocationStateInfoReceived would have already
            // closed the connection. No need to do it here again
        }

        
        internal void SendStopPowerShellMessage()
        {
            TransportManager.CryptoHelper.CompleteKeyExchange();
            TransportManager.SendStopSignal();
        }

        
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSignalCompleted(object sender, EventArgs e)
        {
            // Raise stopped event locally...By the time this event
            // is raised, the remote server would have sent state changed info.
            // A bad server may not send appropriate sate info, in which case we
            // fail safely
            PSRemotingDataStructureException exception = new PSRemotingDataStructureException(
                RemotingErrorIdStrings.PipelineStopped);
            InvocationStateInfoReceived.SafeInvoke(this,
                new RemoteDataEventArgs<PSInvocationStateInfo>(
                    new PSInvocationStateInfo(PSInvocationState.Stopped, exception)));
        }

        
        /// <param name="hostResponse">Host response to send.</param>
        internal void SendHostResponseToServer(RemoteHostResponse hostResponse)
        {
            RemoteDataObject<PSObject> dataToBeSent =
                RemoteDataObject<PSObject>.CreateFrom(RemotingDestination.Server,
                RemotingDataType.RemotePowerShellHostResponseData,
                _clientRunspacePoolId,
                _clientPowerShellId,
                hostResponse.Encode());

            TransportManager.DataToBeSentCollection.Add<PSObject>(dataToBeSent,
                DataPriorityType.PromptResponse);
        }

        
        /// <param name="inputstream"></param>
        internal void SendInput(ObjectStreamBase inputstream)
        {
            if (!inputstream.IsOpen && inputstream.Count == 0)
            {
                // there is no input, send an end of input
                // message
                lock (_inputSyncObject)
                {
                    // send input closed information to server
                    SendDataAsync(RemotingEncoder.GeneratePowerShellInputEnd(
                        _clientRunspacePoolId, _clientPowerShellId));
                }
            }
            else
            {
                // its possible that in client input data is written in a thread
                // other than the current thread. Since we want to write input
                // to the server in the order in which it was received, this
                // operation of writing to the server need to be synced
                // Also we need to ensure that all the data currently available
                // for enumeration are written out before any newly added data
                // is written. Hence the lock is made even before the handler is
                // registered
                lock (_inputSyncObject)
                {
                    inputstream.DataReady += HandleInputDataReady;
                    WriteInput(inputstream);
                }
            }
        }

        
        /// <param name="receivedData">Data received.</param>
        internal void ProcessReceivedData(RemoteDataObject<PSObject> receivedData)
        {
            // verify if this data structure handler is the intended recipient
            if (receivedData.PowerShellId != _clientPowerShellId)
            {
                throw new PSRemotingDataStructureException(RemotingErrorIdStrings.PipelineIdsDoNotMatch,
                                receivedData.PowerShellId, _clientPowerShellId);
            }

            // decode the message and take appropriate action
            Dbg.Assert(receivedData.TargetInterface == RemotingTargetInterface.PowerShell,
                "Target interface is expected to be Pipeline");

            switch (receivedData.DataType)
            {
                case RemotingDataType.PowerShellStateInfo:
                    {
                        PSInvocationStateInfo stateInfo =
                            RemotingDecoder.GetPowerShellStateInfo(receivedData.Data);

                        Dbg.Assert(InvocationStateInfoReceived != null,
                            "ClientRemotePowerShell should subscribe to all data structure handler events");
                        InvocationStateInfoReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<PSInvocationStateInfo>(stateInfo));
                    }

                    break;

                case RemotingDataType.PowerShellOutput:
                    {
                        object outputObject =
                            RemotingDecoder.GetPowerShellOutput(receivedData.Data);

                        // since it is possible that powershell can have
                        // strongly typed output, origin information will
                        // not be added in this case. If a remoting cmdlet
                        // is using PowerShell, then it should take care
                        // of adding the origin information
                        Dbg.Assert(OutputReceived != null,
                            "ClientRemotePowerShell should subscribe to all data structure handler events");
                        OutputReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<object>(outputObject));
                    }

                    break;

                case RemotingDataType.PowerShellErrorRecord:
                    {
                        ErrorRecord errorRecord =
                            RemotingDecoder.GetPowerShellError(receivedData.Data);

                        // since it is possible that powershell can have
                        // strongly typed output, origin information will
                        // not be added for output. Therefore, origin
                        // information will not be added for error records
                        // as well. If a remoting cmdlet
                        // is using PowerShell, then it should take care
                        // of adding the origin information
                        Dbg.Assert(ErrorReceived != null,
                                "ClientRemotePowerShell should subscribe to all data structure handler events");

                        ErrorReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<ErrorRecord>(errorRecord));
                    }

                    break;
                case RemotingDataType.PowerShellDebug:
                    {
                        DebugRecord record = RemotingDecoder.GetPowerShellDebug(receivedData.Data);

                        InformationalMessageReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<InformationalMessage>(
                                new InformationalMessage(record, RemotingDataType.PowerShellDebug)));
                    }

                    break;

                case RemotingDataType.PowerShellVerbose:
                    {
                        VerboseRecord record = RemotingDecoder.GetPowerShellVerbose(receivedData.Data);

                        InformationalMessageReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<InformationalMessage>(
                                new InformationalMessage(record, RemotingDataType.PowerShellVerbose)));
                    }

                    break;

                case RemotingDataType.PowerShellWarning:
                    {
                        WarningRecord record = RemotingDecoder.GetPowerShellWarning(receivedData.Data);

                        InformationalMessageReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<InformationalMessage>(
                                new InformationalMessage(record, RemotingDataType.PowerShellWarning)));
                    }

                    break;

                case RemotingDataType.PowerShellProgress:
                    {
                        ProgressRecord record = RemotingDecoder.GetPowerShellProgress(receivedData.Data);

                        InformationalMessageReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<InformationalMessage>(
                                new InformationalMessage(record, RemotingDataType.PowerShellProgress)));
                    }

                    break;

                case RemotingDataType.PowerShellInformationStream:
                    {
                        InformationRecord record = RemotingDecoder.GetPowerShellInformation(receivedData.Data);

                        InformationalMessageReceived.SafeInvoke(this,
                            new RemoteDataEventArgs<InformationalMessage>(
                                new InformationalMessage(record, RemotingDataType.PowerShellInformationStream)));
                    }

                    break;

                case RemotingDataType.RemoteHostCallUsingPowerShellHost:
                    {
                        RemoteHostCall remoteHostCall = RemoteHostCall.Decode(receivedData.Data);
                        HostCallReceived.SafeInvoke(this, new RemoteDataEventArgs<RemoteHostCall>(remoteHostCall));
                    }

                    break;

                default:
                    {
                        Dbg.Assert(false, "we should not be encountering this");
                    }

                    break;
            }
        }

        
        /// <param name="reason">reason why this state change
        /// should occur</param>
        /// <remarks>This method is called by the associated
        /// runspace pool data structure handler when the server runspace pool
        /// goes into a closed or broken state</remarks>
        internal void SetStateToFailed(Exception reason)
        {
            Dbg.Assert(BrokenNotificationFromRunspacePool != null,
                "ClientRemotePowerShell should subscribe to all data structure handler events");

            BrokenNotificationFromRunspacePool.SafeInvoke(this, new RemoteDataEventArgs<Exception>(reason));
        }

        
        /// <param name="reason">reason why the powershell has to be
        /// set to a stopped state.</param>
        internal void SetStateToStopped(Exception reason)
        {
            Dbg.Assert(ClosedNotificationFromRunspacePool != null,
                "ClientRemotePowerShell should subscribe to all data structure handler events");

            ClosedNotificationFromRunspacePool.SafeInvoke(this, new RemoteDataEventArgs<Exception>(reason));
        }

        
        internal void CloseConnectionAsync(Exception sessionCloseReason)
        {
            _sessionClosedReason = sessionCloseReason;

            // wait for the close to complete and then dispose the transport manager
            TransportManager.CloseCompleted += (object source, EventArgs args) =>
            {
                if (CloseCompleted != null)
                {
                    // If the provided event args are empty then call CloseCompleted with
                    // RemoteSessionStateEventArgs containing session closed reason exception.
                    EventArgs closeCompletedEventArgs = (args == EventArgs.Empty) ?
                        new RemoteSessionStateEventArgs(new RemoteSessionStateInfo(RemoteSessionState.Closed, _sessionClosedReason)) :
                        args;

                    CloseCompleted(this, closeCompletedEventArgs);
                }

                TransportManager.Dispose();
            };

            TransportManager.CloseAsync();
        }

        
        internal void RaiseRemoveAssociationEvent()
        {
            RemoveAssociation.SafeInvoke(this, EventArgs.Empty);
        }

        
        internal void ProcessDisconnect(RunspacePoolStateInfo rsStateInfo)
        {
            // disconnect may be called on a pipeline that is already disconnected.
            PSInvocationStateInfo stateInfo =
                            new PSInvocationStateInfo(PSInvocationState.Disconnected,
                                rsStateInfo?.Reason);

            Dbg.Assert(InvocationStateInfoReceived != null,
                "ClientRemotePowerShell should subscribe to all data structure handler events");
            InvocationStateInfoReceived.SafeInvoke(this,
                new RemoteDataEventArgs<PSInvocationStateInfo>(stateInfo));

            Interlocked.CompareExchange(ref _connectionState, (int)connectionStates.Disconnected, (int)connectionStates.Connected);
        }

        
        internal void ReconnectAsync()
        {
            int currentState = Interlocked.CompareExchange(ref _connectionState, (int)connectionStates.Reconnecting, (int)connectionStates.Disconnected);
            if ((currentState != (int)connectionStates.Disconnected))
            {
                Dbg.Assert(false, "Pipeline DS Handler is in unexpected connection state");

                // TODO: Raise appropriate exception
                return;
            }

            TransportManager.ReconnectAsync();
        }

        // Called from session DSHandler. Connects to a remote powershell instance.
        internal void ConnectAsync()
        {
            int currentState = Interlocked.CompareExchange(ref _connectionState, (int)connectionStates.Connecting, (int)connectionStates.Disconnected);

            // Connect is called for *reconstruct* connection case and so
            // we need to set up all transport manager callbacks.
            SetupTransportManager(false);
            TransportManager.ConnectAsync();
        }

        
        /// <param name="e"></param>
        internal void ProcessRobustConnectionNotification(
            ConnectionStatusEventArgs e)
        {
            // Raise event for PowerShell client.
            RobustConnectionNotification.SafeInvoke(this, e);
        }

        #endregion Data Structure Handler Methods

        #region Constructors

        
        /// <param name="clientRunspacePoolId">id of the client
        /// remote runspace pool associated with this data structure handler
        /// </param>
        /// <param name="clientPowerShellId">id of the client
        /// powershell associated with this data structure handler</param>
        /// <param name="transportManager">transport manager associated
        /// with this connection</param>
        internal ClientPowerShellDataStructureHandler(BaseClientCommandTransportManager transportManager,
                    Guid clientRunspacePoolId, Guid clientPowerShellId)
        {
            TransportManager = transportManager;
            _clientRunspacePoolId = clientRunspacePoolId;
            _clientPowerShellId = clientPowerShellId;
            transportManager.SignalCompleted += OnSignalCompleted;
        }

        #endregion Constructors

        #region Internal Methods

        
        internal Guid PowerShellId
        {
            get
            {
                return _clientPowerShellId;
            }
        }

        
        internal BaseClientCommandTransportManager TransportManager { get; }

        #endregion Internal Methods

        #region Private Methods

        
        /// <param name="data">Data to send.</param>
        /// <remarks>This overload takes a RemoteDataObject and should be
        /// the one used within the code</remarks>
        private void SendDataAsync(RemoteDataObject data)
        {
            RemoteDataObject<object> dataToBeSent = (RemoteDataObject<object>)data;
            TransportManager.DataToBeSentCollection.Add<object>(dataToBeSent);
        }

        
        /// <param name="sender">Sender of this event.</param>
        /// <param name="e">Information describing this event.</param>
        private void HandleInputDataReady(object sender, EventArgs e)
        {
            // make sure only one thread calls the WriteInput.
            lock (_inputSyncObject)
            {
                ObjectStreamBase inputstream = sender as ObjectStreamBase;
                WriteInput(inputstream);
            }
        }

        
        /// <remarks>This method doesn't lock and its the responsibility
        /// of the caller to actually do the locking</remarks>
        /// <param name="inputstream"></param>
        private void WriteInput(ObjectStreamBase inputstream)
        {
            Collection<object> inputObjects = inputstream.ObjectReader.NonBlockingRead(Int32.MaxValue);

            foreach (object inputObject in inputObjects)
            {
                SendDataAsync(RemotingEncoder.GeneratePowerShellInput(inputObject,
                    _clientRunspacePoolId, _clientPowerShellId));
            }

            if (!inputstream.IsOpen)
            {
                // Write any data written after the NonBlockingRead call above.
                inputObjects = inputstream.ObjectReader.NonBlockingRead(Int32.MaxValue);

                foreach (object inputObject in inputObjects)
                {
                    SendDataAsync(RemotingEncoder.GeneratePowerShellInput(inputObject,
                        _clientRunspacePoolId, _clientPowerShellId));
                }

                // we are sending input end to the server. Ignore the future
                // DataReady events (A DataReady event is raised while Closing
                // the stream as well)
                inputstream.DataReady -= HandleInputDataReady;
                // stream close: send end of input
                SendDataAsync(RemotingEncoder.GeneratePowerShellInputEnd(
                    _clientRunspacePoolId, _clientPowerShellId));
            }
        }

        
        /// <param name="inDisconnectMode">Boolean.</param>
        private void SetupTransportManager(bool inDisconnectMode)
        {
            TransportManager.WSManTransportErrorOccured += HandleTransportError;
            TransportManager.ReconnectCompleted += HandleReconnectCompleted;
            TransportManager.ConnectCompleted += HandleConnectCompleted;
            TransportManager.DelayStreamRequestProcessed += HandleDelayStreamRequestProcessed;
            TransportManager.startInDisconnectedMode = inDisconnectMode;
        }

        #endregion Private Methods

        #region Private Members

        private readonly Guid _clientRunspacePoolId;
        private readonly Guid _clientPowerShellId;

        // object for synchronizing input to be sent
        // to server powershell
        private readonly object _inputSyncObject = new object();

        private enum connectionStates
        {
            Connected = 1, Disconnected = 3, Reconnecting = 4, Connecting = 5
        }

        private int _connectionState = (int)connectionStates.Connected;

        // Contains the associated session closed reason exception if any,
        // otherwise is null.
        private Exception _sessionClosedReason;

        #endregion Private Members
    }

    internal sealed class InformationalMessage
    {
        internal object Message { get; }

        internal RemotingDataType DataType { get; }

        internal InformationalMessage(object message, RemotingDataType dataType)
        {
            DataType = dataType;
            Message = message;
        }
    }
}
