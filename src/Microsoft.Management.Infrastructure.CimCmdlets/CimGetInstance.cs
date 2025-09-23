// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Text;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal class FormatPartialCimInstance : IObjectPreProcess
    {
        
        internal const string PartialPSTypeName = @"Microsoft.Management.Infrastructure.CimInstance#__PartialCIMInstance";

        
        public object Process(object resultObject)
        {
            if (resultObject is CimInstance)
            {
                PSObject obj = PSObject.AsPSObject(resultObject);
                obj.TypeNames.Insert(0, PartialPSTypeName);
                return obj;
            }

            return resultObject;
        }
    }

    
    internal class CimGetInstance : CimAsyncOperation
    {
        
        public CimGetInstance() : base()
        {
        }

        
        public void GetCimInstance(GetCimInstanceCommand cmdlet)
        {
            GetCimInstanceInternal(cmdlet);
        }

        
        protected void GetCimInstanceInternal(CimBaseCommand cmdlet)
        {
            IEnumerable<string> computerNames = ConstValue.GetComputerNames(
                GetComputerName(cmdlet));
            string nameSpace;
            List<CimSessionProxy> proxys = new();
            bool isGetCimInstanceCommand = cmdlet is GetCimInstanceCommand;
            CimInstance targetCimInstance = null;
            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.CimInstanceComputerSet:
                    foreach (string computerName in computerNames)
                    {
                        targetCimInstance = GetCimInstanceParameter(cmdlet);
                        CimSessionProxy proxy = CreateSessionProxy(computerName, targetCimInstance, cmdlet);
                        if (isGetCimInstanceCommand)
                        {
                            SetPreProcess(proxy, cmdlet as GetCimInstanceCommand);
                        }

                        proxys.Add(proxy);
                    }

                    break;
                case CimBaseCommand.ClassNameComputerSet:
                case CimBaseCommand.QueryComputerSet:
                case CimBaseCommand.ResourceUriComputerSet:
                    foreach (string computerName in computerNames)
                    {
                        CimSessionProxy proxy = CreateSessionProxy(computerName, cmdlet);
                        if (isGetCimInstanceCommand)
                        {
                            SetPreProcess(proxy, cmdlet as GetCimInstanceCommand);
                        }

                        proxys.Add(proxy);
                    }

                    break;
                case CimBaseCommand.ClassNameSessionSet:
                case CimBaseCommand.CimInstanceSessionSet:
                case CimBaseCommand.QuerySessionSet:
                case CimBaseCommand.ResourceUriSessionSet:
                    foreach (CimSession session in GetCimSession(cmdlet))
                    {
                        CimSessionProxy proxy = CreateSessionProxy(session, cmdlet);
                        if (isGetCimInstanceCommand)
                        {
                            SetPreProcess(proxy, cmdlet as GetCimInstanceCommand);
                        }

                        proxys.Add(proxy);
                    }

                    break;
                default:
                    break;
            }

            switch (cmdlet.ParameterSetName)
            {
                case CimBaseCommand.ClassNameComputerSet:
                case CimBaseCommand.ClassNameSessionSet:
                    nameSpace = ConstValue.GetNamespace(GetNamespace(cmdlet));
                    if (IsClassNameQuerySet(cmdlet))
                    {
                        string query = CreateQuery(cmdlet);
                        DebugHelper.WriteLogEx(@"Query = {0}", 1, query);
                        foreach (CimSessionProxy proxy in proxys)
                        {
                            proxy.QueryInstancesAsync(nameSpace,
                                ConstValue.GetQueryDialectWithDefault(GetQueryDialect(cmdlet)),
                                query);
                        }
                    }
                    else
                    {
                        foreach (CimSessionProxy proxy in proxys)
                        {
                            proxy.EnumerateInstancesAsync(nameSpace, GetClassName(cmdlet));
                        }
                    }

                    break;
                case CimBaseCommand.CimInstanceComputerSet:
                case CimBaseCommand.CimInstanceSessionSet:
                    {
                        CimInstance instance = GetCimInstanceParameter(cmdlet);
                        nameSpace = ConstValue.GetNamespace(instance.CimSystemProperties.Namespace);
                        foreach (CimSessionProxy proxy in proxys)
                        {
                            proxy.GetInstanceAsync(nameSpace, instance);
                        }
                    }

                    break;
                case CimBaseCommand.QueryComputerSet:
                case CimBaseCommand.QuerySessionSet:
                    nameSpace = ConstValue.GetNamespace(GetNamespace(cmdlet));
                    foreach (CimSessionProxy proxy in proxys)
                    {
                        proxy.QueryInstancesAsync(nameSpace,
                            ConstValue.GetQueryDialectWithDefault(GetQueryDialect(cmdlet)),
                            GetQuery(cmdlet));
                    }

                    break;
                case CimBaseCommand.ResourceUriSessionSet:
                case CimBaseCommand.ResourceUriComputerSet:
                    foreach (CimSessionProxy proxy in proxys)
                    {
                        proxy.EnumerateInstancesAsync(GetNamespace(cmdlet), GetClassName(cmdlet));
                    }

                    break;
                default:
                    break;
            }
        }

        #region bridge methods to read properties from cmdlet

        protected static string[] GetComputerName(CimBaseCommand cmdlet)
        {
            if (cmdlet is GetCimInstanceCommand)
            {
                return (cmdlet as GetCimInstanceCommand).ComputerName;
            }
            else if (cmdlet is RemoveCimInstanceCommand)
            {
                return (cmdlet as RemoveCimInstanceCommand).ComputerName;
            }
            else if (cmdlet is SetCimInstanceCommand)
            {
                return (cmdlet as SetCimInstanceCommand).ComputerName;
            }

            return null;
        }

        protected static string GetNamespace(CimBaseCommand cmdlet)
        {
            if (cmdlet is GetCimInstanceCommand)
            {
                return (cmdlet as GetCimInstanceCommand).Namespace;
            }
            else if (cmdlet is RemoveCimInstanceCommand)
            {
                return (cmdlet as RemoveCimInstanceCommand).Namespace;
            }
            else if (cmdlet is SetCimInstanceCommand)
            {
                return (cmdlet as SetCimInstanceCommand).Namespace;
            }

            return null;
        }

        protected static CimSession[] GetCimSession(CimBaseCommand cmdlet)
        {
            if (cmdlet is GetCimInstanceCommand)
            {
                return (cmdlet as GetCimInstanceCommand).CimSession;
            }
            else if (cmdlet is RemoveCimInstanceCommand)
            {
                return (cmdlet as RemoveCimInstanceCommand).CimSession;
            }
            else if (cmdlet is SetCimInstanceCommand)
            {
                return (cmdlet as SetCimInstanceCommand).CimSession;
            }

            return null;
        }

        protected static string GetClassName(CimBaseCommand cmdlet)
        {
            if (cmdlet is GetCimInstanceCommand)
            {
                return (cmdlet as GetCimInstanceCommand).ClassName;
            }

            return null;
        }

        protected static string GetQuery(CimBaseCommand cmdlet)
        {
            if (cmdlet is GetCimInstanceCommand)
            {
                return (cmdlet as GetCimInstanceCommand).Query;
            }
            else if (cmdlet is RemoveCimInstanceCommand)
            {
                return (cmdlet as RemoveCimInstanceCommand).Query;
            }
            else if (cmdlet is SetCimInstanceCommand)
            {
                return (cmdlet as SetCimInstanceCommand).Query;
            }

            return null;
        }

        internal static bool IsClassNameQuerySet(CimBaseCommand cmdlet)
        {
            DebugHelper.WriteLogEx();
            if (cmdlet is GetCimInstanceCommand cmd)
            {
                if (cmd.QueryDialect != null || cmd.SelectProperties != null || cmd.Filter != null)
                {
                    return true;
                }
            }

            return false;
        }

        protected static string CreateQuery(CimBaseCommand cmdlet)
        {
            DebugHelper.WriteLogEx();
            if (cmdlet is GetCimInstanceCommand cmd)
            {
                StringBuilder propertyList = new();
                if (cmd.SelectProperties == null)
                {
                    propertyList.Append('*');
                }
                else
                {
                    foreach (string property in cmd.SelectProperties)
                    {
                        if (propertyList.Length > 0)
                        {
                            propertyList.Append(',');
                        }

                        propertyList.Append(property);
                    }
                }

                return (cmd.Filter == null) ?
                    string.Format(CultureInfo.CurrentUICulture, queryWithoutWhere, propertyList, cmd.ClassName) :
                    string.Format(CultureInfo.CurrentUICulture, queryWithWhere, propertyList, cmd.ClassName, cmd.Filter);
            }

            return null;
        }

        protected static string GetQueryDialect(CimBaseCommand cmdlet)
        {
            if (cmdlet is GetCimInstanceCommand)
            {
                return (cmdlet as GetCimInstanceCommand).QueryDialect;
            }
            else if (cmdlet is RemoveCimInstanceCommand)
            {
                return (cmdlet as RemoveCimInstanceCommand).QueryDialect;
            }
            else if (cmdlet is SetCimInstanceCommand)
            {
                return (cmdlet as SetCimInstanceCommand).QueryDialect;
            }

            return null;
        }

        protected static CimInstance GetCimInstanceParameter(CimBaseCommand cmdlet)
        {
            if (cmdlet is GetCimInstanceCommand)
            {
                return (cmdlet as GetCimInstanceCommand).CimInstance;
            }
            else if (cmdlet is RemoveCimInstanceCommand)
            {
                return (cmdlet as RemoveCimInstanceCommand).CimInstance;
            }
            else if (cmdlet is SetCimInstanceCommand)
            {
                return (cmdlet as SetCimInstanceCommand).CimInstance;
            }

            return null;
        }
        #endregion

        #region help methods

        
        private static void SetSessionProxyProperties(
            ref CimSessionProxy proxy,
            CimBaseCommand cmdlet)
        {
            if (cmdlet is GetCimInstanceCommand)
            {
                GetCimInstanceCommand getCimInstance = cmdlet as GetCimInstanceCommand;
                proxy.KeyOnly = getCimInstance.KeyOnly;
                proxy.Shallow = getCimInstance.Shallow;
                proxy.OperationTimeout = getCimInstance.OperationTimeoutSec;
                if (getCimInstance.ResourceUri != null)
                {
                    proxy.ResourceUri = getCimInstance.ResourceUri;
                }
            }
            else if (cmdlet is RemoveCimInstanceCommand)
            {
                RemoveCimInstanceCommand removeCimInstance = cmdlet as RemoveCimInstanceCommand;
                proxy.OperationTimeout = removeCimInstance.OperationTimeoutSec;
                if (removeCimInstance.ResourceUri != null)
                {
                    proxy.ResourceUri = removeCimInstance.ResourceUri;
                }

                CimRemoveCimInstanceContext context = new(
                    ConstValue.GetNamespace(removeCimInstance.Namespace),
                    proxy);
                proxy.ContextObject = context;
            }
            else if (cmdlet is SetCimInstanceCommand)
            {
                SetCimInstanceCommand setCimInstance = cmdlet as SetCimInstanceCommand;
                proxy.OperationTimeout = setCimInstance.OperationTimeoutSec;
                if (setCimInstance.ResourceUri != null)
                {
                    proxy.ResourceUri = setCimInstance.ResourceUri;
                }

                CimSetCimInstanceContext context = new(
                    ConstValue.GetNamespace(setCimInstance.Namespace),
                    setCimInstance.Property,
                    proxy,
                    cmdlet.ParameterSetName,
                    setCimInstance.PassThru);
                proxy.ContextObject = context;
            }
        }

        
        protected CimSessionProxy CreateSessionProxy(
            string computerName,
            CimBaseCommand cmdlet)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(computerName);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        
        protected CimSessionProxy CreateSessionProxy(
            string computerName,
            CimInstance cimInstance,
            CimBaseCommand cmdlet,
            bool passThru)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(computerName, cimInstance, passThru);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        
        protected CimSessionProxy CreateSessionProxy(
            CimSession session,
            CimBaseCommand cmdlet)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(session);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        
        protected CimSessionProxy CreateSessionProxy(
            string computerName,
            CimInstance cimInstance,
            CimBaseCommand cmdlet)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(computerName, cimInstance);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        
        protected CimSessionProxy CreateSessionProxy(
            CimSession session,
            CimBaseCommand cmdlet,
            bool passThru)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(session, passThru);
            SetSessionProxyProperties(ref proxy, cmdlet);
            return proxy;
        }

        
        private static void SetPreProcess(CimSessionProxy proxy, GetCimInstanceCommand cmdlet)
        {
            if (cmdlet.KeyOnly || (cmdlet.SelectProperties != null))
            {
                proxy.ObjectPreProcess = new FormatPartialCimInstance();
            }
        }
        #endregion

        #region const strings
        
        private const string queryWithWhere = @"SELECT {0} FROM {1} WHERE {2}";

        
        private const string queryWithoutWhere = @"SELECT {0} FROM {1}";
        #endregion
    }
}
