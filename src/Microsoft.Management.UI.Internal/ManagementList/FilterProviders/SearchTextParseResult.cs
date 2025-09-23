// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class SearchTextParseResult
    {
        
        public SearchTextParseResult(FilterRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            this.FilterRule = rule;
        }

        
        public FilterRule FilterRule
        {
            get;
            private set;
        }
    }
}
