// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;

namespace System.Management.Automation.Help
{
    
    internal class UpdatableHelpUri
    {
        
        /// <param name="moduleName">Module name.</param>
        /// <param name="moduleGuid">Module guid.</param>
        /// <param name="culture">UI culture.</param>
        /// <param name="resolvedUri">Resolved URI.</param>
        internal UpdatableHelpUri(string moduleName, Guid moduleGuid, CultureInfo culture, string resolvedUri)
        {
            Debug.Assert(!string.IsNullOrEmpty(moduleName));
            Debug.Assert(!string.IsNullOrEmpty(resolvedUri));

            ModuleName = moduleName;
            ModuleGuid = moduleGuid;
            Culture = culture;
            ResolvedUri = resolvedUri;
        }

        
        internal string ModuleName { get; }

        
        internal Guid ModuleGuid { get; }

        
        internal CultureInfo Culture { get; }

        
        internal string ResolvedUri { get; }
    }
}
