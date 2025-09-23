// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Management.Automation;
using System.Text;
using System.Xml;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommunications.Write, "Host", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097137", RemotingCapability = RemotingCapability.None)]
    public sealed class WriteHostCommand : ConsoleColorCmdlet
    {
        
        [Parameter(Position = 0, ValueFromRemainingArguments = true, ValueFromPipeline = true)]
        [Alias("Msg", "Message")]
        public object Object { get; set; }

        
        [Parameter]
        public SwitchParameter NoNewline
        {
            get
            {
                return _notAppendNewline;
            }

            set
            {
                _notAppendNewline = value;
            }
        }

        
        [Parameter]
        public object Separator { get; set; } = " ";

        //
        // Cmdlet Overrides
        //
        private string ProcessObject(object o)
        {
            if (o != null)
            {
                if (o is string s)
                {
                    // strings are IEnumerable, so we special case them
                    if (s.Length > 0)
                    {
                        return s;
                    }
                }
                else if (o is XmlNode xmlNode)
                {
                    return xmlNode.Name;
                }
                else if (o is IEnumerable enumerable)
                {
                    // unroll enumerables, including arrays.

                    bool printSeparator = false;
                    StringBuilder result = new();

                    foreach (object element in enumerable)
                    {
                        if (printSeparator && Separator != null)
                        {
                            result.Append(Separator.ToString());
                        }

                        result.Append(ProcessObject(element));
                        printSeparator = true;
                    }

                    return result.ToString();
                }
                else
                {
                    s = o.ToString();

                    if (s.Length > 0)
                    {
                        return s;
                    }
                }
            }

            return null;
        }

        
        protected override void ProcessRecord()
        {
            string result = ProcessObject(Object) ?? string.Empty;

            HostInformationMessage informationMessage = new();
            informationMessage.Message = result;
            informationMessage.NoNewLine = NoNewline.IsPresent;

            try
            {
                informationMessage.ForegroundColor = ForegroundColor;
                informationMessage.BackgroundColor = BackgroundColor;
            }
            catch (System.Management.Automation.Host.HostException)
            {
                // Expected if the host is not interactive, or doesn't have Foreground / Background
                // colours.
            }

            this.WriteInformation(informationMessage, new string[] { "PSHOST" });
        }

        private bool _notAppendNewline = false;
    }
}
