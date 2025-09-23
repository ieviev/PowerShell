// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Internal;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class MetadataException : RuntimeException
    {
        internal const string MetadataMemberInitialization = "MetadataMemberInitialization";
        internal const string BaseName = "Metadata";

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected MetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public MetadataException() : base(typeof(MetadataException).FullName)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
        }

        
        /// <param name="message">The exception's message.</param>
        public MetadataException(string message) : base(message)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
        }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public MetadataException(string message, Exception innerException) : base(message, innerException)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
        }

        internal MetadataException(
            string errorId,
            Exception innerException,
            string resourceStr,
            params object[] arguments)
            : base(
                  StringUtil.Format(resourceStr, arguments),
                  innerException)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
            SetErrorId(errorId);
        }
    }

    
    public class ValidationMetadataException : MetadataException
    {
        internal const string ValidateRangeElementType = "ValidateRangeElementType";
        internal const string ValidateRangePositiveFailure = "ValidateRangePositiveFailure";
        internal const string ValidateRangeNonNegativeFailure = "ValidateRangeNonNegativeFailure";
        internal const string ValidateRangeNegativeFailure = "ValidateRangeNegativeFailure";
        internal const string ValidateRangeNonPositiveFailure = "ValidateRangeNonPositiveFailure";
        internal const string ValidateRangeMinRangeMaxRangeType = "ValidateRangeMinRangeMaxRangeType";
        internal const string ValidateRangeNotIComparable = "ValidateRangeNotIComparable";
        internal const string ValidateRangeMaxRangeSmallerThanMinRange = "ValidateRangeMaxRangeSmallerThanMinRange";
        internal const string ValidateRangeGreaterThanMaxRangeFailure = "ValidateRangeGreaterThanMaxRangeFailure";
        internal const string ValidateRangeSmallerThanMinRangeFailure = "ValidateRangeSmallerThanMinRangeFailure";

        internal const string ValidateFailureResult = "ValidateFailureResult";

        internal const string ValidatePatternFailure = "ValidatePatternFailure";
        internal const string ValidateScriptFailure = "ValidateScriptFailure";

        internal const string ValidateCountNotInArray = "ValidateCountNotInArray";
        internal const string ValidateCountMaxLengthSmallerThanMinLength = "ValidateCountMaxLengthSmallerThanMinLength";
        internal const string ValidateCountMinLengthFailure = "ValidateCountMinLengthFailure";
        internal const string ValidateCountMaxLengthFailure = "ValidateCountMaxLengthFailure";

        internal const string ValidateLengthMaxLengthSmallerThanMinLength = "ValidateLengthMaxLengthSmallerThanMinLength";
        internal const string ValidateLengthNotString = "ValidateLengthNotString";
        internal const string ValidateLengthMinLengthFailure = "ValidateLengthMinLengthFailure";
        internal const string ValidateLengthMaxLengthFailure = "ValidateLengthMaxLengthFailure";
        internal const string ValidateSetFailure = "ValidateSetFailure";
        internal const string ValidateVersionFailure = "ValidateVersionFailure";
        internal const string InvalidValueFailure = "InvalidValueFailure";

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ValidationMetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public ValidationMetadataException() : base(typeof(ValidationMetadataException).FullName) { }
        
        /// <param name="message">The exception's message.</param>
        public ValidationMetadataException(string message) : this(message, false) { }
        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public ValidationMetadataException(string message, Exception innerException) : base(message, innerException) { }

        internal ValidationMetadataException(
            string errorId,
            Exception innerException,
            string resourceStr,
            params object[] arguments)
            : base(errorId, innerException, resourceStr, arguments)
        {
        }

        
        /// <param name="message">
        /// The error message</param>
        /// <param name="swallowException">
        /// Indicate whether to swallow this exception in positional binding phase
        /// </param>
        internal ValidationMetadataException(string message, bool swallowException) : base(message)
        {
            _swallowException = swallowException;
        }

        
        /// <remarks>
        /// This property is only used internally in the positional binding phase
        /// </remarks>
        internal bool SwallowException
        {
            get { return _swallowException; }
        }

        private readonly bool _swallowException = false;
    }

    
    public class ArgumentTransformationMetadataException : MetadataException
    {
        internal const string ArgumentTransformationArgumentsShouldBeStrings = "ArgumentTransformationArgumentsShouldBeStrings";

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ArgumentTransformationMetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public ArgumentTransformationMetadataException()
            : base(typeof(ArgumentTransformationMetadataException).FullName) { }

        
        /// <param name="message">The exception's message.</param>
        public ArgumentTransformationMetadataException(string message)
            : base(message) { }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public ArgumentTransformationMetadataException(string message, Exception innerException)
            : base(message, innerException) { }

        internal ArgumentTransformationMetadataException(
            string errorId,
            Exception innerException,
            string resourceStr,
            params object[] arguments)
            : base(errorId, innerException, resourceStr, arguments)
        {
        }
    }

    
    public class ParsingMetadataException : MetadataException
    {
        internal const string ParsingTooManyParameterSets = "ParsingTooManyParameterSets";

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ParsingMetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public ParsingMetadataException()
            : base(typeof(ParsingMetadataException).FullName) { }

        
        /// <param name="message">The exception's message.</param>
        public ParsingMetadataException(string message)
            : base(message) { }

        
        /// <param name="message">The exception's message.</param>
        /// <param name="innerException">The exception's inner exception.</param>
        public ParsingMetadataException(string message, Exception innerException)
            : base(message, innerException) { }

        internal ParsingMetadataException(
            string errorId,
            Exception innerException,
            string resourceStr,
            params object[] arguments)
            : base(errorId, innerException, resourceStr, arguments)
        {
        }
    }
}
