// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextDoesNotEqualFilterRule : TextEqualsFilterRule
    {
        
        public TextDoesNotEqualFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_DoesNotEqual;
            this.DefaultNullValueEvaluation = true;
        }

        
        public TextDoesNotEqualFilterRule(TextDoesNotEqualFilterRule source)
            : base(source)
        {
        }

        
        protected override bool Evaluate(string data)
        {
            return !base.Evaluate(data);
        }
    }
}
