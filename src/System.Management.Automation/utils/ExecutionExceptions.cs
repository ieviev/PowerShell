// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Internal;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation
{
    #region CmdletInvocationException
    
    public class CmdletInvocationException : RuntimeException
    {
        #region ctor
        
        internal CmdletInvocationException(ErrorRecord errorRecord)
            : base(RetrieveMessage(errorRecord), RetrieveException(errorRecord))
        {
            ArgumentNullException.ThrowIfNull(errorRecord);

            _errorRecord = errorRecord;
            if (errorRecord.Exception != null)
            {
                // 2005/04/13-JonN Can't do this in an unsealed class: HelpLink = errorRecord.Exception.HelpLink;
                // Exception.Source is set by Throw
                // Source = errorRecord.Exception.Source;
            }
        }

        
        internal CmdletInvocationException(Exception innerException,
                                           InvocationInfo invocationInfo)
            : base(RetrieveMessage(innerException), innerException)
        {
            ArgumentNullException.ThrowIfNull(innerException);
            // invocationInfo may be null

            if (innerException is IContainsErrorRecord icer && icer.ErrorRecord != null)
            {
                _errorRecord = new ErrorRecord(icer.ErrorRecord, innerException);
            }
            else
            {
                // When no ErrorId is specified by a thrown exception,
                //  we use innerException.GetType().FullName.
                _errorRecord = new ErrorRecord(
                    innerException,
                    innerException.GetType().FullName,
                    ErrorCategory.NotSpecified,
                    null);
            }

            _errorRecord.SetInvocationInfo(invocationInfo);
            // 2005/04/13-JonN Can't do this in an unsealed class: HelpLink = innerException.HelpLink;
            // Exception.Source is set by Throw
            // Source = innerException.Source;
        }

        
        public CmdletInvocationException()
            : base()
        {
        }

        
        public CmdletInvocationException(string message)
            : base(message)
        {
        }

        
        public CmdletInvocationException(string message,
                                         Exception innerException)
            : base(message, innerException)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected CmdletInvocationException(SerializationInfo info,
                                            StreamingContext context)
        {
            throw new NotSupportedException();
        }        
        #endregion Serialization
        #endregion ctor

        #region Properties
        
        public override ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    "CmdletInvocationException",
                    ErrorCategory.NotSpecified,
                    null);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord = null;

        #endregion Properties
    }
    #endregion CmdletInvocationException

    #region CmdletProviderInvocationException
    
    public class CmdletProviderInvocationException : CmdletInvocationException
    {
        #region ctor
        
        internal CmdletProviderInvocationException(
                    ProviderInvocationException innerException,
                    InvocationInfo myInvocation)
            : base(GetInnerException(innerException), myInvocation)
        {
            ArgumentNullException.ThrowIfNull(innerException);

            _providerInvocationException = innerException;
        }

        
        public CmdletProviderInvocationException()
            : base()
        {
        }

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected CmdletProviderInvocationException(SerializationInfo info,
                                                    StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public CmdletProviderInvocationException(string message)
            : base(message)
        {
        }

        
        public CmdletProviderInvocationException(string message,
                                                 Exception innerException)
            : base(message, innerException)
        {
            _providerInvocationException = innerException as ProviderInvocationException;
        }
        #endregion ctor

        #region Properties
        
        public ProviderInvocationException ProviderInvocationException
        {
            get
            {
                return _providerInvocationException;
            }
        }

        [NonSerialized]
        private readonly ProviderInvocationException _providerInvocationException;

        
        public ProviderInfo ProviderInfo
        {
            get
            {
                return _providerInvocationException?.ProviderInfo;
            }
        }

        #endregion Properties

        #region Internal
        private static Exception GetInnerException(Exception e)
        {
            return e?.InnerException;
        }
        #endregion Internal
    }
    #endregion CmdletProviderInvocationException

    #region PipelineStoppedException
    
    public class PipelineStoppedException : RuntimeException
    {
        #region ctor
        
        public PipelineStoppedException()
            : base(GetErrorText.PipelineStoppedException)
        {
            SetErrorId("PipelineStopped");
            SetErrorCategory(ErrorCategory.OperationStopped);
        }

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PipelineStoppedException(SerializationInfo info,
                                           StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public PipelineStoppedException(string message)
            : base(message)
        {
        }

        
        public PipelineStoppedException(string message,
                                        Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctor
    }
    #endregion PipelineStoppedException

    #region PipelineClosedException
    
    public class PipelineClosedException : RuntimeException
    {
        #region ctor
        
        public PipelineClosedException()
            : base()
        {
        }

        
        public PipelineClosedException(string message)
            : base(message)
        {
        }

        
        public PipelineClosedException(string message,
                                       Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PipelineClosedException(SerializationInfo info,
                                          StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization
    }
    #endregion PipelineClosedException

    #region ActionPreferenceStopException
    
    public class ActionPreferenceStopException : RuntimeException
    {
        #region ctor
        
        public ActionPreferenceStopException()
            : this(GetErrorText.ActionPreferenceStop)
        {
        }

        
        internal ActionPreferenceStopException(ErrorRecord error)
            : this(RetrieveMessage(error))
        {
            ArgumentNullException.ThrowIfNull(error);

            _errorRecord = error;
        }

        
        internal ActionPreferenceStopException(InvocationInfo invocationInfo, string message)
            : this(message)
        {
            base.ErrorRecord.SetInvocationInfo(invocationInfo);
        }

        
        internal ActionPreferenceStopException(InvocationInfo invocationInfo,
                                               ErrorRecord errorRecord,
                                               string message)
            : this(invocationInfo, message)
        {
            ArgumentNullException.ThrowIfNull(errorRecord);

            _errorRecord = errorRecord;
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ActionPreferenceStopException(SerializationInfo info,
                                                StreamingContext context)
        {
            throw new NotSupportedException();
        }        
        #endregion Serialization

        
        public ActionPreferenceStopException(string message)
            : base(message)
        {
            SetErrorCategory(ErrorCategory.OperationStopped);
            SetErrorId("ActionPreferenceStop");

            // fix for BUG: Windows Out Of Band Releases: 906263 and 906264
            // The interpreter prompt CommandBaseStrings:InquireHalt
            // should be suppressed when this flag is set.  This will be set
            // when this prompt has already occurred and Break was chosen,
            // or for ActionPreferenceStopException in all cases.
            this.SuppressPromptInInterpreter = true;
        }

        
        public ActionPreferenceStopException(string message,
                                             Exception innerException)
            : base(message, innerException)
        {
            SetErrorCategory(ErrorCategory.OperationStopped);
            SetErrorId("ActionPreferenceStop");

            // fix for BUG: Windows Out Of Band Releases: 906263 and 906264
            // The interpreter prompt CommandBaseStrings:InquireHalt
            // should be suppressed when this flag is set.  This will be set
            // when this prompt has already occurred and Break was chosen,
            // or for ActionPreferenceStopException in all cases.
            this.SuppressPromptInInterpreter = true;
        }
        #endregion ctor

        #region Properties
        
        public override ErrorRecord ErrorRecord
        {
            get { return _errorRecord ?? base.ErrorRecord; }
        }

        private readonly ErrorRecord _errorRecord = null;
        #endregion Properties
    }
    #endregion ActionPreferenceStopException

    #region ParentContainsErrorRecordException
    
    public class ParentContainsErrorRecordException : SystemException
    {
        #region Constructors
        
#pragma warning disable 56506

        // BUGBUG : We should check whether wrapperException is not null.
        // Please remove the #pragma warning when this is fixed.
        public ParentContainsErrorRecordException(Exception wrapperException)
        {
            _wrapperException = wrapperException;
        }

#pragma warning restore 56506

        
        public ParentContainsErrorRecordException(string message)
        {
            _message = message;
        }

        
        public ParentContainsErrorRecordException()
            : base()
        {
        }

        
        public ParentContainsErrorRecordException(string message,
                                                  Exception innerException)
            : base(message, innerException)
        {
            _message = message;
        }
        #endregion Constructors

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ParentContainsErrorRecordException(
            SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization
        
        public override string Message
        {
            get
            {
                return _message ??= (_wrapperException != null) ? _wrapperException.Message : string.Empty;
            }
        }

        #region Private Data

        private readonly Exception _wrapperException;
        private string _message;

        #endregion
    }
    #endregion ParentContainsErrorRecordException

    #region RedirectedException
    
    public class RedirectedException : RuntimeException
    {
        #region constructors
        
        public RedirectedException()
            : base()
        {
            SetErrorId("RedirectedException");
            SetErrorCategory(ErrorCategory.NotSpecified);
        }

        
        public RedirectedException(string message)
            : base(message)
        {
            SetErrorId("RedirectedException");
            SetErrorCategory(ErrorCategory.NotSpecified);
        }

        
        public RedirectedException(string message,
                                   Exception innerException)
            : base(message, innerException)
        {
            SetErrorId("RedirectedException");
            SetErrorCategory(ErrorCategory.NotSpecified);
        }

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected RedirectedException(SerializationInfo info,
                                      StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion constructors
    }
    #endregion RedirectedException

    #region ScriptCallDepthException
    
    public class ScriptCallDepthException : SystemException, IContainsErrorRecord
    {
        #region ctor

        
        public ScriptCallDepthException()
            : base(GetErrorText.ScriptCallDepthException)
        {
        }

        
        public ScriptCallDepthException(string message)
            : base(message)
        {
        }

        
        public ScriptCallDepthException(string message,
                                        Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ScriptCallDepthException(SerializationInfo info,
                                           StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        #region properties
        
        public ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    "CallDepthOverflow",
                    ErrorCategory.InvalidOperation,
                    CallDepth);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord = null;

        
        public int CallDepth
        {
            get { return 0; }
        }
        #endregion properties
    }
    #endregion ScriptCallDepthException

    #region PipelineDepthException
    
    public class PipelineDepthException : SystemException, IContainsErrorRecord
    {
        #region ctor
        
        public PipelineDepthException()
            : base(GetErrorText.PipelineDepthException)
        {
        }

        
        public PipelineDepthException(string message)
            : base(message)
        {
        }

        
        public PipelineDepthException(string message,
                                        Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PipelineDepthException(SerializationInfo info,
                                           StreamingContext context)            
        {
            throw new NotSupportedException();            
        }
        #endregion Serialization

        #region properties
        
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    "CallDepthOverflow",
                    ErrorCategory.InvalidOperation,
                    CallDepth);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord = null;

        
        public int CallDepth
        {
            get { return 0; }
        }
        #endregion properties
    }
    #endregion

    #region HaltCommandException
    
    public class HaltCommandException : SystemException
    {
        #region ctor
        
        public HaltCommandException()
            : base(StringUtil.Format(AutomationExceptions.HaltCommandException))
        {
        }

        
        public HaltCommandException(string message)
            : base(message)
        {
        }

        
        public HaltCommandException(string message,
                                    Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctor

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected HaltCommandException(SerializationInfo info,
                                       StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization
    }
    #endregion HaltCommandException
}

#pragma warning restore 56506
