// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.ComponentModel;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    public abstract class CimIndicationEventArgs : EventArgs
    {
        
        public object Context
        {
            get
            {
                return context;
            }
        }

        internal object context;
    }

    
    public class CimIndicationEventExceptionEventArgs : CimIndicationEventArgs
    {
        
        public Exception Exception { get; }

        
        /// <param name="result"></param>
        public CimIndicationEventExceptionEventArgs(Exception theException)
        {
            context = null;
            this.Exception = theException;
        }
    }

    
    public class CimIndicationEventInstanceEventArgs : CimIndicationEventArgs
    {
        
        public CimInstance NewEvent
        {
            get
            {
                return result?.Instance;
            }
        }

        
        public string MachineId
        {
            get
            {
                return result?.MachineId;
            }
        }

        
        public string Bookmark
        {
            get
            {
                return result?.Bookmark;
            }
        }

        
        /// <param name="result"></param>
        public CimIndicationEventInstanceEventArgs(CimSubscriptionResult result)
        {
            context = null;
            this.result = result;
        }

        
        private readonly CimSubscriptionResult result;
    }

    
    public class CimIndicationWatcher
    {
        
        internal enum Status
        {
            Default,
            Started,
            Stopped
        }

        
        public event EventHandler<CimIndicationEventArgs> CimIndicationArrived;

        
        /// <param name="computerName"></param>
        /// <param name="nameSpace"></param>
        /// <param name="queryExpression"></param>
        /// <param name="operationTimeout"></param>
        public CimIndicationWatcher(
            string computerName,
            string theNamespace,
            string queryDialect,
            string queryExpression,
            uint operationTimeout)
        {
            ValidationHelper.ValidateNoNullorWhiteSpaceArgument(queryExpression, queryExpressionParameterName);
            computerName = ConstValue.GetComputerName(computerName);
            theNamespace = ConstValue.GetNamespace(theNamespace);
            Initialize(computerName, null, theNamespace, queryDialect, queryExpression, operationTimeout);
        }

        
        /// <param name="cimSession"></param>
        /// <param name="nameSpace"></param>
        /// <param name="queryExpression"></param>
        /// <param name="operationTimeout"></param>
        public CimIndicationWatcher(
            CimSession cimSession,
            string theNamespace,
            string queryDialect,
            string queryExpression,
            uint operationTimeout)
        {
            ValidationHelper.ValidateNoNullorWhiteSpaceArgument(queryExpression, queryExpressionParameterName);
            ValidationHelper.ValidateNoNullArgument(cimSession, cimSessionParameterName);
            theNamespace = ConstValue.GetNamespace(theNamespace);
            Initialize(null, cimSession, theNamespace, queryDialect, queryExpression, operationTimeout);
        }

        
        private void Initialize(
            string theComputerName,
            CimSession theCimSession,
            string theNameSpace,
            string theQueryDialect,
            string theQueryExpression,
            uint theOperationTimeout)
        {
            enableRaisingEvents = false;
            status = Status.Default;
            myLock = new object();
            cimRegisterCimIndication = new CimRegisterCimIndication();
            cimRegisterCimIndication.OnNewSubscriptionResult += NewSubscriptionResultHandler;

            this.cimSession = theCimSession;
            this.nameSpace = theNameSpace;
            this.queryDialect = ConstValue.GetQueryDialectWithDefault(theQueryDialect);
            this.queryExpression = theQueryExpression;
            this.operationTimeout = theOperationTimeout;
            this.computerName = theComputerName;
        }

        
        /// <param name="src"></param>
        /// <param name="args"></param>
        private void NewSubscriptionResultHandler(object src, CimSubscriptionEventArgs args)
        {
            EventHandler<CimIndicationEventArgs> temp = this.CimIndicationArrived;
            if (temp != null)
            {
                // raise the event
                if (args is CimSubscriptionResultEventArgs resultArgs)
                    temp(this, new CimIndicationEventInstanceEventArgs(resultArgs.Result));
                else if (args is CimSubscriptionExceptionEventArgs exceptionArgs)
                {
                    temp(this, new CimIndicationEventExceptionEventArgs(exceptionArgs.Exception));
                }
            }
        }

        
        [Browsable(false)]
        public bool EnableRaisingEvents
        {
            get
            {
                return enableRaisingEvents;
            }

            set
            {
                DebugHelper.WriteLogEx();
                if (value && !enableRaisingEvents)
                {
                    enableRaisingEvents = value;
                    Start();
                }
            }
        }

        private bool enableRaisingEvents;

        
        public void Start()
        {
            DebugHelper.WriteLogEx();

            lock (myLock)
            {
                if (status == Status.Default)
                {
                    if (this.cimSession == null)
                    {
                        cimRegisterCimIndication.RegisterCimIndication(
                            this.computerName,
                            this.nameSpace,
                            this.queryDialect,
                            this.queryExpression,
                            this.operationTimeout);
                    }
                    else
                    {
                        cimRegisterCimIndication.RegisterCimIndication(
                            this.cimSession,
                            this.nameSpace,
                            this.queryDialect,
                            this.queryExpression,
                            this.operationTimeout);
                    }

                    status = Status.Started;
                }
            }
        }

        
        public void Stop()
        {
            DebugHelper.WriteLogEx("Status = {0}", 0, this.status);

            lock (this.myLock)
            {
                if (status == Status.Started)
                {
                    if (this.cimRegisterCimIndication != null)
                    {
                        DebugHelper.WriteLog("Dispose CimRegisterCimIndication object", 4);
                        this.cimRegisterCimIndication.Dispose();
                    }

                    status = Status.Stopped;
                }
            }
        }

        #region internal method
        
        /// <param name="cmdlet"></param>
        internal void SetCmdlet(Cmdlet cmdlet)
        {
            if (this.cimRegisterCimIndication != null)
            {
                this.cimRegisterCimIndication.Cmdlet = cmdlet;
            }
        }
        #endregion

        #region private members
        
        private CimRegisterCimIndication cimRegisterCimIndication;

        
        private Status status;

        
        private object myLock;

        
        private const string cimSessionParameterName = "cimSession";

        
        private const string queryExpressionParameterName = "queryExpression";

        #region parameters
        
        private string computerName;
        private CimSession cimSession;
        private string nameSpace;
        private string queryDialect;
        private string queryExpression;
        private uint operationTimeout;
        #endregion
        #endregion
    }
}
