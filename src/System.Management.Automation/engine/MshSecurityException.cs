// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class PSSecurityException : RuntimeException
    {
        #region ctor
        
        /// <returns>Constructed object.</returns>
        public PSSecurityException()
            : base()
        {
            _errorRecord = new ErrorRecord(
                new ParentContainsErrorRecordException(this),
                "UnauthorizedAccess",
                ErrorCategory.SecurityError,
                null);
            _errorRecord.ErrorDetails = new ErrorDetails(SessionStateStrings.CanNotRun);
            _message = _errorRecord.ErrorDetails.Message;
        }

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        /// <returns>Constructed object.</returns>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PSSecurityException(SerializationInfo info,
                           StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public PSSecurityException(string message)
            : base(message)
        {
            _message = message;
            _errorRecord = new ErrorRecord(
                new ParentContainsErrorRecordException(this),
                "UnauthorizedAccess",
                ErrorCategory.SecurityError,
                null);
            _errorRecord.ErrorDetails = new ErrorDetails(message);
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns>Constructed object.</returns>
        public PSSecurityException(string message,
                                Exception innerException)
            : base(message, innerException)
        {
            _errorRecord = new ErrorRecord(
                new ParentContainsErrorRecordException(this),
                "UnauthorizedAccess",
                ErrorCategory.SecurityError,
                null);
            _errorRecord.ErrorDetails = new ErrorDetails(message);
            _message = _errorRecord.ErrorDetails.Message;
        }
        #endregion ctor

        
        public override ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    "UnauthorizedAccess",
                    ErrorCategory.SecurityError,
                    null);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;

        
        /// <value></value>
        public override string Message
        {
            get { return _message; }
        }

        private readonly string _message;
    }
}
