// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public static class FilterRuleExtensions
    {
        
        /// <param name="rule">
        /// The FilterRule to clone.
        /// </param>
        /// <returns>
        /// Returns a deep copy of the passed in rule.
        /// </returns>
        public static FilterRule DeepCopy(this FilterRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            return (FilterRule)rule.DeepClone();
        }
    }
}
