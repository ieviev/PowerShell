// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    
    internal interface IDeepCloneable
    {
        
        /// <returns>A new object that is a deep copy of the current instance.</returns>
        object DeepClone();
    }
}
