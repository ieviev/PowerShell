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

    
#nullable enable
    public interface IDynamicParameters
    {
        
        object? GetDynamicParameters();
    }
#nullable restore

    
    public readonly struct SwitchParameter
    {
        private readonly bool _isPresent;
        
        public bool IsPresent
        {
            get { return _isPresent; }
        }
        
        public static implicit operator bool(SwitchParameter switchParameter)
        {
            return switchParameter.IsPresent;
        }

        
        public static implicit operator SwitchParameter(bool value)
        {
            return new SwitchParameter(value);
        }

        
        public bool ToBool()
        {
            return _isPresent;
        }

        
        public SwitchParameter(bool isPresent)
        {
            _isPresent = isPresent;
        }

        
        public static SwitchParameter Present
        {
            get { return new SwitchParameter(true); }
        }

        
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
        
        public override int GetHashCode()
        {
            return _isPresent.GetHashCode();
        }

        
        public static bool operator ==(SwitchParameter first, SwitchParameter second)
        {
            return first.Equals(second);
        }
        
        public static bool operator !=(SwitchParameter first, SwitchParameter second)
        {
            return !first.Equals(second);
        }
        
        public static bool operator ==(SwitchParameter first, bool second)
        {
            return first.Equals(second);
        }
        
        public static bool operator !=(SwitchParameter first, bool second)
        {
            return !first.Equals(second);
        }
        
        public static bool operator ==(bool first, SwitchParameter second)
        {
            return first.Equals(second);
        }
        
        public static bool operator !=(bool first, SwitchParameter second)
        {
            return !first.Equals(second);
        }

        
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

        
        public string ExpandString(string source)
        {
            _cmdlet?.ThrowIfStopping();
            return _context.Engine.Expand(source);
        }

        
        public CommandInfo GetCommand(string commandName, CommandTypes type)
        {
            return GetCommand(commandName, type, null);
        }

        
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

        
        public CmdletInfo GetCmdlet(string commandName)
        {
            return GetCmdlet(commandName, _context);
        }

        
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

        
        public List<CmdletInfo> GetCmdlets()
        {
            return GetCmdlets("*");
        }

        
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

        
        public Collection<PSObject> InvokeScript(string script)
        {
            return InvokeScript(script, useNewScope: true, PipelineResultTypes.None, input: null);
        }

        
        public Collection<PSObject> InvokeScript(string script, params object[] args)
        {
            return InvokeScript(script, useNewScope: true, PipelineResultTypes.None, input: null, args);
        }

        
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

        
        public ScriptBlock NewScriptBlock(string scriptText)
        {
            _commandRuntime?.ThrowIfStopping();

            ScriptBlock result = ScriptBlock.Create(_context, scriptText);
            return result;
        }
    }
    #endregion Auxiliary

    
    public abstract partial class PSCmdlet : Cmdlet
    {
        #region private_members

        internal bool HasDynamicParameters
        {
            get { return this is IDynamicParameters; }
        }

        #endregion private_members

        #region public members
        
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
