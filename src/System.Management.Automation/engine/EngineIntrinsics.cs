// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Host;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public class EngineIntrinsics
    {
        #region Constructors

        
        private EngineIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of ExecutionContext should be called.");
        }

        
        internal EngineIntrinsics(ExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _context = context;
            _host = context.EngineHostInterface;
        }

        #endregion Constructors

        #region Public methods

        
        public PSHost Host
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _host != null,
                    "The only constructor for this class should always set the host field");

                return _host;
            }
        }

        
        public PSEventManager Events
        {
            get
            {
                return _context.Events;
            }
        }

        
        public ProviderIntrinsics InvokeProvider
        {
            get
            {
                return _context.EngineSessionState.InvokeProvider;
            }
        }

        
        public SessionState SessionState
        {
            get
            {
                return _context.EngineSessionState.PublicSessionState;
            }
        }

        
        public CommandInvocationIntrinsics InvokeCommand
        {
            get { return _invokeCommand ??= new CommandInvocationIntrinsics(_context); }
        }

        #endregion Public methods

        #region private data

        private readonly ExecutionContext _context;
        private readonly PSHost _host;
        private CommandInvocationIntrinsics _invokeCommand;
        #endregion private data
    }
}
