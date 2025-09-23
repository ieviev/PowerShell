// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Cmdletization
{
    
    public abstract class CmdletAdapter<TObjectInstance>
        where TObjectInstance : class
    {
        internal void Initialize(PSCmdlet cmdlet, string className, string classVersion, IDictionary<string, string> privateData)
        {
            ArgumentNullException.ThrowIfNull(cmdlet);
            ArgumentException.ThrowIfNullOrEmpty(className);

            // possible and ok to have classVersion==string.Empty
            ArgumentNullException.ThrowIfNull(classVersion);
            ArgumentNullException.ThrowIfNull(privateData);

            _cmdlet = cmdlet;
            _className = className;
            _classVersion = classVersion;
            _privateData = privateData;

            if (this.Cmdlet is PSScriptCmdlet compiledScript)
            {
                compiledScript.StoppingEvent += delegate { this.StopProcessing(); };
                compiledScript.DisposingEvent +=
                        delegate
                        {
                            var disposable = this as IDisposable;
                            disposable?.Dispose();
                        };
            }
        }

        
        public void Initialize(PSCmdlet cmdlet, string className, string classVersion, Version moduleVersion, IDictionary<string, string> privateData)
        {
            _moduleVersion = moduleVersion;

            Initialize(cmdlet, className, classVersion, privateData);
        }

        
        public virtual QueryBuilder GetQueryBuilder()
        {
            throw new NotImplementedException();
        }

        
        public virtual void ProcessRecord(QueryBuilder query)
        {
            throw new NotImplementedException();
        }

        
        public virtual void BeginProcessing()
        {
        }

        
        public virtual void EndProcessing()
        {
        }

        
        public virtual void StopProcessing()
        {
        }

        
        public virtual void ProcessRecord(TObjectInstance objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
            throw new NotImplementedException();
        }

        
        public virtual void ProcessRecord(QueryBuilder query, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
            throw new NotImplementedException();
        }

        
        public virtual void ProcessRecord(
            MethodInvocationInfo methodInvocationInfo)
        {
            throw new NotImplementedException();
        }

        
        public PSCmdlet Cmdlet
        {
            get
            {
                return _cmdlet;
            }
        }

        private PSCmdlet _cmdlet;

        
        public string ClassName
        {
            get
            {
                return _className;
            }
        }

        private string _className;

        
        public string ClassVersion
        {
            get
            {
                return _classVersion;
            }
        }

        private string _classVersion;

        
        public Version ModuleVersion
        {
            get
            {
                return _moduleVersion;
            }
        }

        private Version _moduleVersion;

        
        public IDictionary<string, string> PrivateData
        {
            get
            {
                return _privateData;
            }
        }

        private IDictionary<string, string> _privateData;
    }
}
