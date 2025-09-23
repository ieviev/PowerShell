// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextStartsWithFilterRule : TextFilterRule
    {
        private static readonly string TextStartsWithCharactersRegexPattern = "^{0}";
        private static readonly string TextStartsWithWordsRegexPattern = TextStartsWithCharactersRegexPattern + WordBoundaryRegexPattern;

        
        public TextStartsWithFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_TextStartsWith;
        }

        
        public TextStartsWithFilterRule(TextStartsWithFilterRule source)
            : base(source)
        {
        }

        
        protected override bool Evaluate(string data)
        {
            Debug.Assert(this.IsValid, "is valid");

            return this.ExactMatchEvaluate(data, TextStartsWithCharactersRegexPattern, TextStartsWithWordsRegexPattern);
        }
    }
}
