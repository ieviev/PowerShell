// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class PSNotImplementedException
            : NotImplementedException, IContainsErrorRecord
    {
        #region ctor
        
        public PSNotImplementedException()
            : base()
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PSNotImplementedException(SerializationInfo info,
                                              StreamingContext context)
        {
            throw new NotSupportedException();
        }        
        #endregion Serialization

        
        public PSNotImplementedException(string message)
            : base(message)
        {
        }

        
        public PSNotImplementedException(string message,
                        Exception innerException)
                : base(message, innerException)
        {
        }
        #endregion ctor

        
        public ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    _errorId,
                    ErrorCategory.NotImplemented,
                    null);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;
        private readonly string _errorId = "NotImplemented";
    }
}
