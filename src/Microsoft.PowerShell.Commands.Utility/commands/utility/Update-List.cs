// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsData.Update, "List", DefaultParameterSetName = "AddRemoveSet",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2109383", RemotingCapability = RemotingCapability.None)]
    public class UpdateListCommand : PSCmdlet
    {
        
        [Parameter(ParameterSetName = "AddRemoveSet")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public object[] Add { get; set; }

        
        [Parameter(ParameterSetName = "AddRemoveSet")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public object[] Remove { get; set; }

        
        [Parameter(Mandatory = true, ParameterSetName = "ReplaceSet")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public object[] Replace { get; set; }

        
        // [Parameter(ValueFromPipeline = true, ParameterSetName = "AddRemoveSet")]
        // [Parameter(ValueFromPipeline = true, ParameterSetName = "ReplaceSet")]
        [Parameter(ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public PSObject InputObject { get; set; }

        
        // [Parameter(Position = 0, ParameterSetName = "AddRemoveSet")]
        // [Parameter(Position = 0, ParameterSetName = "ReplaceSet")]
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Property { get; set; }

        private PSListModifier _listModifier;

        
        protected override void ProcessRecord()
        {
            if (Property != null)
            {
                if (InputObject == null)
                {
                    WriteError(NewError("MissingInputObjectParameter", "MissingInputObjectParameter", null));
                }
                else
                {
                    _listModifier ??= CreatePSListModifier();

                    PSMemberInfo memberInfo = InputObject.Members[Property];
                    if (memberInfo != null)
                    {
                        try
                        {
                            _listModifier.ApplyTo(memberInfo.Value);
                            WriteObject(InputObject);
                        }
                        catch (PSInvalidOperationException e)
                        {
                            WriteError(new ErrorRecord(e, "ApplyFailed", ErrorCategory.InvalidOperation, null));
                        }
                    }
                    else
                    {
                        WriteError(NewError("MemberDoesntExist", "MemberDoesntExist", InputObject, Property));
                    }
                }
            }
        }

        
        protected override void EndProcessing()
        {
            if (Property == null)
            {
                if (InputObject != null)
                {
                    ThrowTerminatingError(NewError("MissingPropertyParameter", "MissingPropertyParameter", null));
                }
                else
                {
                    WriteObject(CreateHashtable());
                }
            }
        }

        private Hashtable CreateHashtable()
        {
            Hashtable hash = new(2);
            if (Add != null)
            {
                hash.Add("Add", Add);
            }

            if (Remove != null)
            {
                hash.Add("Remove", Remove);
            }

            if (Replace != null)
            {
                hash.Add("Replace", Replace);
            }

            return hash;
        }

        private PSListModifier CreatePSListModifier()
        {
            PSListModifier listModifier = new();
            if (Add != null)
            {
                foreach (object obj in Add)
                {
                    listModifier.Add.Add(obj);
                }
            }

            if (Remove != null)
            {
                foreach (object obj in Remove)
                {
                    listModifier.Remove.Add(obj);
                }
            }

            if (Replace != null)
            {
                foreach (object obj in Replace)
                {
                    listModifier.Replace.Add(obj);
                }
            }

            return listModifier;
        }

        private ErrorRecord NewError(string errorId, string resourceId, object targetObject, params object[] args)
        {
            ErrorDetails details = new(this.GetType().Assembly, "UpdateListStrings", resourceId, args);
            ErrorRecord errorRecord = new(
                new InvalidOperationException(details.Message),
                errorId,
                ErrorCategory.InvalidOperation,
                targetObject);
            return errorRecord;
        }
    }
}
