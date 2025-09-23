// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterRuleTemplateSelector : DataTemplateSelector
    {
        private Dictionary<Type, DataTemplate> templateDictionary = new Dictionary<Type, DataTemplate>();

        
        public IDictionary<Type, DataTemplate> TemplateDictionary
        {
            get { return this.templateDictionary; }
        }

        
        public override DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            if (item == null)
            {
                return base.SelectTemplate(item, container);
            }

            Type type = item as Type ?? item.GetType();

            DataTemplate template;

            do
            {
                if (type.IsGenericType)
                {
                    type = type.GetGenericTypeDefinition();
                }

                if (this.TemplateDictionary.TryGetValue(type, out template))
                {
                    return template;
                }

                type = type.BaseType;
            }
            while (type != null);

            return base.SelectTemplate(item, container);
        }
    }
}
