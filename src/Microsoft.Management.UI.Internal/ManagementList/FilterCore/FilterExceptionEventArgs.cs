// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterExceptionEventArgs : EventArgs
    {
        
        public Exception Exception
        {
            get;
            private set;
        }

        
        /// <param name="exception">
        /// The Exception that was raised when filtering was evaluated.
        /// </param>
        public FilterExceptionEventArgs(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            this.Exception = exception;
        }
    }
}
