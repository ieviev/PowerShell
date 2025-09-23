// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsData.Out, "Printer", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2109553")]
    public class OutPrinterCommand : FrontEndCommandBase
    {
        
        public OutPrinterCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        
        [Parameter(Position = 0)]
        [Alias("PrinterName")]
        public string Name
        {
            get { return _printerName; }

            set { _printerName = value; }
        }

        private string _printerName;

        
        protected override void BeginProcessing()
        {
            // set up the Screen Host interface
            OutputManagerInner outInner = (OutputManagerInner)this.implementation;

            outInner.LineOutput = InstantiateLineOutputInterface();

            // finally call the base class for general hookup
            base.BeginProcessing();
        }

        
        private LineOutput InstantiateLineOutputInterface()
        {
            PrinterLineOutput printOutput = new(_printerName);
            return (LineOutput)printOutput;
        }
    }
}
