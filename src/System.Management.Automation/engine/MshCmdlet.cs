// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Reflection;

using PipelineResultTypes = System.Management.Automation.Runspaces.PipelineResultTypes;

namespace System.Management.Automation
{
    #region Auxiliary

    
    /// <remarks>
    /// Dynamic parameters allow a
    /// <see cref="Cmdlet"/> or <see cref="Provider.CmdletProvider"/>
    /// to define additional parameters based on the value of
    /// the formal arguments.  For example, the parameters of
    /// "set-itemproperty" for the file system provider vary
    /// depending on whether the target object is a file or directory.
    /// </remarks>
    /// <seealso cref="Cmdlet"/>
    /// <seealso cref="PSCmdlet"/>
    /// <seealso cref="RuntimeDefinedParameter"/>
    /// <seealso cref="RuntimeDefinedParameterDictionary"/>
#nullable enable
    public interface IDynamicParameters
    {
        
        /// <returns>
        /// This method should return an object that has properties and fields
        /// decorated with parameter attributes similar to a
        /// <see cref="Cmdlet"/> or <see cref="Provider.CmdletProvider"/>.
        /// These attributes include <see cref="ParameterAttribute"/>,
        /// <see cref="AliasAttribute"/>, argument transformation and
        /// validation attributes, etc.
        ///
        /// Alternately, it can return a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>
        /// instead.
        ///
        /// The <see cref="Cmdlet"/> or <see cref="Provider.CmdletProvider"/>
        /// should hold on to a reference to the object which it returns from
        /// this method, since the argument values for the dynamic parameters
        /// specified by that object will be set in that object.
        ///
        /// This method will be called after all formal (command-line)
        /// parameters are set, but before <see cref="Cmdlet.BeginProcessing"/>
        /// is called and before any incoming pipeline objects are read.
        /// Therefore, parameters which allow input from the pipeline
        /// may not be set at the time this method is called,
        /// even if the parameters are mandatory.
        /// </returns>
        object? GetDynamicParameters();
    }
#nullable restore

    
    public readonly struct SwitchParameter
    {
        private readonly bool _isPresent;
        
        /// <value>True if the parameter was specified, false otherwise</value>
        public bool IsPresent
        {
            get { return _isPresent; }
        }
        
        /// <param name="switchParameter">The SwitchParameter object to convert to bool.</param>
        /// <returns>The corresponding boolean value.</returns>
        public static implicit operator bool(SwitchParameter switchParameter)
        {
            return switchParameter.IsPresent;
        }

        
        /// <param name="value">The bool to convert to SwitchParameter.</param>
        /// <returns>The corresponding boolean value.</returns>
        public static implicit operator SwitchParameter(bool value)
        {
            return new SwitchParameter(value);
        }

        
        /// <returns>The boolean equivalent of the SwitchParameter.</returns>
        public bool ToBool()
        {
            return _isPresent;
        }

        
        /// <param name="isPresent">
        /// If true, it indicates that the switch is present, false otherwise.
        /// </param>
        public SwitchParameter(bool isPresent)
        {
            _isPresent = isPresent;
        }

        
        /// <value>An instance of a switch parameter that will convert to true in a boolean context</value>
        public static SwitchParameter Present
        {
            get { return new SwitchParameter(true); }
        }

        
        /// <param name="obj">An object to compare against.</param>
        /// <returns>True if the objects are the same value.</returns>
        public override bool Equals(object obj)
        {
            if (obj is bool)
            {
                return _isPresent == (bool)obj;
            }
            else if (obj is SwitchParameter)
            {
                return _isPresent == ((SwitchParameter)obj).IsPresent;
            }
            else
            {
                return false;
            }
        }
        
        /// <returns>The hash code for this cobject.</returns>
        public override int GetHashCode()
        {
            return _isPresent.GetHashCode();
        }

        
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are the same.</returns>
        public static bool operator ==(SwitchParameter first, SwitchParameter second)
        {
            return first.Equals(second);
        }
        
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are different.</returns>
        public static bool operator !=(SwitchParameter first, SwitchParameter second)
        {
            return !first.Equals(second);
        }
        
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are the same.</returns>
        public static bool operator ==(SwitchParameter first, bool second)
        {
            return first.Equals(second);
        }
        
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are different.</returns>
        public static bool operator !=(SwitchParameter first, bool second)
        {
            return !first.Equals(second);
        }
        
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are the same.</returns>
        public static bool operator ==(bool first, SwitchParameter second)
        {
            return first.Equals(second);
        }
        
        /// <param name="first">First object to compare.</param>
        /// <param name="second">Second object to compare.</param>
        /// <returns>True if they are different.</returns>
        public static bool operator !=(bool first, SwitchParameter second)
        {
            return !first.Equals(second);
        }

        
        /// <returns>The string for this object.</returns>
        public override string ToString()
        {
            return _isPresent.ToString();
        }
    }

    
    public class CommandInvocationIntrinsics
    {
        private readonly ExecutionContext _context;
        private readonly PSCmdlet _cmdlet;
        private readonly MshCommandRuntime _commandRuntime;

        internal CommandInvocationIntrinsics(ExecutionContext context, PSCmdlet cmdlet)
        {
            _context = context;
            if (cmdlet != null)
            {
                _cmdlet = cmdlet;
                _commandRuntime = cmdlet.CommandRuntime as MshCommandRuntime;
            }
        }

        internal CommandInvocationIntrinsics(ExecutionContext context)
            : this(context, null)
        {
        }

        
        public bool HasErrors
        {
            get
            {
                return _commandRuntime.PipelineProcessor.ExecutionFailed;
            }

            set
            {
                _commandRuntime.PipelineProcessor.ExecutionFailed = value;
            }
        }

        
        /// <param name="source">The string to expand.
        /// </param>
        /// <returns>The expanded string.</returns>
        /// <exception cref="ParseException">
        /// Thrown if a parse exception occurred during subexpression substitution.
        /// </exception>
        public string ExpandString(string source)
        {
            _cmdlet?.ThrowIfStopping();
            return _context.Engine.Expand(source);
        }

        
        /// <param name="commandName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public CommandInfo GetCommand(string commandName, CommandTypes type)
        {
            return GetCommand(commandName, type, null);
        }

        
        /// <param name="commandName">The command name to search for.</param>
        /// <param name="type">The command type to search for.</param>
        /// <param name="arguments">The command arguments used to resolve dynamic parameters.</param>
        /// <returns>A CommandInfo result that represents the resolved command.</returns>
        public CommandInfo GetCommand(string commandName, CommandTypes type, object[] arguments)
        {
            CommandInfo result = null;

            try
            {
                CommandOrigin commandOrigin = CommandOrigin.Runspace;
                if (_cmdlet != null)
                {
                    commandOrigin = _cmdlet.CommandOrigin;
                }
                else if (_context != null)
                {
                    commandOrigin = _context.EngineSessionState.CurrentScope.ScopeOrigin;
                }

                result = CommandDiscovery.LookupCommandInfo(commandName, type, SearchResolutionOptions.None, commandOrigin, _context);

                if ((result != null) && (arguments != null) && (arguments.Length > 0))
                {
                    // We've been asked to retrieve dynamic parameters
                    if (result.ImplementsDynamicParameters)
                    {
                        result = result.CreateGetCommandCopy(arguments);
                    }
                }
            }
            catch (CommandNotFoundException) { }

            return result;
        }

        
        public System.EventHandler<CommandLookupEventArgs> CommandNotFoundAction { get; set; }

        
        public System.EventHandler<CommandLookupEventArgs> PreCommandLookupAction { get; set; }

        
        public System.EventHandler<CommandLookupEventArgs> PostCommandLookupAction { get; set; }

        
        public System.EventHandler<LocationChangedEventArgs> LocationChangedAction { get; set; }

        
        /// <param name="commandName">The name of the cmdlet to look for.</param>
        /// <returns>The cmdletInfo object if found, null otherwise.</returns>
        public CmdletInfo GetCmdlet(string commandName)
        {
            return GetCmdlet(commandName, _context);
        }

        
        /// <param name="commandName">The name of the cmdlet to look for.</param>
        /// <param name="context">The execution context instance to use for lookup.</param>
        /// <returns>The cmdletInfo object if found, null otherwise.</returns>
        internal static CmdletInfo GetCmdlet(string commandName, ExecutionContext context)
        {
            CmdletInfo current = null;

            CommandSearcher searcher = new CommandSearcher(
                    commandName,
                    SearchResolutionOptions.None,
                    CommandTypes.Cmdlet,
                    context);
            while (true)
            {
                try
                {
                    if (!searcher.MoveNext())
                    {
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (PathTooLongException)
                {
                    continue;
                }
                catch (FileLoadException)
                {
                    continue;
                }
                catch (MetadataException)
                {
                    continue;
                }
                catch (FormatException)
                {
                    continue;
                }

                current = ((IEnumerator)searcher).Current as CmdletInfo;
            }

            return current;
        }

        
        /// <param name="cmdletTypeName">The type name of the class implementing this cmdlet.</param>
        /// <returns>CmdletInfo for the cmdlet if found, null otherwise.</returns>
        public CmdletInfo GetCmdletByTypeName(string cmdletTypeName)
        {
            if (string.IsNullOrEmpty(cmdletTypeName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdletTypeName));
            }

            Exception e = null;
            Type cmdletType = TypeResolver.ResolveType(cmdletTypeName, out e);
            if (e != null)
            {
                throw e;
            }

            if (cmdletType == null)
            {
                return null;
            }

            CmdletAttribute ca = null;
            foreach (var attr in cmdletType.GetCustomAttributes(true))
            {
                ca = attr as CmdletAttribute;
                if (ca != null)
                    break;
            }

            if (ca == null)
            {
                throw PSTraceSource.NewNotSupportedException();
            }

            string noun = ca.NounName;
            string verb = ca.VerbName;
            string cmdletName = verb + "-" + noun;

            return new CmdletInfo(cmdletName, cmdletType, null, null, _context);
        }

        
        /// <returns></returns>
        public List<CmdletInfo> GetCmdlets()
        {
            return GetCmdlets("*");
        }

        
        /// <returns>A list of CmdletInfo objects...</returns>
        public List<CmdletInfo> GetCmdlets(string pattern)
        {
            if (pattern == null)
                throw PSTraceSource.NewArgumentNullException(nameof(pattern));

            List<CmdletInfo> cmdlets = new List<CmdletInfo>();

            CmdletInfo current = null;

            CommandSearcher searcher = new CommandSearcher(
                    pattern,
                    SearchResolutionOptions.CommandNameIsPattern,
                    CommandTypes.Cmdlet,
                    _context);
            while (true)
            {
                try
                {
                    if (!searcher.MoveNext())
                    {
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (PathTooLongException)
                {
                    continue;
                }
                catch (FileLoadException)
                {
                    continue;
                }
                catch (MetadataException)
                {
                    continue;
                }
                catch (FormatException)
                {
                    continue;
                }

                current = ((IEnumerator)searcher).Current as CmdletInfo;
                if (current != null)
                    cmdlets.Add(current);
            }

            return cmdlets;
        }

        
        /// <param name="name">The name of the command to use.</param>
        /// <param name="nameIsPattern">If true treat the name as a pattern to search for.</param>
        /// <param name="returnFullName">If true, return the full path to scripts and applications.</param>
        /// <returns>A list of command names...</returns>
        public List<string> GetCommandName(string name, bool nameIsPattern, bool returnFullName)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            List<string> commands = new List<string>();

            foreach (CommandInfo current in this.GetCommands(name, CommandTypes.All, nameIsPattern))
            {
                if (current.CommandType == CommandTypes.Application)
                {
                    string cmdExtension = System.IO.Path.GetExtension(current.Name);
                    if (!string.IsNullOrEmpty(cmdExtension))
                    {
                        // Only add the application in PATHEXT...
                        foreach (string extension in CommandDiscovery.PathExtensions)
                        {
                            if (extension.Equals(cmdExtension, StringComparison.OrdinalIgnoreCase))
                            {
                                if (returnFullName)
                                {
                                    commands.Add(current.Definition);
                                }
                                else
                                {
                                    commands.Add(current.Name);
                                }
                            }
                        }
                    }
                }
                else if (current.CommandType == CommandTypes.ExternalScript)
                {
                    if (returnFullName)
                    {
                        commands.Add(current.Definition);
                    }
                    else
                    {
                        commands.Add(current.Name);
                    }
                }
                else
                {
                    commands.Add(current.Name);
                }
            }

            return commands;
        }

        
        /// <param name="name">The name of the command to use.</param>
        /// <param name="commandTypes">Type of commands to support.</param>
        /// <param name="nameIsPattern">If true treat the name as a pattern to search for.</param>
        /// <returns>Collection of command names...</returns>
        public IEnumerable<CommandInfo> GetCommands(string name, CommandTypes commandTypes, bool nameIsPattern)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            SearchResolutionOptions options = nameIsPattern ?
                (SearchResolutionOptions.CommandNameIsPattern | SearchResolutionOptions.ResolveFunctionPatterns | SearchResolutionOptions.ResolveAliasPatterns)
                : SearchResolutionOptions.None;

            return GetCommands(name, commandTypes, options);
        }

        internal IEnumerable<CommandInfo> GetCommands(string name, CommandTypes commandTypes, SearchResolutionOptions options, CommandOrigin? commandOrigin = null)
        {
            CommandSearcher searcher = new CommandSearcher(
                name,
                options,
                commandTypes,
                _context);

            if (commandOrigin != null)
            {
                searcher.CommandOrigin = commandOrigin.Value;
            }

            while (true)
            {
                try
                {
                    if (!searcher.MoveNext())
                    {
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
                catch (PathTooLongException)
                {
                    continue;
                }
                catch (FileLoadException)
                {
                    continue;
                }
                catch (MetadataException)
                {
                    continue;
                }
                catch (FormatException)
                {
                    continue;
                }

                CommandInfo commandInfo = ((IEnumerator)searcher).Current as CommandInfo;
                if (commandInfo != null)
                {
                    yield return commandInfo;
                }
            }
        }

        
        /// <param name="script">The script text to evaluate.</param>
        /// <returns>A collection of PSObjects generated by the script. Never null, but may be empty.</returns>
        /// <exception cref="ParseException">Thrown if there was a parsing error in the script.</exception>
        /// <exception cref="RuntimeException">Represents a script-level exception.</exception>
        /// <exception cref="FlowControlException"></exception>
        public Collection<PSObject> InvokeScript(string script)
        {
            return InvokeScript(script, useNewScope: true, PipelineResultTypes.None, input: null);
        }

        
        /// <param name="script">The script text to evaluate.</param>
        /// <param name="args">The arguments to the script, available as $args.</param>
        /// <returns>A collection of PSObjects generated by the script. Never null, but may be empty.</returns>
        /// <exception cref="ParseException">Thrown if there was a parsing error in the script.</exception>
        /// <exception cref="RuntimeException">Represents a script-level exception.</exception>
        /// <exception cref="FlowControlException"></exception>
        public Collection<PSObject> InvokeScript(string script, params object[] args)
        {
            return InvokeScript(script, useNewScope: true, PipelineResultTypes.None, input: null, args);
        }

        
        /// <param name="sessionState">The session state in which to execute the scriptblock.</param>
        /// <param name="scriptBlock">The scriptblock to execute.</param>
        /// <param name="args">The arguments to the scriptblock, available as $args.</param>
        /// <returns>A collection of the PSObjects emitted by the executing scriptblock. Never null, but may be empty.</returns>
        public Collection<PSObject> InvokeScript(
            SessionState sessionState,
            ScriptBlock scriptBlock,
            params object[] args)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(scriptBlock));
            }

            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            SessionStateInternal _oldSessionState = _context.EngineSessionState;
            try
            {
                _context.EngineSessionState = sessionState.Internal;
                return InvokeScript(
                    sb: scriptBlock,
                    useNewScope: false,
                    writeToPipeline: PipelineResultTypes.None,
                    input: null,
                    args: args);
            }
            finally
            {
                _context.EngineSessionState = _oldSessionState;
            }
        }

        
        /// <param name="useLocalScope">If true, executes the scriptblock in a new child scope, otherwise the scriptblock is dot-sourced into the calling scope.</param>
        /// <param name="scriptBlock">The scriptblock to execute.</param>
        /// <param name="input">Optional input to the command.</param>
        /// <param name="args">Arguments to pass to the scriptblock.</param>
        /// <returns>
        /// A collection of the PSObjects generated by executing the script. Never null, but may be empty.
        /// </returns>
        public Collection<PSObject> InvokeScript(
            bool useLocalScope,
            ScriptBlock scriptBlock,
            IList input,
            params object[] args)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(scriptBlock));
            }

            // Force the current runspace onto the callers thread - this is needed
            // if this API is going to be callable through the SessionStateProxy on the runspace.
            var old = System.Management.Automation.Runspaces.Runspace.DefaultRunspace;
            System.Management.Automation.Runspaces.Runspace.DefaultRunspace = _context.CurrentRunspace;
            try
            {
                return InvokeScript(scriptBlock, useLocalScope, PipelineResultTypes.None, input, args);
            }
            finally
            {
                System.Management.Automation.Runspaces.Runspace.DefaultRunspace = old;
            }
        }

        
        /// <param name="script">The script to evaluate.</param>
        /// <param name="useNewScope">If true, evaluate the script in its own scope.
        /// If false, the script will be evaluated in the current scope i.e. it will be dot-sourced.</param>
        /// <param name="writeToPipeline">If set to Output, all output will be streamed
        /// to the output pipe of the calling cmdlet. If set to None, the result will be returned
        /// to the caller as a collection of PSObjects. No other flags are supported at this time and
        /// will result in an exception if used.</param>
        /// <param name="input">The list of objects to use as input to the script.</param>
        /// <param name="args">The array of arguments to the command, available as $args.</param>
        /// <returns>A collection of PSObjects generated by the script. This will be
        /// empty if output was redirected. Never null.</returns>
        /// <exception cref="ParseException">Thrown if there was a parsing error in the script.</exception>
        /// <exception cref="RuntimeException">Represents a script-level exception.</exception>
        /// <exception cref="NotImplementedException">Thrown if any redirect other than output is attempted.</exception>
        /// <exception cref="FlowControlException"></exception>
        public Collection<PSObject> InvokeScript(
            string script,
            bool useNewScope,
            PipelineResultTypes writeToPipeline,
            IList input,
            params object[] args)
        {
            ArgumentNullException.ThrowIfNull(script);

            // Compile the script text into an executable script block.
            ScriptBlock sb = ScriptBlock.Create(_context, script);

            return InvokeScript(sb, useNewScope, writeToPipeline, input, args);
        }

        private Collection<PSObject> InvokeScript(
            ScriptBlock sb,
            bool useNewScope,
            PipelineResultTypes writeToPipeline,
            IList input,
            params object[] args)
        {
            _cmdlet?.ThrowIfStopping();

            Cmdlet cmdletToUse = null;
            ScriptBlock.ErrorHandlingBehavior errorHandlingBehavior = ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe;

            // Check if they want output
            if ((writeToPipeline & PipelineResultTypes.Output) == PipelineResultTypes.Output)
            {
                cmdletToUse = _cmdlet;
                writeToPipeline &= (~PipelineResultTypes.Output);
            }

            // Check if they want error
            if ((writeToPipeline & PipelineResultTypes.Error) == PipelineResultTypes.Error)
            {
                errorHandlingBehavior = ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe;
                writeToPipeline &= (~PipelineResultTypes.Error);
            }

            if (writeToPipeline != PipelineResultTypes.None)
            {
                // The only output types are Output and Error.
                throw PSTraceSource.NewNotImplementedException();
            }

            // If the cmdletToUse is not null, then the result of the evaluation will be
            // streamed out the output pipe of the cmdlet.
            object rawResult;
            if (cmdletToUse != null)
            {
                sb.InvokeUsingCmdlet(
                    contextCmdlet: cmdletToUse,
                    useLocalScope: useNewScope,
                    errorHandlingBehavior: errorHandlingBehavior,
                    dollarUnder: AutomationNull.Value,
                    input: input,
                    scriptThis: AutomationNull.Value,
                    args: args);
                rawResult = AutomationNull.Value;
            }
            else
            {
                rawResult = sb.DoInvokeReturnAsIs(
                    useLocalScope: useNewScope,
                    errorHandlingBehavior: errorHandlingBehavior,
                    dollarUnder: AutomationNull.Value,
                    input: input,
                    scriptThis: AutomationNull.Value,
                    args: args);
            }

            if (rawResult == AutomationNull.Value)
            {
                return new Collection<PSObject>();
            }

            // If the result is already a collection of PSObjects, just return it...
            Collection<PSObject> result = rawResult as Collection<PSObject>;
            if (result != null)
                return result;

            result = new Collection<PSObject>();

            IEnumerator list = null;
            list = LanguagePrimitives.GetEnumerator(rawResult);

            if (list != null)
            {
                while (list.MoveNext())
                {
                    object val = list.Current;

                    result.Add(LanguagePrimitives.AsPSObjectOrNull(val));
                }
            }
            else
            {
                result.Add(LanguagePrimitives.AsPSObjectOrNull(rawResult));
            }

            return result;
        }

        
        /// <param name="scriptText">The source text to compile.</param>
        /// <returns>The compiled script block.</returns>
        /// <exception cref="ParseException"></exception>
        public ScriptBlock NewScriptBlock(string scriptText)
        {
            _commandRuntime?.ThrowIfStopping();

            ScriptBlock result = ScriptBlock.Create(_context, scriptText);
            return result;
        }
    }
    #endregion Auxiliary

    
    /// <remarks>
    /// Do not attempt to create instances of
    /// <see cref="System.Management.Automation.Cmdlet"/>
    /// or its subclasses.
    /// Instead, derive your own subclasses and mark them with
    /// <see cref="System.Management.Automation.CmdletAttribute"/>,
    /// and when your assembly is included in a shell, the Engine will
    /// take care of instantiating your subclass.
    /// </remarks>
    public abstract partial class PSCmdlet : Cmdlet
    {
        #region private_members

        internal bool HasDynamicParameters
        {
            get { return this is IDynamicParameters; }
        }

        #endregion private_members

        #region public members
        
        /// <value>the parameter set name</value>
        public string ParameterSetName
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return _ParameterSetName;
                }
            }
        }

        
        /// <value></value>
        public new InvocationInfo MyInvocation
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return base.MyInvocation;
                }
            }
        }

        
        public PagingParameters PagingParameters
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    if (!this.CommandInfo.CommandMetadata.SupportsPaging)
                    {
                        return null;
                    }

                    if (_pagingParameters == null)
                    {
                        MshCommandRuntime mshCommandRuntime = this.CommandRuntime as MshCommandRuntime;
                        if (mshCommandRuntime != null)
                        {
                            _pagingParameters = mshCommandRuntime.PagingParameters ?? new PagingParameters(mshCommandRuntime);
                        }
                    }

                    return _pagingParameters;
                }
            }
        }

        private PagingParameters _pagingParameters;

        #region InvokeCommand
        private CommandInvocationIntrinsics _invokeCommand;

        
        /// <value>Returns an object exposing the utility routines.</value>
        public CommandInvocationIntrinsics InvokeCommand
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return _invokeCommand ??= new CommandInvocationIntrinsics(Context, this);
                }
            }
        }
        #endregion InvokeCommand

        #endregion public members

    }
}
