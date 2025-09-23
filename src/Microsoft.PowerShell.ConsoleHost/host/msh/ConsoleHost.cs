// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Management.Automation.Subsystem.Feedback;
using System.Management.Automation.Tracing;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.PowerShell.Commands;
using ConsoleHandle = Microsoft.Win32.SafeHandles.SafeFileHandle;
using Dbg = System.Management.Automation.Diagnostics;
using Debugger = System.Management.Automation.Debugger;

namespace Microsoft.PowerShell
{
    
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal sealed partial class ConsoleHost
        :
        PSHost,
        IDisposable,
#if LEGACYTELEMETRY
        IHostProvidesTelemetryData,
#endif
        IHostSupportsInteractiveSession
    {
        #region static methods

        internal const int ExitCodeSuccess = 0;
        internal const int ExitCodeCtrlBreak = 128 + 21; // SIGBREAK
        internal const int ExitCodeInitFailure = 70; // Internal Software Error
        internal const int ExitCodeBadCommandLineParameter = 64; // Command Line Usage Error
        private const uint SPI_GETSCREENREADER = 0x0046;
#if UNIX
        internal const string DECCKM_ON = "\x1b[?1h";
        internal const string DECCKM_OFF = "\x1b[?1l";
#endif

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref bool pvParam, uint fWinIni);

        
        internal static int Start(
            string bannerText,
            string helpText,
            bool issProvidedExternally)
        {
            
            string profileDir = Platform.CacheDirectory;
            uint exitCode = ExitCodeSuccess;

            try
            {
                HostException hostException = null;
                try
                {
                    s_theConsoleHost = ConsoleHost.CreateSingletonInstance();
                }
                catch (HostException e)
                {
                    hostException = e;
                }

                PSHostUserInterface hostUI = s_theConsoleHost?.UI ?? new NullHostUserInterface();
                {
                    // Run PowerShell in normal console mode.
                    if (hostException != null)
                    {
                        // Unable to create console host.
                        throw hostException;
                    }

                    LoadPSReadline();

                    s_theConsoleHost.BindBreakHandler();
                    PSHost.IsStdOutputRedirected = Console.IsOutputRedirected;
                    exitCode = s_theConsoleHost.Run(s_cpp, false);
                }
            }
            finally
            {
#pragma warning disable IDE0031
                if (s_theConsoleHost != null)
                {
                    if (s_theConsoleHost.IsInteractive && s_theConsoleHost.UI.SupportsVirtualTerminal)
                    {
                        // https://github.com/dotnet/runtime/issues/27626 leaves terminal in application mode
                        // for now, we explicitly emit DECRST 1 sequence
                        s_theConsoleHost.UI.Write(DECCKM_OFF);
                    }
                    s_theConsoleHost.Dispose();
                }
#pragma warning restore IDE0031
            }

            unchecked
            {
                return (int)exitCode;
            }
        }

        internal static void ParseCommandLine(string[] args)
        {
            s_cpp.Parse(args);
        }

        private static readonly CommandLineParameterParser s_cpp = new CommandLineParameterParser();

        
        private static void MyBreakHandler(object sender, ConsoleCancelEventArgs args)
        {
            // Set the Cancel property to true to prevent the process from terminating.
            args.Cancel = true;
            switch (args.SpecialKey)
            {
                case ConsoleSpecialKey.ControlC:
                    SpinUpBreakHandlerThread(shouldEndSession: false);
                    return;
                case ConsoleSpecialKey.ControlBreak:
                    if (s_cpp.NonInteractive)
                    {
                        // ControlBreak mimics ControlC in Noninteractive shells
                        SpinUpBreakHandlerThread(shouldEndSession: true);
                    }

                    return;
            }
        }

        
        private static void SpinUpBreakHandlerThread(bool shouldEndSession)
        {
            ConsoleHost host = ConsoleHost.SingletonInstance;

            Thread bht = null;

            lock (host.hostGlobalLock)
            {
                bht = host._breakHandlerThread;
                if (!host.ShouldEndSession && shouldEndSession)
                {
                    host.ShouldEndSession = shouldEndSession;
                }

                // Creation of the thread and starting it should be an atomic operation.
                // otherwise the code in Run method can get instance of the breakhandlerThread
                // after it is created and before started and call join on it. This will result
                // in ThreadStateException.
                // NTRAID#Windows OutofBand Bugs-938289-2006/07/27-hiteshr
                if (bht == null)
                {
                    // we're not already running HandleBreak on a separate thread, so run it now.

                    host._breakHandlerThread = new Thread(new ThreadStart(ConsoleHost.HandleBreak));
                    host._breakHandlerThread.Name = "ConsoleHost.HandleBreak";
                    host._breakHandlerThread.Start();
                }
            }
        }

        private static void HandleBreak()
        {
            ConsoleHost consoleHost = s_theConsoleHost;
            if (consoleHost != null)
            {
                if (consoleHost.InDebugMode)
                {
                    // Only stop a running user command, ignore prompt evaluation.
                    if (consoleHost.DebuggerCanStopCommand)
                    {
                        // Cancel any executing debugger command if in debug mode.
                        try
                        {
                            consoleHost.Runspace.Debugger.StopProcessCommand();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                else
                {
                    // Cancel the reconnected debugged running pipeline command.
                    if (!StopPipeline(consoleHost.runningCmd))
                    {
                        Executor.CancelCurrentExecutor();
                    }
                }

                if (consoleHost.ShouldEndSession)
                {
                    var runspaceRef = ConsoleHost.SingletonInstance._runspaceRef;
                    if (runspaceRef != null)
                    {
                        var runspace = runspaceRef.Runspace;
                        runspace?.Close();
                    }
                }
            }

            ConsoleHost.SingletonInstance._breakHandlerThread = null;
        }

        private static bool StopPipeline(Pipeline cmd)
        {
            if (cmd != null &&
                (cmd.PipelineStateInfo.State == PipelineState.Running ||
                 cmd.PipelineStateInfo.State == PipelineState.Disconnected))
            {
                try
                {
                    cmd.StopAsync();
                    return true;
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        
        internal static ConsoleHost CreateSingletonInstance()
        {
            Dbg.Assert(s_theConsoleHost == null, "CreateSingletonInstance should not be called multiple times");
            s_theConsoleHost = new ConsoleHost();
            return s_theConsoleHost;
        }

        internal static ConsoleHost SingletonInstance
        {
            get
            {
                Dbg.Assert(s_theConsoleHost != null, "CreateSingletonInstance should be called before calling this method");
                return s_theConsoleHost;
            }
        }

        #endregion static methods

        #region overrides

        
        public override string Name
        {
            get
            {
                const string myName = "ConsoleHost";

                // const, no lock needed.
                return myName;
            }
        }

        
        public override System.Version Version
        {
            get
            {
                // const, no lock needed.
                return _ver;
            }
        }

        
        public override System.Guid InstanceId { get; } = Guid.NewGuid();

        
        public override PSHostUserInterface UI
        {
            get
            {
                Dbg.Assert(ui != null, "ui should have been allocated in ctor");
                return ui;
            }
        }

        
        public void PushRunspace(Runspace runspace)
        {
            if (_runspaceRef == null)
            {
                return;
            }

            if (runspace is not RemoteRunspace remoteRunspace)
            {
                throw new ArgumentException(ConsoleHostStrings.PushRunspaceNotRemote, nameof(runspace));
            }

            remoteRunspace.StateChanged += HandleRemoteRunspaceStateChanged;

            // Unsubscribe the local session debugger.
            if (_runspaceRef.Runspace.Debugger != null)
            {
                _runspaceRef.Runspace.Debugger.DebuggerStop -= OnExecutionSuspended;
            }

            // Subscribe to debugger stop event.
            if (remoteRunspace.Debugger != null)
            {
                remoteRunspace.Debugger.DebuggerStop += OnExecutionSuspended;
            }

            // Connect a disconnected command.
            this.runningCmd = EnterPSSessionCommand.ConnectRunningPipeline(remoteRunspace);

            // Push runspace.
            _runspaceRef.Override(remoteRunspace, hostGlobalLock, out _isRunspacePushed);
            RunspacePushed.SafeInvoke(this, EventArgs.Empty);

            if (this.runningCmd != null)
            {
                EnterPSSessionCommand.ContinueCommand(
                    remoteRunspace,
                    this.runningCmd,
                    this,
                    InDebugMode,
                    _runspaceRef.OldRunspace.ExecutionContext);
            }

            this.runningCmd = null;
        }

        
        private void HandleRemoteRunspaceStateChanged(object sender, RunspaceStateEventArgs eventArgs)
        {
            RunspaceState state = eventArgs.RunspaceStateInfo.State;

            switch (state)
            {
                case RunspaceState.Opening:
                case RunspaceState.Opened:
                    {
                        return;
                    }
                case RunspaceState.Closing:
                case RunspaceState.Closed:
                case RunspaceState.Broken:
                case RunspaceState.Disconnected:
                    {
                        PopRunspace();
                    }

                    break;
            }
        }

        
        public void PopRunspace()
        {
            if (_runspaceRef == null ||
                !_runspaceRef.IsRunspaceOverridden)
            {
                return;
            }

            if (_inPushedConfiguredSession)
            {
                // For configured endpoint sessions, end session when configured runspace is popped.
                this.ShouldEndSession = true;
            }

            if (_runspaceRef.Runspace.Debugger != null)
            {
                // Unsubscribe pushed runspace debugger.
                _runspaceRef.Runspace.Debugger.DebuggerStop -= OnExecutionSuspended;

                StopPipeline(this.runningCmd);

                if (this.InDebugMode)
                {
                    ExitDebugMode(DebuggerResumeAction.Continue);
                }
            }

            this.runningCmd = null;

            lock (hostGlobalLock)
            {
                _runspaceRef.Revert();
                _isRunspacePushed = false;
            }

            // Re-subscribe local runspace debugger.
            _runspaceRef.Runspace.Debugger.DebuggerStop += OnExecutionSuspended;

            // raise events outside the lock
            RunspacePopped.SafeInvoke(this, EventArgs.Empty);
        }

        
        public bool IsRunspacePushed
        {
            get
            {
                return _isRunspacePushed;
            }
        }

        private bool _isRunspacePushed = false;

        
        public Runspace Runspace
        {
            get
            {
                if (this.RunspaceRef == null)
                {
                    return null;
                }

                return this.RunspaceRef.Runspace;
            }
        }

        internal LocalRunspace LocalRunspace
        {
            get
            {
                if (_isRunspacePushed)
                {
                    return RunspaceRef.OldRunspace as LocalRunspace;
                }

                if (RunspaceRef == null)
                {
                    return null;
                }

                return RunspaceRef.Runspace as LocalRunspace;
            }
        }

        public class ConsoleColorProxy
        {
            private readonly ConsoleHostUserInterface _ui;

            public ConsoleColorProxy(ConsoleHostUserInterface ui)
            {
                ArgumentNullException.ThrowIfNull(ui);
                _ui = ui;
            }

            public ConsoleColor FormatAccentColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.FormatAccentColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.FormatAccentColor = value;
                }
            }

            public ConsoleColor ErrorAccentColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.ErrorAccentColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.ErrorAccentColor = value;
                }
            }

            public ConsoleColor ErrorForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.ErrorForegroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.ErrorForegroundColor = value;
                }
            }

            public ConsoleColor ErrorBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.ErrorBackgroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.ErrorBackgroundColor = value;
                }
            }

            public ConsoleColor WarningForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.WarningForegroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.WarningForegroundColor = value;
                }
            }

            public ConsoleColor WarningBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.WarningBackgroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.WarningBackgroundColor = value;
                }
            }

            public ConsoleColor DebugForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.DebugForegroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.DebugForegroundColor = value;
                }
            }

            public ConsoleColor DebugBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.DebugBackgroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.DebugBackgroundColor = value;
                }
            }

            public ConsoleColor VerboseForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.VerboseForegroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.VerboseForegroundColor = value;
                }
            }

            public ConsoleColor VerboseBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.VerboseBackgroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.VerboseBackgroundColor = value;
                }
            }

            public ConsoleColor ProgressForegroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.ProgressForegroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.ProgressForegroundColor = value;
                }
            }

            public ConsoleColor ProgressBackgroundColor
            {
                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                get
                {
                    return _ui.ProgressBackgroundColor;
                }

                [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
                set
                {
                    _ui.ProgressBackgroundColor = value;
                }
            }
        }

        
        public override PSObject PrivateData
        {
            get
            {
                if (ui == null)
                {
                    return null;
                }

                return _consoleColorProxy ??= PSObject.AsPSObject(new ConsoleColorProxy(ui));
            }
        }

        private PSObject _consoleColorProxy;

        
        public override System.Globalization.CultureInfo CurrentCulture
        {
            get
            {
                lock (hostGlobalLock)
                {
                    return CultureInfo.CurrentCulture;
                }
            }
        }

        
        public override System.Globalization.CultureInfo CurrentUICulture
        {
            get
            {
                lock (hostGlobalLock)
                {
                    return CultureInfo.CurrentUICulture;
                }
            }
        }

        
        public override void SetShouldExit(int exitCode)
        {
            lock (hostGlobalLock)
            {
                // Check for the pushed runspace scenario.
                if (this.IsRunspacePushed)
                {
                    this.PopRunspace();
                }
                else if (InDebugMode)
                {
                    ExitDebugMode(DebuggerResumeAction.Continue);
                }
                else
                {
                    _setShouldExitCalled = true;
                    _exitCodeFromRunspace = exitCode;
                    ShouldEndSession = true;
                }
            }
        }

        
        public override void EnterNestedPrompt()
        {
            // save the old Executor, then clear it so that a break does not cancel the pipeline from which this method
            // might be called.

            Executor oldCurrent = Executor.CurrentExecutor;

            try
            {
                // this assignment is threadsafe -- protected in CurrentExecutor property

                Executor.CurrentExecutor = null;
                lock (hostGlobalLock)
                {
                    IsNested = oldCurrent != null || this.ui.IsCommandCompletionRunning;
                }

                InputLoop.RunNewInputLoop(this, IsNested);
            }
            finally
            {
                Executor.CurrentExecutor = oldCurrent;
            }
        }

        
        public override void ExitNestedPrompt()
        {
            lock (hostGlobalLock)
            {
                IsNested = InputLoop.ExitCurrentLoop();
            }
        }

        
        public override void NotifyBeginApplication()
        {
            lock (hostGlobalLock)
            {
                if (++_beginApplicationNotifyCount == 1)
                {
                    // Save the window title when first notified.
                    _savedWindowTitle = ui.RawUI.WindowTitle;
#if !UNIX
                    if (_initialConsoleMode != ConsoleControl.ConsoleModes.Unknown)
                    {
                        var outputHandle = ConsoleControl.GetActiveScreenBufferHandle();
                        _savedConsoleMode = ConsoleControl.GetMode(outputHandle);
                        ConsoleControl.SetMode(outputHandle, _initialConsoleMode);
                    }
#endif
                }
            }
        }

        
        public override void NotifyEndApplication()
        {
            lock (hostGlobalLock)
            {
                if (--_beginApplicationNotifyCount == 0)
                {
                    // Restore the window title when the last application started has ended.
                    ui.RawUI.WindowTitle = _savedWindowTitle;
#if !UNIX
                    if (_savedConsoleMode != ConsoleControl.ConsoleModes.Unknown)
                    {
                        ConsoleControl.SetMode(ConsoleControl.GetActiveScreenBufferHandle(), _savedConsoleMode);
                        if (_savedConsoleMode.HasFlag(ConsoleControl.ConsoleModes.VirtualTerminal))
                        {
                            // If the console output mode we just set already has 'VirtualTerminal' turned on,
                            // we don't need to try turn on the VT mode separately.
                            return;
                        }
                    }

                    if (ui.SupportsVirtualTerminal)
                    {
                        // Re-enable VT mode if it was previously enabled, as a native command may have turned it off.
                        ui.TryTurnOnVirtualTerminal();
                    }
#endif
                }
            }
        }

#if LEGACYTELEMETRY
        bool IHostProvidesTelemetryData.HostIsInteractive
        {
            get
            {
                return !s_cpp.NonInteractive && !s_cpp.AbortStartup &&
                       ((s_cpp.InitialCommand == null && s_cpp.File == null) || s_cpp.NoExit);
            }
        }

        double IHostProvidesTelemetryData.ProfileLoadTimeInMS { get { return _profileLoadTimeInMS; } }

        double IHostProvidesTelemetryData.ReadyForInputTimeInMS { get { return _readyForInputTimeInMS; } }

        int IHostProvidesTelemetryData.InteractiveCommandCount { get { return _interactiveCommandCount; } }

        private double _readyForInputTimeInMS;
        private int _interactiveCommandCount;
#endif

        // private double _profileLoadTimeInMS;

        #endregion overrides

        #region non-overrides

        
        internal ConsoleHost()
        {
#if !UNIX
            try
            {
                // Capture the initial console mode before constructing our ui - that constructor
                // will change the console mode to turn on VT100 support.
                _initialConsoleMode = ConsoleControl.GetMode(ConsoleControl.GetActiveScreenBufferHandle());
            }
            catch (Exception)
            {
                // Make sure we skip setting the console mode later.
                _savedConsoleMode = _initialConsoleMode = ConsoleControl.ConsoleModes.Unknown;
            }
#endif

            Thread.CurrentThread.CurrentUICulture = this.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = this.CurrentCulture;
            // BUG: 610329. Tell PowerShell engine to apply console
            // related properties while launching Pipeline Execution
            // Thread.
            base.ShouldSetThreadUILanguageToZero = true;

            InDebugMode = false;
            _displayDebuggerBanner = true;

            this.ui = new ConsoleHostUserInterface(this);
            _consoleWriter = new ConsoleTextWriter(ui);

            UnhandledExceptionEventHandler handler = new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
            AppDomain.CurrentDomain.UnhandledException += handler;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "Accesses instance members in preprocessor branch.")]
        private void BindBreakHandler()
        {
#if UNIX
            Console.CancelKeyPress += new ConsoleCancelEventHandler(MyBreakHandler);
#else
            breakHandlerGcHandle = GCHandle.Alloc(new ConsoleControl.BreakHandler(MyBreakHandler));
            ConsoleControl.AddBreakHandler((ConsoleControl.BreakHandler)breakHandlerGcHandle.Target);
#endif
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            // FYI: sender is a reference to the source app domain

            // The CLR will shut down the app as soon as this handler is complete, but just in case, we want
            // to exit at our next opportunity.

            _shouldEndSession = true;

            Exception e = null;

            if (args != null)
            {
                e = (Exception)args.ExceptionObject;
            }

            ui.WriteLine();
            ui.Write(
                ConsoleColor.Red,
                ui.RawUI.BackgroundColor,
                ConsoleHostStrings.UnhandledExceptionShutdownMessage);
            ui.WriteLine();
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposingNotFinalizing)
        {
            if (!_isDisposed)
            {
#if !UNIX
                ConsoleControl.RemoveBreakHandler();
                if (breakHandlerGcHandle.IsAllocated)
                {
                    breakHandlerGcHandle.Free();
                }
#endif

                if (isDisposingNotFinalizing)
                {
                    if (IsTranscribing)
                    {
                        StopTranscribing();
                    }

                    _outputSerializer?.End();
                    _errorSerializer?.End();

                    if (_runspaceRef != null)
                    {
                        // NTRAID#Windows Out Of Band Releases-925297-2005/12/14
                        try
                        {
                            _runspaceRef.Runspace.Dispose();
                        }
                        catch (InvalidRunspaceStateException)
                        {
                        }
                    }

                    _runspaceRef = null;
                    ui = null;
                }
            }

            _isDisposed = true;
        }

        
        ~ConsoleHost()
        {
            Dispose(false);
        }

        
        internal bool ShouldEndSession
        {
            // This might get called from the main thread, or from the pipeline thread, or from a break handler thread.
            get
            {
                bool result = false;

                lock (hostGlobalLock)
                {
                    result = _shouldEndSession;
                }

                return result;
            }

            set
            {
                lock (hostGlobalLock)
                {
                    // If ShouldEndSession is already true, you can't set it back

                    Dbg.Assert(!_shouldEndSession || value,
                        "ShouldEndSession can only be set from false to true");

                    _shouldEndSession = value;
                }
            }
        }

        
        internal RunspaceRef RunspaceRef
        {
            get
            {
                return _runspaceRef;
            }
        }

        internal WrappedSerializer.DataFormat OutputFormat { get; private set; }

        internal bool OutputFormatSpecified { get; private set; }

        internal WrappedSerializer.DataFormat InputFormat { get; private set; }

        internal WrappedDeserializer.DataFormat ErrorFormat
        {
            get
            {
                WrappedDeserializer.DataFormat format = OutputFormat;

                // If this shell is invoked in non-interactive, error is redirected, and OutputFormat was not
                // specified write data in error stream in xml format assuming PowerShell->PowerShell usage.
                if (!OutputFormatSpecified && !IsInteractive && Console.IsErrorRedirected && _wasInitialCommandEncoded)
                {
                    format = Serialization.DataFormat.XML;
                }

                return format;
            }
        }

        internal bool IsRunningAsync
        {
            get
            {
                return !IsInteractive && ((OutputFormat != Serialization.DataFormat.Text) || Console.IsInputRedirected);
            }
        }

        internal bool IsNested { get; private set; }

        internal WrappedSerializer OutputSerializer
        {
            get
            {
                _outputSerializer ??=
                    new WrappedSerializer(
                        OutputFormat,
                        "Output",
                        Console.IsOutputRedirected ? Console.Out : ConsoleTextWriter);

                return _outputSerializer;
            }
        }

        internal WrappedSerializer ErrorSerializer
        {
            get
            {
                _errorSerializer ??=
                    new WrappedSerializer(
                        ErrorFormat,
                        "Error",
                        Console.IsErrorRedirected ? Console.Error : ConsoleTextWriter);

                return _errorSerializer;
            }
        }

        internal bool IsInteractive
        {
            get
            {
                // we're running interactive if we're in a prompt loop, and we're not reading keyboard input from stdin.

                return _isRunningPromptLoop && !ui.ReadFromStdin;
            }
        }

        internal TextWriter ConsoleTextWriter
        {
            get
            {
                Dbg.Assert(_consoleWriter != null, "consoleWriter should have been initialized");
                return _consoleWriter;
            }
        }

        
        private uint Run(CommandLineParameterParser cpp, bool isPrestartWarned)
        {
            uint exitCode = ExitCodeSuccess;

            do
            {
                exitCode = ExitCodeSuccess;
                if (!string.IsNullOrEmpty(cpp.InitialCommand) && isPrestartWarned)
                {
                    string msg = StringUtil.Format(ConsoleHostStrings.InitialCommandNotExecuted, cpp.InitialCommand);
                    ui.WriteErrorLine(msg);
                    exitCode = ExitCodeInitFailure;
                    break;
                }

                if (cpp.AbortStartup)
                {
                    exitCode = cpp.ExitCode;
                    break;
                }

                OutputFormat = cpp.OutputFormat;
                OutputFormatSpecified = cpp.OutputFormatSpecified;
                InputFormat = cpp.InputFormat;
                _wasInitialCommandEncoded = cpp.WasInitialCommandEncoded;

                ui.ReadFromStdin = cpp.ExplicitReadCommandsFromStdin || Console.IsInputRedirected;
                ui.NoPrompt = cpp.NoPrompt;
                ui.ThrowOnReadAndPrompt = cpp.ThrowOnReadAndPrompt;
                _noExit = cpp.NoExit;

                // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
                // Removed HandleUnexpectedExceptions infrastructure
                exitCode = DoRunspaceLoop(cpp.InitialCommand, cpp.SkipProfiles, cpp.Args, cpp.StaMode, cpp.ConfigurationName, cpp.ConfigurationFile);
            }
            while (false);

            return exitCode;
        }

        
        private uint DoRunspaceLoop(
            string initialCommand,
            bool skipProfiles,
            Collection<CommandParameter> initialCommandArgs,
            bool staMode,
            string configurationName,
            string configurationFilePath)
        {
            ExitCode = ExitCodeSuccess;

            while (!ShouldEndSession)
            {
                RunspaceCreationEventArgs args = new RunspaceCreationEventArgs(initialCommand, skipProfiles, staMode, configurationName, configurationFilePath, initialCommandArgs);
                CreateRunspace(args);

                if (ExitCode == ExitCodeInitFailure)
                {
                    break;
                }

                if (!_noExit)
                {
                    // Wait for runspace to open, init, and run init script before
                    // setting ShouldEndSession, to allow debugger to work.
                    ShouldEndSession = true;
                }
                else
                {
                    // Start nested prompt loop.
                    EnterNestedPrompt();
                }

                if (_setShouldExitCalled)
                {
                    ExitCode = unchecked((uint)_exitCodeFromRunspace);
                }
                else
                {
                    Executor exec = new Executor(this, false, false);

                    bool dollarHook = exec.ExecuteCommandAndGetResultAsBool("$global:?") ?? false;

                    if (dollarHook && (_lastRunspaceInitializationException == null))
                    {
                        ExitCode = ExitCodeSuccess;
                    }
                    else
                    {
                        ExitCode = ExitCodeSuccess | 0x1;
                    }
                }

                _runspaceRef.Runspace.Close();
                _runspaceRef = null;

                if (staMode)
                {
                    // don't continue the session in STA mode
                    ShouldEndSession = true;
                }
            }

            return ExitCode;
        }

        private Exception InitializeRunspaceHelper(string command, Executor exec, Executor.ExecutionOptions options)
        {
            Dbg.Assert(!string.IsNullOrEmpty(command), "command should have a value");
            Dbg.Assert(exec != null, "non-null Executor instance needed");

            s_runspaceInitTracer.WriteLine("running command {0}", command);

            Exception e = null;

            if (IsRunningAsync)
            {
                exec.ExecuteCommandAsync(command, out e, options);
            }
            else
            {
                exec.ExecuteCommand(command, out e, options);
            }

            if (e != null)
            {
                ReportException(e, exec);
            }

            return e;
        }

        private void CreateRunspace(RunspaceCreationEventArgs runspaceCreationArgs)
        {
            try
            {
                Dbg.Assert(runspaceCreationArgs != null, "Arguments to CreateRunspace should not be null.");
                DoCreateRunspace(runspaceCreationArgs);
            }
            catch (ConsoleHostStartupException startupException)
            {
                ReportExceptionFallback(startupException.InnerException, startupException.Message);
                ExitCode = ExitCodeInitFailure;
            }
        }

        
        private bool IsScreenReaderActive()
        {
            if (_screenReaderActive.HasValue)
            {
                return _screenReaderActive.Value;
            }

            _screenReaderActive = false;
            if (Platform.IsWindowsDesktop)
            {
                // Note: this API can detect if a third-party screen reader is active, such as NVDA, but not the in-box Windows Narrator.
                // Quoted from https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-systemparametersinfoa about the
                // accessibility parameter 'SPI_GETSCREENREADER':
                // "Narrator, the screen reader that is included with Windows, does not set the SPI_SETSCREENREADER or SPI_GETSCREENREADER flags."
                bool enabled = false;
                if (SystemParametersInfo(SPI_GETSCREENREADER, 0, ref enabled, 0))
                {
                    _screenReaderActive = enabled;
                }
            }

            return _screenReaderActive.Value;
        }

        private static bool LoadPSReadline()
        {
            // Don't load PSReadline if:
            //   * we don't think the process will be interactive, e.g. -command or -file
            //     - exception: when -noexit is specified, we will be interactive after the command/file finishes
            //   * -noninteractive: this should be obvious, they've asked that we don't ever prompt
            //
            // Note that PSReadline doesn't support redirected stdin/stdout, but we don't check that here because
            // a future version might, and we should automatically load it at that unknown point in the future.
            // PSReadline will ideally fall back to Console.ReadLine or whatever when stdin/stdout is redirected.
            return ((s_cpp.InitialCommand == null && s_cpp.File == null) || s_cpp.NoExit) && !s_cpp.NonInteractive;
        }

        
        private void DoCreateRunspace(RunspaceCreationEventArgs args)
        {
            Dbg.Assert(_runspaceRef == null, "_runspaceRef field should be null");
            Dbg.Assert(DefaultInitialSessionState != null, "DefaultInitialSessionState should not be null");
            s_runspaceInitTracer.WriteLine("Calling RunspaceFactory.CreateRunspace");

            // Use session configuration file if provided.
            bool customConfigurationProvided = false;
            if (!string.IsNullOrEmpty(args.ConfigurationFilePath))
            {
                try
                {
                    // Replace DefaultInitialSessionState with the initial state configuration defined by the file.
                    DefaultInitialSessionState = InitialSessionState.CreateFromSessionConfigurationFile(
                        path: args.ConfigurationFilePath,
                        roleVerifier: null,
                        validateFile: true);
                }
                catch (Exception ex)
                {
                    throw new ConsoleHostStartupException(ConsoleHostStrings.ShellCannotBeStarted, ex);
                }

                customConfigurationProvided = true;
            }

            try
            {
                Runspace consoleRunspace = null;
                bool psReadlineFailed = false;

                // Load PSReadline by default unless there is no use:
                //    - screen reader is active, such as NVDA, indicating non-visual access
                //    - we're running a command/file and just exiting
                //    - stdin is redirected by a parent process
                //    - we're not interactive
                //    - we're explicitly reading from stdin (the '-' argument)
                // It's also important to have a scenario where PSReadline is not loaded so it can be updated, e.g.
                //    powershell -command "Update-Module PSReadline"
                // This should work just fine as long as no other instances of PowerShell are running.
                ReadOnlyCollection<ModuleSpecification> defaultImportModulesList = null;
                if (!customConfigurationProvided && LoadPSReadline())
                {
                    if (IsScreenReaderActive())
                    {
                        s_theConsoleHost.UI.WriteLine(ManagedEntranceStrings.PSReadLineDisabledWhenScreenReaderIsActive);
                        s_theConsoleHost.UI.WriteLine();
                    }
                    else
                    {
                        // Create and open Runspace with PSReadline.
                        defaultImportModulesList = DefaultInitialSessionState.Modules;
                        DefaultInitialSessionState.ImportPSModule(new[] { "PSReadLine" });
                        consoleRunspace = RunspaceFactory.CreateRunspace(this, DefaultInitialSessionState);
                        try
                        {
                            OpenConsoleRunspace(consoleRunspace, args.StaMode);
                        }
                        catch (Exception)
                        {
                            consoleRunspace = null;
                            psReadlineFailed = true;
                        }
                    }
                }

                if (consoleRunspace == null)
                {
                    if (psReadlineFailed)
                    {
                        // Try again but without importing the PSReadline module.
                        DefaultInitialSessionState.ClearPSModules();
                        DefaultInitialSessionState.ImportPSModule(defaultImportModulesList);
                    }

                    consoleRunspace = RunspaceFactory.CreateRunspace(this, DefaultInitialSessionState);
                    OpenConsoleRunspace(consoleRunspace, args.StaMode);
                }

                Runspace.PrimaryRunspace = consoleRunspace;
                _runspaceRef = new RunspaceRef(consoleRunspace);

                if (psReadlineFailed)
                {
                    // Notify the user that PSReadline could not be loaded.
                    Console.Error.WriteLine(ConsoleHostStrings.CannotLoadPSReadline);
                }
            }
            catch (Exception e)
            {
                // no need to do CheckForSevereException here
                // since the ConsoleHostStartupException is uncaught
                // higher in the call stack and the whole process
                // will exit soon
                throw new ConsoleHostStartupException(ConsoleHostStrings.ShellCannotBeStarted, e);
            }
            finally
            {
                // Stop PerfTrack
                
            }


            DoRunspaceInitialization(args);
        }

        private static void OpenConsoleRunspace(Runspace runspace, bool staMode)
        {
            if (staMode && Platform.IsWindowsDesktop)
            {
                runspace.ApartmentState = ApartmentState.STA;
            }

            runspace.ThreadOptions = PSThreadOptions.ReuseThread;
            runspace.EngineActivityId = EtwActivity.GetActivityId();

            s_runspaceInitTracer.WriteLine("Calling Runspace.Open");
            runspace.Open();
        }

        private void DoRunspaceInitialization(RunspaceCreationEventArgs args)
        {
            if (_runspaceRef.Runspace.Debugger != null)
            {
                _runspaceRef.Runspace.Debugger.SetDebugMode(DebugModes.LocalScript | DebugModes.RemoteScript);
                _runspaceRef.Runspace.Debugger.DebuggerStop += this.OnExecutionSuspended;
            }

            Executor exec = new Executor(this, false, false);

            // If working directory was specified, set it
            if (s_cpp != null && s_cpp.WorkingDirectory != null)
            {
                Pipeline tempPipeline = exec.CreatePipeline();
                var command = new Command("Set-Location");
                command.Parameters.Add("LiteralPath", s_cpp.WorkingDirectory);
                tempPipeline.Commands.Add(command);

                Exception exception;
                if (IsRunningAsync)
                {
                    exec.ExecuteCommandAsyncHelper(tempPipeline, out exception, Executor.ExecutionOptions.AddOutputter);
                }
                else
                {
                    exec.ExecuteCommandHelper(tempPipeline, out exception, Executor.ExecutionOptions.AddOutputter);
                }

                if (exception != null)
                {
                    _lastRunspaceInitializationException = exception;
                    ReportException(exception, exec);
                }
            }

            if (!string.IsNullOrEmpty(args.ConfigurationName))
            {
                // If an endpoint configuration is specified then create a loop-back remote runspace targeting
                // the endpoint and push onto runspace ref stack.  Ignore profile and configuration scripts.
                try
                {
                    RemoteRunspace remoteRunspace = HostUtilities.CreateConfiguredRunspace(args.ConfigurationName, this);
                    remoteRunspace.ShouldCloseOnPop = true;
                    PushRunspace(remoteRunspace);

                    // Ensure that session ends when configured remote runspace is popped.
                    _inPushedConfiguredSession = true;
                }
                catch (Exception e)
                {
                    throw new ConsoleHostStartupException(ConsoleHostStrings.ShellCannotBeStarted, e);
                }
            }
            else
            {
                // const string shellId = "Microsoft.PowerShell";

                // If the system lockdown policy says "Enforce", do so. Do this after types / formatting, default functions, etc
                // are loaded so that they are trusted. (Validation of their signatures is done in F&O).
                var languageMode = Utils.EnforceSystemLockDownLanguageMode(_runspaceRef.Runspace.ExecutionContext);
                // When displaying banner, also display the language mode if running in any restricted mode.
                if (s_cpp.ShowBanner)
                {
                    switch (languageMode)
                    {
                        case PSLanguageMode.ConstrainedLanguage:
                            if (SystemPolicy.GetSystemLockdownPolicy() != SystemEnforcementMode.Audit)
                            {
                                s_theConsoleHost.UI.WriteLine(ManagedEntranceStrings.ShellBannerCLMode);
                            }
                            else
                            {
                                s_theConsoleHost.UI.WriteLine(ManagedEntranceStrings.ShellBannerCLAuditMode);
                            }

                            break;

                        case PSLanguageMode.NoLanguage:
                            s_theConsoleHost.UI.WriteLine(ManagedEntranceStrings.ShellBannerNLMode);
                            break;

                        case PSLanguageMode.RestrictedLanguage:
                            s_theConsoleHost.UI.WriteLine(ManagedEntranceStrings.ShellBannerRLMode);
                            break;

                        default:
                            break;
                    }
                }

                string currentUserProfile = Platform.ConfigDirectory + "/profile.ps1";

                // $PROFILE has to be set from the host
                // Should be "per-user,host-specific profile.ps1"
                // This should be set even if -noprofile is specified
                _runspaceRef.Runspace.SessionStateProxy.SetVariable("PROFILE", currentUserProfile);

                if (!args.SkipProfiles)
                {
                    // Run the profiles.
                    // Profiles are run in the following order:
                    // 1. host independent profile meant for all users
                    // 2. host specific profile meant for all users
                    // 3. host independent profile of the current user
                    // 4. host specific profile of the current user

                    // var sw = new Stopwatch();
                    // sw.Start();
                    // RunProfile(allUsersProfile, exec);
                    // RunProfile(allUsersHostSpecificProfile, exec);
                    RunProfile(currentUserProfile, exec);
                    // RunProfile(currentUserHostSpecificProfile, exec);
                    // sw.Stop();

                    // var profileLoadTimeInMs = sw.ElapsedMilliseconds;
                    // if (s_cpp.ShowBanner && !s_cpp.NoProfileLoadTime && profileLoadTimeInMs > 500)
                    // {
                    //     Console.Error.WriteLine(ConsoleHostStrings.SlowProfileLoadingMessage, profileLoadTimeInMs);
                    // }

                    // _profileLoadTimeInMS = profileLoadTimeInMs;
                }
                else
                {
                    s_tracer.WriteLine("-noprofile option specified: skipping profiles");
                }
            }
#if LEGACYTELEMETRY
            // Startup is reported after possibly running the profile, but before running the initial command (or file)
            // if one is specified.
            TelemetryAPI.ReportStartupTelemetry(this);
#endif

            // If a file was specified as the argument to run, then run it...
            if (s_cpp != null && s_cpp.File != null)
            {
                string filePath = s_cpp.File;

                s_tracer.WriteLine("running -file '{0}'", filePath);

                Pipeline tempPipeline = exec.CreatePipeline();
                Command c;
#if UNIX
                // if file doesn't have .ps1 extension, we read the contents and treat it as a script to support shebang with no .ps1 extension usage
                if (!filePath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                {
                    string script = File.ReadAllText(filePath);
                    c = new Command(script, isScript: true, useLocalScope: false);
                }
                else
#endif
                {
                    c = new Command(filePath, false, false);
                }

                tempPipeline.Commands.Add(c);

                if (args.InitialCommandArgs != null)
                {
                    // add the args passed to the command.

                    foreach (CommandParameter p in args.InitialCommandArgs)
                    {
                        c.Parameters.Add(p);
                    }
                }

                // If we're not going to continue, then get the exit code out of the runspace and
                // and indicate that it should be returned...
                if (!_noExit && this.Runspace is not RemoteRunspace)
                {
                    this.Runspace.ExecutionContext.ScriptCommandProcessorShouldRethrowExit = true;
                }

                Exception e1;

                if (IsRunningAsync)
                {
                    Executor.ExecutionOptions executionOptions = Executor.ExecutionOptions.AddOutputter;

                    Token[] tokens;
                    ParseError[] errors;

                    // Detect if they're using input. If so, read from it.
                    Ast parsedInput = Parser.ParseFile(filePath, out tokens, out errors);
                    if (AstSearcher.IsUsingDollarInput(parsedInput))
                    {
                        executionOptions |= Executor.ExecutionOptions.ReadInputObjects;

                        // We will consume all of the input to pass to the script, so don't try to read commands from stdin.
                        ui.ReadFromStdin = false;
                    }

                    exec.ExecuteCommandAsyncHelper(tempPipeline, out e1, executionOptions);
                }
                else
                {
                    exec.ExecuteCommandHelper(tempPipeline, out e1, Executor.ExecutionOptions.AddOutputter);
                }

                // Pipeline.Invoke has thrown, that's bad. It means the script did not actually
                // execute properly. These exceptions should be reflected in the exit code
                if (e1 != null)
                {
                    if (!_noExit)
                    {
                        // Set ExitCode to 0x1
                        lock (hostGlobalLock)
                        {
                            _setShouldExitCalled = true;
                            _exitCodeFromRunspace = 0x1;
                            ShouldEndSession = true;
                        }
                    }

                    ReportException(e1, exec);
                }
            }
            else if (!string.IsNullOrEmpty(args.InitialCommand))
            {
                // Run the command passed on the command line

                s_tracer.WriteLine("running initial command");

                Pipeline tempPipeline = exec.CreatePipeline(args.InitialCommand, true);

                if (args.InitialCommandArgs != null)
                {
                    // add the args passed to the command.

                    foreach (CommandParameter p in args.InitialCommandArgs)
                    {
                        tempPipeline.Commands[0].Parameters.Add(p);
                    }
                }

                Exception e1;

                if (IsRunningAsync)
                {
                    Executor.ExecutionOptions executionOptions = Executor.ExecutionOptions.AddOutputter;

                    Token[] tokens;
                    ParseError[] errors;

                    // Detect if they're using input. If so, read from it.
                    Ast parsedInput = Parser.ParseInput(args.InitialCommand, out tokens, out errors);
                    if (AstSearcher.IsUsingDollarInput(parsedInput))
                    {
                        executionOptions |= Executor.ExecutionOptions.ReadInputObjects;

                        // We will consume all of the input to pass to the script, so don't try to read commands from stdin.
                        ui.ReadFromStdin = false;
                    }

                    exec.ExecuteCommandAsyncHelper(tempPipeline, out e1, executionOptions);
                }
                else
                {
                    exec.ExecuteCommandHelper(tempPipeline, out e1, Executor.ExecutionOptions.AddOutputter);
                }

                if (e1 != null)
                {
                    // Remember last exception
                    _lastRunspaceInitializationException = e1;
                    ReportException(e1, exec);
                }
            }
        }

        private void RunProfile(string profileFileName, Executor exec)
        {
            if (!string.IsNullOrEmpty(profileFileName))
            {
                s_runspaceInitTracer.WriteLine("checking profile" + profileFileName);

                try
                {
                    if (File.Exists(profileFileName))
                    {
                        InitializeRunspaceHelper(
                            ". '" + EscapeSingleQuotes(profileFileName) + "'",
                            exec,
                            Executor.ExecutionOptions.AddOutputter);
                    }
                    else
                    {
                        s_runspaceInitTracer.WriteLine("profile file not found");
                    }
                }
                catch (Exception e) // Catch-all OK, 3rd party callout
                {
                    ReportException(e, exec);

                    s_runspaceInitTracer.WriteLine("Could not load profile.");
                }
            }
        }

        
        internal static string EscapeSingleQuotes(string str)
        {
            // worst case we have to escape every character, so capacity is twice as large as input length
            StringBuilder sb = new StringBuilder(str.Length * 2);

            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                if (c == '\'')
                {
                    sb.Append(c);
                }

                sb.Append(c);
            }

            string result = sb.ToString();

            return result;
        }

        private void WriteErrorLine(string line)
        {
            const ConsoleColor fg = ConsoleColor.Red;
            ConsoleColor bg = UI.RawUI.BackgroundColor;

            UI.WriteLine(fg, bg, line);
        }

        // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
        // Removed HandleUnexpectedExceptions infrastructure
        private void ReportException(Exception e, Executor exec)
        {
            Dbg.Assert(e != null, "must supply an Exception");
            Dbg.Assert(exec != null, "must supply an Executor");

            // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
            // Removed HandleUnexpectedExceptions infrastructure

            // Attempt to write the exception into the error stream so that the normal F&O machinery will
            // display it according to preferences.

            object error = null;
            Pipeline tempPipeline = exec.CreatePipeline();

            // NTRAID#Windows OS Bugs-1143621-2005/04/08-sburns

            if (e is IContainsErrorRecord icer)
            {
                error = icer.ErrorRecord;
            }
            else
            {
                error = (object)new ErrorRecord(e, "ConsoleHost.ReportException", ErrorCategory.NotSpecified, null);
            }

            PSObject wrappedError = new PSObject(error)
            {
                WriteStream = WriteStreamType.Error
            };

            Exception e1 = null;

            tempPipeline.Input.Write(wrappedError);
            if (IsRunningAsync)
            {
                exec.ExecuteCommandAsyncHelper(tempPipeline, out e1, Executor.ExecutionOptions.AddOutputter);
            }
            else
            {
                exec.ExecuteCommandHelper(tempPipeline, out e1, Executor.ExecutionOptions.AddOutputter);
            }

            if (e1 != null)
            {
                // that didn't work.  Write out the error ourselves as a last resort.

                ReportExceptionFallback(e, null);
            }
        }

        
        private void ReportExceptionFallback(Exception e, string header)
        {
            if (!string.IsNullOrEmpty(header))
            {
                Console.Error.WriteLine(header);
            }

            if (e == null)
            {
                return;
            }

            // See if the exception has an error record attached to it...
            ErrorRecord er = null;
            if (e is IContainsErrorRecord icer)
                er = icer.ErrorRecord;

            if (e is PSRemotingTransportException)
            {
                // For remoting errors use full fidelity error writer.
                UI.WriteErrorLine(e.Message);
            }
            else if (e is TargetInvocationException)
            {
                Console.Error.WriteLine(e.InnerException.Message);
            }
            else
            {
                Console.Error.WriteLine(e.Message);
            }

            // Add the position message for the error if it's available.
            if (er != null && er.InvocationInfo != null)
                Console.Error.WriteLine(er.InvocationInfo.PositionMessage);
        }

        
        internal event EventHandler RunspacePopped;

        
        internal event EventHandler RunspacePushed;

        #endregion non-overrides

        #region debugger

        
        private void OnExecutionSuspended(object sender, DebuggerStopEventArgs e)
        {
            // Check local runspace internalHost to see if debugging is enabled.
            LocalRunspace localrunspace = LocalRunspace;
            if ((localrunspace != null) && !localrunspace.ExecutionContext.EngineHostInterface.DebuggerEnabled)
            {
                return;
            }

            _debuggerStopEventArgs = e;
            InputLoop baseLoop = null;

            try
            {
                if (this.IsRunspacePushed)
                {
                    // For remote debugging block data coming from the main (not-nested)
                    // running command.
                    baseLoop = InputLoop.GetNonNestedLoop();
                    baseLoop?.BlockCommandOutput();
                }

                //
                // Display the banner only once per session
                //
                if (_displayDebuggerBanner)
                {
                    WriteDebuggerMessage(ConsoleHostStrings.EnteringDebugger);
                    WriteDebuggerMessage(string.Empty);
                    _displayDebuggerBanner = false;
                }

                //
                // If we hit a breakpoint output its info
                //
                if (e.Breakpoints.Count > 0)
                {
                    string format = ConsoleHostStrings.HitBreakpoint;

                    foreach (Breakpoint breakpoint in e.Breakpoints)
                    {
                        WriteDebuggerMessage(string.Format(CultureInfo.CurrentCulture, format, breakpoint));
                    }

                    WriteDebuggerMessage(string.Empty);
                }

                //
                // Write the source line
                //
                if (e.InvocationInfo != null)
                {
                    //    line = StringUtil.Format(ConsoleHostStrings.DebuggerSourceCodeFormat, scriptFileName, e.InvocationInfo.ScriptLineNumber, e.InvocationInfo.Line);
                    WriteDebuggerMessage(e.InvocationInfo.PositionMessage);
                }

                //
                // Start the debug mode
                //
                EnterDebugMode();
            }
            finally
            {
                _debuggerStopEventArgs = null;
                baseLoop?.ResumeCommandOutput();
            }
        }

        
        private bool InDebugMode { get; set; }

        
        internal bool DebuggerCanStopCommand
        {
            get;
            set;
        }

        private Exception _lastRunspaceInitializationException = null;
        internal uint ExitCode;

        
        private void EnterDebugMode()
        {
            InDebugMode = true;

            try
            {
                //
                // Note that we need to enter the nested prompt via the InternalHost interface.
                //

                // EnterNestedPrompt must always be run on the local runspace.
                Runspace runspace = _runspaceRef.OldRunspace ?? this.RunspaceRef.Runspace;
                runspace.ExecutionContext.EngineHostInterface.EnterNestedPrompt();
            }
            catch (PSNotImplementedException)
            {
                WriteDebuggerMessage(ConsoleHostStrings.SessionDoesNotSupportDebugger);
            }
            finally
            {
                InDebugMode = false;
            }
        }

        
        private void ExitDebugMode(DebuggerResumeAction resumeAction)
        {
            _debuggerStopEventArgs.ResumeAction = resumeAction;

            try
            {
                //
                // Note that we need to exit the nested prompt via the InternalHost interface.
                //

                // ExitNestedPrompt must always be run on the local runspace.
                Runspace runspace = _runspaceRef.OldRunspace ?? this.RunspaceRef.Runspace;
                runspace.ExecutionContext.EngineHostInterface.ExitNestedPrompt();
            }
            catch (ExitNestedPromptException)
            {
                // ignore the exception
            }
        }

        
        private void WriteDebuggerMessage(string line)
        {
            this.ui.WriteLine(this.ui.DebugForegroundColor, this.ui.DebugBackgroundColor, line);
        }

        #endregion debugger

        #region aux classes

        
        private sealed class InputLoop
        {
            internal static void RunNewInputLoop(ConsoleHost parent, bool isNested)
            {
                // creates an instance and adds it to the stack and starts it running.

                int stackCount = s_instanceStack.Count;

                if (stackCount == PSHost.MaximumNestedPromptLevel)
                {
                    throw PSTraceSource.NewInvalidOperationException(ConsoleHostStrings.TooManyNestedPromptsError);
                }

                InputLoop il = new InputLoop(parent, isNested);

                s_instanceStack.Push(il);
                il.Run(s_instanceStack.Count > 1);

                // Once the loop has finished running, remove it from the instance stack.

                InputLoop il2 = s_instanceStack.Pop();

                Dbg.Assert(il == il2, "top of instance stack does not correspond to the instance pushed");
            }

            // Presently, this will not work if the Run loop is blocked on a ReadLine call.  Whether that's a
            // problem or not depends on when we expect calls to this function to be made.
            
            internal static bool ExitCurrentLoop()
            {
                if (s_instanceStack.Count == 0)
                {
                    throw PSTraceSource.NewInvalidOperationException(ConsoleHostStrings.InputExitCurrentLoopOutOfSyncError);
                }

                InputLoop il = s_instanceStack.Peek();
                il._shouldExit = true;

                // The main (non-nested) input loop has Count == 1,
                // so Count == 2 is the value that indicates the next
                // popped stack input loop is non-nested.
                return (s_instanceStack.Count > 2);
            }

            
            internal static InputLoop GetNonNestedLoop()
            {
                if (s_instanceStack.Count == 1)
                {
                    return s_instanceStack.Peek();
                }

                return null;
            }

            private InputLoop(ConsoleHost parent, bool isNested)
            {
                _parent = parent;
                _isNested = isNested;
                _isRunspacePushed = parent.IsRunspacePushed;
                parent.RunspacePopped += HandleRunspacePopped;
                parent.RunspacePushed += HandleRunspacePushed;
                _exec = new Executor(parent, isNested, false);
                _promptExec = new Executor(parent, isNested, true);
            }

            private void HandleRunspacePushed(object sender, EventArgs e)
            {
                lock (_syncObject)
                {
                    _isRunspacePushed = true;
                    _runspacePopped = false;
                }
            }

            
            private void HandleRunspacePopped(object sender, EventArgs eventArgs)
            {
                lock (_syncObject)
                {
                    _isRunspacePushed = false;
                    _runspacePopped = true;
                }
            }

            // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
            // Removed HandleUnexpectedExceptions infrastructure
            
            internal void Run(bool inputLoopIsNested)
            {
                PSHostUserInterface c = _parent.UI;
                ConsoleHostUserInterface ui = c as ConsoleHostUserInterface;

                Dbg.Assert(ui != null, "Host.UI should return an instance.");

                bool inBlockMode = false;
                // Use nullable so that we don't evaluate suggestions at startup.
                bool? previousResponseWasEmpty = null;
                var inputBlock = new StringBuilder();

                while (!_parent.ShouldEndSession && !_shouldExit)
                {
                    try
                    {
                        _parent._isRunningPromptLoop = true;

                        string prompt = null;
                        string line = null;

                        if (!ui.NoPrompt)
                        {
                            if (inBlockMode)
                            {
                                // use a special prompt that denotes block mode
                                prompt = ">> ";
                            }
                            else
                            {
                                // Make sure the cursor is at the start of the line - some external programs don't
                                // write a newline, so we do that for them.
                                if (ui.RawUI.CursorPosition.X != 0)
                                    ui.WriteLine();

                                // Evaluate any suggestions
                                if (previousResponseWasEmpty == false)
                                {
                                    {
                                        EvaluateSuggestions(ui);
                                    }
                                }

                                // Then output the prompt
                                if (_parent.InDebugMode)
                                {
                                    prompt = EvaluateDebugPrompt();
                                }

                                prompt ??= EvaluatePrompt();
                            }

                            ui.Write(prompt);
                        }

#if UNIX
                        if (c.SupportsVirtualTerminal)
                        {
                            // enable DECCKM as .NET requires cursor keys to emit VT for Console class
                            c.Write(DECCKM_ON);
                        }
#endif

                        previousResponseWasEmpty = false;
                        // There could be a profile. So there could be a user defined custom readline command
                        line = ui.ReadLineWithTabCompletion(_exec);

                        // line will be null in the case that Ctrl-C terminated the input

                        if (line == null)
                        {
                            previousResponseWasEmpty = true;

                            s_tracer.WriteLine("line is null");
                            if (!ui.ReadFromStdin)
                            {
                                // If we're not reading from stdin, the we probably got here
                                // because the user hit ctrl-C. Do a writeline to clean up
                                // the output...
                                ui.WriteLine();
                            }

                            inBlockMode = false;

                            if (Console.IsInputRedirected)
                            {
                                // null is also the result of reading stdin to EOF.
                                _parent.ShouldEndSession = true;
                                break;
                            }

                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            if (inBlockMode)
                            {
                                // end block mode and execute the block accumulated block

                                s_tracer.WriteLine("exiting block mode");
                                line = inputBlock.ToString();
                                inBlockMode = false;
                            }
                            else if (!_parent.InDebugMode)
                            {
                                previousResponseWasEmpty = true;
                                continue;
                            }
                        }
                        else
                        {
                            if (inBlockMode)
                            {
                                s_tracer.WriteLine("adding line to block");
                                inputBlock.Append('\n');
                                inputBlock.Append(line);
                                continue;
                            }
                        }

                        Dbg.Assert(line != null, "line should not be null");
                        Dbg.Assert(line.Length > 0 || _parent.InDebugMode, "line should not be empty unless the host is in debug mode");
                        Dbg.Assert(!inBlockMode, "should not be in block mode at point of pipeline execution");

                        Exception e = null;

                        if (_parent.InDebugMode)
                        {
                            DebuggerCommandResults results = ProcessDebugCommand(line, out e);

                            if (results.ResumeAction != null)
                            {
                                _parent.ExitDebugMode(results.ResumeAction.Value);
                            }

                            if (e != null)
                            {
                                var ex = e as PSInvalidOperationException;
                                if (e is PSRemotingTransportException ||
                                    e is RemoteException ||
                                    (ex != null &&
                                     ex.ErrorRecord != null &&
                                     ex.ErrorRecord.FullyQualifiedErrorId.Equals("Debugger:CannotProcessCommandNotStopped", StringComparison.OrdinalIgnoreCase)))
                                {
                                    // Debugger session is broken.  Exit nested loop.
                                    _parent.ExitDebugMode(DebuggerResumeAction.Continue);
                                }
                                else
                                {
                                    // Handle incomplete parse and other errors.
                                    inBlockMode = HandleErrors(e, line, inBlockMode, ref inputBlock);
                                }
                            }

                            continue;
                        }

                        if (_runspacePopped)
                        {
                            string msg = StringUtil.Format(ConsoleHostStrings.CommandNotExecuted, line);
                            ui.WriteErrorLine(msg);
                            _runspacePopped = false;
                        }
                        else
                        {
#if UNIX
                            if (c.SupportsVirtualTerminal)
                            {
                                // disable DECCKM to standard mode as applications may not expect VT for cursor keys
                                c.Write(DECCKM_OFF);
                            }
#endif

                            if (_parent.IsRunningAsync && !_parent.IsNested)
                            {
                                _exec.ExecuteCommandAsync(line, out e, Executor.ExecutionOptions.AddOutputter | Executor.ExecutionOptions.AddToHistory);
                            }
                            else
                            {
                                _exec.ExecuteCommand(line, out e, Executor.ExecutionOptions.AddOutputter | Executor.ExecutionOptions.AddToHistory);
                            }

                            Thread bht = null;

                            lock (_parent.hostGlobalLock)
                            {
                                bht = _parent._breakHandlerThread;
                            }

                            bht?.Join();

                            // Once the pipeline has been executed, we toss any outstanding progress data and
                            // take down the display.

                            ui.ResetProgress();

                            if (e != null)
                            {
                                // Handle incomplete parse and other errors.
                                inBlockMode = HandleErrors(e, line, inBlockMode, ref inputBlock);

                                // If a remote runspace is pushed and it is not in a good state
                                // then pop it.
                                if (_isRunspacePushed && (_parent.Runspace != null) &&
                                    ((_parent.Runspace.RunspaceStateInfo.State != RunspaceState.Opened) ||
                                     (_parent.Runspace.RunspaceAvailability != RunspaceAvailability.Available)))
                                {
                                    _parent.PopRunspace();
                                }
                            }

#if LEGACYTELEMETRY
                            if (!inBlockMode)
                                s_theConsoleHost._interactiveCommandCount += 1;
#endif
                        }
                    }

                    // NTRAID#Windows Out Of Band Releases-915506-2005/09/09
                    // Removed HandleUnexpectedExceptions infrastructure
                    finally
                    {
                        _parent._isRunningPromptLoop = false;
                    }
                }
            }

            internal void BlockCommandOutput()
            {
                if (_parent.runningCmd is RemotePipeline rCmdPipeline)
                {
                    rCmdPipeline.DrainIncomingData();
                    rCmdPipeline.SuspendIncomingData();
                }
                else
                {
                    _exec.BlockCommandOutput();
                }
            }

            internal void ResumeCommandOutput()
            {
                if (_parent.runningCmd is RemotePipeline rCmdPipeline)
                {
                    rCmdPipeline.ResumeIncomingData();
                }
                else
                {
                    _exec.ResumeCommandOutput();
                }
            }

            private bool HandleErrors(Exception e, string line, bool inBlockMode, ref StringBuilder inputBlock)
            {
                Dbg.Assert(e != null, "Exception reference should not be null.");

                if (IsIncompleteParseException(e))
                {
                    if (!inBlockMode)
                    {
                        inBlockMode = true;
                        inputBlock = new StringBuilder(line);
                    }
                    else
                    {
                        inputBlock.Append(line);
                    }
                }
                else
                {
                    // an exception occurred when the command was executed.  Tell the user about it.
                    _parent.ReportException(e, _exec);
                }

                return inBlockMode;
            }

            private DebuggerCommandResults ProcessDebugCommand(string cmd, out Exception e)
            {
                DebuggerCommandResults results = null;

                try
                {
                    _parent.DebuggerCanStopCommand = true;

                    // Use PowerShell object to write streaming data to host.
                    using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
                    {
                        PSInvocationSettings settings = new PSInvocationSettings()
                        {
                            Host = _parent
                        };

                        PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();
                        ps.AddCommand("Out-Default");
                        IAsyncResult async = ps.BeginInvoke<PSObject>(output, settings, null, null);

                        // Let debugger evaluate command and stream output data.
                        results = _parent.Runspace.Debugger.ProcessCommand(
                            new PSCommand(
                                new Command(cmd, true)),
                            output);

                        output.Complete();
                        ps.EndInvoke(async);
                    }

                    e = null;
                }
                catch (Exception ex)
                {
                    e = ex;
                    results = new DebuggerCommandResults(null, false);
                }
                finally
                {
                    _parent.DebuggerCanStopCommand = false;
                }

                // Exit debugger if command fails to evaluate.
                return results ?? new DebuggerCommandResults(DebuggerResumeAction.Continue, false);
            }

            private static bool IsIncompleteParseException(Exception e)
            {
                // Check e's type.
                if (e is IncompleteParseException)
                {
                    return true;
                }

                // If it is remote exception ferret out the real exception.
                if (e is not RemoteException remoteException || remoteException.ErrorRecord == null)
                {
                    return false;
                }

                return remoteException.ErrorRecord.CategoryInfo.Reason == nameof(IncompleteParseException);
            }

            private void EvaluateFeedbacks(ConsoleHostUserInterface ui)
            {
                // Output any training suggestions
                try
                {
                    List<FeedbackResult> feedbacks = FeedbackHub.GetFeedback(_parent.Runspace);
                    if (feedbacks is null || feedbacks.Count is 0)
                    {
                        return;
                    }

                    HostUtilities.RenderFeedback(feedbacks, ui);
                }
                catch (Exception e)
                {
                    // Catch-all OK. This is a third-party call-out.
                    ui.WriteErrorLine(e.Message);

                    LocalRunspace localRunspace = (LocalRunspace)_parent.Runspace;
                    localRunspace.GetExecutionContext.AppendDollarError(e);
                }
            }

            private void EvaluateSuggestions(ConsoleHostUserInterface ui)
            {
                // Output any training suggestions
                try
                {
                    List<string> suggestions = HostUtilities.GetSuggestion(_parent.Runspace);

                    if (suggestions.Count > 0)
                    {
                        ui.WriteLine();
                    }

                    bool first = true;
                    foreach (string suggestion in suggestions)
                    {
                        if (!first)
                            ui.WriteLine();

                        ui.WriteLine(suggestion);

                        first = false;
                    }
                }
                catch (TerminateException)
                {
                    // A variable breakpoint may be hit by HostUtilities.GetSuggestion. The debugger throws TerminateExceptions to stop the execution
                    // of the current statement; we do not want to treat these exceptions as errors.
                }
                catch (Exception e)
                {
                    // Catch-all OK. This is a third-party call-out.
                    ui.WriteErrorLine(e.Message);

                    LocalRunspace localRunspace = (LocalRunspace)_parent.Runspace;
                    localRunspace.GetExecutionContext.AppendDollarError(e);
                }
            }

            private string EvaluatePrompt()
            {
                string promptString = _promptExec.ExecuteCommandAndGetResultAsString("prompt", out _);

                if (string.IsNullOrEmpty(promptString))
                {
                    promptString = ConsoleHostStrings.DefaultPrompt;
                }

                // Check for the pushed runspace scenario.
                if (_isRunspacePushed)
                {
                    if (_parent.Runspace is RemoteRunspace remoteRunspace)
                    {
                        promptString = HostUtilities.GetRemotePrompt(remoteRunspace, promptString, _parent._inPushedConfiguredSession);
                    }
                }
                else
                {
                    if (_runspacePopped)
                    {
                        _runspacePopped = false;
                    }
                }

                // Return composed prompt string.
                return promptString;
            }

            private string EvaluateDebugPrompt()
            {
                PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

                try
                {
                    _parent.Runspace.Debugger.ProcessCommand(
                        new PSCommand(new Command("prompt")),
                        output);
                }
                catch (Exception ex)
                {
                    _parent.ReportException(ex, _exec);
                }

                PSObject prompt = output.ReadAndRemoveAt0();
                string promptString = (prompt != null) ? (prompt.BaseObject as string) : null;
                if (promptString != null && _parent.Runspace is RemoteRunspace remoteRunspace)
                {
                    promptString = HostUtilities.GetRemotePrompt(remoteRunspace, promptString, _parent._inPushedConfiguredSession);
                }

                return promptString;
            }

            private readonly ConsoleHost _parent;
            private readonly bool _isNested;
            private bool _shouldExit;
            private readonly Executor _exec;
            private readonly Executor _promptExec;
            private readonly object _syncObject = new object();
            private bool _isRunspacePushed = false;
            private bool _runspacePopped = false;

            // The instance stack is used to keep track of which InputLoop instance should be told to exit
            // when PSHost.ExitNestedPrompt is called.

            // threadsafety guaranteed by enclosing class

            private static readonly Stack<InputLoop> s_instanceStack = new Stack<InputLoop>();
        }

        [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Justification =
            "This exception cannot be used outside of the console host application. It is not thrown by a library routine, only by an application.")]
        private sealed class ConsoleHostStartupException : Exception
        {
            internal
            ConsoleHostStartupException()
                : base()
            {
            }

            internal
            ConsoleHostStartupException(string message)
                : base(message)
            {
            }

            internal
            ConsoleHostStartupException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        #endregion aux classes

        
        private RunspaceRef _runspaceRef;

#if !UNIX
        private GCHandle breakHandlerGcHandle;

        // Set to Unknown so that we avoid saving/restoring the console mode if we don't have a console.
        private ConsoleControl.ConsoleModes _savedConsoleMode = ConsoleControl.ConsoleModes.Unknown;
        private readonly ConsoleControl.ConsoleModes _initialConsoleMode = ConsoleControl.ConsoleModes.Unknown;
#endif
        private Thread _breakHandlerThread;
        private bool _isDisposed;
        internal ConsoleHostUserInterface ui;

        internal Lazy<TextReader> ConsoleIn { get; } = new Lazy<TextReader>(static () => Console.In);

        private string _savedWindowTitle = string.Empty;
        private readonly Version _ver = PSVersionInfo.PSVersion;
        private int _exitCodeFromRunspace;
        private bool _noExit = true;
        private bool _setShouldExitCalled;
        private bool _isRunningPromptLoop;
        private bool _wasInitialCommandEncoded;
        private bool? _screenReaderActive;

        // hostGlobalLock is used to sync public method calls (in case multiple threads call into the host) and access to
        // state that persists across method calls, like progress data. It's internal because the ui object also
        // uses this same object.

        internal object hostGlobalLock = new object();

        // These members are possibly accessed from multiple threads (the break handler thread, a pipeline thread, or the main
        // thread). We use hostGlobalLock to sync access to them.

        private bool _shouldEndSession;
        private int _beginApplicationNotifyCount;

        private readonly ConsoleTextWriter _consoleWriter;
        private WrappedSerializer _outputSerializer;
        private WrappedSerializer _errorSerializer;
        private bool _displayDebuggerBanner;
        private DebuggerStopEventArgs _debuggerStopEventArgs;
        private bool _inPushedConfiguredSession;
        internal Pipeline runningCmd;

        // The ConsoleHost class is a singleton.  Note that there is not a thread-safety issue with these statics as there can
        // only be one console host per process.

        private static ConsoleHost s_theConsoleHost;

        internal static InitialSessionState DefaultInitialSessionState;

        [TraceSource("ConsoleHost", "ConsoleHost subclass of S.M.A.PSHost")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("ConsoleHost", "ConsoleHost subclass of S.M.A.PSHost");

        [TraceSource("ConsoleHostRunspaceInit", "Initialization code for ConsoleHost's Runspace")]
        private static readonly PSTraceSource s_runspaceInitTracer =
            PSTraceSource.GetTracer("ConsoleHostRunspaceInit", "Initialization code for ConsoleHost's Runspace", false);
    }

    
    internal sealed class RunspaceCreationEventArgs : EventArgs
    {
        
        internal RunspaceCreationEventArgs(
            string initialCommand,
            bool skipProfiles,
            bool staMode,
            string configurationName,
            string configurationFilePath,
            Collection<CommandParameter> initialCommandArgs)
        {
            InitialCommand = initialCommand;
            SkipProfiles = skipProfiles;
            StaMode = staMode;
            ConfigurationName = configurationName;
            ConfigurationFilePath = configurationFilePath;
            InitialCommandArgs = initialCommandArgs;
        }

        internal string InitialCommand { get; set; }

        internal bool SkipProfiles { get; set; }

        internal bool StaMode { get; set; }

        internal string ConfigurationName { get; set; }

        internal string ConfigurationFilePath { get; set; }

        internal Collection<CommandParameter> InitialCommandArgs { get; set; }
    }
}   // namespace
