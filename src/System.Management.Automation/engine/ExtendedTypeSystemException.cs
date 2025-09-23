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

        
        /// <param name="message">The exception's message.</param>
        public ExtendedTypeSystemException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public ExtendedTypeSystemException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception, null for none.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
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
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
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

        
        /// <param name="message">The exception's message.</param>
        public MethodException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public MethodException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal MethodException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
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

        
        /// <param name="message">The exception's message.</param>
        public MethodInvocationException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public MethodInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal MethodInvocationException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
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

        
        /// <param name="message">The exception's message.</param>
        public GetValueException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public GetValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal GetValueException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
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

        
        /// <param name="message">The exception's message.</param>
        public PropertyNotFoundException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public PropertyNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal PropertyNotFoundException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
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

        
        /// <param name="message">The exception's message.</param>
        public GetValueInvocationException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public GetValueInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal GetValueInvocationException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
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

        
        /// <param name="message">The exception's message.</param>
        public SetValueException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public SetValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal SetValueException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
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

        
        /// <param name="message">The exception's message.</param>
        public SetValueInvocationException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public SetValueInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        /// <param name="errorId">String that uniquely identifies each thrown Exception.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="resourceString">Resource string.</param>
        /// <param name="arguments">Arguments to the resource string.</param>
        internal SetValueInvocationException(
            string errorId,
            Exception innerException,
            string resourceString,
            params object[] arguments)
            : base(errorId, innerException, resourceString, arguments)
        {
        }

        #region Serialization
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
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
        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PSInvalidCastException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public PSInvalidCastException()
            : base(typeof(PSInvalidCastException).FullName)
        {
        }
        
        /// <param name="message">The exception's message.</param>
        public PSInvalidCastException(string message)
            : base(message)
        {
        }
        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
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
