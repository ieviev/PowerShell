// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public interface IEvaluate
    {
        
        /// <param name="item">
        /// The item to evaluate.
        /// </param>
        /// <returns>
        /// Returns true if the item meets the criteria. False otherwise.
        /// </returns>
        bool Evaluate(object item);
    }
}
