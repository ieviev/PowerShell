// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsLifecycle.Wait, "Job", DefaultParameterSetName = JobCmdletBase.SessionIdParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096902")]
    [OutputType(typeof(Job))]
    public class WaitJobCommand : JobCmdletBase, IDisposable
    {
        #region Parameters

        
        [Parameter(Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = RemoveJobCommand.JobParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Job[] Job { get; set; }

        
        [Parameter]
        public SwitchParameter Any { get; set; }

        
        [Parameter]
        [Alias("TimeoutSec")]
        [ValidateRange(-1, Int32.MaxValue)]
        public int Timeout
        {
            get
            {
                return _timeoutInSeconds;
            }

            set
            {
                _timeoutInSeconds = value;
            }
        }

        private int _timeoutInSeconds = -1; // -1: infinite, this default is to wait for as long as it takes.

        
        [Parameter]
        public SwitchParameter Force { get; set; }

        
        public override string[] Command { get; set; }
        #endregion Parameters

        #region Coordinating how different events (timeout, stopprocessing, job finished, job blocked) affect what happens in EndProcessing

        private readonly object _endProcessingActionLock = new object();
        private Action _endProcessingAction;
        private readonly ManualResetEventSlim _endProcessingActionIsReady = new ManualResetEventSlim(false);

        private void SetEndProcessingAction(Action endProcessingAction)
        {
            Dbg.Assert(endProcessingAction != null, "Caller should verify endProcessingAction != null");
            lock (_endProcessingActionLock)
            {
                if (_endProcessingAction == null)
                {
                    Dbg.Assert(!_endProcessingActionIsReady.IsSet, "This line should execute only once");
                    _endProcessingAction = endProcessingAction;
                    _endProcessingActionIsReady.Set();
                }
            }
        }

        private void InvokeEndProcessingAction()
        {
            _endProcessingActionIsReady.Wait();

            Action endProcessingAction;
            lock (_endProcessingActionLock)
            {
                endProcessingAction = _endProcessingAction;
            }

            // Invoke action outside lock.
            endProcessingAction?.Invoke();
        }

        private void CleanUpEndProcessing()
        {
            _endProcessingActionIsReady.Dispose();
        }

        #endregion

        #region Support for triggering EndProcessing when jobs are finished or blocked

        private readonly HashSet<Job> _finishedJobs = new HashSet<Job>();
        private readonly HashSet<Job> _blockedJobs = new HashSet<Job>();
        private readonly List<Job> _jobsToWaitFor = new List<Job>();
        private readonly object _jobTrackingLock = new object();

        private void HandleJobStateChangedEvent(object source, JobStateEventArgs eventArgs)
        {
            Dbg.Assert(source is Job, "Caller should verify source is Job");
            Dbg.Assert(eventArgs != null, "Caller should verify eventArgs != null");

            var job = (Job)source;
            lock (_jobTrackingLock)
            {
                Dbg.Assert(_blockedJobs.All(j => !_finishedJobs.Contains(j)), "Job cannot be in *both* _blockedJobs and _finishedJobs");

                if (eventArgs.JobStateInfo.State == JobState.Blocked)
                {
                    _blockedJobs.Add(job);
                }
                else
                {
                    _blockedJobs.Remove(job);
                }

                // Treat jobs in Disconnected state as finished jobs since the user
                // will have to reconnect the job before more information can be
                // obtained.
                // Suspended jobs require a Resume-Job call. Both of these states are persistent
                // without user interaction.
                // Wait should wait until a job is in a persistent state, OR if the force parameter
                // is specified, until the job is in a finished state, which is a subset of
                // persistent states.
                if (!Force && job.IsPersistentState(eventArgs.JobStateInfo.State) || (Force && job.IsFinishedState(eventArgs.JobStateInfo.State)))
                {
                    if (!job.IsFinishedState(eventArgs.JobStateInfo.State))
                    {
                        _warnNotTerminal = true;
                    }

                    _finishedJobs.Add(job);
                }
                else
                {
                    _finishedJobs.Remove(job);
                }

                Dbg.Assert(_blockedJobs.All(j => !_finishedJobs.Contains(j)), "Job cannot be in *both* _blockedJobs and _finishedJobs");

                if (this.Any.IsPresent)
                {
                    if (_finishedJobs.Count > 0)
                    {
                        this.SetEndProcessingAction(this.EndProcessingOutputSingleFinishedJob);
                    }
                    else if (_blockedJobs.Count == _jobsToWaitFor.Count)
                    {
                        this.SetEndProcessingAction(this.EndProcessingBlockedJobsError);
                    }
                }
                else
                {
                    if (_finishedJobs.Count == _jobsToWaitFor.Count)
                    {
                        this.SetEndProcessingAction(this.EndProcessingOutputAllFinishedJobs);
                    }
                    else if (_blockedJobs.Count > 0)
                    {
                        this.SetEndProcessingAction(this.EndProcessingBlockedJobsError);
                    }
                }
            }
        }

        private void AddJobsThatNeedJobChangesTracking(IEnumerable<Job> jobsToAdd)
        {
            Dbg.Assert(jobsToAdd != null, "Caller should verify jobs != null");

            lock (_jobTrackingLock)
            {
                _jobsToWaitFor.AddRange(jobsToAdd);
            }
        }

        private void StartJobChangesTracking()
        {
            lock (_jobTrackingLock)
            {
                if (_jobsToWaitFor.Count == 0)
                {
                    this.SetEndProcessingAction(this.EndProcessingDoNothing);
                    return;
                }

                foreach (Job job in _jobsToWaitFor)
                {
                    job.StateChanged += this.HandleJobStateChangedEvent;
                    this.HandleJobStateChangedEvent(job, new JobStateEventArgs(job.JobStateInfo));
                }
            }
        }

        private void CleanUpJobChangesTracking()
        {
            lock (_jobTrackingLock)
            {
                foreach (Job job in _jobsToWaitFor)
                {
                    job.StateChanged -= this.HandleJobStateChangedEvent;
                }
            }
        }

        private List<Job> GetFinishedJobs()
        {
            List<Job> jobsToOutput;
            lock (_jobTrackingLock)
            {
                jobsToOutput = _jobsToWaitFor.Where(j => ((!Force && j.IsPersistentState(j.JobStateInfo.State)) || (Force && j.IsFinishedState(j.JobStateInfo.State)))).ToList();
            }

            return jobsToOutput;
        }

        private Job GetOneBlockedJob()
        {
            lock (_jobTrackingLock)
            {
                return _jobsToWaitFor.Find(static j => j.JobStateInfo.State == JobState.Blocked);
            }
        }

        #endregion

        #region Support for triggering EndProcessing when timing out

        private Timer _timer;
        private readonly object _timerLock = new object();

        private void StartTimeoutTracking(int timeoutInSeconds)
        {
            if (timeoutInSeconds == 0)
            {
                this.SetEndProcessingAction(this.EndProcessingDoNothing);
            }
            else if (timeoutInSeconds > 0)
            {
                lock (_timerLock)
                {
                    _timer = new Timer((_) => this.SetEndProcessingAction(this.EndProcessingDoNothing), null, timeoutInSeconds * 1000, System.Threading.Timeout.Infinite);
                }
            }
        }

        private void CleanUpTimeoutTracking()
        {
            lock (_timerLock)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        #endregion

        #region Overrides

        
        protected override void StopProcessing()
        {
            this.SetEndProcessingAction(this.EndProcessingDoNothing);
        }

        
        protected override void BeginProcessing()
        {
            this.StartTimeoutTracking(_timeoutInSeconds);
        }

        
        protected override void ProcessRecord()
        {
            // List of jobs to wait
            List<Job> matches;

            switch (ParameterSetName)
            {
                case NameParameterSet:
                    matches = FindJobsMatchingByName(true, false, true, false);
                    break;

                case InstanceIdParameterSet:
                    matches = FindJobsMatchingByInstanceId(true, false, true, false);
                    break;

                case SessionIdParameterSet:
                    matches = FindJobsMatchingBySessionId(true, false, true, false);
                    break;

                case StateParameterSet:
                    matches = FindJobsMatchingByState(false);
                    break;

                case FilterParameterSet:
                    matches = FindJobsMatchingByFilter(false);
                    break;

                default:
                    matches = CopyJobsToList(this.Job, false, false);
                    break;
            }

            this.AddJobsThatNeedJobChangesTracking(matches);
        }

        
        protected override void EndProcessing()
        {
            this.StartJobChangesTracking();
            this.InvokeEndProcessingAction();
            if (_warnNotTerminal)
            {
                WriteWarning(RemotingErrorIdStrings.JobSuspendedDisconnectedWaitWithForce);
            }
        }

        private void EndProcessingOutputSingleFinishedJob()
        {
            Job finishedJob = this.GetFinishedJobs().FirstOrDefault();
            if (finishedJob != null)
            {
                this.WriteObject(finishedJob);
            }
        }

        private void EndProcessingOutputAllFinishedJobs()
        {
            IEnumerable<Job> finishedJobs = this.GetFinishedJobs();
            foreach (Job finishedJob in finishedJobs)
            {
                this.WriteObject(finishedJob);
            }
        }

        private void EndProcessingBlockedJobsError()
        {
            string message = RemotingErrorIdStrings.JobBlockedSoWaitJobCannotContinue;
            Exception exception = new ArgumentException(message);
            ErrorRecord errorRecord = new ErrorRecord(
                exception,
                "BlockedJobsDeadlockWithWaitJob",
                ErrorCategory.DeadlockDetected,
                this.GetOneBlockedJob());
            this.ThrowTerminatingError(errorRecord);
        }

        private void EndProcessingDoNothing()
        {
            // do nothing
        }

        #endregion Overrides

        #region IDisposable Members

        
        public void Dispose()
        {
            Dispose(true);
            // To prevent derived types with finalizers from having to re-implement System.IDisposable to call it,
            // unsealed types without finalizers should still call SuppressFinalize.
            System.GC.SuppressFinalize(this);
        }

        
        /// <param name="disposing">
        /// if true, release all the managed objects.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_disposableLock)
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;

                        this.CleanUpTimeoutTracking();
                        this.CleanUpJobChangesTracking();
                        this.CleanUpEndProcessing(); // <- has to be last
                    }
                }
            }
        }

        private bool _isDisposed;
        private readonly object _disposableLock = new object();
        private bool _warnNotTerminal = false;

        #endregion IDisposable Members
    }
}
