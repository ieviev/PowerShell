// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Windows;

using Microsoft.Management.UI;
using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

using SMAI = System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public class CommandViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        
        private const string SharedParameterSetName = "__AllParameterSets";

        
        private static readonly GridLength star = new GridLength(1, GridUnitType.Star);

        
        private ModuleViewModel parentModule;

        
        private string defaultParameterSetName;

        
        private bool areCommonParametersExpanded;

        
        private ParameterSetViewModel selectedParameterSet;

        
        private List<ParameterSetViewModel> parameterSets = new List<ParameterSetViewModel>();

        
        private bool noCommonParameters;

        
        private ParameterSetViewModel comonParameters;

        
        private ShowCommandCommandInfo commandInfo;

        
        private bool selectedParameterSetAllMandatoryParametersHaveValues;

        
        private bool moduleQualifyCommandName;

        
        private GridLength commonParametersHeight;
        #endregion

        
        private CommandViewModel()
        {
        }

        #region INotifyPropertyChanged Members

        
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        
        public event EventHandler<HelpNeededEventArgs> HelpNeeded;

        
        public event EventHandler<EventArgs> ImportModule;

        #region Public Properties
        
        public bool ModuleQualifyCommandName
        {
            get { return this.moduleQualifyCommandName; }
            set { this.moduleQualifyCommandName = value; }
        }

        
        public bool AreCommonParametersExpanded
        {
            get
            {
                return this.areCommonParametersExpanded;
            }

            set
            {
                if (this.areCommonParametersExpanded == value)
                {
                    return;
                }

                this.areCommonParametersExpanded = value;
                this.OnNotifyPropertyChanged("AreCommonParametersExpanded");
                this.SetCommonParametersHeight();
            }
        }

        
        public ParameterSetViewModel SelectedParameterSet
        {
            get
            {
                return this.selectedParameterSet;
            }

            set
            {
                if (this.selectedParameterSet != value)
                {
                    if (this.selectedParameterSet != null)
                    {
                        this.selectedParameterSet.PropertyChanged -= this.SelectedParameterSet_PropertyChanged;
                    }

                    this.selectedParameterSet = value;
                    if (this.selectedParameterSet != null)
                    {
                        this.selectedParameterSet.PropertyChanged += this.SelectedParameterSet_PropertyChanged;
                        this.SelectedParameterSetAllMandatoryParametersHaveValues = this.SelectedParameterSet.AllMandatoryParametersHaveValues;
                    }
                    else
                    {
                        this.SelectedParameterSetAllMandatoryParametersHaveValues = true;
                    }

                    this.OnNotifyPropertyChanged("SelectedParameterSet");
                }
            }
        }

        
        public bool SelectedParameterSetAllMandatoryParametersHaveValues
        {
            get
            {
                return this.selectedParameterSetAllMandatoryParametersHaveValues;
            }

            set
            {
                if (this.selectedParameterSetAllMandatoryParametersHaveValues == value)
                {
                    return;
                }

                this.selectedParameterSetAllMandatoryParametersHaveValues = value;
                this.OnNotifyPropertyChanged("SelectedParameterSetAllMandatoryParametersHaveValues");
            }
        }

        
        public List<ParameterSetViewModel> ParameterSets
        {
            get { return this.parameterSets; }
        }

        
        public Visibility ParameterSetTabControlVisibility
        {
            get { return (this.ParameterSets.Count > 1) && this.IsImported ? Visibility.Visible : Visibility.Collapsed; }
        }

        
        public Visibility SingleParameterSetControlVisibility
        {
            get { return (this.ParameterSets.Count == 1) ? Visibility.Visible : Visibility.Collapsed; }
        }

        
        public ParameterSetViewModel CommonParameters
        {
            get { return this.comonParameters; }
        }

        
        public Visibility CommonParameterVisibility
        {
            get { return this.noCommonParameters || (this.CommonParameters.Parameters.Count == 0) ? Visibility.Collapsed : Visibility.Visible; }
        }

        
        public GridLength CommonParametersHeight
        {
            get
            {
                return this.commonParametersHeight;
            }

            set
            {
                if (this.commonParametersHeight == value)
                {
                    return;
                }

                this.commonParametersHeight = value;
                this.OnNotifyPropertyChanged("CommonParametersHeight");
            }
        }

        
        public Visibility NotImportedVisibility
        {
            get
            {
                return this.IsImported ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        
        public Visibility NoParameterVisibility
        {
            get
            {
                bool hasNoParameters = this.ParameterSets.Count == 0 || (this.ParameterSets.Count == 1 && this.ParameterSets[0].Parameters.Count == 0);
                return this.IsImported && hasNoParameters ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        
        public bool IsImported
        {
            get
            {
                return this.commandInfo.Module == null || this.ParentModule.IsModuleImported;
            }
        }

        
        public string Name
        {
            get
            {
                if (this.commandInfo != null)
                {
                    return this.commandInfo.Name;
                }

                return string.Empty;
            }
        }

        
        public string ModuleName
        {
            get
            {
                if (this.commandInfo != null && this.commandInfo.ModuleName != null)
                {
                    return this.commandInfo.ModuleName;
                }

                return string.Empty;
            }
        }

        
        public ModuleViewModel ParentModule
        {
            get
            {
                return this.parentModule;
            }
        }

        
        public string ToolTip
        {
            get
            {
                return string.Format(
                     CultureInfo.CurrentCulture,
                     ShowCommandResources.CmdletTooltipFormat,
                     this.Name,
                     this.ParentModule.DisplayName,
                     this.IsImported ? ShowCommandResources.Imported : ShowCommandResources.NotImported);
            }
        }

        
        public string ImportModuleMessage
        {
            get
            {
                return string.Format(
                     CultureInfo.CurrentCulture,
                     ShowCommandResources.NotImportedFormat,
                     this.ModuleName,
                     this.Name,
                     ShowCommandResources.ImportModuleButtonText);
            }
        }

        
        public string DetailsTitle
        {
            get
            {
                if (this.IsImported)
                {
                    return string.Format(
                         CultureInfo.CurrentCulture,
                         ShowCommandResources.DetailsParameterTitleFormat,
                         this.Name);
                }
                else
                {
                    return string.Format(
                         CultureInfo.CurrentCulture,
                         ShowCommandResources.NameLabelFormat,
                         this.Name);
                }
            }
        }
        #endregion

        
        internal static GridLength Star
        {
            get { return CommandViewModel.star; }
        }

        
        /// <returns>Return script as string.</returns>
        public string GetScript()
        {
            StringBuilder builder = new StringBuilder();

            string commandName = this.commandInfo.CommandType == CommandTypes.ExternalScript ? this.commandInfo.Definition : this.Name;

            if (this.ModuleQualifyCommandName && !string.IsNullOrEmpty(this.ModuleName))
            {
                commandName = this.ModuleName + "\\" + commandName;
            }

            if (commandName.Contains(' '))
            {
                builder.Append($"& \"{commandName}\"");
            }
            else
            {
                builder.Append(commandName);
            }

            builder.Append(' ');

            if (this.SelectedParameterSet != null)
            {
                builder.Append(this.SelectedParameterSet.GetScript());
                builder.Append(' ');
            }

            if (this.CommonParameters != null)
            {
                builder.Append(this.CommonParameters.GetScript());
            }

            string script = builder.ToString();

            return script.Trim();
        }

        
        public void OpenHelpWindow()
        {
            this.OnHelpNeeded();
        }

        
        /// <param name="name">The name of ShareParameterSet.</param>
        /// <returns>Return true is ShareParameterSet. Else return false.</returns>
        internal static bool IsSharedParameterSetName(string name)
        {
            return name.Equals(CommandViewModel.SharedParameterSetName, StringComparison.OrdinalIgnoreCase);
        }

        
        /// <param name="module">Module to which the CommandViewModel will belong to.</param>
        /// <param name="commandInfo">Will showing command.</param>
        /// <param name="noCommonParameters">True to ommit displaying common parameter.</param>
        /// <exception cref="ArgumentNullException">If commandInfo is null</exception>
        /// <exception cref="RuntimeException">
        /// If could not create the CommandViewModel. For instance the ShowCommandCommandInfo corresponding to
        /// the following function will throw a RuntimeException when the ShowCommandCommandInfo Parameters
        /// are retrieved:
        /// function CrashMe ([I.Am.A.Type.That.Does.Not.Exist]$name) {}
        /// </exception>
        /// <returns>The CommandViewModel corresponding to commandInfo.</returns>
        internal static CommandViewModel GetCommandViewModel(ModuleViewModel module, ShowCommandCommandInfo commandInfo, bool noCommonParameters)
        {
            ArgumentNullException.ThrowIfNull(commandInfo);

            CommandViewModel returnValue = new CommandViewModel();
            returnValue.commandInfo = commandInfo;
            returnValue.noCommonParameters = noCommonParameters;
            returnValue.parentModule = module;

            Dictionary<string, ParameterViewModel> commonParametersTable = new Dictionary<string, ParameterViewModel>();

            foreach (ShowCommandParameterSetInfo parameterSetInfo in commandInfo.ParameterSets)
            {
                if (parameterSetInfo.IsDefault)
                {
                    returnValue.defaultParameterSetName = parameterSetInfo.Name;
                }

                List<ParameterViewModel> parametersForParameterSet = new List<ParameterViewModel>();
                foreach (ShowCommandParameterInfo parameterInfo in parameterSetInfo.Parameters)
                {
                    bool isCommon = Cmdlet.CommonParameters.Contains(parameterInfo.Name);

                    if (isCommon)
                    {
                        if (!commonParametersTable.ContainsKey(parameterInfo.Name))
                        {
                            commonParametersTable.Add(parameterInfo.Name, new ParameterViewModel(parameterInfo, parameterSetInfo.Name));
                        }

                        continue;
                    }

                    parametersForParameterSet.Add(new ParameterViewModel(parameterInfo, parameterSetInfo.Name));
                }

                if (parametersForParameterSet.Count != 0)
                {
                    returnValue.ParameterSets.Add(new ParameterSetViewModel(parameterSetInfo.Name, parametersForParameterSet));
                }
            }

            List<ParameterViewModel> commonParametersList = commonParametersTable.Values.ToList<ParameterViewModel>();
            returnValue.comonParameters = new ParameterSetViewModel(string.Empty, commonParametersList);

            returnValue.parameterSets.Sort(returnValue.Compare);

            if (returnValue.parameterSets.Count > 0)
            {
                // Setting SelectedParameterSet will also set SelectedParameterSetAllMandatoryParametersHaveValues
                returnValue.SelectedParameterSet = returnValue.ParameterSets[0];
            }
            else
            {
                returnValue.SelectedParameterSetAllMandatoryParametersHaveValues = true;
            }

            returnValue.SetCommonParametersHeight();

            return returnValue;
        }

        
        internal void OnHelpNeeded()
        {
            EventHandler<HelpNeededEventArgs> handler = this.HelpNeeded;
            if (handler != null)
            {
                handler(this, new HelpNeededEventArgs(this.Name));
            }
        }

        
        internal void OnImportModule()
        {
            EventHandler<EventArgs> handler = this.ImportModule;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #region Private Methods
        
        private void SetCommonParametersHeight()
        {
            this.CommonParametersHeight = this.AreCommonParametersExpanded ? CommandViewModel.Star : GridLength.Auto;
        }

        
        /// <param name="source">Source paremeterset.</param>
        /// <param name="target">Target parameterset.</param>
        /// <returns>0 if they are the same, -1 if source is smaller, 1 if source is larger.</returns>
        private int Compare(ParameterSetViewModel source, ParameterSetViewModel target)
        {
            if (this.defaultParameterSetName != null)
            {
                if (source.Name.Equals(this.defaultParameterSetName) && target.Name.Equals(this.defaultParameterSetName))
                {
                    return 0;
                }

                if (source.Name.Equals(this.defaultParameterSetName, StringComparison.Ordinal))
                {
                    return -1;
                }

                if (target.Name.Equals(this.defaultParameterSetName, StringComparison.Ordinal))
                {
                    return 1;
                }
            }

            return string.CompareOrdinal(source.Name, target.Name);
        }

        
        /// <param name="propertyName">The changed property.</param>
        private void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedParameterSet_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("AllMandatoryParametersHaveValues"))
            {
                return;
            }

            this.SelectedParameterSetAllMandatoryParametersHaveValues = this.SelectedParameterSet.AllMandatoryParametersHaveValues;
        }

        #endregion
    }
}
