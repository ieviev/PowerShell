// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public partial class CmdletControl : UserControl
    {
        
        private CommandViewModel currentCommandViewModel;

        #region Construction and Destructor
        
        public CmdletControl()
        {
            InitializeComponent();
            this.NotImportedControl.ImportModuleButton.Click += ImportModuleButton_Click;
            this.ParameterSetTabControl.DataContextChanged += new DependencyPropertyChangedEventHandler(this.ParameterSetTabControl_DataContextChanged);
            this.KeyDown += this.CmdletControl_KeyDown;
            this.helpButton.innerButton.Click += this.HelpButton_Click;
        }
        #endregion

        #region Properties
        
        private CommandViewModel CurrentCommandViewModel
        {
            get { return this.currentCommandViewModel; }
        }
        #endregion

        #region Private Events

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void ParameterSetTabControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext == null)
            {
                return;
            }

            CommandViewModel viewModel = (CommandViewModel)this.DataContext;
            this.currentCommandViewModel = viewModel;

            if (viewModel.ParameterSets.Count == 0)
            {
                return;
            }

            this.ParameterSetTabControl.SelectedItem = viewModel.ParameterSets[0];
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void CmdletControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F1)
            {
                this.CurrentCommandViewModel.OpenHelpWindow();
            }
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            this.CurrentCommandViewModel.OpenHelpWindow();
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void ImportModuleButton_Click(object sender, RoutedEventArgs e)
        {
            this.CurrentCommandViewModel.OnImportModule();
        }
        #endregion
    }
}
