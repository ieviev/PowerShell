// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class EqualsFilterRule<T> : SingleValueComparableValueFilterRule<T> where T : IComparable
    {
        
        public EqualsFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_Equals;
        }

        
        public EqualsFilterRule(EqualsFilterRule<T> source)
            : base(source)
        {
        }

        
        protected override bool Evaluate(T data)
        {
            Debug.Assert(this.IsValid, "isValid");

            int result = CustomTypeComparer.Compare<T>(this.Value.GetCastValue(), data);
            return result == 0;
        }
    }
}
