// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    
    internal class ParameterSetPromptingData
    {
        internal ParameterSetPromptingData(uint parameterSet, bool isDefaultSet)
        {
            ParameterSet = parameterSet;
            IsDefaultSet = isDefaultSet;
        }

        
        internal bool IsDefaultSet { get; }

        
        internal uint ParameterSet { get; } = 0;

        
        internal bool IsAllSet
        {
            get { return ParameterSet == uint.MaxValue; }
        }

        
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryParameters
        { get; } = new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryByValueParameters
        { get; } = new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryByPropertyNameParameters
        { get; } = new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> NonpipelineableMandatoryParameters
        { get; } = new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();
    }
}
