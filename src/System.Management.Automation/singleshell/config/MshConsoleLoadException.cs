// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace System.Management.Automation.Runspaces
{
    
    /// 
    public class PSConsoleLoadException : SystemException, IContainsErrorRecord
    {
        
        public PSConsoleLoadException() : base()
        {
        }

        
        /// <param name="message">Error message.</param>
        public PSConsoleLoadException(string message)
            : base(message)
        {
        }

        
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception.</param>
        public PSConsoleLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private ErrorRecord _errorRecord;

        
        /// 
        public ErrorRecord ErrorRecord
        {
            get
            {
                return _errorRecord;
            }
        }

        
        private void CreateErrorRecord()
        {
            StringBuilder sb = new StringBuilder();

            if (PSSnapInExceptions != null)
            {
                foreach (PSSnapInException e in PSSnapInExceptions)
                {
                    sb.Append('\n');
                    sb.Append(e.Message);
                }
            }

            _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "ConsoleLoadFailure", ErrorCategory.ResourceUnavailable, null);
        }

        private readonly Collection<PSSnapInException> _PSSnapInExceptions = new Collection<PSSnapInException>();

        internal Collection<PSSnapInException> PSSnapInExceptions
        {
            get
            {
                return _PSSnapInExceptions;
            }
        }

        
        public override string Message
        {
            get
            {
                if (_errorRecord != null)
                {
                    return _errorRecord.ToString();
                }
                else
                {
                    return base.Message;
                }
            }
        }
    }
}
