// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsLifecycle.Register, "ObjectEvent", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096714")]
    [OutputType(typeof(PSEventJob))]
    public class RegisterObjectEventCommand : ObjectEventRegistrationBase
    {
        #region parameters

        
        [Parameter(Mandatory = true, Position = 0)]
        public PSObject InputObject
        {
            get
            {
                return _inputObject;
            }

            set
            {
                _inputObject = value;
            }
        }

        private PSObject _inputObject = null;

        
        [Parameter(Mandatory = true, Position = 1)]
        public string EventName
        {
            get
            {
                return _eventName;
            }

            set
            {
                _eventName = value;
            }
        }

        private string _eventName = null;

        #endregion parameters

        
        protected override object GetSourceObject()
        {
            return _inputObject;
        }

        
        protected override string GetSourceObjectEventName()
        {
            return _eventName;
        }
    }
}
