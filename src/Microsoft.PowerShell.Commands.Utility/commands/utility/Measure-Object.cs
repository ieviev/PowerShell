// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    
    public abstract class MeasureInfo
    {
        
        public string Property { get; set; }
    }

    
    public sealed class GenericMeasureInfo : MeasureInfo
    {
        
        public GenericMeasureInfo()
        {
            Average = Sum = Maximum = Minimum = StandardDeviation = null;
        }

        
        public int Count { get; set; }

        
        public double? Average { get; set; }

        
        public double? Sum { get; set; }

        
        public double? Maximum { get; set; }

        
        public double? Minimum { get; set; }

        
        public double? StandardDeviation { get; set; }
    }

    
    public sealed class GenericObjectMeasureInfo : MeasureInfo
    {
        
        public GenericObjectMeasureInfo()
        {
            Average = Sum = StandardDeviation = null;
            Maximum = Minimum = null;
        }

        
        public int Count { get; set; }

        
        public double? Average { get; set; }

        
        public double? Sum { get; set; }

        
        public object Maximum { get; set; }

        
        public object Minimum { get; set; }

        
        public double? StandardDeviation { get; set; }
    }

    
    public sealed class TextMeasureInfo : MeasureInfo
    {
        
        public TextMeasureInfo()
        {
            Lines = Words = Characters = null;
        }

        
        public int? Lines { get; set; }

        
        public int? Words { get; set; }

        
        public int? Characters { get; set; }
    }

    
    [Cmdlet(VerbsDiagnostic.Measure, "Object", DefaultParameterSetName = GenericParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096617", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(GenericMeasureInfo), typeof(TextMeasureInfo), typeof(GenericObjectMeasureInfo))]
    public sealed class MeasureObjectCommand : PSCmdlet
    {
        
        private sealed class MeasureObjectDictionary<TValue> : Dictionary<string, TValue>
            where TValue : new()
        {
            
            internal MeasureObjectDictionary() : base(StringComparer.OrdinalIgnoreCase)
            {
            }

            
            public TValue EnsureEntry(string key)
            {
                TValue val;
                if (!TryGetValue(key, out val))
                {
                    val = new TValue();
                    this[key] = val;
                }

                return val;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private sealed class Statistics
        {
            // Common properties
            internal int count = 0;

            // Generic/Numeric statistics
            internal double sum = 0.0;
            internal double sumPrevious = 0.0;
            internal double variance = 0.0;
            internal object max = null;
            internal object min = null;

            // Text statistics
            internal int characters = 0;
            internal int words = 0;
            internal int lines = 0;
        }

        
        public MeasureObjectCommand()
            : base()
        {
        }

        #region Command Line Switches

        #region Common parameters in both sets

        
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; } = AutomationNull.Value;

        
        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0)]
        public PSPropertyExpression[] Property { get; set; }

        #endregion Common parameters in both sets

        
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter StandardDeviation
        {
            get
            {
                return _measureStandardDeviation;
            }

            set
            {
                _measureStandardDeviation = value;
            }
        }

        private bool _measureStandardDeviation;

        
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter Sum
        {
            get
            {
                return _measureSum;
            }

            set
            {
                _measureSum = value;
            }
        }

        private bool _measureSum;

        
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter AllStats
        {
            get
            {
                return _allStats;
            }

            set
            {
                _allStats = value;
            }
        }

        private bool _allStats;

        
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter Average
        {
            get
            {
                return _measureAverage;
            }

            set
            {
                _measureAverage = value;
            }
        }

        private bool _measureAverage;

        
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter Maximum
        {
            get
            {
                return _measureMax;
            }

            set
            {
                _measureMax = value;
            }
        }

        private bool _measureMax;

        
        [Parameter(ParameterSetName = GenericParameterSet)]
        public SwitchParameter Minimum
        {
            get
            {
                return _measureMin;
            }

            set
            {
                _measureMin = value;
            }
        }

        private bool _measureMin;

        #region TextMeasure ParameterSet
        
        [Parameter(ParameterSetName = TextParameterSet)]
        public SwitchParameter Line
        {
            get
            {
                return _measureLines;
            }

            set
            {
                _measureLines = value;
            }
        }

        private bool _measureLines = false;

        
        [Parameter(ParameterSetName = TextParameterSet)]
        public SwitchParameter Word
        {
            get
            {
                return _measureWords;
            }

            set
            {
                _measureWords = value;
            }
        }

        private bool _measureWords = false;

        
        [Parameter(ParameterSetName = TextParameterSet)]
        public SwitchParameter Character
        {
            get
            {
                return _measureCharacters;
            }

            set
            {
                _measureCharacters = value;
            }
        }

        private bool _measureCharacters = false;

        
        [Parameter(ParameterSetName = TextParameterSet)]
        public SwitchParameter IgnoreWhiteSpace
        {
            get
            {
                return _ignoreWhiteSpace;
            }

            set
            {
                _ignoreWhiteSpace = value;
            }
        }

        private bool _ignoreWhiteSpace;

        #endregion TextMeasure ParameterSet
        #endregion Command Line Switches

        
        private bool IsMeasuringGeneric
        {
            get
            {
                return string.Equals(ParameterSetName, GenericParameterSet, StringComparison.Ordinal);
            }
        }

        
        protected override void BeginProcessing()
        {
            // Sets all other generic parameters to true to get all statistics.
            if (_allStats)
            {
                _measureSum = _measureStandardDeviation = _measureAverage = _measureMax = _measureMin = true;
            }

            // finally call the base class.
            base.BeginProcessing();
        }

        
        protected override void ProcessRecord()
        {
            if (InputObject == null || InputObject == AutomationNull.Value)
            {
                return;
            }

            _totalRecordCount++;

            if (Property == null)
                AnalyzeValue(null, InputObject.BaseObject);
            else
                AnalyzeObjectProperties(InputObject);
        }

        
        private void AnalyzeObjectProperties(PSObject inObj)
        {
            // Keep track of which properties are counted for an
            // input object so that repeated properties won't be
            // counted twice.
            MeasureObjectDictionary<object> countedProperties = new();

            // First iterate over the user-specified list of
            // properties...
            foreach (var expression in Property)
            {
                List<PSPropertyExpression> resolvedNames = expression.ResolveNames(inObj);
                if (resolvedNames == null || resolvedNames.Count == 0)
                {
                    // Insert a blank entry so we can track
                    // property misses in EndProcessing.
                    if (!expression.HasWildCardCharacters)
                    {
                        string propertyName = expression.ToString();
                        _statistics.EnsureEntry(propertyName);
                    }

                    continue;
                }

                // Each property value can potentially refer
                // to multiple properties via globbing. Iterate over
                // the actual property names.
                foreach (PSPropertyExpression resolvedName in resolvedNames)
                {
                    string propertyName = resolvedName.ToString();
                    // skip duplicated properties
                    if (countedProperties.ContainsKey(propertyName))
                    {
                        continue;
                    }

                    List<PSPropertyExpressionResult> tempExprRes = resolvedName.GetValues(inObj);
                    if (tempExprRes == null || tempExprRes.Count == 0)
                    {
                        // Shouldn't happen - would somehow mean
                        // that the property went away between when
                        // we resolved it and when we tried to get its
                        // value.
                        continue;
                    }

                    AnalyzeValue(propertyName, tempExprRes[0].Result);

                    // Remember resolved propertyNames that have been counted
                    countedProperties[propertyName] = null;
                }
            }
        }

        
        private void AnalyzeValue(string propertyName, object objValue)
        {
            propertyName ??= thisObject;

            Statistics stat = _statistics.EnsureEntry(propertyName);

            // Update common properties.
            stat.count++;

            if (_measureCharacters || _measureWords || _measureLines)
            {
                string strValue = (objValue == null) ? string.Empty : objValue.ToString();
                AnalyzeString(strValue, stat);
            }

            if (_measureAverage || _measureSum || _measureStandardDeviation)
            {
                double numValue = 0.0;
                if (!LanguagePrimitives.TryConvertTo(objValue, out numValue))
                {
                    _nonNumericError = true;
                    ErrorRecord errorRecord = new(
                        PSTraceSource.NewInvalidOperationException(MeasureObjectStrings.NonNumericInputObject, objValue),
                        "NonNumericInputObject",
                        ErrorCategory.InvalidType,
                        objValue);
                    WriteError(errorRecord);
                    return;
                }

                AnalyzeNumber(numValue, stat);
            }

            // Measure-Object -MAX -MIN should work with ANYTHING that supports CompareTo
            if (_measureMin)
            {
                stat.min = Compare(objValue, stat.min, true);
            }

            if (_measureMax)
            {
                stat.max = Compare(objValue, stat.max, false);
            }
        }

        
        private static object Compare(object objValue, object statMinOrMaxValue, bool isMin)
        {
            object currentValue = objValue;
            object statValue = statMinOrMaxValue;

            double temp;
            currentValue = ((objValue != null) && LanguagePrimitives.TryConvertTo<double>(objValue, out temp)) ? temp : currentValue;
            statValue = ((statValue != null) && LanguagePrimitives.TryConvertTo<double>(statValue, out temp)) ? temp : statValue;

            if (currentValue != null && statValue != null && !currentValue.GetType().Equals(statValue.GetType()))
            {
                currentValue = PSObject.AsPSObject(currentValue).ToString();
                statValue = PSObject.AsPSObject(statValue).ToString();
            }

            if (statValue == null)
            {
                return objValue;
            }

            int comparisonResult = LanguagePrimitives.Compare(statValue, currentValue, ignoreCase: false, CultureInfo.CurrentCulture);
            return (isMin ? comparisonResult : -comparisonResult) > 0
                ? objValue
                : statMinOrMaxValue;
        }

        
        private static class TextCountUtilities
        {
            
            internal static int CountChar(string inStr, bool ignoreWhiteSpace)
            {
                if (string.IsNullOrEmpty(inStr))
                {
                    return 0;
                }

                if (!ignoreWhiteSpace)
                {
                    return inStr.Length;
                }

                int len = 0;
                foreach (char c in inStr)
                {
                    if (!char.IsWhiteSpace(c))
                    {
                        len++;
                    }
                }

                return len;
            }

            
            internal static int CountWord(string inStr)
            {
                if (string.IsNullOrEmpty(inStr))
                {
                    return 0;
                }

                int wordCount = 0;
                bool wasAWhiteSpace = true;
                foreach (char c in inStr)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        wasAWhiteSpace = true;
                    }
                    else
                    {
                        if (wasAWhiteSpace)
                        {
                            wordCount++;
                        }

                        wasAWhiteSpace = false;
                    }
                }

                return wordCount;
            }

            
            internal static int CountLine(string inStr)
            {
                if (string.IsNullOrEmpty(inStr))
                {
                    return 0;
                }

                int numberOfLines = 0;
                foreach (char c in inStr)
                {
                    if (c == '\n')
                    {
                        numberOfLines++;
                    }
                }
                // 'abc\nd' has two lines
                // but 'abc\n' has one line
                if (inStr[inStr.Length - 1] != '\n')
                {
                    numberOfLines++;
                }

                return numberOfLines;
            }
        }

        
        private void AnalyzeString(string strValue, Statistics stat)
        {
            if (_measureCharacters)
                stat.characters += TextCountUtilities.CountChar(strValue, _ignoreWhiteSpace);
            if (_measureWords)
                stat.words += TextCountUtilities.CountWord(strValue);
            if (_measureLines)
                stat.lines += TextCountUtilities.CountLine(strValue);
        }

        
        private void AnalyzeNumber(double numValue, Statistics stat)
        {
            if (_measureSum || _measureAverage || _measureStandardDeviation)
            {
                stat.sumPrevious = stat.sum;
                stat.sum += numValue;
            }

            if (_measureStandardDeviation && stat.count > 1)
            {
                // Based off of iterative method of calculating variance on
                // https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Online_algorithm
                double avgPrevious = stat.sumPrevious / (stat.count - 1);
                stat.variance *= (stat.count - 2.0) / (stat.count - 1);
                stat.variance += (numValue - avgPrevious) * (numValue - avgPrevious) / stat.count;
            }
        }

        
        private void WritePropertyNotFoundError(string propertyName, string errorId)
        {
            Diagnostics.Assert(Property != null, "no property and no InputObject should have been addressed");
            ErrorRecord errorRecord = new(
                    PSTraceSource.NewArgumentException(propertyName),
                    errorId,
                    ErrorCategory.ObjectNotFound,
                    null);
            errorRecord.ErrorDetails = new ErrorDetails(
                this, "MeasureObjectStrings", "PropertyNotFound", propertyName);
            WriteError(errorRecord);
        }

        
        protected override void EndProcessing()
        {
            // Fix for 917114: If Property is not set,
            // and we aren't passed any records at all,
            // output 0s to emulate wc behavior.
            if (_totalRecordCount == 0 && Property == null)
            {
                _statistics.EnsureEntry(thisObject);
            }

            foreach (string propertyName in _statistics.Keys)
            {
                Statistics stat = _statistics[propertyName];
                if (stat.count == 0 && Property != null)
                {
                    if (Context.IsStrictVersion(2))
                    {
                        string errorId = (IsMeasuringGeneric) ? "GenericMeasurePropertyNotFound" : "TextMeasurePropertyNotFound";
                        WritePropertyNotFoundError(propertyName, errorId);
                    }
                    
                    continue;
                }

                MeasureInfo mi = null;
                if (IsMeasuringGeneric)
                {
                    double temp;
                    if ((stat.min == null || LanguagePrimitives.TryConvertTo<double>(stat.min, out temp)) &&
                        (stat.max == null || LanguagePrimitives.TryConvertTo<double>(stat.max, out temp)))
                    {
                        mi = CreateGenericMeasureInfo(stat, true);
                    }
                    else
                    {
                        mi = CreateGenericMeasureInfo(stat, false);
                    }
                }
                else
                    mi = CreateTextMeasureInfo(stat);

                // Set common properties.
                if (Property != null)
                    mi.Property = propertyName;

                WriteObject(mi);
            }
        }

        
        private MeasureInfo CreateGenericMeasureInfo(Statistics stat, bool shouldUseGenericMeasureInfo)
        {
            double? sum = null;
            double? average = null;
            double? StandardDeviation = null;
            object max = null;
            object min = null;

            if (!_nonNumericError)
            {
                if (_measureSum)
                    sum = stat.sum;

                if (_measureAverage && stat.count > 0)
                    average = stat.sum / stat.count;

                if (_measureStandardDeviation)
                {
                    StandardDeviation = Math.Sqrt(stat.variance);
                }
            }

            if (_measureMax)
            {
                if (shouldUseGenericMeasureInfo && (stat.max != null))
                {
                    double temp;
                    LanguagePrimitives.TryConvertTo<double>(stat.max, out temp);
                    max = temp;
                }
                else
                {
                    max = stat.max;
                }
            }

            if (_measureMin)
            {
                if (shouldUseGenericMeasureInfo && (stat.min != null))
                {
                    double temp;
                    LanguagePrimitives.TryConvertTo<double>(stat.min, out temp);
                    min = temp;
                }
                else
                {
                    min = stat.min;
                }
            }

            if (shouldUseGenericMeasureInfo)
            {
                GenericMeasureInfo gmi = new();
                gmi.Count = stat.count;
                gmi.Sum = sum;
                gmi.Average = average;
                gmi.StandardDeviation = StandardDeviation;
                if (max != null)
                {
                    gmi.Maximum = (double)max;
                }

                if (min != null)
                {
                    gmi.Minimum = (double)min;
                }

                return gmi;
            }
            else
            {
                GenericObjectMeasureInfo gomi = new();
                gomi.Count = stat.count;
                gomi.Sum = sum;
                gomi.Average = average;
                gomi.Maximum = max;
                gomi.Minimum = min;

                return gomi;
            }
        }

        
        private TextMeasureInfo CreateTextMeasureInfo(Statistics stat)
        {
            TextMeasureInfo tmi = new();

            if (_measureCharacters)
                tmi.Characters = stat.characters;
            if (_measureWords)
                tmi.Words = stat.words;
            if (_measureLines)
                tmi.Lines = stat.lines;

            return tmi;
        }

        
        private readonly MeasureObjectDictionary<Statistics> _statistics = new();

        
        private bool _nonNumericError = false;

        
        private int _totalRecordCount = 0;

        
        private const string GenericParameterSet = "GenericMeasure";

        
        private const string TextParameterSet = "TextMeasure";

        
        private const string thisObject = "$_";
    }
}
