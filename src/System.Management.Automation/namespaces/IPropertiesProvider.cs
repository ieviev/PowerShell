// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

#nullable enable
namespace System.Management.Automation.Provider
{
    #region IPropertyCmdletProvider

    
    public interface IPropertyCmdletProvider
    {
        
        void GetProperty(
            string path,
            Collection<string>? providerSpecificPickList);

        
        object? GetPropertyDynamicParameters(
            string path,
            Collection<string>? providerSpecificPickList);

        
        void SetProperty(
            string path,
            PSObject propertyValue);

        
        object? SetPropertyDynamicParameters(
            string path,
            PSObject propertyValue);

        
        void ClearProperty(
            string path,
            Collection<string> propertyToClear);

        
        object? ClearPropertyDynamicParameters(
            string path,
            Collection<string> propertyToClear);
    }

    #endregion IPropertyCmdletProvider
}
