// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsNotEmptyFilterRule : IsEmptyFilterRule
    {
        
        public IsNotEmptyFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_IsNotEmpty;
        }

        
        /// <param name="source">The source to initialize from.</param>
        public IsNotEmptyFilterRule(IsNotEmptyFilterRule source)
            : base(source)
        {
        }

        
        /// <param name="item">The item to evaluate.</param>
        /// <returns>
        /// Returns false if the item is null or if the item is a string
        /// composed of whitespace. True otherwise.
        /// </returns>
        public override bool Evaluate(object item)
        {
            return !base.Evaluate(item);
        }
    }
}
