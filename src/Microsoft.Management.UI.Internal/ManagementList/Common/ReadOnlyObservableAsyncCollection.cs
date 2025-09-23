// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ReadOnlyObservableAsyncCollection<T> :
        ReadOnlyCollection<T>,
        IAsyncProgress,
        INotifyPropertyChanged, INotifyCollectionChanged
    {
        #region Private fields
        private IAsyncProgress asyncProgress;
        #endregion Private fields

        #region Constructors
        
        public ReadOnlyObservableAsyncCollection(IList<T> list)
            : base(list)
        {
            this.asyncProgress = list as IAsyncProgress;

            ((INotifyCollectionChanged)this.Items).CollectionChanged += this.HandleCollectionChanged;
            ((INotifyPropertyChanged)this.Items).PropertyChanged += this.HandlePropertyChanged;
        }
        #endregion Constructors

        #region Events
        
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion Events

        #region IAsyncProgress
        
        public bool OperationInProgress
        {
            get
            {
                if (this.asyncProgress == null)
                {
                    return false;
                }
                else
                {
                    return this.asyncProgress.OperationInProgress;
                }
            }
        }

        
        public Exception OperationError
        {
            get
            {
                if (this.asyncProgress == null)
                {
                    return null;
                }
                else
                {
                    return this.asyncProgress.OperationError;
                }
            }
        }
        #endregion IAsyncProgress

        #region Private Methods
        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler eh = this.CollectionChanged;

            if (eh != null)
            {
                eh(this, args);
            }
        }

        private void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventHandler eh = this.PropertyChanged;

            if (eh != null)
            {
                eh(this, args);
            }
        }

        // forward CollectionChanged events from the base list to our listeners
        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnCollectionChanged(e);
        }

        // forward PropertyChanged events from the base list to our listeners
        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(e);
        }
        #endregion Private Methods
    }
}
