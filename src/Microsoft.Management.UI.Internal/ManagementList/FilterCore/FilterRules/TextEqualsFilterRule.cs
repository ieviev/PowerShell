// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextEqualsFilterRule : TextFilterRule
    {
        private static readonly string TextEqualsCharactersRegexPattern = "^{0}$";

        
        public TextEqualsFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_Equals;
        }

        
        /// <param name="source">The source to initialize from.</param>
        public TextEqualsFilterRule(TextEqualsFilterRule source)
            : base(source)
        {
        }

        
        /// <param name="data">
        /// The value to compare against.
        /// </param>
        /// <returns>
        /// Returns true is data equals Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(string data)
        {
            Debug.Assert(this.IsValid, "is valid");

            return this.ExactMatchEvaluate(data, TextEqualsCharactersRegexPattern, TextEqualsCharactersRegexPattern);
        }
    }
}
