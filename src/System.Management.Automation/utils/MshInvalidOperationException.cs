// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class PSInvalidOperationException
            : InvalidOperationException, IContainsErrorRecord
    {
        #region ctor
        
        public PSInvalidOperationException()
            : base()
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PSInvalidOperationException(SerializationInfo info,
                           StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        
        public PSInvalidOperationException(string message)
            : base(message)
        {
        }

        
        public PSInvalidOperationException(string message,
                                            Exception innerException)
                : base(message, innerException)
        {
        }

        
        internal PSInvalidOperationException(string message, Exception innerException, string errorId, ErrorCategory errorCategory, object target)
            : base(message, innerException)
        {
            _errorId = errorId;
            _errorCategory = errorCategory;
            _target = target;
        }
        #endregion ctor

        
        public ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    _errorId,
                    _errorCategory,
                    _target);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;
        private string _errorId = "InvalidOperation";

        internal void SetErrorId(string errorId)
        {
            _errorId = errorId;
        }

        private readonly ErrorCategory _errorCategory = ErrorCategory.InvalidOperation;
        private readonly object _target = null;
    }
}
