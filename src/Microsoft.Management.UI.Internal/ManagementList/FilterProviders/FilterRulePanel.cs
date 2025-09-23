// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class FilterRulePanel : Control, IFilterExpressionProvider
    {
        #region Properties

        #region Filter Rule Panel Items

        
        public ReadOnlyCollection<FilterRulePanelItem> FilterRulePanelItems
        {
            get
            {
                return this.Controller.FilterRulePanelItems;
            }
        }

        #endregion Filter Rule Panel Items

        #region Filter Expression

        
        public FilterExpressionNode FilterExpression
        {
            get
            {
                return this.Controller.FilterExpression;
            }
        }

        #endregion Filter Expression

        #region Controller

        private FilterRulePanelController controller = new FilterRulePanelController();

        
        public FilterRulePanelController Controller
        {
            get
            {
                return this.controller;
            }
        }

        #endregion Controller

        #region Filter Rule Template Selector

        private FilterRuleTemplateSelector filterRuleTemplateSelector;

        
        public DataTemplateSelector FilterRuleTemplateSelector
        {
            get
            {
                return this.filterRuleTemplateSelector;
            }
        }

        #endregion Filter Rule Template Selector

        
        public bool HasFilterExpression
        {
            get
            {
                return this.Controller.HasFilterExpression;
            }
        }

        #endregion Properties

        #region Events

        
        public event EventHandler FilterExpressionChanged;

        #endregion

        #region Ctor

        
        public FilterRulePanel()
        {
            this.InitializeTemplates();

            this.Controller.FilterExpressionChanged += this.Controller_FilterExpressionChanged;
        }

        #endregion Ctor

        #region Public Methods

        #region Content Templates

        
        public void AddFilterRulePanelItemContentTemplate(Type type, DataTemplate dataTemplate)
        {
            ArgumentNullException.ThrowIfNull(type);

            ArgumentNullException.ThrowIfNull(dataTemplate);

            this.filterRuleTemplateSelector.TemplateDictionary.Add(new KeyValuePair<Type, DataTemplate>(type, dataTemplate));
        }

        
        public void RemoveFilterRulePanelItemContentTemplate(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            this.filterRuleTemplateSelector.TemplateDictionary.Remove(type);
        }

        
        public bool TryGetContentTemplate(Type type, out DataTemplate dataTemplate)
        {
            dataTemplate = null;
            return this.filterRuleTemplateSelector.TemplateDictionary.TryGetValue(type, out dataTemplate);
        }

        
        public void ClearContentTemplates()
        {
            this.filterRuleTemplateSelector.TemplateDictionary.Clear();
        }

        #endregion Content Templates

        #region Notify Filter Expression Changed

        
        protected virtual void NotifyFilterExpressionChanged()
        {
            EventHandler eh = this.FilterExpressionChanged;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

        private void Controller_FilterExpressionChanged(object sender, EventArgs e)
        {
            this.NotifyFilterExpressionChanged();
        }

        #endregion Notify Filter Expression Changed

        #endregion Public Methods

        #region Private Methods

        #region Add Rules Command Callback

        partial void OnAddRulesExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            Debug.Assert(e != null, "not null");

            if (e.Parameter == null)
            {
                throw new ArgumentException("e.Parameter is null.", "e");
            }

            List<FilterRulePanelItem> itemsToAdd = new List<FilterRulePanelItem>();

            IList selectedItems = (IList)e.Parameter;
            foreach (object item in selectedItems)
            {
                FilterRulePanelItem newItem = item as FilterRulePanelItem;
                if (newItem == null)
                {
                    throw new ArgumentException(
                        "e.Parameter contains a value which is not a valid FilterRulePanelItem object.",
                        "e");
                }

                itemsToAdd.Add(newItem);
            }

            foreach (FilterRulePanelItem item in itemsToAdd)
            {
                this.AddFilterRuleInternal(item);
            }
        }

        #endregion Add Rules Command Callback

        #region Remove Rule Command Callback

        partial void OnRemoveRuleExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            Debug.Assert(e != null, "not null");

            if (e.Parameter == null)
            {
                throw new ArgumentException("e.Parameter is null.", "e");
            }

            FilterRulePanelItem item = e.Parameter as FilterRulePanelItem;
            if (item == null)
            {
                throw new ArgumentException("e.Parameter is not a valid FilterRulePanelItem object.", "e");
            }

            this.RemoveFilterRuleInternal(item);
        }

        #endregion Remove Rule Command Callback

        #region InitializeTemplates

        private void InitializeTemplates()
        {
            this.filterRuleTemplateSelector = new FilterRuleTemplateSelector();

            this.InitializeTemplatesForInputTypes();

            List<KeyValuePair<Type, string>> defaultTemplates = new List<KeyValuePair<Type, string>>()
            {
                new KeyValuePair<Type, string>(typeof(SelectorFilterRule), "CompositeRuleTemplate"),
                new KeyValuePair<Type, string>(typeof(SingleValueComparableValueFilterRule<>), "ComparableValueRuleTemplate"),
                new KeyValuePair<Type, string>(typeof(IsEmptyFilterRule), "NoInputTemplate"),
                new KeyValuePair<Type, string>(typeof(IsNotEmptyFilterRule), "NoInputTemplate"),
                new KeyValuePair<Type, string>(typeof(FilterRulePanelItemType), "FilterRulePanelGroupItemTypeTemplate"),
                new KeyValuePair<Type, string>(typeof(ValidatingValue<>), "ValidatingValueTemplate"),
                new KeyValuePair<Type, string>(typeof(ValidatingSelectorValue<>), "ValidatingSelectorValueTemplate"),
                new KeyValuePair<Type, string>(typeof(IsBetweenFilterRule<>), "IsBetweenRuleTemplate"),
                new KeyValuePair<Type, string>(typeof(object), "CatchAllTemplate")
            };

            defaultTemplates.ForEach(templateInfo => this.AddFilterRulePanelItemContentTemplate(templateInfo.Key, templateInfo.Value));
        }

        private void InitializeTemplatesForInputTypes()
        {
            List<Type> inputTypes = new List<Type>()
            {
                typeof(sbyte),
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(char),
                typeof(Single),
                typeof(double),
                typeof(decimal),
                typeof(bool),
                typeof(Enum),
                typeof(DateTime),
                typeof(string)
            };

            inputTypes.ForEach(type => this.AddFilterRulePanelItemContentTemplate(type, "InputValueTemplate"));
        }

        private void AddFilterRulePanelItemContentTemplate(Type type, string resourceName)
        {
            Debug.Assert(type != null, "not null");
            Debug.Assert(!string.IsNullOrEmpty(resourceName), "not null");

            var templateInfo = new ComponentResourceKey(typeof(FilterRulePanel), resourceName);

            DataTemplate template = (DataTemplate)this.TryFindResource(templateInfo);
            Debug.Assert(template != null, "not null");

            this.AddFilterRulePanelItemContentTemplate(type, template);
        }

        #endregion InitializeTemplates

        #region Add/Remove FilterRules to Controller

        private void AddFilterRuleInternal(FilterRulePanelItem item)
        {
            Debug.Assert(item != null, "not null");

            FilterRulePanelItem newItem = new FilterRulePanelItem(item.Rule.DeepCopy(), item.GroupId);
            this.Controller.AddFilterRulePanelItem(newItem);
        }

        private void RemoveFilterRuleInternal(FilterRulePanelItem item)
        {
            Debug.Assert(item != null, "not null");
            this.Controller.RemoveFilterRulePanelItem(item);
        }

        #endregion Add/Remove FilterRules to Controller

        #endregion Private Methods
    }
}
