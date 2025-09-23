// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Format, "Table", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096703")]
    [OutputType(typeof(FormatStartData), typeof(FormatEntryData), typeof(FormatEndData), typeof(GroupStartData), typeof(GroupEndData))]
    public class FormatTableCommand : OuterFormatTableBase
    {
        
        public FormatTableCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.Table);
        }
    }
}
