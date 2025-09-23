// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    internal sealed class CmdletProviderContext
    {
        #region Trace object

        
        [Dbg.TraceSource(
             "CmdletProviderContext",
             "The context under which a core command is being run.")]
        private static readonly Dbg.PSTraceSource s_tracer =
            Dbg.PSTraceSource.GetTracer("CmdletProviderContext",
             "The context under which a core command is being run.");

        #endregion Trace object

        #region Constructor

        
        internal CmdletProviderContext(ExecutionContext executionContext)
        {
            if (executionContext == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(executionContext));
            }

            ExecutionContext = executionContext;
            Origin = CommandOrigin.Internal;
            Drive = executionContext.EngineSessionState.CurrentDrive;
            if ((executionContext.CurrentCommandProcessor != null) &&
                (executionContext.CurrentCommandProcessor.Command is Cmdlet))
            {
                _command = (Cmdlet)executionContext.CurrentCommandProcessor.Command;
            }
        }

        
        internal CmdletProviderContext(ExecutionContext executionContext, CommandOrigin origin)
        {
            if (executionContext == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(executionContext));
            }

            ExecutionContext = executionContext;
            Origin = origin;
        }

        
        internal CmdletProviderContext(
            PSCmdlet command,
            PSCredential credentials,
            PSDriveInfo drive)
        {
            // verify the command parameter
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            _command = command;
            Origin = command.CommandOrigin;

            if (credentials != null)
            {
                _credentials = credentials;
            }

            Drive = drive;

            if (command.Host == null)
            {
                throw PSTraceSource.NewArgumentException("command.Host");
            }

            if (command.Context == null)
            {
                throw PSTraceSource.NewArgumentException("command.Context");
            }

            ExecutionContext = command.Context;

            // Stream will default to true because command methods will be used.

            PassThru = true;
            _streamErrors = true;
        }

        
        internal CmdletProviderContext(
            PSCmdlet command,
            PSCredential credentials)
        {
            // verify the command parameter
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            _command = command;
            Origin = command.CommandOrigin;

            if (credentials != null)
            {
                _credentials = credentials;
            }

            if (command.Host == null)
            {
                throw PSTraceSource.NewArgumentException("command.Host");
            }

            if (command.Context == null)
            {
                throw PSTraceSource.NewArgumentException("command.Context");
            }

            ExecutionContext = command.Context;

            // Stream will default to true because command methods will be used.

            PassThru = true;
            _streamErrors = true;
        }

        
        internal CmdletProviderContext(
            Cmdlet command)
        {
            // verify the command parameter
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            _command = command;
            Origin = command.CommandOrigin;

            if (command.Context == null)
            {
                throw PSTraceSource.NewArgumentException("command.Context");
            }

            ExecutionContext = command.Context;

            // Stream will default to true because command methods will be used.

            PassThru = true;
            _streamErrors = true;
        }

        
        internal CmdletProviderContext(
            CmdletProviderContext contextToCopyFrom)
        {
            if (contextToCopyFrom == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(contextToCopyFrom));
            }

            ExecutionContext = contextToCopyFrom.ExecutionContext;

            _command = contextToCopyFrom._command;

            if (contextToCopyFrom.Credential != null)
            {
                _credentials = contextToCopyFrom.Credential;
            }

            Drive = contextToCopyFrom.Drive;
            _force = contextToCopyFrom.Force;
            this.CopyFilters(contextToCopyFrom);
            SuppressWildcardExpansion = contextToCopyFrom.SuppressWildcardExpansion;
            DynamicParameters = contextToCopyFrom.DynamicParameters;
            Origin = contextToCopyFrom.Origin;

            // Copy the stopping state incase the source context
            // has already been signaled for stopping

            Stopping = contextToCopyFrom.Stopping;

            // add this context to the stop referral on the copied
            // context

            contextToCopyFrom.StopReferrals.Add(this);
            _copiedContext = contextToCopyFrom;
        }

        #endregion Constructor

        #region private properties

        
        private readonly CmdletProviderContext _copiedContext;

        
        private readonly PSCredential _credentials = PSCredential.Empty;

        
        private bool _force;

        
        private readonly Cmdlet _command;

        
        internal CommandOrigin Origin { get; } = CommandOrigin.Internal;

        
        private readonly bool _streamErrors;

        
        private Collection<PSObject> _accumulatedObjects = new Collection<PSObject>();

        
        private Collection<ErrorRecord> _accumulatedErrorObjects = new Collection<ErrorRecord>();

        
        private System.Management.Automation.Provider.CmdletProvider _providerInstance;

        #endregion private properties

        #region Internal properties

        
        internal ExecutionContext ExecutionContext { get; }

        
        internal System.Management.Automation.Provider.CmdletProvider ProviderInstance
        {
            get
            {
                return _providerInstance;
            }

            set
            {
                _providerInstance = value;
            }
        }

        
        private void CopyFilters(CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                context != null,
                "The caller should have verified the context");

            Include = context.Include;
            Exclude = context.Exclude;
            Filter = context.Filter;
        }

        internal void RemoveStopReferral() => _copiedContext?.StopReferrals.Remove(this);

        #endregion Internal properties

        #region Public properties

        
        internal object DynamicParameters { get; set; }

        
        internal InvocationInfo MyInvocation
        {
            get
            {
                if (_command != null)
                {
                    return _command.MyInvocation;
                }
                else
                {
                    return null;
                }
            }
        }

        
        internal bool PassThru { get; set; }

        
        internal PSDriveInfo Drive { get; set; }

        
        internal PSCredential Credential
        {
            get
            {
                PSCredential result = _credentials;

                // If the username wasn't specified, use the drive credentials

                if (_credentials == null && Drive != null)
                {
                    result = Drive.Credential;
                }

                return result;
            }
        }

        #region Transaction Support

        
        internal bool UseTransaction
        {
            get
            {
                if ((_command != null) && (_command.CommandRuntime != null))
                {
                    MshCommandRuntime mshRuntime = _command.CommandRuntime as MshCommandRuntime;

                    if (mshRuntime != null)
                    {
                        return mshRuntime.UseTransaction;
                    }
                }

                return false;
            }
        }

        
        public bool TransactionAvailable()
        {
            if (_command != null)
            {
                return _command.TransactionAvailable();
            }

            return false;
        }

        
        public PSTransactionContext CurrentPSTransaction
        {
            get
            {
                if (_command != null)
                {
                    return _command.CurrentPSTransaction;
                }

                return null;
            }
        }
        #endregion Transaction Support

        
        internal SwitchParameter Force
        {
            get { return _force; }

            set { _force = value; }
        }

        
        internal string Filter { get; set; }

        
        internal Collection<string> Include { get; private set; }

        
        internal Collection<string> Exclude { get; private set; }

        
        public bool SuppressWildcardExpansion { get; internal set; }

        #region User feedback mechanisms

        
        internal bool ShouldProcess(
            string target)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldProcess(target);
            }

            return result;
        }

        
        internal bool ShouldProcess(
            string target,
            string action)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldProcess(target, action);
            }

            return result;
        }

        
        internal bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldProcess(
                    verboseDescription,
                    verboseWarning,
                    caption);
            }

            return result;
        }

        
        internal bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption,
            out ShouldProcessReason shouldProcessReason)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldProcess(
                    verboseDescription,
                    verboseWarning,
                    caption,
                    out shouldProcessReason);
            }
            else
            {
                shouldProcessReason = ShouldProcessReason.None;
            }

            return result;
        }

        
        internal bool ShouldContinue(
            string query,
            string caption)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldContinue(query, caption);
            }

            return result;
        }

        
        internal bool ShouldContinue(
            string query,
            string caption,
            ref bool yesToAll,
            ref bool noToAll)
        {
            bool result = true;
            if (_command != null)
            {
                result = _command.ShouldContinue(
                    query, caption, ref yesToAll, ref noToAll);
            }
            else
            {
                yesToAll = false;
                noToAll = false;
            }

            return result;
        }

        
        internal void WriteVerbose(string text) => _command?.WriteVerbose(text);

        
        internal void WriteWarning(string text) => _command?.WriteWarning(text);

        internal void WriteProgress(ProgressRecord record) => _command?.WriteProgress(record);

        
        internal void WriteDebug(string text) => _command?.WriteDebug(text);

        internal void WriteInformation(InformationRecord record) => _command?.WriteInformation(record);

        internal void WriteInformation(object messageData, string[] tags) => _command?.WriteInformation(messageData, tags);

        #endregion User feedback mechanisms

        #endregion Public properties

        #region Public methods

        
        internal void SetFilters(Collection<string> include, Collection<string> exclude, string filter)
        {
            Include = include;
            Exclude = exclude;
            Filter = filter;
        }

        
        internal Collection<PSObject> GetAccumulatedObjects()
        {
            // Get the contents as an array

            Collection<PSObject> results = _accumulatedObjects;
            _accumulatedObjects = new Collection<PSObject>();

            // Return the array

            return results;
        }

        
        internal Collection<ErrorRecord> GetAccumulatedErrorObjects()
        {
            // Get the contents as an array

            Collection<ErrorRecord> results = _accumulatedErrorObjects;
            _accumulatedErrorObjects = new Collection<ErrorRecord>();

            // Return the array

            return results;
        }

        
        internal void ThrowFirstErrorOrDoNothing()
        {
            ThrowFirstErrorOrDoNothing(true);
        }

        
        internal void ThrowFirstErrorOrDoNothing(bool wrapExceptionInProviderException)
        {
            if (HasErrors())
            {
                Collection<ErrorRecord> errors = GetAccumulatedErrorObjects();

                if (errors != null && errors.Count > 0)
                {
                    // Throw the first exception

                    if (wrapExceptionInProviderException)
                    {
                        ProviderInfo providerInfo = null;
                        if (this.ProviderInstance != null)
                        {
                            providerInfo = this.ProviderInstance.ProviderInfo;
                        }

                        ProviderInvocationException e =
                            new ProviderInvocationException(
                                providerInfo,
                                errors[0]);

                        // Log a provider health event

                        MshLog.LogProviderHealthEvent(
                            this.ExecutionContext,
                            providerInfo != null ? providerInfo.Name : "unknown provider",
                            e,
                            Severity.Warning);

                        throw e;
                    }
                    else
                    {
                        throw errors[0].Exception;
                    }
                }
            }
        }

        
        internal void WriteErrorsToContext(CmdletProviderContext errorContext)
        {
            if (errorContext == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(errorContext));
            }

            if (HasErrors())
            {
                foreach (ErrorRecord errorRecord in GetAccumulatedErrorObjects())
                {
                    errorContext.WriteError(errorRecord);
                }
            }
        }

        
        internal void WriteObject(object obj)
        {
            // Making sure to obey the StopProcessing by
            // throwing an exception anytime a provider tries
            // to WriteObject

            if (Stopping)
            {
                PipelineStoppedException stopPipeline =
                    new PipelineStoppedException();

                throw stopPipeline;
            }

            if (PassThru)
            {
                if (_command != null)
                {
                    s_tracer.WriteLine("Writing to command pipeline");

                    // Since there was no writeObject handler use
                    // the command WriteObject method.

                    _command.WriteObject(obj);
                }
                else
                {
                    // The flag was set for streaming but we have no where
                    // to stream to.

                    InvalidOperationException e =
                        PSTraceSource.NewInvalidOperationException(
                            SessionStateStrings.OutputStreamingNotEnabled);
                    throw e;
                }
            }
            else
            {
                s_tracer.WriteLine("Writing to accumulated objects");

                // Convert the object to a PSObject if it's not already
                // one.

                PSObject newObj = PSObject.AsPSObject(obj);

                // Since we are not streaming, just add the object to the accumulatedObjects

                _accumulatedObjects.Add(newObj);
            }
        }

        
        internal void WriteError(ErrorRecord errorRecord)
        {
            // Making sure to obey the StopProcessing by
            // throwing an exception anytime a provider tries
            // to WriteError

            if (Stopping)
            {
                PipelineStoppedException stopPipeline =
                    new PipelineStoppedException();

                throw stopPipeline;
            }

            if (_streamErrors)
            {
                if (_command != null)
                {
                    s_tracer.WriteLine("Writing error package to command error pipe");

                    _command.WriteError(errorRecord);
                }
                else
                {
                    InvalidOperationException e =
                        PSTraceSource.NewInvalidOperationException(
                            SessionStateStrings.ErrorStreamingNotEnabled);
                    throw e;
                }
            }
            else
            {
                // Since we are not streaming, just add the object to the accumulatedErrorObjects
                _accumulatedErrorObjects.Add(errorRecord);

                if (errorRecord.ErrorDetails != null
                    && errorRecord.ErrorDetails.TextLookupError != null)
                {
                    Exception textLookupError = errorRecord.ErrorDetails.TextLookupError;
                    errorRecord.ErrorDetails.TextLookupError = null;
                    MshLog.LogProviderHealthEvent(
                        this.ExecutionContext,
                        this.ProviderInstance.ProviderInfo.Name,
                        textLookupError,
                        Severity.Warning);
                }
            }
        }

        
        internal bool HasErrors()
        {
            return _accumulatedErrorObjects != null && _accumulatedErrorObjects.Count > 0;
        }

        
        internal void StopProcessing()
        {
            Stopping = true;

            // We don't need to catch any of the exceptions here because
            // we are terminating the pipeline and any exception will
            // be caught by the engine.
            _providerInstance?.StopProcessing();

            // Call the stop referrals if any

            foreach (CmdletProviderContext referralContext in StopReferrals)
            {
                referralContext.StopProcessing();
            }
        }

        internal bool Stopping { get; private set; }

        
        internal Collection<CmdletProviderContext> StopReferrals { get; } = new Collection<CmdletProviderContext>();

        internal bool HasIncludeOrExclude
        {
            get
            {
                return ((Include != null && Include.Count > 0) ||
                        (Exclude != null && Exclude.Count > 0));
            }
        }

        #endregion Public methods
    }
}
