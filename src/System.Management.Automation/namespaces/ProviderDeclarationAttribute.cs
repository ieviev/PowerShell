// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Provider
{
    
    /// <remarks>
    /// The class must be derived from System.Management.Automation.Provider.CmdletProvider to
    /// be recognized by the runspace.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CmdletProviderAttribute : Attribute
    {
        
        /// <param name="providerName">
        /// The provider name.
        /// </param>
        /// <param name="providerCapabilities">
        /// An enumeration of the capabilities that the provider implements beyond the
        /// default capabilities that are required.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="providerName"/> is null or empty.
        /// </exception>
        /// <exception cref="PSArgumentException">
        /// If <paramref name="providerName"/> contains any of the following characters: \ [ ] ? * :
        /// </exception>
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
