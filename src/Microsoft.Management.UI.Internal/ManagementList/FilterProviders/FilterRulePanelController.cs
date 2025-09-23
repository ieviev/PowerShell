// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterRulePanelController : IFilterExpressionProvider
    {
        #region Properties

        private ObservableCollection<FilterRulePanelItem> filterRulePanelItems;
        private ReadOnlyObservableCollection<FilterRulePanelItem> readOnlyFilterRulePanelItems;

        
        public ReadOnlyCollection<FilterRulePanelItem> FilterRulePanelItems
        {
            get { return this.readOnlyFilterRulePanelItems; }
        }

        
        public FilterExpressionNode FilterExpression
        {
            get
            {
                return this.CreateFilterExpression();
            }
        }

        
        public bool HasFilterExpression
        {
            get
            {
                return this.FilterExpression != null;
            }
        }

        #endregion Properties

        #region Events

        
        public event EventHandler FilterExpressionChanged;

        #endregion Events

        #region Ctor

        
        public FilterRulePanelController()
        {
            this.filterRulePanelItems =
                new ObservableCollection<FilterRulePanelItem>();
            this.readOnlyFilterRulePanelItems =
                new ReadOnlyObservableCollection<FilterRulePanelItem>(this.filterRulePanelItems);
        }

        #endregion Ctor

        #region Public Methods

        
        public void AddFilterRulePanelItem(FilterRulePanelItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            int insertionIndex = this.GetInsertionIndex(item);
            this.filterRulePanelItems.Insert(insertionIndex, item);

            item.Rule.EvaluationResultInvalidated += this.Rule_EvaluationResultInvalidated;

            this.UpdateFilterRulePanelItemTypes();

            this.NotifyFilterExpressionChanged();
        }

        private void Rule_EvaluationResultInvalidated(object sender, EventArgs e)
        {
            this.NotifyFilterExpressionChanged();
        }

        
        public void RemoveFilterRulePanelItem(FilterRulePanelItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            item.Rule.EvaluationResultInvalidated -= this.Rule_EvaluationResultInvalidated;

            this.filterRulePanelItems.Remove(item);
            this.UpdateFilterRulePanelItemTypes();

            this.NotifyFilterExpressionChanged();
        }

        
        public void ClearFilterRulePanelItems()
        {
            this.filterRulePanelItems.Clear();

            this.NotifyFilterExpressionChanged();
        }

        #endregion Public Methods

        #region Private Methods

        #region CreateFilterExpression

        private FilterExpressionNode CreateFilterExpression()
        {
            List<FilterExpressionNode> groupNodes = new List<FilterExpressionNode>();

            for (int i = 0; i < this.filterRulePanelItems.Count;)
            {
                int endIndex = this.GetExclusiveEndIndexForGroupStartingAt(i);

                FilterExpressionOrOperatorNode operatorOrNode = this.CreateFilterExpressionForGroup(i, endIndex);
                if (operatorOrNode.Children.Count > 0)
                {
                    groupNodes.Add(operatorOrNode);
                }

                i = endIndex;
            }

            if (groupNodes.Count == 0)
            {
                return null;
            }

            return new FilterExpressionAndOperatorNode(groupNodes);
        }

        private int GetExclusiveEndIndexForGroupStartingAt(int startIndex)
        {
            Debug.Assert(this.filterRulePanelItems.Count > 0, "greater than 0");
            Debug.Assert(startIndex >= 0, "greater than or equal to 0");

            int i = startIndex;
            for (; i < this.filterRulePanelItems.Count; i++)
            {
                if (i == startIndex)
                {
                    continue;
                }

                string currentId = this.filterRulePanelItems[i].GroupId;
                string previousId = this.filterRulePanelItems[i - 1].GroupId;

                if (!currentId.Equals(previousId, StringComparison.Ordinal))
                {
                    break;
                }
            }

            return i;
        }

        private FilterExpressionOrOperatorNode CreateFilterExpressionForGroup(int startIndex, int endIndex)
        {
            Debug.Assert(this.filterRulePanelItems.Count > 0, "greater than 0");
            Debug.Assert(startIndex >= 0, "greater than or equal to 0");
            Debug.Assert(this.filterRulePanelItems.Count >= endIndex, "greater than or equal to endIndex");

            FilterExpressionOrOperatorNode groupNode = new FilterExpressionOrOperatorNode();
            for (int i = startIndex; i < endIndex; i++)
            {
                FilterRule rule = this.filterRulePanelItems[i].Rule;
                if (rule.IsValid)
                {
                    groupNode.Children.Add(new FilterExpressionOperandNode(rule.DeepCopy()));
                }
            }

            return groupNode;
        }

        #endregion CreateFilterExpression

        #region Add/Remove Item Helpers

        private int GetInsertionIndex(FilterRulePanelItem item)
        {
            Debug.Assert(item != null, "not null");

            for (int i = this.filterRulePanelItems.Count - 1; i >= 0; i--)
            {
                string uniqueId = this.filterRulePanelItems[i].GroupId;
                if (uniqueId.Equals(item.GroupId, StringComparison.Ordinal))
                {
                    return i + 1;
                }
            }

            return this.filterRulePanelItems.Count;
        }

        private void UpdateFilterRulePanelItemTypes()
        {
            if (this.filterRulePanelItems.Count > 0)
            {
                this.filterRulePanelItems[0].ItemType = FilterRulePanelItemType.FirstHeader;
            }

            for (int i = 1; i < this.filterRulePanelItems.Count; i++)
            {
                string currentId = this.filterRulePanelItems[i].GroupId;
                string previousId = this.filterRulePanelItems[i - 1].GroupId;

                if (!currentId.Equals(previousId, StringComparison.Ordinal))
                {
                    this.filterRulePanelItems[i].ItemType = FilterRulePanelItemType.Header;
                }
                else
                {
                    this.filterRulePanelItems[i].ItemType = FilterRulePanelItemType.Item;
                }
            }
        }

        #endregion Add/Remove Item Helpers

        #region Notify Filter Expression Changed

        
        protected virtual void NotifyFilterExpressionChanged()
        {
            EventHandler eh = this.FilterExpressionChanged;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

        #endregion Notify Filter Expression Changed

        #endregion Private Methods
    }
}
