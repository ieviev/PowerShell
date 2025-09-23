// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class PSSecurityException : RuntimeException
    {
        #region ctor
        
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

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PSSecurityException(SerializationInfo info,
                           StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
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

        
        public override string Message
        {
            get { return _message; }
        }

        private readonly string _message;
    }
}
