// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class ValidatingValueBase : IDataErrorInfo, INotifyPropertyChanged, IDeepCloneable
    {
        
        protected ValidatingValueBase()
        {
        }

        
        protected ValidatingValueBase(ValidatingValueBase source)
        {
            ArgumentNullException.ThrowIfNull(source);
            validationRules.EnsureCapacity(source.validationRules.Count);
            foreach (var rule in source.validationRules)
            {
                validationRules.Add((DataErrorInfoValidationRule)rule.DeepClone());
            }
        }

        #region Properties

        #region ValidationRules

        private List<DataErrorInfoValidationRule> validationRules = new List<DataErrorInfoValidationRule>();
        private ReadOnlyCollection<DataErrorInfoValidationRule> readonlyValidationRules;
        private bool isValidationRulesCollectionDirty = true;

        private DataErrorInfoValidationResult cachedValidationResult;

        
        public ReadOnlyCollection<DataErrorInfoValidationRule> ValidationRules
        {
            get
            {
                if (this.isValidationRulesCollectionDirty)
                {
                    this.readonlyValidationRules = new ReadOnlyCollection<DataErrorInfoValidationRule>(this.validationRules);
                }

                return this.readonlyValidationRules;
            }
        }

        #endregion ValidationRules

        #region IsValid

        
        public bool IsValid
        {
            get
            {
                return this.GetValidationResult().IsValid;
            }
        }

        #endregion IsValid

        #region IDataErrorInfo implementation
        #region Item

        
        public string this[string columnName]
        {
            get
            {
                ArgumentException.ThrowIfNullOrEmpty(columnName);

                this.UpdateValidationResult(columnName);
                return this.GetValidationResult().ErrorMessage;
            }
        }

        #endregion Item

        #region Error

        
        public string Error
        {
            get
            {
                DataErrorInfoValidationResult result = this.GetValidationResult();
                return (!result.IsValid) ? result.ErrorMessage : string.Empty;
            }
        }

        #endregion Error
        #endregion IDataErrorInfo implementation

        #endregion Properties

        #region Events

        #region PropertyChanged

        
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion PropertyChanged

        #endregion Events

        #region Public Methods

        public abstract object DeepClone();

        #region AddValidationRule

        
        public void AddValidationRule(DataErrorInfoValidationRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            this.validationRules.Add(rule);

            this.isValidationRulesCollectionDirty = true;
            this.NotifyPropertyChanged("ValidationRules");
        }

        #endregion AddValidationRule

        #region RemoveValidationRule

        
        public void RemoveValidationRule(DataErrorInfoValidationRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            this.validationRules.Remove(rule);

            this.isValidationRulesCollectionDirty = true;
            this.NotifyPropertyChanged("ValidationRules");
        }

        #endregion RemoveValidationRule

        #region ClearValidationRules

        
        public void ClearValidationRules()
        {
            this.validationRules.Clear();

            this.isValidationRulesCollectionDirty = true;
            this.NotifyPropertyChanged("ValidationRules");
        }

        #endregion ClearValidationRules

        #region Validate

        
        protected abstract DataErrorInfoValidationResult Validate();

        
        protected abstract DataErrorInfoValidationResult Validate(string propertyName);

        #endregion Validate

        #region EvaluateValidationRules

        internal DataErrorInfoValidationResult EvaluateValidationRules(object value, System.Globalization.CultureInfo cultureInfo)
        {
            foreach (DataErrorInfoValidationRule rule in this.ValidationRules)
            {
                DataErrorInfoValidationResult result = rule.Validate(value, cultureInfo);
                if (result == null)
                {
                    throw new InvalidOperationException(string.Create(CultureInfo.CurrentCulture, $"DataErrorInfoValidationResult not returned by ValidationRule: {rule}"));
                }

                if (!result.IsValid)
                {
                    return result;
                }
            }

            return DataErrorInfoValidationResult.ValidResult;
        }

        #endregion EvaluateValidationRules

        #region InvalidateValidationResult

        
        protected void InvalidateValidationResult()
        {
            this.ClearValidationResult();
        }

        #endregion InvalidateValidationResult

        #region NotifyPropertyChanged

        
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler eh = this.PropertyChanged;

            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion NotifyPropertyChanged

        #endregion Public Methods

        #region Private Methods

        #region GetValidationResult

        private DataErrorInfoValidationResult GetValidationResult()
        {
            if (this.cachedValidationResult == null)
            {
                this.UpdateValidationResult();
            }

            return this.cachedValidationResult;
        }

        #endregion GetValidationResult

        #region UpdateValidationResult

        private void UpdateValidationResult()
        {
            this.cachedValidationResult = this.Validate();
            this.NotifyValidationResultUpdated();
        }

        private void UpdateValidationResult(string columnName)
        {
            this.cachedValidationResult = this.Validate(columnName);
            this.NotifyValidationResultUpdated();
        }

        private void NotifyValidationResultUpdated()
        {
            Debug.Assert(this.cachedValidationResult != null, "not null");
            this.NotifyPropertyChanged("IsValid");
            this.NotifyPropertyChanged("Error");
        }

        #endregion UpdateValidationResult

        #region ClearValidationResult

        private void ClearValidationResult()
        {
            this.cachedValidationResult = null;
        }

        #endregion ClearValidationResult

        #endregion Private Methods
    }
}
