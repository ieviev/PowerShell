// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class PropertyValueSelectorFilterRule<T> : SelectorFilterRule where T : IComparable
    {
        #region Properties

        
        public string PropertyName
        {
            get;
            protected set;
        }

        #endregion Properties

        #region Ctor

        
        /// <param name="propertyName">
        /// Gets the name of the property on the item to evaluate which holds
        /// the real value which should be evaluated.
        /// </param>
        /// <param name="propertyDisplayName">
        /// The display friendly representation of the property name.
        /// </param>
        public PropertyValueSelectorFilterRule(string propertyName, string propertyDisplayName)
            : this(propertyName, propertyDisplayName, FilterRuleCustomizationFactory.FactoryInstance.CreateDefaultFilterRulesForPropertyValueSelectorFilterRule<T>())
        {
            // Empty
        }

        
        /// <param name="propertyName">
        /// The propertyName on the item to evaluate which holds the real
        /// value which should be evaluated.
        /// </param>
        /// <param name="propertyDisplayName">
        /// The display friendly representation of the propertyName.
        /// </param>
        /// <param name="rules">
        /// The collection of available rules.
        /// </param>
        public PropertyValueSelectorFilterRule(string propertyName, string propertyDisplayName, IEnumerable<FilterRule> rules)
        {
            this.PropertyName = propertyName;
            this.DisplayName = propertyDisplayName;

            foreach (FilterRule rule in rules)
            {
                if (rule == null)
                {
                    throw new ArgumentException("A value within rules is null", "rules");
                }

                this.AvailableRules.AvailableValues.Add(rule);
            }

            this.AvailableRules.DisplayNameConverter = new FilterRuleToDisplayNameConverter();
        }

        
        /// <param name="source">The source to initialize from.</param>
        public PropertyValueSelectorFilterRule(PropertyValueSelectorFilterRule<T> source)
            : base(source)
        {
            this.PropertyName = source.PropertyName;
            this.AvailableRules.DisplayNameConverter = new FilterRuleToDisplayNameConverter();
        }

        #endregion Ctor

        #region Public Methods

        
        /// <param name="item">
        /// The item to evaluate.
        /// </param>
        /// <returns>
        /// Returns true if the item matches the filtering criteria, false otherwise.
        /// </returns>
        public override bool Evaluate(object item)
        {
            if (!this.IsValid)
            {
                return false;
            }

            if (item == null)
            {
                return false;
            }

            T propertyValue;
            if (!this.TryGetPropertyValue(item, out propertyValue))
            {
                return false;
            }

            return this.AvailableRules.SelectedValue.Evaluate(propertyValue);
        }

        #endregion Public Methods

        #region Private Methods

        private bool TryGetPropertyValue(object item, out T propertyValue)
        {
            propertyValue = default(T);

            Debug.Assert(item != null, "item not null");

            return FilterRuleCustomizationFactory.FactoryInstance.PropertyValueGetter.TryGetPropertyValue<T>(this.PropertyName, item, out propertyValue);
        }

        #endregion Private Methods
    }
}
