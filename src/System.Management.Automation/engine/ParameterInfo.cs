// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    
    public class CommandParameterInfo
    {
        #region ctor

        
        /// <param name="parameter">
        /// The parameter metadata to retrieve the parameter information from.
        /// </param>
        /// <param name="parameterSetFlag">
        /// The parameter set flag to get the parameter information from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="parameter"/> is null.
        /// </exception>
        internal CommandParameterInfo(
            CompiledCommandParameter parameter,
            uint parameterSetFlag)
        {
            if (parameter == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(parameter));
            }

            Name = parameter.Name;
            ParameterType = parameter.Type;
            IsDynamic = parameter.IsDynamic;
            Aliases = new ReadOnlyCollection<string>(parameter.Aliases);

            SetAttributes(parameter.CompiledAttributes);
            SetParameterSetData(parameter.GetParameterSetData(parameterSetFlag));
        }

        #endregion ctor

        #region public members

        
        public string Name { get; } = string.Empty;

        
        public Type ParameterType { get; }

        
        /// <remarks>
        /// True if the parameter is dynamic, or false otherwise.
        /// </remarks>
        public bool IsMandatory { get; private set; }

        
        /// <remarks>
        /// True if the parameter is mandatory, or false otherwise.
        /// </remarks>
        public bool IsDynamic { get; }

        
        public int Position { get; private set; } = int.MinValue;

        
        public bool ValueFromPipeline { get; private set; }

        
        public bool ValueFromPipelineByPropertyName { get; private set; }

        
        public bool ValueFromRemainingArguments { get; private set; }

        
        public string HelpMessage { get; private set; } = string.Empty;

        
        public ReadOnlyCollection<string> Aliases { get; }

        
        public ReadOnlyCollection<Attribute> Attributes { get; private set; }

        #endregion public members

        #region private members

        private void SetAttributes(IList<Attribute> attributeMetadata)
        {
            Diagnostics.Assert(
                attributeMetadata != null,
                "The compiled attribute collection should never be null");

            Collection<Attribute> processedAttributes = new Collection<Attribute>();

            foreach (var attribute in attributeMetadata)
            {
                processedAttributes.Add(attribute);
            }

            Attributes = new ReadOnlyCollection<Attribute>(processedAttributes);
        }

        private void SetParameterSetData(ParameterSetSpecificMetadata parameterMetadata)
        {
            IsMandatory = parameterMetadata.IsMandatory;
            Position = parameterMetadata.Position;
            ValueFromPipeline = parameterMetadata.valueFromPipeline;
            ValueFromPipelineByPropertyName = parameterMetadata.valueFromPipelineByPropertyName;
            ValueFromRemainingArguments = parameterMetadata.ValueFromRemainingArguments;
            HelpMessage = parameterMetadata.HelpMessage;
        }

        #endregion private members
    }
}
