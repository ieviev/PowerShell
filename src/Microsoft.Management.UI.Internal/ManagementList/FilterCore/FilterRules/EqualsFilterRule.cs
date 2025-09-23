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
    public class EqualsFilterRule<T> : SingleValueComparableValueFilterRule<T> where T : IComparable
    {
        
        public EqualsFilterRule()
        {
            this.DisplayName = UICultureResources.FilterRule_Equals;
        }

        
        /// <param name="source">The source to initialize from.</param>
        public EqualsFilterRule(EqualsFilterRule<T> source)
            : base(source)
        {
        }

        
        /// <param name="data">
        /// The data to compare against.
        /// </param>
        /// <returns>
        /// Returns true if data is equal to Value.
        /// </returns>
        protected override bool Evaluate(T data)
        {
            Debug.Assert(this.IsValid, "isValid");

            int result = CustomTypeComparer.Compare<T>(this.Value.GetCastValue(), data);
            return result == 0;
        }
    }
}
