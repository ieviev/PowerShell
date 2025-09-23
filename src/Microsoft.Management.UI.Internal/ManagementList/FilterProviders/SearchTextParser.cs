// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class SearchTextParser
    {
        #region Public

        
        public SearchTextParser()
        {
            this.SearchableRules = new List<SearchableRule>();
        }

        
        public TextFilterRule FullTextRule
        {
            get;
            set;
        }

        
        /// <typeparam name="T">The type of the text rule within the specified selector rule.</typeparam>
        /// <param name="selectorRule">The rule to include in the search expression.</param>
        /// <returns><c>true</c> if a rule of type <typeparamref name="T"/> was added; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public bool TryAddSearchableRule<T>(SelectorFilterRule selectorRule)
            where T : TextFilterRule
        {
            ArgumentNullException.ThrowIfNull(selectorRule);

            T textRule = selectorRule.AvailableRules.AvailableValues.Find<T>();

            if (textRule != null)
            {
                SearchableRule rule = new SearchableRule("RULE_" + this.SearchableRules.Count.ToString(CultureInfo.InvariantCulture), selectorRule, textRule);

                this.SearchableRules.Add(rule);

                return true;
            }

            return false;
        }

        
        public void ClearSearchableRules()
        {
            this.SearchableRules.Clear();
            this.FullTextRule = null;
        }

        
        /// <param name="text">The text to parse.</param>
        /// <returns>A read-only collection of results.</returns>
        public virtual ReadOnlyCollection<SearchTextParseResult> Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new ReadOnlyCollection<SearchTextParseResult>(new List<SearchTextParseResult>(0));
            }

            string pattern = this.GetPattern();

            MatchCollection matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

            return new ReadOnlyCollection<SearchTextParseResult>(this.ParseMatches(matches));
        }

        #endregion

        #region Protected

        
        protected static readonly string FullTextRuleGroupName = "FULLTEXTRULE";

        
        protected static readonly string ValueGroupName = "VALUE";

        
        protected static readonly string ValuePattern = "((?<" + ValueGroupName + ">\"[^\"]+\"?)|(?<" + ValueGroupName + ">[^\\s\"]+))";

        
        /// <returns>A regular expression pattern used to parse the search text.</returns>
        protected virtual string GetPattern()
        {
            List<string> patterns = new List<string>(this.SearchableRules.Count + 1);

            foreach (SearchableRule rule in this.SearchableRules)
            {
                patterns.Add(rule.Pattern);
            }

            patterns.Add(string.Format(CultureInfo.InvariantCulture, "(?<{0}>){1}", FullTextRuleGroupName, ValuePattern));

            return string.Join("|", patterns.ToArray());
        }

        
        protected List<SearchableRule> SearchableRules
        {
            get;
            private set;
        }

        #endregion

        #region Private

        private List<SearchTextParseResult> ParseMatches(MatchCollection matches)
        {
            List<SearchTextParseResult> searchBoxItems = new List<SearchTextParseResult>(matches.Count);

            foreach (Match match in matches)
            {
                string value = match.Groups[ValueGroupName].Value;

                if (match.Groups[FullTextRuleGroupName].Success && this.FullTextRule != null)
                {
                    TextFilterRule fullTextRule = (TextFilterRule)this.FullTextRule.DeepCopy();
                    fullTextRule.Value.Value = value;

                    searchBoxItems.Add(new SearchTextParseResult(fullTextRule));
                }
                else
                {
                    foreach (SearchableRule rule in this.SearchableRules)
                    {
                        if (match.Groups[rule.UniqueId].Success)
                        {
                            searchBoxItems.Add(new SearchTextParseResult(rule.GetRuleWithValueSet(value)));
                        }
                    }
                }
            }

            return searchBoxItems;
        }

        #endregion

        #region SearchableRule Class

        
        protected class SearchableRule
        {
            private SelectorFilterRule selectorFilterRule;
            private TextFilterRule childRule;

            
            /// <param name="uniqueId">A unique ID for this instance.</param>
            /// <param name="selectorFilterRule">A selector rule that contains <paramref name="childRule"/>.</param>
            /// <param name="childRule">A text rule within <paramref name="selectorFilterRule"/>.</param>
            /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
            public SearchableRule(string uniqueId, SelectorFilterRule selectorFilterRule, TextFilterRule childRule)
            {
                ArgumentNullException.ThrowIfNull(uniqueId);

                ArgumentNullException.ThrowIfNull(selectorFilterRule);

                ArgumentNullException.ThrowIfNull(childRule);

                this.UniqueId = uniqueId;
                this.selectorFilterRule = selectorFilterRule;
                this.childRule = childRule;
                this.Pattern = string.Format(
                    CultureInfo.InvariantCulture,
                    "(?<{0}>){1}\\s*:\\s*{2}",
                    uniqueId,
                    Regex.Escape(selectorFilterRule.DisplayName),
                    SearchTextParser.ValuePattern);
            }

            
            public string UniqueId
            {
                get;
                private set;
            }

            
            public string Pattern
            {
                get;
                private set;
            }

            
            /// <param name="value">The search value.</param>
            /// <returns>A rule with the specified search value set.</returns>
            /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
            public SelectorFilterRule GetRuleWithValueSet(string value)
            {
                ArgumentNullException.ThrowIfNull(value);

                SelectorFilterRule selectorRule = (SelectorFilterRule)this.selectorFilterRule.DeepCopy();
                selectorRule.AvailableRules.SelectedIndex = this.selectorFilterRule.AvailableRules.AvailableValues.IndexOf(this.childRule);
                ((TextFilterRule)selectorRule.AvailableRules.SelectedValue).Value.Value = value;

                return selectorRule;
            }
        }

        #endregion
    }
}
