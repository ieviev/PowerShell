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

        
        /// <param name="source">The source to initialize from.</param>
        public TextDoesNotContainFilterRule(TextDoesNotContainFilterRule source)
            : base(source)
        {
        }

        
        /// <param name="data">
        /// The data to compare with.
        /// </param>
        /// <returns>
        /// Returns true if data does not contain Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(string data)
        {
            return !base.Evaluate(data);
        }
    }
}
