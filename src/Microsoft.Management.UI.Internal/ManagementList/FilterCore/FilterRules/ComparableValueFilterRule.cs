// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class ComparableValueFilterRule<T> : FilterRule where T : IComparable
    {
        
        protected ComparableValueFilterRule()
        {
        }

        
        /// <param name="source">The source to initialize from.</param>
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

        
        /// <param name="item">
        /// The item to match evaluate.
        /// </param>
        /// <returns>
        /// Returns true if the item matches, false otherwise.
        /// </returns>
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

        
        /// <param name="data">
        /// The item to match evaluate.
        /// </param>
        /// <returns>
        /// Returns true if the item matches, false otherwise.
        /// </returns>
        protected abstract bool Evaluate(T data);

        #endregion Public Methods
    }
}
