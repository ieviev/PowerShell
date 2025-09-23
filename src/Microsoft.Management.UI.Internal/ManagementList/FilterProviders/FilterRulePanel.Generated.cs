// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{

    [Localizability(LocalizationCategory.None)]
    partial class FilterRulePanel
    {
        //
        // AddRules routed command
        //
        
        public static readonly RoutedCommand AddRulesCommand = new RoutedCommand("AddRules",typeof(FilterRulePanel));

        static private void AddRulesCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            FilterRulePanel obj = (FilterRulePanel) sender;
            obj.OnAddRulesExecuted( e );
        }

        
        protected virtual void OnAddRulesExecuted(ExecutedRoutedEventArgs e)
        {
            OnAddRulesExecutedImplementation(e);
        }

        partial void OnAddRulesExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // RemoveRule routed command
        //
        
        public static readonly RoutedCommand RemoveRuleCommand = new RoutedCommand("RemoveRule",typeof(FilterRulePanel));

        static private void RemoveRuleCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            FilterRulePanel obj = (FilterRulePanel) sender;
            obj.OnRemoveRuleExecuted( e );
        }

        
        protected virtual void OnRemoveRuleExecuted(ExecutedRoutedEventArgs e)
        {
            OnRemoveRuleExecutedImplementation(e);
        }

        partial void OnRemoveRuleExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // Static constructor
        //

        
        static FilterRulePanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FilterRulePanel), new FrameworkPropertyMetadata(typeof(FilterRulePanel)));
            CommandManager.RegisterClassCommandBinding( typeof(FilterRulePanel), new CommandBinding( FilterRulePanel.AddRulesCommand, AddRulesCommand_CommandExecuted ));
            CommandManager.RegisterClassCommandBinding( typeof(FilterRulePanel), new CommandBinding( FilterRulePanel.RemoveRuleCommand, RemoveRuleCommand_CommandExecuted ));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

        //
        // CreateAutomationPeer
        //
        
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new ExtendedFrameworkElementAutomationPeer(this,AutomationControlType.Group,true);
        }

    }
}
#endregion
