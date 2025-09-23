// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// this file contains the data structures for the in memory database
// containing display and formatting information

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    
    internal sealed class FieldControlBody : ControlBody
    {
        internal FieldFormattingDirective fieldFormattingDirective = new FieldFormattingDirective();
    }
}
