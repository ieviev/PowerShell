// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Threading;
using Microsoft.Management.Infrastructure.Options;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal abstract class CimBaseAction
    {
        
        protected CimBaseAction()
        {
        }

        
        public virtual void Execute(CmdletOperationBase cmdlet)
        {
        }

        
        protected XOperationContextBase Context { get; set; }
    }

    
    internal class CimSyncAction : CimBaseAction, IDisposable
    {
        
        public CimSyncAction()
        {
            this.completeEvent = new ManualResetEventSlim(false);
            this.responseType = CimResponseType.None;
        }

        
        public virtual CimResponseType GetResponse()
        {
            this.Block();
            return responseType;
        }

        
        internal CimResponseType ResponseType
        {
            set { this.responseType = value; }
        }

        
        internal virtual void OnComplete()
        {
            this.completeEvent.Set();
        }

        
        protected virtual void Block()
        {
            this.completeEvent.Wait();
            this.completeEvent.Dispose();
        }

        #region members

        
        private readonly ManualResetEventSlim completeEvent;

        
        protected CimResponseType responseType;

        #endregion

        #region IDisposable interface
        
        private bool _disposed;

        
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
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    this.completeEvent?.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                _disposed = true;
            }
        }
        #endregion
    }
}
