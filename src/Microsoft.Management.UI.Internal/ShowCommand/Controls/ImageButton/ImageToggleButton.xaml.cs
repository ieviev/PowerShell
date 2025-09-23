// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Required by XAML")]
    public partial class ImageToggleButton : ImageButtonBase
    {
        
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(ImageToggleButton));

        
        public ImageToggleButton()
        {
            InitializeComponent();
            this.Loaded += this.ImageButton_Loaded;
        }

        
        public bool IsChecked
        {
            get { return (bool)GetValue(ImageToggleButton.IsCheckedProperty); }
            set { SetValue(ImageToggleButton.IsCheckedProperty, value); }
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ImageButton_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            object thisAutomationId = this.GetValue(AutomationProperties.AutomationIdProperty);
            if (thisAutomationId != null)
            {
                this.toggleInnerButton.SetValue(AutomationProperties.AutomationIdProperty, thisAutomationId);
            }

            object thisAutomationName = this.GetValue(AutomationProperties.NameProperty);
            if (thisAutomationName != null)
            {
                this.toggleInnerButton.SetValue(AutomationProperties.NameProperty, thisAutomationName);
            }
        }
    }
}
