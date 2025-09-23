// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Management.Automation
{
    
    public class ApplicationInfo : CommandInfo
    {
        #region ctor

        
        internal ApplicationInfo(string name, string path, ExecutionContext context) : base(name, CommandTypes.Application)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(context));
            }

            Path = path;
            Extension = System.IO.Path.GetExtension(path);
            _context = context;
        }

        private readonly ExecutionContext _context;
        #endregion ctor

        
        public string Path { get; } = string.Empty;

        
        public string Extension { get; } = string.Empty;

        
        public override string Definition
        {
            get
            {
                return Path;
            }
        }

        
        public override string Source
        {
            get { return this.Definition; }
        }

        
        public override Version Version
        {
            get
            {
                if (_version == null)
                {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Path);
                    _version = new Version(versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart);
                }

                return _version;
            }
        }

        private Version _version;

        
        public override SessionStateEntryVisibility Visibility
        {
            get
            {
                return _context.EngineSessionState.CheckApplicationVisibility(Path);
            }

            set
            {
                throw PSTraceSource.NewNotImplementedException();
            }
        }

        
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get
            {
                if (_outputType == null)
                {
                    List<PSTypeName> l = new List<PSTypeName>();
                    l.Add(new PSTypeName(typeof(string)));
                    _outputType = new ReadOnlyCollection<PSTypeName>(l);
                }

                return _outputType;
            }
        }

        private ReadOnlyCollection<PSTypeName> _outputType = null;
    }
}
