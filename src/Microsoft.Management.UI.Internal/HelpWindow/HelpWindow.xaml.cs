// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

using Microsoft.Management.UI.Internal;

namespace Microsoft.Management.UI
{
    
    public partial class HelpWindow : Window
    {
        
        public static double MinimumZoom
        {
            get
            {
                return 20;
            }
        }

        
        public static double MaximumZoom
        {
            get
            {
                return 300;
            }
        }

        
        public static double ZoomInterval
        {
            get
            {
                return 10;
            }
        }

        
        private readonly HelpViewModel viewModel;

        
        public HelpWindow(PSObject helpObject)
        {
            InitializeComponent();
            this.viewModel = new HelpViewModel(helpObject, this.DocumentParagraph);
            CommonHelper.SetStartingPositionAndSize(
                this,
                HelpWindowSettings.Default.HelpWindowTop,
                HelpWindowSettings.Default.HelpWindowLeft,
                HelpWindowSettings.Default.HelpWindowWidth,
                HelpWindowSettings.Default.HelpWindowHeight,
                double.Parse((string)HelpWindowSettings.Default.Properties["HelpWindowWidth"].DefaultValue, CultureInfo.InvariantCulture.NumberFormat),
                double.Parse((string)HelpWindowSettings.Default.Properties["HelpWindowHeight"].DefaultValue, CultureInfo.InvariantCulture.NumberFormat),
                HelpWindowSettings.Default.HelpWindowMaximized);

            this.ReadZoomUserSetting();

            this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
            this.DataContext = this.viewModel;

            this.Loaded += this.HelpDialog_Loaded;
            this.Closed += this.HelpDialog_Closed;
        }

        
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                return;
            }

            if (e.Delta > 0)
            {
                this.viewModel.ZoomIn();
                e.Handled = true;
            }
            else
            {
                this.viewModel.ZoomOut();
                e.Handled = true;
            }
        }

        
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                if (e.Key == Key.PageUp)
                {
                    this.Scroll.PageUp();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.PageDown)
                {
                    this.Scroll.PageDown();
                    e.Handled = true;
                    return;
                }
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.HandleZoomInAndZoomOut(e);
                if (e.Handled)
                {
                    return;
                }

                if (e.Key == Key.F)
                {
                    this.Find.Focus();
                    e.Handled = true;
                    return;
                }
            }

            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                this.HandleZoomInAndZoomOut(e);
            }
        }

        
        private void ReadZoomUserSetting()
        {
            if (HelpWindowSettings.Default.HelpZoom < HelpWindow.MinimumZoom || HelpWindowSettings.Default.HelpZoom > HelpWindow.MaximumZoom)
            {
                HelpWindowSettings.Default.HelpZoom = 100;
            }

            this.viewModel.Zoom = HelpWindowSettings.Default.HelpZoom;
        }

        
        private void HandleZoomInAndZoomOut(KeyEventArgs e)
        {
            if (e.Key == Key.OemPlus || e.Key == Key.Add)
            {
                this.viewModel.ZoomIn();
                e.Handled = true;
            }

            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                this.viewModel.ZoomOut();
                e.Handled = true;
            }
        }

        
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Zoom")
            {
                HelpWindowSettings.Default.HelpZoom = this.viewModel.Zoom;
            }
        }

        
        private void HelpDialog_Closed(object sender, System.EventArgs e)
        {
            HelpWindowSettings.Default.Save();
        }

        
        private void HelpDialog_StateChanged(object sender, System.EventArgs e)
        {
            HelpWindowSettings.Default.HelpWindowMaximized = this.WindowState == WindowState.Maximized;
        }

        
        private void HelpDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.StateChanged += this.HelpDialog_StateChanged;
            this.LocationChanged += this.HelpDialog_LocationChanged;
            this.SizeChanged += this.HelpDialog_SizeChanged;
        }

        
        private void HelpDialog_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            HelpWindowSettings.Default.HelpWindowWidth = this.Width;
            HelpWindowSettings.Default.HelpWindowHeight = this.Height;
        }

        
        private void HelpDialog_LocationChanged(object sender, System.EventArgs e)
        {
            HelpWindowSettings.Default.HelpWindowTop = this.Top;
            HelpWindowSettings.Default.HelpWindowLeft = this.Left;
        }

        
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog settings = new SettingsDialog();
            settings.Owner = this;

            settings.ShowDialog();

            if (settings.DialogResult == true)
            {
                this.viewModel.HelpBuilder.AddTextToParagraphBuilder();
                this.viewModel.Search();
            }
        }

        
        private void PreviousMatch_Click(object sender, RoutedEventArgs e)
        {
            this.MoveToNextMatch(false);
        }

        
        private void NextMatch_Click(object sender, RoutedEventArgs e)
        {
            this.MoveToNextMatch(true);
        }

        
        private void MoveToNextMatch(bool forward)
        {
            TextPointer caretPosition = this.HelpText.CaretPosition;
            Run nextRun = this.viewModel.Searcher.MoveAndHighlightNextNextMatch(forward, caretPosition);
            this.MoveToRun(nextRun);
        }

        
        private void MoveToRun(Run run)
        {
            if (run == null)
            {
                return;
            }

            run.BringIntoView();
            this.HelpText.CaretPosition = run.ElementEnd;
            this.HelpText.Focus();
        }
    }
}
