// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class SelectorFilterRule : FilterRule
    {
        #region Properties

        
        public override bool IsValid
        {
            get
            {
                return this.AvailableRules.IsValid && this.AvailableRules.SelectedValue.IsValid;
            }
        }

        
        public ValidatingSelectorValue<FilterRule> AvailableRules
        {
            get;
            protected set;
        }

        #endregion Properties

        #region Ctor

        
        public SelectorFilterRule()
        {
            this.AvailableRules = new ValidatingSelectorValue<FilterRule>();
            this.AvailableRules.SelectedValueChanged += this.AvailableRules_SelectedValueChanged;
        }

        
        public SelectorFilterRule(SelectorFilterRule source)
            : base(source)
        {
            this.AvailableRules = (ValidatingSelectorValue<FilterRule>)source.AvailableRules.DeepClone();
            this.AvailableRules.SelectedValueChanged += this.AvailableRules_SelectedValueChanged;
            this.AvailableRules.SelectedValue.EvaluationResultInvalidated += this.SelectedValue_EvaluationResultInvalidated;
        }

        #endregion Ctor

        #region Public Methods

        
        public override bool Evaluate(object item)
        {
            if (!this.IsValid)
            {
                return false;
            }

            return this.AvailableRules.SelectedValue.Evaluate(item);
        }

        
        protected void OnSelectedValueChanged(FilterRule oldValue, FilterRule newValue)
        {
            FilterRuleCustomizationFactory.FactoryInstance.ClearValues(newValue);
            FilterRuleCustomizationFactory.FactoryInstance.TransferValues(oldValue, newValue);
            FilterRuleCustomizationFactory.FactoryInstance.ClearValues(oldValue);

            oldValue.EvaluationResultInvalidated -= this.SelectedValue_EvaluationResultInvalidated;
            newValue.EvaluationResultInvalidated += this.SelectedValue_EvaluationResultInvalidated;

            this.NotifyEvaluationResultInvalidated();
        }

        private void SelectedValue_EvaluationResultInvalidated(object sender, EventArgs e)
        {
            this.NotifyEvaluationResultInvalidated();
        }

        #endregion Public Methods

        #region Private Methods

        private void AvailableRules_SelectedValueChanged(object sender, PropertyChangedEventArgs<FilterRule> e)
        {
            this.OnSelectedValueChanged(e.OldValue, e.NewValue);
        }

        #endregion Private Methods
    }
}
