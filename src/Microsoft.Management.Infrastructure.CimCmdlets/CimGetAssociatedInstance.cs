// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System.Collections.Generic;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal sealed class CimGetAssociatedInstance : CimAsyncOperation
    {
        
        public CimGetAssociatedInstance()
            : base()
        {
        }

        
        public void GetCimAssociatedInstance(GetCimAssociatedInstanceCommand cmdlet)
        {
            IEnumerable<string> computerNames = ConstValue.GetComputerNames(cmdlet.ComputerName);
            // use the namespace from parameter
            string nameSpace = cmdlet.Namespace;
            if ((nameSpace == null) && (cmdlet.ResourceUri == null))
            {
                // try to use namespace of ciminstance, then fall back to default namespace
                nameSpace = ConstValue.GetNamespace(cmdlet.CimInstance.CimSystemProperties.Namespace);
            }

            List<CimSessionProxy> proxys = new();
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.ComputerSetName:
                    foreach (string computerName in computerNames)
                    {
                        CimSessionProxy proxy = CreateSessionProxy(computerName, cmdlet.CimInstance, cmdlet);
                        proxys.Add(proxy);
                    }

                    break;
                case CimBaseCommand.SessionSetName:
                    foreach (CimSession session in cmdlet.CimSession)
                    {
                        CimSessionProxy proxy = CreateSessionProxy(session, cmdlet);
                        proxys.Add(proxy);
                    }

                    break;
                default:
                    return;
            }

            foreach (CimSessionProxy proxy in proxys)
            {
                proxy.EnumerateAssociatedInstancesAsync(
                    nameSpace,
                    cmdlet.CimInstance,
                    cmdlet.Association,
                    cmdlet.ResultClassName,
                    null,
                    null);
            }
        }

        #region private methods

        
        private static void SetSessionProxyProperties(
            ref CimSessionProxy proxy,
            GetCimAssociatedInstanceCommand cmdlet)
        {
            proxy.OperationTimeout = cmdlet.OperationTimeoutSec;
            proxy.KeyOnly = cmdlet.KeyOnly;
            if (cmdlet.ResourceUri != null)
            {
                proxy.ResourceUri = cmdlet.ResourceUri;
            }
        }

        
        private CimSessionProxy CreateSessionProxy(
            string computerName,
            CimInstance cimInstance,
            GetCimAssociatedInstanceCommand cmdlet)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(computerName, cimInstance);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        
        private CimSessionProxy CreateSessionProxy(
            CimSession session,
            GetCimAssociatedInstanceCommand cmdlet)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(session);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        #endregion

    }
}
