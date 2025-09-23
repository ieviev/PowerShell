// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsLifecycle.Register, "EngineEvent", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097128")]
    [OutputType(typeof(PSEventJob))]
    public class RegisterEngineEventCommand : ObjectEventRegistrationBase
    {
        
        [Parameter(Mandatory = true, Position = 100)]
        public new string SourceIdentifier
        {
            get
            {
                return base.SourceIdentifier;
            }

            set
            {
                base.SourceIdentifier = value;
            }
        }

        
        protected override object GetSourceObject()
        {
            // If it's not a forwarded event, the user must specify
            // an action
            if (
                (Action == null) &&
                (!(bool)Forward)
               )
            {
                ErrorRecord errorRecord = new(
                    new ArgumentException(EventingStrings.ActionMandatoryForLocal),
                    "ACTION_MANDATORY_FOR_LOCAL",
                    ErrorCategory.InvalidArgument,
                    null);

                ThrowTerminatingError(errorRecord);
            }

            return null;
        }

        
        protected override string GetSourceObjectEventName()
        {
            return null;
        }
    }
}
