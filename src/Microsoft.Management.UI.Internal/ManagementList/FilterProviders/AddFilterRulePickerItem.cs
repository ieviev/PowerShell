// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class AddFilterRulePickerItem : INotifyPropertyChanged
    {
        private bool isChecked;

        
        public bool IsChecked
        {
            get
            {
                return this.isChecked;
            }

            set
            {
                if (value != this.isChecked)
                {
                    this.isChecked = value;
                    this.NotifyPropertyChanged("IsChecked");
                }
            }
        }

        
        public FilterRulePanelItem FilterRule
        {
            get;
            private set;
        }

        
        /// <param name="filterRule">
        /// The FilterRulePanelItem that will be added to the FilterRulePanel.
        /// </param>
        public AddFilterRulePickerItem(FilterRulePanelItem filterRule)
        {
            this.FilterRule = filterRule;
        }

        
        public event PropertyChangedEventHandler PropertyChanged;

        #region NotifyPropertyChanged

        
        /// <param name="propertyName">
        /// The propertyName which has changed.
        /// </param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler eh = this.PropertyChanged;

            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion NotifyPropertyChanged
    }
}
