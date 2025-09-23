// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;

namespace Microsoft.PowerShell.Commands;


internal sealed class JsonSchemaReferenceResolutionException : Exception
{
    
    public JsonSchemaReferenceResolutionException(Exception innerException)
        : base(message: null, innerException)
    {
    }
}
