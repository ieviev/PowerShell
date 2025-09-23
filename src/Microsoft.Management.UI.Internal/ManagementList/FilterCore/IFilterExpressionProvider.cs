// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public interface IFilterExpressionProvider
    {
        
        FilterExpressionNode FilterExpression
        {
            get;
        }

        
        bool HasFilterExpression
        {
            get;
        }

        
        event EventHandler FilterExpressionChanged;
    }
}
