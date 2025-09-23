// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{
    
    internal class WeakEventListener<TEventArgs> : IWeakEventListener where TEventArgs : EventArgs
    {
        private EventHandler<TEventArgs> realHander;

        
        public WeakEventListener(EventHandler<TEventArgs> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            this.realHander = handler;
        }

        
        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            TEventArgs realArgs = (TEventArgs)e;

            this.realHander(sender, realArgs);

            return true;
        }
    }
}
