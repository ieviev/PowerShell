// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{
    
    /// <typeparam name="TEventArgs">The EventArgs type for the event.</typeparam>
    internal class WeakEventListener<TEventArgs> : IWeakEventListener where TEventArgs : EventArgs
    {
        private EventHandler<TEventArgs> realHander;

        
        /// <param name="handler">The handler for the event.</param>
        public WeakEventListener(EventHandler<TEventArgs> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            this.realHander = handler;
        }

        
        /// <param name="managerType">The type of the WeakEventManager calling this method.</param>
        /// <param name="sender">Object that originated the event.</param>
        /// <param name="e">Event data.</param>
        /// <returns>
        /// true if the listener handled the event. It is considered an error by the WeakEventManager handling in WPF to register a listener for an event that the listener does not handle. Regardless, the method should return false if it receives an event that it does not recognize or handle.
        /// </returns>
        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            TEventArgs realArgs = (TEventArgs)e;

            this.realHander(sender, realArgs);

            return true;
        }
    }
}
