// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class SingleValueComparableValueFilterRule<T> : ComparableValueFilterRule<T> where T : IComparable
    {
        #region Properties

        
        public ValidatingValue<T> Value
        {
            get;
            protected set;
        }

        
        public override bool IsValid
        {
            get
            {
                return this.Value.IsValid;
            }
        }

        #endregion Properties

        #region Ctor

        
        protected SingleValueComparableValueFilterRule()
        {
            this.Value = new ValidatingValue<T>();
            this.Value.PropertyChanged += this.Value_PropertyChanged;
        }

        
        protected SingleValueComparableValueFilterRule(SingleValueComparableValueFilterRule<T> source)
            : base(source)
        {
            this.Value = (ValidatingValue<T>)source.Value.DeepClone();
            this.Value.PropertyChanged += this.Value_PropertyChanged;
        }

        #endregion Ctor

        private void Value_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                this.NotifyEvaluationResultInvalidated();
            }
        }
    }
}
