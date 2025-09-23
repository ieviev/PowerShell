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

        
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public
        HostException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the <paramref name="innerException"/>
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        public
        HostException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the <paramref name="innerException"/>
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        /// <param name="errorId">
        /// The string that should uniquely identifies the situation where the exception is thrown.
        /// The string should not contain white space.
        /// </param>
        /// <param name="errorCategory">
        /// The ErrorCategory into which this exception situation falls
        /// </param>
        /// <remarks>
        /// Intentionally public, third-party hosts can call this
        /// </remarks>
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

        
        /// <param name="info">
        /// The object that holds the serialized object data.
        /// </param>
        /// <param name="context">
        /// The contextual information about the source or destination.
        /// </param>
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

        
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public
        PromptingException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the <paramref name="innerException"/>
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        public
        PromptingException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the <paramref name="innerException"/>
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        /// <param name="errorId">
        /// The string that should uniquely identifies the situation where the exception is thrown.
        /// The string should not contain white space.
        /// </param>
        /// <param name="errorCategory">
        /// The ErrorCategory into which this exception situation falls
        /// </param>
        /// <remarks>
        /// Intentionally public, third-party hosts can call this
        /// </remarks>
        public
        PromptingException(
            string message,
            Exception innerException,
            string errorId,
            ErrorCategory errorCategory)
            : base(message, innerException, errorId, errorCategory)
        {
        }

        
        /// <param name="info">
        /// The object that holds the serialized object data.
        /// </param>
        /// <param name="context">
        /// The contextual information about the source or destination.
        /// </param>
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
