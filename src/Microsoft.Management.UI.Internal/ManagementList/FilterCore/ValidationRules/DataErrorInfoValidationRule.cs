// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class DataErrorInfoValidationRule : IDeepCloneable
    {
        
        /// <param name="value">
        /// The value to check.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use in this rule.
        /// </param>
        /// <returns>
        /// A DataErrorInfoValidationResult object.
        /// </returns>
        public abstract DataErrorInfoValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo);

        /// <inheritdoc cref="IDeepCloneable.DeepClone()" />
        public abstract object DeepClone();
    }
}
