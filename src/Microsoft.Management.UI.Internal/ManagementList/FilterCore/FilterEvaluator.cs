// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class FilterEvaluator : IFilterExpressionProvider, INotifyPropertyChanged
    {
        #region Properties

        private Collection<IFilterExpressionProvider> filterExpressionProviders = new Collection<IFilterExpressionProvider>();

        
        public ReadOnlyCollection<IFilterExpressionProvider> FilterExpressionProviders
        {
            get
            {
                return new ReadOnlyCollection<IFilterExpressionProvider>(this.filterExpressionProviders);
            }
        }

        private FilterStatus filterStatus = FilterStatus.NotApplied;

        
        public FilterStatus FilterStatus
        {
            get
            {
                return this.filterStatus;
            }

            protected set
            {
                this.filterStatus = value;
                this.NotifyPropertyChanged("FilterStatus");
            }
        }

        private bool startFilterOnExpressionChanged = true;

        
        public bool StartFilterOnExpressionChanged
        {
            get
            {
                return this.startFilterOnExpressionChanged;
            }

            set
            {
                this.startFilterOnExpressionChanged = value;
                this.NotifyPropertyChanged("StartFilterOnExpressionChanged");
            }
        }

        private bool hasFilterExpression = false;

        
        public bool HasFilterExpression
        {
            get
            {
                return this.hasFilterExpression;
            }

            protected set
            {
                this.hasFilterExpression = value;
                this.NotifyPropertyChanged("HasFilterExpression");
            }
        }

        #endregion Properties

        #region Events

        
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Public Methods

        
        public abstract void StartFilter();

        
        public abstract void StopFilter();

        
        public FilterExpressionNode FilterExpression
        {
            get
            {
                FilterExpressionAndOperatorNode andNode = new FilterExpressionAndOperatorNode();
                foreach (IFilterExpressionProvider provider in this.FilterExpressionProviders)
                {
                    FilterExpressionNode node = provider.FilterExpression;
                    if (node != null)
                    {
                        andNode.Children.Add(node);
                    }
                }

                return (andNode.Children.Count != 0) ? andNode : null;
            }
        }

        
        public void AddFilterExpressionProvider(IFilterExpressionProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            this.filterExpressionProviders.Add(provider);
            provider.FilterExpressionChanged += this.FilterProvider_FilterExpressionChanged;
        }

        
        public void RemoveFilterExpressionProvider(IFilterExpressionProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            this.filterExpressionProviders.Remove(provider);
            provider.FilterExpressionChanged -= this.FilterProvider_FilterExpressionChanged;
        }

        #region NotifyPropertyChanged

        
        protected void NotifyPropertyChanged(string propertyName)
        {
            Debug.Assert(!string.IsNullOrEmpty(propertyName), "propertyName is not null");

            PropertyChangedEventHandler eh = this.PropertyChanged;

            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion NotifyPropertyChanged

        #endregion Public Methods

        #region Private Methods

        
        public event EventHandler FilterExpressionChanged;

        
        protected virtual void NotifyFilterExpressionChanged()
        {
            EventHandler eh = this.FilterExpressionChanged;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

        private void FilterProvider_FilterExpressionChanged(object sender, EventArgs e)
        {
            // Update HasFilterExpression \\
            var hasFilterExpression = false;

            foreach (IFilterExpressionProvider provider in this.FilterExpressionProviders)
            {
                if (provider.HasFilterExpression)
                {
                    hasFilterExpression = true;
                    break;
                }
            }

            this.HasFilterExpression = hasFilterExpression;

            // Update FilterExpression \\
            this.NotifyFilterExpressionChanged();
            this.NotifyPropertyChanged("FilterExpression");

            // Start filtering if requested \\
            if (this.StartFilterOnExpressionChanged)
            {
                this.StartFilter();
            }
        }

        #endregion Private Methods
    }
}
