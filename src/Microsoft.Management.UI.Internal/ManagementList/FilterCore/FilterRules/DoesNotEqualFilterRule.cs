// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DoesNotEqualFilterRule<T> : EqualsFilterRule<T> where T : IComparable
    {
        
        public DoesNotEqualFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_DoesNotEqual;
            this.DefaultNullValueEvaluation = true;
        }

        
        /// <param name="source">The source to initialize from.</param>
        public DoesNotEqualFilterRule(DoesNotEqualFilterRule<T> source)
            : base(source)
        {
        }

        
        /// <param name="data">
        /// The data to compare against.
        /// </param>
        /// <returns>
        /// Returns true if data is not equal to Value, false otherwise.
        /// </returns>
        protected override bool Evaluate(T data)
        {
            return !base.Evaluate(data);
        }
    }
}
