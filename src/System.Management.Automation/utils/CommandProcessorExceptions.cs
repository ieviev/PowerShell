// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class ApplicationFailedException : RuntimeException
    {
        #region private
        private const string errorIdString = "NativeCommandFailed";
        #endregion

        #region ctor

        #region Serialization
        
        /// <param name="info">The serialization information to use when initializing this object.</param>
        /// <param name="context">The streaming context to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ApplicationFailedException(SerializationInfo info,
                           StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        
        /// <returns>Constructed object.</returns>
        public ApplicationFailedException() : base()
        {
            base.SetErrorId(errorIdString);
            base.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }

        
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        public ApplicationFailedException(string message) : base(message)
        {
            base.SetErrorId(errorIdString);
            base.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }

        
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <param name="errorId">The errorId to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        internal ApplicationFailedException(string message, string errorId) : base(message)
        {
            base.SetErrorId(errorId);
            base.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }

        
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <param name="errorId">The errorId to use when initializing this object.</param>
        /// <param name="innerException">The inner exception to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        internal ApplicationFailedException(string message, string errorId, Exception innerException)
            : base(message, innerException)
        {
            base.SetErrorId(errorId);
            base.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }

        
        /// <param name="message">The error message to use when initializing this object.</param>
        /// <param name="innerException">The inner exception to use when initializing this object.</param>
        /// <returns>Constructed object.</returns>
        public ApplicationFailedException(string message,
                        Exception innerException)
                : base(message, innerException)
        {
            base.SetErrorId(errorIdString);
            base.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }
        #endregion ctor
    }
}
