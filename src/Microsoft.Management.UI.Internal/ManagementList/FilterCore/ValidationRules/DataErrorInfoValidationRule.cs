// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class DataErrorInfoValidationRule : IDeepCloneable
    {
        
        public abstract DataErrorInfoValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo);

        public abstract object DeepClone();
    }
}
