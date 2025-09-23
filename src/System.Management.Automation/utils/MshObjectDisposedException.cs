// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class PSObjectDisposedException
            : ObjectDisposedException, IContainsErrorRecord
    {
        #region ctor
        
        public PSObjectDisposedException(string objectName)
            : base(objectName)
        {
        }

        
        public PSObjectDisposedException(string objectName, string message)
                : base(objectName, message)
        {
        }

        
        public PSObjectDisposedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PSObjectDisposedException(SerializationInfo info, StreamingContext context) : base(info, context)
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
                    ErrorCategory.InvalidOperation,
                    null);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;
        private readonly string _errorId = "ObjectDisposed";
    }
}
