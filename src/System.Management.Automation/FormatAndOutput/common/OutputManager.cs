// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    
    internal sealed class OutputManagerInner : ImplementationCommandBase
    {
        #region tracer
        [TraceSource("format_out_OutputManagerInner", "OutputManagerInner")]
        internal static readonly PSTraceSource tracer = PSTraceSource.GetTracer("format_out_OutputManagerInner", "OutputManagerInner");
        #endregion tracer

        #region LineOutput
        internal LineOutput LineOutput
        {
            set
            {
                lock (_syncRoot)
                {
                    _lo = value;

                    if (_isStopped)
                    {
                        _lo.StopProcessing();
                    }
                }
            }
        }

        private LineOutput _lo = null;
        #endregion

        
        internal override void ProcessRecord()
        {
            PSObject so = this.ReadObject();

            if (so == null || so == AutomationNull.Value)
            {
                return;
            }

            // on demand initialization when the first pipeline
            // object is initialized
            if (_mgr == null)
            {
                _mgr = new SubPipelineManager();
                _mgr.Initialize(_lo, this.OuterCmdlet().Context);
            }

#if false
            // if the object supports IEnumerable,
            // unpack the object and process each member separately
            IEnumerable e = PSObjectHelper.GetEnumerable (so);

            if (e == null)
            {
                this.mgr.Process (so);
            }
            else
            {
                foreach (object obj in e)
                {
                    this.mgr.Process (PSObjectHelper.AsPSObject (obj));
                }
            }
#else
            _mgr.Process(so);
#endif
        }

        
        internal override void EndProcessing()
        {
            // shut down only if we ever processed a pipeline object
            _mgr?.ShutDown();
        }

        internal override void StopProcessing()
        {
            lock (_syncRoot)
            {
                _lo?.StopProcessing();
                _isStopped = true;
            }
        }

        
        protected override void InternalDispose()
        {
            base.InternalDispose();
            if (_mgr != null)
            {
                _mgr.Dispose();
                _mgr = null;
            }
        }

        
        private SubPipelineManager _mgr = null;

        
        private bool _isStopped = false;

        
        private readonly object _syncRoot = new object();
    }

    
    internal sealed class SubPipelineManager : IDisposable
    {
        
        private sealed class CommandEntry : IDisposable
        {
            
            internal CommandWrapper command = new CommandWrapper();

            
            /// <param name="typeName">ETS type name of the object to process.</param>
            /// <returns>True if there is a match.</returns>
            internal bool AppliesToType(string typeName)
            {
                foreach (string s in _applicableTypes)
                {
                    if (string.Equals(s, typeName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }

            
            public void Dispose()
            {
                if (this.command == null)
                    return;

                this.command.Dispose();
                this.command = null;
            }

            
            private readonly StringCollection _applicableTypes = new StringCollection();
        }

        
        /// <param name="lineOutput">LineOutput to pass to the child pipelines.</param>
        /// <param name="context">ExecutionContext to pass to the child pipelines.</param>
        internal void Initialize(LineOutput lineOutput, ExecutionContext context)
        {
            _lo = lineOutput;
            InitializeCommandsHardWired(context);
        }

        
        /// <param name="context">ExecutionContext to pass to the child pipeline.</param>
        private void InitializeCommandsHardWired(ExecutionContext context)
        {
            // set the default handler
            RegisterCommandDefault(context, "out-lineoutput", typeof(OutLineOutputCommand));
            
        }

        
        /// <param name="context">ExecutionContext to pass to the child pipeline.</param>
        /// <param name="commandName">Name of the command to execute.</param>
        /// <param name="commandType">Type of the command to execute.</param>
        private void RegisterCommandDefault(ExecutionContext context, string commandName, Type commandType)
        {
            CommandEntry ce = new CommandEntry();

            ce.command.Initialize(context, commandName, commandType);
            ce.command.AddNamedParameter("LineOutput", _lo);
            _defaultCommandEntry = ce;
        }

        
        /// <param name="so">Pipeline object to process.</param>
        internal void Process(PSObject so)
        {
            // select which pipeline should handle the object
            CommandEntry ce = this.GetActiveCommandEntry(so);

            Diagnostics.Assert(ce != null, "CommandEntry ce must not be null");

            // delegate the processing
            ce.command.Process(so);
        }

        
        internal void ShutDown()
        {
            // we assume that command entries are never null
            foreach (CommandEntry ce in _commandEntryList)
            {
                Diagnostics.Assert(ce != null, "ce != null");
                ce.command.ShutDown();
                ce.command = null;
            }

            // we assume we always have a default command entry
            Diagnostics.Assert(_defaultCommandEntry != null, "defaultCommandEntry != null");
            _defaultCommandEntry.command.ShutDown();
            _defaultCommandEntry.command = null;
        }

        public void Dispose()
        {
            // we assume that command entries are never null
            foreach (CommandEntry ce in _commandEntryList)
            {
                Diagnostics.Assert(ce != null, "ce != null");
                ce.Dispose();
            }

            // we assume we always have a default command entry
            Diagnostics.Assert(_defaultCommandEntry != null, "defaultCommandEntry != null");
            _defaultCommandEntry.Dispose();
        }

        
        /// <param name="so">Pipeline object to be processed.</param>
        /// <returns>Applicable command entry.</returns>
        private CommandEntry GetActiveCommandEntry(PSObject so)
        {
            string typeName = PSObjectHelper.PSObjectIsOfExactType(so.InternalTypeNames);
            foreach (CommandEntry ce in _commandEntryList)
            {
                if (ce.AppliesToType(typeName))
                    return ce;
            }

            // failed any match: return the default handler
            return _defaultCommandEntry;
        }

        private LineOutput _lo = null;

        
        private readonly List<CommandEntry> _commandEntryList = new List<CommandEntry>();

        
        private CommandEntry _defaultCommandEntry = new CommandEntry();
    }
}
