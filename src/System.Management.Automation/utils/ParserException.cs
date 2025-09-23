// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Language;
using System.Runtime.Serialization;

namespace System.Management.Automation
{
    
    public class ParseException : RuntimeException
    {
        private const string errorIdString = "Parse";

        private readonly ParseError[] _errors;

        
        public ParseError[] Errors
        {
            get { return _errors; }
        }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected ParseException(SerializationInfo info,
                           StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Serialization

        #region ctor

        
        public ParseException() : base()
        {
            base.SetErrorId(errorIdString);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        
        public ParseException(string message) : base(message)
        {
            base.SetErrorId(errorIdString);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        
        internal ParseException(string message, string errorId) : base(message)
        {
            base.SetErrorId(errorId);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        
        internal ParseException(string message, string errorId, Exception innerException)
            : base(message, innerException)
        {
            base.SetErrorId(errorId);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        
        public ParseException(string message,
                        Exception innerException)
                : base(message, innerException)
        {
            base.SetErrorId(errorIdString);
            base.SetErrorCategory(ErrorCategory.ParserError);
        }

        
        public ParseException(ParseError[] errors)
        {
            if (errors is null || errors.Length == 0)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            _errors = errors;
            // Arbitrarily choose the first error message for the ErrorId.
            base.SetErrorId(_errors[0].ErrorId);
            base.SetErrorCategory(ErrorCategory.ParserError);

            if (errors[0].Extent != null)
                this.ErrorRecord.SetInvocationInfo(new InvocationInfo(null, errors[0].Extent));
        }

        #endregion ctor

        
        public override string Message
        {
            get
            {
                if (_errors == null)
                {
                    return base.Message;
                }

                // Report at most the first 10 errors
                var errorsToReport = (_errors.Length > 10)
                    ? _errors.Take(10).Select(static e => e.ToString()).Append(ParserStrings.TooManyErrors)
                    : _errors.Select(static e => e.ToString());

                return string.Join(Environment.NewLine + Environment.NewLine, errorsToReport);
            }
        }
    }

    
    public class IncompleteParseException
            : ParseException
    {
        #region private
        private const string errorIdString = "IncompleteParse";
        #endregion

        #region ctor

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected IncompleteParseException(SerializationInfo info,
                           StreamingContext context)
        {
            throw new NotSupportedException();
        }
        #endregion Serialization

        
        public IncompleteParseException() : base()
        {
            // Error category is set in base constructor
            base.SetErrorId(errorIdString);
        }

        
        public IncompleteParseException(string message) : base(message)
        {
            // Error category is set in base constructor
            base.SetErrorId(errorIdString);
        }

        
        internal IncompleteParseException(string message, string errorId) : base(message, errorId)
        {
            // Error category is set in base constructor
        }

        
        internal IncompleteParseException(string message, string errorId, Exception innerException)
            : base(message, errorId, innerException)
        {
            // Error category is set in base constructor
        }

        
        public IncompleteParseException(string message,
                        Exception innerException)
                : base(message, innerException)
        {
            // Error category is set in base constructor
            base.SetErrorId(errorIdString);
        }
        #endregion ctor
    }
}
