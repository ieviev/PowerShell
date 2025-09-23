// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class FilterRule : IEvaluate, IDeepCloneable
    {
        
        public virtual bool IsValid
        {
            get
            {
                return true;
            }
        }

        
        public string DisplayName
        {
            get;
            protected set;
        }

        
        protected FilterRule()
        {
        }

        
        /// <param name="source">The source to initialize from.</param>
        protected FilterRule(FilterRule source)
        {
            ArgumentNullException.ThrowIfNull(source);
            this.DisplayName = source.DisplayName;
        }

        /// <inheritdoc cref="IDeepCloneable.DeepClone()" />
        public object DeepClone()
        {
            return Activator.CreateInstance(this.GetType(), new object[] { this });
        }

        
        /// <param name="item">The item to evaluate.</param>
        /// <returns>Returns true if the item meets the criteria. False otherwise.</returns>
        public abstract bool Evaluate(object item);

        #region EvaluationResultInvalidated

        
        public event EventHandler EvaluationResultInvalidated;

        
        protected void NotifyEvaluationResultInvalidated()
        {
            var eh = this.EvaluationResultInvalidated;

            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

        #endregion
    }
}
