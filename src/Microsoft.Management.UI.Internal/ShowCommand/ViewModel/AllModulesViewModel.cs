// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Windows;

using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public class AllModulesViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        
        private bool waitMessageDisplayed;

        
        private bool noCommonParameter;

        
        private string commandNameFilter;

        
        private List<ModuleViewModel> modules;

        
        private bool canRun;

        
        private bool canCopy;

        
        private ModuleViewModel selectedModule;

        
        private Visibility refreshVisibility = Visibility.Collapsed;

        
        private object extraViewModel;

        
        private double zoomLevel = 1.0;
        #endregion

        #region Construction and Destructor
        
        /// <param name="importedModules">The loaded modules.</param>
        /// <param name="commands">Commands to show.</param>
        public AllModulesViewModel(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands)
        {
            ArgumentNullException.ThrowIfNull(commands);

            if (!commands.GetEnumerator().MoveNext())
            {
                throw new ArgumentNullException("commands");
            }

            this.Initialization(importedModules, commands, true);
        }

        
        /// <param name="importedModules">The loaded modules.</param>
        /// <param name="commands">All PowerShell commands.</param>
        /// <param name="noCommonParameter">True not to show common parameters.</param>
        public AllModulesViewModel(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands, bool noCommonParameter)
        {
            ArgumentNullException.ThrowIfNull(commands);

            this.Initialization(importedModules, commands, noCommonParameter);
        }

        #endregion

        #region INotifyPropertyChanged Members
        
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        
        public event EventHandler<HelpNeededEventArgs> SelectedCommandInSelectedModuleNeedsHelp;

        
        public event EventHandler<ImportModuleEventArgs> SelectedCommandInSelectedModuleNeedsImportModule;

        
        public event EventHandler<CommandEventArgs> RunSelectedCommandInSelectedModule;

        
        public event EventHandler<EventArgs> Refresh;

        #region Public Properties

        
        public double ZoomLevel
        {
            get
            {
                return this.zoomLevel;
            }

            set
            {
                if (value > 0)
                {
                    this.zoomLevel = value / 100.0;
                    this.OnNotifyPropertyChanged("ZoomLevel");
                }
            }
        }

        
        public static string RefreshTooltip
        {
            get { return string.Format(CultureInfo.CurrentUICulture, ShowCommandResources.RefreshShowCommandTooltipFormat, "import-module"); }
        }

        
        public Visibility RefreshVisibility
        {
            get
            {
                return this.refreshVisibility;
            }

            set
            {
                if (this.refreshVisibility == value)
                {
                    return;
                }

                this.refreshVisibility = value;
                this.OnNotifyPropertyChanged("RefreshVisibility");
            }
        }

        
        public bool NoCommonParameter
        {
            get { return this.noCommonParameter; }
        }

        
        public string CommandNameFilter
        {
            get
            {
                return this.commandNameFilter;
            }

            set
            {
                if (this.CommandNameFilter == value)
                {
                    return;
                }

                this.commandNameFilter = value;
                if (this.selectedModule != null)
                {
                    this.selectedModule.RefreshFilteredCommands(this.CommandNameFilter);
                    this.selectedModule.SelectedCommand = null;
                }

                this.OnNotifyPropertyChanged("CommandNameFilter");
            }
        }

        
        public ModuleViewModel SelectedModule
        {
            get
            {
                return this.selectedModule;
            }

            set
            {
                if (this.selectedModule == value)
                {
                    return;
                }

                if (this.selectedModule != null)
                {
                    this.selectedModule.SelectedCommandNeedsImportModule -= this.SelectedModule_SelectedCommandNeedsImportModule;
                    this.selectedModule.SelectedCommandNeedsHelp -= this.SelectedModule_SelectedCommandNeedsHelp;
                    this.selectedModule.RunSelectedCommand -= this.SelectedModule_RunSelectedCommand;
                    this.selectedModule.PropertyChanged -= this.SelectedModule_PropertyChanged;
                }

                this.selectedModule = value;
                this.SetCanRun();
                this.SetCanCopy();

                if (this.selectedModule != null)
                {
                    this.selectedModule.RefreshFilteredCommands(this.CommandNameFilter);
                    this.selectedModule.SelectedCommandNeedsImportModule += this.SelectedModule_SelectedCommandNeedsImportModule;
                    this.selectedModule.SelectedCommandNeedsHelp += this.SelectedModule_SelectedCommandNeedsHelp;
                    this.selectedModule.RunSelectedCommand += this.SelectedModule_RunSelectedCommand;
                    this.selectedModule.PropertyChanged += this.SelectedModule_PropertyChanged;
                    this.selectedModule.SelectedCommand = null;
                }

                this.OnNotifyPropertyChanged("SelectedModule");
            }
        }

        
        public bool CanRun
        {
            get
            {
                return this.canRun;
            }
        }

        
        public bool CanCopy
        {
            get
            {
                return this.canCopy;
            }
        }

        
        public List<ModuleViewModel> Modules
        {
            get { return this.modules; }
        }

        
        public Visibility WaitMessageVisibility
        {
            get
            {
                return this.waitMessageDisplayed ? Visibility.Visible : Visibility.Hidden;
            }
        }

        
        public Visibility MainGridVisibility
        {
            get
            {
                return this.waitMessageDisplayed ? Visibility.Hidden : Visibility.Visible;
            }
        }

        
        public bool MainGridDisplayed
        {
            get
            {
                return !this.waitMessageDisplayed;
            }
        }

        
        public bool WaitMessageDisplayed
        {
            get
            {
                return this.waitMessageDisplayed;
            }

            set
            {
                if (this.waitMessageDisplayed == value)
                {
                    return;
                }

                this.waitMessageDisplayed = value;
                this.SetCanCopy();
                this.SetCanRun();
                this.OnNotifyPropertyChanged("WaitMessageDisplayed");
                this.OnNotifyPropertyChanged("WaitMessageVisibility");
                this.OnNotifyPropertyChanged("MainGridDisplayed");
                this.OnNotifyPropertyChanged("MainGridVisibility");
            }
        }

        
        public object ExtraViewModel
        {
            get
            {
                return this.extraViewModel;
            }

            set
            {
                if (this.extraViewModel == value)
                {
                    return;
                }

                this.extraViewModel = value;
                this.OnNotifyPropertyChanged("ExtraViewModel");
            }
        }
        #endregion

        
        /// <returns>The selected script.</returns>
        public string GetScript()
        {
            if (this.SelectedModule == null)
            {
                return null;
            }

            if (this.SelectedModule.SelectedCommand == null)
            {
                return null;
            }

            return this.SelectedModule.SelectedCommand.GetScript();
        }

        
        internal void OnRefresh()
        {
            EventHandler<EventArgs> handler = this.Refresh;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #region Private Methods
        
        /// <param name="name">The modules name.</param>
        /// <returns>Return true is the module name is ALLModulesViewModel.</returns>
        private static bool IsAll(string name)
        {
            return name.Equals(ShowCommandResources.All, StringComparison.Ordinal);
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedModule_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues")
            {
                this.SetCanRun();
            }
            else if (e.PropertyName == "IsThereASelectedCommand")
            {
                this.SetCanCopy();
            }
        }

        
        private void SetCanRun()
        {
            bool newValue = this.selectedModule != null && this.MainGridDisplayed &&
                this.selectedModule.IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues;

            if (this.canRun == newValue)
            {
                return;
            }

            this.canRun = newValue;
            this.OnNotifyPropertyChanged("CanRun");
        }

        
        private void SetCanCopy()
        {
            bool newValue = this.selectedModule != null && this.MainGridDisplayed && this.selectedModule.IsThereASelectedCommand;

            if (this.canCopy == newValue)
            {
                return;
            }

            this.canCopy = newValue;
            this.OnNotifyPropertyChanged("CanCopy");
        }

        
        /// <param name="importedModules">All loaded modules.</param>
        /// <param name="commands">List of commands in all modules.</param>
        /// <param name="noCommonParameterInModel">Whether showing common parameter.</param>
        private void Initialization(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands, bool noCommonParameterInModel)
        {
            if (commands == null)
            {
                return;
            }

            Dictionary<string, ModuleViewModel> rawModuleViewModels = new Dictionary<string, ModuleViewModel>();

            this.noCommonParameter = noCommonParameterInModel;

            // separates commands in their Modules
            foreach (ShowCommandCommandInfo command in commands)
            {
                ModuleViewModel moduleViewModel;
                if (!rawModuleViewModels.TryGetValue(command.ModuleName, out moduleViewModel))
                {
                    moduleViewModel = new ModuleViewModel(command.ModuleName, importedModules);
                    rawModuleViewModels.Add(command.ModuleName, moduleViewModel);
                }

                CommandViewModel commandViewModel;

                try
                {
                    commandViewModel = CommandViewModel.GetCommandViewModel(moduleViewModel, command, noCommonParameterInModel);
                }
                catch (RuntimeException)
                {
                    continue;
                }

                moduleViewModel.Commands.Add(commandViewModel);
                moduleViewModel.SetAllModules(this);
            }

            // populates this.modules
            this.modules = new List<ModuleViewModel>();

            // if there is just one module then use only it
            if (rawModuleViewModels.Values.Count == 1)
            {
                this.modules.Add(rawModuleViewModels.Values.First());
                this.modules[0].SortCommands(false);
                this.SelectedModule = this.modules[0];
                return;
            }

            // If there are more modules, create an additional module to aggregate all commands
            ModuleViewModel allCommandsModule = new ModuleViewModel(ShowCommandResources.All, null);
            this.modules.Add(allCommandsModule);
            allCommandsModule.SetAllModules(this);

            if (rawModuleViewModels.Values.Count > 0)
            {
                foreach (ModuleViewModel module in rawModuleViewModels.Values)
                {
                    module.SortCommands(false);
                    this.modules.Add(module);

                    allCommandsModule.Commands.AddRange(module.Commands);
                }
            }

            allCommandsModule.SortCommands(true);

            this.modules.Sort(this.Compare);
            this.SelectedModule = this.modules.Count == 0 ? null : this.modules[0];
        }

        
        /// <param name="source">The source ModuleViewModel.</param>
        /// <param name="target">The target ModuleViewModel.</param>
        /// <returns>Compare result.</returns>
        private int Compare(ModuleViewModel source, ModuleViewModel target)
        {
            if (AllModulesViewModel.IsAll(source.Name) && !AllModulesViewModel.IsAll(target.Name))
            {
                return -1;
            }

            if (!AllModulesViewModel.IsAll(source.Name) && AllModulesViewModel.IsAll(target.Name))
            {
                return 1;
            }

            return string.Compare(source.Name, target.Name, StringComparison.OrdinalIgnoreCase);
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedModule_SelectedCommandNeedsHelp(object sender, HelpNeededEventArgs e)
        {
            this.OnSelectedCommandInSelectedModuleNeedsHelp(e);
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedModule_SelectedCommandNeedsImportModule(object sender, ImportModuleEventArgs e)
        {
            this.OnSelectedCommandInSelectedModuleNeedsImportModule(e);
        }

        
        /// <param name="e">Event arguments.</param>
        private void OnSelectedCommandInSelectedModuleNeedsHelp(HelpNeededEventArgs e)
        {
            EventHandler<HelpNeededEventArgs> handler = this.SelectedCommandInSelectedModuleNeedsHelp;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        
        /// <param name="e">Event arguments.</param>
        private void OnSelectedCommandInSelectedModuleNeedsImportModule(ImportModuleEventArgs e)
        {
            EventHandler<ImportModuleEventArgs> handler = this.SelectedCommandInSelectedModuleNeedsImportModule;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedModule_RunSelectedCommand(object sender, CommandEventArgs e)
        {
            this.OnRunSelectedCommandInSelectedModule(e);
        }

        
        /// <param name="e">Event arguments.</param>
        private void OnRunSelectedCommandInSelectedModule(CommandEventArgs e)
        {
            EventHandler<CommandEventArgs> handler = this.RunSelectedCommandInSelectedModule;
            if (handler != null)
            {
                handler(this, e);
            }
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
        #endregion
    }
}
