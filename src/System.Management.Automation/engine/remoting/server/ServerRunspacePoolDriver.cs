// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
#if !UNIX
using System.Security.Principal;
#endif
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Interface exposing driver single thread invoke enter/exit
    /// nested pipeline.
    /// </summary>
#nullable enable
    internal interface IRSPDriverInvoke
    {
        void EnterNestedPipeline();

        void ExitNestedPipeline();

        bool HandleStopSignal();
    }
#nullable restore

    /// <summary>
    /// This class wraps the script debugger for a object runspace.
    /// </summary>
    internal sealed class ServerRemoteDebugger : Debugger, IDisposable
    {
        #region Private Members

        private readonly IRSPDriverInvoke _driverInvoker;
        private readonly Runspace _runspace;
        private readonly ObjectRef<Debugger> _wrappedDebugger;
        private bool _inDebugMode;
        private DebuggerStopEventArgs _debuggerStopEventArgs;

        private ManualResetEventSlim _nestedDebugStopCompleteEvent;
        private bool _nestedDebugging;
        private ManualResetEventSlim _processCommandCompleteEvent;
        private ThreadCommandProcessing _threadCommandProcessing;

        private bool _raiseStopEventLocally;

        internal const string SetPSBreakCommandText = "Set-PSBreakpoint";

        #endregion

        #region Constructor

        private ServerRemoteDebugger() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="driverInvoker"></param>
        /// <param name="runspace"></param>
        /// <param name="debugger"></param>
        internal ServerRemoteDebugger(
            IRSPDriverInvoke driverInvoker,
            Runspace runspace,
            Debugger debugger)
        {
            if (driverInvoker == null)
            {
                throw new PSArgumentNullException(nameof(driverInvoker));
            }

            if (runspace == null)
            {
                throw new PSArgumentNullException(nameof(runspace));
            }

            if (debugger == null)
            {
                throw new PSArgumentNullException(nameof(debugger));
            }

            _driverInvoker = driverInvoker;
            _runspace = runspace;

            _wrappedDebugger = new ObjectRef<Debugger>(debugger);

            SetDebuggerCallbacks();

            _runspace.Name = "RemoteHost";
            _runspace.InternalDebugger = this;
        }

        #endregion

        #region Debugger overrides

        /// <summary>
        /// True when debugger is stopped at a breakpoint.
        /// </summary>
        public override bool InBreakpoint
        {
            get { return _inDebugMode; }
        }

        /// <summary>
        /// Adds the provided set of breakpoints to the debugger.
        /// </summary>
        /// <param name="breakpoints">List of breakpoints.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        public override void SetBreakpoints(IEnumerable<Breakpoint> breakpoints, int? runspaceId) =>
            _wrappedDebugger.Value.SetBreakpoints(breakpoints, runspaceId);

        /// <summary>
        /// Get a breakpoint by id, primarily for Enable/Disable/Remove-PSBreakpoint cmdlets.
        /// </summary>
        /// <param name="id">Id of the breakpoint you want.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The breakpoint with the specified id.</returns>
        public override Breakpoint GetBreakpoint(int id, int? runspaceId) =>
            _wrappedDebugger.Value.GetBreakpoint(id, runspaceId);

        /// <summary>
        /// Returns breakpoints on a runspace.
        /// </summary>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>A list of breakpoints in a runspace.</returns>
        public override List<Breakpoint> GetBreakpoints(int? runspaceId) =>
            _wrappedDebugger.Value.GetBreakpoints(runspaceId);

        /// <summary>
        /// Sets a command breakpoint in the debugger.
        /// </summary>
        /// <param name="command">The name of the command that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the command is invoked.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The command breakpoint that was set.</returns>
        public override CommandBreakpoint SetCommandBreakpoint(string command, ScriptBlock action, string path, int? runspaceId) =>
            _wrappedDebugger.Value.SetCommandBreakpoint(command, action, path, runspaceId);

        /// <summary>
        /// Sets a line breakpoint in the debugger.
        /// </summary>
        /// <param name="path">The path to the script file where the breakpoint may be hit. This value may not be null.</param>
        /// <param name="line">The line in the script file where the breakpoint may be hit. This value must be greater than or equal to 1.</param>
        /// <param name="column">The column in the script file where the breakpoint may be hit. If 0, the breakpoint will trigger on any statement on the line.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The line breakpoint that was set.</returns>
        public override LineBreakpoint SetLineBreakpoint(string path, int line, int column, ScriptBlock action, int? runspaceId) =>
            _wrappedDebugger.Value.SetLineBreakpoint(path, line, column, action, runspaceId);

        /// <summary>
        /// Sets a variable breakpoint in the debugger.
        /// </summary>
        /// <param name="variableName">The name of the variable that will trigger the breakpoint. This value may not be null.</param>
        /// <param name="accessMode">The variable access mode that will trigger the breakpoint.</param>
        /// <param name="action">The action to take when the breakpoint is hit. If null, PowerShell will break into the debugger when the breakpoint is hit.</param>
        /// <param name="path">The path to the script file where the breakpoint may be hit. If null, the breakpoint may be hit anywhere the variable is accessed using the specified access mode.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The variable breakpoint that was set.</returns>
        public override VariableBreakpoint SetVariableBreakpoint(string variableName, VariableAccessMode accessMode, ScriptBlock action, string path, int? runspaceId) =>
            _wrappedDebugger.Value.SetVariableBreakpoint(variableName, accessMode, action, path, runspaceId);

        /// <summary>
        /// Removes a breakpoint from the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to remove from the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>True if the breakpoint was removed from the debugger; false otherwise.</returns>
        public override bool RemoveBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.Value.RemoveBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Enables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint EnableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.Value.EnableBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Disables a breakpoint in the debugger.
        /// </summary>
        /// <param name="breakpoint">The breakpoint to enable in the debugger. This value may not be null.</param>
        /// <param name="runspaceId">The runspace id of the runspace you want to interact with. A null value will use the current runspace.</param>
        /// <returns>The updated breakpoint if it was found; null if the breakpoint was not found in the debugger.</returns>
        public override Breakpoint DisableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.Value.DisableBreakpoint(breakpoint, runspaceId);

        /// <summary>
        /// Exits debugger mode with the provided resume action.
        /// </summary>
        /// <param name="resumeAction">DebuggerResumeAction.</param>
        public override void SetDebuggerAction(DebuggerResumeAction resumeAction)
        {
            if (!_inDebugMode)
            {
                throw new PSInvalidOperationException(
                    StringUtil.Format(DebuggerStrings.CannotSetRemoteDebuggerAction));
            }

            ExitDebugMode(resumeAction);
        }

        /// <summary>
        /// Returns debugger stop event args if in debugger stop state.
        /// </summary>
        /// <returns>DebuggerStopEventArgs.</returns>
        public override DebuggerStopEventArgs GetDebuggerStopArgs()
        {
            return _wrappedDebugger.Value.GetDebuggerStopArgs();
        }

        /// <summary>
        /// ProcessCommand.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <param name="output">Output.</param>
        /// <returns></returns>
        public override DebuggerCommandResults ProcessCommand(PSCommand command, PSDataCollection<PSObject> output)
        {
            if (LocalDebugMode)
            {
                return _wrappedDebugger.Value.ProcessCommand(command, output);
            }

            if (!InBreakpoint || (_threadCommandProcessing != null))
            {
                throw new PSInvalidOperationException(
                    StringUtil.Format(DebuggerStrings.CannotProcessDebuggerCommandNotStopped));
            }

            _processCommandCompleteEvent ??= new ManualResetEventSlim(false);

            _threadCommandProcessing = new ThreadCommandProcessing(command, output, _wrappedDebugger.Value, _processCommandCompleteEvent);
            try
            {
                return _threadCommandProcessing.Invoke(_nestedDebugStopCompleteEvent);
            }
            finally
            {
                _threadCommandProcessing = null;
            }
        }

        /// <summary>
        /// StopProcessCommand.
        /// </summary>
        public override void StopProcessCommand()
        {
            if (LocalDebugMode)
            {
                _wrappedDebugger.Value.StopProcessCommand();
            }

            ThreadCommandProcessing threadCommandProcessing = _threadCommandProcessing;
            threadCommandProcessing?.Stop();
        }

        /// <summary>
        /// SetDebugMode.
        /// </summary>
        /// <param name="mode"></param>
        public override void SetDebugMode(DebugModes mode)
        {
            _wrappedDebugger.Value.SetDebugMode(mode);

            base.SetDebugMode(mode);
        }

        /// <summary>
        /// True when debugger is active with breakpoints.
        /// </summary>
        public override bool IsActive
        {
            get
            {
                return (InBreakpoint || _wrappedDebugger.Value.IsActive || _wrappedDebugger.Value.InBreakpoint);
            }
        }

        /// <summary>
        /// Sets debugger stepping mode.
        /// </summary>
        /// <param name="enabled">True if stepping is to be enabled.</param>
        public override void SetDebuggerStepMode(bool enabled)
        {
            // Enable both the wrapper and wrapped debuggers for debugging before setting step mode.
            const DebugModes mode = DebugModes.LocalScript | DebugModes.RemoteScript;
            base.SetDebugMode(mode);
            _wrappedDebugger.Value.SetDebugMode(mode);

            _wrappedDebugger.Value.SetDebuggerStepMode(enabled);
        }

        /// <summary>
        /// InternalProcessCommand.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        internal override DebuggerCommand InternalProcessCommand(string command, IList<PSObject> output)
        {
            return _wrappedDebugger.Value.InternalProcessCommand(command, output);
        }

        /// <summary>
        /// Sets up debugger to debug provided job or its child jobs.
        /// </summary>
        /// <param name="job">
        /// Job object that is either a debuggable job or a container of
        /// debuggable child jobs.
        /// </param>
        /// <param name="breakAll">
        /// If true, the debugger automatically invokes a break all when it
        /// attaches to the job.
        /// </param>
        internal override void DebugJob(Job job, bool breakAll) =>
            _wrappedDebugger.Value.DebugJob(job, breakAll);

        /// <summary>
        /// Removes job from debugger job list and pops its
        /// debugger from the active debugger stack.
        /// </summary>
        /// <param name="job">Job.</param>
        internal override void StopDebugJob(Job job)
        {
            _wrappedDebugger.Value.StopDebugJob(job);
        }

        /// <summary>
        /// Sets up debugger to debug provided Runspace in a nested debug session.
        /// </summary>
        /// <param name="runspace">
        /// Runspace to debug.
        /// </param>
        /// <param name="breakAll">
        /// When true, this command will invoke a BreakAll when the debugger is
        /// first attached.
        /// </param>
        internal override void DebugRunspace(Runspace runspace, bool breakAll)
        {
            _wrappedDebugger.Value.DebugRunspace(runspace, breakAll);
        }

        /// <summary>
        /// Removes the provided Runspace from the nested "active" debugger state.
        /// </summary>
        /// <param name="runspace">Runspace.</param>
        internal override void StopDebugRunspace(Runspace runspace)
        {
            _wrappedDebugger.Value.StopDebugRunspace(runspace);
        }

        /// <summary>
        /// IsPushed.
        /// </summary>
        internal override bool IsPushed
        {
            get
            {
                return _wrappedDebugger.Value.IsPushed;
            }
        }

        /// <summary>
        /// IsRemote.
        /// </summary>
        internal override bool IsRemote
        {
            get
            {
                return _wrappedDebugger.Value.IsRemote;
            }
        }

        /// <summary>
        /// IsDebuggerSteppingEnabled.
        /// </summary>
        internal override bool IsDebuggerSteppingEnabled
        {
            get
            {
                return _wrappedDebugger.Value.IsDebuggerSteppingEnabled;
            }
        }

        /// <summary>
        /// UnhandledBreakpointMode.
        /// </summary>
        internal override UnhandledBreakpointProcessingMode UnhandledBreakpointMode
        {
            get
            {
                return _wrappedDebugger.Value.UnhandledBreakpointMode;
            }

            set
            {
                _wrappedDebugger.Value.UnhandledBreakpointMode = value;
                if (value == UnhandledBreakpointProcessingMode.Ignore &&
                    _inDebugMode)
                {
                    // Release debugger stop hold.
                    ExitDebugMode(DebuggerResumeAction.Continue);
                }
            }
        }

        /// <summary>
        /// IsPendingDebugStopEvent.
        /// </summary>
        internal override bool IsPendingDebugStopEvent
        {
            get { return _wrappedDebugger.Value.IsPendingDebugStopEvent; }
        }

        /// <summary>
        /// ReleaseSavedDebugStop.
        /// </summary>
        internal override void ReleaseSavedDebugStop()
        {
            _wrappedDebugger.Value.ReleaseSavedDebugStop();
        }

        /// <summary>
        /// Returns IEnumerable of CallStackFrame objects.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<CallStackFrame> GetCallStack()
        {
            return _wrappedDebugger.Value.GetCallStack();
        }

        internal override void Break(object triggerObject = null)
        {
            _wrappedDebugger.Value.Break(triggerObject);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            RemoveDebuggerCallbacks();
            if (_inDebugMode)
            {
                ExitDebugMode(DebuggerResumeAction.Stop);
            }

            _nestedDebugStopCompleteEvent?.Dispose();
            _processCommandCompleteEvent?.Dispose();
        }

        #endregion

        #region Private Classes

        private sealed class ThreadCommandProcessing
        {
            // Members
            private readonly ManualResetEventSlim _commandCompleteEvent;
            private readonly Debugger _wrappedDebugger;
            private readonly PSCommand _command;
            private readonly PSDataCollection<PSObject> _output;
            private DebuggerCommandResults _results;
            private Exception _exception;
#if !UNIX
            private WindowsIdentity _identityToImpersonate;
#endif

            // Constructors
            private ThreadCommandProcessing() { }

            public ThreadCommandProcessing(
                PSCommand command,
                PSDataCollection<PSObject> output,
                Debugger debugger,
                ManualResetEventSlim processCommandCompleteEvent)
            {
                _command = command;
                _output = output;
                _wrappedDebugger = debugger;
                _commandCompleteEvent = processCommandCompleteEvent;
            }

            // Methods
            public DebuggerCommandResults Invoke(ManualResetEventSlim startInvokeEvent)
            {
#if !UNIX
                // Get impersonation information to flow if any.
                Utils.TryGetWindowsImpersonatedIdentity(out _identityToImpersonate);
#endif

                // Signal thread to process command.
                Dbg.Assert(!_commandCompleteEvent.IsSet, "Command complete event shoulds always be non-signaled here.");
                Dbg.Assert(!startInvokeEvent.IsSet, "The event should always be in non-signaled state here.");
                startInvokeEvent.Set();

                // Wait for completion.
                _commandCompleteEvent.Wait();
                _commandCompleteEvent.Reset();
#if !UNIX
                if (_identityToImpersonate != null)
                {
                    _identityToImpersonate.Dispose();
                    _identityToImpersonate = null;
                }
#endif

                // Propagate exception.
                if (_exception != null)
                {
                    throw _exception;
                }

                // Return command processing results.
                return _results;
            }

            public void Stop()
            {
                Debugger debugger = _wrappedDebugger;
                debugger?.StopProcessCommand();
            }

            internal void DoInvoke()
            {
                try
                {
#if !UNIX
                    if (_identityToImpersonate != null)
                    {
                        _results = WindowsIdentity.RunImpersonated(
                            _identityToImpersonate.AccessToken,
                            () => _wrappedDebugger.ProcessCommand(_command, _output));
                        return;
                    }
#endif
                    _results = _wrappedDebugger.ProcessCommand(_command, _output);
                }
                catch (Exception e)
                {
                    _exception = e;
                }
                finally
                {
                    _commandCompleteEvent.Set();
                }
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Add Debugger suspend execution callback.
        /// </summary>
        private void SetDebuggerCallbacks()
        {
            if (_runspace != null &&
                _runspace.ExecutionContext != null &&
                _wrappedDebugger.Value != null)
            {
                SubscribeWrappedDebugger(_wrappedDebugger.Value);

                // Register debugger events for remote forwarding.
                var eventManager = _runspace.ExecutionContext.Events;

                if (!eventManager.GetEventSubscribers(RemoteDebugger.RemoteDebuggerStopEvent).GetEnumerator().MoveNext())
                {
                    eventManager.SubscribeEvent(
                        source: null,
                        eventName: null,
                        sourceIdentifier: RemoteDebugger.RemoteDebuggerStopEvent,
                        data: null,
                        action: null,
                        supportEvent: true,
                        forwardEvent: true);
                }

                if (!eventManager.GetEventSubscribers(RemoteDebugger.RemoteDebuggerBreakpointUpdatedEvent).GetEnumerator().MoveNext())
                {
                    eventManager.SubscribeEvent(
                        source: null,
                        eventName: null,
                        sourceIdentifier: RemoteDebugger.RemoteDebuggerBreakpointUpdatedEvent,
                        data: null,
                        action: null,
                        supportEvent: true,
                        forwardEvent: true);
                }
            }
        }

        /// <summary>
        /// Remove the suspend execution callback.
        /// </summary>
        private void RemoveDebuggerCallbacks()
        {
            if (_runspace != null &&
                _runspace.ExecutionContext != null &&
                _wrappedDebugger.Value != null)
            {
                UnsubscribeWrappedDebugger(_wrappedDebugger.Value);

                // Unregister debugger events for remote forwarding.
                var eventManager = _runspace.ExecutionContext.Events;

                foreach (var subscriber in eventManager.GetEventSubscribers(RemoteDebugger.RemoteDebuggerStopEvent))
                {
                    eventManager.UnsubscribeEvent(subscriber);
                }

                foreach (var subscriber in eventManager.GetEventSubscribers(RemoteDebugger.RemoteDebuggerBreakpointUpdatedEvent))
                {
                    eventManager.UnsubscribeEvent(subscriber);
                }
            }
        }

        /// <summary>
        /// Handler for debugger events.
        /// </summary>
        private void HandleDebuggerStop(object sender, DebuggerStopEventArgs e)
        {
            // Ignore if we are in restricted mode.
            if (!IsDebuggingSupported())
            {
                return;
            }

            if (LocalDebugMode)
            {
                // Forward event locally.
                RaiseDebuggerStopEvent(e);
                return;
            }

            if ((DebugMode & DebugModes.RemoteScript) != DebugModes.RemoteScript)
            {
                return;
            }

            _debuggerStopEventArgs = e;
            PSHost contextHost = null;

            try
            {
                // Save current context remote host.
                contextHost = _runspace.ExecutionContext.InternalHost.ExternalHost;

                // Forward event to remote client.
                Dbg.Assert(_runspace != null, "Runspace cannot be null.");
                _runspace.ExecutionContext.Events.GenerateEvent(
                    sourceIdentifier: RemoteDebugger.RemoteDebuggerStopEvent,
                    sender: null,
                    args: new object[] { e },
                    extraData: null);

                //
                // Start the debug mode.  This is a blocking call and will return only
                // after ExitDebugMode() is called.
                //
                EnterDebugMode(_wrappedDebugger.Value.IsPushed);

                // Restore original context remote host.
                _runspace.ExecutionContext.InternalHost.SetHostRef(contextHost);
            }
            catch (Exception)
            {
            }
            finally
            {
                _debuggerStopEventArgs = null;
            }
        }

        /// <summary>
        /// HandleBreakpointUpdated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleBreakpointUpdated(object sender, BreakpointUpdatedEventArgs e)
        {
            // Ignore if we are in restricted mode.
            if (!IsDebuggingSupported())
            {
                return;
            }

            if (LocalDebugMode)
            {
                // Forward event locally.
                RaiseBreakpointUpdatedEvent(e);
                return;
            }

            try
            {
                // Forward event to remote client.
                Dbg.Assert(_runspace != null, "Runspace cannot be null.");
                _runspace.ExecutionContext.Events.GenerateEvent(
                    sourceIdentifier: RemoteDebugger.RemoteDebuggerBreakpointUpdatedEvent,
                    sender: null,
                    args: new object[] { e },
                    extraData: null);
            }
            catch (Exception)
            {
            }
        }

        private void HandleNestedDebuggingCancelEvent(object sender, EventArgs e)
        {
            // Forward cancel event from wrapped debugger.
            RaiseNestedDebuggingCancelEvent();

            // Release debugger.
            if (_inDebugMode)
            {
                ExitDebugMode(DebuggerResumeAction.Continue);
            }
        }

        /// <summary>
        /// Sends a DebuggerStop event to the client and enters a nested pipeline.
        /// </summary>
        private void EnterDebugMode(bool isNestedStop)
        {
            _inDebugMode = true;

            try
            {
                _runspace.ExecutionContext.SetVariable(SpecialVariables.NestedPromptCounterVarPath, 1);

                if (isNestedStop)
                {
                    // Blocking call for nested debugger execution (Debug-Runspace) stop events.
                    // The root debugger never makes two EnterDebugMode calls without an ExitDebugMode.
                    _nestedDebugStopCompleteEvent ??= new ManualResetEventSlim(false);

                    _nestedDebugging = true;
                    OnEnterDebugMode(_nestedDebugStopCompleteEvent);
                }
                else
                {
                    // Blocking call.
                    // Process all client commands as nested until nested pipeline is exited at
                    // which point this call returns.
                    _driverInvoker.EnterNestedPipeline();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                _inDebugMode = false;
                _nestedDebugging = false;
            }

            // Check to see if we should re-raise the stop event locally.
            if (_raiseStopEventLocally)
            {
                _raiseStopEventLocally = false;
                LocalDebugMode = true;
                HandleDebuggerStop(this, _debuggerStopEventArgs);
            }
        }

        /// <summary>
        /// Blocks DebuggerStop event thread until exit debug mode is
        /// received from the client.
        /// </summary>
        private void OnEnterDebugMode(ManualResetEventSlim debugModeCompletedEvent)
        {
            Dbg.Assert(!debugModeCompletedEvent.IsSet, "Event should always be non-signaled here.");

            while (true)
            {
                debugModeCompletedEvent.Wait();
                debugModeCompletedEvent.Reset();

                if (_threadCommandProcessing != null)
                {
                    // Process command.
                    _threadCommandProcessing.DoInvoke();
                    _threadCommandProcessing = null;
                }
                else
                {
                    // No command to process.  Exit debug mode.
                    break;
                }
            }
        }

        /// <summary>
        /// Exits the server side nested pipeline.
        /// </summary>
        private void ExitDebugMode(DebuggerResumeAction resumeAction)
        {
            _debuggerStopEventArgs.ResumeAction = resumeAction;

            try
            {
                if (_nestedDebugging)
                {
                    // Release nested debugger.
                    _nestedDebugStopCompleteEvent.Set();
                }
                else
                {
                    // Release EnterDebugMode blocking call.
                    _driverInvoker.ExitNestedPipeline();
                }

                _runspace.ExecutionContext.SetVariable(SpecialVariables.NestedPromptCounterVarPath, 0);
            }
            catch (Exception)
            {
            }
        }

        private void SubscribeWrappedDebugger(Debugger wrappedDebugger)
        {
            wrappedDebugger.DebuggerStop += HandleDebuggerStop;
            wrappedDebugger.BreakpointUpdated += HandleBreakpointUpdated;
            wrappedDebugger.NestedDebuggingCancelledEvent += HandleNestedDebuggingCancelEvent;
        }

        private void UnsubscribeWrappedDebugger(Debugger wrappedDebugger)
        {
            wrappedDebugger.DebuggerStop -= HandleDebuggerStop;
            wrappedDebugger.BreakpointUpdated -= HandleBreakpointUpdated;
            wrappedDebugger.NestedDebuggingCancelledEvent -= HandleNestedDebuggingCancelEvent;
        }

        private bool IsDebuggingSupported()
        {
            // Restriction only occurs on a (non-pushed) local runspace.
            LocalRunspace localRunspace = _runspace as LocalRunspace;
            if (localRunspace != null)
            {
                CmdletInfo cmdletInfo = localRunspace.ExecutionContext.EngineSessionState.GetCmdlet(SetPSBreakCommandText);
                if ((cmdletInfo != null) && (cmdletInfo.Visibility != SessionStateEntryVisibility.Public))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// HandleStopSignal.
        /// </summary>
        /// <returns>True if stop signal is handled.</returns>
        internal bool HandleStopSignal()
        {
            // If in pushed mode then stop any running command.
            if (IsPushed && (_threadCommandProcessing != null))
            {
                StopProcessCommand();
                return true;
            }

            // Set debug mode to "None" so that current command can stop and not
            // potentially not respond in a debugger stop.  Use RestoreDebugger() to
            // restore debugger to original mode.
            _wrappedDebugger.Value.SetDebugMode(DebugModes.None);
            if (InBreakpoint)
            {
                try
                {
                    SetDebuggerAction(DebuggerResumeAction.Continue);
                }
                catch (PSInvalidOperationException) { }
            }

            return false;
        }

        // Sets the wrapped debugger to the same mode as the wrapper
        // server remote debugger, enabling it if remote debugging is enabled.
        internal void CheckDebuggerState()
        {}

        internal void StartPowerShellCommand(
            PowerShell powershell,
            Guid powershellId,
            Guid runspacePoolId,
            object runspacePoolDriver,
            ApartmentState apartmentState,
            ServerRemoteHost remoteHost,
            HostInfo hostInfo,
            RemoteStreamOptions streamOptions,
            bool addToHistory)
        {
        }

        private void HandlePowerShellInvocationStateChanged(object sender, PSInvocationStateChangedEventArgs e)
        {
            if (e.InvocationStateInfo.State == PSInvocationState.Completed ||
                e.InvocationStateInfo.State == PSInvocationState.Stopped ||
                e.InvocationStateInfo.State == PSInvocationState.Failed)
            {
                PowerShell powershell = sender as PowerShell;
                powershell.InvocationStateChanged -= HandlePowerShellInvocationStateChanged;

                Runspace runspace = powershell.GetRunspaceConnection() as Runspace;
                runspace.Close();
                runspace.Dispose();
            }
        }

        internal int GetBreakpointCount()
        {
            ScriptDebugger scriptDebugger = _wrappedDebugger.Value as ScriptDebugger;
            if (scriptDebugger != null)
            {
                return scriptDebugger.GetBreakpoints().Count;
            }
            else
            {
                return 0;
            }
        }

        internal void PushDebugger(Debugger debugger)
        {}

        internal void PopDebugger()
        {}

        internal void ReleaseAndRaiseDebugStopLocal()
        {}

        #endregion

        #region Internal Properties

        /// <summary>
        /// When true, this debugger is being used for local debugging (not remote debugging)
        /// via the Debug-Runspace cmdlet.
        /// </summary>
        internal bool LocalDebugMode
        {
            get;
            set;
        }

        #endregion
    }
}
