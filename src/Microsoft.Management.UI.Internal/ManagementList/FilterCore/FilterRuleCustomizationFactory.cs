// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class FilterRuleCustomizationFactory
    {
        private static FilterRuleCustomizationFactory factoryInstance;

        
        public static FilterRuleCustomizationFactory FactoryInstance
        {
            get
            {
                Debug.Assert(factoryInstance != null, "factoryInstance not null");
                return factoryInstance;
            }

            set
            {
                ArgumentNullException.ThrowIfNull(value);

                factoryInstance = value;
            }
        }

        
        static FilterRuleCustomizationFactory()
        {
            FactoryInstance = new DefaultFilterRuleCustomizationFactory();
        }

        
        public abstract IPropertyValueGetter PropertyValueGetter
        {
            get;
            set;
        }

        
        /// <typeparam name="T">
        /// The type used to determine what rules to include.
        /// </typeparam>
        /// <returns>
        /// Returns a collection of FilterRules.
        /// </returns>
        public abstract ICollection<FilterRule> CreateDefaultFilterRulesForPropertyValueSelectorFilterRule<T>() where T : IComparable;

        
        /// <param name="oldRule">
        /// The old filter rule.
        /// </param>
        /// <param name="newRule">
        /// The new filter rule.
        /// </param>
        public abstract void TransferValues(FilterRule oldRule, FilterRule newRule);

        
        /// <param name="rule">
        /// The rule to clear.
        /// </param>
        public abstract void ClearValues(FilterRule rule);

        
        /// <param name="value">
        /// The value entered by the user.
        /// </param>
        /// <param name="typeToParseTo">
        /// The desired type to parse value to.
        /// </param>
        /// <returns>
        /// An error message to a user to explain how they can
        /// enter a valid value.
        /// </returns>
        public abstract string GetErrorMessageForInvalidValue(string value, Type typeToParseTo);
    }
}
