// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using System.Windows;

using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public class ModuleViewModel : INotifyPropertyChanged
    {
        
        private bool isModuleImported;

        
        private string name;

        
        private ObservableCollection<CommandViewModel> filteredCommands;

        
        private CommandViewModel selectedCommand;

        
        private List<CommandViewModel> commands;

        
        private bool isThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues;

        
        private bool isThereASelectedCommand;

        
        private AllModulesViewModel allModules;

        #region Construction and Destructor
        
        public ModuleViewModel(string name, Dictionary<string, ShowCommandModuleInfo> importedModules)
        {
            ArgumentNullException.ThrowIfNull(name);

            this.name = name;
            this.commands = new List<CommandViewModel>();
            this.filteredCommands = new ObservableCollection<CommandViewModel>();

            // This check looks to see if the given module name shows up in
            // the set of modules that are known to be imported in the current
            // session.  In remote PowerShell sessions, the core cmdlet module
            // Microsoft.PowerShell.Core doesn't appear as being imported despite
            // always being loaded by default.  To make sure we don't incorrectly
            // mark this module as not imported, check for it by name.
            this.isModuleImported =
                importedModules == null ? true : name.Length == 0 ||
                importedModules.ContainsKey(name) ||
                string.Equals("Microsoft.PowerShell.Core", name, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region INotifyPropertyChanged Members

        
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        
        public event EventHandler<HelpNeededEventArgs> SelectedCommandNeedsHelp;

        
        public event EventHandler<ImportModuleEventArgs> SelectedCommandNeedsImportModule;

        
        public event EventHandler<CommandEventArgs> RunSelectedCommand;

        #region Public Property
        
        public string Name
        {
            get { return this.name; }
        }

        
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(this.name))
                {
                    return this.name;
                }

                return ShowCommandResources.NoModuleName;
            }
        }

        
        public Visibility CommandControlVisibility
        {
            get { return this.selectedCommand == null ? Visibility.Collapsed : Visibility.Visible; }
        }

        
        public GridLength CommandRowHeight
        {
            get { return this.selectedCommand == null ? GridLength.Auto : CommandViewModel.Star; }
        }

        
        public List<CommandViewModel> Commands
        {
            get { return this.commands; }
        }

        
        public ObservableCollection<CommandViewModel> FilteredCommands
        {
            get { return this.filteredCommands; }
        }

        
        public CommandViewModel SelectedCommand
        {
            get
            {
                return this.selectedCommand;
            }

            set
            {
                if (value == this.selectedCommand)
                {
                    return;
                }

                if (this.selectedCommand != null)
                {
                    this.selectedCommand.PropertyChanged -= this.SelectedCommand_PropertyChanged;
                    this.selectedCommand.HelpNeeded -= this.SelectedCommand_HelpNeeded;
                    this.selectedCommand.ImportModule -= this.SelectedCommand_ImportModule;
                }

                this.selectedCommand = value;

                this.SetIsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues();

                if (this.selectedCommand != null)
                {
                    this.selectedCommand.PropertyChanged += this.SelectedCommand_PropertyChanged;
                    this.selectedCommand.HelpNeeded += this.SelectedCommand_HelpNeeded;
                    this.selectedCommand.ImportModule += this.SelectedCommand_ImportModule;
                    this.IsThereASelectedCommand = true;
                }
                else
                {
                    this.IsThereASelectedCommand = false;
                }

                this.OnNotifyPropertyChanged("SelectedCommand");
                this.OnNotifyPropertyChanged("CommandControlVisibility");
                this.OnNotifyPropertyChanged("CommandRowHeight");
            }
        }

        
        public bool IsThereASelectedCommand
        {
            get
            {
                return this.isThereASelectedCommand;
            }

            set
            {
                if (value == this.isThereASelectedCommand)
                {
                    return;
                }

                this.isThereASelectedCommand = value;
                this.OnNotifyPropertyChanged("IsThereASelectedCommand");
            }
        }

        
        public bool IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues
        {
            get
            {
                return this.isThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues;
            }

            set
            {
                if (value == this.isThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues)
                {
                    return;
                }

                this.isThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues = value;

                this.OnNotifyPropertyChanged("IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues");
            }
        }

        
        public AllModulesViewModel AllModules
        {
            get
            {
                return this.allModules;
            }
        }
        #endregion

        
        internal bool IsModuleImported
        {
            get
            {
                return this.isModuleImported;
            }
        }

        
        internal void SetAllModules(AllModulesViewModel parentAllModules)
        {
            this.allModules = parentAllModules;
        }

        
        internal void SortCommands(bool markRepeatedCmdlets)
        {
            this.commands.Sort(this.Compare);

            if (!markRepeatedCmdlets || this.commands.Count == 0)
            {
                return;
            }

            CommandViewModel reference = this.commands[0];
            for (int i = 1; i < this.commands.Count; i++)
            {
                CommandViewModel command = this.commands[i];
                if (reference.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase))
                {
                    reference.ModuleQualifyCommandName = true;
                    command.ModuleQualifyCommandName = true;
                }
                else
                {
                    reference = command;
                }
            }
        }

        
        internal void RefreshFilteredCommands(string filter)
        {
            this.filteredCommands.Clear();
            if (string.IsNullOrEmpty(filter))
            {
                foreach (CommandViewModel command in this.Commands)
                {
                    this.filteredCommands.Add(command);
                }

                return;
            }

            WildcardPattern filterPattern = null;
            if (WildcardPattern.ContainsWildcardCharacters(filter))
            {
                filterPattern = new WildcardPattern(filter, WildcardOptions.IgnoreCase);
            }

            foreach (CommandViewModel command in this.Commands)
            {
                if (ModuleViewModel.Matches(filterPattern, command.Name, filter))
                {
                    this.filteredCommands.Add(command);
                    continue;
                }

                if (filterPattern != null)
                {
                    continue;
                }

                string[] textSplit = filter.Split(' ');
                if (textSplit.Length != 2)
                {
                    continue;
                }

                if (ModuleViewModel.Matches(filterPattern, command.Name, textSplit[0] + "-" + textSplit[1]))
                {
                    this.filteredCommands.Add(command);
                }
            }
        }

        
        internal void OnRunSelectedCommand()
        {
            EventHandler<CommandEventArgs> handler = this.RunSelectedCommand;
            if (handler != null)
            {
                handler(this, new CommandEventArgs(this.SelectedCommand));
            }
        }

        
        internal void OnSelectedCommandNeedsHelp(HelpNeededEventArgs e)
        {
            EventHandler<HelpNeededEventArgs> handler = this.SelectedCommandNeedsHelp;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        
        internal void OnSelectedCommandNeedsImportModule()
        {
            EventHandler<ImportModuleEventArgs> handler = this.SelectedCommandNeedsImportModule;
            if (handler != null)
            {
                handler(this, new ImportModuleEventArgs(this.SelectedCommand.Name, this.SelectedCommand.ModuleName, this.Name));
            }
        }
        #region Private Method

        
        private static bool Matches(WildcardPattern filterPattern, string commandName, string filter)
        {
            if (filterPattern != null)
            {
                return filterPattern.IsMatch(commandName);
            }

            return ModuleViewModel.MatchesEvenIfInPlural(commandName, filter);
        }

        
        private static bool MatchesEvenIfInPlural(string commandName, string filter)
        {
            if (commandName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (filter.Length > 5 && filter.EndsWith("es", StringComparison.OrdinalIgnoreCase))
            {
                ReadOnlySpan<char> filterSpan = filter.AsSpan(0, filter.Length - 2);
                return commandName.AsSpan().Contains(filterSpan, StringComparison.OrdinalIgnoreCase);
            }

            if (filter.Length > 4 && filter.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                ReadOnlySpan<char> filterSpan = filter.AsSpan(0, filter.Length - 1);
                return commandName.AsSpan().Contains(filterSpan, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        
        private void SelectedCommand_HelpNeeded(object sender, HelpNeededEventArgs e)
        {
            this.OnSelectedCommandNeedsHelp(e);
        }

        
        private void SelectedCommand_ImportModule(object sender, EventArgs e)
        {
            this.OnSelectedCommandNeedsImportModule();
        }

        
        private void SelectedCommand_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("SelectedParameterSetAllMandatoryParametersHaveValues"))
            {
                return;
            }

            this.SetIsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues();
        }

        
        private void SetIsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues()
        {
            this.IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues =
                this.selectedCommand != null &&
                this.selectedCommand.IsImported &&
                this.selectedCommand.SelectedParameterSetAllMandatoryParametersHaveValues;
        }

        
        private int Compare(CommandViewModel source, CommandViewModel target)
        {
            return string.Compare(source.Name, target.Name, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        
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
