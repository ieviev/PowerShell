// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal abstract class CimAsyncOperation : IDisposable
    {
        #region Constructor

        
        protected CimAsyncOperation()
        {
            this.moreActionEvent = new ManualResetEventSlim(false);
            this.actionQueue = new ConcurrentQueue<CimBaseAction>();
            this._disposed = 0;
            this.operationCount = 0;
        }

        #endregion

        #region Event handler

        
        protected void NewCmdletActionHandler(object cimSession, CmdletActionEventArgs actionArgs)
        {
            DebugHelper.WriteLogEx("Disposed {0}, action type = {1}", 0, this.Disposed, actionArgs.Action);

            if (this.Disposed)
            {
                if (actionArgs.Action is CimSyncAction)
                {
                    // unblock the thread waiting for response
                    (actionArgs.Action as CimSyncAction).OnComplete();
                }

                return;
            }

            bool isEmpty = this.actionQueue.IsEmpty;
            this.actionQueue.Enqueue(actionArgs.Action);
            if (isEmpty)
            {
                this.moreActionEvent.Set();
            }
        }

        
        protected void OperationCreatedHandler(object cimSession, OperationEventArgs actionArgs)
        {
            DebugHelper.WriteLogEx();

            lock (this.a_lock)
            {
                this.operationCount++;
            }
        }

        
        protected void OperationDeletedHandler(object cimSession, OperationEventArgs actionArgs)
        {
            DebugHelper.WriteLogEx();

            lock (this.a_lock)
            {
                this.operationCount--;
                if (this.operationCount == 0)
                {
                    this.moreActionEvent.Set();
                }
            }
        }

        #endregion

        
        public void ProcessActions(CmdletOperationBase cmdletOperation)
        {
            if (!this.actionQueue.IsEmpty)
            {
                CimBaseAction action;
                while (GetActionAndRemove(out action))
                {
                    action.Execute(cmdletOperation);
                    if (this.Disposed)
                    {
                        break;
                    }
                }
            }
        }

        
        public void ProcessRemainActions(CmdletOperationBase cmdletOperation)
        {
            DebugHelper.WriteLogEx();

            while (true)
            {
                ProcessActions(cmdletOperation);
                if (!this.IsActive())
                {
                    DebugHelper.WriteLogEx("Either disposed or all operations completed.", 2);
                    break;
                }

                try
                {
                    this.moreActionEvent.Wait();
                    this.moreActionEvent.Reset();
                }
                catch (ObjectDisposedException ex)
                {
                    // This might happen if this object is being disposed,
                    // while another thread is processing the remaining actions
                    DebugHelper.WriteLogEx("moreActionEvent was disposed: {0}.", 2, ex);
                    break;
                }
            }

            ProcessActions(cmdletOperation);
        }

        #region helper methods

        
        protected bool GetActionAndRemove(out CimBaseAction action)
        {
            return this.actionQueue.TryDequeue(out action);
        }

        
        protected void AddCimSessionProxy(CimSessionProxy sessionproxy)
        {
            lock (cimSessionProxyCacheLock)
            {
                this.cimSessionProxyCache ??= new List<CimSessionProxy>();

                if (!this.cimSessionProxyCache.Contains(sessionproxy))
                {
                    this.cimSessionProxyCache.Add(sessionproxy);
                }
            }
        }

        
        protected bool IsActive()
        {
            DebugHelper.WriteLogEx("Disposed {0}, Operation Count {1}", 2, this.Disposed, this.operationCount);
            bool isActive = (!this.Disposed) && (this.operationCount > 0);
            return isActive;
        }

        
        protected CimSessionProxy CreateCimSessionProxy(CimSessionProxy originalProxy)
        {
            CimSessionProxy proxy = new(originalProxy);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        
        protected CimSessionProxy CreateCimSessionProxy(CimSessionProxy originalProxy, bool passThru)
        {
            CimSessionProxy proxy = new CimSessionProxySetCimInstance(originalProxy, passThru);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        
        protected CimSessionProxy CreateCimSessionProxy(CimSession session)
        {
            CimSessionProxy proxy = new(session);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        
        protected CimSessionProxy CreateCimSessionProxy(CimSession session, bool passThru)
        {
            CimSessionProxy proxy = new CimSessionProxySetCimInstance(session, passThru);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        
        protected CimSessionProxy CreateCimSessionProxy(string computerName)
        {
            CimSessionProxy proxy = new(computerName);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        
        protected CimSessionProxy CreateCimSessionProxy(string computerName, CimInstance cimInstance)
        {
            CimSessionProxy proxy = new(computerName, cimInstance);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        
        protected CimSessionProxy CreateCimSessionProxy(string computerName, CimInstance cimInstance, bool passThru)
        {
            CimSessionProxy proxy = new CimSessionProxySetCimInstance(computerName, cimInstance, passThru);
            this.SubscribeEventAndAddProxytoCache(proxy);
            return proxy;
        }

        
        protected void SubscribeEventAndAddProxytoCache(CimSessionProxy proxy)
        {
            this.AddCimSessionProxy(proxy);
            SubscribeToCimSessionProxyEvent(proxy);
        }

        
        protected virtual void SubscribeToCimSessionProxyEvent(CimSessionProxy proxy)
        {
            DebugHelper.WriteLogEx();

            proxy.OnNewCmdletAction += this.NewCmdletActionHandler;
            proxy.OnOperationCreated += this.OperationCreatedHandler;
            proxy.OnOperationDeleted += this.OperationDeletedHandler;
        }

        
        protected object GetBaseObject(object value)
        {
            if (value is not PSObject psObject)
            {
                return value;
            }
            else
            {
                object baseObject = psObject.BaseObject;
                if (baseObject is not object[] arrayObject)
                {
                    return baseObject;
                }
                else
                {
                    object[] arraybaseObject = new object[arrayObject.Length];
                    for (int i = 0; i < arrayObject.Length; i++)
                    {
                        arraybaseObject[i] = GetBaseObject(arrayObject[i]);
                    }

                    return arraybaseObject;
                }
            }
        }

        
        protected object GetReferenceOrReferenceArrayObject(object value, ref CimType referenceType)
        {
            if (value is PSReference cimReference)
            {
                object baseObject = GetBaseObject(cimReference.Value);
                if (!(baseObject is CimInstance cimInstance))
                {
                    return null;
                }

                referenceType = CimType.Reference;
                return cimInstance;
            }
            else
            {
                if (value is not object[] cimReferenceArray)
                {
                    return null;
                }
                else if (cimReferenceArray[0] is not PSReference)
                {
                    return null;
                }

                CimInstance[] cimInstanceArray = new CimInstance[cimReferenceArray.Length];
                for (int i = 0; i < cimReferenceArray.Length; i++)
                {
                    if (!(cimReferenceArray[i] is PSReference tempCimReference))
                    {
                        return null;
                    }

                    object baseObject = GetBaseObject(tempCimReference.Value);
                    cimInstanceArray[i] = baseObject as CimInstance;
                    if (cimInstanceArray[i] == null)
                    {
                        return null;
                    }
                }

                referenceType = CimType.ReferenceArray;
                return cimInstanceArray;
            }
        }
        #endregion

        #region IDisposable

        
        protected bool Disposed
        {
            get
            {
                return this._disposed == 1;
            }
        }

        private int _disposed;

        
        public void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        
        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref this._disposed, 1, 0) == 0)
            {
                if (disposing)
                {
                    // free managed resources
                    Cleanup();
                }
                // free native resources if there are any
            }
        }

        
        private void Cleanup()
        {
            DebugHelper.WriteLogEx();

            // unblock thread that waiting for more actions
            this.moreActionEvent.Set();
            CimBaseAction action;
            while (GetActionAndRemove(out action))
            {
                DebugHelper.WriteLog("Action {0}", 2, action);

                if (action is CimSyncAction)
                {
                    // unblock the thread waiting for response
                    (action as CimSyncAction).OnComplete();
                }
            }

            if (this.cimSessionProxyCache != null)
            {
                List<CimSessionProxy> temporaryProxy;
                lock (this.cimSessionProxyCache)
                {
                    temporaryProxy = new List<CimSessionProxy>(this.cimSessionProxyCache);
                    this.cimSessionProxyCache.Clear();
                }

                // clean up all proxy objects
                foreach (CimSessionProxy proxy in temporaryProxy)
                {
                    DebugHelper.WriteLog("Dispose proxy ", 2);
                    proxy.Dispose();
                }
            }

            this.moreActionEvent.Dispose();
            this.ackedEvent?.Dispose();

            DebugHelper.WriteLog("Cleanup complete.", 2);
        }

        #endregion

        #region private members

        
        private readonly object a_lock = new();

        
        private uint operationCount;

        
        private readonly ManualResetEventSlim moreActionEvent;

        
        private readonly ConcurrentQueue<CimBaseAction> actionQueue;

        
        private readonly object cimSessionProxyCacheLock = new();

        
        private List<CimSessionProxy> cimSessionProxyCache;

        #endregion

        #region protected members
        
        protected ManualResetEventSlim ackedEvent;
        #endregion

        #region const strings
        internal const string ComputerNameArgument = @"ComputerName";
        internal const string CimSessionArgument = @"CimSession";
        #endregion
    }
}
