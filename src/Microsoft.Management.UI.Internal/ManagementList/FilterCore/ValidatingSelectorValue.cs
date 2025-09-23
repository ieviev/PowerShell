// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ValidatingSelectorValue<T> : ValidatingValueBase
    {
        
        public ValidatingSelectorValue()
        {
        }

        
        public ValidatingSelectorValue(ValidatingSelectorValue<T> source)
            : base(source)
        {
            availableValues.EnsureCapacity(source.availableValues.Count);
            if (typeof(IDeepCloneable).IsAssignableFrom(typeof(T)))
            {
                foreach (var value in source.availableValues)
                {
                    availableValues.Add((T)((IDeepCloneable)value).DeepClone());
                }
            }
            else
            {
                availableValues.AddRange(source.availableValues);
            }

            selectedIndex = source.selectedIndex;
            displayNameConverter = source.displayNameConverter;
        }

        #region Properties

        #region Consts

        private static readonly DataErrorInfoValidationResult InvalidSelectionResult = new DataErrorInfoValidationResult(false, null, UICultureResources.ValidatingSelectorValueOutOfBounds);

        #endregion Consts

        #region AvailableValues

        private List<T> availableValues = new List<T>();

        
        public IList<T> AvailableValues
        {
            get
            {
                return this.availableValues;
            }
        }

        #endregion AvailableValues

        #region SelectedIndex

        private const string SelectedIndexPropertyName = "SelectedIndex";

        private int selectedIndex;

        
        public int SelectedIndex
        {
            get
            {
                return this.IsIndexWithinBounds(this.selectedIndex) ? this.selectedIndex : -1;
            }

            set
            {
                if (value < -1)
                {
                    throw new ArgumentException("value out of range", "value");
                }

                if (value < this.availableValues.Count)
                {
                    var oldValue = this.selectedIndex;

                    this.selectedIndex = value;

                    this.InvalidateValidationResult();
                    this.NotifySelectedValueChanged(oldValue, this.selectedIndex);
                    this.NotifyPropertyChanged(SelectedIndexPropertyName);
                    this.NotifyPropertyChanged(SelectedValuePropertyName);
                }
            }
        }

        #endregion SelectedIndex

        #region SelectedValue

        private const string SelectedValuePropertyName = "SelectedValue";

        
        public T SelectedValue
        {
            get
            {
                if (!this.IsIndexWithinBounds(this.SelectedIndex))
                {
                    return default(T);
                }

                return this.availableValues[this.SelectedIndex];
            }
        }

        #endregion SelectedValue

        #region DisplayNameConverter

        private IValueConverter displayNameConverter;

        
        public IValueConverter DisplayNameConverter
        {
            get
            {
                return this.displayNameConverter;
            }

            set
            {
                this.displayNameConverter = value;
            }
        }

        #endregion DisplayNameConverter

        #endregion Properties

        #region Events

        
        public event EventHandler<PropertyChangedEventArgs<T>> SelectedValueChanged;

        #endregion Events

        #region Public Methods

        public override object DeepClone()
        {
            return new ValidatingSelectorValue<T>(this);
        }

        #region Validate

        
        protected override DataErrorInfoValidationResult Validate()
        {
            return this.Validate(SelectedIndexPropertyName);
        }

        
        protected override DataErrorInfoValidationResult Validate(string columnName)
        {
            if (!columnName.Equals(SelectedIndexPropertyName, StringComparison.CurrentCulture))
            {
                throw new ArgumentException(string.Create(CultureInfo.CurrentCulture, $"{columnName} is not a valid column name."), "columnName");
            }

            if (!this.IsIndexWithinBounds(this.SelectedIndex))
            {
                return InvalidSelectionResult;
            }

            return this.EvaluateValidationRules(this.SelectedValue, System.Globalization.CultureInfo.CurrentCulture);
        }

        #endregion Validate

        #region NotifySelectedValueChanged

        
        protected void NotifySelectedValueChanged(T oldValue, T newValue)
        {
            EventHandler<PropertyChangedEventArgs<T>> eh = this.SelectedValueChanged;

            if (eh != null)
            {
                eh(this, new PropertyChangedEventArgs<T>(oldValue, newValue));
            }
        }

        #endregion NotifySelectedValueChanged

        #endregion Public Methods

        #region Private Methods

        #region IsIndexWithinBounds

        private bool IsIndexWithinBounds(int value)
        {
            return value >= 0 && value < this.AvailableValues.Count;
        }

        #endregion IsIndexWithinBounds

        private void NotifySelectedValueChanged(int oldIndex, int newIndex)
        {
            if (this.IsIndexWithinBounds(oldIndex) && this.IsIndexWithinBounds(newIndex))
            {
                this.NotifySelectedValueChanged(this.availableValues[oldIndex], this.availableValues[newIndex]);
            }
        }

        #endregion Private Methods
    }
}
