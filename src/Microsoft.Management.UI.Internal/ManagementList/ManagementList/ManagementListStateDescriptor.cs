// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ManagementListStateDescriptor : StateDescriptor<ManagementList>
    {
        #region Fields
        private Dictionary<string, ColumnStateDescriptor> columns = new Dictionary<string, ColumnStateDescriptor>();
        private string searchBoxText;
        private List<RuleStateDescriptor> rulesSelected = new List<RuleStateDescriptor>();
        private string sortOrderPropertyName;
        #endregion Fields

        #region Constructors
        
        public ManagementListStateDescriptor()
            : base()
        {
            // empty
        }

        
        public ManagementListStateDescriptor(string name)
            : base(name)
        {
            // empty
        }
        #endregion Constructors

        #region Save/Restore
        
        public override void SaveState(ManagementList subject)
        {
            ArgumentNullException.ThrowIfNull(subject);

            this.SaveColumns(subject);
            this.SaveSortOrder(subject);
            this.SaveRulesSelected(subject);
        }

        
        public override void RestoreState(ManagementList subject)
        {
            this.RestoreState(subject, true);
        }

        
        public void RestoreState(ManagementList subject, bool applyRestoredFilter)
        {
            ArgumentNullException.ThrowIfNull(subject);

            // Clear the sort, otherwise restoring columns and filters may trigger extra sorting \\
            subject.List.ClearSort();

            this.RestoreColumns(subject);
            this.RestoreRulesSelected(subject);

            if (applyRestoredFilter)
            {
                subject.Evaluator.StartFilter();
            }

            // Apply sorting after everything else has been set up \\
            this.RestoreSortOrder(subject);
        }

        #region Verify State Helpers

        private static bool VerifyColumnsSavable(ManagementList subject, RetryActionCallback<ManagementList> callback)
        {
            if (!VerifyColumnsRestorable(subject, callback))
            {
                return false;
            }

            if (subject.List.InnerGrid == null)
            {
                return false;
            }

            return true;
        }

        
        private static bool VerifyColumnsRestorable(ManagementList subject, RetryActionCallback<ManagementList> callback)
        {
            if (WpfHelp.RetryActionAfterLoaded<ManagementList>(subject, callback, subject))
            {
                return false;
            }

            if (WpfHelp.RetryActionAfterLoaded<ManagementList>(subject.List, callback, subject))
            {
                return false;
            }

            if (subject.List == null)
            {
                return false;
            }

            // Columns are not savable/restorable if AutoGenerateColumns is true.
            if (subject.List.AutoGenerateColumns)
            {
                throw new InvalidOperationException("View Manager is not supported when AutoGenerateColumns is set.");
            }

            return true;
        }

        private static bool VerifyRulesSavableAndRestorable(ManagementList subject, RetryActionCallback<ManagementList> callback)
        {
            if (WpfHelp.RetryActionAfterLoaded<ManagementList>(subject, callback, subject))
            {
                return false;
            }

            if (subject.AddFilterRulePicker == null)
            {
                return false;
            }

            if (subject.FilterRulePanel == null)
            {
                return false;
            }

            if (subject.SearchBox == null)
            {
                return false;
            }

            return true;
        }

        #endregion Verify State Helpers

        #region Save/Restore Helpers

        #region Columns

        private void SaveColumns(ManagementList subject)
        {
            if (!VerifyColumnsSavable(subject, this.SaveColumns))
            {
                return;
            }

            this.columns.Clear();

            int i = 0;
            foreach (InnerListColumn ilc in subject.List.InnerGrid.Columns)
            {
                ColumnStateDescriptor csd = CreateColumnStateDescriptor(ilc, true);
                csd.Index = i++;
                this.columns.Add(ilc.DataDescription.PropertyName, csd);
            }

            foreach (InnerListColumn ilc in subject.List.InnerGrid.AvailableColumns)
            {
                if (subject.List.InnerGrid.Columns.Contains(ilc))
                {
                    continue;
                }

                ColumnStateDescriptor csd = CreateColumnStateDescriptor(ilc, false);
                csd.Index = i++;
                this.columns.Add(ilc.DataDescription.PropertyName, csd);
            }
        }

        private void RestoreColumns(ManagementList subject)
        {
            if (!VerifyColumnsRestorable(subject, this.RestoreColumns))
            {
                return;
            }

            this.RestoreColumnsState(subject);
            this.RestoreColumnsOrder(subject);

            subject.List.RefreshColumns();
        }

        
        private void RestoreColumnsState(ManagementList subject)
        {
            ColumnStateDescriptor csd;
            foreach (InnerListColumn ilc in subject.List.Columns)
            {
                if (this.columns.TryGetValue(ilc.DataDescription.PropertyName, out csd))
                {
                    SetColumnSortDirection(ilc, csd.SortDirection);
                    SetColumnIsInUse(ilc, csd.IsInUse || ilc.Required);
                    SetColumnWidth(ilc, csd.Width);
                }
                else
                {
                    SetColumnIsInUse(ilc, ilc.Required);
                }
            }
        }

        private void RestoreColumnsOrder(ManagementList subject)
        {
            // Restore the order of Columns
            // Use the sorted copy to determine what values to swap
            List<InnerListColumn> columnsCopy = new List<InnerListColumn>(subject.List.Columns);
            InnerListColumnOrderComparer ilcc = new InnerListColumnOrderComparer(this.columns);
            columnsCopy.Sort(ilcc);
            Debug.Assert(columnsCopy.Count == subject.List.Columns.Count, "match count");

            Utilities.ResortObservableCollection<InnerListColumn>(
                subject.List.Columns,
                columnsCopy);

            // Restore the order of InnerGrid.Columns
            // Use the sorted copy to determine what values to swap
            columnsCopy.Clear();
            foreach (GridViewColumn gvc in subject.List.InnerGrid.Columns)
            {
                columnsCopy.Add((InnerListColumn)gvc);
            }

            columnsCopy.Sort(ilcc);
            Debug.Assert(columnsCopy.Count == subject.List.InnerGrid.Columns.Count, "match count");

            Utilities.ResortObservableCollection<GridViewColumn>(
                subject.List.InnerGrid.Columns,
                columnsCopy);
        }
        #endregion Columns

        #region Rules

        private void SaveRulesSelected(ManagementList subject)
        {
            if (!VerifyRulesSavableAndRestorable(subject, this.SaveRulesSelected))
            {
                return;
            }

            this.rulesSelected.Clear();
            this.searchBoxText = subject.SearchBox.Text;

            foreach (FilterRulePanelItem item in subject.FilterRulePanel.FilterRulePanelItems)
            {
                RuleStateDescriptor rsd = new RuleStateDescriptor();
                rsd.UniqueName = item.GroupId;
                rsd.Rule = item.Rule.DeepCopy();

                this.rulesSelected.Add(rsd);
            }
        }

        private void RestoreRulesSelected(ManagementList subject)
        {
            if (!VerifyRulesSavableAndRestorable(subject, this.RestoreRulesSelected))
            {
                return;
            }

            subject.Evaluator.StopFilter();

            subject.SearchBox.Text = this.searchBoxText;
            this.AddSelectedRules(subject);
        }

        private void AddSelectedRules(ManagementList subject)
        {
            // Cache values
            Dictionary<string, FilterRulePanelItem> rulesCache = new Dictionary<string, FilterRulePanelItem>();
            foreach (AddFilterRulePickerItem pickerItem in subject.AddFilterRulePicker.ShortcutFilterRules)
            {
                rulesCache.Add(pickerItem.FilterRule.GroupId, pickerItem.FilterRule);
            }

            foreach (AddFilterRulePickerItem pickerItem in subject.AddFilterRulePicker.ColumnFilterRules)
            {
                rulesCache.Add(pickerItem.FilterRule.GroupId, pickerItem.FilterRule);
            }

            subject.FilterRulePanel.Controller.ClearFilterRulePanelItems();
            foreach (RuleStateDescriptor rsd in this.rulesSelected)
            {
                AddSelectedRule(subject, rsd, rulesCache);
            }
        }

        private static void AddSelectedRule(ManagementList subject, RuleStateDescriptor rsd, Dictionary<string, FilterRulePanelItem> rulesCache)
        {
            FilterRulePanelItem item;
            if (rulesCache.TryGetValue(rsd.UniqueName, out item))
            {
                subject.FilterRulePanel.Controller.AddFilterRulePanelItem(new FilterRulePanelItem(rsd.Rule.DeepCopy(), item.GroupId));
            }
        }

        #endregion Rules

        #region Sort Order

        private void SaveSortOrder(ManagementList subject)
        {
            if (!VerifyColumnsRestorable(subject, this.SaveSortOrder))
            {
                return;
            }

            // NOTE : We only support sorting on one property.
            if (subject.List.SortedColumn != null)
            {
                this.sortOrderPropertyName = subject.List.SortedColumn.DataDescription.PropertyName;
            }
            else
            {
                this.sortOrderPropertyName = string.Empty;
            }
        }

        private void RestoreSortOrder(ManagementList subject)
        {
            if (!VerifyColumnsRestorable(subject, this.RestoreSortOrder))
            {
                return;
            }

            subject.List.ClearSort();

            if (!string.IsNullOrEmpty(this.sortOrderPropertyName))
            {
                foreach (InnerListColumn column in subject.List.Columns)
                {
                    if (column.DataDescription.PropertyName == this.sortOrderPropertyName)
                    {
                        subject.List.ApplySort(column, false);
                        break;
                    }
                }
            }
        }

        #endregion Sort Order

        #endregion Save/Restore Helpers

        #region Column Helpers

        private static ColumnStateDescriptor CreateColumnStateDescriptor(InnerListColumn ilc, bool isInUse)
        {
            ColumnStateDescriptor csd = new ColumnStateDescriptor();

            csd.IsInUse = isInUse;
            csd.Width = GetColumnWidth(ilc);
            csd.SortDirection = ilc.DataDescription.SortDirection;

            return csd;
        }

        #region SortDirection

        private static void SetColumnSortDirection(InnerListColumn ilc, ListSortDirection sortDirection)
        {
            ilc.DataDescription.SortDirection = sortDirection;
        }

        #endregion IsInUse

        #region IsInUse

        private static void SetColumnIsInUse(InnerListColumn ilc, bool isInUse)
        {
            ilc.Visible = isInUse;
        }

        #endregion IsInUse

        #region Width

        private static double GetColumnWidth(InnerListColumn ilc)
        {
            return ilc.Visible ? ilc.ActualWidth : ilc.Width;
        }

        private static void SetColumnWidth(GridViewColumn ilc, double width)
        {
            if (!double.IsNaN(width))
            {
                ilc.Width = width;
            }
        }

        #endregion Width

        #endregion Column Helpers
        #endregion Save/Restore

        #region Helper Classes

        internal class ColumnStateDescriptor
        {
            private int index;
            private bool isInUse;
            private ListSortDirection sortDirection;
            private double width;

            
            public int Index
            {
                get { return this.index; }
                set { this.index = value; }
            }

            
            public bool IsInUse
            {
                get { return this.isInUse; }
                set { this.isInUse = value; }
            }

            
            public ListSortDirection SortDirection
            {
                get { return this.sortDirection; }
                set { this.sortDirection = value; }
            }

            
            public double Width
            {
                get { return this.width; }
                set { this.width = value; }
            }
        }

        internal class RuleStateDescriptor
        {
            
            public string UniqueName
            {
                get;
                set;
            }

            
            public FilterRule Rule
            {
                get;
                set;
            }
        }

        internal class InnerListColumnOrderComparer : IComparer<InnerListColumn>
        {
            private Dictionary<string, ColumnStateDescriptor> columns;

            
            public InnerListColumnOrderComparer(Dictionary<string, ColumnStateDescriptor> columns)
            {
                this.columns = columns;
            }

            
            public int Compare(InnerListColumn x, InnerListColumn y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }
                else if (x == null)
                {
                    return -1;
                }
                else if (y == null)
                {
                    return 1;
                }

                ColumnStateDescriptor csdX;
                ColumnStateDescriptor csdY;
                this.columns.TryGetValue(x.DataDescription.PropertyName, out csdX);
                this.columns.TryGetValue(y.DataDescription.PropertyName, out csdY);

                if (csdX == null || csdY == null || (csdX.IsInUse && csdX.IsInUse) == false)
                {
                    return 0;
                }

                return (csdX.Index > csdY.Index) ? 1 : -1;
            }
        }

        #endregion Helper Classes

        #region ToString
        
        public override string ToString()
        {
            return this.Name;
        }
        #endregion ToString
    }
}
