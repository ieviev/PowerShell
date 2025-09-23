// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Runtime.Serialization;

namespace Microsoft.PowerShell.Commands
{
    #region WriteDebugCommand
    
    [Cmdlet(VerbsCommunications.Write, "Debug", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097132", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteDebugCommand : PSCmdlet
    {
        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        [Alias("Msg")]
        public string Message { get; set; }

        
        protected override void ProcessRecord()
        {
            //
            // The write-debug command must use the script's InvocationInfo rather than its own,
            // so we create the DebugRecord here and fill it up with the appropriate InvocationInfo;
            // then, we call the command runtime directly and pass this record to WriteDebug().
            //
            if (this.CommandRuntime is MshCommandRuntime mshCommandRuntime)
            {
                DebugRecord record = new(Message);

                if (GetVariableValue(SpecialVariables.MyInvocation) is InvocationInfo invocationInfo)
                {
                    record.SetInvocationInfo(invocationInfo);
                }

                mshCommandRuntime.WriteDebug(record);
            }
            else
            {
                WriteDebug(Message);
            }
        }
    }
    #endregion WriteDebugCommand

    #region WriteVerboseCommand
    
    [Cmdlet(VerbsCommunications.Write, "Verbose", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097043", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteVerboseCommand : PSCmdlet
    {
        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        [Alias("Msg")]
        public string Message { get; set; }

        
        protected override void ProcessRecord()
        {
            //
            // The write-verbose command must use the script's InvocationInfo rather than its own,
            // so we create the VerboseRecord here and fill it up with the appropriate InvocationInfo;
            // then, we call the command runtime directly and pass this record to WriteVerbose().
            //
            if (this.CommandRuntime is MshCommandRuntime mshCommandRuntime)
            {
                VerboseRecord record = new(Message);

                if (GetVariableValue(SpecialVariables.MyInvocation) is InvocationInfo invocationInfo)
                {
                    record.SetInvocationInfo(invocationInfo);
                }

                mshCommandRuntime.WriteVerbose(record);
            }
            else
            {
                WriteVerbose(Message);
            }
        }
    }
    #endregion WriteVerboseCommand

    #region WriteWarningCommand
    
    [Cmdlet(VerbsCommunications.Write, "Warning", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097044", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteWarningCommand : PSCmdlet
    {
        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyString]
        [Alias("Msg")]
        public string Message { get; set; }

        
        protected override void ProcessRecord()
        {
            //
            // The write-warning command must use the script's InvocationInfo rather than its own,
            // so we create the WarningRecord here and fill it up with the appropriate InvocationInfo;
            // then, we call the command runtime directly and pass this record to WriteWarning().
            //
            if (this.CommandRuntime is MshCommandRuntime mshCommandRuntime)
            {
                WarningRecord record = new(Message);

                if (GetVariableValue(SpecialVariables.MyInvocation) is InvocationInfo invocationInfo)
                {
                    record.SetInvocationInfo(invocationInfo);
                }

                mshCommandRuntime.WriteWarning(record);
            }
            else
            {
                WriteWarning(Message);
            }
        }
    }
    #endregion WriteWarningCommand

    #region WriteInformationCommand
    
    [Cmdlet(VerbsCommunications.Write, "Information", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097040", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteInformationCommand : PSCmdlet
    {
        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [Alias("Msg", "Message")]
        [AllowNull]
        public object MessageData { get; set; }

        
        [Parameter(Position = 1)]
        public string[] Tags { get; set; }

        
        protected override void BeginProcessing()
        {
            if (Tags != null)
            {
                foreach (string tag in Tags)
                {
                    if (tag.StartsWith("PS", StringComparison.OrdinalIgnoreCase))
                    {
                        ErrorRecord er = new(
                            new InvalidOperationException(StringUtil.Format(UtilityCommonStrings.PSPrefixReservedInInformationTag, tag)),
                            "PSPrefixReservedInInformationTag", ErrorCategory.InvalidArgument, tag);
                        ThrowTerminatingError(er);
                    }
                }
            }
        }

        
        protected override void ProcessRecord()
        {
            WriteInformation(MessageData, Tags);
        }
    }

    #endregion WriteInformationCommand

    #region WriteOrThrowErrorCommand

    
    public class WriteOrThrowErrorCommand : PSCmdlet
    {
        
        [Parameter(Position = 0, ParameterSetName = "WithException", Mandatory = true)]
        public Exception Exception { get; set; }

        
        [Parameter(Position = 0, ParameterSetName = "NoException", Mandatory = true, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "WithException")]
        [AllowNull]
        [AllowEmptyString]
        [Alias("Msg")]
        public string Message { get; set; }

        
        [Parameter(Position = 0, ParameterSetName = "ErrorRecord", Mandatory = true)]
        public ErrorRecord ErrorRecord { get; set; }

        
        [Parameter(ParameterSetName = "NoException")]
        [Parameter(ParameterSetName = "WithException")]
        public ErrorCategory Category { get; set; } = ErrorCategory.NotSpecified;

        
        [Parameter(ParameterSetName = "NoException")]
        [Parameter(ParameterSetName = "WithException")]
        public string ErrorId { get; set; } = string.Empty;

        
        [Parameter(ParameterSetName = "NoException")]
        [Parameter(ParameterSetName = "WithException")]
        public object TargetObject { get; set; }

        
        [Parameter]
        public string RecommendedAction { get; set; } = string.Empty;

        

        
        [Parameter]
        [Alias("Activity")]
        public string CategoryActivity { get; set; } = string.Empty;

        
        [Parameter]
        [Alias("Reason")]
        public string CategoryReason { get; set; } = string.Empty;

        
        [Parameter]
        [Alias("TargetName")]
        public string CategoryTargetName { get; set; } = string.Empty;

        
        [Parameter]
        [Alias("TargetType")]
        public string CategoryTargetType { get; set; } = string.Empty;

        
        protected override void ProcessRecord()
        {
            ErrorRecord errorRecord = this.ErrorRecord;
            if (errorRecord != null)
            {
                // copy constructor
                errorRecord = new ErrorRecord(errorRecord, null);
            }
            else
            {
                Exception e = this.Exception;
                string msg = Message;
                e ??= new WriteErrorException(msg);

                string errid = ErrorId;
                if (string.IsNullOrEmpty(errid))
                {
                    errid = e.GetType().FullName;
                }

                errorRecord = new ErrorRecord(
                    e,
                    errid,
                    Category,
                    TargetObject
                    );

                if (this.Exception != null && !string.IsNullOrEmpty(msg))
                {
                    errorRecord.ErrorDetails = new ErrorDetails(msg);
                }
            }

            string recact = RecommendedAction;
            if (!string.IsNullOrEmpty(recact))
            {
                errorRecord.ErrorDetails ??= new ErrorDetails(errorRecord.ToString());

                errorRecord.ErrorDetails.RecommendedAction = recact;
            }

            if (!string.IsNullOrEmpty(CategoryActivity))
                errorRecord.CategoryInfo.Activity = CategoryActivity;
            if (!string.IsNullOrEmpty(CategoryReason))
                errorRecord.CategoryInfo.Reason = CategoryReason;
            if (!string.IsNullOrEmpty(CategoryTargetName))
                errorRecord.CategoryInfo.TargetName = CategoryTargetName;
            if (!string.IsNullOrEmpty(CategoryTargetType))
                errorRecord.CategoryInfo.TargetType = CategoryTargetType;

            

            // 2005/07/14-913791 "write-error output is confusing and misleading"
            // set InvocationInfo to the script not the command
            if (GetVariableValue(SpecialVariables.MyInvocation) is InvocationInfo myInvocation)
            {
                errorRecord.SetInvocationInfo(myInvocation);
                errorRecord.PreserveInvocationInfoOnce = true;
                if (!string.IsNullOrEmpty(CategoryActivity))
                    errorRecord.CategoryInfo.Activity = CategoryActivity;
                else
                    errorRecord.CategoryInfo.Activity = "Write-Error";
            }

            WriteError(errorRecord);
            
        }
    }

    
    [Cmdlet(VerbsCommunications.Write, "Error", DefaultParameterSetName = "NoException",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097039", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteErrorCommand : WriteOrThrowErrorCommand
    {
        
        public WriteErrorCommand()
        {
        }
    }

    

    #endregion WriteOrThrowErrorCommand

    #region WriteErrorException
    
    public class WriteErrorException : SystemException
    {
        #region ctor
        
        /// <returns>Constructed object.</returns>
        public WriteErrorException()
            : base(StringUtil.Format(WriteErrorStrings.WriteErrorException))
        {
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public WriteErrorException(string message)
            : base(message)
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public WriteErrorException(string message,
                                          Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected WriteErrorException(SerializationInfo info,
                                      StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization
    }
    #endregion WriteErrorException
}
