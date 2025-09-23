// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    using System;
    using System.Diagnostics.Eventing;
    using System.Management.Automation.Tracing;
    using System.Threading;

    
    public interface IBackgroundDispatcher
    {
        
        bool QueueUserWorkItem(WaitCallback callback);

        
        bool QueueUserWorkItem(WaitCallback callback, object state);

        
        IAsyncResult BeginInvoke(WaitCallback callback, object state, AsyncCallback completionCallback, object asyncState);

        
        void EndInvoke(IAsyncResult asyncResult);
    }

    
    public class BackgroundDispatcher :
        IBackgroundDispatcher
    {
        #region Instance Data

        private readonly IMethodInvoker _etwActivityMethodInvoker;
        private readonly WaitCallback _invokerWaitCallback;

        #endregion

        #region Creation/Cleanup

        
        public BackgroundDispatcher(EventProvider transferProvider, EventDescriptor transferEvent)
            : this(new EtwActivityReverterMethodInvoker(new EtwEventCorrelator(transferProvider, transferEvent)))
        {
            // nothing
        }

        // internal for unit testing only.  Otherwise, would be private.
        internal BackgroundDispatcher(IMethodInvoker etwActivityMethodInvoker)
        {
            ArgumentNullException.ThrowIfNull(etwActivityMethodInvoker);
            _etwActivityMethodInvoker = etwActivityMethodInvoker;
            _invokerWaitCallback = DoInvoker;
        }

        #endregion

        #region Instance Utilities

        private void DoInvoker(object invokerArgs)
        {
            var invokerArgsArray = (object[])invokerArgs;

            _etwActivityMethodInvoker.Invoker.DynamicInvoke(invokerArgsArray);
        }

        #endregion

        #region Instance Access

        
        public bool QueueUserWorkItem(WaitCallback callback)
        {
            return QueueUserWorkItem(callback, null);
        }

        
        public bool QueueUserWorkItem(WaitCallback callback, object state)
        {
            var invokerArgs = _etwActivityMethodInvoker.CreateInvokerArgs(callback, new object[] { state });

            var result = ThreadPool.QueueUserWorkItem(_invokerWaitCallback, invokerArgs);
            return result;
        }

        
        public IAsyncResult BeginInvoke(WaitCallback callback, object state, AsyncCallback completionCallback, object asyncState)
        {
            var invokerArgs = _etwActivityMethodInvoker.CreateInvokerArgs(callback, new object[] { state });

            var result = _invokerWaitCallback.BeginInvoke(invokerArgs, completionCallback, asyncState);
            return result;
        }

        
        public void EndInvoke(IAsyncResult asyncResult)
        {
            _invokerWaitCallback.EndInvoke(asyncResult);
        }

        #endregion
    }
}
