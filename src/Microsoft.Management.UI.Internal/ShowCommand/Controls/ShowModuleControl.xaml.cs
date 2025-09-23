// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public partial class ShowModuleControl : UserControl
    {
        
        private Window owner;

        
        public ShowModuleControl()
        {
            InitializeComponent();

            // See comment in method summary to understand why this event is handled
            this.CommandList.PreviewMouseMove += this.CommandList_PreviewMouseMove;

            // See comment in method summary to understand why this event is handled
            this.CommandList.SelectionChanged += this.CommandList_SelectionChanged;
        }

        
        public Window Owner
        {
            get { return this.owner; }
            set { this.owner = value; }
        }

        #region Events Handlers
        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CommandList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (this.CommandList.IsMouseCaptured)
            {
                this.CommandList.ReleaseMouseCapture();
            }
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CommandList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.CommandList.SelectedItem == null)
            {
                return;
            }

            this.CommandList.ScrollIntoView(this.CommandList.SelectedItem);
        }
        #endregion Events Handlers
    }
}
