// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;

using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public partial class ParameterSetControl : UserControl
    {
        
        private UIElement firstFocusableElement;

        
        private ParameterSetViewModel currentParameterSetViewModel;

        #region Construction and Destructor
        
        public ParameterSetControl()
        {
            InitializeComponent();
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(this.ParameterSetControl_DataContextChanged);
        }
        #endregion

        #region Public Methods

        
        public void FocusFirstElement()
        {
            if (this.firstFocusableElement != null)
            {
                this.firstFocusableElement.Focus();
            }
        }

        #endregion

        #region Private Property
        
        private ParameterSetViewModel CurrentParameterSetViewModel
        {
            get { return this.currentParameterSetViewModel; }
        }

        #endregion

        
        private static CheckBox CreateCheckBox(ParameterViewModel parameterViewModel, int rowNumber)
        {
            CheckBox checkBox = new CheckBox();

            checkBox.SetBinding(Label.ContentProperty, new Binding("NameCheckLabel"));
            checkBox.DataContext = parameterViewModel;
            checkBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            checkBox.SetValue(Grid.ColumnProperty, 0);
            checkBox.SetValue(Grid.ColumnSpanProperty, 2);
            checkBox.SetValue(Grid.RowProperty, rowNumber);
            checkBox.IsThreeState = false;
            checkBox.Margin = new Thickness(8, rowNumber == 0 ? 7 : 5, 0, 5);
            checkBox.SetBinding(CheckBox.ToolTipProperty, new Binding("ToolTip"));
            checkBox.SetBinding(AutomationProperties.HelpTextProperty, new Binding("ToolTip"));
            Binding valueBinding = new Binding("Value");
            checkBox.SetBinding(CheckBox.IsCheckedProperty, valueBinding);

            checkBox.SetValue(
                System.Windows.Automation.AutomationProperties.AutomationIdProperty,
                string.Create(CultureInfo.CurrentCulture, $"chk{parameterViewModel.Name}"));

            checkBox.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                parameterViewModel.Name);

            return checkBox;
        }

        
        private static ComboBox CreateComboBoxControl(ParameterViewModel parameterViewModel, int rowNumber, IEnumerable itemsSource)
        {
            ComboBox comboBox = new ComboBox();

            comboBox.DataContext = parameterViewModel;
            comboBox.SetValue(Grid.ColumnProperty, 1);
            comboBox.SetValue(Grid.RowProperty, rowNumber);
            comboBox.Margin = new Thickness(2);
            comboBox.SetBinding(TextBox.ToolTipProperty, new Binding("ToolTip"));
            comboBox.ItemsSource = itemsSource;

            Binding selectedItemBinding = new Binding("Value");
            comboBox.SetBinding(ComboBox.SelectedItemProperty, selectedItemBinding);

            string automationId = string.Create(CultureInfo.CurrentCulture, $"combox{parameterViewModel.Name}");

            comboBox.SetValue(
                System.Windows.Automation.AutomationProperties.AutomationIdProperty,
                automationId);

            comboBox.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                parameterViewModel.Name);

            return comboBox;
        }

        
        private static MultipleSelectionControl CreateMultiSelectComboControl(ParameterViewModel parameterViewModel, int rowNumber, IEnumerable itemsSource)
        {
            MultipleSelectionControl multiControls = new MultipleSelectionControl();

            multiControls.DataContext = parameterViewModel;
            multiControls.SetValue(Grid.ColumnProperty, 1);
            multiControls.SetValue(Grid.RowProperty, rowNumber);
            multiControls.Margin = new Thickness(2);
            multiControls.comboxParameter.ItemsSource = itemsSource;
            multiControls.SetBinding(TextBox.ToolTipProperty, new Binding("ToolTip"));

            Binding valueBinding = new Binding("Value");
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            multiControls.comboxParameter.SetBinding(ComboBox.TextProperty, valueBinding);

            // Add AutomationProperties.AutomationId for Ui Automation test.
            multiControls.SetValue(System.Windows.Automation.AutomationProperties.AutomationIdProperty, string.Create(CultureInfo.CurrentCulture, $"combox{parameterViewModel.Name}"));

            multiControls.comboxParameter.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                parameterViewModel.Name);

            string buttonToolTipAndName = string.Format(
                CultureInfo.CurrentUICulture,
                ShowCommandResources.SelectMultipleValuesForParameterFormat,
                parameterViewModel.Name);

            multiControls.multipleValueButton.SetValue(Button.ToolTipProperty, buttonToolTipAndName);
            multiControls.multipleValueButton.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                buttonToolTipAndName);

            return multiControls;
        }

        
        private static TextBox CreateTextBoxControl(ParameterViewModel parameterViewModel, int rowNumber)
        {
            TextBox textBox = new TextBox();

            textBox.DataContext = parameterViewModel;
            textBox.SetValue(Grid.ColumnProperty, 1);
            textBox.SetValue(Grid.RowProperty, rowNumber);
            textBox.Margin = new Thickness(2);
            textBox.SetBinding(TextBox.ToolTipProperty, new Binding("ToolTip"));

            Binding valueBinding = new Binding("Value");
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            textBox.SetBinding(TextBox.TextProperty, valueBinding);

            textBox.SetValue(
                System.Windows.Automation.AutomationProperties.AutomationIdProperty,
                string.Create(CultureInfo.CurrentCulture, $"txt{parameterViewModel.Name}"));

            textBox.SetValue(
                System.Windows.Automation.AutomationProperties.NameProperty,
                parameterViewModel.Name);

            ShowCommandParameterType parameterType = parameterViewModel.Parameter.ParameterType;

            if (parameterType.IsArray)
            {
                parameterType = parameterType.ElementType;
            }

            if (parameterType.IsScriptBlock || parameterType.ImplementsDictionary)
            {
                textBox.AcceptsReturn = true;
                textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                textBox.Loaded += ParameterSetControl.MultiLineTextBox_Loaded;
            }

            return textBox;
        }

        
        private static void MultiLineTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox senderTextBox = (TextBox)sender;
            senderTextBox.Loaded -= ParameterSetControl.MultiLineTextBox_Loaded;

            // This will set the height to about 3 lines since the total height of the
            // TextBox is a bit greater than a line's height
            senderTextBox.Height = senderTextBox.ActualHeight * 2;
        }

        #region Event Methods

        
        private void ParameterSetControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.firstFocusableElement = null;
            this.MainGrid.Children.Clear();
            this.MainGrid.RowDefinitions.Clear();

            ParameterSetViewModel viewModel = e.NewValue as ParameterSetViewModel;
            if (viewModel == null)
            {
                return;
            }

            this.currentParameterSetViewModel = viewModel;

            for (int rowNumber = 0; rowNumber < viewModel.Parameters.Count; rowNumber++)
            {
                ParameterViewModel parameter = viewModel.Parameters[rowNumber];
                this.MainGrid.RowDefinitions.Add(this.CreateNewRow());

                if (parameter.Parameter.ParameterType.IsSwitch)
                {
                    this.AddControlToMainGrid(ParameterSetControl.CreateCheckBox(parameter, rowNumber));
                }
                else
                {
                    this.CreateAndAddLabel(parameter, rowNumber);
                    Control control = null;
                    if (parameter.Parameter.HasParameterSet)
                    {
                        // For ValidateSet parameter
                        ArrayList itemsSource = new ArrayList();
                        itemsSource.Add(string.Empty);

                        for (int i = 0; i < parameter.Parameter.ValidParamSetValues.Count; i++)
                        {
                            itemsSource.Add(parameter.Parameter.ValidParamSetValues[i]);
                        }

                        control = ParameterSetControl.CreateComboBoxControl(parameter, rowNumber, itemsSource);
                    }
                    else if (parameter.Parameter.ParameterType.IsEnum)
                    {
                        if (parameter.Parameter.ParameterType.HasFlagAttribute)
                        {
                            ArrayList itemsSource = new ArrayList();
                            itemsSource.Add(string.Empty);
                            itemsSource.AddRange(parameter.Parameter.ParameterType.EnumValues);
                            control = ParameterSetControl.CreateComboBoxControl(parameter, rowNumber, itemsSource);
                        }
                        else
                        {
                            control = ParameterSetControl.CreateMultiSelectComboControl(parameter, rowNumber, parameter.Parameter.ParameterType.EnumValues);
                        }
                    }
                    else if (parameter.Parameter.ParameterType.IsBoolean)
                    {
                        control = ParameterSetControl.CreateComboBoxControl(parameter, rowNumber, new string[] { string.Empty, "$True", "$False" });
                    }
                    else
                    {
                        // For input parameter
                        control = ParameterSetControl.CreateTextBoxControl(parameter, rowNumber);
                    }

                    if (control != null)
                    {
                        this.AddControlToMainGrid(control);
                    }
                }
            }
        }

        
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox senderCheck = (CheckBox)sender;
            ((ParameterViewModel)senderCheck.DataContext).Value = senderCheck.IsChecked.ToString();
        }

        #endregion

        #region Private Method

        
        private RowDefinition CreateNewRow()
        {
            RowDefinition row = new RowDefinition();
            row.Height = GridLength.Auto;
            return row;
        }

        
        private void AddControlToMainGrid(UIElement uiControl)
        {
            if (this.firstFocusableElement == null && uiControl is not Label)
            {
                this.firstFocusableElement = uiControl;
            }

            this.MainGrid.Children.Add(uiControl);
        }

        
        private void CreateAndAddLabel(ParameterViewModel parameterViewModel, int rowNumber)
        {
            Label label = this.CreateLabel(parameterViewModel, rowNumber);
            this.AddControlToMainGrid(label);
        }

        
        private Label CreateLabel(ParameterViewModel parameterViewModel, int rowNumber)
        {
            Label label = new Label();

            label.SetBinding(Label.ContentProperty, new Binding("NameTextLabel"));
            label.DataContext = parameterViewModel;
            label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            label.SetValue(Grid.ColumnProperty, 0);
            label.SetValue(Grid.RowProperty, rowNumber);
            label.Margin = new Thickness(2);
            label.SetBinding(Label.ToolTipProperty, new Binding("ToolTip"));

            label.SetValue(
                System.Windows.Automation.AutomationProperties.AutomationIdProperty,
                string.Create(CultureInfo.CurrentCulture, $"lbl{parameterViewModel.Name}"));

            return label;
        }
        #endregion
    }
}
