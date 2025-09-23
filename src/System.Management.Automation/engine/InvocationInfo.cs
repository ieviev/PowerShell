// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    
    [DebuggerDisplay("Command = {MyCommand}")]
    public class InvocationInfo
    {
        #region Constructors

        
        internal InvocationInfo(InternalCommand command)
            : this(command.CommandInfo,
                   command.InvocationExtent ?? PositionUtilities.EmptyExtent)
        {
            CommandOrigin = command.CommandOrigin;
        }

        
        internal InvocationInfo(CommandInfo commandInfo, IScriptExtent scriptPosition)
            : this(commandInfo, scriptPosition, null)
        {
            // nothing to do here
        }

        
        internal InvocationInfo(CommandInfo commandInfo, IScriptExtent scriptPosition, ExecutionContext context)
        {
            MyCommand = commandInfo;
            CommandOrigin = CommandOrigin.Internal;
            _scriptPosition = scriptPosition;

            ExecutionContext contextToUse = null;
            if ((commandInfo != null) && (commandInfo.Context != null))
            {
                contextToUse = commandInfo.Context;
            }
            else if (context != null)
            {
                contextToUse = context;
            }

            // Populate the history ID of this command
            if (contextToUse != null)
            {
                Runspaces.LocalRunspace localRunspace = contextToUse.CurrentRunspace as Runspaces.LocalRunspace;
                if (localRunspace != null && localRunspace.History != null)
                {
                    HistoryId = localRunspace.History.GetNextHistoryId();
                }
            }
        }

        
        internal InvocationInfo(PSObject psObject)
        {
            CommandOrigin = (CommandOrigin)SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "InvocationInfo_CommandOrigin");
            ExpectingInput = (bool)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_ExpectingInput");
            _invocationName = (string)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_InvocationName");
            HistoryId = (long)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_HistoryId");
            PipelineLength = (int)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_PipelineLength");
            PipelinePosition = (int)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_PipelinePosition");

            string scriptName = (string)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_ScriptName");
            int scriptLineNumber = (int)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_ScriptLineNumber");
            int offsetInLine = (int)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_OffsetInLine");
            string line = (string)SerializationUtilities.GetPropertyValue(psObject, "InvocationInfo_Line");
            var scriptPosition = new ScriptPosition(scriptName, scriptLineNumber, offsetInLine, line);

            ScriptPosition scriptEndPosition;
            if (!string.IsNullOrEmpty(line))
            {
                int endColumn = line.Length + 1;
                scriptEndPosition = new ScriptPosition(scriptName, scriptLineNumber, endColumn, line);
            }
            else
            {
                scriptEndPosition = scriptPosition;
            }

            _scriptPosition = new ScriptExtent(scriptPosition, scriptEndPosition);

            MyCommand = RemoteCommandInfo.FromPSObjectForRemoting(psObject);

            //
            // Arrays are de-serialized as ArrayList so we need to convert the deserialized
            // object into an int[] before assigning to pipelineIterationInfo.
            //
            var list = (ArrayList)SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "InvocationInfo_PipelineIterationInfo");
            if (list != null)
            {
                PipelineIterationInfo = (int[])list.ToArray(typeof(int));
            }
            else
            {
                PipelineIterationInfo = Array.Empty<int>();
            }

            //
            // Dictionaries are de-serialized as Hashtables so we need to convert the deserialized object into a dictionary
            // before assigning to CommandLineParameters.
            //
            Hashtable hashtable = (Hashtable)SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "InvocationInfo_BoundParameters");

            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            if (hashtable != null)
            {
                foreach (DictionaryEntry entry in hashtable)
                {
                    dictionary.Add((string)entry.Key, entry.Value);
                }
            }

            _boundParameters = dictionary;

            //
            // The unbound parameters are de-serialized as an ArrayList, which we need to convert to a List
            //
            var unboundArguments = (ArrayList)SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "InvocationInfo_UnboundArguments");

            _unboundArguments = new List<object>();

            if (unboundArguments != null)
            {
                foreach (object o in unboundArguments)
                {
                    _unboundArguments.Add(o);
                }
            }

            object value = SerializationUtilities.GetPropertyValue(psObject, "SerializeExtent");
            bool serializeExtent = false;

            if (value != null)
                serializeExtent = (bool)value;

            if (serializeExtent)
                DisplayScriptPosition = ScriptExtent.FromPSObjectForRemoting(psObject);
        }

        #endregion Constructors

        #region Private Data

        private IScriptExtent _scriptPosition;
        private string _invocationName;
        private Dictionary<string, object> _boundParameters;
        private List<object> _unboundArguments;

        #endregion Internal or Private

        #region Public Members

        
        public CommandInfo MyCommand { get; }

        
        public Dictionary<string, object> BoundParameters
        {
            get
            {
                return _boundParameters ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            internal set
            {
                _boundParameters = value;
            }
        }

        
        public List<object> UnboundArguments
        {
            get { return _unboundArguments ??= new List<object>(); }

            internal set { _unboundArguments = value; }
        }

        
        public int ScriptLineNumber
        {
            get { return ScriptPosition.StartLineNumber; }
        }

        
        public int OffsetInLine
        {
            get { return ScriptPosition.StartColumnNumber; }
        }

        
        public long HistoryId { get; internal set; } = -1;

        
        public string ScriptName
        {
            get { return ScriptPosition.File ?? string.Empty; }
        }

        
        public string Line
        {
            get
            {
                if (ScriptPosition.StartScriptPosition != null)
                {
                    return ScriptPosition.StartScriptPosition.Line;
                }

                return string.Empty;
            }
        }

        
        public string Statement
        {
            get
            {
                return ScriptPosition.Text;
            }
        }

        
        public string PositionMessage
        {
            get { return PositionUtilities.VerboseMessage(ScriptPosition); }
        }

        
        public string PSScriptRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(ScriptPosition.File))
                {
                    return Path.GetDirectoryName(ScriptPosition.File);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        
        public string PSCommandPath
        {
            get { return ScriptPosition.File; }
        }

        
        public string InvocationName
        {
            get { return _invocationName ?? string.Empty; }

            internal set { _invocationName = value; }
        }

        
        public int PipelineLength { get; internal set; }

        
        public int PipelinePosition { get; internal set; }

        
        public bool ExpectingInput { get; internal set; }

        
        public CommandOrigin CommandOrigin { get; internal set; }

        
        public IScriptExtent DisplayScriptPosition { get; set; }

        
        public static InvocationInfo Create(
            CommandInfo commandInfo,
            IScriptExtent scriptPosition)
        {
            var invocationInfo = new InvocationInfo(commandInfo, scriptPosition);
            invocationInfo.DisplayScriptPosition = scriptPosition;

            return invocationInfo;
        }

        #endregion Public Members

        #region Internal Members

        
        internal IScriptExtent ScriptPosition
        {
            get
            {
                if (DisplayScriptPosition != null)
                {
                    return DisplayScriptPosition;
                }
                else
                {
                    return _scriptPosition;
                }
            }

            set
            {
                _scriptPosition = value;
            }
        }

        
        internal string GetFullScript()
        {
            return (ScriptPosition != null) && (ScriptPosition.StartScriptPosition != null) ?
                ScriptPosition.StartScriptPosition.GetFullScript() : null;
        }

        
        internal int[] PipelineIterationInfo { get; set; } = Array.Empty<int>();

        
        internal void ToPSObjectForRemoting(PSObject psObject)
        {
            RemotingEncoder.AddNoteProperty<object>(psObject, "InvocationInfo_BoundParameters", () => this.BoundParameters);
            RemotingEncoder.AddNoteProperty<CommandOrigin>(psObject, "InvocationInfo_CommandOrigin", () => this.CommandOrigin);
            RemotingEncoder.AddNoteProperty<bool>(psObject, "InvocationInfo_ExpectingInput", () => this.ExpectingInput);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_InvocationName", () => this.InvocationName);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_Line", () => this.Line);
            RemotingEncoder.AddNoteProperty<int>(psObject, "InvocationInfo_OffsetInLine", () => this.OffsetInLine);
            RemotingEncoder.AddNoteProperty<long>(psObject, "InvocationInfo_HistoryId", () => this.HistoryId);
            RemotingEncoder.AddNoteProperty<int[]>(psObject, "InvocationInfo_PipelineIterationInfo", () => this.PipelineIterationInfo);
            RemotingEncoder.AddNoteProperty<int>(psObject, "InvocationInfo_PipelineLength", () => this.PipelineLength);
            RemotingEncoder.AddNoteProperty<int>(psObject, "InvocationInfo_PipelinePosition", () => this.PipelinePosition);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_PSScriptRoot", () => this.PSScriptRoot);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_PSCommandPath", () => this.PSCommandPath);
            // PositionMessage is ignored when deserializing because it is synthesized from the other position related fields, but
            // it is serialized for backwards compatibility.
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_PositionMessage", () => this.PositionMessage);
            RemotingEncoder.AddNoteProperty<int>(psObject, "InvocationInfo_ScriptLineNumber", () => this.ScriptLineNumber);
            RemotingEncoder.AddNoteProperty<string>(psObject, "InvocationInfo_ScriptName", () => this.ScriptName);
            RemotingEncoder.AddNoteProperty<object>(psObject, "InvocationInfo_UnboundArguments", () => this.UnboundArguments);

            ScriptExtent extent = DisplayScriptPosition as ScriptExtent;
            if (extent != null)
            {
                extent.ToPSObjectForRemoting(psObject);
                RemotingEncoder.AddNoteProperty(psObject, "SerializeExtent", static () => true);
            }
            else
            {
                RemotingEncoder.AddNoteProperty(psObject, "SerializeExtent", static () => false);
            }

            RemoteCommandInfo.ToPSObjectForRemoting(this.MyCommand, psObject);
        }

        #endregion Internal Members
    }

    
    public class RemoteCommandInfo : CommandInfo
    {
        
        private RemoteCommandInfo(string name, CommandTypes type)
            : base(name, type)
        {
            // nothing to do here
        }

        
        public override string Definition { get { return _definition; } }

        
        internal static RemoteCommandInfo FromPSObjectForRemoting(PSObject psObject)
        {
            RemoteCommandInfo commandInfo = null;

            object ctype = SerializationUtilities.GetPsObjectPropertyBaseObject(psObject, "CommandInfo_CommandType");

            if (ctype != null)
            {
                CommandTypes type = RemotingDecoder.GetPropertyValue<CommandTypes>(psObject, "CommandInfo_CommandType");
                string name = RemotingDecoder.GetPropertyValue<string>(psObject, "CommandInfo_Name");

                commandInfo = new RemoteCommandInfo(name, type);
                commandInfo._definition = RemotingDecoder.GetPropertyValue<string>(psObject, "CommandInfo_Definition");
                commandInfo.Visibility = RemotingDecoder.GetPropertyValue<SessionStateEntryVisibility>(psObject, "CommandInfo_Visibility");
            }

            return commandInfo;
        }

        
        internal static void ToPSObjectForRemoting(CommandInfo commandInfo, PSObject psObject)
        {
            if (commandInfo != null)
            {
                RemotingEncoder.AddNoteProperty<CommandTypes>(psObject, "CommandInfo_CommandType", () => commandInfo.CommandType);
                RemotingEncoder.AddNoteProperty<string>(psObject, "CommandInfo_Definition", () => commandInfo.Definition);
                RemotingEncoder.AddNoteProperty<string>(psObject, "CommandInfo_Name", () => commandInfo.Name);
                RemotingEncoder.AddNoteProperty<SessionStateEntryVisibility>(psObject, "CommandInfo_Visibility", () => commandInfo.Visibility);
            }
        }

        
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get { return null; }
        }

        private string _definition;
    }
}
