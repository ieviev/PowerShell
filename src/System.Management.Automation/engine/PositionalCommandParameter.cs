// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    internal class PositionalCommandParameter
    {
        #region ctor

        
        internal PositionalCommandParameter(MergedCompiledCommandParameter parameter)
        {
            Parameter = parameter;
        }

        #endregion ctor

        internal MergedCompiledCommandParameter Parameter { get; }

        internal Collection<ParameterSetSpecificMetadata> ParameterSetData { get; } = new Collection<ParameterSetSpecificMetadata>();
    }
}
