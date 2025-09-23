// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class PSArgumentException
            : ArgumentException, IContainsErrorRecord
    {
        #region ctor
        
        public PSArgumentException()
            : base()
        {
        }

        
        public PSArgumentException(string message)
            : base(message)
        {
        }

        
        public PSArgumentException(string message, string paramName)
                : base(message, paramName)
        {
            _message = message;
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PSArgumentException(SerializationInfo info,
                           StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Serialization

        
        public PSArgumentException(string message,
                                    Exception innerException)
                : base(message, innerException)
        {
            _message = message;
        }
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
        private readonly string _errorId = "Argument";

        
        public override string Message
        {
            get { return string.IsNullOrEmpty(_message) ? base.Message : _message; }
        }

        private readonly string _message;
    }
}
