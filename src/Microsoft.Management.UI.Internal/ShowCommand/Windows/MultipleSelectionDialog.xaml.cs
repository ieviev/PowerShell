// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public partial class MultipleSelectionDialog : Window
    {
        
        public MultipleSelectionDialog()
        {
            this.InitializeComponent();
        }

        
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
