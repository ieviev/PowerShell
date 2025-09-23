// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Cmdletization
{
    
    /// <typeparam name="TObjectInstance">Type that represents instances of objects from the wrapped object model</typeparam>
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

        
        /// <param name="cmdlet"></param>
        /// <param name="className"></param>
        /// <param name="classVersion"></param>
        /// <param name="moduleVersion"></param>
        /// <param name="privateData"></param>
        public void Initialize(PSCmdlet cmdlet, string className, string classVersion, Version moduleVersion, IDictionary<string, string> privateData)
        {
            _moduleVersion = moduleVersion;

            Initialize(cmdlet, className, classVersion, privateData);
        }

        
        /// <returns>Query builder for a given object model.</returns>
        public virtual QueryBuilder GetQueryBuilder()
        {
            throw new NotImplementedException();
        }

        
        /// <param name="query">Query parameters.</param>
        /// <returns>A lazy evaluated collection of object instances.</returns>
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

        
        /// <remarks>
        /// The PowerShell engine will call this method on a separate thread
        /// from the pipeline thread where BeginProcessing, EndProcessing
        /// and other methods are normally being executed.
        /// </remarks>
        public virtual void StopProcessing()
        {
        }

        
        /// <param name="objectInstance">The object on which to invoke the method.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        /// <param name="passThru"><see langword="true"/> if successful method invocations should emit downstream the <paramref name="objectInstance"/> being operated on.</param>
        public virtual void ProcessRecord(TObjectInstance objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
            throw new NotImplementedException();
        }

        
        /// <param name="query">Query parameters.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        /// <param name="passThru"><see langword="true"/> if successful method invocations should emit downstream the object instance being operated on.</param>
        public virtual void ProcessRecord(QueryBuilder query, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
            throw new NotImplementedException();
        }

        
        /// <param name="methodInvocationInfo">Method invocation details.</param>
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
