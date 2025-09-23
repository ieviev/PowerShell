// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public interface IPropertyValueGetter
    {
        
        bool TryGetPropertyValue(string propertyName, object value, out object propertyValue);

        
        bool TryGetPropertyValue<T>(string propertyName, object value, out T propertyValue);
    }
}
