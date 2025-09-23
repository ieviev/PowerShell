// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet("Out", "Null", SupportsShouldProcess = false,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096792", RemotingCapability = RemotingCapability.None)]
    public class OutNullCommand : PSCmdlet
    {
        
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; } = AutomationNull.Value;

        
        protected override void ProcessRecord()
        {
            // explicitely overridden:
            // do not do any processing
        }
    }

    
    [Cmdlet(VerbsData.Out, "Default", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096486", RemotingCapability = RemotingCapability.None)]
    public class OutDefaultCommand : FrontEndCommandBase
    {
        
        [Parameter]
        public SwitchParameter Transcript { get; set; }

        
        public OutDefaultCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        
        protected override void BeginProcessing()
        {
            var lineOutput = new ConsoleLineOutput(Host, false, new TerminatingErrorContext(this));

            ((OutputManagerInner)this.implementation).LineOutput = lineOutput;

            if (this.CommandRuntime is MshCommandRuntime mrt)
            {
                mrt.MergeUnclaimedPreviousErrorResults = true;
            }

            if (Transcript)
            {
                _transcribeOnlyCookie = Host.UI.SetTranscribeOnly();
            }

            // This needs to be done directly through the command runtime, as Out-Default
            // doesn't actually write pipeline objects.
            base.BeginProcessing();

            if (Context.CurrentCommandProcessor.CommandRuntime.OutVarList != null)
            {
                _outVarResults = new List<PSObject>();
            }
        }

        
        protected override void ProcessRecord()
        {
            if (Transcript)
            {
                WriteObject(InputObject);
            }

            // This needs to be done directly through the command runtime, as Out-Default
            // doesn't actually write pipeline objects.
            if (_outVarResults != null)
            {
                object inputObjectBase = PSObject.Base(InputObject);

                // Ignore errors and formatting records, as those can't be captured
                if (inputObjectBase != null &&
                    inputObjectBase is not ErrorRecord &&
                    !inputObjectBase.GetType().FullName.StartsWith(
                        "Microsoft.PowerShell.Commands.Internal.Format", StringComparison.OrdinalIgnoreCase))
                {
                    _outVarResults.Add(InputObject);
                }
            }

            base.ProcessRecord();
        }

        
        protected override void EndProcessing()
        {
            // This needs to be done directly through the command runtime, as Out-Default
            // doesn't actually write pipeline objects.
            if ((_outVarResults != null) && (_outVarResults.Count > 0))
            {
                Context.CurrentCommandProcessor.CommandRuntime.OutVarList.Clear();
                foreach (object item in _outVarResults)
                {
                    Context.CurrentCommandProcessor.CommandRuntime.OutVarList.Add(item);
                }

                _outVarResults = null;
            }

            base.EndProcessing();
        }

        
        protected override void InternalDispose()
        {
            try
            {
                base.InternalDispose();
            }
            finally
            {
                if (_transcribeOnlyCookie != null)
                {
                    _transcribeOnlyCookie.Dispose();
                    _transcribeOnlyCookie = null;
                }
            }
        }

        private List<PSObject> _outVarResults = null;
        private IDisposable _transcribeOnlyCookie = null;
    }

    
    [Cmdlet(VerbsData.Out, "Host", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096863", RemotingCapability = RemotingCapability.None)]
    public class OutHostCommand : FrontEndCommandBase
    {
        #region Command Line Parameters

        
        private bool _paging;

        #endregion

        
        public OutHostCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        
        [Parameter]
        public SwitchParameter Paging
        {
            get { return _paging; }

            set { _paging = value; }
        }

        
        protected override void BeginProcessing()
        {
            var lineOutput = new ConsoleLineOutput(Host, _paging, new TerminatingErrorContext(this));

            ((OutputManagerInner)this.implementation).LineOutput = lineOutput;
            base.BeginProcessing();
        }
    }
}
