// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Tracing;
using System.Threading;
#if !UNIX
using System.Security.Principal;
#endif

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting.Server
{
    internal abstract class OutOfProcessMediatorBase
    {
        #region Protected Data

        protected TextReader originalStdIn;

        protected static object SyncObject = new object();
        protected object _syncObject = new object();
        protected string _initialCommand;
        protected ManualResetEvent allcmdsClosedEvent;

#if !UNIX
        // Thread impersonation.
        protected WindowsIdentity _windowsIdentityToImpersonate;
#endif

        /// <summary>
        /// Count of commands in progress.
        /// </summary>
        protected int _inProgressCommandsCount = 0;

        protected PowerShellTraceSource tracer = PowerShellTraceSourceFactory.GetTraceSource();

        protected bool _exitProcessOnError;

        #endregion

        #region Constructor

        protected OutOfProcessMediatorBase(bool exitProcessOnError)
        {
            
        }

        #endregion

        #region Data Processing handlers

        protected void ProcessingThreadStart(object state)
        {
          
        }

        protected void OnDataPacketReceived(byte[] rawData, string stream, Guid psGuid)
        {
            
        }

        protected void OnDataAckPacketReceived(Guid psGuid)
        {
            
        }

        protected void OnCommandCreationPacketReceived(Guid psGuid)
        {
            
        }

        protected void OnCommandCreationAckReceived(Guid psGuid)
        {
            
        }

        protected void OnSignalPacketReceived(Guid psGuid)
        {
            
        }

        protected void OnSignalAckPacketReceived(Guid psGuid)
        {
            
        }

        protected void OnClosePacketReceived(Guid psGuid)
        {
            
        }

        protected void OnCloseAckPacketReceived(Guid psGuid)
        {
            
        }

        #endregion

        #region Methods



        protected void Start(
            string initialCommand,
            PSRemotingCryptoHelperServer cryptoHelper,
            string workingDirectory,
            string configurationName,
            string configurationFile)
        {
            
        }

        #endregion
    }

    internal sealed class StdIOProcessMediator : OutOfProcessMediatorBase
    {
        #region Private Data


        #endregion

        #region Constructors

        /// <summary>
        /// The mediator will take actions from the StdIn stream and responds to them.
        /// It will replace StdIn,StdOut and StdErr stream with TextWriter.Null. This is
        /// to make sure these streams are totally used by our Mediator.
        /// </summary>
        /// <param name="combineErrOutStream">Redirects remoting errors to the Out stream.</param>
        private StdIOProcessMediator(bool combineErrOutStream) : base(exitProcessOnError: true)
        {
            
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Starts the out-of-process powershell server instance.
        /// </summary>
        /// <param name="initialCommand">Specifies the initialization script.</param>
        /// <param name="workingDirectory">Specifies the initial working directory. The working directory is set before the initial command.</param>
        /// <param name="configurationName">Specifies an optional configuration name that configures the endpoint session.</param>
        /// <param name="configurationFile">Specifies an optional path to a configuration (.pssc) file for the session.</param>
        /// <param name="combineErrOutStream">Specifies the option to write remoting errors to stdOut stream, with special formatting.</param>
        internal static void Run(
            string initialCommand,
            string workingDirectory,
            string configurationName,
            string configurationFile,
            bool combineErrOutStream)
        {
            
        }

        #endregion
    }

    internal sealed class NamedPipeProcessMediator : OutOfProcessMediatorBase
    {
        #region Private Data


        private readonly RemoteSessionNamedPipeServer _namedPipeServer;

        #endregion

        #region Properties

        internal bool IsDisposed
        {
            get { return _namedPipeServer.IsDisposed; }
        }

        #endregion

        #region Constructors

        private NamedPipeProcessMediator() : base(false) { }

        private NamedPipeProcessMediator(
            RemoteSessionNamedPipeServer namedPipeServer) : base(false)
        {
            
        }

        #endregion

        #region Static Methods

        internal static void Run(
            string initialCommand,
            RemoteSessionNamedPipeServer namedPipeServer)
        {
            
        }

        #endregion
    }




}
