// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Format, "Default")]
    public class FormatDefaultCommand : FrontEndCommandBase
    {
        
        public FormatDefaultCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.Undefined);
        }
    }
}
