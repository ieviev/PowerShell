// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Commands
{
    
    public abstract class PSBreakpointAccessorCommandBase : PSBreakpointCommandBase
    {
        #region strings

        internal const string CommandParameterSetName = "Command";
        internal const string LineParameterSetName = "Line";
        internal const string VariableParameterSetName = "Variable";

        #endregion strings
    }
}
