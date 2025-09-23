// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ManagementListStateDescriptorFactory : IStateDescriptorFactory<ManagementList>
    {
        
        /// <returns>A new ManagementListStateDescriptor.</returns>
        public StateDescriptor<ManagementList> Create()
        {
            return new ManagementListStateDescriptor();
        }
    }
}
