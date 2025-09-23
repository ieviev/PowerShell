// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Language;
using System.Text;

namespace System.Management.Automation
{
    internal class MergedCommandParameterMetadata
    {
        
        internal List<MergedCompiledCommandParameter> ReplaceMetadata(MergedCommandParameterMetadata metadata)
        {
            var result = new List<MergedCompiledCommandParameter>();

            // Replace bindable parameters
            _bindableParameters.Clear();
            foreach (KeyValuePair<string, MergedCompiledCommandParameter> entry in metadata.BindableParameters)
            {
                _bindableParameters.Add(entry.Key, entry.Value);
                result.Add(entry.Value);
            }

            _aliasedParameters.Clear();
            foreach (KeyValuePair<string, MergedCompiledCommandParameter> entry in metadata.AliasedParameters)
            {
                _aliasedParameters.Add(entry.Key, entry.Value);
            }

            // Replace additional meta info
            _defaultParameterSetName = metadata._defaultParameterSetName;
            _nextAvailableParameterSetIndex = metadata._nextAvailableParameterSetIndex;

            _parameterSetMap.Clear();
            var parameterSetMapInList = (List<string>)_parameterSetMap;
            parameterSetMapInList.AddRange(metadata._parameterSetMap);

            Diagnostics.Assert(ParameterSetCount == _nextAvailableParameterSetIndex,
                "After replacement with the metadata of the new parameters, ParameterSetCount should be equal to nextAvailableParameterSetIndex");

            return result;
        }

        
        internal Collection<MergedCompiledCommandParameter> AddMetadataForBinder(
            InternalParameterMetadata parameterMetadata,
            ParameterBinderAssociation binderAssociation)
        {
            if (parameterMetadata == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(parameterMetadata));
            }

            Collection<MergedCompiledCommandParameter> result =
                new Collection<MergedCompiledCommandParameter>();

            // Merge in the bindable parameters

            foreach (KeyValuePair<string, CompiledCommandParameter> bindableParameter in parameterMetadata.BindableParameters)
            {
                if (_bindableParameters.ContainsKey(bindableParameter.Key))
                {
                    MetadataException exception =
                        new MetadataException(
                            "ParameterNameAlreadyExistsForCommand",
                            null,
                            Metadata.ParameterNameAlreadyExistsForCommand,
                            bindableParameter.Key);
                    throw exception;
                }

                // NTRAID#Windows Out Of Band Releases-926371-2005/12/27-JonN
                if (_aliasedParameters.ContainsKey(bindableParameter.Key))
                {
                    MetadataException exception =
                        new MetadataException(
                            "ParameterNameConflictsWithAlias",
                            null,
                            Metadata.ParameterNameConflictsWithAlias,
                            bindableParameter.Key,
                            RetrieveParameterNameForAlias(bindableParameter.Key, _aliasedParameters));
                    throw exception;
                }

                MergedCompiledCommandParameter mergedParameter =
                    new MergedCompiledCommandParameter(bindableParameter.Value, binderAssociation);

                _bindableParameters.Add(bindableParameter.Key, mergedParameter);
                result.Add(mergedParameter);

                // Merge in the aliases

                foreach (string aliasName in bindableParameter.Value.Aliases)
                {
                    if (_aliasedParameters.ContainsKey(aliasName))
                    {
                        MetadataException exception =
                            new MetadataException(
                                "AliasParameterNameAlreadyExistsForCommand",
                                null,
                                Metadata.AliasParameterNameAlreadyExistsForCommand,
                                aliasName);
                        throw exception;
                    }

                    // NTRAID#Windows Out Of Band Releases-926371-2005/12/27-JonN
                    if (_bindableParameters.ContainsKey(aliasName))
                    {
                        MetadataException exception =
                            new MetadataException(
                                "ParameterNameConflictsWithAlias",
                                null,
                                Metadata.ParameterNameConflictsWithAlias,
                                RetrieveParameterNameForAlias(aliasName, _bindableParameters),
                                bindableParameter.Value.Name);
                        throw exception;
                    }

                    _aliasedParameters.Add(aliasName, mergedParameter);
                }
            }

            return result;
        }

        
        private uint _nextAvailableParameterSetIndex;

        
        private const uint MaxParameterSetCount = 32;

        
        internal int ParameterSetCount
        {
            get
            {
                return _parameterSetMap.Count;
            }
        }

        
        internal uint AllParameterSetFlags
        {
            get
            {
                return (1u << ParameterSetCount) - 1;
            }
        }

        
        private IList<string> _parameterSetMap = new List<string>();

        
        private string _defaultParameterSetName;

        
        private int AddParameterSetToMap(string parameterSetName)
        {
            int index = -1;
            if (!string.IsNullOrEmpty(parameterSetName))
            {
                index = _parameterSetMap.IndexOf(parameterSetName);

                // A parameter set name should only be added once
                if (index == -1)
                {
                    if (_nextAvailableParameterSetIndex >= MaxParameterSetCount)
                    {
                        // Don't let the parameter set index overflow
                        ParsingMetadataException parsingException =
                            new ParsingMetadataException(
                                "ParsingTooManyParameterSets",
                                null,
                                Metadata.ParsingTooManyParameterSets);

                        throw parsingException;
                    }

                    _parameterSetMap.Add(parameterSetName);
                    index = _parameterSetMap.IndexOf(parameterSetName);

                    Diagnostics.Assert(
                        index == _nextAvailableParameterSetIndex,
                        "AddParameterSetToMap should always add the parameter set name to the map at the nextAvailableParameterSetIndex");

                    _nextAvailableParameterSetIndex++;
                }
            }

            return index;
        }

        
        internal uint GenerateParameterSetMappingFromMetadata(string defaultParameterSetName)
        {
            // First clear the parameter set map
            _parameterSetMap.Clear();
            _nextAvailableParameterSetIndex = 0;

            uint defaultParameterSetFlag = 0;

            if (!string.IsNullOrEmpty(defaultParameterSetName))
            {
                _defaultParameterSetName = defaultParameterSetName;

                // Add the default parameter set to the parameter set map

                int index = AddParameterSetToMap(defaultParameterSetName);
                defaultParameterSetFlag = (uint)1 << index;
            }

            // Loop through all the parameters and then each parameter set for each parameter
            foreach (MergedCompiledCommandParameter parameter in BindableParameters.Values)
            {
                // For each parameter we need to generate a bit-field for the parameter sets
                // that the parameter is a part of.

                uint parameterSetBitField = 0;

                foreach (var keyValuePair in parameter.Parameter.ParameterSetData)
                {
                    var parameterSetName = keyValuePair.Key;
                    var parameterSetData = keyValuePair.Value;
                    if (string.Equals(parameterSetName, ParameterAttribute.AllParameterSets, StringComparison.OrdinalIgnoreCase))
                    {
                        // Don't add the parameter set name but assign the bit field zero and then mark the bool
                        parameterSetData.ParameterSetFlag = 0;
                        parameterSetData.IsInAllSets = true;
                        parameter.Parameter.IsInAllSets = true;
                    }
                    else
                    {
                        // Add the parameter set name and/or get the index in the map

                        int index = AddParameterSetToMap(parameterSetName);

                        Diagnostics.Assert(
                            index >= 0,
                            "AddParameterSetToMap should always be able to add the parameter set name, if not it should throw");

                        // Calculate the bit for this parameter set
                        uint parameterSetBit = (uint)1 << index;

                        // Add the bit to the bit-field
                        parameterSetBitField |= parameterSetBit;

                        // Add the bit to the parameter set specific data
                        parameterSetData.ParameterSetFlag = parameterSetBit;
                    }
                }

                // Set the bit field in the parameter
                parameter.Parameter.ParameterSetFlags = parameterSetBitField;
            }

            return defaultParameterSetFlag;
        }

        
        internal string GetParameterSetName(uint parameterSet)
        {
            string result = _defaultParameterSetName;

            if (string.IsNullOrEmpty(result))
            {
                result = ParameterAttribute.AllParameterSets;
            }

            if (parameterSet != uint.MaxValue && parameterSet != 0)
            {
                // Count the number of right shifts it takes to hit the parameter set
                // This is the index into the parameter set map.
                int index = 0;

                while (((parameterSet >> index) & 0x1) == 0)
                {
                    ++index;
                }

                // Now check to see if there are any remaining sets passed this bit.
                // If so return string.Empty

                if (((parameterSet >> (index + 1)) & 0x1) == 0)
                {
                    // Ensure that the bit found was within the map, if not return an empty string
                    if (index < _parameterSetMap.Count)
                    {
                        result = _parameterSetMap[index];
                    }
                    else
                    {
                        result = string.Empty;
                    }
                }
                else
                {
                    result = string.Empty;
                }
            }

            return result;
        }

        
        private static string RetrieveParameterNameForAlias(
            string key,
            IDictionary<string, MergedCompiledCommandParameter> dict)
        {
            MergedCompiledCommandParameter mergedParam = dict[key];
            if (mergedParam != null)
            {
                CompiledCommandParameter compiledParam = mergedParam.Parameter;
                if (compiledParam != null)
                {
                    if (!string.IsNullOrEmpty(compiledParam.Name))
                        return compiledParam.Name;
                }
            }

            return string.Empty;
        }

        
        internal MergedCompiledCommandParameter GetMatchingParameter(
            string name,
            bool throwOnParameterNotFound,
            bool tryExactMatching,
            InvocationInfo invocationInfo)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            Collection<MergedCompiledCommandParameter> matchingParameters =
                new Collection<MergedCompiledCommandParameter>();

            // Skip the leading '-' if present
            if (name.Length > 0 && CharExtensions.IsDash(name[0]))
            {
                name = name.Substring(1);
            }

            // First try to match the bindable parameters

            foreach (string parameterName in _bindableParameters.Keys)
            {
                if (CultureInfo.InvariantCulture.CompareInfo.IsPrefix(parameterName, name, CompareOptions.IgnoreCase))
                {
                    // If it is an exact match then only return the exact match
                    // as the result

                    if (tryExactMatching && string.Equals(parameterName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return _bindableParameters[parameterName];
                    }
                    else
                    {
                        matchingParameters.Add(_bindableParameters[parameterName]);
                    }
                }
            }

            // Now check the aliases

            foreach (string parameterName in _aliasedParameters.Keys)
            {
                if (CultureInfo.InvariantCulture.CompareInfo.IsPrefix(parameterName, name, CompareOptions.IgnoreCase))
                {
                    // If it is an exact match then only return the exact match
                    // as the result

                    if (tryExactMatching && string.Equals(parameterName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return _aliasedParameters[parameterName];
                    }
                    else
                    {
                        if (!matchingParameters.Contains(_aliasedParameters[parameterName]))
                        {
                            matchingParameters.Add(_aliasedParameters[parameterName]);
                        }
                    }
                }
            }

            if (matchingParameters.Count > 1)
            {
                // Prefer parameters in the cmdlet over common parameters
                Collection<MergedCompiledCommandParameter> filteredParameters =
                    new Collection<MergedCompiledCommandParameter>();

                foreach (MergedCompiledCommandParameter matchingParameter in matchingParameters)
                {
                    if ((matchingParameter.BinderAssociation == ParameterBinderAssociation.DeclaredFormalParameters) ||
                        (matchingParameter.BinderAssociation == ParameterBinderAssociation.DynamicParameters))
                    {
                        filteredParameters.Add(matchingParameter);
                    }
                }

                if (tryExactMatching && filteredParameters.Count == 1)
                {
                    matchingParameters = filteredParameters;
                }
                else
                {
                    StringBuilder possibleMatches = new StringBuilder();

                    foreach (MergedCompiledCommandParameter matchingParameter in matchingParameters)
                    {
                        possibleMatches.Append(" -");
                        possibleMatches.Append(matchingParameter.Parameter.Name);
                    }

                    ParameterBindingException exception =
                        new ParameterBindingException(
                            ErrorCategory.InvalidArgument,
                            invocationInfo,
                            null,
                            name,
                            null,
                            null,
                            ParameterBinderStrings.AmbiguousParameter,
                            "AmbiguousParameter",
                            possibleMatches);

                    throw exception;
                }
            }
            else if (matchingParameters.Count == 0)
            {
                if (throwOnParameterNotFound)
                {
                    ParameterBindingException exception =
                        new ParameterBindingException(
                            ErrorCategory.InvalidArgument,
                            invocationInfo,
                            null,
                            name,
                            null,
                            null,
                            ParameterBinderStrings.NamedParameterNotFound,
                            "NamedParameterNotFound");

                    throw exception;
                }
            }

            MergedCompiledCommandParameter result = null;
            if (matchingParameters.Count > 0)
            {
                result = matchingParameters[0];
            }

            return result;
        }

        
        internal Collection<MergedCompiledCommandParameter> GetParametersInParameterSet(uint parameterSetFlag)
        {
            Collection<MergedCompiledCommandParameter> result =
                new Collection<MergedCompiledCommandParameter>();

            foreach (MergedCompiledCommandParameter parameter in BindableParameters.Values)
            {
                if ((parameterSetFlag & parameter.Parameter.ParameterSetFlags) != 0 ||
                    parameter.Parameter.IsInAllSets)
                {
                    result.Add(parameter);
                }
            }

            return result;
        }

        
        internal IDictionary<string, MergedCompiledCommandParameter> BindableParameters { get { return _bindableParameters; } }

        private IDictionary<string, MergedCompiledCommandParameter> _bindableParameters =
            new Dictionary<string, MergedCompiledCommandParameter>(StringComparer.OrdinalIgnoreCase);

        
        internal IDictionary<string, MergedCompiledCommandParameter> AliasedParameters { get { return _aliasedParameters; } }

        private IDictionary<string, MergedCompiledCommandParameter> _aliasedParameters =
            new Dictionary<string, MergedCompiledCommandParameter>(StringComparer.OrdinalIgnoreCase);

        internal void MakeReadOnly()
        {
            _bindableParameters = new ReadOnlyDictionary<string, MergedCompiledCommandParameter>(_bindableParameters);
            _aliasedParameters = new ReadOnlyDictionary<string, MergedCompiledCommandParameter>(_aliasedParameters);
            _parameterSetMap = new ReadOnlyCollection<string>(_parameterSetMap);
        }

        internal void ResetReadOnly()
        {
            _bindableParameters = new Dictionary<string, MergedCompiledCommandParameter>(_bindableParameters, StringComparer.OrdinalIgnoreCase);
            _aliasedParameters = new Dictionary<string, MergedCompiledCommandParameter>(_aliasedParameters, StringComparer.OrdinalIgnoreCase);
        }
    }

    
    internal class MergedCompiledCommandParameter
    {
        
        internal MergedCompiledCommandParameter(
                CompiledCommandParameter parameter,
                ParameterBinderAssociation binderAssociation)
        {
            Diagnostics.Assert(parameter != null, "caller to verify parameter is not null");
            this.Parameter = parameter;
            this.BinderAssociation = binderAssociation;
        }

        
        internal CompiledCommandParameter Parameter { get; }

        
        internal ParameterBinderAssociation BinderAssociation { get; }

        public override string ToString()
        {
            return Parameter.ToString();
        }
    }

    
    internal enum ParameterBinderAssociation
    {
        
        DeclaredFormalParameters,

        
        DynamicParameters,

        
        CommonParameters,

        
        ShouldProcessParameters,

        
        TransactionParameters,

        
        PagingParameters,
    }
}
