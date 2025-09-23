// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class TextEndsWithFilterRule : TextFilterRule
    {
        private static readonly string TextEndsWithCharactersRegexPattern = "{0}$";
        private static readonly string TextEndsWithWordsRegexPattern = WordBoundaryRegexPattern + TextEndsWithCharactersRegexPattern;

        
        public TextEndsWithFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_TextEndsWith;
        }

        
        public TextEndsWithFilterRule(TextEndsWithFilterRule source)
            : base(source)
        {
        }

        
        protected override bool Evaluate(string data)
        {
            Debug.Assert(this.IsValid, "is valid");

            return this.ExactMatchEvaluate(data, TextEndsWithCharactersRegexPattern, TextEndsWithWordsRegexPattern);
        }
    }
}
