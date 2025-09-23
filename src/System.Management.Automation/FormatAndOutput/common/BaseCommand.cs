// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    
    internal sealed class TerminatingErrorContext
    {
        internal TerminatingErrorContext(PSCmdlet command)
        {
            if (command == null)
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            _command = command;
        }

        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        internal void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            _command.ThrowTerminatingError(errorRecord);
        }

        private readonly PSCmdlet _command;
    }

    
    internal sealed class CommandWrapper : IDisposable
    {
        
        internal void Initialize(ExecutionContext execContext, string nameOfCommand, Type typeOfCommand)
        {
            _context = execContext;
            _commandName = nameOfCommand;
            _commandType = typeOfCommand;
        }

        
        internal void AddNamedParameter(string parameterName, object parameterValue)
        {
            _commandParameterList.Add(
                CommandParameterInternal.CreateParameterWithArgument(
                    null, parameterName, null,
                    null, parameterValue,
                    false));
        }

        
        internal Array Process(object o)
        {
            if (_pp == null)
            {
                // if this is the first call, we need to initialize the
                // pipeline underneath
                DelayedInternalInitialize();
            }

            return _pp.Step(o);
        }

        
        internal Array ShutDown()
        {
            if (_pp == null)
            {
                // if Process() never got called, no sub pipeline
                // ever got created, hence we just return an empty array
                return Array.Empty<object>();
            }

            PipelineProcessor ppTemp = _pp;

            _pp = null;
            return ppTemp.SynchronousExecuteEnumerate(AutomationNull.Value);
        }

        private void DelayedInternalInitialize()
        {
            _pp = new PipelineProcessor();

            CmdletInfo cmdletInfo = new CmdletInfo(_commandName, _commandType, null, null, _context);

            CommandProcessor cp = new CommandProcessor(cmdletInfo, _context);

            foreach (CommandParameterInternal par in _commandParameterList)
            {
                cp.AddParameter(par);
            }

            _pp.Add(cp);
        }

        
        public void Dispose()
        {
            if (_pp == null)
                return;

            _pp.Dispose();
            _pp = null;
        }

        private PipelineProcessor _pp = null;

        private string _commandName = null;
        private Type _commandType;
        private readonly List<CommandParameterInternal> _commandParameterList = new List<CommandParameterInternal>();

        private ExecutionContext _context = null;
    }

    
    public abstract class FrontEndCommandBase : PSCmdlet, IDisposable
    {
        #region Command Line Switches
        
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; } = AutomationNull.Value;

        #endregion

        
        protected override void BeginProcessing()
        {
            Diagnostics.Assert(this.implementation != null, "this.implementation is null");
            this.implementation.OuterCmdletCall = new ImplementationCommandBase.OuterCmdletCallback(this.OuterCmdletCall);
            this.implementation.InputObjectCall = new ImplementationCommandBase.InputObjectCallback(this.InputObjectCall);
            this.implementation.WriteObjectCall = new ImplementationCommandBase.WriteObjectCallback(this.WriteObjectCall);

            this.implementation.CreateTerminatingErrorContext();

            implementation.BeginProcessing();
        }

        
        protected override void ProcessRecord()
        {
            implementation.ProcessRecord();
        }

        
        protected override void EndProcessing()
        {
            implementation.EndProcessing();
        }

        
        protected override void StopProcessing()
        {
            implementation.StopProcessing();
        }

        
        protected virtual PSCmdlet OuterCmdletCall()
        {
            return this;
        }

        
        protected virtual PSObject InputObjectCall()
        {
            // just bind to the input object parameter
            return this.InputObject;
        }

        
        protected virtual void WriteObjectCall(object value)
        {
            // just call Monad API
            this.WriteObject(value);
        }

        
        internal ImplementationCommandBase implementation = null;

        #region IDisposable Implementation

        
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                InternalDispose();
            }
        }

        
        protected virtual void InternalDispose()
        {
            if (this.implementation == null)
                return;

            this.implementation.Dispose();
            this.implementation = null;
        }
        #endregion
    }

    
    internal class ImplementationCommandBase : IDisposable
    {
        
        internal virtual void BeginProcessing()
        {
        }

        
        internal virtual void ProcessRecord()
        {
        }

        
        internal virtual void EndProcessing()
        {
        }

        
        internal virtual void StopProcessing()
        {
        }

        
        internal virtual PSObject ReadObject()
        {
            // delegate to the front end object
            System.Diagnostics.Debug.Assert(this.InputObjectCall != null, "this.InputObjectCall is null");
            return this.InputObjectCall();
        }

        
        internal virtual void WriteObject(object o)
        {
            // delegate to the front end object
            System.Diagnostics.Debug.Assert(this.WriteObjectCall != null, "this.WriteObjectCall is null");
            this.WriteObjectCall(o);
        }

        // callback methods to get to the outer Monad Cmdlet
        
        internal virtual PSCmdlet OuterCmdlet()
        {
            // delegate to the front end object
            System.Diagnostics.Debug.Assert(this.OuterCmdletCall != null, "this.OuterCmdletCall is null");
            return this.OuterCmdletCall();
        }

        protected TerminatingErrorContext TerminatingErrorContext { get; private set; }

        internal void CreateTerminatingErrorContext()
        {
            TerminatingErrorContext = new TerminatingErrorContext(this.OuterCmdlet());
        }

        
        internal delegate PSCmdlet OuterCmdletCallback();

        
        internal OuterCmdletCallback OuterCmdletCall;

        // callback to the methods to get an object and write an object
        
        internal delegate PSObject InputObjectCallback();

        
        internal delegate void WriteObjectCallback(object o);

        
        internal InputObjectCallback InputObjectCall;

        
        internal WriteObjectCallback WriteObjectCall;

        #region IDisposable Implementation

        
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                InternalDispose();
            }
        }

        
        protected virtual void InternalDispose()
        {
        }
        #endregion

    }
}
