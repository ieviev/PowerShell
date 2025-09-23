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

        
        public IsNotEmptyFilterRule(IsNotEmptyFilterRule source)
            : base(source)
        {
        }

        
        public override bool Evaluate(object item)
        {
            return !base.Evaluate(item);
        }
    }
}
