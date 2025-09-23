// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    internal class ParameterSetSpecificMetadata
    {
        #region ctor

        internal ParameterSetSpecificMetadata(ParameterAttribute attribute)
        {
            if (attribute == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(attribute));
            }

            _attribute = attribute;
            IsMandatory = attribute.Mandatory;
            Position = attribute.Position;
            ValueFromRemainingArguments = attribute.ValueFromRemainingArguments;
            this.valueFromPipeline = attribute.ValueFromPipeline;
            this.valueFromPipelineByPropertyName = attribute.ValueFromPipelineByPropertyName;
            HelpMessage = attribute.HelpMessage;
            HelpMessageBaseName = attribute.HelpMessageBaseName;
            HelpMessageResourceId = attribute.HelpMessageResourceId;
        }

        internal ParameterSetSpecificMetadata(
            bool isMandatory,
            int position,
            bool valueFromRemainingArguments,
            bool valueFromPipeline,
            bool valueFromPipelineByPropertyName,
            string helpMessageBaseName,
            string helpMessageResourceId,
            string helpMessage)
        {
            IsMandatory = isMandatory;
            Position = position;
            ValueFromRemainingArguments = valueFromRemainingArguments;
            this.valueFromPipeline = valueFromPipeline;
            this.valueFromPipelineByPropertyName = valueFromPipelineByPropertyName;
            HelpMessageBaseName = helpMessageBaseName;
            HelpMessageResourceId = helpMessageResourceId;
            HelpMessage = helpMessage;
        }

        #endregion ctor

        internal bool IsMandatory { get; }

        internal int Position { get; } = int.MinValue;

        internal bool IsPositional
        {
            get
            {
                return Position != int.MinValue;
            }
        }

        internal bool ValueFromRemainingArguments { get; }

        internal bool valueFromPipeline;
        internal bool ValueFromPipeline
        {
            get
            {
                return valueFromPipeline;
            }
        }

        internal bool valueFromPipelineByPropertyName;
        internal bool ValueFromPipelineByPropertyName
        {
            get
            {
                return valueFromPipelineByPropertyName;
            }
        }

        internal string HelpMessage { get; }

        internal string HelpMessageBaseName { get; }

        internal string HelpMessageResourceId { get; } = null;

        internal bool IsInAllSets { get; set; }

        internal uint ParameterSetFlag { get; set; }

        internal string GetHelpMessage(Cmdlet cmdlet)
        {
            string helpInfo = null;
            bool isHelpMsgSet = !string.IsNullOrEmpty(HelpMessage);
            bool isHelpMsgBaseNameSet = !string.IsNullOrEmpty(HelpMessageBaseName);
            bool isHelpMsgResIdSet = !string.IsNullOrEmpty(HelpMessageResourceId);

            if (isHelpMsgBaseNameSet ^ isHelpMsgResIdSet)
            {
                throw PSTraceSource.NewArgumentException(isHelpMsgBaseNameSet ? "HelpMessageResourceId" : "HelpMessageBaseName");
            }

            if (isHelpMsgBaseNameSet && isHelpMsgResIdSet)
            {
                try
                {
                    helpInfo = cmdlet.GetResourceString(HelpMessageBaseName, HelpMessageResourceId);
                }
                catch (ArgumentException)
                {
                    if (isHelpMsgSet)
                    {
                        helpInfo = HelpMessage;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (InvalidOperationException)
                {
                    if (isHelpMsgSet)
                    {
                        helpInfo = HelpMessage;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (isHelpMsgSet)
            {
                helpInfo = HelpMessage;
            }

            return helpInfo;
        }

        private readonly ParameterAttribute _attribute;
    }
}
