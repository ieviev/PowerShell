// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Management.Automation.Security;
using System.Runtime.InteropServices;

namespace System.Management.Automation
{
    
    internal abstract class CommandProcessorBase : IDisposable
    {
        #region ctor

        
        internal CommandProcessorBase()
        {
        }

        
        internal CommandProcessorBase(CommandInfo commandInfo)
        {
            if (commandInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandInfo));
            }

            if (commandInfo is IScriptCommandInfo scriptCommand)
            {
                ExperimentalAttribute expAttribute = scriptCommand.ScriptBlock.ExperimentalAttribute;
                if (expAttribute != null && expAttribute.ToHide)
                {
                    string errorTemplate = expAttribute.ExperimentAction == ExperimentAction.Hide
                        ? DiscoveryExceptions.ScriptDisabledWhenFeatureOn
                        : DiscoveryExceptions.ScriptDisabledWhenFeatureOff;

                    string errorMsg = StringUtil.Format(errorTemplate, expAttribute.ExperimentName);
                    ErrorRecord errorRecord = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "ScriptCommandDisabled",
                        ErrorCategory.InvalidOperation,
                        commandInfo);
                    throw new CmdletInvocationException(errorRecord);
                }

                HasCleanBlock = scriptCommand.ScriptBlock.HasCleanBlock;
            }

            CommandInfo = commandInfo;
        }

        #endregion ctor

        #region properties

        private InternalCommand _command;

        // Marker of whether BeginProcessing() has already run,
        // also used by CommandProcessor.
        internal bool RanBeginAlready;

        // Marker of whether this command has already been added to
        // a PipelineProcessor. It is an error to add the same command
        // more than once.
        internal bool AddedToPipelineAlready
        {
            get { return _addedToPipelineAlready; }

            set { _addedToPipelineAlready = value; }
        }

        internal bool _addedToPipelineAlready;

        
        internal CommandInfo CommandInfo { get; set; }

        
        internal bool HasCleanBlock { get; }

        
        public bool FromScriptFile { get { return _fromScriptFile; } }

        protected bool _fromScriptFile = false;

        
        internal bool RedirectShellErrorOutputPipe { get; set; } = false;

        
        internal InternalCommand Command
        {
            get
            {
                return _command;
            }

            set
            {
                // The command runtime needs to be set up...
                if (value != null)
                {
                    value.commandRuntime = this.commandRuntime;
                    if (_command != null)
                        value.CommandInfo = _command.CommandInfo;

                    // Set the execution context for the command it's currently
                    // null and our context has already been set up.
                    if (value.Context == null && _context != null)
                        value.Context = _context;
                }

                _command = value;
            }
        }

        
        internal virtual ObsoleteAttribute ObsoleteAttribute
        {
            get { return null; }
        }

        // Full Qualified ID for the obsolete command warning
        private const string FQIDCommandObsolete = "CommandObsolete";

        
        protected MshCommandRuntime commandRuntime;

        internal MshCommandRuntime CommandRuntime
        {
            get { return commandRuntime; }

            set { commandRuntime = value; }
        }

        
        internal bool UseLocalScope
        {
            get { return _useLocalScope; }

            set { _useLocalScope = value; }
        }

        protected bool _useLocalScope;

        
        protected static void ValidateCompatibleLanguageMode(
            ScriptBlock scriptBlock,
            ExecutionContext context,
            InvocationInfo invocationInfo)
        {
            // If we are in a constrained language mode (Core or Restricted), block it.
            // We are currently restricting in one direction:
            //    - Can't dot something from a more permissive mode, since that would probably expose
            //      functions that were never designed to handle untrusted data.
            // This function won't be called for NoLanguage mode so the only direction checked is trusted
            // (FullLanguage mode) script running in a constrained/restricted session.
            var languageMode = context.LanguageMode;
            if (scriptBlock.LanguageMode.HasValue &&
                scriptBlock.LanguageMode != languageMode &&
                (languageMode == PSLanguageMode.RestrictedLanguage ||
                 languageMode == PSLanguageMode.ConstrainedLanguage))
            {
                // Finally check if script block is really just PowerShell commands plus parameters.
                // If so then it is safe to dot source across language mode boundaries.
                bool isSafeToDotSource = false;
                try
                {
                    scriptBlock.GetPowerShell();
                    isSafeToDotSource = true;
                }
                catch (Exception)
                {
                }

                if (!isSafeToDotSource)
                {
                    if (SystemPolicy.GetSystemLockdownPolicy() != SystemEnforcementMode.Audit)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(
                            new NotSupportedException(DiscoveryExceptions.DotSourceNotSupported),
                            "DotSourceNotSupported",
                            ErrorCategory.InvalidOperation,
                            targetObject: null);
                        errorRecord.SetInvocationInfo(invocationInfo);
                        throw new CmdletInvocationException(errorRecord);
                    }

                    string scriptBlockId = scriptBlock.GetFileName() ?? string.Empty;
                    SystemPolicy.LogWDACAuditMessage(
                        context: context,
                        title: CommandBaseStrings.WDACLogTitle,
                        message: StringUtil.Format(CommandBaseStrings.WDACLogMessage, scriptBlockId, scriptBlock.LanguageMode, languageMode),
                        fqid: "ScriptBlockDotSourceNotAllowed",
                        dropIntoDebugger: true);
                }
            }
        }

        
        protected ExecutionContext _context;

        internal ExecutionContext Context
        {
            get { return _context; }

            set { _context = value; }
        }

        
        internal Guid PipelineActivityId { get; set; } = Guid.Empty;

        #endregion properties

        #region methods

        #region handling of -? parameter

        
        internal virtual bool IsHelpRequested(out string helpTarget, out HelpCategory helpCategory)
        {
            // by default we don't handle "-?" parameter at all
            // (we want to do the checks only for cmdlets - this method is overridden in CommandProcessor)
            helpTarget = null;
            helpCategory = HelpCategory.None;
            return false;
        }

        
        internal static CommandProcessorBase CreateGetHelpCommandProcessor(
            ExecutionContext context,
            string helpTarget,
            HelpCategory helpCategory)
        {
            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(helpTarget))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(helpTarget));
            }

            CommandProcessorBase helpCommandProcessor = context.CreateCommand("get-help", false);
            var cpi = CommandParameterInternal.CreateParameterWithArgument(
                null, "Name", "-Name:",
                null, helpTarget,
                false);
            helpCommandProcessor.AddParameter(cpi);
            cpi = CommandParameterInternal.CreateParameterWithArgument(
                null, "Category", "-Category:",
                null, helpCategory.ToString(),
                false);
            helpCommandProcessor.AddParameter(cpi);
            return helpCommandProcessor;
        }

        #endregion

        
        internal bool IsPipelineInputExpected()
        {
            return commandRuntime.IsPipelineInputExpected;
        }

        
        internal SessionStateInternal CommandSessionState { get; set; }

        
        protected internal SessionStateScope CommandScope { get; protected set; }

        protected virtual void OnSetCurrentScope()
        {
        }

        protected virtual void OnRestorePreviousScope()
        {
        }

        
        internal void SetCurrentScopeToExecutionScope()
        {
            // Make sure we have a session state instance for this command.
            // If one hasn't been explicitly set, then use the session state
            // available on the engine execution context...
            CommandSessionState ??= Context.EngineSessionState;

            // Store off the current scope
            _previousScope = CommandSessionState.CurrentScope;
            _previousCommandSessionState = Context.EngineSessionState;
            Context.EngineSessionState = CommandSessionState;

            // Set the current scope to the pipeline execution scope
            CommandSessionState.CurrentScope = CommandScope;

            OnSetCurrentScope();
        }

        
        internal void RestorePreviousScope()
        {
            OnRestorePreviousScope();

            Context.EngineSessionState = _previousCommandSessionState;

            // Restore the scope but use the same session state instance we
            // got it from because the command may have changed the execution context
            // session state...
            CommandSessionState.CurrentScope = _previousScope;
        }

        private SessionStateScope _previousScope;
        private SessionStateInternal _previousCommandSessionState;

        
        internal Collection<CommandParameterInternal> arguments = new Collection<CommandParameterInternal>();

        
        internal void AddParameter(CommandParameterInternal parameter)
        {
            Diagnostics.Assert(parameter != null, "Caller to verify parameter argument");
            arguments.Add(parameter);
        }

        
        internal abstract void Prepare(IDictionary psDefaultParameterValues);

        
        private void HandleObsoleteCommand(ObsoleteAttribute obsoleteAttr)
        {
            string commandName =
                string.IsNullOrEmpty(CommandInfo.Name)
                    ? "script block"
                    : string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                    CommandBaseStrings.ObsoleteCommand, CommandInfo.Name);

            string warningMsg = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                CommandBaseStrings.UseOfDeprecatedCommandWarning,
                commandName, obsoleteAttr.Message);

            // We ignore the IsError setting because we don't want to break people when obsoleting a command
            using (this.CommandRuntime.AllowThisCommandToWrite(false))
            {
                this.CommandRuntime.WriteWarning(new WarningRecord(FQIDCommandObsolete, warningMsg));
            }
        }

        
        internal void DoPrepare(IDictionary psDefaultParameterValues)
        {
            CommandProcessorBase oldCurrentCommandProcessor = _context.CurrentCommandProcessor;
            try
            {
                Context.CurrentCommandProcessor = this;
                SetCurrentScopeToExecutionScope();
                Prepare(psDefaultParameterValues);

                // Check obsolete attribute after Prepare so that -WarningAction will be respected for cmdlets
                if (ObsoleteAttribute != null)
                {
                    // Obsolete command is rare. Put the IF here to avoid method call overhead
                    HandleObsoleteCommand(ObsoleteAttribute);
                }
            }
            catch (InvalidComObjectException e)
            {
                // This type of exception could be thrown from parameter binding.
                string msg = StringUtil.Format(ParserStrings.InvalidComObjectException, e.Message);
                var newEx = new RuntimeException(msg, e);

                newEx.SetErrorId("InvalidComObjectException");
                throw newEx;
            }
            finally
            {
                Context.CurrentCommandProcessor = oldCurrentCommandProcessor;
                RestorePreviousScope();
            }
        }

        
        internal virtual void DoBegin()
        {
            // Note that DoPrepare() and DoBegin() should NOT be combined.
            // Reason: Encoding of commandline parameters happen as part
            // of DoPrepare(). If they are combined, the first command's
            // DoBegin() will be called before the next command's
            // DoPrepare(). Since BeginProcessing() can write objects
            // to the downstream commandlet, it will end up calling
            // DoExecute() (from Pipe.Add()) before DoPrepare.
            if (!RanBeginAlready)
            {
                RanBeginAlready = true;
                Pipe oldErrorOutputPipe = _context.ShellFunctionErrorOutputPipe;
                CommandProcessorBase oldCurrentCommandProcessor = _context.CurrentCommandProcessor;
                try
                {
                    //
                    // On V1 the output pipe was redirected to the command's output pipe only when it
                    // was already redirected. This is the original comment explaining this behaviour:
                    //
                    //      NTRAID#Windows Out of Band Releases-926183-2005-12-15
                    //      MonadTestHarness has a bad dependency on an artifact of the current implementation
                    //      The following code only redirects the output pipe if it's already redirected
                    //      to preserve the artifact. The test suites need to be fixed and then this
                    //      the check can be removed and the assignment always done.
                    //
                    // However, this makes the hosting APIs behave differently than commands executed
                    // from the command-line host (for example, see bugs Win7:415915 and Win7:108670).
                    // The RedirectShellErrorOutputPipe flag is used by the V2 hosting API to force the
                    // redirection.
                    //
                    if (RedirectShellErrorOutputPipe || _context.ShellFunctionErrorOutputPipe is not null)
                    {
                        _context.ShellFunctionErrorOutputPipe = commandRuntime.ErrorOutputPipe;
                    }

                    _context.CurrentCommandProcessor = this;
                    SetCurrentScopeToExecutionScope();

                    using (commandRuntime.AllowThisCommandToWrite(true))
                    using (ParameterBinderBase.bindingTracer.TraceScope("CALLING BeginProcessing"))
                    {
                        if (Context._debuggingMode > 0 && Command is not PSScriptCmdlet)
                        {
                            Context.Debugger.CheckCommand(Command.MyInvocation);
                        }

                        Command.DoBeginProcessing();
                    }
                }
                catch (Exception e)
                {
                    // This cmdlet threw an exception, so
                    // wrap it and bubble it up.
                    throw ManageInvocationException(e);
                }
                finally
                {
                    _context.ShellFunctionErrorOutputPipe = oldErrorOutputPipe;
                    _context.CurrentCommandProcessor = oldCurrentCommandProcessor;
                    RestorePreviousScope();
                }
            }
        }

        
        internal abstract void ProcessRecord();

        
        internal void DoExecute()
        {
            ExecutionContext.CheckStackDepth();

            CommandProcessorBase oldCurrentCommandProcessor = _context.CurrentCommandProcessor;
            try
            {
                Context.CurrentCommandProcessor = this;
                SetCurrentScopeToExecutionScope();
                ProcessRecord();
            }
            finally
            {
                Context.CurrentCommandProcessor = oldCurrentCommandProcessor;
                RestorePreviousScope();
            }
        }

        
        internal virtual void Complete()
        {
            // Call ProcessRecord once from complete. Don't call DoExecute...
            ProcessRecord();

            try
            {
                using (commandRuntime.AllowThisCommandToWrite(true))
                using (ParameterBinderBase.bindingTracer.TraceScope("CALLING EndProcessing"))
                {
                    this.Command.DoEndProcessing();
                }
            }
            catch (Exception e)
            {
                // This cmdlet threw an exception, wrap it as needed and bubble it up.
                throw ManageInvocationException(e);
            }
        }

        
        internal void DoComplete()
        {
            Pipe oldErrorOutputPipe = _context.ShellFunctionErrorOutputPipe;
            CommandProcessorBase oldCurrentCommandProcessor = _context.CurrentCommandProcessor;
            try
            {
                //
                // On V1 the output pipe was redirected to the command's output pipe only when it
                // was already redirected. This is the original comment explaining this behaviour:
                //
                //      NTRAID#Windows Out of Band Releases-926183-2005-12-15
                //      MonadTestHarness has a bad dependency on an artifact of the current implementation
                //      The following code only redirects the output pipe if it's already redirected
                //      to preserve the artifact. The test suites need to be fixed and then this
                //      the check can be removed and the assignment always done.
                //
                // However, this makes the hosting APIs behave differently than commands executed
                // from the command-line host (for example, see bugs Win7:415915 and Win7:108670).
                // The RedirectShellErrorOutputPipe flag is used by the V2 hosting API to force the
                // redirection.
                //
                if (RedirectShellErrorOutputPipe || _context.ShellFunctionErrorOutputPipe is not null)
                {
                    _context.ShellFunctionErrorOutputPipe = commandRuntime.ErrorOutputPipe;
                }

                _context.CurrentCommandProcessor = this;
                SetCurrentScopeToExecutionScope();
                Complete();
            }
            finally
            {
                _context.ShellFunctionErrorOutputPipe = oldErrorOutputPipe;
                _context.CurrentCommandProcessor = oldCurrentCommandProcessor;

                RestorePreviousScope();
            }
        }

        protected virtual void CleanResource()
        {
            try
            {
                using (commandRuntime.AllowThisCommandToWrite(permittedToWriteToPipeline: true))
                using (ParameterBinderBase.bindingTracer.TraceScope("CALLING CleanResource"))
                {
                    Command.DoCleanResource();
                }
            }
            catch (HaltCommandException)
            {
                throw;
            }
            catch (FlowControlException)
            {
                throw;
            }
            catch (Exception e)
            {
                // This cmdlet threw an exception, so wrap it and bubble it up.
                throw ManageInvocationException(e);
            }
        }

        internal void DoCleanup()
        {
            // The property 'PropagateExceptionsToEnclosingStatementBlock' controls whether a general exception
            // (an exception thrown from a .NET method invocation, or an expression like '1/0') will be turned
            // into a terminating error, which will be propagated up and thus stop the rest of the running script.
            // It is usually used by TryStatement and TrapStatement, which makes the general exception catch-able.
            //
            // For the 'Clean' block, we don't want to bubble up the general exception when the command is enclosed
            // in a TryStatement or has TrapStatement accompanying, because no exception can escape from 'Clean' and
            // thus it's pointless to bubble up the general exception in this case.
            //
            // Therefore we set this property to 'false' here to mask off the previous setting that could be from a
            // TryStatement or TrapStatement. Example:
            //   PS:1> function b { end {} clean { 1/0; Write-Host 'clean' } }
            //   PS:2> b
            //   RuntimeException: Attempted to divide by zero.
            //   clean
            //   ## Note that, outer 'try/trap' doesn't affect the general exception happens in 'Clean' block.
            //   ## so its behavior is consistent regardless of whether the command is enclosed by 'try/catch' or not.
            //   PS:3> try { b } catch { 'outer catch' }
            //   RuntimeException: Attempted to divide by zero.
            //   clean
            //
            // Be noted that, this doesn't affect the TryStatement/TrapStatement within the 'Clean' block. Example:
            //   ## 'try/trap' within 'Clean' block makes the general exception catch-able.
            //   PS:3> function a { end {} clean { try { 1/0; Write-Host 'clean' } catch { Write-Host "caught: $_" } } }
            //   PS:4> a
            //   caught: Attempted to divide by zero.
            bool oldExceptionPropagationState = _context.PropagateExceptionsToEnclosingStatementBlock;
            _context.PropagateExceptionsToEnclosingStatementBlock = false;

            Pipe oldErrorOutputPipe = _context.ShellFunctionErrorOutputPipe;
            CommandProcessorBase oldCurrentCommandProcessor = _context.CurrentCommandProcessor;

            try
            {
                if (RedirectShellErrorOutputPipe || _context.ShellFunctionErrorOutputPipe is not null)
                {
                    _context.ShellFunctionErrorOutputPipe = commandRuntime.ErrorOutputPipe;
                }

                _context.CurrentCommandProcessor = this;
                SetCurrentScopeToExecutionScope();
                CleanResource();
            }
            finally
            {
                _context.PropagateExceptionsToEnclosingStatementBlock = oldExceptionPropagationState;
                _context.ShellFunctionErrorOutputPipe = oldErrorOutputPipe;
                _context.CurrentCommandProcessor = oldCurrentCommandProcessor;

                RestorePreviousScope();
            }
        }

        internal void ReportCleanupError(Exception exception)
        {
            var error = exception is IContainsErrorRecord icer
                ? icer.ErrorRecord
                : new ErrorRecord(exception, "Clean.ReportException", ErrorCategory.NotSpecified, targetObject: null);

            PSObject errorWrap = PSObject.AsPSObject(error);
            errorWrap.WriteStream = WriteStreamType.Error;

            var errorPipe = commandRuntime.ErrorMergeTo == MshCommandRuntime.MergeDataStream.Output
                ? commandRuntime.OutputPipe
                : commandRuntime.ErrorOutputPipe;

            errorPipe.Add(errorWrap);
            _context.QuestionMarkVariableValue = false;
        }

        
        public override string ToString()
        {
            if (CommandInfo != null)
                return CommandInfo.ToString();
            return "<NullCommandInfo>"; // does not require localization
        }

        
        private bool _firstCallToRead = true;

        
        internal virtual bool Read()
        {
            // Prepare the default value parameter list if this is the first call to Read
            if (_firstCallToRead)
            {
                _firstCallToRead = false;
            }

            // Retrieve the object from the input pipeline
            object inputObject = this.commandRuntime.InputPipe.Retrieve();

            if (inputObject == AutomationNull.Value)
            {
                return false;
            }

            // If we are reading input for the first command in the pipeline increment PipelineIterationInfo[0], which is the number of items read from the input
            if (this.Command.MyInvocation.PipelinePosition == 1)
            {
                this.Command.MyInvocation.PipelineIterationInfo[0]++;
            }

            Command.CurrentPipelineObject = LanguagePrimitives.AsPSObjectOrNull(inputObject);

            return true;
        }

        
        internal PipelineStoppedException ManageInvocationException(Exception e)
        {
            try
            {
                if (Command != null)
                {
                    do // false loop
                    {
                        if (e is ProviderInvocationException pie)
                        {
                            // If a ProviderInvocationException occurred, discard the ProviderInvocationException
                            // and re-wrap it in CmdletProviderInvocationException.
                            e = new CmdletProviderInvocationException(pie, Command.MyInvocation);
                            break;
                        }

                        // HaltCommandException will cause the command to stop, but not be reported as an error.
                        // FlowControlException should not be wrapped.
                        if (e is PipelineStoppedException
                            || e is CmdletInvocationException
                            || e is ActionPreferenceStopException
                            || e is HaltCommandException
                            || e is FlowControlException
                            || e is ScriptCallDepthException)
                        {
                            // do nothing; do not rewrap these exceptions
                            break;
                        }

                        RuntimeException rte = e as RuntimeException;
                        if (rte != null && rte.WasThrownFromThrowStatement)
                        {
                            // do not rewrap a script based throw
                            break;
                        }

                        // wrap all other exceptions
                        e = new CmdletInvocationException(e, Command.MyInvocation);
                    } while (false);

                    // commandRuntime.ManageException will always throw PipelineStoppedException
                    // Otherwise, just return this exception...

                    // If this exception happened in a transacted cmdlet,
                    // rollback the transaction
                    if (commandRuntime.UseTransaction)
                    {
                        // The "transaction timed out" exception is
                        // exceedingly obtuse. We clarify things here.
                        bool isTimeoutException = false;
                        Exception tempException = e;
                        while (tempException != null)
                        {
                            if (tempException is System.TimeoutException)
                            {
                                isTimeoutException = true;
                                break;
                            }

                            tempException = tempException.InnerException;
                        }

                        if (isTimeoutException)
                        {
                            ErrorRecord errorRecord = new ErrorRecord(
                                new InvalidOperationException(
                                    TransactionStrings.TransactionTimedOut),
                                "TRANSACTION_TIMEOUT",
                                ErrorCategory.InvalidOperation,
                                e);
                            errorRecord.SetInvocationInfo(Command.MyInvocation);

                            e = new CmdletInvocationException(errorRecord);
                        }

                        // Rollback the transaction in the case of errors.
                        if (
                            _context.TransactionManager.HasTransaction
                            &&
                            _context.TransactionManager.RollbackPreference != RollbackSeverity.Never
                           )
                        {
                            Context.TransactionManager.Rollback(true);
                        }
                    }

                    return (PipelineStoppedException)this.commandRuntime.ManageException(e);
                }

                // Upstream cmdlets see only that execution stopped
                // This should only happen if Command is null
                return new PipelineStoppedException();
            }
            catch (Exception)
            {
                // this method should not throw exceptions; warn about any violations on checked builds and re-throw
                Diagnostics.Assert(false, "This method should not throw exceptions!");
                throw;
            }
        }

        
        internal void ManageScriptException(RuntimeException e)
        {
            if (Command != null && commandRuntime.PipelineProcessor != null)
            {
                commandRuntime.PipelineProcessor.RecordFailure(e, Command);

                // An explicit throw is written to $error as an ErrorRecord, so we
                // skip adding what is more or less a duplicate.
                if (e is not PipelineStoppedException && !e.WasThrownFromThrowStatement)
                    commandRuntime.AppendErrorToVariables(e);
            }
            // Upstream cmdlets see only that execution stopped
            throw new PipelineStoppedException();
        }

        
        internal void ForgetScriptException()
        {
            if (Command != null && commandRuntime.PipelineProcessor != null)
            {
                commandRuntime.PipelineProcessor.ForgetFailure();
            }
        }

        #endregion methods

        #region IDispose
        // 2004/03/05-JonN BrucePay has suggested that the IDispose
        // implementations in PipelineProcessor and CommandProcessor can be
        // removed.
        private bool _disposed;

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (UseLocalScope)
                {
                    // Clean up the PS drives that are associated with this local scope.
                    // This operation may be needed at multiple stages depending on whether the 'clean' block is declared:
                    //  1. when there is a 'clean' block, it needs to be done only after 'clean' block runs, because the scope
                    //     needs to be preserved until the 'clean' block finish execution.
                    //  2. when there is no 'clean' block, it needs to be done when
                    //      (1) there is any exception thrown from 'DoPrepare()', 'DoBegin()', 'DoExecute()', or 'DoComplete';
                    //      (2) OR, the command runs to the end successfully;
                    // Doing this cleanup at those multiple stages is cumbersome. Since we will always dispose the command in
                    // the end, doing this cleanup here will cover all the above cases.
                    CommandSessionState.RemoveScope(CommandScope);
                }

                if (Command is IDisposable id)
                {
                    id.Dispose();
                }
            }

            _disposed = true;
        }

        #endregion IDispose
    }
}
