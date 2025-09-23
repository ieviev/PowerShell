// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Runspaces;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// By design, on the server side, each remote connection is represented by
    /// a ServerRemoteSession object, which contains one instance of this class.
    ///
    /// This class holds 4 pieces of information.
    /// 1. Client capability: This is the capability received during the negotiation process.
    /// 2. Server capability: This comes from default parameters.
    /// 3. Client configuration: This holds the remote session related configuration parameters that
    ///    the client sent to the server. This parameters can be changed and resent after the connection
    ///    is established.
    /// 4. Server configuration: this holds the server sider configuration parameters.
    ///
    /// All these together define the connection level parameters.
    /// </summary>
    internal class ServerRemoteSessionContext
    {
        #region Constructors

        /// <summary>
        /// The constructor instantiates a server capability object and a server configuration
        /// using default values.
        /// </summary>
        internal ServerRemoteSessionContext()
        {
        }

        #endregion Constructors

        /// <summary>
        /// This property represents the capability that the server receives from the client.
        /// </summary>
        internal RemoteSessionCapability ClientCapability { get; set; }

        /// <summary>
        /// This property is the server capability generated on the server side.
        /// </summary>
        internal RemoteSessionCapability ServerCapability { get; set; }

        /// <summary>
        /// True if negotiation from client is succeeded...in which case ClientCapability
        /// is the capability that server agreed with.
        /// </summary>
        internal bool IsNegotiationSucceeded { get; set; }
    }

    /// <summary>
    /// This class is designed to be the server side controller of a remote connection.
    ///
    /// It contains a static entry point that the PowerShell server process will get into
    /// the server mode. At this entry point, a runspace configuration is passed in. This runspace
    /// configuration is used to instantiate a server side runspace.
    ///
    /// This class controls a remote connection by using a Session data structure handler, which
    /// in turn contains a Finite State Machine, and a transport mechanism.
    /// </summary>
    internal class ServerRemoteSession : RemoteSession
    {
        [TraceSource("ServerRemoteSession", "ServerRemoteSession")]
        private static readonly PSTraceSource s_trace = PSTraceSource.GetTracer("ServerRemoteSession", "ServerRemoteSession");

        private readonly PSSenderInfo _senderInfo;
        private readonly string _configProviderId;
        private readonly string _initParameters;
        private string _initScriptForOutOfProcRS;
        private PSSessionConfiguration _sessionConfigProvider;

        // used to apply quotas on command and session transportmanagers.
        private int? _maxRecvdObjectSize;
        private int? _maxRecvdDataSizeCommand;

        private object _runspacePoolDriver;
        private readonly PSRemotingCryptoHelperServer _cryptoHelper;


      
        #region Events
        /// <summary>
        /// Raised when session is closed.
        /// </summary>
        internal EventHandler<RemoteSessionStateMachineEventArgs> Closed;
        #endregion

        #region Constructors

        /// <summary>
        /// This constructor instantiates a ServerRemoteSession object and
        /// a ServerRemoteSessionDataStructureHandler object.
        /// </summary>
        /// <param name="senderInfo">
        /// Details about the user creating this session.
        /// </param>
        /// <param name="configurationProviderId">
        /// The resource URI for which this session is being created
        /// </param>
        /// <param name="initializationParameters">
        /// Initialization Parameters xml passed by WSMan API. This data is read from the config
        /// xml.
        /// </param>
        /// <param name="transportManager">
        /// The transport manager this session should use to send/receive data
        /// </param>
        /// <returns></returns>
        internal ServerRemoteSession(PSSenderInfo senderInfo,
            string configurationProviderId,
            string initializationParameters,
            AbstractServerSessionTransportManager transportManager)
        {
            
        }

        #endregion Constructors

        #region Creation Factory

        /// <summary>
        /// Creates a server remote session for the supplied <paramref name="configurationProviderId"/>
        /// and <paramref name="transportManager"/>.
        /// </summary>
        /// <param name="senderInfo"></param>
        /// <param name="configurationProviderId"></param>
        /// <param name="initializationParameters">
        /// Initialization Parameters xml passed by WSMan API. This data is read from the config
        /// xml.
        /// </param>
        /// <param name="transportManager"></param>
        /// <param name="initialCommand">Optional initial command used for OutOfProc sessions.</param>
        /// <param name="configurationName">Optional configuration endpoint name for OutOfProc sessions.</param>
        /// <param name="configurationFile">Optional configuration file (.pssc) path for OutOfProc sessions.</param>
        /// <param name="initialLocation">Optional configuration initial location of the powershell session.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// InitialSessionState provider with <paramref name="configurationProviderId"/> does
        /// not exist on the remote server.
        /// </exception>
        
        internal static ServerRemoteSession CreateServerRemoteSession(
            PSSenderInfo senderInfo,
            string configurationProviderId,
            string initializationParameters,
            AbstractServerSessionTransportManager transportManager,
            string initialCommand,
            string configurationName,
            string configurationFile,
            string initialLocation)
        {
           

            return null;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// This indicates the remote session object is Client, Server or Listener.
        /// </summary>
        internal override RemotingDestination MySelf
        {
            get
            {
                return RemotingDestination.Server;
            }
        }

        /// <summary>
        /// This is the data dispatcher for the whole remote connection.
        /// This dispatcher is registered with the server side input queue's InputDataReady event.
        /// When the input queue has received data from client, it calls the InputDataReady listeners.
        ///
        /// This dispatcher distinguishes the negotiation packet as a special case. For all other data,
        /// it dispatches the data through Finite State Machines DoMessageReceived handler by raising the event
        /// MessageReceived. The FSM's DoMessageReceived handler further dispatches to the receiving
        /// components: such as runspace or pipeline which have their own data dispatching methods.
        /// </summary>
        /// <param name="sender">
        /// This parameter is not used by the method, in this implementation.
        /// </param>
        /// <param name="dataEventArg">
        /// This parameter contains the remote data received from client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="dataEventArg"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the parameter <paramref name="dataEventArg"/> does not contain remote data.
        /// </exception>
        /// <exception cref="PSRemotingDataStructureException">
        /// If the destination of the data is not for server.
        /// </exception>
        internal void DispatchInputQueueData(object sender, RemoteDataEventArgs dataEventArg)
        {}

        /// <summary>
        /// Have received a public key from the other side
        /// Import or take other action based on the state.
        /// </summary>
        /// <param name="sender">Sender of this event, unused.</param>
        /// <param name="eventArgs">event arguments which contains the
        /// remote public key</param>
        private void HandlePublicKeyReceived(object sender, RemoteDataEventArgs<string> eventArgs)
        {
            if (SessionDataStructureHandler.StateMachine.State == RemoteSessionState.Established ||
                SessionDataStructureHandler.StateMachine.State == RemoteSessionState.EstablishedAndKeyRequested || // this is only for legacy clients
                SessionDataStructureHandler.StateMachine.State == RemoteSessionState.EstablishedAndKeyExchanged)
            {
                string remotePublicKey = eventArgs.Data;

                bool ret = _cryptoHelper.ImportRemotePublicKey(remotePublicKey);

                RemoteSessionStateMachineEventArgs args = null;
                if (!ret)
                {
                    // importing remote public key failed
                    // set state to closed
                    args = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyReceiveFailed);

                    SessionDataStructureHandler.StateMachine.RaiseEvent(args);
                }

                args = new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyReceived);
                SessionDataStructureHandler.StateMachine.RaiseEvent(args);
            }
        }

        /// <summary>
        /// Start the key exchange process.
        /// </summary>
        internal override void StartKeyExchange()
        {
            if (SessionDataStructureHandler.StateMachine.State == RemoteSessionState.Established)
            {
                // send using data structure handler
                SessionDataStructureHandler.SendRequestForPublicKey();

                RemoteSessionStateMachineEventArgs eventArgs =
                    new RemoteSessionStateMachineEventArgs(RemoteSessionEvent.KeyRequested);
                SessionDataStructureHandler.StateMachine.RaiseEvent(eventArgs);
            }
        }

        /// <summary>
        /// Complete the Key exchange process.
        /// </summary>
        internal override void CompleteKeyExchange()
        {
            _cryptoHelper.CompleteKeyExchange();
        }

        /// <summary>
        /// Send an encrypted session key to the client.
        /// </summary>
        internal void SendEncryptedSessionKey()
        {}

        #endregion Overrides

        #region Properties

        /// <summary>
        /// This property returns the ServerRemoteSessionContext object created inside
        /// this object's constructor.
        /// </summary>
        internal ServerRemoteSessionContext Context { get; }

        /// <summary>
        /// This property returns the ServerRemoteSessionDataStructureHandler object created inside
        /// this object's constructor.
        /// </summary>
        internal ServerRemoteSessionDataStructureHandler SessionDataStructureHandler { get; }

        #endregion

        #region Private/Internal Methods

        /// <summary>
        /// Let the session clear its resources.
        /// </summary>
        /// <param name="reasonForClose"></param>
        internal void Close(RemoteSessionStateMachineEventArgs reasonForClose)
        {}


        // pass on application private data when session is connected from new client
        internal void HandlePostConnect()
        {

        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="createRunspaceEventArg"></param>
        /// <exception cref="InvalidOperationException">
        /// 1. InitialSessionState cannot be null.
        /// 2. Non existent InitialSessionState provider for the shellID
        /// </exception>
        private void HandleCreateRunspacePool(object sender, RemoteDataEventArgs createRunspaceEventArg)
        {
            
        }

        /// <summary>
        /// This handler method runs the negotiation algorithm. It decides if the negotiation is successful,
        /// or fails.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="negotiationEventArg">
        /// This parameter contains the client negotiation capability packet.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If the parameter <paramref name="negotiationEventArg"/> is null.
        /// </exception>
        private void HandleNegotiationReceived(object sender, RemoteSessionNegotiationEventArgs negotiationEventArg)
        {
            
        }

        /// <summary>
        /// Handle session closing event to close runspace pool drivers this session is hosting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleSessionDSHandlerClosing(object sender, EventArgs eventArgs)
        {
            
        }

        /// <summary>
        /// This handles closing of any resource used by this session.
        /// Resources used are RunspacePoolDriver, TransportManager.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleResourceClosing(object sender, EventArgs args)
        {
         
        }

        /// <summary>
        /// This is the server side remote session capability negotiation algorithm.
        /// </summary>
        /// <param name="clientCapability">
        /// This is the client capability that the server received from client.
        /// </param>
        /// <param name="onConnect">
        /// If the negotiation is on a connect (and not create)
        /// </param>
        /// <returns>
        /// The method returns true if the capability negotiation is successful.
        /// Otherwise, it returns false.
        /// </returns>
        /// <exception cref="PSRemotingDataStructureException">
        /// 1. PowerShell server does not support the PSVersion {1} negotiated by the client.
        ///    Make sure the client is compatible with the build {2} of PowerShell.
        /// 2. PowerShell server does not support the SerializationVersion {1} negotiated by the client.
        ///    Make sure the client is compatible with the build {2} of PowerShell.
        /// </exception>
        private bool RunServerNegotiationAlgorithm(RemoteSessionCapability clientCapability, bool onConnect)
        {
            
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="clientRunspacePoolId"></param>
        /// <returns></returns>
        internal object GetRunspacePoolDriver(Guid clientRunspacePoolId)
        {
       
            return null;
        }

        /// <summary>
        /// Used by Command Session to apply quotas on the command transport manager.
        /// This method is here because ServerRemoteSession knows about InitialSessionState.
        /// </summary>
        /// <param name="cmdTransportManager">
        /// Command TransportManager to apply the quota on.
        /// </param>
        internal void ApplyQuotaOnCommandTransportManager(AbstractServerTransportManager cmdTransportManager)
        {}

        #endregion
    }
}
