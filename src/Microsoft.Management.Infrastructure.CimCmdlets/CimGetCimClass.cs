// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System.Collections.Generic;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal class CimGetCimClassContext : XOperationContextBase
    {
        
        internal CimGetCimClassContext(
            string theClassName,
            string theMethodName,
            string thePropertyName,
            string theQualifierName)
        {
            this.ClassName = theClassName;
            this.MethodName = theMethodName;
            this.PropertyName = thePropertyName;
            this.QualifierName = theQualifierName;
        }

        
        public string ClassName { get; set; }

        
        internal string MethodName { get; }

        
        internal string PropertyName { get; }

        
        internal string QualifierName { get; }
    }

    
    internal sealed class CimGetCimClass : CimAsyncOperation
    {
        
        public CimGetCimClass()
            : base()
        {
        }

        
        public void GetCimClass(GetCimClassCommand cmdlet)
        {
            List<CimSessionProxy> proxys = new();
            string nameSpace = ConstValue.GetNamespace(cmdlet.Namespace);
            string className = cmdlet.ClassName ?? @"*";
            CimGetCimClassContext context = new(
                cmdlet.ClassName,
                cmdlet.MethodName,
                cmdlet.PropertyName,
                cmdlet.QualifierName);
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.ComputerSetName:
                    {
                        IEnumerable<string> computerNames = ConstValue.GetComputerNames(
                                cmdlet.ComputerName);
                        foreach (string computerName in computerNames)
                        {
                            CimSessionProxy proxy = CreateSessionProxy(computerName, cmdlet);
                            proxy.ContextObject = context;
                            proxys.Add(proxy);
                        }
                    }

                    break;
                case CimBaseCommand.SessionSetName:
                    {
                        foreach (CimSession session in cmdlet.CimSession)
                        {
                            CimSessionProxy proxy = CreateSessionProxy(session, cmdlet);
                            proxy.ContextObject = context;
                            proxys.Add(proxy);
                        }
                    }

                    break;
                default:
                    return;
            }

            if (WildcardPattern.ContainsWildcardCharacters(className))
            {
                // retrieve all classes and then filter based on
                // classname, propertyname, methodname, and qualifiername
                foreach (CimSessionProxy proxy in proxys)
                {
                    proxy.EnumerateClassesAsync(nameSpace);
                }
            }
            else
            {
                foreach (CimSessionProxy proxy in proxys)
                {
                    proxy.GetClassAsync(nameSpace, className);
                }
            }
        }

        #region private methods

        
        private static void SetSessionProxyProperties(
            ref CimSessionProxy proxy,
            GetCimClassCommand cmdlet)
        {
            proxy.OperationTimeout = cmdlet.OperationTimeoutSec;
            proxy.Amended = cmdlet.Amended;
        }

        
        private CimSessionProxy CreateSessionProxy(
            string computerName,
            GetCimClassCommand cmdlet)
        {
            CimSessionProxy proxy = new CimSessionProxyGetCimClass(computerName);
            this.SubscribeEventAndAddProxytoCache(proxy);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        
        private CimSessionProxy CreateSessionProxy(
            CimSession session,
            GetCimClassCommand cmdlet)
        {
            CimSessionProxy proxy = new CimSessionProxyGetCimClass(session);
            this.SubscribeEventAndAddProxytoCache(proxy);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        #endregion

    }
}
