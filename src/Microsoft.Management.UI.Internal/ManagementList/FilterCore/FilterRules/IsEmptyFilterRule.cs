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

        
        public IsEmptyFilterRule(IsEmptyFilterRule source)
            : base(source)
        {
        }

        
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
