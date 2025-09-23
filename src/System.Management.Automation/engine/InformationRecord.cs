// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    /// <remarks>
    /// InformationRecords are passed to <see cref="System.Management.Automation.Cmdlet.WriteInformation(object, string[])"/>,
    /// which, according to host or user preference, forwards that information on to the host for rendering to the user.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Cmdlet.WriteInformation(object, string[])"/>
    [DataContract]
    public class InformationRecord
    {
        
        /// <param name="messageData">The object to be transmitted to the host.</param>
        /// <param name="source">The source of the message (i.e.: script path, function name, etc.).</param>
        public InformationRecord(object messageData, string source)
        {
            this.MessageData = messageData;
            this.Source = source;

            this.TimeGenerated = DateTime.Now;
            this.NativeThreadId = PsUtils.GetNativeThreadId();
            this.ManagedThreadId = (uint)Environment.CurrentManagedThreadId;
        }

        private InformationRecord() { }

        
        internal InformationRecord(InformationRecord baseRecord)
        {
            this.MessageData = baseRecord.MessageData;
            this.Source = baseRecord.Source;

            this.TimeGenerated = baseRecord.TimeGenerated;
            this.Tags = baseRecord.Tags;
            this.User = baseRecord.User;
            this.Computer = baseRecord.Computer;
            this.ProcessId = baseRecord.ProcessId;
            this.NativeThreadId = baseRecord.NativeThreadId;
            this.ManagedThreadId = baseRecord.ManagedThreadId;
        }

        // Some of these setters are internal, while others are public.
        // The ones that are public are left that way because systems that proxy
        // the events may need to alter them (i.e.: workflow). The ones that remain internal
        // are that way because they are fundamental properties of the record itself.

        
        [DataMember]
        public object MessageData { get; internal set; }

        
        [DataMember]
        public string Source { get; set; }

        
        [DataMember]
        public DateTime TimeGenerated { get; set; }

        
        [DataMember]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public List<string> Tags
        {
            get { return _tags ??= new List<string>(); }

            internal set { _tags = value; }
        }

        private List<string> _tags;

        
        [DataMember]
        public string User
        {
            get
            {
                // domain\user on Windows, just user on Unix
                this._user ??=
#if UNIX
                    Environment.UserName;
#else
                    Environment.UserDomainName + "\\" + Environment.UserName;
#endif

                return _user;
            }

            set
            {
                _user = value;
            }
        }

        private string _user;

        
        [DataMember]
        public string Computer
        {
            get { return this._computerName ??= PsUtils.GetHostName(); }

            set { this._computerName = value; }
        }

        private string _computerName;

        
        [DataMember]
        public uint ProcessId
        {
            get
            {
                if (!this._processId.HasValue)
                {
                    this._processId = (uint)Environment.ProcessId;
                }

                return this._processId.Value;
            }

            set
            {
                _processId = value;
            }
        }

        private uint? _processId;

        
        public uint NativeThreadId { get; set; }

        
        [DataMember]
        public uint ManagedThreadId { get; set; }

        
        /// <returns></returns>
        public override string ToString()
        {
            if (MessageData != null)
            {
                return MessageData.ToString();
            }
            else
            {
                return base.ToString();
            }
        }

        internal static InformationRecord FromPSObjectForRemoting(PSObject inputObject)
        {
            InformationRecord informationRecord = new InformationRecord();

            informationRecord.MessageData = RemotingDecoder.GetPropertyValue<object>(inputObject, "MessageData");
            informationRecord.Source = RemotingDecoder.GetPropertyValue<string>(inputObject, "Source");
            informationRecord.TimeGenerated = RemotingDecoder.GetPropertyValue<DateTime>(inputObject, "TimeGenerated");

            informationRecord.Tags = new List<string>();
            System.Collections.ArrayList tagsArrayList = RemotingDecoder.GetPropertyValue<System.Collections.ArrayList>(inputObject, "Tags");
            foreach (string tag in tagsArrayList)
            {
                informationRecord.Tags.Add(tag);
            }

            informationRecord.User = RemotingDecoder.GetPropertyValue<string>(inputObject, "User");
            informationRecord.Computer = RemotingDecoder.GetPropertyValue<string>(inputObject, "Computer");
            informationRecord.ProcessId = RemotingDecoder.GetPropertyValue<uint>(inputObject, "ProcessId");
            informationRecord.NativeThreadId = RemotingDecoder.GetPropertyValue<uint>(inputObject, "NativeThreadId");
            informationRecord.ManagedThreadId = RemotingDecoder.GetPropertyValue<uint>(inputObject, "ManagedThreadId");

            return informationRecord;
        }

        
        /// <returns>This object as a PSObject property bag.</returns>
        internal PSObject ToPSObjectForRemoting()
        {
            PSObject informationAsPSObject = RemotingEncoder.CreateEmptyPSObject();

            informationAsPSObject.Properties.Add(new PSNoteProperty("MessageData", this.MessageData));
            informationAsPSObject.Properties.Add(new PSNoteProperty("Source", this.Source));
            informationAsPSObject.Properties.Add(new PSNoteProperty("TimeGenerated", this.TimeGenerated));
            informationAsPSObject.Properties.Add(new PSNoteProperty("Tags", this.Tags));
            informationAsPSObject.Properties.Add(new PSNoteProperty("User", this.User));
            informationAsPSObject.Properties.Add(new PSNoteProperty("Computer", this.Computer));
            informationAsPSObject.Properties.Add(new PSNoteProperty("ProcessId", this.ProcessId));
            informationAsPSObject.Properties.Add(new PSNoteProperty("NativeThreadId", this.NativeThreadId));
            informationAsPSObject.Properties.Add(new PSNoteProperty("ManagedThreadId", this.ManagedThreadId));

            return informationAsPSObject;
        }
    }

    
    public class HostInformationMessage
    {
        
        public string Message { get; set; }

        
        public bool? NoNewLine { get; set; }

        
        public ConsoleColor? ForegroundColor { get; set; }

        
        public ConsoleColor? BackgroundColor { get; set; }

        
        /// <returns></returns>
        public override string ToString()
        {
            return Message;
        }
    }
}
