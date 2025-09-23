// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Provider
{
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CmdletProviderAttribute : Attribute
    {
        
        public CmdletProviderAttribute(
            string providerName,
            ProviderCapabilities providerCapabilities)
        {
            // verify parameters

            if (string.IsNullOrEmpty(providerName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(providerName));
            }

            if (providerName.IndexOfAny(_illegalCharacters) != -1)
            {
                throw PSTraceSource.NewArgumentException(
                    nameof(providerName),
                    SessionStateStrings.ProviderNameNotValid,
                    providerName);
            }

            ProviderName = providerName;
            ProviderCapabilities = providerCapabilities;
        }

        private readonly char[] _illegalCharacters = new char[] { ':', '\\', '[', ']', '?', '*' };

        
        public string ProviderName { get; } = string.Empty;

        
        public ProviderCapabilities ProviderCapabilities { get; } = ProviderCapabilities.None;

        #region private data

        #endregion private data
    }

    
    [Flags]
    public enum ProviderCapabilities
    {
        
        None = 0x0,

        
        Include = 0x1,

        
        Exclude = 0x2,

        
        Filter = 0x4,

        
        ExpandWildcards = 0x8,

        
        ShouldProcess = 0x10,

        
        Credentials = 0x20,

        
        Transactions = 0x40,
    }
}
