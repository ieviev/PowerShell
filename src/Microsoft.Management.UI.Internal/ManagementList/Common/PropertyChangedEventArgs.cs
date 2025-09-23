// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    using System;

    
    /// <typeparam name="T">The property type.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class PropertyChangedEventArgs<T> : EventArgs
    {
        
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new, current, value.</param>
        public PropertyChangedEventArgs(T oldValue, T newValue)
        {
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        
        public T OldValue
        {
            get;
            private set;
        }

        
        public T NewValue
        {
            get;
            private set;
        }
    }
}
