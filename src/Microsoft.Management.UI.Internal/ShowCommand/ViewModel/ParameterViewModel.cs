// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Management.Automation;
using System.Text;

using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public class ParameterViewModel : INotifyPropertyChanged
    {
        
        private ShowCommandParameterInfo parameter;

        
        private object parameterValue;

        
        private string parameterSetName;

        #region Construction and Destructor
        
        public ParameterViewModel(ShowCommandParameterInfo parameter, string parameterSetName)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            ArgumentNullException.ThrowIfNull(parameterSetName);

            this.parameter = parameter;
            this.parameterSetName = parameterSetName;

            if (this.parameter.ParameterType.IsSwitch)
            {
                this.parameterValue = false;
            }
            else
            {
                this.parameterValue = string.Empty;
            }
        }
        #endregion

        #region INotifyPropertyChanged Members

        
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties
        
        public ShowCommandParameterInfo Parameter
        {
            get { return this.parameter; }
        }

        
        public object Value
        {
            get
            {
                return this.parameterValue;
            }

            set
            {
                if (this.parameterValue != value)
                {
                    this.parameterValue = value;
                    this.OnNotifyPropertyChanged("Value");
                }
            }
        }

        
        public string Name
        {
            get { return this.Parameter.Name; }
        }

        
        public string ParameterSetName
        {
            get { return this.parameterSetName; }
        }

        
        public bool IsInSharedParameterSet
        {
            get { return CommandViewModel.IsSharedParameterSetName(this.parameterSetName); }
        }

        
        public string NameTextLabel
        {
            get
            {
                return this.Parameter.IsMandatory ?
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        ShowCommandResources.MandatoryNameLabelFormat,
                        this.Name,
                        ShowCommandResources.MandatoryLabelSegment) :
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        ShowCommandResources.NameLabelFormat,
                        this.Name);
            }
        }

        
        public string NameCheckLabel
        {
            get
            {
                string returnValue = this.Parameter.Name;
                if (this.Parameter.IsMandatory)
                {
                    returnValue = string.Create(CultureInfo.CurrentUICulture, $"{returnValue}{ShowCommandResources.MandatoryLabelSegment}");
                }

                return returnValue;
            }
        }

        
        public string ToolTip
        {
            get
            {
                return ParameterViewModel.EvaluateTooltip(
                    this.Parameter.ParameterType.FullName,
                    this.Parameter.Position,
                    this.Parameter.IsMandatory,
                    this.IsInSharedParameterSet,
                    this.Parameter.ValueFromPipeline);
            }
        }

        
        public bool IsMandatory
        {
            get { return this.Parameter.IsMandatory; }
        }

        
        public bool HasValue
        {
            get
            {
                if (this.Value == null)
                {
                    return false;
                }

                if (this.Parameter.ParameterType.IsSwitch)
                {
                    return ((bool?)this.Value) == true;
                }

                return this.Value.ToString().Length != 0;
            }
        }
        #endregion

        
        internal static string EvaluateTooltip(string typeName, int position, bool mandatory, bool shared, bool valueFromPipeline)
        {
            StringBuilder returnValue = new StringBuilder(string.Format(
                    CultureInfo.CurrentCulture,
                    ShowCommandResources.TypeFormat,
                    typeName));
            string newlineFormatString = Environment.NewLine + "{0}";

            if (position >= 0)
            {
                string positionFormat = string.Format(
                    CultureInfo.CurrentCulture,
                    ShowCommandResources.PositionFormat,
                    position);

                returnValue.AppendFormat(CultureInfo.InvariantCulture, newlineFormatString, positionFormat);
            }

            string optionalOrMandatory = mandatory ? ShowCommandResources.Mandatory : ShowCommandResources.Optional;

            returnValue.AppendFormat(CultureInfo.InvariantCulture, newlineFormatString, optionalOrMandatory);

            if (shared)
            {
                returnValue.AppendFormat(CultureInfo.InvariantCulture, newlineFormatString, ShowCommandResources.CommonToAllParameterSets);
            }

            if (valueFromPipeline)
            {
                returnValue.AppendFormat(CultureInfo.InvariantCulture, newlineFormatString, ShowCommandResources.CanReceiveValueFromPipeline);
            }

            return returnValue.ToString();
        }

        
        private void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
