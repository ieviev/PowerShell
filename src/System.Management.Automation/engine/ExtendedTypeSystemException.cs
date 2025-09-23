// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Management.Automation
{
    
    public class ExtendedTypeSystemException : RuntimeException
    {
        #region ctor
        
        public ExtendedTypeSystemException()
            : base(typeof(ExtendedTypeSystemException).FullName)
        {
        }

        
        public ExtendedTypeSystemException(string message)
            : base(message)
        {
        }

        
        public ExtendedTypeSystemException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        internal ExtendedTypeSystemException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(
                  StringUtil.Format(resourceString, arguments),
                  innerException)
        {
            SetErrorId(errorId);
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ExtendedTypeSystemException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        #endregion Serialization

        #endregion ctor

    }

    
    public class MethodException : ExtendedTypeSystemException
    {
        internal const string MethodArgumentCountExceptionMsg = "MethodArgumentCountException";
        internal const string MethodAmbiguousExceptionMsg = "MethodAmbiguousException";
        internal const string MethodArgumentConversionExceptionMsg = "MethodArgumentConversionException";
        internal const string NonRefArgumentToRefParameterMsg = "NonRefArgumentToRefParameter";
        internal const string RefArgumentToNonRefParameterMsg = "RefArgumentToNonRefParameter";

        #region ctor
        
        public MethodException()
            : base(typeof(MethodException).FullName)
        {
        }

        
        public MethodException(string message)
            : base(message)
        {
        }

        
        public MethodException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        internal MethodException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected MethodException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        #endregion ctor

    }

    
    public class MethodInvocationException : MethodException
    {
        internal const string MethodInvocationExceptionMsg = "MethodInvocationException";
        internal const string CopyToInvocationExceptionMsg = "CopyToInvocationException";
        internal const string WMIMethodInvocationException = "WMIMethodInvocationException";

        #region ctor
        
        public MethodInvocationException()
            : base(typeof(MethodInvocationException).FullName)
        {
        }

        
        public MethodInvocationException(string message)
            : base(message)
        {
        }

        
        public MethodInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        internal MethodInvocationException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected MethodInvocationException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        #endregion ctor

    }

    
    public class GetValueException : ExtendedTypeSystemException
    {
        internal const string GetWithoutGetterExceptionMsg = "GetWithoutGetterException";
        internal const string WriteOnlyProperty = "WriteOnlyProperty";
        #region ctor
        
        public GetValueException()
            : base(typeof(GetValueException).FullName)
        {
        }

        
        public GetValueException(string message)
            : base(message)
        {
        }

        
        public GetValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        internal GetValueException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected GetValueException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        #endregion ctor

    }

    
    public class PropertyNotFoundException : ExtendedTypeSystemException
    {
        #region ctor
        
        public PropertyNotFoundException()
            : base(typeof(PropertyNotFoundException).FullName)
        {
        }

        
        public PropertyNotFoundException(string message)
            : base(message)
        {
        }

        
        public PropertyNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        internal PropertyNotFoundException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PropertyNotFoundException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization
        #endregion ctor

    }

    
    public class GetValueInvocationException : GetValueException
    {
        internal const string ExceptionWhenGettingMsg = "ExceptionWhenGetting";

        #region ctor
        
        public GetValueInvocationException()
            : base(typeof(GetValueInvocationException).FullName)
        {
        }

        
        public GetValueInvocationException(string message)
            : base(message)
        {
        }

        
        public GetValueInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        internal GetValueInvocationException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected GetValueInvocationException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        #endregion ctor

    }

    
    public class SetValueException : ExtendedTypeSystemException
    {
        #region ctor
        
        public SetValueException()
            : base(typeof(SetValueException).FullName)
        {
        }

        
        public SetValueException(string message)
            : base(message)
        {
        }

        
        public SetValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        internal SetValueException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected SetValueException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        #endregion ctor

    }

    
    public class SetValueInvocationException : SetValueException
    {
        #region ctor
        
        public SetValueInvocationException()
            : base(typeof(SetValueInvocationException).FullName)
        {
        }

        
        public SetValueInvocationException(string message)
            : base(message)
        {
        }

        
        public SetValueInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        internal SetValueInvocationException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected SetValueInvocationException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        #endregion ctor

    }

    
    public class PSInvalidCastException : InvalidCastException, IContainsErrorRecord
    {
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PSInvalidCastException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public PSInvalidCastException()
            : base(typeof(PSInvalidCastException).FullName)
        {
        }
        
        public PSInvalidCastException(string message)
            : base(message)
        {
        }
        
        public PSInvalidCastException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal PSInvalidCastException(string errorId, string message, Exception innerException)
            : base(message, innerException)
        {
            _errorId = errorId;
        }

        internal PSInvalidCastException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : this(
                errorId, StringUtil.Format(resourceString, arguments),
                innerException)
        {
        }

        
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
        private readonly string _errorId = "PSInvalidCastException";
    }
}
