// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    
    internal class OutGridViewWindow
    {
        #region private Fields

        
        private const double ZOOM_INCREMENT = 0.2;

        
        private const double ZOOM_MAX = 3.0;

        
        private const double ZOOM_MIN = 0.5;

        
        private Window gridViewWindow;

        
        private ManagementList managementList;

        
        private ObservableCollection<PSObject> listItems;

        
        private AutoResetEvent gridViewWindowLoaded;

        
        private Exception exception = null;

        
        private AutoResetEvent closedEvent;

        
        private static readonly string OKButtonContent = XamlLocalizableResources.OutGridView_Button_OK;

        
        private static readonly string CancelButtonContent = XamlLocalizableResources.OutGridView_Button_Cancel;

        
        private List<PSObject> selectedItems;

        
        private Thread guiThread;

        
        private double zoomLevel = 1.0;

        #endregion private Fields

        #region internal Constructors

        
        internal OutGridViewWindow()
        {
            // Initialize the data source collection.
            this.listItems = new ObservableCollection<PSObject>();
        }

        #endregion internal Constructors

        #region private delegates
        
        /// <param name="arg">Start GridView Window delegate.</param>
        private delegate void ThreadDelegate(object arg);

        #endregion private delegates

        #region Private method that are intended to be called by the Out-GridView cmdlet.

        
        /// <param name="invocation">Commands of the PowerShell.</param>
        /// <param name="outputModeOptions">Selection mode of the list.</param>
        /// <param name="closedEvent">ClosedEvent.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "The method is called using reflection.")]
        private void StartWindow(string invocation, string outputModeOptions, AutoResetEvent closedEvent)
        {
            this.closedEvent = closedEvent;
            this.gridViewWindowLoaded = new AutoResetEvent(false);

            ParameterizedThreadStart threadStart = new ParameterizedThreadStart(
               new ThreadDelegate(delegate
               {
                   try
                   {
                       this.gridViewWindow = new Window();
                       this.managementList = CreateManagementList(outputModeOptions);
                       this.gridViewWindow.Loaded += this.GridViewWindowLoaded;
                       this.gridViewWindow.Content = CreateMainGrid(outputModeOptions);
                       this.gridViewWindow.Title = invocation;
                       this.gridViewWindow.Closed += this.GridViewWindowClosed;

                       RoutedCommand plusSettings = new RoutedCommand();
                       KeyGestureConverter keyGestureConverter = new KeyGestureConverter();

                       try
                       {
                           plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomIn1Shortcut));
                           plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomIn2Shortcut));
                           plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomIn3Shortcut));
                           plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomIn4Shortcut));
                           this.gridViewWindow.CommandBindings.Add(new CommandBinding(plusSettings, ZoomEventHandlerPlus));
                       }
                       catch (NotSupportedException)
                       {
                           // localized has a problematic string - going to default
                           plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString("Ctrl+Add"));
                           plusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString("Ctrl+Plus"));
                           this.gridViewWindow.CommandBindings.Add(new CommandBinding(plusSettings, ZoomEventHandlerPlus));
                       }

                       RoutedCommand minusSettings = new RoutedCommand();
                       try
                       {
                           minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomOut1Shortcut));
                           minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomOut2Shortcut));
                           minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomOut3Shortcut));
                           minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString(UICultureResources.ZoomOut4Shortcut));

                           this.gridViewWindow.CommandBindings.Add(new CommandBinding(minusSettings, ZoomEventHandlerMinus));
                       }
                       catch (NotSupportedException)
                       {
                           // localized has a problematic string - going to default
                           minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString("Ctrl+Subtract"));
                           minusSettings.InputGestures.Add((KeyGesture)keyGestureConverter.ConvertFromString("Ctrl+Minus"));
                           this.gridViewWindow.CommandBindings.Add(new CommandBinding(minusSettings, ZoomEventHandlerMinus));
                       }

                       this.gridViewWindow.ShowDialog();
                   }
                   catch (Exception e)
                   {
                       // Store the exception in a local variable that will be checked later.
                       if (e.InnerException != null)
                       {
                           this.exception = e.InnerException;
                       }
                       else
                       {
                           this.exception = e;
                       }
                   }
               }));

            guiThread = new Thread(threadStart);
            guiThread.SetApartmentState(ApartmentState.STA);

            guiThread.Start();
        }

        
        /// <param name="sender">.</param>
        /// <param name="e">.</param>
        private void ZoomEventHandlerPlus(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.zoomLevel == 0)
            {
                this.zoomLevel = 1;
            }

            if (this.zoomLevel < ZOOM_MAX)
            {
                this.zoomLevel += ZOOM_INCREMENT;
                Grid g = this.gridViewWindow.Content as Grid;

                if (g != null)
                {
                    g.LayoutTransform = new ScaleTransform(this.zoomLevel, this.zoomLevel, 0, 0);
                }
            }
        }

        
        /// <param name="sender">.</param>
        /// <param name="e">.</param>
        private void ZoomEventHandlerMinus(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.zoomLevel >= ZOOM_MIN)
            {
                this.zoomLevel -= ZOOM_INCREMENT;
                Grid g = this.gridViewWindow.Content as Grid;
                if (g != null)
                {
                    g.LayoutTransform = new ScaleTransform(this.zoomLevel, this.zoomLevel, 0, 0);
                }
            }
        }

        
        /// <param name="outputMode">Output mode of the out-gridview.</param>
        /// <returns>A new ManagementList.</returns>
        private ManagementList CreateManagementList(string outputMode)
        {
            ManagementList newList = new ManagementList();

            newList.ViewSaverUserActionState = UserActionState.Hidden;
            newList.ViewManagerUserActionState = UserActionState.Hidden;
            newList.List.VerticalAlignment = VerticalAlignment.Stretch;
            newList.List.SetValue(Grid.RowProperty, 0);
            newList.List.SelectionMode = (outputMode == "Single") ? SelectionMode.Single : SelectionMode.Extended;

            return newList;
        }

        
        /// <param name="outputMode">Output mode of the out-gridview.</param>
        /// <returns>A new mainGrid.</returns>
        private Grid CreateMainGrid(string outputMode)
        {
            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition());
            mainGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            mainGrid.Children.Add(managementList);

            if (outputMode == "None")
            {
                return mainGrid;
            }

            // OK and Cancel button should only be displayed if OutputMode is not None.
            mainGrid.RowDefinitions.Add(new RowDefinition());
            mainGrid.RowDefinitions[1].Height = GridLength.Auto;
            mainGrid.Children.Add(CreateButtonGrid());

            return mainGrid;
        }

        
        /// <returns>A new buttonGrid.</returns>
        private Grid CreateButtonGrid()
        {
            Grid buttonGrid = new Grid();

            //// This will allow OK and Cancel to have the same width
            buttonGrid.SetValue(Grid.IsSharedSizeScopeProperty, true);
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
            buttonGrid.ColumnDefinitions[0].Width = GridLength.Auto;
            buttonGrid.ColumnDefinitions[0].SharedSizeGroup = "okCancel";
            buttonGrid.ColumnDefinitions[1].Width = GridLength.Auto;
            buttonGrid.ColumnDefinitions[1].SharedSizeGroup = "okCancel";
            buttonGrid.HorizontalAlignment = HorizontalAlignment.Right;
            buttonGrid.SetValue(Grid.RowProperty, 1);

            //// This will add OK and Cancel button to buttonGrid.
            buttonGrid.Children.Add(CreateOKButton());
            buttonGrid.Children.Add(CreateCancelButton());

            return buttonGrid;
        }

        
        /// <returns>A new OK button.</returns>
        private Button CreateOKButton()
        {
            Button ok = new Button();
            ok.Content = OKButtonContent;
            ok.Margin = new Thickness(5);
            ok.Padding = new Thickness(2);
            ok.SetValue(Grid.ColumnProperty, 0);
            ok.IsDefault = true;
            ok.SetValue(AutomationProperties.AutomationIdProperty, "OGVOK");
            ok.Click += OK_Click;
            return ok;
        }

        
        /// <returns>A new Cancel button.</returns>
        private Button CreateCancelButton()
        {
            Button cancel = new Button();
            cancel.Content = CancelButtonContent;
            cancel.Margin = new Thickness(5);
            cancel.Padding = new Thickness(2);
            cancel.SetValue(Grid.ColumnProperty, 1);
            cancel.IsCancel = true;
            cancel.SetValue(AutomationProperties.AutomationIdProperty, "OGVCancel");
            cancel.Click += Cancel_Click;
            return cancel;
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (this.managementList.List.SelectedItems.Count != 0)
            {
                this.selectedItems = new List<PSObject>();
                foreach (PSObject obj in this.managementList.List.SelectedItems)
                {
                    this.selectedItems.Add(obj);
                }
            }

            this.gridViewWindow.Close();
        }

        
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.gridViewWindow.Close();
        }

        
        /// <returns>Selected items of the list.</returns>
        private List<PSObject> SelectedItems()
        {
            return this.selectedItems;
        }

        
        public void CloseWindow()
        {
            this.gridViewWindow.Dispatcher.Invoke(new ThreadStart(delegate { this.gridViewWindow.Close(); }));
        }

        
        /// <param name="propertyNames">An array of property names to add.</param>
        /// <param name="displayNames">An array of display names to add.</param>
        /// <param name="types">An array of types to add.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "The method is called using reflection.")]
        private void AddColumns(string[] propertyNames, string[] displayNames, Type[] types)
        {
            // Wait for the gridViewWindow thread to signal that loading of Window is done
            if (this.gridViewWindowLoaded != null)
            {
                this.gridViewWindowLoaded.WaitOne();
                this.gridViewWindowLoaded = null;
            }

            this.managementList.Dispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(
                    () =>
                    {
                        // Pick the length of the shortest incoming arrays. Normally all incoming arrays should be of the same length.
                        int length = propertyNames.Length;
                        if (length > displayNames.Length)
                        {
                            length = displayNames.Length;
                        }

                        if (length > types.Length)
                        {
                            length = types.Length;
                        }

                        try
                        {
                            // Clear all columns in case the view is changed.
                            this.managementList.List.Columns.Clear();

                            // Clear column filter rules.
                            this.managementList.AddFilterRulePicker.ColumnFilterRules.Clear();

                            // Add columns with provided names and Types.
                            for (int i = 0; i < propertyNames.Length; ++i)
                            {
                                DataTemplate dataTemplate;
                                bool haveTemplate = this.managementList.FilterRulePanel.TryGetContentTemplate(types[i], out dataTemplate);
                                InnerListColumn column = null;

                                if (haveTemplate)
                                {
                                    column = new InnerListColumn(new UIPropertyGroupDescription(propertyNames[i], displayNames[i], types[i]));
                                }
                                else
                                {
                                    column = new InnerListColumn(new UIPropertyGroupDescription(propertyNames[i], displayNames[i], typeof(string)));
                                }

                                this.managementList.AddColumn(column);
                            }

                            this.managementList.List.SetColumnHeaderActions();

                            if (this.managementList.List.ItemsSource == null)
                            {
                                // Setting ItemsSource implicitly regenerates all columns.
                                this.managementList.List.ItemsSource = this.listItems;
                            }

                            // Set focus on ListView
                            this.managementList.List.SelectedIndex = 0;
                            this.managementList.List.Focus();
                        }
                        catch (Exception e)
                        {
                            // Store the exception in a local variable that will be checked later.
                            if (e.InnerException != null)
                            {
                                this.exception = e.InnerException;
                            }
                            else
                            {
                                this.exception = e;
                            }
                        }
                    }));
        }

        
        /// <param name="value">PSObject of comlet data.</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "The method is called using reflection.")]
        private void AddItem(PSObject value)
        {
            if (this.GetWindowClosedStatus())
            {
                return;
            }

            this.managementList.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new Action(
                    () =>
                    {
                        try
                        {
                            // Remove any potential ANSI decoration
                            foreach (var property in value.Properties)
                            {
                                if (property.Value is string str)
                                {
                                    StringDecorated decoratedString = new StringDecorated(str);
                                    property.Value = decoratedString.ToString(OutputRendering.PlainText);
                                }
                            }

                            this.listItems.Add(value);
                        }
                        catch (Exception e)
                        {
                            // Store the exception in a local variable that will be checked later.
                            if (e.InnerException != null)
                            {
                                this.exception = e.InnerException;
                            }
                            else
                            {
                                this.exception = e;
                            }
                        }
                    }));
        }

        
        /// <returns>The status of GridView Window close or not.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "The method is called using reflection.")]
        private bool GetWindowClosedStatus()
        {
            if (this.closedEvent == null)
            {
                return false;
            }

            return this.closedEvent.WaitOne(0);
        }

        
        /// <returns>The thrown and caught exception. It returns null if no exceptions were thrown by any previous method calls.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "The method is called using reflection.")]
        private Exception GetLastException()
        {
            Exception local = this.exception;

            if (local != null)
            {
                // Clear the caught exception.
                this.exception = null;
                return local;
            }

            return this.exception;
        }

        #endregion Private method that are intended to be called by the Out-GridView cmdlet.

        #region Private methods

        
        /// <param name="sender">The sender object.</param>
        /// <param name="e">Event Args.</param>
        private void GridViewWindowClosed(object sender, EventArgs e)
        {
            if (this.closedEvent != null && !this.closedEvent.SafeWaitHandle.IsClosed)
            {
                try
                {
                    this.closedEvent.Set();
                }
                catch (ObjectDisposedException)
                {
                    // we tried to avoid this exception with "&& !this.closedEvent.SafeWaitHandle.IsClosed"
                    // but since this runs in a different thread the if condition could be evaluated and after that
                    // the handle disposed
                }
            }
        }

        
        /// <param name="sender">The sender object.</param>
        /// <param name="e">RoutedEvent Args.</param>
        private void GridViewWindowLoaded(object sender, RoutedEventArgs e)
        {
            // signal the main thread
            this.gridViewWindowLoaded.Set();

            // Make gridview window as active window
            this.gridViewWindow.Activate();

            // Set up AutomationId and Name
            AutomationProperties.SetName(this.gridViewWindow, GraphicalHostResources.OutGridViewWindowObjectName);
            AutomationProperties.SetAutomationId(this.gridViewWindow, "OutGridViewWindow");
        }

        #endregion Private methods
    }
}
