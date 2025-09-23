// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Microsoft.Management.UI.Internal
{
    
    [ContentProperty("AvailableColumns")]
    public class InnerListGridView : GridView
    {
        
        private bool canChangeColumns = false;

        
        public InnerListGridView()
            : this(new ObservableCollection<InnerListColumn>())
        {
        }

        
        internal InnerListGridView(ObservableCollection<InnerListColumn> availableColumns)
        {
            ArgumentNullException.ThrowIfNull(availableColumns);

            // Setting the AvailableColumns property won't trigger CollectionChanged, so we have to do it manually \\
            this.AvailableColumns = availableColumns;
            this.AvailableColumns_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, availableColumns));

            availableColumns.CollectionChanged += this.AvailableColumns_CollectionChanged;
            this.Columns.CollectionChanged += this.Columns_CollectionChanged;
        }

        
        internal ObservableCollection<InnerListColumn> AvailableColumns
        {
            get;
            private set;
        }

        
        public void ReleaseReferences()
        {
            this.AvailableColumns.CollectionChanged -= this.AvailableColumns_CollectionChanged;
            this.Columns.CollectionChanged -= this.Columns_CollectionChanged;

            foreach (InnerListColumn column in this.AvailableColumns)
            {
                // Unsubscribe from the column's change events \\
                ((INotifyPropertyChanged)column).PropertyChanged -= this.Column_PropertyChanged;

                // If the column is shown, store its last width before releasing \\
                if (column.Visible)
                {
                    column.Width = column.ActualWidth;
                }
            }

            // Remove the columns so they can be added to the next GridView \\
            this.Columns.Clear();
        }

        
        internal void PopulateColumns(System.Collections.IEnumerable newValue)
        {
            if (newValue == null)
            {
                return; // No elements, so we can't populate
            }

            IEnumerator newValueEnumerator = newValue.GetEnumerator();
            if (!newValueEnumerator.MoveNext())
            {
                return; // No first element, so we can't populate
            }

            object first = newValueEnumerator.Current;
            if (first == null)
            {
                return;
            }

            Debug.Assert(this.AvailableColumns.Count == 0, "AvailabeColumns should be empty at this point");

            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(first);
            foreach (PropertyDescriptor property in properties)
            {
                UIPropertyGroupDescription dataDescription = new UIPropertyGroupDescription(property.Name, property.Name, property.PropertyType);
                InnerListColumn column = new InnerListColumn(dataDescription);
                this.AvailableColumns.Add(column);
            }
        }

        
        internal void OnColumnPicker(object sender, RoutedEventArgs e)
        {
            ColumnPicker columnPicker = new ColumnPicker(
                this.Columns, this.AvailableColumns);
            columnPicker.Owner = Window.GetWindow((DependencyObject)sender);

            bool? retval = columnPicker.ShowDialog();
            if (retval != true)
            {
                return;
            }

            this.canChangeColumns = true;
            try
            {
                this.Columns.Clear();
                ObservableCollection<InnerListColumn> newColumns = columnPicker.SelectedColumns;
                Debug.Assert(newColumns != null, "SelectedColumns not found");
                foreach (InnerListColumn column in newColumns)
                {
                    Debug.Assert(column.Visible, "is visible");

                    // 185977: ML InnerListGridView.PopulateColumns(): Always set Width on new columns
                    // Workaround to GridView issue suggested by Ben Carter
                    // Issue: Once a column has been added to a GridView
                    //   and then removed, auto-sizing does not work
                    //   after it is added back.
                    // Solution: Remove the column, change the width,
                    //   add the column back, then change the width back.
                    double width = column.Width;
                    column.Width = 0d;
                    this.Columns.Add(column);
                    column.Width = width;
                }
            }
            finally
            {
                this.canChangeColumns = false;
            }
        }

        
        private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                // Move happens in the GUI drag and drop operation, so we have to allow it.
                case NotifyCollectionChangedAction.Move:
                    return;

                // default means all other operations (Add, Move, Replace and Reset) those are reserved.
                // only we should do it, as we keep AvailableColumns in sync with columns
                default:
                    if (!this.canChangeColumns)
                    {
                        throw new NotSupportedException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                InvariantResources.CannotModified,
                                InvariantResources.Columns,
                                "AvailableColumns"));
                    }

                    break;
            }
        }

        
        private void AvailableColumns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.AddOrRemoveNotifications(e);
            this.SynchronizeColumns();
        }

        
        private void AddOrRemoveNotifications(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Move)
            {
                if (e.OldItems != null)
                {
                    foreach (InnerListColumn oldColumn in e.OldItems)
                    {
                        ((INotifyPropertyChanged)oldColumn).PropertyChanged -= this.Column_PropertyChanged;
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (InnerListColumn newColumn in e.NewItems)
                    {
                        ((INotifyPropertyChanged)newColumn).PropertyChanged += this.Column_PropertyChanged;
                    }
                }
            }
        }

        
        private void SynchronizeColumns()
        {
            this.canChangeColumns = true;
            try
            {
                // Add to listViewColumns all Visible columns in availableColumns not already in listViewColumns
                foreach (InnerListColumn column in this.AvailableColumns)
                {
                    if (column.Visible && !this.Columns.Contains(column))
                    {
                        this.Columns.Add(column);
                    }
                }

                // Remove all columns which are not visible or removed from Available columns.
                for (int i = this.Columns.Count - 1; i >= 0; i--)
                {
                    InnerListColumn listViewColumn = (InnerListColumn)this.Columns[i];
                    if (!listViewColumn.Visible || !this.AvailableColumns.Contains(listViewColumn))
                    {
                        this.Columns.RemoveAt(i);
                    }
                }
            }
            finally
            {
                this.canChangeColumns = false;
            }
        }

        
        private void Column_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == InnerListColumn.VisibleProperty.Name)
            {
                this.SynchronizeColumns();
            }
        }
    }
}
