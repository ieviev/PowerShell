// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class UIPropertyGroupDescription : PropertyGroupDescription, INotifyPropertyChanged
    {
        private ListSortDirection sortDirection = ListSortDirection.Ascending;

        #region Constructors
        
        /// <param name="propertyName">The name of the property that this instance describes.</param>
        /// <param name="displayName">The name displayed to users for this data.</param>
        public UIPropertyGroupDescription(string propertyName, string displayName)
            : this(propertyName, displayName, typeof(string))
        {
            // This constructor just calls another constructor to default the data type to a string.
        }

        
        /// <param name="propertyName">The name of the property that this instance describes.</param>
        /// <param name="displayName">The name displayed to users for this data.</param>
        /// <param name="dataType">The type of the data that this instance describes.</param>
        public UIPropertyGroupDescription(string propertyName, string displayName, Type dataType)
            : base(propertyName)
        {
            this.DataType = dataType;
            this.DisplayName = displayName;
            this.DisplayContent = displayName;

            // Ignore case when sorting and grouping by default \\
            this.StringComparison = StringComparison.CurrentCultureIgnoreCase;
        }
        #endregion Constructors

        #region Properties
        
        public string DisplayName
        {
            get;
            set;
        }

        
        public object DisplayContent
        {
            get;
            set;
        }

        
        public ListSortDirection SortDirection
        {
            get
            {
                return this.sortDirection;
            }

            set
            {
                this.sortDirection = value;
                this.OnPropertyChanged(new PropertyChangedEventArgs("SortDirection"));
            }
        }

        
        public Type DataType
        {
            get;
            set;
        }
        #endregion Properties

        #region Methods
        
        /// <returns>The new sort direction.</returns>
        public ListSortDirection ReverseSortDirection()
        {
            if (this.SortDirection == ListSortDirection.Descending)
            {
                this.SortDirection = ListSortDirection.Ascending;
            }
            else
            {
                this.SortDirection = ListSortDirection.Descending;
            }

            return this.SortDirection;
        }
        #endregion Methods

        #region ToString
        
        /// <returns>A string to represent the instance of this class.</returns>
        public override string ToString()
        {
            return this.PropertyName;
        }
        #endregion ToString
    }
}
