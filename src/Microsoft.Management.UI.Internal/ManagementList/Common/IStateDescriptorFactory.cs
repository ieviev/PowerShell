// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.UI.Internal
{
    
    /// <typeparam name="T">The type T used by the StateDescriptor.</typeparam>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public interface IStateDescriptorFactory<T>
    {
        
        /// <returns>A new StateDescriptor.</returns>
        StateDescriptor<T> Create();
    }
}
