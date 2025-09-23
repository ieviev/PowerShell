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
    
    /// <remarks>
    /// InnerException is the error which the cmdlet hit.
    /// </remarks>
    public class CmdletInvocationException : RuntimeException
    {
        #region ctor
        
        /// <param name="errorRecord"></param>
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

        
        /// <param name="innerException">Wrapped exception.</param>
        /// <param name="invocationInfo">
        /// identity of cmdlet, null is unknown
        /// </param>
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

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public CmdletInvocationException(string message)
            : base(message)
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public CmdletInvocationException(string message,
                                         Exception innerException)
            : base(message, innerException)
        {
        }

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected CmdletInvocationException(SerializationInfo info,
                                            StreamingContext context)
        {
            throw new NotSupportedException();
        }        
        #endregion Serialization
        #endregion ctor

        #region Properties
        
        /// <value>never null</value>
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
        
        /// <param name="innerException">Wrapped exception.</param>
        /// <param name="myInvocation">
        /// identity of cmdlet, null is unknown
        /// </param>
        /// <returns>Constructed object.</returns>
        internal CmdletProviderInvocationException(
                    ProviderInvocationException innerException,
                    InvocationInfo myInvocation)
            : base(GetInnerException(innerException), myInvocation)
        {
            ArgumentNullException.ThrowIfNull(innerException);

            _providerInvocationException = innerException;
        }

        
        /// <returns>Constructed object.</returns>
        public CmdletProviderInvocationException()
            : base()
        {
        }

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected CmdletProviderInvocationException(SerializationInfo info,
                                                    StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public CmdletProviderInvocationException(string message)
            : base(message)
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public CmdletProviderInvocationException(string message,
                                                 Exception innerException)
            : base(message, innerException)
        {
            _providerInvocationException = innerException as ProviderInvocationException;
        }
        #endregion ctor

        #region Properties
        
        /// <value>ProviderInvocationException</value>
        public ProviderInvocationException ProviderInvocationException
        {
            get
            {
                return _providerInvocationException;
            }
        }

        [NonSerialized]
        private readonly ProviderInvocationException _providerInvocationException;

        
        /// <value>may be null</value>
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
    
    /// <remarks>
    /// When reported as the result of a command, PipelineStoppedException
    /// indicates that the command was stopped asynchronously, either by the
    /// user hitting CTRL-C, or by a call to
    /// <see cref="System.Management.Automation.Runspaces.Pipeline.Stop"/>.
    ///
    /// When a cmdlet or provider sees this exception thrown from a PowerShell API such as
    ///     WriteObject(object)
    /// this means that the command was already stopped.  The cmdlet or provider
    /// should clean up and return.
    /// Catching this exception is optional; if the cmdlet or providers chooses not to
    /// handle PipelineStoppedException and instead allow it to propagate to the
    /// PowerShell Engine's call to ProcessRecord, the PowerShell Engine will handle it properly.
    /// </remarks>    
    public class PipelineStoppedException : RuntimeException
    {
        #region ctor
        
        /// <returns>Constructed object.</returns>
        public PipelineStoppedException()
            : base(GetErrorText.PipelineStoppedException)
        {
            SetErrorId("PipelineStopped");
            SetErrorCategory(ErrorCategory.OperationStopped);
        }

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PipelineStoppedException(SerializationInfo info,
                                           StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public PipelineStoppedException(string message)
            : base(message)
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public PipelineStoppedException(string message,
                                        Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctor
    }
    #endregion PipelineStoppedException

    #region PipelineClosedException
    
    /// <seealso cref="System.Management.Automation.Runspaces.Pipeline.Input"/>    
    public class PipelineClosedException : RuntimeException
    {
        #region ctor
        
        /// <returns>Constructed object.</returns>
        public PipelineClosedException()
            : base()
        {
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public PipelineClosedException(string message)
            : base(message)
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public PipelineClosedException(string message,
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
        protected PipelineClosedException(SerializationInfo info,
                                          StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization
    }
    #endregion PipelineClosedException

    #region ActionPreferenceStopException
    
    /// <remarks>
    /// For example, if $WarningPreference is "Stop", the command will fail with
    /// this error if a cmdlet calls WriteWarning.
    /// </remarks>    
    public class ActionPreferenceStopException : RuntimeException
    {
        #region ctor
        
        /// <returns>Constructed object.</returns>
        public ActionPreferenceStopException()
            : this(GetErrorText.ActionPreferenceStop)
        {
        }

        
        /// <param name="error">
        /// Non-terminating error which triggered the Stop
        /// </param>
        /// <returns>Constructed object.</returns>
        internal ActionPreferenceStopException(ErrorRecord error)
            : this(RetrieveMessage(error))
        {
            ArgumentNullException.ThrowIfNull(error);

            _errorRecord = error;
        }

        
        /// <param name="invocationInfo"></param>
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
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
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ActionPreferenceStopException(SerializationInfo info,
                                                StreamingContext context)
        {
            throw new NotSupportedException();
        }        
        #endregion Serialization

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
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

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
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
        
        /// <value>ErrorRecord</value>
        /// <remarks>
        /// If this error results from a non-terminating error being promoted to
        /// terminating due to -ErrorAction or $ErrorActionPreference, this is
        /// the non-terminating error.
        /// </remarks>
        public override ErrorRecord ErrorRecord
        {
            get { return _errorRecord ?? base.ErrorRecord; }
        }

        private readonly ErrorRecord _errorRecord = null;
        #endregion Properties
    }
    #endregion ActionPreferenceStopException

    #region ParentContainsErrorRecordException
    
    /// <remarks>
    /// We use this exception class
    /// so that there is not a recursive "containment" relationship
    /// between the PowerShell engine exception and its ErrorRecord.
    /// </remarks>
    public class ParentContainsErrorRecordException : SystemException
    {
        #region Constructors
        
        /// <returns>Constructed object.</returns>
        /// <remarks>
        /// I leave this non-standard constructor form public.
        /// </remarks>
#pragma warning disable 56506

        // BUGBUG : We should check whether wrapperException is not null.
        // Please remove the #pragma warning when this is fixed.
        public ParentContainsErrorRecordException(Exception wrapperException)
        {
            _wrapperException = wrapperException;
        }

#pragma warning restore 56506

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public ParentContainsErrorRecordException(string message)
        {
            _message = message;
        }

        
        /// <returns>Constructed object.</returns>
        public ParentContainsErrorRecordException()
            : base()
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public ParentContainsErrorRecordException(string message,
                                                  Exception innerException)
            : base(message, innerException)
        {
            _message = message;
        }
        #endregion Constructors

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Doesn't return.</returns>
        /// <exception cref="NotImplementedException">Always.</exception>
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
    
    /// <remarks>
    /// The redirected object is available as
    /// <see cref="System.Management.Automation.ErrorRecord.TargetObject"/>
    /// in the ErrorRecord which contains this exception.
    /// </remarks>    
    public class RedirectedException : RuntimeException
    {
        #region constructors
        
        /// <returns>Constructed object.</returns>
        public RedirectedException()
            : base()
        {
            SetErrorId("RedirectedException");
            SetErrorCategory(ErrorCategory.NotSpecified);
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public RedirectedException(string message)
            : base(message)
        {
            SetErrorId("RedirectedException");
            SetErrorCategory(ErrorCategory.NotSpecified);
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public RedirectedException(string message,
                                   Exception innerException)
            : base(message, innerException)
        {
            SetErrorId("RedirectedException");
            SetErrorCategory(ErrorCategory.NotSpecified);
        }

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
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
    
    /// <remarks>
    /// When one PowerShell command or script calls another, this creates an additional
    /// scope.  Some script expressions also create a scope.  PowerShell imposes a maximum
    /// call depth to prevent stack overflows.  The maximum call depth is configurable
    /// but generally high enough that scripts which are not deeply recursive
    /// should not have a problem.
    /// </remarks>    
    public class ScriptCallDepthException : SystemException, IContainsErrorRecord
    {
        #region ctor

        
        /// <returns>Constructed object.</returns>
        public ScriptCallDepthException()
            : base(GetErrorText.ScriptCallDepthException)
        {
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public ScriptCallDepthException(string message)
            : base(message)
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public ScriptCallDepthException(string message,
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
        protected ScriptCallDepthException(SerializationInfo info,
                                           StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        #region properties
        
        /// <value></value>
        /// <remarks>
        /// TargetObject is the offending call depth
        /// </remarks>
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
    
    /// <remarks>
    /// </remarks>
    public class PipelineDepthException : SystemException, IContainsErrorRecord
    {
        #region ctor
        
        /// <returns>Constructed object.</returns>
        public PipelineDepthException()
            : base(GetErrorText.PipelineDepthException)
        {
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public PipelineDepthException(string message)
            : base(message)
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public PipelineDepthException(string message,
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
        protected PipelineDepthException(SerializationInfo info,
                                           StreamingContext context)            
        {
            throw new NotSupportedException();            
        }
        #endregion Serialization

        #region properties
        
        /// <value></value>
        /// <remarks>
        /// TargetObject is the offending call depth
        /// </remarks>
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

        
        /// <value></value>
        public int CallDepth
        {
            get { return 0; }
        }
        #endregion properties
    }
    #endregion

    #region HaltCommandException
    
    /// <remarks>
    /// For example, "more" will throw HaltCommandException if the user hits "q".
    ///
    /// Only throw HaltCommandException from your implementation of ProcessRecord etc.
    ///
    /// Note that HaltCommandException does not define IContainsErrorRecord.
    /// This is because it is not reported to the user.
    /// </remarks>    
    public class HaltCommandException : SystemException
    {
        #region ctor
        
        /// <returns>Constructed object.</returns>
        public HaltCommandException()
            : base(StringUtil.Format(AutomationExceptions.HaltCommandException))
        {
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public HaltCommandException(string message)
            : base(message)
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public HaltCommandException(string message,
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
