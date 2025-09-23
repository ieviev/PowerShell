// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextDoesNotContainFilterRule : TextContainsFilterRule
    {
        
        public TextDoesNotContainFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_DoesNotContain;
            this.DefaultNullValueEvaluation = true;
        }

        
        public TextDoesNotContainFilterRule(TextDoesNotContainFilterRule source)
            : base(source)
        {
        }

        
        protected override bool Evaluate(string data)
        {
            return !base.Evaluate(data);
        }
    }
}
