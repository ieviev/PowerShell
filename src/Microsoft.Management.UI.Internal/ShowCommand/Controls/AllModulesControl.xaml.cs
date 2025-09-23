// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public partial class AllModulesControl : UserControl
    {
        #region Construction and Destructor

        
        public AllModulesControl()
        {
            InitializeComponent();

            this.Loaded += (obj, args) =>
            {
                this.ModulesCombo.Focus();
            };
        }

        #endregion
        
        internal ShowModuleControl CurrentShowModuleControl
        {
            get { return this.ShowModuleControl; }
        }

        private void RefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AllModulesViewModel viewModel = this.DataContext as AllModulesViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.OnRefresh();
        }
    }
}
