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

        
        /// <param name="source">The source to initialize from.</param>
        public TextDoesNotEqualFilterRule(TextDoesNotEqualFilterRule source)
            : base(source)
        {
        }

        
        /// <param name="data">
        /// The value to compare against.
        /// </param>
        /// <returns>
        /// Returns true is data does not equal Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(string data)
        {
            return !base.Evaluate(data);
        }
    }
}
