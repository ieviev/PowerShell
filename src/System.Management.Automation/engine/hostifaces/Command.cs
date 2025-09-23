// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;

using Microsoft.Management.Infrastructure;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Runspaces
{
    
    public sealed class Command
    {
        #region constructors

        
        public Command(string command)
            : this(command, false, null)
        {
        }

        
        public Command(string command, bool isScript)
            : this(command, isScript, null)
        {
        }

        
        public Command(string command, bool isScript, bool useLocalScope)
        {
            IsEndOfStatement = false;
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            CommandText = command;
            IsScript = isScript;
            _useLocalScope = useLocalScope;
        }

        internal Command(string command, bool isScript, bool? useLocalScope)
        {
            IsEndOfStatement = false;
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            }

            CommandText = command;
            IsScript = isScript;
            _useLocalScope = useLocalScope;
        }

        internal Command(string command, bool isScript, bool? useLocalScope, bool mergeUnclaimedPreviousErrorResults)
            : this(command, isScript, useLocalScope)
        {
            if (mergeUnclaimedPreviousErrorResults)
            {
                _mergeUnclaimedPreviousCommandResults = PipelineResultTypes.Error | PipelineResultTypes.Output;
            }
        }

        internal Command(CommandInfo commandInfo)
            : this(commandInfo, false)
        {
        }

        internal Command(CommandInfo commandInfo, bool isScript)
        {
            IsEndOfStatement = false;
            CommandInfo = commandInfo;
            CommandText = CommandInfo.Name;
            IsScript = isScript;
        }

        
        internal Command(Command command)
        {
            IsScript = command.IsScript;
            _useLocalScope = command._useLocalScope;
            CommandText = command.CommandText;
            MergeInstructions = command.MergeInstructions;
            MergeMyResult = command.MergeMyResult;
            MergeToResult = command.MergeToResult;
            _mergeUnclaimedPreviousCommandResults = command._mergeUnclaimedPreviousCommandResults;
            IsEndOfStatement = command.IsEndOfStatement;
            CommandInfo = command.CommandInfo;

            foreach (CommandParameter param in command.Parameters)
            {
                Parameters.Add(new CommandParameter(param.Name, param.Value));
            }
        }

        #endregion constructors

        #region Properties

        
        public CommandParameterCollection Parameters { get; } = new CommandParameterCollection();

        
        public string CommandText { get; } = string.Empty;

        
        internal CommandInfo CommandInfo { get; }

        
        public bool IsScript { get; }

        
        public bool UseLocalScope
        {
            get { return _useLocalScope ?? false; }
        }

        
        public CommandOrigin CommandOrigin { get; set; } = CommandOrigin.Runspace;

        
        internal bool? UseLocalScopeNullable
        {
            get { return _useLocalScope; }
        }

        
        internal object DollarUnderbar { get; set; } = AutomationNull.Value;

        
        public bool IsEndOfStatement { get; internal set; }

        #endregion Properties

        #region Methods

        
        internal Command Clone()
        {
            return new Command(this);
        }

        
        public override string ToString()
        {
            return CommandText;
        }

        #endregion Methods

        #region Merge

        private PipelineResultTypes _mergeUnclaimedPreviousCommandResults =
            PipelineResultTypes.None;
        
        public PipelineResultTypes MergeUnclaimedPreviousCommandResults
        {
            get
            {
                return _mergeUnclaimedPreviousCommandResults;
            }

            set
            {
                if (value == PipelineResultTypes.None)
                {
                    _mergeUnclaimedPreviousCommandResults = value;
                    return;
                }

                if (value != (PipelineResultTypes.Error | PipelineResultTypes.Output))
                {
                    throw PSTraceSource.NewNotSupportedException();
                }

                _mergeUnclaimedPreviousCommandResults = value;
            }
        }

        //
        // These properties are kept for backwards compatibility for V2
        // over the wire, which allows merging only for Error stream.
        //

        internal PipelineResultTypes MergeMyResult { get; private set; } = PipelineResultTypes.None;

        internal PipelineResultTypes MergeToResult { get; private set; } = PipelineResultTypes.None;

        //
        // For V3 we allow merging from all streams except Output.
        //
        internal enum MergeType
        {
            Error = 0,
            Warning = 1,
            Verbose = 2,
            Debug = 3,
            Information = 4
        }

        internal const int MaxMergeType = (int)(MergeType.Information + 1);

        
        internal PipelineResultTypes[] MergeInstructions { get; set; } = new PipelineResultTypes[MaxMergeType];

        
        public void MergeMyResults(PipelineResultTypes myResult, PipelineResultTypes toResult)
        {
            if (myResult == PipelineResultTypes.None && toResult == PipelineResultTypes.None)
            {
                // For V2 backwards compatibility.
                MergeMyResult = myResult;
                MergeToResult = toResult;

                for (int i = 0; i < MaxMergeType; ++i)
                {
                    MergeInstructions[i] = PipelineResultTypes.None;
                }

                return;
            }

            // Validate parameters.
            if (myResult == PipelineResultTypes.None || myResult == PipelineResultTypes.Output)
            {
                throw PSTraceSource.NewArgumentException(nameof(myResult), RunspaceStrings.InvalidMyResultError);
            }

            if (myResult == PipelineResultTypes.Error && toResult != PipelineResultTypes.Output)
            {
                throw PSTraceSource.NewArgumentException(nameof(toResult), RunspaceStrings.InvalidValueToResultError);
            }

            if (toResult != PipelineResultTypes.Output && toResult != PipelineResultTypes.Null)
            {
                throw PSTraceSource.NewArgumentException(nameof(toResult), RunspaceStrings.InvalidValueToResult);
            }

            // For V2 backwards compatibility.
            if (myResult == PipelineResultTypes.Error)
            {
                MergeMyResult = myResult;
                MergeToResult = toResult;
            }

            // Set internal merge instructions.
            if (myResult == PipelineResultTypes.Error || myResult == PipelineResultTypes.All)
            {
                MergeInstructions[(int)MergeType.Error] = toResult;
            }

            if (myResult == PipelineResultTypes.Warning || myResult == PipelineResultTypes.All)
            {
                MergeInstructions[(int)MergeType.Warning] = toResult;
            }

            if (myResult == PipelineResultTypes.Verbose || myResult == PipelineResultTypes.All)
            {
                MergeInstructions[(int)MergeType.Verbose] = toResult;
            }

            if (myResult == PipelineResultTypes.Debug || myResult == PipelineResultTypes.All)
            {
                MergeInstructions[(int)MergeType.Debug] = toResult;
            }

            if (myResult == PipelineResultTypes.Information || myResult == PipelineResultTypes.All)
            {
                MergeInstructions[(int)MergeType.Information] = toResult;
            }
        }

        
        private
        void
        SetMergeSettingsOnCommandProcessor(CommandProcessorBase commandProcessor)
        {
            Dbg.Assert(commandProcessor != null, "caller should validate the parameter");

            MshCommandRuntime mcr = commandProcessor.Command.commandRuntime as MshCommandRuntime;

            if (_mergeUnclaimedPreviousCommandResults != PipelineResultTypes.None)
            {
                // Currently only merging previous unclaimed error and output is supported.
                if (mcr != null)
                {
                    mcr.MergeUnclaimedPreviousErrorResults = true;
                }
            }

            // Error merge.
            if (MergeInstructions[(int)MergeType.Error] == PipelineResultTypes.Output)
            {
                // Currently only merging error with output is supported.
                mcr.ErrorMergeTo = MshCommandRuntime.MergeDataStream.Output;
            }

            // Warning merge.
            PipelineResultTypes toType = MergeInstructions[(int)MergeType.Warning];
            if (toType != PipelineResultTypes.None)
            {
                mcr.WarningOutputPipe = GetRedirectionPipe(toType, mcr);
            }

            // Verbose merge.
            toType = MergeInstructions[(int)MergeType.Verbose];
            if (toType != PipelineResultTypes.None)
            {
                mcr.VerboseOutputPipe = GetRedirectionPipe(toType, mcr);
            }

            // Debug merge.
            toType = MergeInstructions[(int)MergeType.Debug];
            if (toType != PipelineResultTypes.None)
            {
                mcr.DebugOutputPipe = GetRedirectionPipe(toType, mcr);
            }

            // Information merge.
            toType = MergeInstructions[(int)MergeType.Information];
            if (toType != PipelineResultTypes.None)
            {
                mcr.InformationOutputPipe = GetRedirectionPipe(toType, mcr);
            }
        }

        private static Pipe GetRedirectionPipe(
            PipelineResultTypes toType,
            MshCommandRuntime mcr)
        {
            if (toType == PipelineResultTypes.Output)
            {
                return mcr.OutputPipe;
            }

            Pipe pipe = new Pipe();
            pipe.NullPipe = true;
            return pipe;
        }

        #endregion Merge

        
        internal
        CommandProcessorBase
        CreateCommandProcessor
        (
            ExecutionContext executionContext,
            bool addToHistory,
            CommandOrigin origin
        )
        {
            Dbg.Assert(executionContext != null, "Caller should verify the parameters");

            CommandProcessorBase commandProcessorBase;

            if (IsScript)
            {
                if ((executionContext.LanguageMode == PSLanguageMode.NoLanguage) &&
                    (origin == Automation.CommandOrigin.Runspace))
                {
                    throw InterpreterError.NewInterpreterException(CommandText, typeof(ParseException),
                        null, "ScriptsNotAllowed", ParserStrings.ScriptsNotAllowed);
                }

                ScriptBlock scriptBlock = executionContext.Engine.ParseScriptBlock(CommandText, addToHistory);
                if (origin == Automation.CommandOrigin.Internal)
                {
                    scriptBlock.LanguageMode = PSLanguageMode.FullLanguage;
                }

                // If running in restricted language mode, verify that the parse tree represents on legitimate
                // constructions...
                switch (scriptBlock.LanguageMode)
                {
                    case PSLanguageMode.RestrictedLanguage:
                        scriptBlock.CheckRestrictedLanguage(null, null, false);
                        break;
                    case PSLanguageMode.FullLanguage:
                        // Interactive script commands are permitted in this mode.
                        break;
                    case PSLanguageMode.ConstrainedLanguage:
                        // Constrained Language is checked at runtime.
                        break;
                    default:
                        // This should never happen...
                        Diagnostics.Assert(false, "Invalid language mode was set when building a ScriptCommandProcessor");
                        throw new InvalidOperationException("Invalid language mode was set when building a ScriptCommandProcessor");
                }

                if (scriptBlock.UsesCmdletBinding)
                {
                    FunctionInfo functionInfo = new FunctionInfo(string.Empty, scriptBlock, executionContext);
                    commandProcessorBase = new CommandProcessor(functionInfo, executionContext,
                                                                _useLocalScope ?? false, fromScriptFile: false, sessionState: executionContext.EngineSessionState);
                }
                else
                {
                    commandProcessorBase = new DlrScriptCommandProcessor(scriptBlock,
                                                                         executionContext, _useLocalScope ?? false,
                                                                         origin,
                                                                         executionContext.EngineSessionState,
                                                                         DollarUnderbar);
                }
            }
            else
            {
                // RestrictedLanguage / NoLanguage do not support dot-sourcing when CommandOrigin is Runspace
                if ((_useLocalScope.HasValue) && (!_useLocalScope.Value))
                {
                    switch (executionContext.LanguageMode)
                    {
                        case PSLanguageMode.RestrictedLanguage:
                        case PSLanguageMode.NoLanguage:
                            string message = StringUtil.Format(RunspaceStrings.UseLocalScopeNotAllowed,
                                "UseLocalScope",
                                nameof(PSLanguageMode.RestrictedLanguage),
                                nameof(PSLanguageMode.NoLanguage));
                            throw new RuntimeException(message);
                        case PSLanguageMode.FullLanguage:
                            // Interactive script commands are permitted in this mode...
                            break;
                    }
                }

                commandProcessorBase = executionContext.CommandDiscovery.LookupCommandProcessor(CommandText, origin, _useLocalScope);
            }

            CommandParameterCollection parameters = Parameters;

            if (parameters != null)
            {
                bool isNativeCommand = commandProcessorBase is NativeCommandProcessor;
                foreach (CommandParameter publicParameter in parameters)
                {
                    CommandParameterInternal internalParameter = CommandParameter.ToCommandParameterInternal(publicParameter, isNativeCommand);
                    commandProcessorBase.AddParameter(internalParameter);
                }
            }

            string helpTarget;
            HelpCategory helpCategory;
            if (commandProcessorBase.IsHelpRequested(out helpTarget, out helpCategory))
            {
                commandProcessorBase = CommandProcessorBase.CreateGetHelpCommandProcessor(
                    executionContext,
                    helpTarget,
                    helpCategory);
            }

            // Set the merge settings
            SetMergeSettingsOnCommandProcessor(commandProcessorBase);

            return commandProcessorBase;
        }

        #region Private fields

        
        private readonly bool? _useLocalScope;

        #endregion Private fields

        #region Serialization / deserialization for remoting

        
        internal static Command FromPSObjectForRemoting(PSObject commandAsPSObject)
        {
            if (commandAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandAsPSObject));
            }

            string commandText = RemotingDecoder.GetPropertyValue<string>(commandAsPSObject, RemoteDataNameStrings.CommandText);
            bool isScript = RemotingDecoder.GetPropertyValue<bool>(commandAsPSObject, RemoteDataNameStrings.IsScript);
            bool? useLocalScopeNullable = RemotingDecoder.GetPropertyValue<bool?>(commandAsPSObject, RemoteDataNameStrings.UseLocalScopeNullable);
            Command command = new Command(commandText, isScript, useLocalScopeNullable);

            // For V2 backwards compatibility.
            PipelineResultTypes mergeMyResult = RemotingDecoder.GetPropertyValue<PipelineResultTypes>(commandAsPSObject, RemoteDataNameStrings.MergeMyResult);
            PipelineResultTypes mergeToResult = RemotingDecoder.GetPropertyValue<PipelineResultTypes>(commandAsPSObject, RemoteDataNameStrings.MergeToResult);
            command.MergeMyResults(mergeMyResult, mergeToResult);

            command.MergeUnclaimedPreviousCommandResults = RemotingDecoder.GetPropertyValue<PipelineResultTypes>(commandAsPSObject, RemoteDataNameStrings.MergeUnclaimedPreviousCommandResults);

            // V3 merge instructions will not be returned by V2 server and this is expected.
            if (commandAsPSObject.Properties[RemoteDataNameStrings.MergeError] != null)
            {
                command.MergeInstructions[(int)MergeType.Error] = RemotingDecoder.GetPropertyValue<PipelineResultTypes>(commandAsPSObject, RemoteDataNameStrings.MergeError);
            }

            if (commandAsPSObject.Properties[RemoteDataNameStrings.MergeWarning] != null)
            {
                command.MergeInstructions[(int)MergeType.Warning] = RemotingDecoder.GetPropertyValue<PipelineResultTypes>(commandAsPSObject, RemoteDataNameStrings.MergeWarning);
            }

            if (commandAsPSObject.Properties[RemoteDataNameStrings.MergeVerbose] != null)
            {
                command.MergeInstructions[(int)MergeType.Verbose] = RemotingDecoder.GetPropertyValue<PipelineResultTypes>(commandAsPSObject, RemoteDataNameStrings.MergeVerbose);
            }

            if (commandAsPSObject.Properties[RemoteDataNameStrings.MergeDebug] != null)
            {
                command.MergeInstructions[(int)MergeType.Debug] = RemotingDecoder.GetPropertyValue<PipelineResultTypes>(commandAsPSObject, RemoteDataNameStrings.MergeDebug);
            }

            if (commandAsPSObject.Properties[RemoteDataNameStrings.MergeInformation] != null)
            {
                command.MergeInstructions[(int)MergeType.Information] = RemotingDecoder.GetPropertyValue<PipelineResultTypes>(commandAsPSObject, RemoteDataNameStrings.MergeInformation);
            }

            foreach (PSObject parameterAsPSObject in RemotingDecoder.EnumerateListProperty<PSObject>(commandAsPSObject, RemoteDataNameStrings.Parameters))
            {
                command.Parameters.Add(CommandParameter.FromPSObjectForRemoting(parameterAsPSObject));
            }

            return command;
        }

        
        internal PSObject ToPSObjectForRemoting(Version psRPVersion)
        {
            PSObject commandAsPSObject = RemotingEncoder.CreateEmptyPSObject();

            commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.CommandText, this.CommandText));
            commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.IsScript, this.IsScript));
            commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.UseLocalScopeNullable, this.UseLocalScopeNullable));

            // For V2 backwards compatibility.
            commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeMyResult, this.MergeMyResult));
            commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeToResult, this.MergeToResult));

            commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeUnclaimedPreviousCommandResults, this.MergeUnclaimedPreviousCommandResults));

            if (psRPVersion != null &&
                psRPVersion >= RemotingConstants.ProtocolVersion_2_3)
            {
                // V5 merge instructions
                commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeError, MergeInstructions[(int)MergeType.Error]));
                commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeWarning, MergeInstructions[(int)MergeType.Warning]));
                commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeVerbose, MergeInstructions[(int)MergeType.Verbose]));
                commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeDebug, MergeInstructions[(int)MergeType.Debug]));
                commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeInformation, MergeInstructions[(int)MergeType.Information]));
            }
            else if (psRPVersion != null &&
                psRPVersion >= RemotingConstants.ProtocolVersion_2_2)
            {
                // V3 merge instructions.
                commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeError, MergeInstructions[(int)MergeType.Error]));
                commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeWarning, MergeInstructions[(int)MergeType.Warning]));
                commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeVerbose, MergeInstructions[(int)MergeType.Verbose]));
                commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.MergeDebug, MergeInstructions[(int)MergeType.Debug]));

                // If they've explicitly redirected the Information stream, generate an error. Don't
                // generate an error if they've done "*", as that makes any new stream a breaking change.
                if ((MergeInstructions[(int)MergeType.Information] == PipelineResultTypes.Output) &&
                    (MergeInstructions.Length != MaxMergeType))
                {
                    throw new RuntimeException(
                        StringUtil.Format(RunspaceStrings.InformationRedirectionNotSupported));
                }
            }
            else
            {
                // If they've explicitly redirected an unsupported stream, generate an error. Don't
                // generate an error if they've done "*", as that makes any new stream a breaking change.
                if (MergeInstructions.Length != MaxMergeType)
                {
                    if (MergeInstructions[(int)MergeType.Warning] == PipelineResultTypes.Output)
                    {
                        throw new RuntimeException(
                            StringUtil.Format(RunspaceStrings.WarningRedirectionNotSupported));
                    }

                    if (MergeInstructions[(int)MergeType.Verbose] == PipelineResultTypes.Output)
                    {
                        throw new RuntimeException(
                            StringUtil.Format(RunspaceStrings.VerboseRedirectionNotSupported));
                    }

                    if (MergeInstructions[(int)MergeType.Debug] == PipelineResultTypes.Output)
                    {
                        throw new RuntimeException(
                            StringUtil.Format(RunspaceStrings.DebugRedirectionNotSupported));
                    }

                    if (MergeInstructions[(int)MergeType.Information] == PipelineResultTypes.Output)
                    {
                        throw new RuntimeException(
                            StringUtil.Format(RunspaceStrings.InformationRedirectionNotSupported));
                    }
                }
            }

            List<PSObject> parametersAsListOfPSObjects = new List<PSObject>(this.Parameters.Count);
            foreach (CommandParameter parameter in this.Parameters)
            {
                parametersAsListOfPSObjects.Add(parameter.ToPSObjectForRemoting());
            }

            commandAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.Parameters, parametersAsListOfPSObjects));

            return commandAsPSObject;
        }

        #endregion

        #region Win Blue Extensions

#if !CORECLR // PSMI Not Supported On CSS
        internal CimInstance ToCimInstance()
        {
            CimInstance c = InternalMISerializer.CreateCimInstance("PS_Command");
            CimProperty commandTextProperty = InternalMISerializer.CreateCimProperty("CommandText",
                                                                                     this.CommandText,
                                                                                     Microsoft.Management.Infrastructure.CimType.String);
            c.CimInstanceProperties.Add(commandTextProperty);
            CimProperty isScriptProperty = InternalMISerializer.CreateCimProperty("IsScript",
                                                                                  this.IsScript,
                                                                                  Microsoft.Management.Infrastructure.CimType.Boolean);
            c.CimInstanceProperties.Add(isScriptProperty);

            if (this.Parameters != null && this.Parameters.Count > 0)
            {
                List<CimInstance> parameterInstances = new List<CimInstance>();
                foreach (var p in this.Parameters)
                {
                    parameterInstances.Add(p.ToCimInstance());
                }

                if (parameterInstances.Count > 0)
                {
                    CimProperty parametersProperty = InternalMISerializer.CreateCimProperty("Parameters",
                                                                                            parameterInstances.ToArray(),
                                                                                            Microsoft.Management.Infrastructure.CimType.ReferenceArray);
                    c.CimInstanceProperties.Add(parametersProperty);
                }
            }

            return c;
        }
#endif

        #endregion Win Blue Extensions
    }

    
    [Flags]
    public enum PipelineResultTypes
    {
        
        None,

        
        Output,

        
        Error,

        
        Warning,

        
        Verbose,

        
        Debug,

        
        Information,

        
        All,

        
        Null
    }

    
    public sealed class CommandCollection : Collection<Command>
    {
        
        internal CommandCollection()
        {
        }

        
        public void Add(string command)
        {
            if (string.Equals(command, "out-default", StringComparison.OrdinalIgnoreCase))
            {
                this.Add(command, true);
            }
            else
            {
                this.Add(new Command(command));
            }
        }

        internal void Add(string command, bool mergeUnclaimedPreviousCommandError)
        {
            this.Add(new Command(command, false, false, mergeUnclaimedPreviousCommandError));
        }

        
        public void AddScript(string scriptContents)
        {
            this.Add(new Command(scriptContents, true));
        }

        
        public void AddScript(string scriptContents, bool useLocalScope)
        {
            this.Add(new Command(scriptContents, true, useLocalScope));
        }

        
        internal string GetCommandStringForHistory()
        {
            Diagnostics.Assert(this.Count != 0, "this is called when there is at least one element in the collection");
            Command firstCommand = this[0];
            return firstCommand.CommandText;
        }
    }
}
