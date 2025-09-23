// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;
using System.Runtime.Serialization;

namespace System.Management.Automation.Host
{
    
    public
    class HostException : RuntimeException
    {
        #region ctors
        
        public
        HostException()
            : base(StringUtil.Format(HostInterfaceExceptionsStrings.DefaultCtorMessageTemplate, typeof(HostException).FullName))
        {
            SetDefaultErrorRecord();
        }

        
        public
        HostException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        
        public
        HostException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        
        public
        HostException(
            string message,
            Exception innerException,
            string errorId,
            ErrorCategory errorCategory)
            : base(message, innerException)
        {
            SetErrorId(errorId);
            SetErrorCategory(errorCategory);
        }

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected
        HostException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion
        #region private
        private void SetDefaultErrorRecord()
        {
            SetErrorCategory(ErrorCategory.ResourceUnavailable);
            SetErrorId(typeof(HostException).FullName);
        }
        #endregion

    }

    
    public
    class PromptingException : HostException
    {
        #region ctors
        
        public
        PromptingException()
            : base(StringUtil.Format(HostInterfaceExceptionsStrings.DefaultCtorMessageTemplate, typeof(PromptingException).FullName))
        {
            SetDefaultErrorRecord();
        }

        
        public
        PromptingException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        
        public
        PromptingException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        
        public
        PromptingException(
            string message,
            Exception innerException,
            string errorId,
            ErrorCategory errorCategory)
            : base(message, innerException, errorId, errorCategory)
        {
        }

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected
        PromptingException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region private
        private void SetDefaultErrorRecord()
        {
            SetErrorCategory(ErrorCategory.ResourceUnavailable);
            SetErrorId(typeof(PromptingException).FullName);
        }
        #endregion
    }
}
