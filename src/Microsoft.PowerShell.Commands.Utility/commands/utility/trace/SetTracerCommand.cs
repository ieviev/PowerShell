// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Set, "TraceSource", DefaultParameterSetName = "optionsSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097129")]
    [OutputType(typeof(PSTraceSource))]
    public class SetTraceSourceCommand : TraceListenerCommandBase
    {
        #region Parameters

        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Name
        {
            get { return base.NameInternal; }

            set { base.NameInternal = value; }
        }

        
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "optionsSet")]
        public PSTraceSourceOptions Option
        {
            get
            {
                return base.OptionsInternal;
            }

            set
            {
                base.OptionsInternal = value;
            }
        }

        
        [Parameter(ParameterSetName = "optionsSet")]
        public TraceOptions ListenerOption
        {
            get
            {
                return base.ListenerOptionsInternal;
            }

            set
            {
                base.ListenerOptionsInternal = value;
            }
        }

        
        [Parameter(ParameterSetName = "optionsSet")]
        [Alias("PSPath", "Path")]
        public string FilePath
        {
            get { return base.FileListener; }

            set { base.FileListener = value; }
        }

        
        [Parameter(ParameterSetName = "optionsSet")]
        public SwitchParameter Force
        {
            get { return base.ForceWrite; }

            set { base.ForceWrite = value; }
        }

        
        [Parameter(ParameterSetName = "optionsSet")]
        public SwitchParameter Debugger
        {
            get { return base.DebuggerListener; }

            set { base.DebuggerListener = value; }
        }

        
        [Parameter(ParameterSetName = "optionsSet")]
        public SwitchParameter PSHost
        {
            get { return base.PSHostListener; }

            set { base.PSHostListener = value; }
        }

        
        [Parameter(ParameterSetName = "removeAllListenersSet")]
        [ValidateNotNullOrEmpty]
        public string[] RemoveListener { get; set; } = new string[] { "*" };

        
        [Parameter(ParameterSetName = "removeFileListenersSet")]
        [ValidateNotNullOrEmpty]
        public string[] RemoveFileListener { get; set; } = new string[] { "*" };

        
        [Parameter(ParameterSetName = "optionsSet")]
        public SwitchParameter PassThru
        {
            get { return _passThru; }

            set { _passThru = value; }
        }

        private bool _passThru;

        #endregion Parameters

        #region Cmdlet code

        
        protected override void ProcessRecord()
        {
            Collection<PSTraceSource> matchingSources = null;

            switch (ParameterSetName)
            {
                case "optionsSet":
                    Collection<PSTraceSource> preconfiguredTraceSources = null;
                    matchingSources = ConfigureTraceSource(Name, true, out preconfiguredTraceSources);

                    if (PassThru)
                    {
                        WriteObject(matchingSources, true);
                        WriteObject(preconfiguredTraceSources, true);
                    }

                    break;

                case "removeAllListenersSet":
                    matchingSources = GetMatchingTraceSource(Name, true);
                    RemoveListenersByName(matchingSources, RemoveListener, false);
                    break;

                case "removeFileListenersSet":
                    matchingSources = GetMatchingTraceSource(Name, true);
                    RemoveListenersByName(matchingSources, RemoveFileListener, true);
                    break;
            }
        }

        #endregion Cmdlet code
    }
}
