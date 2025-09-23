// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Remoting.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.PSTasks
{
    #region PSTask

    
    internal sealed class PSTask : PSTaskBase
    {
        #region Members

        private readonly PSTaskDataStreamWriter _dataStreamWriter;

        #endregion

        #region Constructor

        
        public PSTask(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            string currentLocationPath,
            PSTaskDataStreamWriter dataStreamWriter)
            : base(
                scriptBlock,
                usingValuesMap,
                dollarUnderbar,
                currentLocationPath)
        {
            _dataStreamWriter = dataStreamWriter;
        }

        #endregion

        #region Overrides

        
        protected override void InitializePowershell()
        {
            // Writer data stream handlers
            _output.DataAdded += (sender, args) => HandleOutputData();
            _powershell.Streams.Error.DataAdded += (sender, args) => HandleErrorData();
            _powershell.Streams.Warning.DataAdded += (sender, args) => HandleWarningData();
            _powershell.Streams.Verbose.DataAdded += (sender, args) => HandleVerboseData();
            _powershell.Streams.Debug.DataAdded += (sender, args) => HandleDebugData();
            _powershell.Streams.Progress.DataAdded += (sender, args) => HandleProgressData();
            _powershell.Streams.Information.DataAdded += (sender, args) => HandleInformationData();

            // State change handler
            _powershell.InvocationStateChanged += (sender, args) => HandleStateChanged(args);
        }

        #endregion

        #region Writer data stream handlers

        private void HandleOutputData()
        {
            foreach (var item in _output.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Output, item));
            }
        }

        private void HandleErrorData()
        {
            foreach (var item in _powershell.Streams.Error.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Error, item));
            }
        }

        private void HandleWarningData()
        {
            foreach (var item in _powershell.Streams.Warning.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Warning, item.Message));
            }
        }

        private void HandleVerboseData()
        {
            foreach (var item in _powershell.Streams.Verbose.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Verbose, item.Message));
            }
        }

        private void HandleDebugData()
        {
            foreach (var item in _powershell.Streams.Debug.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Debug, item.Message));
            }
        }

        private void HandleInformationData()
        {
            foreach (var item in _powershell.Streams.Information.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Information, item));
            }
        }

        private void HandleProgressData()
        {
            foreach (var item in _powershell.Streams.Progress.ReadAll())
            {
                _dataStreamWriter.Add(
                    new PSStreamObject(PSStreamObjectType.Progress, item));
            }
        }

        #endregion

        #region Event handlers

        private void HandleStateChanged(PSInvocationStateChangedEventArgs stateChangeInfo)
        {
            if (_dataStreamWriter != null)
            {
                // Treat any terminating exception as a non-terminating error record
                var newStateInfo = stateChangeInfo.InvocationStateInfo;
                if (newStateInfo.Reason != null)
                {
                    var errorRecord = new ErrorRecord(
                        newStateInfo.Reason,
                        "PSTaskException",
                        ErrorCategory.InvalidOperation,
                        this);

                    _dataStreamWriter.Add(
                        new PSStreamObject(PSStreamObjectType.Error, errorRecord));
                }
            }

            RaiseStateChangedEvent(stateChangeInfo);
        }

        #endregion
    }

    
    internal sealed class PSJobTask : PSTaskBase
    {
        #region Members

        private readonly Job _job;

        #endregion

        #region Constructor

        
        public PSJobTask(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            string currentLocationPath,
            Job job) : base(
                scriptBlock,
                usingValuesMap,
                dollarUnderbar,
                currentLocationPath)
        {
            _job = job;
        }

        #endregion

        #region Overrides

        
        protected override void InitializePowershell()
        {
            // Job data stream handlers
            _output.DataAdded += (sender, args) => HandleJobOutputData();
            _powershell.Streams.Error.DataAdded += (sender, args) => HandleJobErrorData();
            _powershell.Streams.Warning.DataAdded += (sender, args) => HandleJobWarningData();
            _powershell.Streams.Verbose.DataAdded += (sender, args) => HandleJobVerboseData();
            _powershell.Streams.Debug.DataAdded += (sender, args) => HandleJobDebugData();
            _powershell.Streams.Information.DataAdded += (sender, args) => HandleJobInformationData();

            // State change handler
            _powershell.InvocationStateChanged += (sender, args) => HandleStateChanged(args);
        }

        #endregion

        #region Job data stream handlers

        private void HandleJobOutputData()
        {
            foreach (var item in _output.ReadAll())
            {
                _job.Output.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Output, item));
            }
        }

        private void HandleJobErrorData()
        {
            foreach (var item in _powershell.Streams.Error.ReadAll())
            {
                _job.Error.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Error, item));
            }
        }

        private void HandleJobWarningData()
        {
            foreach (var item in _powershell.Streams.Warning.ReadAll())
            {
                _job.Warning.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Warning, item.Message));
            }
        }

        private void HandleJobVerboseData()
        {
            foreach (var item in _powershell.Streams.Verbose.ReadAll())
            {
                _job.Verbose.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Verbose, item.Message));
            }
        }

        private void HandleJobDebugData()
        {
            foreach (var item in _powershell.Streams.Debug.ReadAll())
            {
                _job.Debug.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Debug, item.Message));
            }
        }

        private void HandleJobInformationData()
        {
            foreach (var item in _powershell.Streams.Information.ReadAll())
            {
                _job.Information.Add(item);
                _job.Results.Add(
                    new PSStreamObject(PSStreamObjectType.Information, item));
            }
        }

        #endregion

        #region Event handlers

        private void HandleStateChanged(PSInvocationStateChangedEventArgs stateChangeInfo)
        {
            RaiseStateChangedEvent(stateChangeInfo);
        }

        #endregion

        #region Properties

        
        public Debugger Debugger
        {
            get => _powershell.Runspace.Debugger;
        }

        #endregion
    }

    
    internal abstract class PSTaskBase : IDisposable
    {
        #region Members

        private readonly ScriptBlock _scriptBlockToRun;
        private readonly Dictionary<string, object> _usingValuesMap;
        private readonly object _dollarUnderbar;
        private readonly int _id;
        private readonly string _currentLocationPath;
        private Runspace _runspace;
        protected PowerShell _powershell;
        protected PSDataCollection<PSObject> _output;

        public const string RunspaceName = "PSTask";

        private static int s_taskId;

        #endregion

        #region Events

        
        public event EventHandler<PSInvocationStateChangedEventArgs> StateChanged;

        internal void RaiseStateChangedEvent(PSInvocationStateChangedEventArgs args)
        {
            StateChanged.SafeInvoke(this, args);
        }

        #endregion

        #region Properties

        
        public PSInvocationState State
        {
            get
            {
                PowerShell ps = _powershell;
                if (ps != null)
                {
                    return ps.InvocationStateInfo.State;
                }

                return PSInvocationState.NotStarted;
            }
        }

        
        public int Id { get => _id; }

        
        public Runspace Runspace { get => _runspace; }

        #endregion

        #region Constructor

        private PSTaskBase()
        {
            _id = Interlocked.Increment(ref s_taskId);
        }

        
        protected PSTaskBase(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            string currentLocationPath) : this()
        {
            _scriptBlockToRun = scriptBlock;
            _usingValuesMap = usingValuesMap;
            _dollarUnderbar = dollarUnderbar;
            _currentLocationPath = currentLocationPath;
        }

        #endregion

        #region Abstract Methods

        
        protected abstract void InitializePowershell();

        #endregion

        #region IDisposable

        
        public void Dispose()
        {
            _powershell.Dispose();
            _output.Dispose();
        }

        #endregion

        #region Public Methods

        
        public void Start(Runspace runspace)
        {
            if (_powershell != null)
            {
                Dbg.Assert(false, "A PSTask can be started only once.");
                return;
            }

            Dbg.Assert(runspace != null, "Task runspace cannot be null.");
            _runspace = runspace;

            // If available, set current working directory on the runspace.
            // Temporarily set the newly created runspace as the thread default runspace for any needed module loading.
            if (_currentLocationPath != null)
            {
                var oldDefaultRunspace = Runspace.DefaultRunspace;
                try
                {
                    Runspace.DefaultRunspace = runspace;
                    var context = new CmdletProviderContext(runspace.ExecutionContext)
                    {
                        // _currentLocationPath denotes the current path as-is, and should not be attempted expanded.
                        SuppressWildcardExpansion = true
                    };
                    runspace.ExecutionContext.SessionState.Internal.SetLocation(_currentLocationPath, context);
                }
                catch (DriveNotFoundException)
                {
                    // Allow task to run if current drive is not available.
                }
                finally
                {
                    Runspace.DefaultRunspace = oldDefaultRunspace;
                }
            }

            // Create the PowerShell command pipeline for the provided script block
            // The script will run on the provided Runspace in a new thread by default
            _powershell = PowerShell.Create(runspace);

            // Initialize PowerShell object data streams and event handlers
            _output = new PSDataCollection<PSObject>();
            InitializePowershell();

            // Start the script running in a new thread
            _powershell.AddScript(_scriptBlockToRun.ToString());
            _powershell.Commands.Commands[0].DollarUnderbar = _dollarUnderbar;
            if (_usingValuesMap != null && _usingValuesMap.Count > 0)
            {
                _powershell.AddParameter(Parser.VERBATIM_ARGUMENT, _usingValuesMap);
            }

            _powershell.BeginInvoke<object, PSObject>(input: null, output: _output);
        }

        
        public void SignalStop() => _powershell?.BeginStop(null, null);

        #endregion
    }

    #endregion

    #region PSTaskDataStreamWriter

    
    internal sealed class PSTaskDataStreamWriter : IDisposable
    {
        #region Members

        private readonly PSCmdlet _cmdlet;
        private readonly PSDataCollection<PSStreamObject> _dataStream;
        private readonly int _cmdletThreadId;

        #endregion

        #region Properties

        
        internal WaitHandle DataAddedWaitHandle
        {
            get => _dataStream.WaitHandle;
        }

        #endregion

        #region Constructor

        private PSTaskDataStreamWriter() { }

        
        public PSTaskDataStreamWriter(PSCmdlet psCmdlet)
        {
            _cmdlet = psCmdlet;
            _cmdletThreadId = Environment.CurrentManagedThreadId;
            _dataStream = new PSDataCollection<PSStreamObject>();
        }

        #endregion

        #region Public Methods

        
        public void Add(PSStreamObject streamObject)
        {
            _dataStream.Add(streamObject);
        }

        
        public void WriteImmediate()
        {
            CheckCmdletThread();

            foreach (var item in _dataStream.ReadAll())
            {
                item.WriteStreamObject(cmdlet: _cmdlet, overrideInquire: true);
            }
        }

        
        public void WaitAndWrite()
        {
            CheckCmdletThread();

            while (true)
            {
                _dataStream.WaitHandle.WaitOne();
                WriteImmediate();

                if (!_dataStream.IsOpen)
                {
                    WriteImmediate();
                    break;
                }
            }
        }

        
        public void Close()
        {
            _dataStream.Complete();
        }

        #endregion

        #region Private Methods

        private void CheckCmdletThread()
        {
            if (Environment.CurrentManagedThreadId != _cmdletThreadId)
            {
                throw new PSInvalidOperationException(InternalCommandStrings.PSTaskStreamWriterWrongThread);
            }
        }

        #endregion

        #region IDisposable

        
        public void Dispose()
        {
            _dataStream.Dispose();
        }

        #endregion
    }

    #endregion

    #region PSTaskPool

    
    internal sealed class PSTaskPool : IDisposable
    {
        #region Members

        private readonly ManualResetEvent _addAvailable;
        private readonly int _sizeLimit;
        private readonly ManualResetEvent _stopAll;
        private readonly object _syncObject;
        private readonly Dictionary<int, PSTaskBase> _taskPool;
        private readonly ConcurrentQueue<Runspace> _runspacePool;
        private readonly ConcurrentDictionary<int, Runspace> _activeRunspaces;
        private readonly WaitHandle[] _waitHandles;
        private readonly bool _useRunspacePool;
        private bool _isOpen;
        private bool _stopping;
        private int _createdRunspaceCount;

        private const int AddAvailable = 0;
        private const int Stop = 1;

        #endregion

        #region Constructor

        private PSTaskPool() { }

        
        public PSTaskPool(
            int size,
            bool useNewRunspace)
        {
            _sizeLimit = size;
            _useRunspacePool = !useNewRunspace;
            _isOpen = true;
            _syncObject = new object();
            _addAvailable = new ManualResetEvent(true);
            _stopAll = new ManualResetEvent(false);
            _waitHandles = new WaitHandle[]
            {
                _addAvailable,      // index 0
                _stopAll,           // index 1
            };
            _taskPool = new Dictionary<int, PSTaskBase>(size);
            _activeRunspaces = new ConcurrentDictionary<int, Runspace>();
            if (_useRunspacePool)
            {
                _runspacePool = new ConcurrentQueue<Runspace>();
            }
        }

        #endregion

        #region Events

        
        public event EventHandler<EventArgs> PoolComplete;

        #endregion

        #region Properties

        
        public bool IsOpen
        {
            get => _isOpen;
        }

        
        public int AllocatedRunspaceCount
        {
            get => _createdRunspaceCount;
        }

        #endregion

        #region IDisposable

        
        public void Dispose()
        {
            _addAvailable.Dispose();
            _stopAll.Dispose();

            DisposeRunspaces();
        }

        
        internal void DisposeRunspaces()
        {
            foreach (var item in _activeRunspaces)
            {
                item.Value.Dispose();
            }

            _activeRunspaces.Clear();
        }

        #endregion

        #region Public Methods

        
        public bool Add(PSTaskBase task)
        {
            if (!_isOpen)
            {
                return false;
            }

            // Block until either space is available, or a stop is commanded
            var index = WaitHandle.WaitAny(_waitHandles);

            switch (index)
            {
                case AddAvailable:
                    var runspace = GetRunspace(task.Id);
                    task.StateChanged += HandleTaskStateChangedDelegate;
                    lock (_syncObject)
                    {
                        if (!_isOpen)
                        {
                            return false;
                        }

                        _taskPool.Add(task.Id, task);
                        if (_taskPool.Count == _sizeLimit)
                        {
                            _addAvailable.Reset();
                        }

                        task.Start(runspace);
                    }

                    return true;

                case Stop:
                    return false;

                default:
                    return false;
            }
        }

        
        public bool Add(PSTaskChildJob childJob)
        {
            return Add(childJob.Task);
        }

        
        public void StopAll()
        {
            _stopping = true;

            // Accept no more input
            Close();
            _stopAll.Set();

            // Stop all running tasks
            PSTaskBase[] tasksToStop;
            lock (_syncObject)
            {
                tasksToStop = new PSTaskBase[_taskPool.Values.Count];
                _taskPool.Values.CopyTo(tasksToStop, 0);
            }

            foreach (var task in tasksToStop)
            {
                task.Dispose();
            }

            // Dispose all active runspaces
            DisposeRunspaces();
            _stopping = false;
        }

        
        public void Close()
        {
            _isOpen = false;
            CheckForComplete();
        }

        #endregion

        #region Private Methods

        private void HandleTaskStateChangedDelegate(object sender, PSInvocationStateChangedEventArgs args) => HandleTaskStateChanged(sender, args);

        private void HandleTaskStateChanged(object sender, PSInvocationStateChangedEventArgs args)
        {
            var task = sender as PSTaskBase;
            Dbg.Assert(task != null, "State changed sender must always be PSTaskBase");
            var stateInfo = args.InvocationStateInfo;
            switch (stateInfo.State)
            {
                // Look for completed state and remove
                case PSInvocationState.Completed:
                case PSInvocationState.Stopped:
                case PSInvocationState.Failed:
                    ReturnRunspace(task);
                    lock (_syncObject)
                    {
                        _taskPool.Remove(task.Id);
                        if (_taskPool.Count == (_sizeLimit - 1))
                        {
                            _addAvailable.Set();
                        }
                    }

                    task.StateChanged -= HandleTaskStateChangedDelegate;
                    if (!_stopping || stateInfo.State != PSInvocationState.Stopped)
                    {
                        // StopAll disposes tasks.
                        task.Dispose();
                    }

                    CheckForComplete();
                    break;
            }
        }

        private void CheckForComplete()
        {
            bool isTaskPoolComplete;
            lock (_syncObject)
            {
                isTaskPoolComplete = !_isOpen && _taskPool.Count == 0;
            }

            if (isTaskPoolComplete)
            {
                try
                {
                    PoolComplete.SafeInvoke(
                        this,
                        new EventArgs());
                }
                catch
                {
                    Dbg.Assert(false, "Exceptions should not be thrown on event thread");
                }
            }
        }

        private Runspace GetRunspace(int taskId)
        {
            var runspaceName = string.Create(CultureInfo.InvariantCulture, $"{PSTask.RunspaceName}:{taskId}");

            if (_useRunspacePool && _runspacePool.TryDequeue(out Runspace runspace))
            {
                if (runspace.RunspaceStateInfo.State == RunspaceState.Opened &&
                    runspace.RunspaceAvailability == RunspaceAvailability.Available)
                {
                    try
                    {
                        runspace.ResetRunspaceState();
                        runspace.Name = runspaceName;
                        return runspace;
                    }
                    catch
                    {
                        // If the runspace cannot be reset for any reason, remove it.
                    }
                }

                RemoveActiveRunspace(runspace);
            }

            // Create and initialize a new Runspace
            var iss = InitialSessionState.CreateDefault2();
            switch (SystemPolicy.GetSystemLockdownPolicy())
            {
                case SystemEnforcementMode.Enforce:
                    iss.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                    break;

                case SystemEnforcementMode.Audit:
                    // In audit mode, CL restrictions are not enforced and instead audit
                    // log entries are created.
                    iss.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                    break;

                case SystemEnforcementMode.None:
                    iss.LanguageMode = PSLanguageMode.FullLanguage;
                    break;
            }
            
            runspace = RunspaceFactory.CreateRunspace(iss);
            runspace.Name = runspaceName;
            _activeRunspaces.TryAdd(runspace.Id, runspace);
            runspace.Open();
            _createdRunspaceCount++;

            return runspace;
        }

        private void ReturnRunspace(PSTaskBase task)
        {
            var runspace = task.Runspace;
            Dbg.Assert(runspace != null, "Task runspace cannot be null.");
            if (_useRunspacePool &&
                runspace.RunspaceStateInfo.State == RunspaceState.Opened &&
                runspace.RunspaceAvailability == RunspaceAvailability.Available)
            {
                _runspacePool.Enqueue(runspace);
                return;
            }

            RemoveActiveRunspace(runspace);
        }

        private void RemoveActiveRunspace(Runspace runspace)
        {
            runspace.Dispose();
            _activeRunspaces.TryRemove(runspace.Id, out Runspace _);
        }

        #endregion
    }

    #endregion

    #region PSTaskJobs

    
    public sealed class PSTaskJob : Job
    {
        #region Members

        private readonly PSTaskPool _taskPool;
        private bool _isOpen;
        private bool _stopSignaled;

        #endregion

        #region Properties

        
        public int AllocatedRunspaceCount
        {
            get => _taskPool.AllocatedRunspaceCount;
        }

        #endregion

        #region Constructor

        private PSTaskJob() { }

        
        internal PSTaskJob(
            string command,
            int throttleLimit,
            bool useNewRunspace) : base(command, string.Empty)
        {
            _taskPool = new PSTaskPool(throttleLimit, useNewRunspace);
            _isOpen = true;
            PSJobTypeName = nameof(PSTaskJob);

            _taskPool.PoolComplete += (sender, args) => HandleTaskPoolComplete(sender, args);
        }

        #endregion

        #region Overrides

        
        public override string Location
        {
            get => "PowerShell";
        }

        
        public override bool HasMoreData
        {
            get
            {
                foreach (var childJob in ChildJobs)
                {
                    if (childJob.HasMoreData)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        
        public override string StatusMessage
        {
            get => string.Empty;
        }

        
        public override void StopJob()
        {
            _stopSignaled = true;
            SetJobState(JobState.Stopping);

            _taskPool.StopAll();
            SetJobState(JobState.Stopped);
        }

        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _taskPool.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Internal Methods

        
        internal bool AddJob(PSTaskChildJob childJob)
        {
            if (!_isOpen)
            {
                return false;
            }

            ChildJobs.Add(childJob);
            return true;
        }

        
        internal void Start()
        {
            _isOpen = false;
            SetJobState(JobState.Running);

            // Submit jobs to the task pool, blocking when throttle limit is reached.
            // This thread will end once all jobs reach a finished state by either running
            // to completion, terminating with error, or stopped.
            System.Threading.ThreadPool.QueueUserWorkItem(
                (_) =>
                {
                    foreach (var childJob in ChildJobs)
                    {
                        _taskPool.Add((PSTaskChildJob)childJob);
                    }

                    _taskPool.Close();
                });
        }

        #endregion

        #region Private Methods

        private void HandleTaskPoolComplete(object sender, EventArgs args)
        {
            try
            {
                if (_stopSignaled)
                {
                    SetJobState(JobState.Stopped, new PipelineStoppedException());
                    return;
                }

                // Final state will be 'Complete', only if all child jobs completed successfully.
                JobState finalState = JobState.Completed;
                foreach (var childJob in ChildJobs)
                {
                    if (childJob.JobStateInfo.State != JobState.Completed)
                    {
                        finalState = JobState.Failed;
                        break;
                    }
                }

                SetJobState(finalState);

                // Release job task pool runspace resources.
                (sender as PSTaskPool).DisposeRunspaces();
            }
            catch (ObjectDisposedException)
            { }
        }

        #endregion
    }

    
    internal sealed class PSTaskChildDebugger : Debugger
    {
        #region Members

        private readonly Debugger _wrappedDebugger;
        private readonly string _jobName;

        #endregion

        #region Constructor

        private PSTaskChildDebugger() { }

        
        public PSTaskChildDebugger(
            Debugger debugger,
            string jobName)
        {
            if (debugger == null)
            {
                throw new PSArgumentNullException(nameof(debugger));
            }

            _wrappedDebugger = debugger;
            _jobName = jobName ?? string.Empty;

            // Create handlers for wrapped debugger events.
            _wrappedDebugger.BreakpointUpdated += HandleBreakpointUpdated;
            _wrappedDebugger.DebuggerStop += HandleDebuggerStop;
        }

        #endregion

        #region Debugger overrides

        
        public override DebuggerCommandResults ProcessCommand(
            PSCommand command,
            PSDataCollection<PSObject> output)
        {
            // Special handling for the prompt command.
            if (command.Commands[0].CommandText.Trim().Equals("prompt", StringComparison.OrdinalIgnoreCase))
            {
                return HandlePromptCommand(output);
            }

            return _wrappedDebugger.ProcessCommand(command, output);
        }

        
        public override void SetBreakpoints(IEnumerable<Breakpoint> breakpoints, int? runspaceId) =>
            _wrappedDebugger.SetBreakpoints(breakpoints, runspaceId);

        
        public override void SetDebuggerAction(DebuggerResumeAction resumeAction)
        {
            _wrappedDebugger.SetDebuggerAction(resumeAction);
        }

        
        public override Breakpoint GetBreakpoint(int id, int? runspaceId) =>
            _wrappedDebugger.GetBreakpoint(id, runspaceId);

        
        public override List<Breakpoint> GetBreakpoints(int? runspaceId) =>
            _wrappedDebugger.GetBreakpoints(runspaceId);

        
        public override CommandBreakpoint SetCommandBreakpoint(string command, ScriptBlock action, string path, int? runspaceId) =>
            _wrappedDebugger.SetCommandBreakpoint(command, action, path, runspaceId);

        
        public override VariableBreakpoint SetVariableBreakpoint(string variableName, VariableAccessMode accessMode, ScriptBlock action, string path, int? runspaceId) =>
            _wrappedDebugger.SetVariableBreakpoint(variableName, accessMode, action, path, runspaceId);

        
        public override LineBreakpoint SetLineBreakpoint(string path, int line, int column, ScriptBlock action, int? runspaceId) =>
            _wrappedDebugger.SetLineBreakpoint(path, line, column, action, runspaceId);

        
        public override Breakpoint EnableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.EnableBreakpoint(breakpoint, runspaceId);

        
        public override Breakpoint DisableBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.DisableBreakpoint(breakpoint, runspaceId);

        
        public override bool RemoveBreakpoint(Breakpoint breakpoint, int? runspaceId) =>
            _wrappedDebugger.RemoveBreakpoint(breakpoint, runspaceId);

        
        public override void StopProcessCommand()
        {
            _wrappedDebugger.StopProcessCommand();
        }

        
        public override DebuggerStopEventArgs GetDebuggerStopArgs()
        {
            return _wrappedDebugger.GetDebuggerStopArgs();
        }

        
        public override void SetParent(
            Debugger parent,
            IEnumerable<Breakpoint> breakPoints,
            DebuggerResumeAction? startAction,
            PSHost host,
            PathInfo path)
        {
            // For now always enable step mode debugging.
            SetDebuggerStepMode(true);
        }

        
        public override void SetDebugMode(DebugModes mode)
        {
            _wrappedDebugger.SetDebugMode(mode);

            base.SetDebugMode(mode);
        }

        
        public override IEnumerable<CallStackFrame> GetCallStack()
        {
            return _wrappedDebugger.GetCallStack();
        }

        
        public override void SetDebuggerStepMode(bool enabled)
        {
            _wrappedDebugger.SetDebuggerStepMode(enabled);
        }

        
        public override bool InBreakpoint
        {
            get => _wrappedDebugger.InBreakpoint;
        }

        #endregion

        #region Private methods

        private void HandleDebuggerStop(object sender, DebuggerStopEventArgs e)
        {
            this.RaiseDebuggerStopEvent(e);
        }

        private void HandleBreakpointUpdated(object sender, BreakpointUpdatedEventArgs e)
        {
            this.RaiseBreakpointUpdatedEvent(e);
        }

        private DebuggerCommandResults HandlePromptCommand(PSDataCollection<PSObject> output)
        {
            // Nested debugged runspace prompt should look like:
            // [DBG]: [JobName]: PS C:\>>
            string promptScript = "'[DBG]: '" + " + " + "'[" + CodeGeneration.EscapeSingleQuotedStringContent(_jobName) + "]: '" + " + " + @"""PS $($executionContext.SessionState.Path.CurrentLocation)>> """;
            PSCommand promptCommand = new PSCommand();
            promptCommand.AddScript(promptScript);
            _wrappedDebugger.ProcessCommand(promptCommand, output);

            return new DebuggerCommandResults(null, true);
        }

        #endregion
    }

    
    internal sealed class PSTaskChildJob : Job, IJobDebugger
    {
        #region Members

        private readonly PSJobTask _task;
        private PSTaskChildDebugger _jobDebuggerWrapper;

        #endregion

        #region Constructor

        private PSTaskChildJob() { }

        
        public PSTaskChildJob(
            ScriptBlock scriptBlock,
            Dictionary<string, object> usingValuesMap,
            object dollarUnderbar,
            string currentLocationPath)
            : base(scriptBlock.ToString(), string.Empty)

        {
            PSJobTypeName = nameof(PSTaskChildJob);
            _task = new PSJobTask(scriptBlock, usingValuesMap, dollarUnderbar, currentLocationPath, this);
            _task.StateChanged += (sender, args) => HandleTaskStateChange(sender, args);
        }

        #endregion

        #region Properties

        
        internal PSTaskBase Task
        {
            get => _task;
        }

        #endregion

        #region Overrides

        
        public override string Location
        {
            get => "PowerShell";
        }

        
        public override bool HasMoreData
        {
            get => this.Output.Count > 0 ||
                   this.Error.Count > 0 ||
                   this.Progress.Count > 0 ||
                   this.Verbose.Count > 0 ||
                   this.Debug.Count > 0 ||
                   this.Warning.Count > 0 ||
                   this.Information.Count > 0;
        }

        
        public override string StatusMessage
        {
            get => string.Empty;
        }

        
        public override void StopJob()
        {
            _task.SignalStop();
        }

        #endregion

        #region IJobDebugger

        
        public Debugger Debugger
        {
            get
            {
                _jobDebuggerWrapper ??= new PSTaskChildDebugger(
                    _task.Debugger,
                    this.Name);

                return _jobDebuggerWrapper;
            }
        }

        
        public bool IsAsync { get; set; }

        #endregion

        #region Private Methods

        private void HandleTaskStateChange(object sender, PSInvocationStateChangedEventArgs args)
        {
            var stateInfo = args.InvocationStateInfo;

            switch (stateInfo.State)
            {
                case PSInvocationState.Running:
                    SetJobState(JobState.Running);
                    break;

                case PSInvocationState.Stopped:
                    SetJobState(JobState.Stopped, stateInfo.Reason);
                    break;

                case PSInvocationState.Failed:
                    SetJobState(JobState.Failed, stateInfo.Reason);
                    break;

                case PSInvocationState.Completed:
                    SetJobState(JobState.Completed, stateInfo.Reason);
                    break;
            }
        }

        #endregion
    }

    #endregion
}
