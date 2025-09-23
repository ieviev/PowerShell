// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class TextFilterRule : SingleValueComparableValueFilterRule<string>
    {
        
        protected static readonly string WordBoundaryRegexPattern = @"(^|$|\W|\b)";

        private bool ignoreCase;
        private bool cultureInvariant;

        
        public bool IgnoreCase
        {
            get
            {
                return this.ignoreCase;
            }

            set
            {
                this.ignoreCase = value;

                this.NotifyEvaluationResultInvalidated();
            }
        }

        
        public bool CultureInvariant
        {
            get
            {
                return this.cultureInvariant;
            }

            set
            {
                this.cultureInvariant = value;

                this.NotifyEvaluationResultInvalidated();
            }
        }

        
        protected TextFilterRule()
        {
            this.IgnoreCase = true;
            this.CultureInvariant = false;
        }

        
        protected TextFilterRule(TextFilterRule source)
            : base(source)
        {
            this.IgnoreCase = source.IgnoreCase;
            this.CultureInvariant = source.CultureInvariant;
        }

        
        protected internal string GetParsedValue(out bool evaluateAsExactMatch)
        {
            var parsedValue = this.Value.GetCastValue();

            // Consider it an exact-match value if it starts with a quote; trailing quotes and other requirements can be added later if need be \\
            evaluateAsExactMatch = parsedValue.StartsWith("\"", StringComparison.Ordinal);

            // If it's an exact-match value, remove quotes and use the exact-match pattern \\
            if (evaluateAsExactMatch)
            {
                parsedValue = parsedValue.Replace("\"", string.Empty);
            }

            return parsedValue;
        }

        
        protected internal string GetRegexPattern(string pattern, string exactMatchPattern)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            ArgumentNullException.ThrowIfNull(exactMatchPattern);

            Debug.Assert(this.IsValid, "is valid");

            bool evaluateAsExactMatch;
            string value = this.GetParsedValue(out evaluateAsExactMatch);

            if (evaluateAsExactMatch)
            {
                pattern = exactMatchPattern;
            }

            value = Regex.Escape(value);

            // Format the pattern using the specified data \\
            return string.Format(CultureInfo.InvariantCulture, pattern, value);
        }

        
        protected internal RegexOptions GetRegexOptions()
        {
            RegexOptions options = RegexOptions.None;

            if (this.IgnoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            if (this.CultureInvariant)
            {
                options |= RegexOptions.CultureInvariant;
            }

            return options;
        }

        
        protected internal bool ExactMatchEvaluate(string data, string pattern, string exactMatchPattern)
        {
            Debug.Assert(this.IsValid, "is valid");

            var parsedPattern = this.GetRegexPattern(pattern, exactMatchPattern);
            var options = this.GetRegexOptions();

            return Regex.IsMatch(data, parsedPattern, options);
        }
    }
}
