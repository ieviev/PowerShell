// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class PSArgumentOutOfRangeException
            : ArgumentOutOfRangeException, IContainsErrorRecord
    {
        #region ctor
        
        public PSArgumentOutOfRangeException()
            : base()
        {
        }

        
        public PSArgumentOutOfRangeException(string paramName)
                : base(paramName)
        {
        }

        
        public PSArgumentOutOfRangeException(string paramName, object actualValue, string message)
                : base(paramName, actualValue, message)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PSArgumentOutOfRangeException(SerializationInfo info,
                           StreamingContext context)
        {
            throw new NotSupportedException();
        }
        
        #endregion Serialization

        
        public PSArgumentOutOfRangeException(string message,
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
                    ErrorCategory.InvalidArgument,
                    null);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;
        private readonly string _errorId = "ArgumentOutOfRange";
    }
}
