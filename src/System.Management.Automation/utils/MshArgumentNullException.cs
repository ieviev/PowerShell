// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class PSArgumentNullException
            : ArgumentNullException, IContainsErrorRecord
    {
        #region ctor
        
        public PSArgumentNullException()
            : base()
        {
        }

        
        public PSArgumentNullException(string paramName)
            : base(paramName)
        {
        }

        
        public PSArgumentNullException(string message, Exception innerException)
            : base(message, innerException)
        {
            _message = message;
        }

        
        public PSArgumentNullException(string paramName, string message)
            : base(paramName, message)
        {
            _message = message;
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PSArgumentNullException(SerializationInfo info,
                           StreamingContext context)
        {
            throw new NotSupportedException();
        }        
        #endregion Serialization
        #endregion ctor

        
        public ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    _errorId,
                    ErrorCategory.InvalidArgument,
                    null);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;
        private readonly string _errorId = "ArgumentNull";

        
        public override string Message
        {
            get { return string.IsNullOrEmpty(_message) ? base.Message : _message; }
        }

        private readonly string _message;
    }
}
