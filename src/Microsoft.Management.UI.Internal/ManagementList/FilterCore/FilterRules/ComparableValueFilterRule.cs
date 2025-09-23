// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class ComparableValueFilterRule<T> : FilterRule where T : IComparable
    {
        
        protected ComparableValueFilterRule()
        {
        }

        
        protected ComparableValueFilterRule(ComparableValueFilterRule<T> source)
            : base(source)
        {
            this.DefaultNullValueEvaluation = source.DefaultNullValueEvaluation;
        }

        #region Properties

        
        protected bool DefaultNullValueEvaluation
        {
            get;
            set;
        }

        #endregion Properties

        #region Public Methods

        
        public override bool Evaluate(object item)
        {
            if (item == null)
            {
                return this.DefaultNullValueEvaluation;
            }

            if (!this.IsValid)
            {
                return false;
            }

            T castItem;
            if (!FilterUtilities.TryCastItem<T>(item, out castItem))
            {
                return false;
            }

            return this.Evaluate(castItem);
        }

        
        protected abstract bool Evaluate(T data);

        #endregion Public Methods
    }
}
