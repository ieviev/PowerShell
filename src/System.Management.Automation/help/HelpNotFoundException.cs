// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using System;
using System.Management.Automation;
using System.Runtime.Serialization;
using System.Reflection;
using System.Security.Permissions;

namespace Microsoft.PowerShell.Commands
{
    
    public class HelpNotFoundException : SystemException, IContainsErrorRecord
    {
        
        /// <param name="helpTopic">The help topic for which help is not found.</param>
        public HelpNotFoundException(string helpTopic)
            : base()
        {
            _helpTopic = helpTopic;
            CreateErrorRecord();
        }

        
        public HelpNotFoundException()
            : base()
        {
            CreateErrorRecord();
        }

        
        /// <param name="helpTopic">The help topic for which help is not found.</param>
        /// <param name="innerException">The inner exception.</param>
        public HelpNotFoundException(string helpTopic, Exception innerException)
            : base(
                  (innerException != null) ? innerException.Message : string.Empty,
                  innerException)
        {
            _helpTopic = helpTopic;
            CreateErrorRecord();
        }

        
        private void CreateErrorRecord()
        {
            string errMessage = string.Format(HelpErrors.HelpNotFound, _helpTopic);

            // Don't do ParentContainsErrorRecordException(this), as this causes recursion, and creates a
            // segmentation fault on Linux
            _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(errMessage), "HelpNotFound", ErrorCategory.ResourceUnavailable, null);
            _errorRecord.ErrorDetails = new ErrorDetails(typeof(HelpNotFoundException).Assembly, "HelpErrors", "HelpNotFound", _helpTopic);
        }

        private ErrorRecord _errorRecord;

        
        /// <value>ErrorRecord instance.</value>
        public ErrorRecord ErrorRecord
        {
            get
            {
                return _errorRecord;
            }
        }

        private readonly string _helpTopic = string.Empty;

        
        /// <value>Help topic.</value>
        public string HelpTopic
        {
            get
            {
                return _helpTopic;
            }
        }

        
        /// <value>Error message.</value>
        public override string Message
        {
            get
            {
                if (_errorRecord != null)
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
        protected HelpNotFoundException(SerializationInfo info,
                                        StreamingContext context)
        {
            throw new NotSupportedException();
        }
        
        #endregion Serialization
    }
}

#pragma warning restore 56506
