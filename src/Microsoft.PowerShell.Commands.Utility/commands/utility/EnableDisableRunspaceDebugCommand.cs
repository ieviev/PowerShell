// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    #region PSRunspaceDebug class

    
    public sealed class PSRunspaceDebug
    {
        #region Properties

        
        public bool Enabled { get; }

        
        public bool BreakAll { get; }

        
        public string RunspaceName { get; }

        
        public int RunspaceId { get; }

        #endregion

        #region Constructors

        
        public PSRunspaceDebug(bool enabled, bool breakAll, string runspaceName, int runspaceId)
        {
            if (string.IsNullOrEmpty(runspaceName))
            {
                throw new PSArgumentNullException(nameof(runspaceName));
            }

            this.Enabled = enabled;
            this.BreakAll = breakAll;
            this.RunspaceName = runspaceName;
            this.RunspaceId = runspaceId;
        }

        #endregion
    }

    #endregion

    #region CommonRunspaceCommandBase class

    
    public abstract class CommonRunspaceCommandBase : PSCmdlet
    {
        #region Strings

        
        protected const string RunspaceParameterSet = "RunspaceParameterSet";

        
        protected const string RunspaceNameParameterSet = "RunspaceNameParameterSet";

        
        protected const string RunspaceIdParameterSet = "RunspaceIdParameterSet";

        
        protected const string RunspaceInstanceIdParameterSet = "RunspaceInstanceIdParameterSet";

        
        protected const string ProcessNameParameterSet = "ProcessNameParameterSet";

        #endregion

        #region Parameters

        
        [Parameter(Position = 0,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] RunspaceName
        {
            get;
            set;
        }

        
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ValueFromPipeline = true,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Runspace[] Runspace
        {
            get;
            set;
        }

        
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] RunspaceId
        {
            get;
            set;
        }
        
        [Parameter(Position = 0,
                   Mandatory = true,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public System.Guid[] RunspaceInstanceId
        {
            get;
            set;
        }

        
        [Parameter(Position = 0, ParameterSetName = CommonRunspaceCommandBase.ProcessNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public string ProcessName
        {
            get;
            set;
        }

        
        [Parameter(Position = 1, ParameterSetName = CommonRunspaceCommandBase.ProcessNameParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member",
            Target = "Microsoft.PowerShell.Commands.CommonRunspaceCommandBase.#AppDomainName")]
        public string[] AppDomainName
        {
            get;
            set;
        }

        #endregion

        #region Protected Methods

        
        protected IReadOnlyList<Runspace> GetRunspaces()
        {
            IReadOnlyList<Runspace> results = null;

            if ((ParameterSetName == CommonRunspaceCommandBase.RunspaceNameParameterSet) && ((RunspaceName == null) || RunspaceName.Length == 0))
            {
                results = GetRunspaceUtils.GetAllRunspaces();
            }
            else
            {
                switch (ParameterSetName)
                {
                    case CommonRunspaceCommandBase.RunspaceNameParameterSet:
                        results = GetRunspaceUtils.GetRunspacesByName(RunspaceName);
                        break;

                    case CommonRunspaceCommandBase.RunspaceIdParameterSet:
                        results = GetRunspaceUtils.GetRunspacesById(RunspaceId);
                        break;

                    case CommonRunspaceCommandBase.RunspaceParameterSet:
                        results = new ReadOnlyCollection<Runspace>(new List<Runspace>(Runspace));
                        break;

                    case CommonRunspaceCommandBase.RunspaceInstanceIdParameterSet:
                        results = GetRunspaceUtils.GetRunspacesByInstanceId(RunspaceInstanceId);
                        break;
                }
            }

            return results;
        }

        
        protected System.Management.Automation.Debugger GetDebuggerFromRunspace(Runspace runspace)
        {
            System.Management.Automation.Debugger debugger = null;
            try
            {
                debugger = runspace.Debugger;
            }
            catch (PSInvalidOperationException) { }

            if (debugger == null)
            {
                WriteError(
                    new ErrorRecord(
                        new PSInvalidOperationException(string.Format(CultureInfo.InvariantCulture, Debugger.RunspaceOptionNoDebugger, runspace.Name)),
                        "RunspaceDebugOptionNoDebugger",
                        ErrorCategory.InvalidOperation,
                        this)
                    );
            }

            return debugger;
        }

        
        protected void SetDebugPreferenceHelper(string processName, string[] appDomainName, bool enable, string fullyQualifiedErrorId)
        {
            List<string> appDomainNames = null;
            if (appDomainName != null)
            {
                foreach (string currentAppDomainName in appDomainName)
                {
                    if (!string.IsNullOrEmpty(currentAppDomainName))
                    {
                        appDomainNames ??= new List<string>();

                        appDomainNames.Add(currentAppDomainName.ToLowerInvariant());
                    }
                }
            }

            try
            {
                System.Management.Automation.Runspaces.LocalRunspace.SetDebugPreference(processName.ToLowerInvariant(), appDomainNames, enable);
            }
            catch (Exception ex)
            {
                ErrorRecord errorRecord = new(
                new PSInvalidOperationException(string.Format(CultureInfo.InvariantCulture, Debugger.PersistDebugPreferenceFailure, processName), ex),
                fullyQualifiedErrorId,
                ErrorCategory.InvalidOperation,
                this);
                WriteError(errorRecord);
            }
        }

        #endregion
    }

    #endregion

    #region EnableRunspaceDebugCommand Cmdlet

    
    [Cmdlet(VerbsLifecycle.Enable, "RunspaceDebug", DefaultParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096831")]
    public sealed class EnableRunspaceDebugCommand : CommonRunspaceCommandBase
    {
        #region Parameters

        
        [Parameter(Position = 1,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceParameterSet)]
        [Parameter(Position = 1,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet)]
        [Parameter(Position = 1,
                   ParameterSetName = CommonRunspaceCommandBase.RunspaceIdParameterSet)]
        public SwitchParameter BreakAll
        {
            get;
            set;
        }

        #endregion

        #region Overrides

        
        protected override void ProcessRecord()
        {
            if (this.ParameterSetName.Equals(CommonRunspaceCommandBase.ProcessNameParameterSet))
            {
                SetDebugPreferenceHelper(ProcessName, AppDomainName, true, "EnableRunspaceDebugCommandPersistDebugPreferenceFailure");
                return;
            }

            IReadOnlyList<Runspace> results = GetRunspaces();

            foreach (var runspace in results)
            {
                if (runspace.RunspaceStateInfo.State != RunspaceState.Opened)
                {
                    WriteError(
                        new ErrorRecord(new PSInvalidOperationException(string.Format(CultureInfo.InvariantCulture, Debugger.RunspaceOptionInvalidRunspaceState, runspace.Name)),
                        "SetRunspaceDebugOptionCommandInvalidRunspaceState",
                        ErrorCategory.InvalidOperation,
                        this));

                    continue;
                }

                System.Management.Automation.Debugger debugger = GetDebuggerFromRunspace(runspace);
                if (debugger == null)
                {
                    continue;
                }

                // Enable debugging by preserving debug stop events.
                debugger.UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Wait;

                if (this.MyInvocation.BoundParameters.ContainsKey(nameof(BreakAll)))
                {
                    if (BreakAll)
                    {
                        try
                        {
                            debugger.SetDebuggerStepMode(true);
                        }
                        catch (PSInvalidOperationException e)
                        {
                            WriteError(
                                new ErrorRecord(
                                e,
                                "SetRunspaceDebugOptionCommandCannotEnableDebuggerStepping",
                                ErrorCategory.InvalidOperation,
                                this));
                        }
                    }
                    else
                    {
                        debugger.SetDebuggerStepMode(false);
                    }
                }
            }
        }

        #endregion
    }

    #endregion

    #region DisableRunspaceDebugCommand Cmdlet

    
    [Cmdlet(VerbsLifecycle.Disable, "RunspaceDebug", DefaultParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096924")]
    public sealed class DisableRunspaceDebugCommand : CommonRunspaceCommandBase
    {
        #region Overrides

        
        protected override void ProcessRecord()
        {
            if (this.ParameterSetName.Equals(CommonRunspaceCommandBase.ProcessNameParameterSet))
            {
                SetDebugPreferenceHelper(ProcessName.ToLowerInvariant(), AppDomainName, false, "DisableRunspaceDebugCommandPersistDebugPreferenceFailure");
            }
            else
            {
                IReadOnlyList<Runspace> results = GetRunspaces();

                foreach (var runspace in results)
                {
                    if (runspace.RunspaceStateInfo.State != RunspaceState.Opened)
                    {
                        WriteError(
                            new ErrorRecord(
                                new PSInvalidOperationException(string.Format(CultureInfo.InvariantCulture, Debugger.RunspaceOptionInvalidRunspaceState, runspace.Name)),
                                "SetRunspaceDebugOptionCommandInvalidRunspaceState",
                                ErrorCategory.InvalidOperation,
                                this)
                            );

                        continue;
                    }

                    System.Management.Automation.Debugger debugger = GetDebuggerFromRunspace(runspace);
                    if (debugger == null)
                    {
                        continue;
                    }

                    debugger.SetDebuggerStepMode(false);
                    debugger.UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Ignore;
                }
            }
        }

        #endregion
    }

    #endregion

    #region GetRunspaceDebugCommand Cmdlet

    
    [Cmdlet(VerbsCommon.Get, "RunspaceDebug", DefaultParameterSetName = CommonRunspaceCommandBase.RunspaceNameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097015")]
    [OutputType(typeof(PSRunspaceDebug))]
    public sealed class GetRunspaceDebugCommand : CommonRunspaceCommandBase
    {
        #region Overrides

        
        protected override void ProcessRecord()
        {
            IReadOnlyList<Runspace> results = GetRunspaces();

            foreach (var runspace in results)
            {
                System.Management.Automation.Debugger debugger = GetDebuggerFromRunspace(runspace);
                if (debugger != null)
                {
                    WriteObject(
                        new PSRunspaceDebug((debugger.UnhandledBreakpointMode == UnhandledBreakpointProcessingMode.Wait),
                            debugger.IsDebuggerSteppingEnabled,
                            runspace.Name,
                            runspace.Id)
                        );
                }
            }
        }

        #endregion
    }

    #endregion

    #region WaitDebuggerCommand Cmdlet

    
    [Cmdlet(VerbsLifecycle.Wait, "Debugger",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097035")]
    public sealed class WaitDebuggerCommand : PSCmdlet
    {
        #region Overrides

        
        protected override void EndProcessing()
        {
            Runspace currentRunspace = this.Context.CurrentRunspace;

            if (currentRunspace != null && currentRunspace.Debugger != null)
            {
                WriteVerbose(string.Format(CultureInfo.InvariantCulture, Debugger.DebugBreakMessage, MyInvocation.ScriptLineNumber, MyInvocation.ScriptName));

                currentRunspace.Debugger.Break();
            }
        }

        #endregion
    }

    #endregion
}
