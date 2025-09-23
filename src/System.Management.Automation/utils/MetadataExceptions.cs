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

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected MetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public MetadataException() : base(typeof(MetadataException).FullName)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
        }

        
        public MetadataException(string message) : base(message)
        {
            SetErrorCategory(ErrorCategory.MetadataError);
        }

        
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

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ValidationMetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public ValidationMetadataException() : base(typeof(ValidationMetadataException).FullName) { }
        
        public ValidationMetadataException(string message) : this(message, false) { }
        
        public ValidationMetadataException(string message, Exception innerException) : base(message, innerException) { }

        internal ValidationMetadataException(
            string errorId,
            Exception innerException,
            string resourceStr,
            params object[] arguments)
            : base(errorId, innerException, resourceStr, arguments)
        {
        }

        
        internal ValidationMetadataException(string message, bool swallowException) : base(message)
        {
            _swallowException = swallowException;
        }

        
        internal bool SwallowException
        {
            get { return _swallowException; }
        }

        private readonly bool _swallowException = false;
    }

    
    public class ArgumentTransformationMetadataException : MetadataException
    {
        internal const string ArgumentTransformationArgumentsShouldBeStrings = "ArgumentTransformationArgumentsShouldBeStrings";

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ArgumentTransformationMetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public ArgumentTransformationMetadataException()
            : base(typeof(ArgumentTransformationMetadataException).FullName) { }

        
        public ArgumentTransformationMetadataException(string message)
            : base(message) { }

        
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

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ParsingMetadataException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        
        public ParsingMetadataException()
            : base(typeof(ParsingMetadataException).FullName) { }

        
        public ParsingMetadataException(string message)
            : base(message) { }

        
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
