// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    using System;

    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class PropertyChangedEventArgs<T> : EventArgs
    {
        
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
