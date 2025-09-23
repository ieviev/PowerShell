// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    public abstract class ObjectEventRegistrationBase : PSCmdlet
    {
        #region parameters

        
        [Parameter(Position = 100)]
        public string SourceIdentifier
        {
            get
            {
                return _sourceIdentifier;
            }

            set
            {
                _sourceIdentifier = value;
            }
        }

        private string _sourceIdentifier = Guid.NewGuid().ToString();

        
        [Parameter(Position = 101)]
        public ScriptBlock Action
        {
            get
            {
                return _action;
            }

            set
            {
                _action = value;
            }
        }

        private ScriptBlock _action = null;

        
        [Parameter]
        public PSObject MessageData
        {
            get
            {
                return _messageData;
            }

            set
            {
                _messageData = value;
            }
        }

        private PSObject _messageData = null;

        
        [Parameter]
        public SwitchParameter SupportEvent
        {
            get
            {
                return _supportEvent;
            }

            set
            {
                _supportEvent = value;
            }
        }

        private SwitchParameter _supportEvent = new SwitchParameter();

        
        [Parameter]
        public SwitchParameter Forward
        {
            get
            {
                return _forward;
            }

            set
            {
                _forward = value;
            }
        }

        private SwitchParameter _forward = new SwitchParameter();

        
        [Parameter]
        public int MaxTriggerCount
        {
            get
            {
                return _maxTriggerCount;
            }

            set
            {
                _maxTriggerCount = value <= 0 ? 0 : value;
            }
        }

        private int _maxTriggerCount = 0;

        #endregion parameters

        
        protected abstract object GetSourceObject();

        
        protected abstract string GetSourceObjectEventName();

        
        protected PSEventSubscriber NewSubscriber
        {
            get { return _newSubscriber; }
        }

        private PSEventSubscriber _newSubscriber;

        
        protected override void BeginProcessing()
        {
            if (((bool)_forward) && (_action != null))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new ArgumentException(EventingResources.ActionAndForwardNotSupported),
                        "ACTION_AND_FORWARD_NOT_SUPPORTED",
                        ErrorCategory.InvalidOperation,
                        null));
            }
        }

        
        protected override void EndProcessing()
        {
            object inputObject = PSObject.Base(GetSourceObject());
            string eventName = GetSourceObjectEventName();

            try
            {
                if (
                    ((inputObject != null) || (eventName != null)) &&
                    (Events.GetEventSubscribers(_sourceIdentifier).GetEnumerator().MoveNext())
                    )
                {
                    // Detect if the event identifier already exists
                    ErrorRecord errorRecord = new ErrorRecord(
                        new ArgumentException(
                            string.Format(
                                System.Globalization.CultureInfo.CurrentCulture,
                                EventingResources.SubscriberExists, _sourceIdentifier)),
                        "SUBSCRIBER_EXISTS",
                        ErrorCategory.InvalidArgument,
                        inputObject);

                    WriteError(errorRecord);
                }
                else
                {
                    _newSubscriber =
                        Events.SubscribeEvent(
                            inputObject,
                            eventName,
                            _sourceIdentifier, _messageData, _action, (bool)_supportEvent, (bool)_forward, _maxTriggerCount);

                    if ((_action != null) && (!(bool)_supportEvent))
                        WriteObject(_newSubscriber.Action);
                }
            }
            catch (ArgumentException e)
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    e,
                    "INVALID_REGISTRATION",
                    ErrorCategory.InvalidArgument,
                    inputObject);

                WriteError(errorRecord);
            }
            catch (InvalidOperationException e)
            {
                ErrorRecord errorRecord = new ErrorRecord(
                    e,
                    "INVALID_REGISTRATION",
                    ErrorCategory.InvalidOperation,
                    inputObject);

                WriteError(errorRecord);
            }
        }
    }
}
