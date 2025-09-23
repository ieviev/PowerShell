// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterRulePanelContentPresenter : ContentPresenter
    {
        
        public FilterRulePanelContentPresenter()
        {
            Binding b = new Binding("FilterRuleTemplateSelector");
            b.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(FilterRulePanel), 1);
            this.SetBinding(ContentTemplateSelectorProperty, b);
        }

        
        public IValueConverter ContentConverter
        {
            get;
            set;
        }

        
        /// <returns>
        /// Returns a DataTemplate.
        /// </returns>
        protected override DataTemplate ChooseTemplate()
        {
            if (this.ContentTemplateSelector == null || this.ContentConverter == null)
            {
                return base.ChooseTemplate();
            }

            object converterContent = this.ContentConverter.Convert(this.Content, typeof(object), null, System.Globalization.CultureInfo.CurrentCulture);
            return this.ContentTemplateSelector.SelectTemplate(converterContent, this);
        }
    }
}
