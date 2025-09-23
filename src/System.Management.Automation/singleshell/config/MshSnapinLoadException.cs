// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Management.Automation.Runspaces
{
    
    /// 
    public class PSSnapInException : RuntimeException
    {
        
        /// <param name="PSSnapin">PSSnapin for the exception.</param>
        /// <param name="message">Message with load failure detail.</param>
        internal PSSnapInException(string PSSnapin, string message)
            : base()
        {
            _PSSnapin = PSSnapin;
            _reason = message;
            CreateErrorRecord();
        }

        
        /// <param name="PSSnapin">PSSnapin for the exception.</param>
        /// <param name="message">Message with load failure detail.</param>
        /// <param name="warning">Whether this is just a warning for PSSnapin load.</param>
        internal PSSnapInException(string PSSnapin, string message, bool warning)
            : base()
        {
            _PSSnapin = PSSnapin;
            _reason = message;
            _warning = warning;
            CreateErrorRecord();
        }

        
        /// <param name="PSSnapin">PSSnapin for the exception.</param>
        /// <param name="message">Message with load failure detail.</param>
        /// <param name="exception">Exception for PSSnapin load failure.</param>
        internal PSSnapInException(string PSSnapin, string message, Exception exception)
            : base(message, exception)
        {
            _PSSnapin = PSSnapin;
            _reason = message;
            CreateErrorRecord();
        }

        
        public PSSnapInException() : base()
        {
        }

        
        /// <param name="message">Error message.</param>
        public PSSnapInException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public PSSnapInException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        
        private void CreateErrorRecord()
        {
            // if _PSSnapin or _reason is empty, this exception is created using default
            // constructor. Don't create the error record since there is
            // no useful information anyway.
            if (!string.IsNullOrEmpty(_PSSnapin) && !string.IsNullOrEmpty(_reason))
            {
                Assembly currentAssembly = typeof(PSSnapInException).Assembly;

                if (_warning)
                {
                    _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "PSSnapInLoadWarning", ErrorCategory.ResourceUnavailable, null);
                    _errorRecord.ErrorDetails = new ErrorDetails(string.Format(ConsoleInfoErrorStrings.PSSnapInLoadWarning, _PSSnapin, _reason));
                }
                else
                {
                    _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "PSSnapInLoadFailure", ErrorCategory.ResourceUnavailable, null);
                    _errorRecord.ErrorDetails = new ErrorDetails(string.Format(ConsoleInfoErrorStrings.PSSnapInLoadFailure, _PSSnapin, _reason));
                }
            }
        }

        private readonly bool _warning = false;

        private ErrorRecord _errorRecord;
        private bool _isErrorRecordOriginallyNull;

        
        /// 
        public override ErrorRecord ErrorRecord
        {
            get
            {
                if (_errorRecord == null)
                {
                    _isErrorRecordOriginallyNull = true;
                    _errorRecord = new ErrorRecord(
                        new ParentContainsErrorRecordException(this),
                        "PSSnapInException",
                        ErrorCategory.NotSpecified,
                        null);
                }

                return _errorRecord;
            }
        }

        private readonly string _PSSnapin = string.Empty;
        private readonly string _reason = string.Empty;

        
        public override string Message
        {
            get
            {
                if (_errorRecord != null && !_isErrorRecordOriginallyNull)
                {
                    return _errorRecord.ToString();
                }

                return base.Message;
            }
        }

        #region Serialization

        
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected PSSnapInException(SerializationInfo info,
                                        StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Serialization
    }
}
