// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public partial class MultipleSelectionControl : UserControl
    {
        
        public MultipleSelectionControl()
        {
            InitializeComponent();
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            MultipleSelectionDialog multipleSelectionDialog = new MultipleSelectionDialog();
            multipleSelectionDialog.Title = this.multipleValueButton.ToolTip.ToString();
            multipleSelectionDialog.listboxParameter.ItemsSource = comboxParameter.ItemsSource;
            multipleSelectionDialog.ShowDialog();

            if (multipleSelectionDialog.DialogResult != true)
            {
                return;
            }

            StringBuilder newComboText = new StringBuilder();

            foreach (object selectedItem in multipleSelectionDialog.listboxParameter.SelectedItems)
            {
                newComboText.Append(CultureInfo.InvariantCulture, $"{selectedItem},");
            }

            if (newComboText.Length > 1)
            {
                newComboText.Remove(newComboText.Length - 1, 1);
            }

            comboxParameter.Text = newComboText.ToString();
        }
    }
}
