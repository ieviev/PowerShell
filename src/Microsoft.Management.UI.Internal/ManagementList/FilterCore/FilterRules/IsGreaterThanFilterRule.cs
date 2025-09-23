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
    public class IsGreaterThanFilterRule<T> : SingleValueComparableValueFilterRule<T> where T : IComparable
    {
        
        public IsGreaterThanFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_GreaterThanOrEqual;
        }

        
        /// <param name="source">The source to initialize from.</param>
        public IsGreaterThanFilterRule(IsGreaterThanFilterRule<T> source)
            : base(source)
        {
        }

        
        /// <param name="data">
        /// The data to compare against.
        /// </param>
        /// <returns>
        /// Returns true if data is greater than Value.
        /// </returns>
        protected override bool Evaluate(T data)
        {
            Debug.Assert(this.IsValid, "is valid");

            int result = CustomTypeComparer.Compare<T>(this.Value.GetCastValue(), data);
            return result <= 0;
        }
    }
}
