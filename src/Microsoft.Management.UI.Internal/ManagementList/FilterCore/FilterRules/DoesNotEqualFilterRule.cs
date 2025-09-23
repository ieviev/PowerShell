// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DoesNotEqualFilterRule<T> : EqualsFilterRule<T> where T : IComparable
    {
        
        public DoesNotEqualFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_DoesNotEqual;
            this.DefaultNullValueEvaluation = true;
        }

        
        public DoesNotEqualFilterRule(DoesNotEqualFilterRule<T> source)
            : base(source)
        {
        }

        
        protected override bool Evaluate(T data)
        {
            return !base.Evaluate(data);
        }
    }
}
