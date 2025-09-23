// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Text;

namespace Microsoft.PowerShell.Commands.Utility
{
    
    [Cmdlet(VerbsCommon.Join, "String", RemotingCapability = RemotingCapability.None, DefaultParameterSetName = "default")]
    [OutputType(typeof(string))]
    public sealed class JoinStringCommand : PSCmdlet
    {
        
        private const int DefaultOutputStringCapacity = 256;

        private readonly StringBuilder _outputBuilder = new(DefaultOutputStringCapacity);
        private CultureInfo _cultureInfo = CultureInfo.InvariantCulture;
        private string _separator;
        private char _quoteChar;
        private bool _firstInputObject = true;

        
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PropertyNameCompleter))]
        public PSPropertyExpression Property { get; set; }

        
        [Parameter(Position = 1)]
        [ArgumentCompleter(typeof(SeparatorArgumentCompleter))]
        [AllowEmptyString]
        public string Separator
        {
            get => _separator ?? LanguagePrimitives.ConvertTo<string>(GetVariableValue("OFS"));
            set => _separator = value;
        }

        
        [Parameter]
        [Alias("op")]
        public string OutputPrefix { get; set; }

        
        [Parameter]
        [Alias("os")]
        public string OutputSuffix { get; set; }

        
        [Parameter(ParameterSetName = "SingleQuote")]
        public SwitchParameter SingleQuote { get; set; }

        
        [Parameter(ParameterSetName = "DoubleQuote")]
        public SwitchParameter DoubleQuote { get; set; }

        
        [Parameter(ParameterSetName = "Format")]
        [ArgumentCompleter(typeof(FormatStringArgumentCompleter))]
        public string FormatString { get; set; }

        
        [Parameter]
        public SwitchParameter UseCulture { get; set; }

        
        [Parameter(ValueFromPipeline = true)]
        public PSObject[] InputObject { get; set; }

        protected override void BeginProcessing()
        {
            _quoteChar = SingleQuote ? '\'' : DoubleQuote ? '"' : char.MinValue;
            _outputBuilder.Append(OutputPrefix);
            if (UseCulture)
            {
                _cultureInfo = CultureInfo.CurrentCulture;
            }
        }

        protected override void ProcessRecord()
        {
            if (InputObject != null)
            {
                foreach (PSObject inputObject in InputObject)
                {
                    if (inputObject != null && inputObject != AutomationNull.Value)
                    {
                        var inputValue = Property == null
                                            ? inputObject
                                            : Property.GetValues(inputObject, false, true).FirstOrDefault()?.Result;

                        // conversion to string always succeeds.
                        if (!LanguagePrimitives.TryConvertTo<string>(inputValue, _cultureInfo, out var stringValue))
                        {
                            throw new PSInvalidCastException("InvalidCastFromAnyTypeToString", ExtendedTypeSystem.InvalidCastCannotRetrieveString, null);
                        }

                        if (_firstInputObject)
                        {
                            _firstInputObject = false;
                        }
                        else
                        {
                            _outputBuilder.Append(Separator);
                        }

                        if (_quoteChar != char.MinValue)
                        {
                            _outputBuilder.Append(_quoteChar);
                            _outputBuilder.Append(stringValue);
                            _outputBuilder.Append(_quoteChar);
                        }
                        else if (string.IsNullOrEmpty(FormatString))
                        {
                            _outputBuilder.Append(stringValue);
                        }
                        else
                        {
                            _outputBuilder.AppendFormat(_cultureInfo, FormatString, inputValue);
                        }
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            _outputBuilder.Append(OutputSuffix);
            WriteObject(_outputBuilder.ToString());
        }
    }

    
    public sealed class SeparatorArgumentCompleter : IArgumentCompleter
    {
        private const string NewLineText = "\\n";


        private static readonly CompletionHelpers.CompletionDisplayInfoMapper SeparatorDisplayInfoMapper = separator => separator switch
        {
            "," => (
                ToolTip: TabCompletionStrings.SeparatorCommaToolTip,
                ListItemText: "Comma"),
            ", " => (
                ToolTip: TabCompletionStrings.SeparatorCommaSpaceToolTip,
                ListItemText: "Comma-Space"),
            ";" => (
                ToolTip: TabCompletionStrings.SeparatorSemiColonToolTip,
                ListItemText: "Semi-Colon"),
            "; " => (
                ToolTip: TabCompletionStrings.SeparatorSemiColonSpaceToolTip,
                ListItemText: "Semi-Colon-Space"),
            "-" => (
                ToolTip: TabCompletionStrings.SeparatorDashToolTip,
                ListItemText: "Dash"),
            " " => (
                ToolTip: TabCompletionStrings.SeparatorSpaceToolTip,
                ListItemText: "Space"),
            NewLineText => (
                ToolTip: StringUtil.Format(TabCompletionStrings.SeparatorNewlineToolTip, NewLineText),
                ListItemText: "Newline"),
            _ => (
                ToolTip: separator,
                ListItemText: separator),
        };

        private static readonly IReadOnlyList<string> s_separatorValues = new List<string>(capacity: 7)
        {
            ",",
            ", ",
            ";",
            "; ",
            NewLineText,
            "-",
            " ",
        };

        
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
                => CompletionHelpers.GetMatchingResults(
                    wordToComplete,
                    possibleCompletionValues: s_separatorValues,
                    displayInfoMapper: SeparatorDisplayInfoMapper,
                    resultType: CompletionResultType.ParameterValue);
    }

    
    public sealed class FormatStringArgumentCompleter : IArgumentCompleter
    {
        private static readonly IReadOnlyList<string> s_formatStringValues = new List<string>(capacity: 4)
        {
            "[{0}]",
            "{0:N2}",
            "\\n    `${0}",
            "\\n    [string] `${0}",

        };

        
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
                => CompletionHelpers.GetMatchingResults(
                    wordToComplete,
                    possibleCompletionValues: s_formatStringValues,
                    matchStrategy: CompletionHelpers.WildcardPatternEscapeMatch);
    }
}
