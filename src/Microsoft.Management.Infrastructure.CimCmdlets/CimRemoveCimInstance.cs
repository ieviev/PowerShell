// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal class CimRemoveCimInstanceContext : XOperationContextBase
    {
        
        /// <param name="theNamespace"></param>
        /// <param name="theProxy"></param>
        internal CimRemoveCimInstanceContext(string theNamespace,
            CimSessionProxy theProxy)
        {
            this.proxy = theProxy;
            this.nameSpace = theNamespace;
        }
    }

    
    internal sealed class CimRemoveCimInstance : CimGetInstance
    {
        
        public CimRemoveCimInstance()
            : base()
        {
        }

        
        /// <param name="cmdlet"><see cref="GetCimInstanceCommand"/> object.</param>
        public void RemoveCimInstance(RemoveCimInstanceCommand cmdlet)
        {
            DebugHelper.WriteLogEx();

            IEnumerable<string> computerNames = ConstValue.GetComputerNames(
                GetComputerName(cmdlet));
            List<CimSessionProxy> proxys = new();
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.CimInstanceComputerSet:
                    foreach (string computerName in computerNames)
                    {
                        proxys.Add(CreateSessionProxy(computerName, cmdlet.CimInstance, cmdlet));
                    }

                    break;
                case CimBaseCommand.CimInstanceSessionSet:
                    foreach (CimSession session in GetCimSession(cmdlet))
                    {
                        proxys.Add(CreateSessionProxy(session, cmdlet));
                    }

                    break;
                default:
                    break;
            }

            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.CimInstanceComputerSet:
                case CimBaseCommand.CimInstanceSessionSet:
                    string nameSpace = null;
                    if (cmdlet.ResourceUri != null)
                    {
                        nameSpace = GetCimInstanceParameter(cmdlet).CimSystemProperties.Namespace;
                    }
                    else
                    {
                        nameSpace = ConstValue.GetNamespace(GetCimInstanceParameter(cmdlet).CimSystemProperties.Namespace);
                    }

                    string target = cmdlet.CimInstance.ToString();
                    foreach (CimSessionProxy proxy in proxys)
                    {
                        if (!cmdlet.ShouldProcess(target, action))
                        {
                            return;
                        }

                        proxy.DeleteInstanceAsync(nameSpace, cmdlet.CimInstance);
                    }

                    break;
                case CimBaseCommand.QueryComputerSet:
                case CimBaseCommand.QuerySessionSet:
                    GetCimInstanceInternal(cmdlet);
                    break;
                default:
                    break;
            }
        }

        
        /// <param name="cimInstance"></param>
        internal void RemoveCimInstance(CimInstance cimInstance, XOperationContextBase context, CmdletOperationBase cmdlet)
        {
            DebugHelper.WriteLogEx();

            string target = cimInstance.ToString();
            if (!cmdlet.ShouldProcess(target, action))
            {
                return;
            }

            CimRemoveCimInstanceContext removeContext = context as CimRemoveCimInstanceContext;
            Debug.Assert(removeContext != null, "CimRemoveCimInstance::RemoveCimInstance should has CimRemoveCimInstanceContext != NULL.");

            CimSessionProxy proxy = CreateCimSessionProxy(removeContext.Proxy);
            proxy.DeleteInstanceAsync(removeContext.Namespace, cimInstance);
        }

        #region const strings
        
        private const string action = @"Remove-CimInstance";
        #endregion
    }
}
