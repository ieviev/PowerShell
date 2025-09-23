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

        
        public abstract ICollection<FilterRule> CreateDefaultFilterRulesForPropertyValueSelectorFilterRule<T>() where T : IComparable;

        
        public abstract void TransferValues(FilterRule oldRule, FilterRule newRule);

        
        public abstract void ClearValues(FilterRule rule);

        
        public abstract string GetErrorMessageForInvalidValue(string value, Type typeToParseTo);
    }
}
