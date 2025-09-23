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
    
    public class HelpCategoryInvalidException : ArgumentException, IContainsErrorRecord
    {
        
        /// <param name="helpCategory">The name of help category that is invalid.</param>
        public HelpCategoryInvalidException(string helpCategory)
            : base()
        {
            _helpCategory = helpCategory;
            CreateErrorRecord();
        }

        
        public HelpCategoryInvalidException()
            : base()
        {
            CreateErrorRecord();
        }

        
        /// <param name="helpCategory">The name of help category that is invalid.</param>
        /// <param name="innerException">The inner exception of this exception.</param>
        public HelpCategoryInvalidException(string helpCategory, Exception innerException)
            : base(
                  (innerException != null) ? innerException.Message : string.Empty,
                  innerException)
        {
            _helpCategory = helpCategory;
            CreateErrorRecord();
        }

        
        private void CreateErrorRecord()
        {
            _errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(this), "HelpCategoryInvalid", ErrorCategory.InvalidArgument, null);
            _errorRecord.ErrorDetails = new ErrorDetails(typeof(HelpCategoryInvalidException).Assembly, "HelpErrors", "HelpCategoryInvalid", _helpCategory);
        }

        private ErrorRecord _errorRecord;

        
        /// <value>ErrorRecord instance</value>
        public ErrorRecord ErrorRecord
        {
            get
            {
                return _errorRecord;
            }
        }

        private readonly string _helpCategory = System.Management.Automation.HelpCategory.None.ToString();

        
        /// <value>Name of the help category.</value>
        public string HelpCategory
        {
            get
            {
                return _helpCategory;
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
        protected HelpCategoryInvalidException(SerializationInfo info,
                                        StreamingContext context)            
        {
            throw new NotSupportedException();
        }

        #endregion Serialization
    }
}

#pragma warning restore 56506
