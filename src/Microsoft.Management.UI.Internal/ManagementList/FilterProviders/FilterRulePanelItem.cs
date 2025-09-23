// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterRulePanelItem : INotifyPropertyChanged
    {
        #region Properties

        
        public FilterRule Rule
        {
            get;
            private set;
        }

        
        public string GroupId
        {
            get;
            private set;
        }

        private FilterRulePanelItemType itemType = FilterRulePanelItemType.Header;

        
        public FilterRulePanelItemType ItemType
        {
            get
            {
                return this.itemType;
            }

            protected internal set
            {
                if (value == this.itemType)
                {
                    return;
                }

                this.itemType = value;
                this.NotifyPropertyChanged("ItemType");
            }
        }

        
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Properties

        #region Ctor

        
        public FilterRulePanelItem(FilterRule rule, string groupId)
        {
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentException.ThrowIfNullOrEmpty(groupId);

            this.Rule = rule;
            this.GroupId = groupId;
        }

        #endregion Ctor

        #region Public Methods

        
        protected void NotifyPropertyChanged(string propertyName)
        {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "not null");

            PropertyChangedEventHandler eh = this.PropertyChanged;
            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion Public Methods
    }
}
