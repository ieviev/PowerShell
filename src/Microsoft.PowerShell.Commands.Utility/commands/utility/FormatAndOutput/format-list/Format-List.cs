// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Format, "List", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096928")]
    [OutputType(typeof(FormatStartData), typeof(FormatEntryData), typeof(FormatEndData), typeof(GroupStartData), typeof(GroupEndData))]
    public class FormatListCommand : OuterFormatTableAndListBase
    {
        
        public FormatListCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.List);
        }
    }
}
