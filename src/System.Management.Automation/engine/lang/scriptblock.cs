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
    
    /// <remarks>
    /// This class track a block of script in a compiled form. It is also
    /// used for direct invocation of the script block.
    ///
    /// 1. Overview
    ///
    /// Script block comes in two forms,
    ///
    /// a. Full form (cmdlet form)
    ///
    /// This comes in following format
    ///
    /// {
    ///     begin
    ///     {
    ///         statementlist;
    ///     }
    ///     process
    ///     {
    ///         statementlist;
    ///     }
    ///     end
    ///     {
    ///         statementlist;
    ///     }
    /// }
    ///
    /// This form is used for running the script in a pipeline like
    /// a cmdlet.
    ///
    /// b. Simple form
    ///
    /// This comes in following format
    ///
    /// {
    ///     statementlist;
    /// }
    ///
    /// 2. Script block execution
    ///
    /// For the full form (or cmdlet form) of script block, the script
    /// block itself is part of a pipeline. Its execution is handled through
    /// ScriptCommandProcessor, which involves execution of begin/process/end
    /// blocks like a cmdlet. If a scriptblock in simple form is used in
    /// a pipeline, its execution is done through ScriptCommandProcessor
    /// also, with some of begin/process/end blocks default to be empty.
    ///
    /// A script block in simple form can be directly invoked (outside
    /// of a pipeline context). For example,
    ///
    ///     {"text"}.Invoke()
    ///
    /// A scriptblock can be directly invoked internally or externally through
    /// runspace API.
    ///
    /// This class will handle the logic for direct invocation of script blocks.
    /// </remarks>
    public partial class ScriptBlock
    {
        
        /// <param name="context">Engine context for this script block.</param>
        /// <param name="script">The string to compile.</param>
        internal static ScriptBlock Create(ExecutionContext context, string script)
        {
            ScriptBlock sb = Create(context.Engine.EngineParser, null, script);
            if (context.EngineSessionState != null && context.EngineSessionState.Module != null)
            {
                sb.SessionStateInternal = context.EngineSessionState;
            }

            return sb;
        }

        
        /// <param name="script">The string to compile.</param>
        public static ScriptBlock Create(string script) => Create(
            parser: new Parser(),
            fileName: null,
            fileContents: script);

        internal static ScriptBlock CreateDelayParsedScriptBlock(string script, bool isProductCode)
            => new ScriptBlock(new CompiledScriptBlockData(script, isProductCode)) { DebuggerHidden = true };

        
        /// <returns></returns>
        public ScriptBlock GetNewClosure()
        {
            PSModuleInfo m = new PSModuleInfo(true);
            m.CaptureLocals();
            return m.NewBoundScriptBlock(this);
        }

        
        /// <remarks>
        /// Some ScriptBlocks are too complicated to be converted into a PowerShell object.
        /// For those ScriptBlocks a <see cref="ScriptBlockToPowerShellNotSupportedException"/> is thrown.
        ///
        /// ScriptBlock cannot be converted into a PowerShell object if
        /// - It contains more than one statement
        /// - It references variables undeclared in <c>param(...)</c> block
        /// - It uses redirection to a file
        /// - It uses dot sourcing
        /// - Command names can't be resolved (i.e. if an element of a pipeline is another scriptblock)
        ///
        /// Declaration of variables in a <c>param(...)</c> block is enforced,
        /// because undeclared variables are assumed to be variables from a remoting server.
        /// Since we need to fully evaluate parameters of commands of a PowerShell object's
        /// we reject all variables references that refer to a variable from a remoting server.
        /// </remarks>
        /// <param name="args">
        /// arguments for the ScriptBlock (providing values for variables used within the ScriptBlock);
        /// can be null
        /// </param>
        /// <returns>
        /// PowerShell object representing the pipeline contained in this ScriptBlock
        /// </returns>
        /// <exception cref="ScriptBlockToPowerShellNotSupportedException">
        /// Thrown when this ScriptBlock cannot be expressed as a PowerShell object.
        /// For example thrown when there is more than one statement, if there
        /// are undeclared variables, if redirection to a file is used.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// Thrown when evaluation of command arguments results in an exception.
        /// Might depend on the value of $errorActionPreference variable.
        /// For example trying to translate the following ScriptBlock will result in this exception:
        /// <c>$errorActionPreference = "stop"; $sb = { get-foo $( throw ) }; $sb.GetPowerShell()</c>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when there is no ExecutionContext associated with this ScriptBlock object.
        /// </exception>
        public PowerShell GetPowerShell(params object[] args) => GetPowerShellImpl(
            context: LocalPipeline.GetExecutionContextFromTLS(),
            variables: null,
            isTrustedInput: false,
            filterNonUsingVariables: false,
            createLocalScope: null,
            args);

        
        /// <param name="isTrustedInput">
        /// Specifies whether the scriptblock being converted comes from a trusted source.
        /// The default is False.
        /// </param>
        /// <param name="args">
        /// arguments for the ScriptBlock (providing values for variables used within the ScriptBlock);
        /// can be null
        /// </param>
        public PowerShell GetPowerShell(bool isTrustedInput, params object[] args)
            => GetPowerShellImpl(
                context: LocalPipeline.GetExecutionContextFromTLS(),
                variables: null,
                isTrustedInput,
                filterNonUsingVariables: false,
                createLocalScope: null,
                args);

        
        /// <param name="variables">
        /// variables to be supplied as context to the ScriptBlock (providing values for variables explicitly
        /// requested by the 'using:' prefix.
        /// </param>
        /// <param name="args">
        /// arguments for the ScriptBlock (providing values for variables used within the ScriptBlock);
        /// can be null
        /// </param>
        /// <returns>
        /// PowerShell object representing the pipeline contained in this ScriptBlock
        /// </returns>
        /// <exception cref="ScriptBlockToPowerShellNotSupportedException">
        /// Thrown when this ScriptBlock cannot be expressed as a PowerShell object.
        /// For example thrown when there is more than one statement, if there
        /// are undeclared variables, if redirection to a file is used.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// Thrown when evaluation of command arguments results in an exception.
        /// Might depend on the value of $errorActionPreference variable.
        /// For example trying to translate the following ScriptBlock will result in this exception:
        /// <c>$errorActionPreference = "stop"; $sb = { get-foo $( throw ) }; $sb.GetPowerShell()</c>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when there is no ExecutionContext associated with this ScriptBlock object and no
        /// variables are supplied.
        /// </exception>
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

        
        /// <param name="variables">
        /// variables to be supplied as context to the ScriptBlock (providing values for variables explicitly
        /// requested by the 'using:' prefix.
        /// </param>
        /// <param name="usingVariables">
        /// key-value pairs from the <para>variables</para> that actually get used by the 'using:' prefix variables
        /// </param>
        /// <param name="args">
        /// arguments for the ScriptBlock (providing values for variables used within the ScriptBlock);
        /// can be null
        /// </param>
        /// <returns>
        /// PowerShell object representing the pipeline contained in this ScriptBlock
        /// </returns>
        /// <exception cref="ScriptBlockToPowerShellNotSupportedException">
        /// Thrown when this ScriptBlock cannot be expressed as a PowerShell object.
        /// For example thrown when there is more than one statement, if there
        /// are undeclared variables, if redirection to a file is used.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// Thrown when evaluation of command arguments results in an exception.
        /// Might depend on the value of $errorActionPreference variable.
        /// For example trying to translate the following ScriptBlock will result in this exception:
        /// <c>$errorActionPreference = "stop"; $sb = { get-foo $( throw ) }; $sb.GetPowerShell()</c>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when there is no ExecutionContext associated with this ScriptBlock object and no
        /// variables are supplied.
        /// </exception>
        public PowerShell GetPowerShell(
            Dictionary<string, object> variables,
            out Dictionary<string, object> usingVariables,
            params object[] args)
            => GetPowerShell(variables, out usingVariables, isTrustedInput: false, args);

        
        /// <param name="variables">
        /// variables to be supplied as context to the ScriptBlock (providing values for variables explicitly
        /// requested by the 'using:' prefix.
        /// </param>
        /// <param name="usingVariables">
        /// key-value pairs from the <para>variables</para> that actually get used by the 'using:' prefix variables
        /// </param>
        /// <param name="args">
        /// arguments for the ScriptBlock (providing values for variables used within the ScriptBlock);
        /// can be null
        /// </param>
        /// <param name="isTrustedInput">
        /// Specifies whether the scriptblock being converted comes from a trusted source.
        /// The default is False.
        /// </param>
        /// <returns>
        /// PowerShell object representing the pipeline contained in this ScriptBlock
        /// </returns>
        /// <exception cref="ScriptBlockToPowerShellNotSupportedException">
        /// Thrown when this ScriptBlock cannot be expressed as a PowerShell object.
        /// For example thrown when there is more than one statement, if there
        /// are undeclared variables, if redirection to a file is used.
        /// </exception>
        /// <exception cref="RuntimeException">
        /// Thrown when evaluation of command arguments results in an exception.
        /// Might depend on the value of $errorActionPreference variable.
        /// For example trying to translate the following ScriptBlock will result in this exception:
        /// <c>$errorActionPreference = "stop"; $sb = { get-foo $( throw ) }; $sb.GetPowerShell()</c>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when there is no ExecutionContext associated with this ScriptBlock object and no
        /// variables are supplied.
        /// </exception>
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

        
        /// <returns>A steppable pipeline object.</returns>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly",
            MessageId = "Steppable",
            Justification = "Review this during API naming")]
        public SteppablePipeline GetSteppablePipeline()
            => GetSteppablePipelineImpl(commandOrigin: CommandOrigin.Internal, args: null);

        
        /// <returns>A steppable pipeline object.</returns>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly",
            MessageId = "Steppable",
            Justification = "Review this during API naming")]
        public SteppablePipeline GetSteppablePipeline(CommandOrigin commandOrigin)
            => GetSteppablePipelineImpl(commandOrigin, args: null);

        
        /// <returns>A steppable pipeline object.</returns>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1704:IdentifiersShouldBeSpelledCorrectly",
            MessageId = "Steppable",
            Justification = "Review this during API naming")]
        public SteppablePipeline GetSteppablePipeline(CommandOrigin commandOrigin, object[] args)
            => GetSteppablePipelineImpl(commandOrigin, args);

        
        /// <param name="args">The arguments to this script.</param>
        /// <returns>The object(s) generated during the execution of
        /// the script block returned as a collection of PSObjects.</returns>
        /// <exception cref="RuntimeException">Thrown if a script runtime exceptionexception occurred.</exception>
        /// <exception cref="FlowControlException">An internal (non-public) exception from a flow control statement.</exception>
        public Collection<PSObject> Invoke(params object[] args) =>
            DoInvoke(dollarUnder: AutomationNull.Value, input: AutomationNull.Value, args);

        
        /// <param name="functionsToDefine">A dictionary of functions to define.</param>
        /// <param name="variablesToDefine">A list of variables to define.</param>
        /// <param name="args">The arguments to the actual scriptblock.</param>
        /// <returns></returns>
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

        
        /// <param name="functionsToDefine">A dictionary of functions to define.</param>
        /// <param name="variablesToDefine">A list of variables to define.</param>
        /// <param name="args">The arguments to the actual scriptblock.</param>
        /// <returns></returns>
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

        
        /// <param name="args">The arguments to pass to this scriptblock.</param>
        /// <returns>The object(s) generated during the execution of the
        /// script block. They may or may not be wrapped in PSObject. It's up to the caller to check.</returns>
        /// <exception cref="RuntimeException">Thrown if a script runtime exceptionexception occurred.</exception>
        /// <exception cref="FlowControlException">An internal (non-public) exception from a flow control statement.</exception>
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

        
        /// <remarks>
        /// This does normal array reduction in the case of a one-element array.
        /// </remarks>
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

        
        /// <exception cref="InvalidOperationException">
        /// An attempt was made to use the scriptblock outside the engine.
        /// </exception>
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

        
        /// <param name="dollarUnder">
        /// The value of the $_ variable for the script block. If AutomationNull.Value,
        /// the $_ variable is not created.
        /// </param>
        /// <param name="input">
        /// The value of the $input variable for the script block. If AutomationNull.Value,
        /// the $input variable is not created.
        /// </param>
        /// <param name="args">The arguments to this script.</param>
        /// <returns>The object(s) generated during the execution of
        /// the script block returned as a collection of PSObjects.</returns>
        /// <exception cref="RuntimeException">A script exception occurred.</exception>
        /// <exception cref="FlowControlException">Internal exception from a flow control statement.</exception>
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

        
        /// <param name="result"></param>
        /// <returns></returns>
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

        
        /// <param name="useLocalScope"></param>
        /// <param name="errorHandlingBehavior"></param>
        /// <param name="dollarUnder">
        ///   The value of the $_ variable for the script block. If AutomationNull.Value,
        ///   the $_ variable is not created.
        /// </param>
        /// <param name="input">
        ///   The value of the $input variable for the script block. If AutomationNull.Value,
        ///   the $input variable is not created.
        /// </param>
        /// <param name="scriptThis"></param>
        /// <param name="args">The arguments to this script.</param>
        /// <returns>The object(s) generated during the execution of
        /// the script block returned as a collection of PSObjects.</returns>
        /// <exception cref="RuntimeException">A script exception occurred.</exception>
        /// <exception cref="FlowControlException">Internal exception from a flow control statement.</exception>
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

        
        /// <param name="expectInput"><see langword="true"/> if you plan to write input into this pipe; <see langword="false"/> otherwise.</param>
        public void Begin(bool expectInput) => Begin(expectInput, commandRuntime: (ICommandRuntime)null);

        
        /// <param name="expectInput"><see langword="true"/> if you plan to write input into this pipe; <see langword="false"/> otherwise.</param>
        /// <param name="contextToRedirectTo">Context used to figure out how to route the output and errors.</param>
        public void Begin(bool expectInput, EngineIntrinsics contextToRedirectTo)
        {
            ArgumentNullException.ThrowIfNull(contextToRedirectTo);

            ExecutionContext executionContext = contextToRedirectTo.SessionState.Internal.ExecutionContext;
            CommandProcessorBase commandProcessor = executionContext.CurrentCommandProcessor;
            ICommandRuntime crt = commandProcessor?.CommandRuntime;
            Begin(expectInput, crt);
        }

        
        /// <param name="command">The command you're calling this from (i.e. instance of PSCmdlet or value of $PSCmdlet variable).</param>
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

        
        /// <param name="input">The object to process.</param>
        /// <returns>A collection of 0 or more result objects.</returns>
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
        
        /// <param name="input">The input object to process.</param>
        /// <returns></returns>
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

        
        /// <returns>The result of the execution.</returns>
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

        
        /// <remarks>
        /// <para>
        /// The way we handle 'Clean' blocks in a steppable pipeline makes sure that:</para>
        /// <para>1. The 'Clean' blocks get to run if any exception is thrown from 'Begin/Process/End'.</para>
        /// <para>2. The 'Clean' blocks get to run if 'End' finished successfully.</para>
        /// <para>
        /// However, this is not enough for a steppable pipeline, because the function, where the steppable
        /// pipeline gets used, may fail (think about a proxy function). And that may lead to the situation
        /// where "no exception was thrown from the steppable pipeline" but "the steppable pipeline didn't
        /// run to the end". In that case, 'Clean' won't run unless it's triggered explicitly on the steppable
        /// pipeline. This method allows a user to do that from the 'Clean' block of the proxy function.</para>
        /// </remarks>
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

        
        /// <param name="message">The exception's message.</param>
        public ScriptBlockToPowerShellNotSupportedException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public ScriptBlockToPowerShellNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="message">The error message.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal ScriptBlockToPowerShellNotSupportedException(
            string errorId,
            Exception innerException,
            string message,
            params object[] arguments)
            : base(string.Format(CultureInfo.CurrentCulture, message, arguments), innerException)
            => this.SetErrorId(errorId);

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
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
        
        /// <param name="scriptBlock">The scriptblock to invoke
        /// </param>
        /// /// <param name="useLocalScope"></param>
        /// <param name="errorHandlingBehavior"></param>
        /// <param name="dollarUnder">
        ///   The value of the $_ variable for the script block. If AutomationNull.Value,
        ///   the $_ variable is not created.
        /// </param>
        /// <param name="input">
        ///   The value of the $input variable for the script block. If AutomationNull.Value,
        ///   the $input variable is not created.
        /// </param>
        /// <param name="scriptThis"></param>
        /// <param name="outputPipe">The output pipe which has the results of the invocation
        /// </param>
        /// <param name="invocationInfo">The information about current state of the runspace.</param>
        /// <param name="args">The arguments to this script.</param>
        /// <exception cref="ArgumentNullException">ScriptBlock is null
        /// </exception>
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
