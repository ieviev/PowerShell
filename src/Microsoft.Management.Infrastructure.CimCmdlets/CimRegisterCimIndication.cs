// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Globalization;
using System.Management.Automation;
using System.Threading;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal abstract class CimSubscriptionEventArgs : EventArgs
    {
        
        public object Context
        {
            get
            {
                return context;
            }
        }

        protected object context;
    }

    
    internal class CimSubscriptionResultEventArgs : CimSubscriptionEventArgs
    {
        
        public CimSubscriptionResult Result { get; }

        
        public CimSubscriptionResultEventArgs(
            CimSubscriptionResult theResult)
        {
            this.context = null;
            this.Result = theResult;
        }
    }

    
    internal class CimSubscriptionExceptionEventArgs : CimSubscriptionEventArgs
    {
        
        public Exception Exception { get; }

        
        public CimSubscriptionExceptionEventArgs(
            Exception theException)
        {
            this.context = null;
            this.Exception = theException;
        }
    }

    
    internal sealed class CimRegisterCimIndication : CimAsyncOperation
    {
        
        public event EventHandler<CimSubscriptionEventArgs> OnNewSubscriptionResult;

        
        public CimRegisterCimIndication()
            : base()
        {
            this.ackedEvent = new ManualResetEventSlim(false);
        }

        
        public void RegisterCimIndication(
            string computerName,
            string nameSpace,
            string queryDialect,
            string queryExpression,
            uint operationTimeout)
        {
            DebugHelper.WriteLogEx("queryDialect = '{0}'; queryExpression = '{1}'", 0, queryDialect, queryExpression);
            this.TargetComputerName = computerName;
            CimSessionProxy proxy = CreateSessionProxy(computerName, operationTimeout);
            proxy.SubscribeAsync(nameSpace, queryDialect, queryExpression);
            WaitForAckMessage();
        }

        
        public void RegisterCimIndication(
            CimSession cimSession,
            string nameSpace,
            string queryDialect,
            string queryExpression,
            uint operationTimeout)
        {
            DebugHelper.WriteLogEx("queryDialect = '{0}'; queryExpression = '{1}'", 0, queryDialect, queryExpression);

            ArgumentNullException.ThrowIfNull(cimSession, string.Format(CultureInfo.CurrentUICulture, CimCmdletStrings.NullArgument, nameof(cimSession)));

            this.TargetComputerName = cimSession.ComputerName;
            CimSessionProxy proxy = CreateSessionProxy(cimSession, operationTimeout);
            proxy.SubscribeAsync(nameSpace, queryDialect, queryExpression);
            WaitForAckMessage();
        }

        #region override methods

        
        protected override void SubscribeToCimSessionProxyEvent(CimSessionProxy proxy)
        {
            DebugHelper.WriteLog("SubscribeToCimSessionProxyEvent", 4);
            // Raise event instead of write object to ps
            proxy.OnNewCmdletAction += this.CimIndicationHandler;
            proxy.OnOperationCreated += this.OperationCreatedHandler;
            proxy.OnOperationDeleted += this.OperationDeletedHandler;
            proxy.EnableMethodResultStreaming = false;
        }

        
        private void CimIndicationHandler(object cimSession, CmdletActionEventArgs actionArgs)
        {
            DebugHelper.WriteLogEx("action is {0}. Disposed {1}", 0, actionArgs.Action, this.Disposed);

            if (this.Disposed)
            {
                return;
            }

            // NOTES: should move after this.Disposed, but need to log the exception
            if (actionArgs.Action is CimWriteError cimWriteError)
            {
                this.Exception = cimWriteError.Exception;
                if (!this.ackedEvent.IsSet)
                {
                    // an exception happened
                    DebugHelper.WriteLogEx("an exception happened", 0);
                    this.ackedEvent.Set();
                    return;
                }

                EventHandler<CimSubscriptionEventArgs> temp = this.OnNewSubscriptionResult;
                if (temp != null)
                {
                    DebugHelper.WriteLog("Raise an exception event", 2);

                    temp(this, new CimSubscriptionExceptionEventArgs(this.Exception));
                }

                DebugHelper.WriteLog("Got an exception: {0}", 2, Exception);
            }

            if (actionArgs.Action is CimWriteResultObject cimWriteResultObject)
            {
                if (cimWriteResultObject.Result is CimSubscriptionResult result)
                {
                    EventHandler<CimSubscriptionEventArgs> temp = this.OnNewSubscriptionResult;
                    if (temp != null)
                    {
                        DebugHelper.WriteLog("Raise an result event", 2);
                        temp(this, new CimSubscriptionResultEventArgs(result));
                    }
                }
                else
                {
                    if (!this.ackedEvent.IsSet)
                    {
                        // an ACK message returned
                        DebugHelper.WriteLogEx("an ack message happened", 0);
                        this.ackedEvent.Set();
                        return;
                    }
                    else
                    {
                        DebugHelper.WriteLogEx("an ack message should not happen here", 0);
                    }
                }
            }
        }

        
        private void WaitForAckMessage()
        {
            DebugHelper.WriteLogEx();
            this.ackedEvent.Wait();
            if (this.Exception != null)
            {
                DebugHelper.WriteLogEx("error happened", 0);
                if (this.Cmdlet != null)
                {
                    DebugHelper.WriteLogEx("Throw Terminating error", 1);

                    // throw terminating error
                    ErrorRecord errorRecord = ErrorToErrorRecord.ErrorRecordFromAnyException(
                        new InvocationContext(this.TargetComputerName, null), this.Exception, null);
                    this.Cmdlet.ThrowTerminatingError(errorRecord);
                }
                else
                {
                    DebugHelper.WriteLogEx("Throw exception", 1);
                    // throw exception out
                    throw this.Exception;
                }
            }

            DebugHelper.WriteLogEx("ACK happened", 0);
        }
        #endregion

        #region internal property
        
        internal Cmdlet Cmdlet
        {
            get;
            set;
        }

        
        internal string TargetComputerName
        {
            get;
            set;
        }

        #endregion

        #region private methods
        
        private CimSessionProxy CreateSessionProxy(
            string computerName,
            uint timeout)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(computerName);
            proxy.OperationTimeout = timeout;
            return proxy;
        }

        
        private CimSessionProxy CreateSessionProxy(
            CimSession session,
            uint timeout)
        {
            CimSessionProxy proxy = CreateCimSessionProxy(session);
            proxy.OperationTimeout = timeout;
            return proxy;
        }
        #endregion

        #region private members

        
        internal Exception Exception { get; private set; }

        #endregion

    }
}
