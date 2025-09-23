// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;

namespace System.Management.Automation.Help
{
    
    internal class UpdatableHelpUri
    {
        
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
