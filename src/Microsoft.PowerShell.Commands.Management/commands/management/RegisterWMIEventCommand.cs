// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsLifecycle.Register, "WmiEvent", DefaultParameterSetName = "class",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135245", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class RegisterWmiEventCommand : ObjectEventRegistrationBase
    {
        #region parameters

        
        [Parameter]
        [Alias("NS")]
        public string Namespace { get; set; } = "root\\cimv2";

        
        [Parameter]
        [Credential]
        public PSCredential Credential { get; set; }

        
        [Parameter]
        [Alias("Cn")]
        [ValidateNotNullOrEmpty]
        public string ComputerName { get; set; } = "localhost";

        
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "class")]
        public string Class { get; set; } = null;

        
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "query")]
        public string Query { get; set; } = null;

        
        [Parameter]
        [Alias("TimeoutMSec")]
        public Int64 Timeout
        {
            get
            {
                return _timeOut;
            }

            set
            {
                _timeOut = value;
                _timeoutSpecified = true;
            }
        }

        private Int64 _timeOut = 0;
        private bool _timeoutSpecified = false;

        #endregion parameters
        #region helper functions
        private string BuildEventQuery(string objectName)
        {
            StringBuilder returnValue = new StringBuilder("select * from ");
            returnValue.Append(objectName);
            return returnValue.ToString();
        }

        private string GetScopeString(string computer, string namespaceParameter)
        {
            StringBuilder returnValue = new StringBuilder("\\\\");
            returnValue.Append(computer);
            returnValue.Append('\\');
            returnValue.Append(namespaceParameter);
            return returnValue.ToString();
        }
        #endregion helper functions

        
        protected override object GetSourceObject()
        {
            string wmiQuery = this.Query;
            if (this.Class != null)
            {
                // Validate class format
                for (int i = 0; i < this.Class.Length; i++)
                {
                    if (char.IsLetterOrDigit(this.Class[i]) || this.Class[i].Equals('_'))
                    {
                        continue;
                    }

                    ErrorRecord errorRecord = new ErrorRecord(
                        new ArgumentException(
                            string.Format(
                                Thread.CurrentThread.CurrentCulture,
                                "Class", this.Class)),
                        "INVALID_QUERY_IDENTIFIER",
                        ErrorCategory.InvalidArgument,
                        null);
                    errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiInvalidClass");

                    ThrowTerminatingError(errorRecord);
                    return null;
                }

                wmiQuery = BuildEventQuery(this.Class);
            }

            ConnectionOptions conOptions = new ConnectionOptions();
            if (this.Credential != null)
            {
                System.Net.NetworkCredential cred = this.Credential.GetNetworkCredential();
                if (string.IsNullOrEmpty(cred.Domain))
                {
                    conOptions.Username = cred.UserName;
                }
                else
                {
                    conOptions.Username = cred.Domain + "\\" + cred.UserName;
                }

                conOptions.Password = cred.Password;
            }

            ManagementScope scope = new ManagementScope(GetScopeString(ComputerName, this.Namespace), conOptions);
            EventWatcherOptions evtOptions = new EventWatcherOptions();

            if (_timeoutSpecified)
            {
                evtOptions.Timeout = new TimeSpan(_timeOut * 10000);
            }

            ManagementEventWatcher watcher = new ManagementEventWatcher(scope, new EventQuery(wmiQuery), evtOptions);
            return watcher;
        }

        
        protected override string GetSourceObjectEventName()
        {
            return "EventArrived";
        }

        
        protected override void EndProcessing()
        {
            base.EndProcessing();

            // Register for the "Unsubscribed" event so that we can stop the
            // event watcher.
            PSEventSubscriber newSubscriber = NewSubscriber;
            if (newSubscriber != null)
            {
                newSubscriber.Unsubscribed += new PSEventUnsubscribedEventHandler(newSubscriber_Unsubscribed);
            }
        }

        private void newSubscriber_Unsubscribed(object sender, PSEventUnsubscribedEventArgs e)
        {
            ManagementEventWatcher watcher = sender as ManagementEventWatcher;
            if (watcher != null)
            {
                watcher.Stop();
            }
        }
    }
}
