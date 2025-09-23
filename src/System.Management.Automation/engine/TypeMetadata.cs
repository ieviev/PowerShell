// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Language;
using System.Reflection;

using Microsoft.PowerShell;

using Dbg = System.Diagnostics.Debug;

namespace System.Management.Automation
{
    
    public sealed class ParameterSetMetadata
    {
        #region Private Data

        private bool _isMandatory;
        private int _position;
        private bool _valueFromPipeline;
        private bool _valueFromPipelineByPropertyName;
        private bool _valueFromRemainingArguments;
        private string _helpMessage;
        private string _helpMessageBaseName;
        private string _helpMessageResourceId;

        #endregion

        #region Constructor

        
        internal ParameterSetMetadata(ParameterSetSpecificMetadata psMD)
        {
            Dbg.Assert(psMD != null, "ParameterSetSpecificMetadata cannot be null");
            Initialize(psMD);
        }

        
        internal ParameterSetMetadata(ParameterSetMetadata other)
        {
            if (other == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(other));
            }

            _helpMessage = other._helpMessage;
            _helpMessageBaseName = other._helpMessageBaseName;
            _helpMessageResourceId = other._helpMessageResourceId;
            _isMandatory = other._isMandatory;
            _position = other._position;
            _valueFromPipeline = other._valueFromPipeline;
            _valueFromPipelineByPropertyName = other._valueFromPipelineByPropertyName;
            _valueFromRemainingArguments = other._valueFromRemainingArguments;
        }

        #endregion

        #region Public Properties

        
        public bool IsMandatory
        {
            get
            {
                return _isMandatory;
            }

            set
            {
                _isMandatory = value;
            }
        }

        
        public int Position
        {
            get
            {
                return _position;
            }

            set
            {
                _position = value;
            }
        }

        
        public bool ValueFromPipeline
        {
            get
            {
                return _valueFromPipeline;
            }

            set
            {
                _valueFromPipeline = value;
            }
        }

        
        public bool ValueFromPipelineByPropertyName
        {
            get
            {
                return _valueFromPipelineByPropertyName;
            }

            set
            {
                _valueFromPipelineByPropertyName = value;
            }
        }

        
        public bool ValueFromRemainingArguments
        {
            get
            {
                return _valueFromRemainingArguments;
            }

            set
            {
                _valueFromRemainingArguments = value;
            }
        }

        
        public string HelpMessage
        {
            get
            {
                return _helpMessage;
            }

            set
            {
                _helpMessage = value;
            }
        }

        
        public string HelpMessageBaseName
        {
            get
            {
                return _helpMessageBaseName;
            }

            set
            {
                _helpMessageBaseName = value;
            }
        }

        
        public string HelpMessageResourceId
        {
            get
            {
                return _helpMessageResourceId;
            }

            set
            {
                _helpMessageResourceId = value;
            }
        }

        #endregion

        #region Private / Internal Methods & Properties

        
        internal void Initialize(ParameterSetSpecificMetadata psMD)
        {
            _isMandatory = psMD.IsMandatory;
            _position = psMD.Position;
            _valueFromPipeline = psMD.ValueFromPipeline;
            _valueFromPipelineByPropertyName = psMD.ValueFromPipelineByPropertyName;
            _valueFromRemainingArguments = psMD.ValueFromRemainingArguments;
            _helpMessage = psMD.HelpMessage;
            _helpMessageBaseName = psMD.HelpMessageBaseName;
            _helpMessageResourceId = psMD.HelpMessageResourceId;
        }

        
        internal bool Equals(ParameterSetMetadata second)
        {
            if ((_isMandatory != second._isMandatory) ||
                (_position != second._position) ||
                (_valueFromPipeline != second._valueFromPipeline) ||
                (_valueFromPipelineByPropertyName != second._valueFromPipelineByPropertyName) ||
                (_valueFromRemainingArguments != second._valueFromRemainingArguments) ||
                (_helpMessage != second._helpMessage) ||
                (_helpMessageBaseName != second._helpMessageBaseName) ||
                (_helpMessageResourceId != second._helpMessageResourceId))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Efficient serialization + rehydration logic

        [Flags]
        internal enum ParameterFlags : uint
        {
            Mandatory = 0x01,
            ValueFromPipeline = 0x02,
            ValueFromPipelineByPropertyName = 0x04,
            ValueFromRemainingArguments = 0x08,
        }

        internal ParameterFlags Flags
        {
            get
            {
                ParameterFlags flags = 0;
                if (IsMandatory) { flags |= ParameterFlags.Mandatory; }

                if (ValueFromPipeline) { flags |= ParameterFlags.ValueFromPipeline; }

                if (ValueFromPipelineByPropertyName) { flags |= ParameterFlags.ValueFromPipelineByPropertyName; }

                if (ValueFromRemainingArguments) { flags |= ParameterFlags.ValueFromRemainingArguments; }

                return flags;
            }

            set
            {
                this.IsMandatory = ((value & ParameterFlags.Mandatory) == ParameterFlags.Mandatory);
                this.ValueFromPipeline = ((value & ParameterFlags.ValueFromPipeline) == ParameterFlags.ValueFromPipeline);
                this.ValueFromPipelineByPropertyName = ((value & ParameterFlags.ValueFromPipelineByPropertyName) == ParameterFlags.ValueFromPipelineByPropertyName);
                this.ValueFromRemainingArguments = ((value & ParameterFlags.ValueFromRemainingArguments) == ParameterFlags.ValueFromRemainingArguments);
            }
        }

        
        internal ParameterSetMetadata(
            int position,
            ParameterFlags flags,
            string helpMessage)
        {
            this.Position = position;
            this.Flags = flags;
            this.HelpMessage = helpMessage;
        }

        #endregion

        #region Proxy Parameter Generation

        private const string MandatoryFormat = @"{0}Mandatory=$true";
        private const string PositionFormat = @"{0}Position={1}";
        private const string ValueFromPipelineFormat = @"{0}ValueFromPipeline=$true";
        private const string ValueFromPipelineByPropertyNameFormat = @"{0}ValueFromPipelineByPropertyName=$true";
        private const string ValueFromRemainingArgumentsFormat = @"{0}ValueFromRemainingArguments=$true";
        private const string HelpMessageFormat = @"{0}HelpMessage='{1}'";

        
        internal string GetProxyParameterData()
        {
            Text.StringBuilder result = new System.Text.StringBuilder();
            string prefix = string.Empty;

            if (_isMandatory)
            {
                result.AppendFormat(CultureInfo.InvariantCulture, MandatoryFormat, prefix);
                prefix = ", ";
            }

            if (_position != Int32.MinValue)
            {
                result.AppendFormat(CultureInfo.InvariantCulture, PositionFormat, prefix, _position);
                prefix = ", ";
            }

            if (_valueFromPipeline)
            {
                result.AppendFormat(CultureInfo.InvariantCulture, ValueFromPipelineFormat, prefix);
                prefix = ", ";
            }

            if (_valueFromPipelineByPropertyName)
            {
                result.AppendFormat(CultureInfo.InvariantCulture, ValueFromPipelineByPropertyNameFormat, prefix);
                prefix = ", ";
            }

            if (_valueFromRemainingArguments)
            {
                result.AppendFormat(CultureInfo.InvariantCulture, ValueFromRemainingArgumentsFormat, prefix);
                prefix = ", ";
            }

            if (!string.IsNullOrEmpty(_helpMessage))
            {
                result.AppendFormat(
                    CultureInfo.InvariantCulture,
                    HelpMessageFormat,
                    prefix,
                    CodeGeneration.EscapeSingleQuotedStringContent(_helpMessage));
                prefix = ", ";
            }

            return result.ToString();
        }

        #endregion
    }

    
    public sealed class ParameterMetadata
    {
        #region Private Data

        private string _name;
        private Type _parameterType;
        private bool _isDynamic;
        private Dictionary<string, ParameterSetMetadata> _parameterSets;
        private Collection<string> _aliases;
        private Collection<Attribute> _attributes;

        #endregion

        #region Constructor

        
        public ParameterMetadata(string name)
            : this(name, null)
        {
        }

        
        public ParameterMetadata(string name, Type parameterType)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            _name = name;
            _parameterType = parameterType;

            _attributes = new Collection<Attribute>();
            _aliases = new Collection<string>();
            _parameterSets = new Dictionary<string, ParameterSetMetadata>();
        }

        
        public ParameterMetadata(ParameterMetadata other)
        {
            if (other == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(other));
            }

            _isDynamic = other._isDynamic;
            _name = other._name;
            _parameterType = other._parameterType;

            // deep copy
            _aliases = new Collection<string>(new List<string>(other._aliases.Count));
            foreach (string alias in other._aliases)
            {
                _aliases.Add(alias);
            }

            // deep copy of the collection, collection items (Attributes) copied by reference
            if (other._attributes == null)
            {
                _attributes = null;
            }
            else
            {
                _attributes = new Collection<Attribute>(new List<Attribute>(other._attributes.Count));
                foreach (Attribute attribute in other._attributes)
                {
                    _attributes.Add(attribute);
                }
            }

            // deep copy
            _parameterSets = null;
            if (other._parameterSets == null)
            {
                _parameterSets = null;
            }
            else
            {
                _parameterSets = new Dictionary<string, ParameterSetMetadata>(other._parameterSets.Count);
                foreach (KeyValuePair<string, ParameterSetMetadata> entry in other._parameterSets)
                {
                    _parameterSets.Add(entry.Key, new ParameterSetMetadata(entry.Value));
                }
            }
        }

        
        internal ParameterMetadata(CompiledCommandParameter cmdParameterMD)
        {
            Dbg.Assert(cmdParameterMD != null,
                "CompiledCommandParameter cannot be null");

            Initialize(cmdParameterMD);
        }

        
        internal ParameterMetadata(
            Collection<string> aliases,
            bool isDynamic,
            string name,
            Dictionary<string, ParameterSetMetadata> parameterSets,
            Type parameterType)
        {
            _aliases = aliases;
            _isDynamic = isDynamic;
            _name = name;
            _parameterSets = parameterSets;
            _parameterType = parameterType;
            _attributes = new Collection<Attribute>();
        }

        #endregion

        #region Public Methods/Properties

        
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentNullException("Name");
                }

                _name = value;
            }
        }

        
        public Type ParameterType
        {
            get
            {
                return _parameterType;
            }

            set
            {
                _parameterType = value;
            }
        }

        
        public Dictionary<string, ParameterSetMetadata> ParameterSets
        {
            get
            {
                return _parameterSets;
            }
        }

        
        public bool IsDynamic
        {
            get { return _isDynamic; }

            set { _isDynamic = value; }
        }
        
        public Collection<string> Aliases
        {
            get
            {
                return _aliases;
            }
        }

        
        public Collection<Attribute> Attributes
        {
            get
            {
                return _attributes;
            }
        }

        
        public bool SwitchParameter
        {
            get
            {
                if (_parameterType != null)
                {
                    return _parameterType.Equals(typeof(SwitchParameter));
                }

                return false;
            }
        }

        
        public static Dictionary<string, ParameterMetadata> GetParameterMetadata(Type type)
        {
            if (type == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(type));
            }

            CommandMetadata cmdMetaData = new CommandMetadata(type);
            Dictionary<string, ParameterMetadata> result = cmdMetaData.Parameters;
            // early GC.
            cmdMetaData = null;
            return result;
        }

        #endregion

        #region Internal Methods/Properties

        
        internal void Initialize(CompiledCommandParameter compiledParameterMD)
        {
            _name = compiledParameterMD.Name;
            _parameterType = compiledParameterMD.Type;
            _isDynamic = compiledParameterMD.IsDynamic;

            // Create parameter set metadata
            _parameterSets = new Dictionary<string, ParameterSetMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in compiledParameterMD.ParameterSetData.Keys)
            {
                ParameterSetSpecificMetadata pMD = compiledParameterMD.ParameterSetData[key];
                _parameterSets.Add(key, new ParameterSetMetadata(pMD));
            }

            // Create aliases for this parameter
            _aliases = new Collection<string>();
            foreach (string alias in compiledParameterMD.Aliases)
            {
                _aliases.Add(alias);
            }

            // Create attributes for this parameter
            _attributes = new Collection<Attribute>();
            foreach (var attrib in compiledParameterMD.CompiledAttributes)
            {
                _attributes.Add(attrib);
            }
        }

        
        internal static Dictionary<string, ParameterMetadata> GetParameterMetadata(MergedCommandParameterMetadata
            cmdParameterMetadata)
        {
            Dbg.Assert(cmdParameterMetadata != null, "cmdParameterMetadata cannot be null");

            Dictionary<string, ParameterMetadata> result = new Dictionary<string, ParameterMetadata>(StringComparer.OrdinalIgnoreCase);

            foreach (var keyValuePair in cmdParameterMetadata.BindableParameters)
            {
                var key = keyValuePair.Key;
                var mergedCompiledPMD = keyValuePair.Value;
                ParameterMetadata parameterMetaData = new ParameterMetadata(mergedCompiledPMD.Parameter);
                result.Add(key, parameterMetaData);
            }

            return result;
        }

        internal bool IsMatchingType(PSTypeName psTypeName)
        {
            Type dotNetType = psTypeName.Type;
            if (dotNetType != null)
            {
                // ConstrainedLanguage note - This conversion is analyzed, but actually invoked via regular conversion.
                bool parameterAcceptsObjects =
                    ((int)(LanguagePrimitives.FigureConversion(typeof(object), this.ParameterType).Rank)) >=
                    (int)(ConversionRank.AssignableS2A);
                if (dotNetType.Equals(typeof(object)))
                {
                    return parameterAcceptsObjects;
                }

                if (parameterAcceptsObjects)
                {
                    return (psTypeName.Type != null) && (psTypeName.Type.Equals(typeof(object)));
                }

                // ConstrainedLanguage note - This conversion is analyzed, but actually invoked via regular conversion.
                var conversionData = LanguagePrimitives.FigureConversion(dotNetType, this.ParameterType);
                if (conversionData != null)
                {
                    if ((int)(conversionData.Rank) >= (int)(ConversionRank.NumericImplicitS2A))
                    {
                        return true;
                    }
                }

                return false;
            }

            var wildcardPattern = WildcardPattern.Get(
                "*" + (psTypeName.Name ?? string.Empty),
                WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
            if (wildcardPattern.IsMatch(this.ParameterType.FullName))
            {
                return true;
            }

            if (this.ParameterType.IsArray && wildcardPattern.IsMatch((this.ParameterType.GetElementType().FullName)))
            {
                return true;
            }

            if (this.Attributes != null)
            {
                PSTypeNameAttribute typeNameAttribute = this.Attributes.OfType<PSTypeNameAttribute>().FirstOrDefault();
                if (typeNameAttribute != null && wildcardPattern.IsMatch(typeNameAttribute.PSTypeName))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Proxy Parameter generation

        // The formats are prefixed with {0} to enable easy formatting.
        private const string ParameterNameFormat = @"{0}${{{1}}}";
        private const string ParameterTypeFormat = @"{0}[{1}]";
        private const string ParameterSetNameFormat = "ParameterSetName='{0}'";
        private const string AliasesFormat = @"{0}[Alias({1})]";
        private const string ValidateLengthFormat = @"{0}[ValidateLength({1}, {2})]";
        private const string ValidateRangeRangeKindFormat = @"{0}[ValidateRange([System.Management.Automation.ValidateRangeKind]::{1})]";
        private const string ValidateRangeEnumFormat = @"{0}[ValidateRange([{3}]::{1}, [{3}]::{2})]";
        private const string ValidateRangeFloatFormat = @"{0}[ValidateRange({1:R}, {2:R})]";
        private const string ValidateRangeFormat = @"{0}[ValidateRange({1}, {2})]";
        private const string ValidatePatternFormat = "{0}[ValidatePattern('{1}')]";
        private const string ValidateScriptFormat = @"{0}[ValidateScript({{ {1} }})]";
        private const string ValidateCountFormat = @"{0}[ValidateCount({1}, {2})]";
        private const string ValidateSetFormat = @"{0}[ValidateSet({1})]";
        private const string ValidateNotNullFormat = @"{0}[ValidateNotNull()]";
        private const string ValidateNotNullOrEmptyFormat = @"{0}[ValidateNotNullOrEmpty()]";
        private const string ValidateNotNullOrWhiteSpaceFormat = @"{0}[ValidateNotNullOrWhiteSpace()]";
        private const string AllowNullFormat = @"{0}[AllowNull()]";
        private const string AllowEmptyStringFormat = @"{0}[AllowEmptyString()]";
        private const string AllowEmptyCollectionFormat = @"{0}[AllowEmptyCollection()]";
        private const string PSTypeNameFormat = @"{0}[PSTypeName('{1}')]";
        private const string ObsoleteFormat = @"{0}[Obsolete({1})]";
        private const string CredentialAttributeFormat = @"{0}[System.Management.Automation.CredentialAttribute()]";

        
        internal string GetProxyParameterData(string prefix, string paramNameOverride, bool isProxyForCmdlet)
        {
            Text.StringBuilder result = new System.Text.StringBuilder();

            if (_parameterSets != null && isProxyForCmdlet)
            {
                foreach (var pair in _parameterSets)
                {
                    string parameterSetName = pair.Key;
                    ParameterSetMetadata parameterSet = pair.Value;
                    string paramSetData = parameterSet.GetProxyParameterData();
                    if (!string.IsNullOrEmpty(paramSetData) || !parameterSetName.Equals(ParameterAttribute.AllParameterSets))
                    {
                        string separator = string.Empty;
                        result.Append(prefix);
                        result.Append("[Parameter(");
                        if (!parameterSetName.Equals(ParameterAttribute.AllParameterSets))
                        {
                            result.AppendFormat(
                                CultureInfo.InvariantCulture,
                                ParameterSetNameFormat,
                                CodeGeneration.EscapeSingleQuotedStringContent(parameterSetName));
                            separator = ", ";
                        }

                        if (!string.IsNullOrEmpty(paramSetData))
                        {
                            result.Append(separator);
                            result.Append(paramSetData);
                        }

                        result.Append(")]");
                    }
                }
            }

            if ((_aliases != null) && (_aliases.Count > 0))
            {
                Text.StringBuilder aliasesData = new System.Text.StringBuilder();
                string comma = string.Empty; // comma is not need for the first element

                foreach (string alias in _aliases)
                {
                    aliasesData.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "{0}'{1}'",
                        comma,
                        CodeGeneration.EscapeSingleQuotedStringContent(alias));
                    comma = ",";
                }

                result.AppendFormat(CultureInfo.InvariantCulture, AliasesFormat, prefix, aliasesData.ToString());
            }

            if ((_attributes != null) && (_attributes.Count > 0))
            {
                foreach (Attribute attrib in _attributes)
                {
                    string attribData = GetProxyAttributeData(attrib, prefix);
                    if (!string.IsNullOrEmpty(attribData))
                    {
                        result.Append(attribData);
                    }
                }
            }

            if (SwitchParameter)
            {
                result.AppendFormat(CultureInfo.InvariantCulture, ParameterTypeFormat, prefix, "switch");
            }
            else if (_parameterType != null)
            {
                result.AppendFormat(CultureInfo.InvariantCulture, ParameterTypeFormat, prefix, ToStringCodeMethods.Type(_parameterType));
            }

            
            CredentialAttribute credentialAttrib = _attributes.OfType<CredentialAttribute>().FirstOrDefault();
            if (credentialAttrib != null)
            {
                string attribData = string.Format(CultureInfo.InvariantCulture, CredentialAttributeFormat, prefix);
                if (!string.IsNullOrEmpty(attribData))
                {
                    result.Append(attribData);
                }
            }

            result.AppendFormat(
                CultureInfo.InvariantCulture,
                ParameterNameFormat,
                prefix,
                CodeGeneration.EscapeVariableName(string.IsNullOrEmpty(paramNameOverride) ? _name : paramNameOverride));
            return result.ToString();
        }

        
        private static string GetProxyAttributeData(Attribute attrib, string prefix)
        {
            string result;

            ValidateLengthAttribute validLengthAttrib = attrib as ValidateLengthAttribute;
            if (validLengthAttrib != null)
            {
                result = string.Format(
                    CultureInfo.InvariantCulture,
                    ValidateLengthFormat, prefix,
                    validLengthAttrib.MinLength,
                    validLengthAttrib.MaxLength);
                return result;
            }

            ValidateRangeAttribute validRangeAttrib = attrib as ValidateRangeAttribute;
            if (validRangeAttrib != null)
            {
                if (validRangeAttrib.RangeKind.HasValue)
                {
                    result = string.Format(
                        CultureInfo.InvariantCulture,
                        ValidateRangeRangeKindFormat,
                        prefix,
                        validRangeAttrib.RangeKind.ToString());
                    return result;
                }
                else
                {
                    Type rangeType = validRangeAttrib.MinRange.GetType();
                    string format;

                    if (rangeType == typeof(float) || rangeType == typeof(double))
                    {
                        format = ValidateRangeFloatFormat;
                    }
                    else if (rangeType.IsEnum)
                    {
                        format = ValidateRangeEnumFormat;
                    }
                    else
                    {
                        format = ValidateRangeFormat;
                    }

                    result = string.Format(
                        CultureInfo.InvariantCulture,
                        format,
                        prefix,
                        validRangeAttrib.MinRange,
                        validRangeAttrib.MaxRange,
                        rangeType.FullName);
                    return result;
                }
            }

            AllowNullAttribute allowNullAttrib = attrib as AllowNullAttribute;
            if (allowNullAttrib != null)
            {
                result = string.Format(CultureInfo.InvariantCulture,
                    AllowNullFormat, prefix);
                return result;
            }

            AllowEmptyStringAttribute allowEmptyStringAttrib = attrib as AllowEmptyStringAttribute;
            if (allowEmptyStringAttrib != null)
            {
                result = string.Format(CultureInfo.InvariantCulture,
                    AllowEmptyStringFormat, prefix);
                return result;
            }

            AllowEmptyCollectionAttribute allowEmptyColAttrib = attrib as AllowEmptyCollectionAttribute;
            if (allowEmptyColAttrib != null)
            {
                result = string.Format(CultureInfo.InvariantCulture,
                    AllowEmptyCollectionFormat, prefix);
                return result;
            }

            ValidatePatternAttribute patternAttrib = attrib as ValidatePatternAttribute;
            if (patternAttrib != null)
            {
                

                result = string.Format(CultureInfo.InvariantCulture,
                    ValidatePatternFormat, prefix,
                    CodeGeneration.EscapeSingleQuotedStringContent(patternAttrib.RegexPattern)
                    );
                return result;
            }

            ValidateCountAttribute countAttrib = attrib as ValidateCountAttribute;
            if (countAttrib != null)
            {
                result = string.Format(CultureInfo.InvariantCulture,
                    ValidateCountFormat, prefix, countAttrib.MinLength, countAttrib.MaxLength);
                return result;
            }

            ValidateNotNullAttribute notNullAttrib = attrib as ValidateNotNullAttribute;
            if (notNullAttrib != null)
            {
                result = string.Format(CultureInfo.InvariantCulture,
                    ValidateNotNullFormat, prefix);
                return result;
            }

            ValidateNotNullOrEmptyAttribute notNullEmptyAttrib = attrib as ValidateNotNullOrEmptyAttribute;
            if (notNullEmptyAttrib != null)
            {
                result = string.Format(CultureInfo.InvariantCulture,
                    ValidateNotNullOrEmptyFormat, prefix);
                return result;
            }

            ValidateNotNullOrWhiteSpaceAttribute notNullWhiteSpaceAttrib = attrib as ValidateNotNullOrWhiteSpaceAttribute;
            if (notNullWhiteSpaceAttrib != null)
            {
                result = string.Format(CultureInfo.InvariantCulture,
                    ValidateNotNullOrWhiteSpaceFormat, prefix);
                return result;
            }

            ValidateSetAttribute setAttrib = attrib as ValidateSetAttribute;
            if (setAttrib != null)
            {
                Text.StringBuilder values = new System.Text.StringBuilder();
                string comma = string.Empty;
                foreach (string validValue in setAttrib.ValidValues)
                {
                    values.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "{0}'{1}'",
                        comma,
                        CodeGeneration.EscapeSingleQuotedStringContent(validValue));
                    comma = ",";
                }

                result = string.Format(CultureInfo.InvariantCulture,
                    ValidateSetFormat, prefix, values.ToString());
                return result;
            }

            ValidateScriptAttribute scriptAttrib = attrib as ValidateScriptAttribute;
            if (scriptAttrib != null)
            {
                // Talked with others and I think it is okay to use *unescaped* value from sb.ToString()
                // 1. implicit remoting is not bringing validation scripts across
                // 2. other places in code also assume that contents of a script block can be parsed
                //    without escaping
                result = string.Format(CultureInfo.InvariantCulture,
                    ValidateScriptFormat, prefix, scriptAttrib.ScriptBlock.ToString());
                return result;
            }

            PSTypeNameAttribute psTypeNameAttrib = attrib as PSTypeNameAttribute;
            if (psTypeNameAttrib != null)
            {
                result = string.Format(
                    CultureInfo.InvariantCulture,
                    PSTypeNameFormat,
                    prefix,
                    CodeGeneration.EscapeSingleQuotedStringContent(psTypeNameAttrib.PSTypeName));
                return result;
            }

            ObsoleteAttribute obsoleteAttrib = attrib as ObsoleteAttribute;
            if (obsoleteAttrib != null)
            {
                string parameters = string.Empty;
                if (obsoleteAttrib.IsError)
                {
                    string message = "'" + CodeGeneration.EscapeSingleQuotedStringContent(obsoleteAttrib.Message) + "'";
                    parameters = message + ", $true";
                }
                else if (obsoleteAttrib.Message != null)
                {
                    parameters = "'" + CodeGeneration.EscapeSingleQuotedStringContent(obsoleteAttrib.Message) + "'";
                }

                result = string.Format(
                    CultureInfo.InvariantCulture,
                    ObsoleteFormat,
                    prefix,
                    parameters);
                return result;
            }

            return null;
        }

        #endregion
    }

    
    internal class InternalParameterMetadata
    {
        #region ctor

        
        internal static InternalParameterMetadata Get(RuntimeDefinedParameterDictionary runtimeDefinedParameters,
                                                      bool processingDynamicParameters,
                                                      bool checkNames)
        {
            if (runtimeDefinedParameters == null)
            {
                throw PSTraceSource.NewArgumentNullException("runtimeDefinedParameter");
            }

            return new InternalParameterMetadata(runtimeDefinedParameters, processingDynamicParameters, checkNames);
        }

        
        internal static InternalParameterMetadata Get(Type type, ExecutionContext context, bool processingDynamicParameters)
        {
            if (type == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(type));
            }

            InternalParameterMetadata result;
            if (context == null || !s_parameterMetadataCache.TryGetValue(type.AssemblyQualifiedName, out result))
            {
                result = new InternalParameterMetadata(type, processingDynamicParameters);

                if (context != null)
                {
                    s_parameterMetadataCache.TryAdd(type.AssemblyQualifiedName, result);
                }
            }

            return result;
        }

        //
        
        internal InternalParameterMetadata(RuntimeDefinedParameterDictionary runtimeDefinedParameters, bool processingDynamicParameters, bool checkNames)
        {
            if (runtimeDefinedParameters == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(runtimeDefinedParameters));
            }

            ConstructCompiledParametersUsingRuntimeDefinedParameters(runtimeDefinedParameters, processingDynamicParameters, checkNames);
        }

        //
        
        internal InternalParameterMetadata(Type type, bool processingDynamicParameters)
        {
            if (type == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(type));
            }

            _type = type;
            TypeName = type.Name;

            ConstructCompiledParametersUsingReflection(processingDynamicParameters);
        }

        #endregion ctor

        
        internal string TypeName { get; } = string.Empty;

        
        internal Dictionary<string, CompiledCommandParameter> BindableParameters { get; }

            = new Dictionary<string, CompiledCommandParameter>(StringComparer.OrdinalIgnoreCase);

        
        internal Dictionary<string, CompiledCommandParameter> AliasedParameters { get; }

            = new Dictionary<string, CompiledCommandParameter>(StringComparer.OrdinalIgnoreCase);

        
        private readonly Type _type;

        
        internal static readonly BindingFlags metaDataBindingFlags = (BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        #region helper methods

        
        private void ConstructCompiledParametersUsingRuntimeDefinedParameters(
            RuntimeDefinedParameterDictionary runtimeDefinedParameters,
            bool processingDynamicParameters,
            bool checkNames)
        {
            Diagnostics.Assert(
                runtimeDefinedParameters != null,
                "This method should only be called when constructed with a valid runtime-defined parameter collection");

            foreach (RuntimeDefinedParameter parameterDefinition in runtimeDefinedParameters.Values)
            {
                // Create the compiled parameter and add it to the bindable parameters collection
                if (processingDynamicParameters)
                {
                    // When processing dynamic parameters, parameter definitions come from the user,
                    // Invalid data could be passed in, or the parameter could be actually disabled.
                    if (parameterDefinition == null || parameterDefinition.IsDisabled()) { continue; }
                }

                CompiledCommandParameter parameter = new CompiledCommandParameter(parameterDefinition, processingDynamicParameters);
                AddParameter(parameter, checkNames);
            }
        }

        
        private void ConstructCompiledParametersUsingReflection(bool processingDynamicParameters)
        {
            Diagnostics.Assert(
                _type != null,
                "This method should only be called when constructed with the Type");

            // Get the property and field info

            PropertyInfo[] properties = _type.GetProperties(metaDataBindingFlags);
            FieldInfo[] fields = _type.GetFields(metaDataBindingFlags);

            foreach (PropertyInfo property in properties)
            {
                // Check whether the property is a parameter
                if (!IsMemberAParameter(property))
                {
                    continue;
                }

                AddParameter(property, processingDynamicParameters);
            }

            foreach (FieldInfo field in fields)
            {
                // Check whether the field is a parameter
                if (!IsMemberAParameter(field))
                {
                    continue;
                }

                AddParameter(field, processingDynamicParameters);
            }
        }

        private static void CheckForReservedParameter(string name)
        {
            if (name.Equals("SelectProperty", StringComparison.OrdinalIgnoreCase)
                ||
                name.Equals("SelectObject", StringComparison.OrdinalIgnoreCase))
            {
                throw new MetadataException(
                            "ReservedParameterName",
                            null,
                            DiscoveryExceptions.ReservedParameterName,
                            name);
            }
        }

        // This call verifies that the parameter is unique or
        // can be deemed unique. If not, an exception is thrown.
        // If it is unique (or deemed unique), then it is added
        // to the bindableParameters collection
        //
        private void AddParameter(MemberInfo member, bool processingDynamicParameters)
        {
            bool error = false;
            bool useExisting = false;

            CheckForReservedParameter(member.Name);

            do // false loop
            {
                CompiledCommandParameter existingParameter;
                if (!BindableParameters.TryGetValue(member.Name, out existingParameter))
                {
                    break;
                }

                Type existingParamDeclaringType = existingParameter.DeclaringType;

                if (existingParamDeclaringType == null)
                {
                    error = true;
                    break;
                }

                if (existingParamDeclaringType.IsSubclassOf(member.DeclaringType))
                {
                    useExisting = true;
                    break;
                }

                if (member.DeclaringType.IsSubclassOf(existingParamDeclaringType))
                {
                    // Need to swap out the new member for the parameter definition
                    // that is already defined.

                    RemoveParameter(existingParameter);
                    break;
                }

                error = true;
            } while (false);

            if (error)
            {
                // A duplicate parameter was found and could not be deemed unique
                // through inheritance.

                throw new MetadataException(
                    "DuplicateParameterDefinition",
                    null,
                    ParameterBinderStrings.DuplicateParameterDefinition,
                    member.Name);
            }

            if (!useExisting)
            {
                CompiledCommandParameter parameter = new CompiledCommandParameter(member, processingDynamicParameters);
                AddParameter(parameter, true);
            }
        }

        private void AddParameter(CompiledCommandParameter parameter, bool checkNames)
        {
            if (checkNames)
            {
                CheckForReservedParameter(parameter.Name);
            }

            BindableParameters.Add(parameter.Name, parameter);

            // Now add entries in the parameter aliases collection for any aliases.

            foreach (string alias in parameter.Aliases)
            {
                if (AliasedParameters.ContainsKey(alias))
                {
                    throw new MetadataException(
                            "AliasDeclaredMultipleTimes",
                            null,
                            DiscoveryExceptions.AliasDeclaredMultipleTimes,
                            alias);
                }

                AliasedParameters.Add(alias, parameter);
            }
        }

        private void RemoveParameter(CompiledCommandParameter parameter)
        {
            BindableParameters.Remove(parameter.Name);

            // Now add entries in the parameter aliases collection for any aliases.

            foreach (string alias in parameter.Aliases)
            {
                AliasedParameters.Remove(alias);
            }
        }

        
        private static bool IsMemberAParameter(MemberInfo member)
        {
            try
            {
                var hasAnyVisibleParamAttributes = false;
                var paramAttributes = member.GetCustomAttributes<ParameterAttribute>(inherit: false);
                foreach (var paramAttribute in paramAttributes)
                {
                    if (true)
                    {
                        hasAnyVisibleParamAttributes = true;
                        break;
                    }
                }

                return hasAnyVisibleParamAttributes;
            }
            catch (MetadataException metadataException)
            {
                throw new MetadataException(
                    "GetCustomAttributesMetadataException",
                    metadataException,
                    Metadata.MetadataMemberInitialization,
                    member.Name,
                    metadataException.Message);
            }
            catch (ArgumentException argumentException)
            {
                throw new MetadataException(
                    "GetCustomAttributesArgumentException",
                    argumentException,
                    Metadata.MetadataMemberInitialization,
                    member.Name,
                    argumentException.Message);
            }
        }

        #endregion helper methods

        #region Metadata cache

        
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, InternalParameterMetadata> s_parameterMetadataCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, InternalParameterMetadata>(StringComparer.Ordinal);

        #endregion Metadata cache
    }
}
