// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using Microsoft.PowerShell.Commands.Internal;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "Clipboard", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2109905")]
    [Alias("gcb")]
    [OutputType(typeof(string))]
    public class GetClipboardCommand : PSCmdlet
    {
        
        [Parameter]
        public SwitchParameter Raw
        {
            get
            {
                return _raw;
            }

            set
            {
                _raw = value;
            }
        }

        private bool _raw;

        
        protected override void BeginProcessing()
        {
            this.WriteObject(GetClipboardContentAsText(), true);
        }

        
        /// <returns>Array of strings representing content from clipboard.</returns>
        private List<string> GetClipboardContentAsText()
        {
            var result = new List<string>();
            string textContent = null;

            try
            {
                textContent = Clipboard.GetText();
            }
            catch (PlatformNotSupportedException)
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(ClipboardResources.UnsupportedPlatform), "FailedToGetClipboardUnsupportedPlatform", ErrorCategory.InvalidOperation, "Clipboard"));
            }

            if (_raw)
            {
                result.Add(textContent);
            }
            else
            {
                string[] splitSymbol = { Environment.NewLine };
                result.AddRange(textContent.Split(splitSymbol, StringSplitOptions.None));
            }

            return result;
        }
    }
}
