// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class SearchTextParseResult
    {
        
        /// <param name="rule">The rule that resulted from parsing the search text.</param>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
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
