// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;

namespace Microsoft.PowerShell.Commands;


internal sealed class JsonSchemaReferenceResolutionException : Exception
{
    
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or a null reference
    /// (<code>Nothing</code> in Visual Basic) if no inner exception is specified.
    /// </param>
    public JsonSchemaReferenceResolutionException(Exception innerException)
        : base(message: null, innerException)
    {
    }
}
