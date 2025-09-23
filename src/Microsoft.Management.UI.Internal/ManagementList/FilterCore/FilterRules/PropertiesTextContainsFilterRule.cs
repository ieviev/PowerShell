// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class PropertiesTextContainsFilterRule : TextFilterRule
    {
        private static readonly string TextContainsCharactersRegexPattern = "{0}";
        private static readonly string TextContainsWordsRegexPattern = WordBoundaryRegexPattern + TextContainsCharactersRegexPattern + WordBoundaryRegexPattern;

        private Regex cachedRegex;

        
        public PropertiesTextContainsFilterRule()
        {
            this.PropertyNames = new List<string>();
            this.EvaluationResultInvalidated += this.PropertiesTextContainsFilterRule_EvaluationResultInvalidated;
        }

        
        public PropertiesTextContainsFilterRule(PropertiesTextContainsFilterRule source)
            : base(source)
        {
            this.PropertyNames = new List<string>(source.PropertyNames);
            this.EvaluationResultInvalidated += this.PropertiesTextContainsFilterRule_EvaluationResultInvalidated;
        }

        
        public ICollection<string> PropertyNames
        {
            get;
            private set;
        }

        
        public override bool Evaluate(object item)
        {
            if (item == null)
            {
                return false;
            }

            if (!this.IsValid)
            {
                return false;
            }

            foreach (string propertyName in this.PropertyNames)
            {
                object propertyValue;

                if (!FilterRuleCustomizationFactory.FactoryInstance.PropertyValueGetter.TryGetPropertyValue(propertyName, item, out propertyValue))
                {
                    continue;
                }

                if (propertyValue != null)
                {
                    string data = propertyValue.ToString();

                    if (this.Evaluate(data))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        
        protected override bool Evaluate(string data)
        {
            if (this.cachedRegex == null)
            {
                this.UpdateCachedRegex();
            }

            return this.cachedRegex.IsMatch(data);
        }

        
        protected virtual void OnEvaluationResultInvalidated()
        {
            this.UpdateCachedRegex();
        }

        
        private void UpdateCachedRegex()
        {
            if (this.IsValid)
            {
                var parsedPattern = this.GetRegexPattern(TextContainsCharactersRegexPattern, TextContainsWordsRegexPattern);

                this.cachedRegex = new Regex(parsedPattern, this.GetRegexOptions());
            }
        }

        private void PropertiesTextContainsFilterRule_EvaluationResultInvalidated(object sender, EventArgs e)
        {
            this.OnEvaluationResultInvalidated();
        }
    }
}
