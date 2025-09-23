// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Runtime.Serialization;
using System.Security.Permissions;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace System.Management.Automation.Runspaces
{
    
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "FormatTable")]
    public class FormatTableLoadException : RuntimeException
    {
        private readonly Collection<string> _errors;

        #region Constructors

        
        public FormatTableLoadException()
            : base()
        {
            SetDefaultErrorRecord();
        }

        
        public FormatTableLoadException(string message)
            : base(message)
        {
            SetDefaultErrorRecord();
        }

        
        public FormatTableLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
            SetDefaultErrorRecord();
        }

        
        internal FormatTableLoadException(ConcurrentBag<string> loadErrors)
            : base(StringUtil.Format(FormatAndOutXmlLoadingStrings.FormatTableLoadErrors))
        {
            _errors = new Collection<string>(loadErrors.ToArray());
            SetDefaultErrorRecord();
        }

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected FormatTableLoadException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Constructors

        
        protected void SetDefaultErrorRecord()
        {
            SetErrorCategory(ErrorCategory.InvalidData);
            SetErrorId(typeof(FormatTableLoadException).FullName);
        }

        
        public Collection<string> Errors
        {
            get
            {
                return _errors;
            }
        }
    }

    
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "FormatTable")]
    public sealed class FormatTable
    {
        #region Private Data

        private readonly TypeInfoDataBaseManager _formatDBMgr;

        #endregion

        #region Constructor

        
        internal FormatTable()
        {
            _formatDBMgr = new TypeInfoDataBaseManager();
        }

        
        public FormatTable(IEnumerable<string> formatFiles) : this(formatFiles, null, null)
        {
        }

        
        public void AppendFormatData(IEnumerable<ExtendedTypeDefinition> formatData)
        {
            if (formatData == null)
                throw PSTraceSource.NewArgumentNullException(nameof(formatData));
            _formatDBMgr.AddFormatData(formatData, false);
        }

        
        public void PrependFormatData(IEnumerable<ExtendedTypeDefinition> formatData)
        {
            if (formatData == null)
                throw PSTraceSource.NewArgumentNullException(nameof(formatData));
            _formatDBMgr.AddFormatData(formatData, true);
        }

        
        internal FormatTable(IEnumerable<string> formatFiles, AuthorizationManager authorizationManager, PSHost host)
        {
            if (formatFiles == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(formatFiles));
            }

            _formatDBMgr = new TypeInfoDataBaseManager(formatFiles, true, authorizationManager, host);
        }

        #endregion

        #region Internal Methods / Properties

        internal TypeInfoDataBaseManager FormatDBManager
        {
            get { return _formatDBMgr; }
        }

        
        internal void Add(string formatFile, bool shouldPrepend)
        {
            _formatDBMgr.Add(formatFile, shouldPrepend);
        }

        
        internal void Remove(string formatFile)
        {
            _formatDBMgr.Remove(formatFile);
        }

        #endregion

        #region static methods

        
        public static FormatTable LoadDefaultFormatFiles()
        {
            string psHome = Utils.DefaultPowerShellAppBase;
            List<string> defaultFormatFiles = new List<string>();
            if (!string.IsNullOrEmpty(psHome))
            {
                defaultFormatFiles.AddRange(Platform.FormatFileNames.Select(file => Path.Combine(psHome, file)));
            }

            return new FormatTable(defaultFormatFiles);
        }
        #endregion static methods
    }
}
