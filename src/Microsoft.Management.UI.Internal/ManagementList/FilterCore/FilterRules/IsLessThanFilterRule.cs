// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    /// <typeparam name="T">
    /// The generic parameter.
    /// </typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsLessThanFilterRule<T> : SingleValueComparableValueFilterRule<T> where T : IComparable
    {
        
        public IsLessThanFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_LessThanOrEqual;
        }

        
        /// <param name="source">The source to initialize from.</param>
        public IsLessThanFilterRule(IsLessThanFilterRule<T> source)
            : base(source)
        {
        }

        
        /// <param name="item">
        /// The data to compare against.
        /// </param>
        /// <returns>
        /// Returns true if data is less than Value.
        /// </returns>
        protected override bool Evaluate(T item)
        {
            Debug.Assert(this.IsValid, "is valid");

            int result = CustomTypeComparer.Compare<T>(this.Value.GetCastValue(), item);
            return result >= 0;
        }
    }
}
