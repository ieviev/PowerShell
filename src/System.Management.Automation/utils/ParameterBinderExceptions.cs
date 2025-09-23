// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class ParameterBindingException : RuntimeException
    {
        #region Constructors

        #region Preferred constructors

        
        internal ParameterBindingException(
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(errorCategory, invocationInfo, errorPosition, errorId, null, null)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                throw PSTraceSource.NewArgumentException(nameof(resourceString));
            }

            if (string.IsNullOrEmpty(errorId))
            {
                throw PSTraceSource.NewArgumentException(nameof(errorId));
            }

            _invocationInfo = invocationInfo;

            if (_invocationInfo != null)
            {
                _commandName = invocationInfo.MyCommand.Name;
            }

            _parameterName = parameterName;
            _parameterType = parameterType;
            _typeSpecified = typeSpecified;

            if ((errorPosition == null) && (_invocationInfo != null))
            {
                errorPosition = invocationInfo.ScriptPosition;
            }

            if (errorPosition != null)
            {
                _line = errorPosition.StartLineNumber;
                _offset = errorPosition.StartColumnNumber;
            }

            _resourceString = resourceString;
            _errorId = errorId;

            if (args != null)
            {
                _args = args;
            }
        }

        
        internal ParameterBindingException(
            Exception innerException,
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(errorCategory, invocationInfo, errorPosition, errorId, null, innerException)
        {
            if (invocationInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(invocationInfo));
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw PSTraceSource.NewArgumentException(nameof(resourceString));
            }

            if (string.IsNullOrEmpty(errorId))
            {
                throw PSTraceSource.NewArgumentException(nameof(errorId));
            }

            _invocationInfo = invocationInfo;
            _commandName = invocationInfo.MyCommand.Name;
            _parameterName = parameterName;
            _parameterType = parameterType;
            _typeSpecified = typeSpecified;

            errorPosition ??= invocationInfo.ScriptPosition;

            if (errorPosition != null)
            {
                _line = errorPosition.StartLineNumber;
                _offset = errorPosition.StartColumnNumber;
            }

            _resourceString = resourceString;
            _errorId = errorId;

            if (args != null)
            {
                _args = args;
            }
        }

        
        internal ParameterBindingException(
            Exception innerException,
            ParameterBindingException pbex,
            string resourceString,
            params object[] args)
            : base(string.Empty, innerException)
        {
            if (pbex == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(pbex));
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw PSTraceSource.NewArgumentException(nameof(resourceString));
            }

            _invocationInfo = pbex.CommandInvocation;
            if (_invocationInfo != null)
            {
                _commandName = _invocationInfo.MyCommand.Name;
            }

            IScriptExtent errorPosition = null;
            if (_invocationInfo != null)
            {
                errorPosition = _invocationInfo.ScriptPosition;
            }

            _line = pbex.Line;
            _offset = pbex.Offset;

            _parameterName = pbex.ParameterName;
            _parameterType = pbex.ParameterType;
            _typeSpecified = pbex.TypeSpecified;
            _errorId = pbex.ErrorId;

            _resourceString = resourceString;

            if (args != null)
            {
                _args = args;
            }

            base.SetErrorCategory(pbex.ErrorRecord._category);
            base.SetErrorId(_errorId);
            if (_invocationInfo != null)
            {
                base.ErrorRecord.SetInvocationInfo(new InvocationInfo(_invocationInfo.MyCommand, errorPosition));
            }
        }
        #endregion Preferred constructors

        #region serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ParameterBindingException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion serialization

        #region Do Not Use

        
        public ParameterBindingException() : base() { }

        
        public ParameterBindingException(string message) : base(message) { _message = message; }

        
        public ParameterBindingException(
            string message,
            Exception innerException)
            : base(message, innerException)
        { _message = message; }

        #endregion Do Not Use
        #endregion Constructors

        #region Properties
        
        public override string Message
        {
            get { return _message ??= BuildMessage(); }
        }

        private string _message;

        
        public string ParameterName
        {
            get
            {
                return _parameterName;
            }
        }

        private readonly string _parameterName = string.Empty;

        
        public Type ParameterType
        {
            get
            {
                return _parameterType;
            }
        }

        private readonly Type _parameterType;

        
        public Type TypeSpecified
        {
            get
            {
                return _typeSpecified;
            }
        }

        private readonly Type _typeSpecified;

        
        public string ErrorId
        {
            get
            {
                return _errorId;
            }
        }

        private readonly string _errorId;

        
        public Int64 Line
        {
            get
            {
                return _line;
            }
        }

        private readonly Int64 _line = Int64.MinValue;

        
        public Int64 Offset
        {
            get
            {
                return _offset;
            }
        }

        private readonly Int64 _offset = Int64.MinValue;

        
        public InvocationInfo CommandInvocation
        {
            get
            {
                return _invocationInfo;
            }
        }

        private readonly InvocationInfo _invocationInfo;
        #endregion Properties

        #region private

        private readonly string _resourceString;
        private readonly object[] _args = Array.Empty<object>();
        private readonly string _commandName;

        private string BuildMessage()
        {
            object[] messageArgs = Array.Empty<object>();

            if (_args != null)
            {
                messageArgs = new object[_args.Length + 6];
                messageArgs[0] = _commandName;
                messageArgs[1] = _parameterName;
                messageArgs[2] = _parameterType;
                messageArgs[3] = _typeSpecified;
                messageArgs[4] = _line;
                messageArgs[5] = _offset;
                _args.CopyTo(messageArgs, 6);
            }

            string result = string.Empty;

            if (!string.IsNullOrEmpty(_resourceString))
            {
                result = StringUtil.Format(_resourceString, messageArgs);
            }

            return result;
        }

        #endregion Private
    }
    
    internal class ParameterBindingValidationException : ParameterBindingException
    {
        #region Preferred constructors

        
        internal ParameterBindingValidationException(
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }

        
        internal ParameterBindingValidationException(
            Exception innerException,
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                innerException,
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
            if (innerException is ValidationMetadataException validationException && validationException.SwallowException)
            {
                _swallowException = true;
            }
        }
        #endregion Preferred constructors

        #region serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ParameterBindingValidationException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion serialization

        #region Property

        
        internal bool SwallowException
        {
            get { return _swallowException; }
        }

        private readonly bool _swallowException = false;

        #endregion Property
    }

    internal class ParameterBindingArgumentTransformationException : ParameterBindingException
    {
        #region Preferred constructors

        
        internal ParameterBindingArgumentTransformationException(
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }

        
        internal ParameterBindingArgumentTransformationException(
            Exception innerException,
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                innerException,
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }
        #endregion Preferred constructors
        #region serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ParameterBindingArgumentTransformationException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion serialization
    }
    
    internal class ParameterBindingParameterDefaultValueException : ParameterBindingException
    {
        #region Preferred constructors

        
        internal ParameterBindingParameterDefaultValueException(
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }

        
        internal ParameterBindingParameterDefaultValueException(
            Exception innerException,
            ErrorCategory errorCategory,
            InvocationInfo invocationInfo,
            IScriptExtent errorPosition,
            string parameterName,
            Type parameterType,
            Type typeSpecified,
            string resourceString,
            string errorId,
            params object[] args)
            : base(
                innerException,
                errorCategory,
                invocationInfo,
                errorPosition,
                parameterName,
                parameterType,
                typeSpecified,
                resourceString,
                errorId,
                args)
        {
        }
        #endregion Preferred constructors

        #region serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ParameterBindingParameterDefaultValueException(
            SerializationInfo info,
            StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion serialization
    }
}
