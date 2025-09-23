// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    [DataContract]
    public abstract class InformationalRecord
    {
        /// <remarks>
        /// This class can be instantiated only by its derived classes
        /// </remarks>
        internal InformationalRecord(string message)
        {
            _message = message;
            _invocationInfo = null;
            _pipelineIterationInfo = null;
            _serializeExtendedInfo = false;
        }

        
        internal InformationalRecord(PSObject serializedObject)
        {
            _message = (string)SerializationUtilities.GetPropertyValue(serializedObject, "InformationalRecord_Message");
            _serializeExtendedInfo = (bool)SerializationUtilities.GetPropertyValue(serializedObject, "InformationalRecord_SerializeInvocationInfo");

            if (_serializeExtendedInfo)
            {
                _invocationInfo = new InvocationInfo(serializedObject);

                ArrayList pipelineIterationInfo = (ArrayList)SerializationUtilities.GetPsObjectPropertyBaseObject(serializedObject, "InformationalRecord_PipelineIterationInfo");

                _pipelineIterationInfo = new ReadOnlyCollection<int>((int[])pipelineIterationInfo.ToArray(typeof(int)));
            }
            else
            {
                _invocationInfo = null;
            }
        }

        
        public string Message
        {
            get
            {
                return _message;
            }

            set
            {
                _message = value;
            }
        }

        
        /// <remarks>
        /// The InvocationInfo can be null if the record was not created by a command.
        /// </remarks>
        public InvocationInfo InvocationInfo
        {
            get
            {
                return _invocationInfo;
            }
        }

        
        /// <remarks>
        /// The PipelineIterationInfo can be null if the record was not created by a command.
        /// </remarks>
        public ReadOnlyCollection<int> PipelineIterationInfo
        {
            get
            {
                return _pipelineIterationInfo;
            }
        }

        
        internal void SetInvocationInfo(InvocationInfo invocationInfo)
        {
            _invocationInfo = invocationInfo;

            //
            // Copy a snapshot of the PipelineIterationInfo from the InvocationInfo to this InformationalRecord
            //
            if (invocationInfo.PipelineIterationInfo != null)
            {
                int[] snapshot = (int[])invocationInfo.PipelineIterationInfo.Clone();

                _pipelineIterationInfo = new ReadOnlyCollection<int>(snapshot);
            }
        }

        
        internal bool SerializeExtendedInfo
        {
            get
            {
                return _serializeExtendedInfo;
            }

            set
            {
                _serializeExtendedInfo = value;
            }
        }

        
        public override string ToString()
        {
            return this.Message;
        }

        
        internal virtual void ToPSObjectForRemoting(PSObject psObject)
        {
            RemotingEncoder.AddNoteProperty<string>(psObject, "InformationalRecord_Message", () => this.Message);

            //
            // The invocation info may be null if the record was created via WriteVerbose/Warning/DebugLine instead of WriteVerbose/Warning/Debug, in that case
            // we set InformationalRecord_SerializeInvocationInfo to false.
            //
            if (!this.SerializeExtendedInfo || _invocationInfo == null)
            {
                RemotingEncoder.AddNoteProperty(psObject, "InformationalRecord_SerializeInvocationInfo", () => false);
            }
            else
            {
                RemotingEncoder.AddNoteProperty(psObject, "InformationalRecord_SerializeInvocationInfo", () => true);
                _invocationInfo.ToPSObjectForRemoting(psObject);
                RemotingEncoder.AddNoteProperty<object>(psObject, "InformationalRecord_PipelineIterationInfo", () => this.PipelineIterationInfo);
            }
        }

        [DataMember]
        private string _message;

        private InvocationInfo _invocationInfo;
        private ReadOnlyCollection<int> _pipelineIterationInfo;
        private bool _serializeExtendedInfo;
    }

    
    [DataContract]
    public class WarningRecord : InformationalRecord
    {
        
        /// <param name="message"></param>
        public WarningRecord(string message)
            : base(message)
        { }

        
        /// <param name="record"></param>
        public WarningRecord(PSObject record)
            : base(record)
        { }

        
        /// <param name="fullyQualifiedWarningId">Fully qualified warning Id.</param>
        /// <param name="message">Warning message.</param>
        public WarningRecord(string fullyQualifiedWarningId, string message)
            : base(message)
        {
            _fullyQualifiedWarningId = fullyQualifiedWarningId;
        }

        
        /// <param name="fullyQualifiedWarningId">Fully qualified warning Id.</param>
        /// <param name="record">Warning serialized object.</param>
        public WarningRecord(string fullyQualifiedWarningId, PSObject record)
            : base(record)
        {
            _fullyQualifiedWarningId = fullyQualifiedWarningId;
        }

        
        public string FullyQualifiedWarningId
        {
            get
            {
                return _fullyQualifiedWarningId ?? string.Empty;
            }
        }

        private readonly string _fullyQualifiedWarningId;
    }

    
    [DataContract]
    public class DebugRecord : InformationalRecord
    {
        
        /// <param name="message"></param>
        public DebugRecord(string message)
            : base(message)
        { }

        
        /// <param name="record"></param>
        public DebugRecord(PSObject record)
            : base(record)
        { }
    }

    
    [DataContract]
    public class VerboseRecord : InformationalRecord
    {
        
        /// <param name="message"></param>
        public VerboseRecord(string message)
            : base(message)
        { }

        
        /// <param name="record"></param>
        public VerboseRecord(PSObject record)
            : base(record)
        { }
    }
}
