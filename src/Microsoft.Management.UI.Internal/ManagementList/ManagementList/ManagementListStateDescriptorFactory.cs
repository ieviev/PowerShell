// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ManagementListStateDescriptorFactory : IStateDescriptorFactory<ManagementList>
    {
        
        public StateDescriptor<ManagementList> Create()
        {
            return new ManagementListStateDescriptor();
        }
    }
}
