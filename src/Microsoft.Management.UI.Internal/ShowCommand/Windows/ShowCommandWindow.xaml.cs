// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;

using Microsoft.Management.UI.Internal.ShowCommand;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public partial class ShowCommandWindow : Window
    {
        #region Construction and Destructor

        
        public ShowCommandWindow()
        {
            this.InitializeComponent();
            this.SizeChanged += this.ShowCommandWindow_SizeChanged;
            this.LocationChanged += this.ShowCommandWindow_LocationChanged;
            this.StateChanged += this.ShowCommandWindow_StateChanged;
        }

        
        protected override void OnClosed(System.EventArgs e)
        {
            ShowCommandSettings.Default.Save();
            base.OnClosed(e);
        }

        
        private void ShowCommandWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ShowCommandSettings.Default.ShowOneCommandWidth = this.Width;
            ShowCommandSettings.Default.ShowOneCommandHeight = this.Height;
        }

        
        private void ShowCommandWindow_LocationChanged(object sender, System.EventArgs e)
        {
            ShowCommandSettings.Default.ShowOneCommandTop = this.Top;
            ShowCommandSettings.Default.ShowOneCommandLeft = this.Left;
        }

        
        private void ShowCommandWindow_StateChanged(object sender, System.EventArgs e)
        {
            ShowCommandSettings.Default.ShowOneCommandWindowMaximized = this.WindowState == WindowState.Maximized;
        }
        #endregion
    }
}
