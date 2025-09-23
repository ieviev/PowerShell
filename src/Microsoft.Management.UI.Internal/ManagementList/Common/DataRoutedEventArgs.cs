// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DataRoutedEventArgs<T> : RoutedEventArgs
    {
        private T data;

        
        public DataRoutedEventArgs(T data, RoutedEvent routedEvent)
        {
            this.data = data;
            this.RoutedEvent = routedEvent;
        }

        
        public T Data
        {
            get { return this.data; }
        }
    }
}
