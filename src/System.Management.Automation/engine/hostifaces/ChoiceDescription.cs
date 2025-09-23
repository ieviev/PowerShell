// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Host
{
    
    public sealed
    class ChoiceDescription
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell compatibility with Windows PowerShell

        private readonly string label = null;
        private string helpMessage = string.Empty;

        #endregion

        
        public
        ChoiceDescription(string label)
        {
            // the only required parameter is label.

            if (string.IsNullOrEmpty(label))
            {
                // "label" is not localizable
                throw PSTraceSource.NewArgumentException(nameof(label), DescriptionsStrings.NullOrEmptyErrorTemplate, "label");
            }

            this.label = label;
        }

        
        public
        ChoiceDescription(string label, string helpMessage)
        {
            // the only required parameter is label.

            if (string.IsNullOrEmpty(label))
            {
                // "label" is not localizable
                throw PSTraceSource.NewArgumentException(nameof(label), DescriptionsStrings.NullOrEmptyErrorTemplate, "label");
            }

            if (helpMessage == null)
            {
                // "helpMessage" is not localizable
                throw PSTraceSource.NewArgumentNullException(nameof(helpMessage));
            }

            this.label = label;
            this.helpMessage = helpMessage;
        }

        
        public
        string
        Label
        {
            get
            {
                Dbg.Assert(this.label != null, "label should not be null");

                return this.label;
            }
        }

        
        public
        string
        HelpMessage
        {
            get
            {
                Dbg.Assert(this.helpMessage != null, "helpMessage should not be null");

                return this.helpMessage;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                this.helpMessage = value;
            }
        }
    }
}
