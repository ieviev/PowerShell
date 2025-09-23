// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public class ParameterSetViewModel : INotifyPropertyChanged
    {
        
        private string name;

        
        private bool allMandatoryParametersHaveValues;

        
        private List<ParameterViewModel> parameters;

        #region Construction and Destructor

        
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "this type is internal, made public only for WPF Binding")]
        public ParameterSetViewModel(
            string name,
            List<ParameterViewModel> parameters)
        {
            ArgumentNullException.ThrowIfNull(name);

            ArgumentNullException.ThrowIfNull(parameters);

            parameters.Sort(Compare);

            this.name = name;
            this.parameters = parameters;
            foreach (ParameterViewModel parameter in this.parameters)
            {
                if (!parameter.IsMandatory)
                {
                    continue;
                }

                parameter.PropertyChanged += this.MandatoryParameter_PropertyChanged;
            }

            this.EvaluateAllMandatoryParametersHaveValues();
        }
        #endregion

        #region INotifyPropertyChanged Members

        
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Property
        
        public string Name
        {
            get { return this.name; }
        }

        
        public List<ParameterViewModel> Parameters
        {
            get { return this.parameters; }
        }

        
        public bool AllMandatoryParametersHaveValues
        {
            get
            {
                return this.allMandatoryParametersHaveValues;
            }

            set
            {
                if (this.allMandatoryParametersHaveValues != value)
                {
                    this.allMandatoryParametersHaveValues = value;
                    this.OnNotifyPropertyChanged("AllMandatoryParametersHaveValues");
                }
            }
        }
        #endregion

        #region Public Method
        
        public string GetScript()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (ParameterViewModel parameter in this.Parameters)
            {
                if (parameter.Value == null)
                {
                    continue;
                }

                if (parameter.Parameter.ParameterType.IsSwitch)
                {
                    if (((bool?)parameter.Value) == true)
                    {
                        builder.Append($"-{parameter.Name} ");
                    }

                    continue;
                }

                string parameterValueString = parameter.Value.ToString();

                if (parameterValueString.Length == 0)
                {
                    continue;
                }

                ShowCommandParameterType parameterType = parameter.Parameter.ParameterType;

                if (parameterType.IsEnum || parameterType.IsString || (parameterType.IsArray && parameterType.ElementType.IsString))
                {
                    parameterValueString = ParameterSetViewModel.GetDelimitedParameter(parameterValueString, "\"", "\"");
                }
                else if (parameterType.IsScriptBlock)
                {
                    parameterValueString = ParameterSetViewModel.GetDelimitedParameter(parameterValueString, "{", "}");
                }
                else
                {
                    parameterValueString = ParameterSetViewModel.GetDelimitedParameter(parameterValueString, "(", ")");
                }

                builder.Append($"-{parameter.Name} {parameterValueString} ");
            }

            return builder.ToString().Trim();
        }

        
        public int GetIndividualParameterCount()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return 0;
            }

            int i = 0;

            foreach (ParameterViewModel p in this.Parameters)
            {
                if (p.IsInSharedParameterSet)
                {
                    return i;
                }

                i++;
            }

            return i;
        }

        #endregion

        #region Internal Method

        
        internal static int Compare(ParameterViewModel source, ParameterViewModel target)
        {
            if (source.Parameter.IsMandatory && !target.Parameter.IsMandatory)
            {
                return -1;
            }

            if (!source.Parameter.IsMandatory && target.Parameter.IsMandatory)
            {
                return 1;
            }

            return string.Compare(source.Parameter.Name, target.Parameter.Name);
        }

        #endregion

        
        private static string GetDelimitedParameter(string parameterValue, string openDelimiter, string closeDelimiter)
        {
            string parameterValueTrimmed = parameterValue.Trim();

            if (parameterValueTrimmed.Length == 0)
            {
                return openDelimiter + parameterValue + closeDelimiter;
            }

            char delimitationChar = ParameterSetViewModel.ParameterNeedsDelimitation(parameterValueTrimmed, openDelimiter.Length == 1 && openDelimiter[0] == '{');
            switch (delimitationChar)
            {
                case '1':
                    return openDelimiter + parameterValue + closeDelimiter;
                case '\'':
                    return '\'' + parameterValue + '\'';
                case '\"':
                    return '\"' + parameterValue + '\"';
                default:
                    return parameterValueTrimmed;
            }
        }

        
        private static char ParameterNeedsDelimitation(string parameterValue, bool requireScriptblock)
        {
            Token[] tokens;
            ParseError[] errors;
            ScriptBlockAst values = Parser.ParseInput("commandName -parameterName " + parameterValue, out tokens, out errors);

            if (values == null || values.EndBlock == null || values.EndBlock.Statements.Count == 0)
            {
                return '1';
            }

            PipelineAst pipeline = values.EndBlock.Statements[0] as PipelineAst;
            if (pipeline == null || pipeline.PipelineElements.Count == 0)
            {
                return '1';
            }

            CommandAst commandAst = pipeline.PipelineElements[0] as CommandAst;

            if (commandAst == null || commandAst.CommandElements.Count == 0)
            {
                return '1';
            }

            // 3 is for CommandName, Parameter and its value
            if (commandAst.CommandElements.Count != 3)
            {
                return '1';
            }

            if (requireScriptblock)
            {
                ScriptBlockExpressionAst scriptAst = commandAst.CommandElements[2] as ScriptBlockExpressionAst;
                return scriptAst == null ? '1' : '0';
            }

            StringConstantExpressionAst stringValue = commandAst.CommandElements[2] as StringConstantExpressionAst;
            if (stringValue != null)
            {
                if (errors.Length == 0)
                {
                    return '0';
                }

                char stringTerminationChar;

                if (stringValue.StringConstantType == StringConstantType.BareWord)
                {
                    stringTerminationChar = parameterValue[0];
                }
                else if (stringValue.StringConstantType == StringConstantType.DoubleQuoted || stringValue.StringConstantType == StringConstantType.DoubleQuotedHereString)
                {
                    stringTerminationChar = '\"';
                }
                else
                {
                    stringTerminationChar = '\'';
                }

                char oppositeTerminationChar = stringTerminationChar == '\"' ? '\'' : '\"';

                // If the string is not terminated, it should be delimited by the opposite string termination character
                return oppositeTerminationChar;
            }

            if (errors.Length != 0)
            {
                return '1';
            }

            return '0';
        }

        
        private void EvaluateAllMandatoryParametersHaveValues()
        {
            bool newCanRun = true;
            foreach (ParameterViewModel parameter in this.parameters)
            {
                if (!parameter.IsMandatory)
                {
                    continue;
                }

                if (!parameter.HasValue)
                {
                    newCanRun = false;
                    break;
                }
            }

            this.AllMandatoryParametersHaveValues = newCanRun;
        }

        
        private void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        
        private void MandatoryParameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("Value", StringComparison.Ordinal))
            {
                return;
            }

            this.EvaluateAllMandatoryParametersHaveValues();
        }
    }
}
