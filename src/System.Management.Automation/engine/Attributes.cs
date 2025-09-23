// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Security;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System.Management.Automation.Internal
{
    
    [AttributeUsage(AttributeTargets.All)]
    public abstract class CmdletMetadataAttribute : Attribute
    {
        
        internal CmdletMetadataAttribute()
        {
        }
    }

    
    [AttributeUsage(AttributeTargets.All)]
    public abstract class ParsingBaseAttribute : CmdletMetadataAttribute
    {
        
        internal ParsingBaseAttribute()
        {
        }
    }
}

namespace System.Management.Automation
{
    #region Base Metadata Classes

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class ValidateArgumentsAttribute : CmdletMetadataAttribute
    {
        
        protected abstract void Validate(object arguments, EngineIntrinsics engineIntrinsics);

        
        internal void InternalValidate(object o, EngineIntrinsics engineIntrinsics) => Validate(o, engineIntrinsics);

        
        protected ValidateArgumentsAttribute()
        {
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class ValidateEnumeratedArgumentsAttribute : ValidateArgumentsAttribute
    {
        
        protected ValidateEnumeratedArgumentsAttribute() : base()
        {
        }

        
        protected abstract void ValidateElement(object element);

        
        protected sealed override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (LanguagePrimitives.IsNull(arguments))
            {
                throw new ValidationMetadataException(
                    "ArgumentIsEmpty",
                    null,
                    Metadata.ValidateNotNullOrEmptyCollectionFailure);
            }

            var enumerator = _getEnumeratorSite.Target.Invoke(_getEnumeratorSite, arguments);

            if (enumerator == null)
            {
                ValidateElement(arguments);
                return;
            }

            // arguments is IEnumerator
            while (enumerator.MoveNext())
            {
                ValidateElement(enumerator.Current);
            }

            enumerator.Reset();
        }

        private readonly CallSite<Func<CallSite, object, IEnumerator>> _getEnumeratorSite =
            CallSite<Func<CallSite, object, IEnumerator>>.Create(PSEnumerableBinder.Get());
    }

    #endregion Base Metadata Classes

    #region Misc Attributes

    
    public enum DSCResourceRunAsCredential
    {
        
        Default,
        
        NotSupported,
        
        Mandatory,
        
        Optional = Default,
    }

    
    [AttributeUsage(AttributeTargets.Class)]
    public class DscResourceAttribute : CmdletMetadataAttribute
    {
        
        public DSCResourceRunAsCredential RunAsCredential { get; set; }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DscPropertyAttribute : CmdletMetadataAttribute
    {
        
        public bool Key { get; set; }

        
        public bool Mandatory { get; set; }

        
        public bool NotConfigurable { get; set; }
    }

    
    [AttributeUsage(AttributeTargets.Class)]
    public class DscLocalConfigurationManagerAttribute : CmdletMetadataAttribute
    {
    }

    
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class CmdletCommonMetadataAttribute : CmdletMetadataAttribute
    {
        
        public string DefaultParameterSetName { get; set; }

        
        public bool SupportsShouldProcess { get; set; } = false;

        
        public bool SupportsPaging { get; set; } = false;

        
        public bool SupportsTransactions
        {
            get
            {
                return _supportsTransactions;
            }

            set
            {
#if !CORECLR
                _supportsTransactions = value;
#else
                // Disable 'SupportsTransactions' in CoreCLR
                // No transaction supported on CSS due to the lack of System.Transactions namespace
                _supportsTransactions = false;
#endif
            }
        }

        private bool _supportsTransactions = false;

        private ConfirmImpact _confirmImpact = ConfirmImpact.Medium;

        
        public ConfirmImpact ConfirmImpact
        {
            get => SupportsShouldProcess ? _confirmImpact : ConfirmImpact.None;
            set => _confirmImpact = value;
        }

        
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string HelpUri { get; set; } = string.Empty;

        
        public RemotingCapability RemotingCapability { get; set; } = RemotingCapability.PowerShell;
    }

    
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CmdletAttribute : CmdletCommonMetadataAttribute
    {
        
        public string NounName { get; }

        
        public string VerbName { get; }

        
        public CmdletAttribute(string verbName, string nounName)
        {
            // NounName,VerbName have to be Non-Null strings
            if (string.IsNullOrEmpty(nounName))
            {
                throw PSTraceSource.NewArgumentException(nameof(nounName));
            }

            if (string.IsNullOrEmpty(verbName))
            {
                throw PSTraceSource.NewArgumentException(nameof(verbName));
            }

            NounName = nounName;
            VerbName = verbName;
        }
    }

    
    [AttributeUsage(AttributeTargets.Class)]
    public class CmdletBindingAttribute : CmdletCommonMetadataAttribute
    {
        
        public bool PositionalBinding { get; set; } = true;
    }

    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    public sealed class OutputTypeAttribute : CmdletMetadataAttribute
    {
        
        internal OutputTypeAttribute(Type type)
        {
            Type = new[] { new PSTypeName(type) };
        }

        
        internal OutputTypeAttribute(string typeName)
        {
            Type = new[] { new PSTypeName(typeName) };
        }

        
        public OutputTypeAttribute(params Type[] type)
        {
            if (type?.Length > 0)
            {
                Type = new PSTypeName[type.Length];
                for (int i = 0; i < type.Length; i++)
                {
                    Type[i] = new PSTypeName(type[i]);
                }
            }
            else
            {
                Type = Array.Empty<PSTypeName>();
            }
        }

        
        public OutputTypeAttribute(params string[] type)
        {
            if (type?.Length > 0)
            {
                Type = new PSTypeName[type.Length];
                for (int i = 0; i < type.Length; i++)
                {
                    Type[i] = new PSTypeName(type[i]);
                }
            }
            else
            {
                Type = Array.Empty<PSTypeName>();
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public PSTypeName[] Type { get; }

        
        public string ProviderCmdlet { get; set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ParameterSetName
        {
            get => _parameterSetName ??= new[] { ParameterAttribute.AllParameterSets };

            set => _parameterSetName = value;
        }

        private string[] _parameterSetName;
    }

    
    [AttributeUsage(AttributeTargets.Assembly)]
    public class DynamicClassImplementationAssemblyAttribute : Attribute
    {
        
        public string ScriptFile { get; set; }
    }

    #endregion Misc Attributes

    #region Parsing guidelines Attributes
    
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class AliasAttribute : ParsingBaseAttribute
    {
        internal string[] aliasNames;

        
        public IList<string> AliasNames { get => this.aliasNames; }

        
        public AliasAttribute(params string[] aliasNames)
        {
            if (aliasNames == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(aliasNames));
            }

            this.aliasNames = aliasNames;
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class ParameterAttribute : ParsingBaseAttribute
    {
        
        public const string AllParameterSets = "__AllParameterSets";

        
        public ParameterAttribute()
        {
        }

        
        public ParameterAttribute(string experimentName, ExperimentAction experimentAction)
        {
            ExperimentalAttribute.ValidateArguments(experimentName, experimentAction);
            ExperimentName = experimentName;
            ExperimentAction = experimentAction;
        }

        private string _parameterSetName = ParameterAttribute.AllParameterSets;

        private string _helpMessage;
        private string _helpMessageBaseName;
        private string _helpMessageResourceId;

        #region Experimental Feature Related Properties

        
        public string ExperimentName { get; }

        
        public ExperimentAction ExperimentAction { get; }

        internal bool ToHide => EffectiveAction == ExperimentAction.Hide;

        internal bool ToShow => EffectiveAction == ExperimentAction.Show;

        
        private ExperimentAction EffectiveAction
        {
            get
            {
                if (_effectiveAction == ExperimentAction.None)
                {
                    _effectiveAction = ExperimentalFeature.GetActionToTake(ExperimentName, ExperimentAction);
                }

                return _effectiveAction;
            }
        }

        private ExperimentAction _effectiveAction = default(ExperimentAction);

        #endregion

        
        public int Position { get; set; } = int.MinValue;

        
        public string ParameterSetName
        {
            get => _parameterSetName;

            set => _parameterSetName = string.IsNullOrEmpty(value) ? ParameterAttribute.AllParameterSets : value;
        }

        
        public bool Mandatory { get; set; } = false;

        
        public bool ValueFromPipeline { get; set; }

        
        public bool ValueFromPipelineByPropertyName { get; set; }

        
        public bool ValueFromRemainingArguments { get; set; } = false;

        
        public string HelpMessage
        {
            get => _helpMessage;

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException(nameof(HelpMessage));
                }

                _helpMessage = value;
            }
        }

        
        public string HelpMessageBaseName
        {
            get => _helpMessageBaseName;

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException(nameof(HelpMessageBaseName));
                }

                _helpMessageBaseName = value;
            }
        }

        
        public string HelpMessageResourceId
        {
            get => _helpMessageResourceId;

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException(nameof(HelpMessageResourceId));
                }

                _helpMessageResourceId = value;
            }
        }

        
        public bool DontShow { get; set; }
    }

    
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PSTypeNameAttribute : Attribute
    {
        
        public string PSTypeName { get; }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public PSTypeNameAttribute(string psTypeName)
        {
            if (string.IsNullOrEmpty(psTypeName))
            {
                throw PSTraceSource.NewArgumentException(nameof(psTypeName));
            }

            this.PSTypeName = psTypeName;
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SupportsWildcardsAttribute : ParsingBaseAttribute
    {
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PSDefaultValueAttribute : ParsingBaseAttribute
    {
        
        public object Value { get; set; }

        
        public string Help { get; set; }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Event)]
    public sealed class HiddenAttribute : ParsingBaseAttribute
    {
    }

    #endregion Parsing guidelines Attributes

    #region Data validate Attributes

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateLengthAttribute : ValidateEnumeratedArgumentsAttribute
    {
        
        public int MinLength { get; }

        
        public int MaxLength { get; }

        
        protected override void ValidateElement(object element)
        {
            if (!(element is string objectString))
            {
                throw new ValidationMetadataException(
                    "ValidateLengthNotString",
                    null,
                    Metadata.ValidateLengthNotString);
            }

            int len = objectString.Length;

            if (len < MinLength)
            {
                throw new ValidationMetadataException(
                    "ValidateLengthMinLengthFailure",
                    null,
                    Metadata.ValidateLengthMinLengthFailure,
                    MinLength, len);
            }

            if (len > MaxLength)
            {
                throw new ValidationMetadataException(
                    "ValidateLengthMaxLengthFailure",
                    null,
                    Metadata.ValidateLengthMaxLengthFailure,
                    MaxLength, len);
            }
        }

        
        public ValidateLengthAttribute(int minLength, int maxLength) : base()
        {
            if (minLength < 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(minLength), minLength);
            }

            if (maxLength <= 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(maxLength), maxLength);
            }

            if (maxLength < minLength)
            {
                throw new ValidationMetadataException(
                    "ValidateLengthMaxLengthSmallerThanMinLength",
                    null,
                    Metadata.ValidateLengthMaxLengthSmallerThanMinLength);
            }

            MinLength = minLength;
            MaxLength = maxLength;
        }
    }

    
    public enum ValidateRangeKind
    {
        
        Positive,

        
        NonNegative,

        
        Negative,

        
        NonPositive
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateRangeAttribute : ValidateEnumeratedArgumentsAttribute
    {
        
        public object MinRange { get; }

        private readonly IComparable _minComparable;

        
        public object MaxRange { get; }

        private readonly IComparable _maxComparable;

        
        private readonly Type _promotedType;

        
        internal ValidateRangeKind? RangeKind { get => _rangeKind; }

        private readonly ValidateRangeKind? _rangeKind;

        
        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullFailure);
            }

            var o = element as PSObject;
            if (o != null)
            {
                element = o.BaseObject;
            }

            if (_rangeKind.HasValue)
            {
                ValidateRange(element, (ValidateRangeKind)_rangeKind);
            }
            else
            {
                ValidateRange(element);
            }
        }

        
        public ValidateRangeAttribute(object minRange, object maxRange) : base()
        {
            if (minRange == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(minRange));
            }

            if (maxRange == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(maxRange));
            }

            if (maxRange.GetType() != minRange.GetType())
            {
                bool failure = true;
                _promotedType = GetCommonType(minRange.GetType(), maxRange.GetType());
                if (_promotedType != null)
                {
                    if (LanguagePrimitives.TryConvertTo(minRange, _promotedType, out object minResultValue)
                        && LanguagePrimitives.TryConvertTo(maxRange, _promotedType, out object maxResultValue))
                    {
                        minRange = minResultValue;
                        maxRange = maxResultValue;
                        failure = false;
                    }
                }

                if (failure)
                {
                    throw new ValidationMetadataException(
                        "MinRangeNotTheSameTypeOfMaxRange",
                        null,
                        Metadata.ValidateRangeMinRangeMaxRangeType,
                        minRange.GetType().Name, maxRange.GetType().Name);
                }
            }
            else
            {
                _promotedType = minRange.GetType();
            }

            // minRange and maxRange have the same type, so we just need to check one of them
            _minComparable = minRange as IComparable;
            if (_minComparable == null)
            {
                throw new ValidationMetadataException(
                    "MinRangeNotIComparable",
                    null,
                    Metadata.ValidateRangeNotIComparable);
            }

            _maxComparable = maxRange as IComparable;
            Diagnostics.Assert(_maxComparable != null, "maxComparable comes from a type that is IComparable");

            // Thanks to the IComparable test above this will not throw. They have the same type and are IComparable.
            if (_minComparable.CompareTo(maxRange) > 0)
            {
                throw new ValidationMetadataException(
                    "MaxRangeSmallerThanMinRange",
                    null,
                    Metadata.ValidateRangeMaxRangeSmallerThanMinRange);
            }

            MinRange = minRange;
            MaxRange = maxRange;
        }

        
        public ValidateRangeAttribute(ValidateRangeKind kind) : base()
        {
            _rangeKind = kind;
        }

        private static void ValidateRange(object element, ValidateRangeKind rangeKind)
        {
            if (element is TimeSpan ts)
            {
                ValidateTimeSpanRange(ts, rangeKind);
                return;
            }

            Type commonType = GetCommonType(typeof(int), element.GetType());
            if (commonType == null)
            {
                throw new ValidationMetadataException(
                    "ValidationRangeElementType",
                    innerException: null,
                    Metadata.ValidateRangeElementType,
                    element.GetType().Name,
                    nameof(Int32));
            }

            object resultValue;
            IComparable dynamicZero = 0;

            if (LanguagePrimitives.TryConvertTo(element, commonType, out resultValue))
            {
                element = resultValue;

                if (LanguagePrimitives.TryConvertTo(0, commonType, out resultValue))
                {
                    dynamicZero = (IComparable)resultValue;
                }
            }
            else
            {
                throw new ValidationMetadataException(
                    "ValidationRangeElementType",
                    null,
                    Metadata.ValidateRangeElementType,
                    element.GetType().Name,
                    commonType.Name);
            }

            switch (rangeKind)
            {
                case ValidateRangeKind.Positive:
                    if (dynamicZero.CompareTo(element) >= 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangePositiveFailure",
                            null,
                            Metadata.ValidateRangePositiveFailure,
                            element.ToString());
                    }

                    break;
                case ValidateRangeKind.NonNegative:
                    if (dynamicZero.CompareTo(element) > 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangeNonNegativeFailure",
                            null,
                            Metadata.ValidateRangeNonNegativeFailure,
                            element.ToString());
                    }

                    break;
                case ValidateRangeKind.Negative:
                    if (dynamicZero.CompareTo(element) <= 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangeNegativeFailure",
                            null,
                            Metadata.ValidateRangeNegativeFailure,
                            element.ToString());
                    }

                    break;
                case ValidateRangeKind.NonPositive:
                    if (dynamicZero.CompareTo(element) < 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangeNonPositiveFailure",
                            null,
                            Metadata.ValidateRangeNonPositiveFailure,
                            element.ToString());
                    }

                    break;
            }
        }

        private void ValidateRange(object element)
        {
            // MinRange and MaxRange have the same type, so we just need to compare to one of them.
            if (element.GetType() != _promotedType)
            {
                if (LanguagePrimitives.TryConvertTo(element, _promotedType, out object resultValue))
                {
                    element = resultValue;
                }
                else
                {
                    throw new ValidationMetadataException(
                        "ValidationRangeElementType",
                        null,
                        Metadata.ValidateRangeElementType,
                        element.GetType().Name,
                        MinRange.GetType().Name);
                }
            }

            // They are the same type and are all IComparable, so this should not throw
            if (_minComparable.CompareTo(element) > 0)
            {
                throw new ValidationMetadataException(
                    "ValidateRangeTooSmall",
                    null,
                    Metadata.ValidateRangeSmallerThanMinRangeFailure,
                    element.ToString(),
                    MinRange.ToString());
            }

            if (_maxComparable.CompareTo(element) < 0)
            {
                throw new ValidationMetadataException(
                    "ValidateRangeTooBig",
                    null,
                    Metadata.ValidateRangeGreaterThanMaxRangeFailure,
                    element.ToString(),
                    MaxRange.ToString());
            }
        }

        private static void ValidateTimeSpanRange(TimeSpan element, ValidateRangeKind rangeKind)
        {
            TimeSpan zero = TimeSpan.Zero;

            switch (rangeKind)
            {
                case ValidateRangeKind.Positive:
                    if (zero.CompareTo(element) >= 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangePositiveFailure",
                            null,
                            Metadata.ValidateRangePositiveFailure,
                            element.ToString());
                    }

                    break;
                case ValidateRangeKind.NonNegative:
                    if (zero.CompareTo(element) > 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangeNonNegativeFailure",
                            null,
                            Metadata.ValidateRangeNonNegativeFailure,
                            element.ToString());
                    }

                    break;
                case ValidateRangeKind.Negative:
                    if (zero.CompareTo(element) <= 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangeNegativeFailure",
                            null,
                            Metadata.ValidateRangeNegativeFailure,
                            element.ToString());
                    }

                    break;
                case ValidateRangeKind.NonPositive:
                    if (zero.CompareTo(element) < 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangeNonPositiveFailure",
                            null,
                            Metadata.ValidateRangeNonPositiveFailure,
                            element.ToString());
                    }

                    break;
            }
        }

        private static Type GetCommonType(Type minType, Type maxType)
        {
            Type resultType = null;

            TypeCode minTypeCode = LanguagePrimitives.GetTypeCode(minType);
            TypeCode maxTypeCode = LanguagePrimitives.GetTypeCode(maxType);
            TypeCode opTypeCode = (int)minTypeCode >= (int)maxTypeCode ? minTypeCode : maxTypeCode;
            if ((int)opTypeCode <= (int)TypeCode.Int32)
            {
                resultType = typeof(int);
            }
            else if ((int)opTypeCode <= (int)TypeCode.UInt32)
            {
                // If one of the operands is signed, we need to promote to double if the value is negative.
                // We aren't checking the value, so we unconditionally promote to double.
                resultType = LanguagePrimitives.IsSignedInteger(minTypeCode) || LanguagePrimitives.IsSignedInteger(maxTypeCode)
                    ? typeof(double) : typeof(uint);
            }
            else if ((int)opTypeCode <= (int)TypeCode.Int64)
            {
                resultType = typeof(long);
            }
            else if ((int)opTypeCode <= (int)TypeCode.UInt64)
            {
                // If one of the operands is signed, we need to promote to double if the value is negative.
                // We aren't checking the value, so we unconditionally promote to double.
                resultType = LanguagePrimitives.IsSignedInteger(minTypeCode) || LanguagePrimitives.IsSignedInteger(maxTypeCode)
                    ? typeof(double) : typeof(ulong);
            }
            else if (opTypeCode == TypeCode.Decimal)
            {
                resultType = typeof(decimal);
            }
            else if (opTypeCode == TypeCode.Single || opTypeCode == TypeCode.Double)
            {
                resultType = typeof(double);
            }

            return resultType;
        }

        
        internal IEnumerable GetValidatedElements(IEnumerable elementsToValidate)
        {
            foreach (var el in elementsToValidate)
            {
                try
                {
                    ValidateElement(el);
                }
                catch (ValidationMetadataException)
                {
                    // Element was not in range - drop
                    continue;
                }

                yield return el;
            }
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidatePatternAttribute : ValidateEnumeratedArgumentsAttribute
    {
        
        public string RegexPattern { get; }

        
        public RegexOptions Options { get; set; } = RegexOptions.IgnoreCase;

        
        public string ErrorMessage { get; set; }

        
        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullFailure);
            }

            string objectString = element.ToString();
            var regex = new Regex(RegexPattern, Options);
            Match match = regex.Match(objectString);
            if (!match.Success)
            {
                var errorMessageFormat = string.IsNullOrEmpty(ErrorMessage)
                    ? Metadata.ValidatePatternFailure
                    : ErrorMessage;
                throw new ValidationMetadataException(
                    "ValidatePatternFailure",
                    null,
                    errorMessageFormat,
                    objectString, RegexPattern);
            }
        }

        
        public ValidatePatternAttribute(string regexPattern)
        {
            if (string.IsNullOrEmpty(regexPattern))
            {
                throw PSTraceSource.NewArgumentException(nameof(regexPattern));
            }

            RegexPattern = regexPattern;
        }
    }

    
    public sealed class ValidateScriptAttribute : ValidateEnumeratedArgumentsAttribute
    {
        
        public string ErrorMessage { get; set; }

        
        public ScriptBlock ScriptBlock { get; }

        
        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullFailure);
            }

            object result = ScriptBlock.DoInvokeReturnAsIs(
                useLocalScope: true,
                errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe,
                dollarUnder: LanguagePrimitives.AsPSObjectOrNull(element),
                input: AutomationNull.Value,
                scriptThis: AutomationNull.Value,
                args: Array.Empty<object>());

            if (!LanguagePrimitives.IsTrue(result))
            {
                var errorMessageFormat = string.IsNullOrEmpty(ErrorMessage)
                    ? Metadata.ValidateScriptFailure
                    : ErrorMessage;
                throw new ValidationMetadataException(
                    "ValidateScriptFailure",
                    null,
                    errorMessageFormat,
                    element, ScriptBlock);
            }
        }

        
        public ValidateScriptAttribute(ScriptBlock scriptBlock)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(scriptBlock));
            }

            ScriptBlock = scriptBlock;
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateCountAttribute : ValidateArgumentsAttribute
    {
        
        public int MinLength { get; }

        
        public int MaxLength { get; }

        
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            int len = 0;
            if (arguments == null || arguments == AutomationNull.Value)
            {
                // treat a nul list the same as an empty list
                // with a count of zero.
                len = 0;
            }
            else if (arguments is IList il)
            {
                len = il.Count;
            }
            else if (arguments is ICollection ic)
            {
                len = ic.Count;
            }
            else if (arguments is IEnumerable ie)
            {
                IEnumerator e = ie.GetEnumerator();
                while (e.MoveNext())
                {
                    len++;
                }
            }
            else if (arguments is IEnumerator enumerator)
            {
                while (enumerator.MoveNext())
                {
                    len++;
                }
            }
            else
            {
                // No conversion succeeded so throw an exception...
                throw new ValidationMetadataException(
                    "NotAnArrayParameter",
                    null,
                    Metadata.ValidateCountNotInArray);
            }

            if (MinLength == MaxLength && len != MaxLength)
            {
                throw new ValidationMetadataException(
                    "ValidateCountExactFailure",
                    null,
                    Metadata.ValidateCountExactFailure,
                    MaxLength, len);
            }

            if (len < MinLength || len > MaxLength)
            {
                throw new ValidationMetadataException(
                    "ValidateCountMinMaxFailure",
                    null,
                    Metadata.ValidateCountMinMaxFailure,
                    MinLength, MaxLength, len);
            }
        }

        
        public ValidateCountAttribute(int minLength, int maxLength)
        {
            if (minLength < 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(minLength), minLength);
            }

            if (maxLength <= 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(maxLength), maxLength);
            }

            if (maxLength < minLength)
            {
                throw new ValidationMetadataException(
                    "ValidateRangeMaxLengthSmallerThanMinLength",
                    null,
                    Metadata.ValidateCountMaxLengthSmallerThanMinLength);
            }

            MinLength = minLength;
            MaxLength = maxLength;
        }
    }

    
    public abstract class CachedValidValuesGeneratorBase : IValidateSetValuesGenerator
    {
        // Cached valid values.
        private string[] _validValues;
        private readonly int _validValuesCacheExpiration;

        
        protected CachedValidValuesGeneratorBase(int cacheExpirationInSeconds)
        {
            _validValuesCacheExpiration = cacheExpirationInSeconds;
        }

        
        public abstract string[] GenerateValidValues();

        
        public string[] GetValidValues()
        {
            // Because we have a background task to clear the cache by '_validValues = null'
            // we use the local variable to exclude a race condition.
            var validValuesLocal = _validValues;
            if (validValuesLocal != null)
            {
                return validValuesLocal;
            }

            var validValuesNoCache = GenerateValidValues();

            if (validValuesNoCache == null)
            {
                throw new ValidationMetadataException(
                    "ValidateSetGeneratedValidValuesListIsNull",
                    null,
                    Metadata.ValidateSetGeneratedValidValuesListIsNull);
            }

            if (_validValuesCacheExpiration > 0)
            {
                _validValues = validValuesNoCache;
                Task.Delay(_validValuesCacheExpiration * 1000).ContinueWith((task) => _validValues = null);
            }

            return validValuesNoCache;
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateSetAttribute : ValidateEnumeratedArgumentsAttribute
    {
        // We can use either static '_validValues' or dynamic valid values list generated by instance
        // of 'validValuesGenerator'.
        private readonly string[] _validValues;
        private readonly IValidateSetValuesGenerator validValuesGenerator = null;

        // The valid values generator cache works across 'ValidateSetAttribute' instances.
        private static readonly ConcurrentDictionary<Type, IValidateSetValuesGenerator> s_ValidValuesGeneratorCache =
            new ConcurrentDictionary<Type, IValidateSetValuesGenerator>();

        
        public string ErrorMessage { get; set; }

        
        public bool IgnoreCase { get; set; } = true;

        
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "<Pending>")]
        public IList<string> ValidValues
        {
            get
            {
                if (validValuesGenerator == null)
                {
                    return _validValues;
                }

                var validValuesLocal = validValuesGenerator.GetValidValues();

                if (validValuesLocal == null)
                {
                    throw new ValidationMetadataException(
                        "ValidateSetGeneratedValidValuesListIsNull",
                        null,
                        Metadata.ValidateSetGeneratedValidValuesListIsNull);
                }

                return validValuesLocal;
            }
        }

        
        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw new ValidationMetadataException(
                    "ArgumentIsEmpty",
                    null,
                    Metadata.ValidateNotNullFailure);
            }

            string objString = element.ToString();
            foreach (string setString in ValidValues)
            {
                if (CultureInfo.InvariantCulture.CompareInfo.Compare(
                    setString,
                    objString,
                    IgnoreCase ? CompareOptions.IgnoreCase : CompareOptions.None) == 0)
                {
                    return;
                }
            }

            var errorMessageFormat = string.IsNullOrEmpty(ErrorMessage) ? Metadata.ValidateSetFailure : ErrorMessage;
            throw new ValidationMetadataException(
                "ValidateSetFailure",
                null,
                errorMessageFormat,
                element.ToString(), SetAsString());
        }

        private string SetAsString() => string.Join(CultureInfo.CurrentUICulture.TextInfo.ListSeparator, ValidValues);

        
        public ValidateSetAttribute(params string[] validValues)
        {
            if (validValues == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(validValues));
            }

            if (validValues.Length == 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(validValues), validValues);
            }

            _validValues = validValues;
        }

        
        public ValidateSetAttribute(Type valuesGeneratorType)
        {
            // We check 'IsNotPublic' because we don't want allow 'Activator.CreateInstance' create an
            // instance of non-public type.
            if (!typeof(IValidateSetValuesGenerator).IsAssignableFrom(
                valuesGeneratorType) || valuesGeneratorType.IsNotPublic)
            {
                throw PSTraceSource.NewArgumentException(nameof(valuesGeneratorType));
            }

            // Add a valid values generator to the cache.
            // We don't cache valid values; we expect that valid values will be cached in the generator.
            validValuesGenerator = s_ValidValuesGeneratorCache.GetOrAdd(
                valuesGeneratorType, static (key) => (IValidateSetValuesGenerator)Activator.CreateInstance(key));
        }
    }

    
#nullable enable
    public interface IValidateSetValuesGenerator
    {
        
        string[] GetValidValues();
    }
#nullable restore

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateTrustedDataAttribute : ValidateArgumentsAttribute
    {
        
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (ExecutionContext.HasEverUsedConstrainedLanguage &&
                engineIntrinsics.SessionState.Internal.ExecutionContext.LanguageMode == PSLanguageMode.FullLanguage)
            {
                if (ExecutionContext.IsMarkedAsUntrusted(arguments))
                {
                    if (SystemPolicy.GetSystemLockdownPolicy() != SystemEnforcementMode.Audit)
                    {
                        throw new ValidationMetadataException(
                            "ValidateTrustedDataFailure",
                            null,
                            Metadata.ValidateTrustedDataFailure,
                            arguments);
                    }

                    SystemPolicy.LogWDACAuditMessage(
                        context: null,
                        title: Metadata.WDACParameterArgNotTrustedLogTitle,
                        message: StringUtil.Format(Metadata.WDACParameterArgNotTrustedMessage, arguments),
                        fqid: "ParameterArgumentNotTrusted",
                        dropIntoDebugger: true);
                }
            }
        }
    }

    #region Allow

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AllowNullAttribute : CmdletMetadataAttribute
    {
        
        public AllowNullAttribute() { }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AllowEmptyStringAttribute : CmdletMetadataAttribute
    {
        
        public AllowEmptyStringAttribute() { }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AllowEmptyCollectionAttribute : CmdletMetadataAttribute
    {
        
        public AllowEmptyCollectionAttribute() { }
    }

    #endregion Allow

    #region Path validation attributes

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ValidateDriveAttribute : ValidateArgumentsAttribute
    {
        private readonly string[] _validRootDrives;

        
        public IList<string> ValidRootDrives { get => _validRootDrives; }

        
        public ValidateDriveAttribute(params string[] validRootDrives)
        {
            if (validRootDrives == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(validRootDrives));
            }

            _validRootDrives = validRootDrives;
        }

        
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments == null)
            {
                throw new ValidationMetadataException(
                    "PathArgumentIsEmpty",
                    null,
                    Metadata.ValidateNotNullFailure);
            }

            if (!(arguments is string path))
            {
                throw new ValidationMetadataException(
                    "PathArgumentIsNotValid",
                    null,
                    Metadata.ValidateDrivePathArgNotString);
            }

            var resolvedPath = engineIntrinsics.SessionState.Internal.Globber.GetProviderPath(
                path: path,
                context: new CmdletProviderContext(engineIntrinsics.SessionState.Internal.ExecutionContext),
                isTrusted: true,
                provider: out ProviderInfo providerInfo,
                drive: out PSDriveInfo driveInfo);

            string rootDrive = driveInfo.Name;
            if (string.IsNullOrEmpty(rootDrive))
            {
                throw new ValidationMetadataException(
                    "PathArgumentNoRoot",
                    null,
                    Metadata.ValidateDrivePathNoRoot);
            }

            bool rootFound = false;
            foreach (var validDrive in _validRootDrives)
            {
                if (rootDrive.Equals(validDrive, StringComparison.OrdinalIgnoreCase))
                {
                    rootFound = true;
                    break;
                }
            }

            if (!rootFound)
            {
                throw new ValidationMetadataException(
                    "PathRootInvalid",
                    null,
                    Metadata.ValidateDrivePathFailure,
                    rootDrive, ValidDriveListAsString());
            }
        }

        private string ValidDriveListAsString()
        {
            return string.Join(CultureInfo.CurrentUICulture.TextInfo.ListSeparator, _validRootDrives);
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateUserDriveAttribute : ValidateDriveAttribute
    {
        
        public ValidateUserDriveAttribute()
            : base(new string[] { "User" })
        {
        }
    }

    #endregion

    #region NULL validation attributes

    
    public abstract class NullValidationAttributeBase : ValidateArgumentsAttribute
    {
        
        protected bool IsArgumentCollection(Type argumentType, out bool isElementValueType)
        {
            isElementValueType = false;
            var information = new ParameterCollectionTypeInformation(argumentType);
            switch (information.ParameterCollectionType)
            {
                // If 'arguments' is an array, or implement 'IList', or implement 'ICollection<>'
                // then we continue to check each element of the collection.
                case ParameterCollectionType.Array:
                case ParameterCollectionType.IList:
                case ParameterCollectionType.ICollectionGeneric:
                    Type elementType = information.ElementType;
                    isElementValueType = elementType != null && elementType.IsValueType;
                    return true;
                default:
                    return false;
            }
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateNotNullAttribute : NullValidationAttributeBase
    {
        
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (LanguagePrimitives.IsNull(arguments))
            {
                throw new ValidationMetadataException(
                    "ArgumentIsNull",
                    null,
                    Metadata.ValidateNotNullFailure);
            }
            else if (IsArgumentCollection(arguments.GetType(), out bool isElementValueType))
            {
                // If the element of the collection is of value type, then no need to check for null
                // because a value-type value cannot be null.
                if (isElementValueType)
                {
                    return;
                }

                IEnumerator enumerator = LanguagePrimitives.GetEnumerator(arguments);
                while (enumerator.MoveNext())
                {
                    object element = enumerator.Current;
                    if (LanguagePrimitives.IsNull(element))
                    {
                        throw new ValidationMetadataException(
                            "ArgumentIsNull",
                            null,
                            Metadata.ValidateNotNullCollectionFailure);
                    }
                }
            }
        }
    }

    
    public abstract class ValidateNotNullOrAttributeBase : NullValidationAttributeBase
    {
        
        protected readonly bool _checkWhiteSpace;

        
        protected ValidateNotNullOrAttributeBase(bool checkWhiteSpace)
        {
            _checkWhiteSpace = checkWhiteSpace;
        }

        
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (LanguagePrimitives.IsNull(arguments))
            {
                throw new ValidationMetadataException(
                    "ArgumentIsNull",
                    null,
                    Metadata.ValidateNotNullOrEmptyFailure);
            }
            else if (arguments is string str)
            {
                if (_checkWhiteSpace)
                {
                    if (string.IsNullOrWhiteSpace(str))
                    {
                        throw new ValidationMetadataException(
                            "ArgumentIsEmptyOrWhiteSpace",
                            null,
                            Metadata.ValidateNotNullOrWhiteSpaceFailure);
                    }
                }
                else if (string.IsNullOrEmpty(str))
                {
                    throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullOrEmptyFailure);
                }
            }
            else if (IsArgumentCollection(arguments.GetType(), out bool isElementValueType))
            {
                bool isEmpty = true;
                IEnumerator enumerator = LanguagePrimitives.GetEnumerator(arguments);
                if (enumerator.MoveNext())
                {
                    isEmpty = false;
                }

                // If the element of the collection is of value type, then no need to check for null
                // because a value-type value cannot be null.
                if (!isEmpty && !isElementValueType)
                {
                    do
                    {
                        object element = enumerator.Current;
                        if (LanguagePrimitives.IsNull(element))
                        {
                            throw new ValidationMetadataException(
                                "ArgumentIsNull",
                                null,
                                Metadata.ValidateNotNullOrEmptyCollectionFailure);
                        }

                        if (element is string elementAsString)
                        {
                            if (_checkWhiteSpace)
                            {
                                if (string.IsNullOrWhiteSpace(elementAsString))
                                {
                                    throw new ValidationMetadataException(
                                        "ArgumentCollectionContainsEmptyOrWhiteSpace",
                                        null,
                                        Metadata.ValidateNotNullOrWhiteSpaceCollectionFailure);
                                }
                            }
                            else if (string.IsNullOrEmpty(elementAsString))
                            {
                                throw new ValidationMetadataException(
                                    "ArgumentCollectionContainsEmpty",
                                    null,
                                    Metadata.ValidateNotNullOrEmptyCollectionFailure);
                            }
                        }
                    } while (enumerator.MoveNext());
                }

                if (isEmpty)
                {
                    throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullOrEmptyCollectionFailure);
                }
            }
            else if (arguments is IDictionary dict)
            {
                if (dict.Count == 0)
                {
                    throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullOrEmptyCollectionFailure);
                }
            }
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateNotNullOrEmptyAttribute : ValidateNotNullOrAttributeBase
    {
        
        public ValidateNotNullOrEmptyAttribute()
            : base(checkWhiteSpace: false)
        {
        }
    }

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateNotNullOrWhiteSpaceAttribute : ValidateNotNullOrAttributeBase
    {
        
        public ValidateNotNullOrWhiteSpaceAttribute()
            : base(checkWhiteSpace: true)
        {
        }
    }

    #endregion NULL validation attributes

    #endregion Data validate Attributes

    #region Data Generation Attributes

    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class ArgumentTransformationAttribute : CmdletMetadataAttribute
    {
        
        protected ArgumentTransformationAttribute()
        {
        }

        
        public abstract object Transform(EngineIntrinsics engineIntrinsics, object inputData);

        
        internal object TransformInternal(
            EngineIntrinsics engineIntrinsics,
            object inputData,
            bool trackDataInputSource = true)
        {
            object result = Transform(engineIntrinsics, inputData);
            if (trackDataInputSource && engineIntrinsics != null)
            {
                ExecutionContext.PropagateInputSource(
                    inputData,
                    result,
                    engineIntrinsics.SessionState.Internal.LanguageMode);
            }

            return result;
        }

        
        public virtual bool TransformNullOptionalParameters { get => true; }
    }

    #endregion Data Generation Attributes
}
