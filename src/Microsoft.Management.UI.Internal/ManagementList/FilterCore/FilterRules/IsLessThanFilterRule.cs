// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsLessThanFilterRule<T> : SingleValueComparableValueFilterRule<T> where T : IComparable
    {
        
        public IsLessThanFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_LessThanOrEqual;
        }

        
        public IsLessThanFilterRule(IsLessThanFilterRule<T> source)
            : base(source)
        {
        }

        
        protected override bool Evaluate(T item)
        {
            Debug.Assert(this.IsValid, "is valid");

            int result = CustomTypeComparer.Compare<T>(this.Value.GetCastValue(), item);
            return result >= 0;
        }
    }
}
