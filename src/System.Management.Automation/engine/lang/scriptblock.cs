// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public partial class ScriptBlock
    {
        
        internal static ScriptBlock Create(ExecutionContext context, string script)
        {
            ScriptBlock sb = Create(context.Engine.EngineParser, null, script);
            if (context.EngineSessionState != null && context.EngineSessionState.Module != null)
            {
                sb.SessionStateInternal = context.EngineSessionState;
            }

            return sb;
        }

        
        public static ScriptBlock Create(string script) => Create(
            parser: new Parser(),
            fileName: null,
            fileContents: script);

        internal static ScriptBlock CreateDelayParsedScriptBlock(string script, bool isProductCode)
            => new ScriptBlock(new CompiledScriptBlockData(script, isProductCode)) { DebuggerHidden = true };

        
        public ScriptBlock GetNewClosure()
        {
            PSModuleInfo m = new PSModuleInfo(true);
            m.CaptureLocals();
            return m.NewBoundScriptBlock(this);
        }

        
        public PowerShell GetPowerShell(params object[] args) => GetPowerShellImpl(
            context: LocalPipeline.GetExecutionContextFromTLS(),
            variables: null,
            isTrustedInput: false,
            filterNonUsingVariables: false,
            createLocalScope: null,
            args);

        
        public PowerShell GetPowerShell(bool isTrustedInput, params object[] args)
            => GetPowerShellImpl(
                context: LocalPipeline.GetExecutionContextFromTLS(),
                variables: null,
                isTrustedInput,
                filterNonUsingVariables: false,
                createLocalScope: null,
                args);

        
        public PowerShell GetPowerShell(Dictionary<string, object> variables, params object[] args)
        {
            ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();
            Dictionary<string, object> suppliedVariables = null;

            if (variables != null)
            {
                suppliedVariables = new Dictionary<string, object>(variables, StringComparer.OrdinalIgnoreCase);
                context = null;
            }

            return GetPowerShellImpl(context, suppliedVariables, false, false, null, args);
        }

        
        public PowerShell GetPowerShell(
            Dictionary<string, object> variables,
            out Dictionary<string, object> usingVariables,
            params object[] args)
            => GetPowerShell(variables, out usingVariables, isTrustedInput: false, args);

        
        public PowerShell GetPowerShell(
            Dictionary<string, object> variables,
            out Dictionary<string, object> usingVariables,
            bool isTrustedInput,
            params object[] args)
        {
            ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();
            Dictionary<string, object> suppliedVariables = null;

            if (variables != null)
            {
                suppliedVariables = new Dictionary<string, object>(variables, StringComparer.OrdinalIgnoreCase);
                context = null;
            }

            PowerShell powershell = GetPowerShellImpl(context, suppliedVariables, isTrustedInput, true, null, args);
            usingVariables = suppliedVariables;

            return powershell;
        }

        internal PowerShell GetPowerShell(
            ExecutionContext context,
            bool isTrustedInput,
            bool? useLocalScope,
            object[] args)
            => GetPowerShellImpl(
                context,
                variables: null,
                isTrustedInput,
                filterNonUsingVariables: false,
                useLocalScope,
                args);

        
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly",
            MessageId = "Steppable",
            Justification = "Review this during API naming")]
        public SteppablePipeline GetSteppablePipeline()
            => GetSteppablePipelineImpl(commandOrigin: CommandOrigin.Internal, args: null);

        
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly",
            MessageId = "Steppable",
            Justification = "Review this during API naming")]
        public SteppablePipeline GetSteppablePipeline(CommandOrigin commandOrigin)
            => GetSteppablePipelineImpl(commandOrigin, args: null);

        
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly",
            MessageId = "Steppable",
            Justification = "Review this during API naming")]
        public SteppablePipeline GetSteppablePipeline(CommandOrigin commandOrigin, object[] args)
            => GetSteppablePipelineImpl(commandOrigin, args);

        
        public Collection<PSObject> Invoke(params object[] args) =>
            DoInvoke(dollarUnder: AutomationNull.Value, input: AutomationNull.Value, args);

        
        public Collection<PSObject> InvokeWithContext(
            IDictionary functionsToDefine,
            List<PSVariable> variablesToDefine,
            params object[] args)
        {
            Dictionary<string, ScriptBlock> functionsToDefineDictionary = null;
            if (functionsToDefine != null)
            {
                functionsToDefineDictionary = new Dictionary<string, ScriptBlock>();
                foreach (DictionaryEntry pair in functionsToDefine)
                {
                    string functionName = pair.Key as string;
                    if (string.IsNullOrWhiteSpace(functionName))
                    {
                        PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                            ParserStrings.EmptyFunctionNameInFunctionDefinitionDictionary);

                        e.SetErrorId("EmptyFunctionNameInFunctionDefinitionDictionary");
                        throw e;
                    }

                    ScriptBlock functionBody = pair.Value as ScriptBlock;
                    // null check for functionBody is done at the lower layer.
                    functionsToDefineDictionary.Add(functionName, functionBody);
                }
            }

            return InvokeWithContext(
                functionsToDefineDictionary,
                variablesToDefine,
                args);
        }

        
        public Collection<PSObject> InvokeWithContext(
            Dictionary<string, ScriptBlock> functionsToDefine,
            List<PSVariable> variablesToDefine,
            params object[] args)
        {
            object input = AutomationNull.Value;
            object dollarUnder = AutomationNull.Value;
            object scriptThis = AutomationNull.Value;

            if (variablesToDefine != null)
            {
                // Extract the special variables "this", "input" and "_"
                PSVariable located = variablesToDefine.Find(
                    v => string.Equals(v.Name, "this", StringComparison.OrdinalIgnoreCase));
                if (located != null)
                {
                    scriptThis = located.Value;
                    variablesToDefine.Remove(located);
                }

                located = variablesToDefine.Find(
                    v => string.Equals(v.Name, "_", StringComparison.Ordinal));
                if (located != null)
                {
                    dollarUnder = located.Value;
                    variablesToDefine.Remove(located);
                }

                located = variablesToDefine.Find(
                    v => string.Equals(v.Name, "input", StringComparison.OrdinalIgnoreCase));
                if (located != null)
                {
                    input = located.Value;
                    variablesToDefine.Remove(located);
                }
            }

            List<object> result = new List<object>();
            Pipe outputPipe = new Pipe(result);

            InvokeWithPipe(
                useLocalScope: true,
                functionsToDefine: functionsToDefine,
                variablesToDefine: variablesToDefine,
                errorHandlingBehavior: ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                dollarUnder: dollarUnder,
                input: input,
                scriptThis: scriptThis,
                outputPipe: outputPipe,
                invocationInfo: null,
                args: args);
            return GetWrappedResult(result);
        }

        
        public object InvokeReturnAsIs(params object[] args)
            => DoInvokeReturnAsIs(
                useLocalScope: true,
                errorHandlingBehavior: ErrorHandlingBehavior.WriteToExternalErrorPipe,
                dollarUnder: AutomationNull.Value,
                input: AutomationNull.Value,
                scriptThis: AutomationNull.Value,
                args: args);

        internal T InvokeAsMemberFunctionT<T>(object instance, object[] args)
        {
            List<object> result = new List<object>();
            Pipe pipe = new Pipe(result);

            InvokeWithPipe(
                useLocalScope: true,
                errorHandlingBehavior: ErrorHandlingBehavior.WriteToExternalErrorPipe,
                dollarUnder: AutomationNull.Value,
                input: AutomationNull.Value,
                scriptThis: instance ?? AutomationNull.Value,
                outputPipe: pipe,
                invocationInfo: null,
                propagateAllExceptionsToTop: true,
                args: args);

            // This is needed only for the case where the
            // method returns [object]. If the argument to 'return'
            // is a pipeline that emits nothing then result.Count will
            // be zero so we catch that and "convert" it to null. Note that
            // the return statement is still required in the method, it
            // just receives nothing from it's argument.
            if (result.Count == 0)
            {
                return default(T);
            }

            return (T)result[0];
        }

        internal void InvokeAsMemberFunction(object instance, object[] args)
        {
            List<object> result = new List<object>();
            Pipe pipe = new Pipe(result);

            InvokeWithPipe(
                useLocalScope: true,
                errorHandlingBehavior: ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                dollarUnder: AutomationNull.Value,
                input: AutomationNull.Value,
                scriptThis: instance ?? AutomationNull.Value,
                outputPipe: pipe,
                invocationInfo: null,
                propagateAllExceptionsToTop: true,
                args: args);
            Diagnostics.Assert(result.Count == 0, "Code generation ensures we return the correct type");
        }

        
        public List<Attribute> Attributes { get => GetAttributes(); }

        
        public string File { get => GetFileName(); }

        
        public bool IsFilter { get => _scriptBlockData.IsFilter; }

        
        public bool IsConfiguration { get => _scriptBlockData.GetIsConfiguration(); }

        
        public PSModuleInfo Module { get => SessionStateInternal?.Module; }

        
        public PSToken StartPosition { get => GetStartPosition(); }

        // LanguageMode is a nullable PSLanguageMode enumeration because script blocks
        // need to inherit the language mode from the context in which they are executing.
        // We can't assume FullLanguage by default when there is no context, as there are
        // script blocks (such as the script blocks used in Workflow activities) that are
        // created by the host without a "current language mode" to inherit. They ultimately
        // get their language mode set when they are finally invoked in a constrained
        // language runspace.
        // Script blocks that should always be run under FullLanguage mode (i.e.: set in
        // InitialSessionState, etc.) should explicitly set the LanguageMode to FullLanguage
        // when they are created.
        internal PSLanguageMode? LanguageMode { get; set; }

        internal enum ErrorHandlingBehavior
        {
            WriteToCurrentErrorPipe = 1,
            WriteToExternalErrorPipe = 2,
            SwallowErrors = 3,
        }

        internal ReadOnlyCollection<PSTypeName> OutputType
        {
            get
            {
                List<PSTypeName> result = new List<PSTypeName>();
                foreach (Attribute attribute in Attributes)
                {
                    OutputTypeAttribute outputType = attribute as OutputTypeAttribute;
                    if (outputType != null)
                    {
                        result.AddRange(outputType.Type);
                    }
                }

                return new ReadOnlyCollection<PSTypeName>(result);
            }
        }

        
        internal static object GetRawResult(List<object> result, bool wrapToPSObject)
        {
            switch (result.Count)
            {
                case 0:
                    return AutomationNull.Value;
                case 1:
                    return wrapToPSObject ? LanguagePrimitives.AsPSObjectOrNull(result[0]) : result[0];
                default:
                    object resultArray = result.ToArray();
                    return wrapToPSObject ? LanguagePrimitives.AsPSObjectOrNull(resultArray) : resultArray;
            }
        }

        internal void InvokeUsingCmdlet(
            Cmdlet contextCmdlet,
            bool useLocalScope,
            ErrorHandlingBehavior errorHandlingBehavior,
            object dollarUnder,
            object input,
            object scriptThis,
            object[] args)
        {
            Diagnostics.Assert(contextCmdlet != null, "caller to verify contextCmdlet parameter");

            Pipe outputPipe = ((MshCommandRuntime)contextCmdlet.CommandRuntime).OutputPipe;
            ExecutionContext context = GetContextFromTLS();
            var myInv = context.EngineSessionState.CurrentScope.GetAutomaticVariableValue(AutomaticVariable.MyInvocation);
            InvocationInfo inInfo = myInv == AutomationNull.Value ? null : (InvocationInfo)myInv;
            InvokeWithPipe(
                useLocalScope,
                errorHandlingBehavior,
                dollarUnder,
                input,
                scriptThis,
                outputPipe,
                inInfo,
                propagateAllExceptionsToTop: false,
                args: args);
        }

        
        internal SessionStateInternal SessionStateInternal { get; set; }

        
        internal SessionState SessionState
        {
            get
            {
                if (SessionStateInternal == null)
                {
                    ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();
                    if (context != null)
                    {
                        SessionStateInternal = context.EngineSessionState.PublicSessionState.Internal;
                    }
                }

                return SessionStateInternal?.PublicSessionState;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(value));
                }

                SessionStateInternal = value.Internal;
            }
        }

        #region Delegates

        private static readonly ConditionalWeakTable<ScriptBlock, ConcurrentDictionary<Type, Delegate>> s_delegateTable =
            new ConditionalWeakTable<ScriptBlock, ConcurrentDictionary<Type, Delegate>>();

        internal Delegate GetDelegate(Type delegateType)
            => s_delegateTable.GetOrCreateValue(this).GetOrAdd(delegateType, CreateDelegate);

        
        internal Delegate CreateDelegate(Type delegateType)
        {
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            ParameterInfo[] parameters = invokeMethod.GetParameters();
            if (invokeMethod.ContainsGenericParameters)
            {
                throw new ScriptBlockToPowerShellNotSupportedException(
                    "CantConvertScriptBlockToOpenGenericType",
                    null,
                    "AutomationExceptions",
                    "CantConvertScriptBlockToOpenGenericType",
                    delegateType);
            }

            var parameterExprs = new List<ParameterExpression>();
            foreach (var parameter in parameters)
            {
                parameterExprs.Add(Expression.Parameter(parameter.ParameterType));
            }

            bool returnsSomething = !invokeMethod.ReturnType.Equals(typeof(void));

            Expression dollarUnderExpr;
            Expression dollarThisExpr;
            if (parameters.Length == 2 && !returnsSomething)
            {
                // V1 was designed for System.EventHandler and not much else.
                // The first arg (sender) was bound to $this, the second (e or EventArgs) was bound to $_.
                // We do this for backwards compatibility, but we also bind the parameters (or $args) for
                // consistency w/ delegates that take more or fewer parameters.
                dollarUnderExpr = parameterExprs[1].Cast(typeof(object));
                dollarThisExpr = parameterExprs[0].Cast(typeof(object));
            }
            else
            {
                dollarUnderExpr = ExpressionCache.AutomationNullConstant;
                dollarThisExpr = ExpressionCache.AutomationNullConstant;
            }

            Expression call = Expression.Call(
                Expression.Constant(this),
                CachedReflectionInfo.ScriptBlock_InvokeAsDelegateHelper,
                dollarUnderExpr,
                dollarThisExpr,
                Expression.NewArrayInit(typeof(object), parameterExprs.Select(static p => p.Cast(typeof(object)))));
            if (returnsSomething)
            {
                call = DynamicExpression.Dynamic(
                    PSConvertBinder.Get(invokeMethod.ReturnType),
                    invokeMethod.ReturnType,
                    call);
            }

            return Expression.Lambda(delegateType, call, parameterExprs).Compile();
        }

        internal object InvokeAsDelegateHelper(object dollarUnder, object dollarThis, object[] args)
        {
            // Retrieve context and current runspace to ensure that we throw exception, if this is non-default runspace.
            ExecutionContext context = GetContextFromTLS();
            RunspaceBase runspace = (RunspaceBase)context.CurrentRunspace;

            List<object> rawResult = new List<object>();
            Pipe outputPipe = new Pipe(rawResult);
            InvokeWithPipe(
                useLocalScope: true,
                errorHandlingBehavior: ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                dollarUnder: dollarUnder,
                input: null,
                scriptThis: dollarThis,
                outputPipe: outputPipe,
                invocationInfo: null,
                args: args);
            return GetRawResult(rawResult, wrapToPSObject: false);
        }

        #endregion

        
        internal ExecutionContext GetContextFromTLS()
        {
            ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();

            // If ExecutionContext from TLS is null then we are not in powershell engine thread.
            if (context == null)
            {
                string scriptText = this.ToString();

                scriptText = ErrorCategoryInfo.Ellipsize(CultureInfo.CurrentUICulture, scriptText);

                PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                    ParserStrings.ScriptBlockDelegateInvokedFromWrongThread,
                    scriptText);

                e.SetErrorId("ScriptBlockDelegateInvokedFromWrongThread");
                throw e;
            }

            return context;
        }

        
        internal Collection<PSObject> DoInvoke(object dollarUnder, object input, object[] args)
        {
            List<object> result = new List<object>();
            Pipe outputPipe = new Pipe(result);
            InvokeWithPipe(
                useLocalScope: true,
                errorHandlingBehavior: ErrorHandlingBehavior.WriteToExternalErrorPipe,
                dollarUnder: dollarUnder,
                input: input,
                scriptThis: AutomationNull.Value,
                outputPipe: outputPipe,
                invocationInfo: null,
                args: args);
            return GetWrappedResult(result);
        }

        
        private static Collection<PSObject> GetWrappedResult(List<object> result)
        {
            if (result == null || result.Count == 0)
            {
                return new Collection<PSObject>();
            }

            Collection<PSObject> wrappedResult = new Collection<PSObject>();
            for (int i = 0; i < result.Count; i++)
            {
                wrappedResult.Add(LanguagePrimitives.AsPSObjectOrNull(result[i]));
            }

            return wrappedResult;
        }

        
        internal object DoInvokeReturnAsIs(
            bool useLocalScope,
            ErrorHandlingBehavior errorHandlingBehavior,
            object dollarUnder,
            object input,
            object scriptThis,
            object[] args)
        {
            List<object> result = new List<object>();
            Pipe outputPipe = new Pipe(result);
            InvokeWithPipe(
                useLocalScope: useLocalScope,
                errorHandlingBehavior: errorHandlingBehavior,
                dollarUnder: dollarUnder,
                input: input,
                scriptThis: scriptThis,
                outputPipe: outputPipe,
                invocationInfo: null,
                args: args);
            return GetRawResult(result, wrapToPSObject: true);
        }

        internal void InvokeWithPipe(
            bool useLocalScope,
            ErrorHandlingBehavior errorHandlingBehavior,
            object dollarUnder,
            object input,
            object scriptThis,
            Pipe outputPipe,
            InvocationInfo invocationInfo,
            bool propagateAllExceptionsToTop = false,
            List<PSVariable> variablesToDefine = null,
            Dictionary<string, ScriptBlock> functionsToDefine = null,
            object[] args = null)
        {
            bool shouldGenerateEvent = false;
            bool oldPropagateExceptions = false;
            ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();

            if (SessionStateInternal != null && SessionStateInternal.ExecutionContext != context)
            {
                context = SessionStateInternal.ExecutionContext;
                shouldGenerateEvent = true;
            }
            else if (context == null)
            {
                // This will throw.
                GetContextFromTLS();
            }
            else
            {
                if (propagateAllExceptionsToTop)
                {
                    oldPropagateExceptions = context.PropagateExceptionsToEnclosingStatementBlock;
                    context.PropagateExceptionsToEnclosingStatementBlock = true;
                }

                try
                {
                    var runspace = (RunspaceBase)context.CurrentRunspace;
                    if (runspace.CanRunActionInCurrentPipeline())
                    {
                        InvokeWithPipeImpl(
                            useLocalScope,
                            functionsToDefine,
                            variablesToDefine,
                            errorHandlingBehavior,
                            dollarUnder,
                            input,
                            scriptThis,
                            outputPipe,
                            invocationInfo,
                            args);
                    }
                    else
                    {
                        shouldGenerateEvent = true;
                    }
                }
                finally
                {
                    if (propagateAllExceptionsToTop)
                    {
                        context.PropagateExceptionsToEnclosingStatementBlock = oldPropagateExceptions;
                    }
                }
            }

            if (shouldGenerateEvent)
            {
                context.Events.SubscribeEvent(
                    source: null,
                    eventName: PSEngineEvent.OnScriptBlockInvoke,
                    sourceIdentifier: PSEngineEvent.OnScriptBlockInvoke,
                    data: null,
                    handlerDelegate: new PSEventReceivedEventHandler(OnScriptBlockInvokeEventHandler),
                    supportEvent: true,
                    forwardEvent: false,
                    shouldQueueAndProcessInExecutionThread: true,
                    maxTriggerCount: 1);

                var scriptBlockInvocationEventArgs = new ScriptBlockInvocationEventArgs(
                    scriptBlock: this,
                    useLocalScope,
                    errorHandlingBehavior,
                    dollarUnder,
                    input,
                    scriptThis,
                    outputPipe,
                    invocationInfo,
                    args);

                context.Events.GenerateEvent(
                    sourceIdentifier: PSEngineEvent.OnScriptBlockInvoke,
                    sender: null,
                    args: new object[1] { scriptBlockInvocationEventArgs },
                    extraData: null,
                    processInCurrentThread: true,
                    waitForCompletionInCurrentThread: true);

                scriptBlockInvocationEventArgs.Exception?.Throw();
            }
        }

        
        private static void OnScriptBlockInvokeEventHandler(object sender, PSEventArgs args)
        {
            var eventArgs = (object)args.SourceEventArgs as ScriptBlockInvocationEventArgs;
            Diagnostics.Assert(eventArgs != null,
                "Event Arguments to OnScriptBlockInvokeEventHandler should not be null");

            try
            {
                ScriptBlock sb = eventArgs.ScriptBlock;
                sb.InvokeWithPipeImpl(
                    eventArgs.UseLocalScope,
                    functionsToDefine: null,
                    variablesToDefine: null,
                    eventArgs.ErrorHandlingBehavior,
                    eventArgs.DollarUnder,
                    eventArgs.Input,
                    eventArgs.ScriptThis,
                    eventArgs.OutputPipe,
                    eventArgs.InvocationInfo,
                    eventArgs.Args);
            }
            catch (Exception e)
            {
                eventArgs.Exception = ExceptionDispatchInfo.Capture(e);
            }
        }

        internal void SetPSScriptRootAndPSCommandPath(MutableTuple locals, ExecutionContext context)
        {
            var psScriptRoot = string.Empty;
            var psCommandPath = string.Empty;
            if (!string.IsNullOrEmpty(File))
            {
                psScriptRoot = Path.GetDirectoryName(File);
                psCommandPath = File;
            }

            locals.SetAutomaticVariable(AutomaticVariable.PSScriptRoot, psScriptRoot, context);
            locals.SetAutomaticVariable(AutomaticVariable.PSCommandPath, psCommandPath, context);
        }
    }

    
    [SuppressMessage(
        "Microsoft.Naming",
        "CA1704:IdentifiersShouldBeSpelledCorrectly",
        MessageId = "Steppable",
        Justification = "Consider Name change during API review")]
    public sealed class SteppablePipeline : IDisposable
    {
        internal SteppablePipeline(ExecutionContext context, PipelineProcessor pipeline)
        {
            ArgumentNullException.ThrowIfNull(pipeline);

            ArgumentNullException.ThrowIfNull(context);

            _pipeline = pipeline;
            _context = context;
        }

        private readonly PipelineProcessor _pipeline;
        private readonly ExecutionContext _context;
        private bool _expectInput;

        
        public void Begin(bool expectInput) => Begin(expectInput, commandRuntime: (ICommandRuntime)null);

        
        public void Begin(bool expectInput, EngineIntrinsics contextToRedirectTo)
        {
            ArgumentNullException.ThrowIfNull(contextToRedirectTo);

            ExecutionContext executionContext = contextToRedirectTo.SessionState.Internal.ExecutionContext;
            CommandProcessorBase commandProcessor = executionContext.CurrentCommandProcessor;
            ICommandRuntime crt = commandProcessor?.CommandRuntime;
            Begin(expectInput, crt);
        }

        
        public void Begin(InternalCommand command)
        {
            if (command is null || command.MyInvocation is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            Begin(command.MyInvocation.ExpectingInput, command.commandRuntime);
        }

        private void Begin(bool expectInput, ICommandRuntime commandRuntime)
        {
            try
            {
                _pipeline.ExecutionScope = _context.EngineSessionState.CurrentScope;
                _context.PushPipelineProcessor(_pipeline);
                _expectInput = expectInput;

                // Start the pipeline, if the command calling this pipeline is
                // not expecting input (as indicated by it's position in the pipeline
                // then neither should we.
                MshCommandRuntime crt = commandRuntime as MshCommandRuntime;
                if (crt != null)
                {
                    if (crt.OutputPipe != null)
                    {
                        _pipeline.LinkPipelineSuccessOutput(crt.OutputPipe);
                    }

                    if (crt.ErrorOutputPipe != null)
                    {
                        _pipeline.LinkPipelineErrorOutput(crt.ErrorOutputPipe);
                    }
                }

                _pipeline.StartStepping(_expectInput);
            }
            finally
            {
                // then pop this pipeline...
                _context.PopPipelineProcessor(true);
            }
        }

        
        public Array Process(object input)
        {
            try
            {
                _context.PushPipelineProcessor(_pipeline);
                if (_expectInput)
                {
                    return _pipeline.Step(input);
                }
                else
                {
                    return _pipeline.Step(AutomationNull.Value);
                }
            }
            finally
            {
                // then pop this pipeline...
                _context.PopPipelineProcessor(true);
            }
        }
        
        public Array Process(PSObject input)
        {
            try
            {
                _context.PushPipelineProcessor(_pipeline);
                if (_expectInput)
                {
                    return _pipeline.Step(input);
                }
                else
                {
                    return _pipeline.Step(AutomationNull.Value);
                }
            }
            finally
            {
                // then pop this pipeline...
                _context.PopPipelineProcessor(true);
            }
        }

        
        public Array Process()
        {
            try
            {
                _context.PushPipelineProcessor(_pipeline);
                return _pipeline.Step(AutomationNull.Value);
            }
            finally
            {
                // then pop this pipeline...
                _context.PopPipelineProcessor(true);
            }
        }

        
        public Array End()
        {
            try
            {
                _context.PushPipelineProcessor(_pipeline);
                return _pipeline.DoComplete();
            }
            finally
            {
                // then pop this pipeline and dispose it...
                _context.PopPipelineProcessor(true);
                Dispose();
            }
        }

        
        public void Clean()
        {
            if (_pipeline.Commands is null)
            {
                // The pipeline commands have been disposed. In this case, 'Clean'
                // should have already been called on the pipeline processor.
                return;
            }

            try
            {
                _context.PushPipelineProcessor(_pipeline);
                _pipeline.DoCleanup();
            }
            finally
            {
                // then pop this pipeline and dispose it...
                _context.PopPipelineProcessor(true);
                Dispose();
            }
        }

        #region IDispose

        private bool _disposed;

        
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _pipeline.Dispose();
            _disposed = true;
        }

        #endregion IDispose
    }

    
    public class ScriptBlockToPowerShellNotSupportedException : RuntimeException
    {
        #region ctor

        
        public ScriptBlockToPowerShellNotSupportedException()
            : base(typeof(ScriptBlockToPowerShellNotSupportedException).FullName)
        {
        }

        
        public ScriptBlockToPowerShellNotSupportedException(string message)
            : base(message)
        {
        }

        
        public ScriptBlockToPowerShellNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        internal ScriptBlockToPowerShellNotSupportedException(
            string errorId,
            Exception innerException,
            string message,
            params object[] arguments)
            : base(string.Format(CultureInfo.CurrentCulture, message, arguments), innerException)
            => this.SetErrorId(errorId);

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ScriptBlockToPowerShellNotSupportedException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        #endregion ctor

    }

    
    internal sealed class ScriptBlockInvocationEventArgs : EventArgs
    {
        
        internal ScriptBlockInvocationEventArgs(
            ScriptBlock scriptBlock,
            bool useLocalScope,
            ScriptBlock.ErrorHandlingBehavior errorHandlingBehavior,
            object dollarUnder,
            object input,
            object scriptThis,
            Pipe outputPipe,
            InvocationInfo invocationInfo,
            object[] args)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(scriptBlock));
            }

            ScriptBlock = scriptBlock;
            OutputPipe = outputPipe;
            UseLocalScope = useLocalScope;
            ErrorHandlingBehavior = errorHandlingBehavior;
            DollarUnder = dollarUnder;
            Input = input;
            ScriptThis = scriptThis;
            InvocationInfo = invocationInfo;
            Args = args;
        }

        internal ScriptBlock ScriptBlock { get; set; }

        internal bool UseLocalScope { get; set; }

        internal ScriptBlock.ErrorHandlingBehavior ErrorHandlingBehavior { get; set; }

        internal object DollarUnder { get; set; }

        internal object Input { get; set; }

        internal object ScriptThis { get; set; }

        internal Pipe OutputPipe { get; set; }

        internal InvocationInfo InvocationInfo { get; set; }

        internal object[] Args { get; set; }

        
        internal ExceptionDispatchInfo Exception { get; set; }
    }
}
