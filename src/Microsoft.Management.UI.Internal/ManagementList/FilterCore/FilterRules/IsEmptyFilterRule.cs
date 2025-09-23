// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsEmptyFilterRule : FilterRule
    {
        
        public IsEmptyFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_IsEmpty;
        }

        
        /// <param name="source">The source to initialize from.</param>
        public IsEmptyFilterRule(IsEmptyFilterRule source)
            : base(source)
        {
        }

        
        /// <param name="item">The item to evaluate.</param>
        /// <returns>
        /// Returns true if the item is null or if the item is a string
        /// composed of whitespace. False otherwise.
        /// </returns>
        public override bool Evaluate(object item)
        {
            if (item == null)
            {
                return true;
            }

            Type type = item.GetType();

            if (typeof(string) == type)
            {
                return ((string)item).Trim().Length == 0;
            }

            return false;
        }
    }
}
