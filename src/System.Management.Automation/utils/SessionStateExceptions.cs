// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class ProviderInvocationException : RuntimeException
    {
        #region Constructors
        
        public ProviderInvocationException() : base()
        {
        }

        
        /// <param name="info">
        /// serialization information
        /// </param>
        /// <param name="context">
        /// streaming context
        /// </param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ProviderInvocationException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        /// <param name="message">
        /// The message for the exception.
        /// </param>
        public ProviderInvocationException(string message)
            : base(message)
        {
            _message = message;
        }

        
        /// <param name="provider">
        /// Information about the provider to be used in formatting the message.
        /// </param>
        /// <param name="innerException">
        /// The inner exception for this exception.
        /// </param>
        internal ProviderInvocationException(ProviderInfo provider, Exception innerException)
            : base(RuntimeException.RetrieveMessage(innerException), innerException)
        {
            _message = base.Message;
            _providerInfo = provider;

            if (innerException is IContainsErrorRecord icer && icer.ErrorRecord != null)
            {
                _errorRecord = new ErrorRecord(icer.ErrorRecord, innerException);
            }
            else
            {
                _errorRecord = new ErrorRecord(
                    innerException,
                    "ErrorRecordNotSpecified",
                    ErrorCategory.InvalidOperation,
                    null);
            }
        }

        
        /// <param name="provider">
        /// Information about the provider to be used in formatting the message.
        /// </param>
        /// <param name="errorRecord">
        /// Detailed error information
        /// </param>
        internal ProviderInvocationException(ProviderInfo provider, ErrorRecord errorRecord)
            : base(RuntimeException.RetrieveMessage(errorRecord),
                    RuntimeException.RetrieveException(errorRecord))
        {
            ArgumentNullException.ThrowIfNull(errorRecord);

            _message = base.Message;
            _providerInfo = provider;
            _errorRecord = errorRecord;
        }

        
        /// <param name="message">
        /// The message for the exception.
        /// </param>
        /// <param name="innerException">
        /// The inner exception for this exception.
        /// </param>
        public ProviderInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
            _message = message;
        }

        
        /// <param name="errorId">
        /// This string will be used to construct the FullyQualifiedErrorId,
        /// which is a global identifier of the error condition.  Pass a
        /// non-empty string which is specific to this error condition in
        /// this context.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the message template string.
        /// </param>
        /// <param name="provider">
        /// The provider information used to format into the message.
        /// </param>
        /// <param name="path">
        /// The path that was being processed when the exception occurred.
        /// </param>
        /// <param name="innerException">
        /// The exception that was thrown by the provider.
        /// </param>
        internal ProviderInvocationException(
            string errorId,
            string resourceStr,
            ProviderInfo provider,
            string path,
            Exception innerException)
            : this(errorId, resourceStr, provider, path, innerException, true)
        {
        }

        
        /// <param name="errorId">
        /// This string will be used to construct the FullyQualifiedErrorId,
        /// which is a global identifier of the error condition.  Pass a
        /// non-empty string which is specific to this error condition in
        /// this context.
        /// </param>
        /// <param name="resourceStr">
        /// This is the message template string
        /// </param>
        /// <param name="provider">
        /// The provider information used to format into the message.
        /// </param>
        /// <param name="path">
        /// The path that was being processed when the exception occurred.
        /// </param>
        /// <param name="innerException">
        /// The exception that was thrown by the provider.
        /// </param>
        /// <param name="useInnerExceptionMessage">
        /// If true, the message from the inner exception will be used if the exception contains
        /// an ErrorRecord. If false, the error message retrieved using the errorId will be used.
        /// </param>
        internal ProviderInvocationException(
            string errorId,
            string resourceStr,
            ProviderInfo provider,
            string path,
            Exception innerException,
            bool useInnerExceptionMessage)
            : base(
                RetrieveMessage(errorId, resourceStr, provider, path, innerException),
                innerException)
        {
            _providerInfo = provider;

            _message = base.Message;

            Exception errorRecordException = null;
            if (useInnerExceptionMessage)
            {
                errorRecordException = innerException;
            }
            else
            {
                errorRecordException = new ParentContainsErrorRecordException(this);
            }

            if (innerException is IContainsErrorRecord icer && icer.ErrorRecord != null)
            {
                _errorRecord = new ErrorRecord(icer.ErrorRecord, errorRecordException);
            }
            else
            {
                _errorRecord = new ErrorRecord(
                    errorRecordException,
                    errorId,
                    ErrorCategory.InvalidOperation,
                    null);
            }
        }
        #endregion Constructors

        #region Properties
        
        public ProviderInfo ProviderInfo { get { return _providerInfo; } }

        [NonSerialized]
        internal ProviderInfo _providerInfo;

        
        public override ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    "ProviderInvocationException",
                    ErrorCategory.NotSpecified,
                    null);

                return _errorRecord;
            }
        }

        [NonSerialized]
        private ErrorRecord _errorRecord;
        #endregion Properties

        #region Private/Internal
        private static string RetrieveMessage(
            string errorId,
            string resourceStr,
            ProviderInfo provider,
            string path,
            Exception innerException)
        {
            if (innerException == null)
            {
                Diagnostics.Assert(false,
                "ProviderInvocationException.RetrieveMessage needs innerException");
                return string.Empty;
            }

            if (string.IsNullOrEmpty(errorId))
            {
                Diagnostics.Assert(false,
                "ProviderInvocationException.RetrieveMessage needs errorId");
                return RuntimeException.RetrieveMessage(innerException);
            }

            if (provider == null)
            {
                Diagnostics.Assert(false,
                "ProviderInvocationException.RetrieveMessage needs provider");
                return RuntimeException.RetrieveMessage(innerException);
            }

            string format = resourceStr;
            if (string.IsNullOrEmpty(format))
            {
                Diagnostics.Assert(false,
                "ProviderInvocationException.RetrieveMessage bad errorId " + errorId);
                return RuntimeException.RetrieveMessage(innerException);
            }

            string result = null;

            if (path == null)
            {
                result =
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        format,
                        provider.Name,
                        RuntimeException.RetrieveMessage(innerException));
            }
            else
            {
                result =
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        format,
                        provider.Name,
                        path,
                        RuntimeException.RetrieveMessage(innerException));
            }

            return result;
        }

        
        public override string Message
        {
            get { return (string.IsNullOrEmpty(_message)) ? base.Message : _message; }
        }

        [NonSerialized]
        private readonly string _message ;

        #endregion Private/Internal
    }

    
    public enum SessionStateCategory
    {
        
        Variable = 0,

        
        Alias = 1,

        
        Function = 2,

        
        Filter = 3,

        
        Drive = 4,

        
        CmdletProvider = 5,

        
        Scope = 6,

        
        Command = 7,

        
        Resource = 8,

        
        Cmdlet = 9,
    }

    
    public class SessionStateException : RuntimeException
    {
        #region ctor
        
        /// <param name="itemName">Name of session state object.</param>
        /// <param name="sessionStateCategory">Category of session state object.</param>
        /// <param name="resourceStr">This string is the message template string.</param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="errorCategory">ErrorRecord.CategoryInfo.Category.</param>
        /// <param name="messageArgs">
        /// Additional insertion strings used to construct the message.
        /// Note that itemName is always the first insertion string.
        /// </param>
        internal SessionStateException(
            string itemName,
            SessionStateCategory sessionStateCategory,
            string errorIdAndResourceId,
            string resourceStr,
            ErrorCategory errorCategory,
            params object[] messageArgs)
            : base(BuildMessage(itemName, resourceStr, messageArgs))
        {
            _itemName = itemName;
            _sessionStateCategory = sessionStateCategory;
            _errorId = errorIdAndResourceId;
            _errorCategory = errorCategory;
        }

        
        public SessionStateException()
            : base()
        {
        }

        
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        public SessionStateException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">
        /// The message used in the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public SessionStateException(string message,
                                     Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected SessionStateException(SerializationInfo info,
                                        StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #region Properties
        
        public override ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    _errorId,
                    _errorCategory,
                    _itemName);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;

        
        public string ItemName
        {
            get { return _itemName; }
        }

        private readonly string _itemName = string.Empty;

        
        public SessionStateCategory SessionStateCategory
        {
            get { return _sessionStateCategory; }
        }

        private readonly SessionStateCategory _sessionStateCategory = SessionStateCategory.Variable;
        #endregion Properties

        #region Private
        private readonly string _errorId = "SessionStateException";
        private readonly ErrorCategory _errorCategory = ErrorCategory.InvalidArgument;

        private static string BuildMessage(
            string itemName,
            string resourceStr,
            params object[] messageArgs)
        {
            object[] a;
            if (messageArgs != null && messageArgs.Length > 0)
            {
                a = new object[messageArgs.Length + 1];
                a[0] = itemName;
                messageArgs.CopyTo(a, 1);
            }
            else
            {
                a = new object[1];
                a[0] = itemName;
            }

            return StringUtil.Format(resourceStr, a);
        }
        #endregion Private
    }

    
    public class SessionStateUnauthorizedAccessException : SessionStateException
    {
        #region ctor
        
        /// <param name="itemName">
        /// The name of the session state object the error occurred on.
        /// </param>
        /// <param name="sessionStateCategory">
        /// The category of session state object.
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        internal SessionStateUnauthorizedAccessException(
            string itemName,
            SessionStateCategory sessionStateCategory,
            string errorIdAndResourceId,
            string resourceStr
            )
            : base(itemName, sessionStateCategory,
                    errorIdAndResourceId, resourceStr, ErrorCategory.WriteError)
        {
        }

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected SessionStateUnauthorizedAccessException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public SessionStateUnauthorizedAccessException()
            : base()
        {
        }

        
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        public SessionStateUnauthorizedAccessException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public SessionStateUnauthorizedAccessException(string message,
                                             Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor
    }

    
    public class ProviderNotFoundException : SessionStateException
    {
        #region ctor
        
        /// <param name="itemName">
        /// The name of provider that could not be found.
        /// </param>
        /// <param name="sessionStateCategory">
        /// The category of session state object
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the message template string
        /// </param>
        /// <param name="messageArgs">
        /// Additional arguments to build the message from.
        /// </param>
        internal ProviderNotFoundException(
            string itemName,
            SessionStateCategory sessionStateCategory,
            string errorIdAndResourceId,
            string resourceStr,
            params object[] messageArgs)
            : base(
                itemName,
                sessionStateCategory,
                errorIdAndResourceId,
                resourceStr,
                ErrorCategory.ObjectNotFound,
                messageArgs)
        {
        }

        
        public ProviderNotFoundException()
            : base()
        {
        }

        
        /// <param name="message">
        /// The messaged used by the exception.
        /// </param>
        public ProviderNotFoundException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public ProviderNotFoundException(string message,
                                         Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor
    }

    
    public class ProviderNameAmbiguousException : ProviderNotFoundException
    {
        #region ctor
        
        /// <param name="providerName">
        /// The name of provider that was ambiguous.
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the message template string
        /// </param>
        /// <param name="possibleMatches">
        /// The provider information for the providers that match the specified
        /// name.
        /// </param>
        /// <param name="messageArgs">
        /// Additional arguments to build the message from.
        /// </param>
        internal ProviderNameAmbiguousException(
            string providerName,
            string errorIdAndResourceId,
            string resourceStr,
            Collection<ProviderInfo> possibleMatches,
            params object[] messageArgs)
            : base(
                providerName,
                SessionStateCategory.CmdletProvider,
                errorIdAndResourceId,
                resourceStr,
                messageArgs)
        {
            _possibleMatches = new ReadOnlyCollection<ProviderInfo>(possibleMatches);
        }

        
        public ProviderNameAmbiguousException()
            : base()
        {
        }

        
        /// <param name="message">
        /// The messaged used by the exception.
        /// </param>
        public ProviderNameAmbiguousException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public ProviderNameAmbiguousException(string message,
                                         Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctor

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ProviderNameAmbiguousException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #region public properties

        
        public ReadOnlyCollection<ProviderInfo> PossibleMatches
        {
            get
            {
                return _possibleMatches;
            }
        }

        private readonly ReadOnlyCollection<ProviderInfo> _possibleMatches;

        #endregion public properties
    }

    
    public class DriveNotFoundException : SessionStateException
    {
        #region ctor
        
        /// <param name="itemName">
        /// The name of the drive that could not be found.
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the message template string
        /// </param>
        internal DriveNotFoundException(
            string itemName,
            string errorIdAndResourceId,
            string resourceStr
            )
            : base(itemName, SessionStateCategory.Drive,
                    errorIdAndResourceId, resourceStr, ErrorCategory.ObjectNotFound)
        {
        }

        
        public DriveNotFoundException()
            : base()
        {
        }

        
        /// <param name="message">
        /// The message that will be used by the exception.
        /// </param>
        public DriveNotFoundException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">
        /// The message that will be used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public DriveNotFoundException(string message,
                                      Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected DriveNotFoundException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }
    }

    
    public class ItemNotFoundException : SessionStateException
    {
        #region ctor
        
        /// <param name="path">
        /// The path that was not found.
        /// </param>
        /// <param name="errorIdAndResourceId">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        /// <param name="resourceStr">
        /// This string is the ErrorId passed to the ErrorRecord, and is also
        /// the resourceId used to look up the message template string in
        /// SessionStateStrings.txt.
        /// </param>
        internal ItemNotFoundException(
            string path,
            string errorIdAndResourceId,
            string resourceStr
            )
            : base(path, SessionStateCategory.Drive,
                    errorIdAndResourceId, resourceStr, ErrorCategory.ObjectNotFound)
        {
        }

        
        public ItemNotFoundException()
            : base()
        {
        }

        
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        public ItemNotFoundException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">
        /// The message used by the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that caused the error.
        /// </param>
        public ItemNotFoundException(string message,
                                      Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ItemNotFoundException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }
    }
}
