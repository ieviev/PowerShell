// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{

    

    [TemplatePart(Name="PART_ViewManager", Type=typeof(ListOrganizer))]
    [TemplatePart(Name="PART_ViewSaver", Type=typeof(PickerBase))]
    [Localizability(LocalizationCategory.None)]
    partial class ManagementList
    {
        //
        // Fields
        //
        private ListOrganizer viewManager;
        private PickerBase viewSaver;

        //
        // ViewsChanged RoutedEvent
        //
        
        public static readonly RoutedEvent ViewsChangedEvent = EventManager.RegisterRoutedEvent("ViewsChanged",RoutingStrategy.Bubble,typeof(RoutedEventHandler),typeof(ManagementList));

        
        public event RoutedEventHandler ViewsChanged
        {
            add
            {
                AddHandler(ViewsChangedEvent,value);
            }
            remove
            {
                RemoveHandler(ViewsChangedEvent,value);
            }
        }

        //
        // ClearFilter routed command
        //
        
        public static readonly RoutedCommand ClearFilterCommand = new RoutedCommand("ClearFilter",typeof(ManagementList));

        static private void ClearFilterCommand_CommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ManagementList obj = (ManagementList) sender;
            obj.OnClearFilterCanExecute( e );
        }

        static private void ClearFilterCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ManagementList obj = (ManagementList) sender;
            obj.OnClearFilterExecuted( e );
        }

        
        protected virtual void OnClearFilterCanExecute(CanExecuteRoutedEventArgs e)
        {
            OnClearFilterCanExecuteImplementation(e);
        }

        partial void OnClearFilterCanExecuteImplementation(CanExecuteRoutedEventArgs e);

        
        protected virtual void OnClearFilterExecuted(ExecutedRoutedEventArgs e)
        {
            OnClearFilterExecutedImplementation(e);
        }

        partial void OnClearFilterExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // SaveView routed command
        //
        
        public static readonly RoutedCommand SaveViewCommand = new RoutedCommand("SaveView",typeof(ManagementList));

        static private void SaveViewCommand_CommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ManagementList obj = (ManagementList) sender;
            obj.OnSaveViewCanExecute( e );
        }

        static private void SaveViewCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ManagementList obj = (ManagementList) sender;
            obj.OnSaveViewExecuted( e );
        }

        
        protected virtual void OnSaveViewCanExecute(CanExecuteRoutedEventArgs e)
        {
            OnSaveViewCanExecuteImplementation(e);
        }

        partial void OnSaveViewCanExecuteImplementation(CanExecuteRoutedEventArgs e);

        
        protected virtual void OnSaveViewExecuted(ExecutedRoutedEventArgs e)
        {
            OnSaveViewExecutedImplementation(e);
        }

        partial void OnSaveViewExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // StartFilter routed command
        //
        
        public static readonly RoutedCommand StartFilterCommand = new RoutedCommand("StartFilter",typeof(ManagementList));

        static private void StartFilterCommand_CommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ManagementList obj = (ManagementList) sender;
            obj.OnStartFilterCanExecute( e );
        }

        static private void StartFilterCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ManagementList obj = (ManagementList) sender;
            obj.OnStartFilterExecuted( e );
        }

        
        protected virtual void OnStartFilterCanExecute(CanExecuteRoutedEventArgs e)
        {
            OnStartFilterCanExecuteImplementation(e);
        }

        partial void OnStartFilterCanExecuteImplementation(CanExecuteRoutedEventArgs e);

        
        protected virtual void OnStartFilterExecuted(ExecutedRoutedEventArgs e)
        {
            OnStartFilterExecutedImplementation(e);
        }

        partial void OnStartFilterExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // StopFilter routed command
        //
        
        public static readonly RoutedCommand StopFilterCommand = new RoutedCommand("StopFilter",typeof(ManagementList));

        static private void StopFilterCommand_CommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ManagementList obj = (ManagementList) sender;
            obj.OnStopFilterCanExecute( e );
        }

        static private void StopFilterCommand_CommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ManagementList obj = (ManagementList) sender;
            obj.OnStopFilterExecuted( e );
        }

        
        protected virtual void OnStopFilterCanExecute(CanExecuteRoutedEventArgs e)
        {
            OnStopFilterCanExecuteImplementation(e);
        }

        partial void OnStopFilterCanExecuteImplementation(CanExecuteRoutedEventArgs e);

        
        protected virtual void OnStopFilterExecuted(ExecutedRoutedEventArgs e)
        {
            OnStopFilterExecutedImplementation(e);
        }

        partial void OnStopFilterExecutedImplementation(ExecutedRoutedEventArgs e);

        //
        // AddFilterRulePicker dependency property
        //
        
        private static readonly DependencyPropertyKey AddFilterRulePickerPropertyKey = DependencyProperty.RegisterReadOnly( "AddFilterRulePicker", typeof(AddFilterRulePicker), typeof(ManagementList), new PropertyMetadata( null, AddFilterRulePickerProperty_PropertyChanged) );
        
        public static readonly DependencyProperty AddFilterRulePickerProperty = AddFilterRulePickerPropertyKey.DependencyProperty;

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets the filter rule picker.")]
        [Localizability(LocalizationCategory.None)]
        public AddFilterRulePicker AddFilterRulePicker
        {
            get
            {
                return (AddFilterRulePicker) GetValue(AddFilterRulePickerProperty);
            }
            private set
            {
                SetValue(AddFilterRulePickerPropertyKey,value);
            }
        }

        static private void AddFilterRulePickerProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnAddFilterRulePickerChanged( new PropertyChangedEventArgs<AddFilterRulePicker>((AddFilterRulePicker)e.OldValue, (AddFilterRulePicker)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<AddFilterRulePicker>> AddFilterRulePickerChanged;

        
        protected virtual void OnAddFilterRulePickerChanged(PropertyChangedEventArgs<AddFilterRulePicker> e)
        {
            OnAddFilterRulePickerChangedImplementation(e);
            RaisePropertyChangedEvent(AddFilterRulePickerChanged, e);
        }

        partial void OnAddFilterRulePickerChangedImplementation(PropertyChangedEventArgs<AddFilterRulePicker> e);

        //
        // CurrentView dependency property
        //
        
        private static readonly DependencyPropertyKey CurrentViewPropertyKey = DependencyProperty.RegisterReadOnly( "CurrentView", typeof(StateDescriptor<ManagementList>), typeof(ManagementList), new PropertyMetadata( null, CurrentViewProperty_PropertyChanged) );
        
        public static readonly DependencyProperty CurrentViewProperty = CurrentViewPropertyKey.DependencyProperty;

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets current view.")]
        [Localizability(LocalizationCategory.None)]
        public StateDescriptor<ManagementList> CurrentView
        {
            get
            {
                return (StateDescriptor<ManagementList>) GetValue(CurrentViewProperty);
            }
            private set
            {
                SetValue(CurrentViewPropertyKey,value);
            }
        }

        static private void CurrentViewProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnCurrentViewChanged( new PropertyChangedEventArgs<StateDescriptor<ManagementList>>((StateDescriptor<ManagementList>)e.OldValue, (StateDescriptor<ManagementList>)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<StateDescriptor<ManagementList>>> CurrentViewChanged;

        
        protected virtual void OnCurrentViewChanged(PropertyChangedEventArgs<StateDescriptor<ManagementList>> e)
        {
            OnCurrentViewChangedImplementation(e);
            RaisePropertyChangedEvent(CurrentViewChanged, e);
        }

        partial void OnCurrentViewChangedImplementation(PropertyChangedEventArgs<StateDescriptor<ManagementList>> e);

        //
        // Evaluator dependency property
        //
        
        public static readonly DependencyProperty EvaluatorProperty = DependencyProperty.Register( "Evaluator", typeof(ItemsControlFilterEvaluator), typeof(ManagementList), new PropertyMetadata( null, EvaluatorProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the FilterEvaluator.")]
        [Localizability(LocalizationCategory.None)]
        public ItemsControlFilterEvaluator Evaluator
        {
            get
            {
                return (ItemsControlFilterEvaluator) GetValue(EvaluatorProperty);
            }
            set
            {
                SetValue(EvaluatorProperty,value);
            }
        }

        static private void EvaluatorProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnEvaluatorChanged( new PropertyChangedEventArgs<ItemsControlFilterEvaluator>((ItemsControlFilterEvaluator)e.OldValue, (ItemsControlFilterEvaluator)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<ItemsControlFilterEvaluator>> EvaluatorChanged;

        
        protected virtual void OnEvaluatorChanged(PropertyChangedEventArgs<ItemsControlFilterEvaluator> e)
        {
            OnEvaluatorChangedImplementation(e);
            RaisePropertyChangedEvent(EvaluatorChanged, e);
        }

        partial void OnEvaluatorChangedImplementation(PropertyChangedEventArgs<ItemsControlFilterEvaluator> e);

        //
        // FilterRulePanel dependency property
        //
        
        private static readonly DependencyPropertyKey FilterRulePanelPropertyKey = DependencyProperty.RegisterReadOnly( "FilterRulePanel", typeof(FilterRulePanel), typeof(ManagementList), new PropertyMetadata( null, FilterRulePanelProperty_PropertyChanged) );
        
        public static readonly DependencyProperty FilterRulePanelProperty = FilterRulePanelPropertyKey.DependencyProperty;

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets the filter rule panel.")]
        [Localizability(LocalizationCategory.None)]
        public FilterRulePanel FilterRulePanel
        {
            get
            {
                return (FilterRulePanel) GetValue(FilterRulePanelProperty);
            }
            private set
            {
                SetValue(FilterRulePanelPropertyKey,value);
            }
        }

        static private void FilterRulePanelProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnFilterRulePanelChanged( new PropertyChangedEventArgs<FilterRulePanel>((FilterRulePanel)e.OldValue, (FilterRulePanel)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<FilterRulePanel>> FilterRulePanelChanged;

        
        protected virtual void OnFilterRulePanelChanged(PropertyChangedEventArgs<FilterRulePanel> e)
        {
            OnFilterRulePanelChangedImplementation(e);
            RaisePropertyChangedEvent(FilterRulePanelChanged, e);
        }

        partial void OnFilterRulePanelChangedImplementation(PropertyChangedEventArgs<FilterRulePanel> e);

        //
        // IsFilterShown dependency property
        //
        
        public static readonly DependencyProperty IsFilterShownProperty = DependencyProperty.Register( "IsFilterShown", typeof(bool), typeof(ManagementList), new PropertyMetadata( BooleanBoxes.TrueBox, IsFilterShownProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether the filter is shown.")]
        [Localizability(LocalizationCategory.None)]
        public bool IsFilterShown
        {
            get
            {
                return (bool) GetValue(IsFilterShownProperty);
            }
            set
            {
                SetValue(IsFilterShownProperty,BooleanBoxes.Box(value));
            }
        }

        static private void IsFilterShownProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnIsFilterShownChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<bool>> IsFilterShownChanged;

        
        protected virtual void OnIsFilterShownChanged(PropertyChangedEventArgs<bool> e)
        {
            OnIsFilterShownChangedImplementation(e);
            RaisePropertyChangedEvent(IsFilterShownChanged, e);
        }

        partial void OnIsFilterShownChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // IsLoadingItems dependency property
        //
        
        public static readonly DependencyProperty IsLoadingItemsProperty = DependencyProperty.Register( "IsLoadingItems", typeof(bool), typeof(ManagementList), new PropertyMetadata( BooleanBoxes.FalseBox, IsLoadingItemsProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether items are loading.")]
        [Localizability(LocalizationCategory.None)]
        public bool IsLoadingItems
        {
            get
            {
                return (bool) GetValue(IsLoadingItemsProperty);
            }
            set
            {
                SetValue(IsLoadingItemsProperty,BooleanBoxes.Box(value));
            }
        }

        static private void IsLoadingItemsProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnIsLoadingItemsChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<bool>> IsLoadingItemsChanged;

        
        protected virtual void OnIsLoadingItemsChanged(PropertyChangedEventArgs<bool> e)
        {
            OnIsLoadingItemsChangedImplementation(e);
            RaisePropertyChangedEvent(IsLoadingItemsChanged, e);
        }

        partial void OnIsLoadingItemsChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // IsSearchShown dependency property
        //
        
        public static readonly DependencyProperty IsSearchShownProperty = DependencyProperty.Register( "IsSearchShown", typeof(bool), typeof(ManagementList), new PropertyMetadata( BooleanBoxes.TrueBox, IsSearchShownProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether the search box is shown.")]
        [Localizability(LocalizationCategory.None)]
        public bool IsSearchShown
        {
            get
            {
                return (bool) GetValue(IsSearchShownProperty);
            }
            set
            {
                SetValue(IsSearchShownProperty,BooleanBoxes.Box(value));
            }
        }

        static private void IsSearchShownProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnIsSearchShownChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<bool>> IsSearchShownChanged;

        
        protected virtual void OnIsSearchShownChanged(PropertyChangedEventArgs<bool> e)
        {
            OnIsSearchShownChangedImplementation(e);
            RaisePropertyChangedEvent(IsSearchShownChanged, e);
        }

        partial void OnIsSearchShownChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // List dependency property
        //
        
        private static readonly DependencyPropertyKey ListPropertyKey = DependencyProperty.RegisterReadOnly( "List", typeof(InnerList), typeof(ManagementList), new PropertyMetadata( null, ListProperty_PropertyChanged) );
        
        public static readonly DependencyProperty ListProperty = ListPropertyKey.DependencyProperty;

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets the list.")]
        [Localizability(LocalizationCategory.None)]
        public InnerList List
        {
            get
            {
                return (InnerList) GetValue(ListProperty);
            }
            private set
            {
                SetValue(ListPropertyKey,value);
            }
        }

        static private void ListProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnListChanged( new PropertyChangedEventArgs<InnerList>((InnerList)e.OldValue, (InnerList)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<InnerList>> ListChanged;

        
        protected virtual void OnListChanged(PropertyChangedEventArgs<InnerList> e)
        {
            OnListChangedImplementation(e);
            RaisePropertyChangedEvent(ListChanged, e);
        }

        partial void OnListChangedImplementation(PropertyChangedEventArgs<InnerList> e);

        //
        // SearchBox dependency property
        //
        
        private static readonly DependencyPropertyKey SearchBoxPropertyKey = DependencyProperty.RegisterReadOnly( "SearchBox", typeof(SearchBox), typeof(ManagementList), new PropertyMetadata( null, SearchBoxProperty_PropertyChanged) );
        
        public static readonly DependencyProperty SearchBoxProperty = SearchBoxPropertyKey.DependencyProperty;

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets the search box.")]
        [Localizability(LocalizationCategory.None)]
        public SearchBox SearchBox
        {
            get
            {
                return (SearchBox) GetValue(SearchBoxProperty);
            }
            private set
            {
                SetValue(SearchBoxPropertyKey,value);
            }
        }

        static private void SearchBoxProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnSearchBoxChanged( new PropertyChangedEventArgs<SearchBox>((SearchBox)e.OldValue, (SearchBox)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<SearchBox>> SearchBoxChanged;

        
        protected virtual void OnSearchBoxChanged(PropertyChangedEventArgs<SearchBox> e)
        {
            OnSearchBoxChangedImplementation(e);
            RaisePropertyChangedEvent(SearchBoxChanged, e);
        }

        partial void OnSearchBoxChangedImplementation(PropertyChangedEventArgs<SearchBox> e);

        //
        // ViewManagerUserActionState dependency property
        //
        
        public static readonly DependencyProperty ViewManagerUserActionStateProperty = DependencyProperty.Register( "ViewManagerUserActionState", typeof(UserActionState), typeof(ManagementList), new PropertyMetadata( UserActionState.Enabled, ViewManagerUserActionStateProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the user interaction state of the view manager.")]
        [Localizability(LocalizationCategory.None)]
        public UserActionState ViewManagerUserActionState
        {
            get
            {
                return (UserActionState) GetValue(ViewManagerUserActionStateProperty);
            }
            set
            {
                SetValue(ViewManagerUserActionStateProperty,value);
            }
        }

        static private void ViewManagerUserActionStateProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnViewManagerUserActionStateChanged( new PropertyChangedEventArgs<UserActionState>((UserActionState)e.OldValue, (UserActionState)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<UserActionState>> ViewManagerUserActionStateChanged;

        
        protected virtual void OnViewManagerUserActionStateChanged(PropertyChangedEventArgs<UserActionState> e)
        {
            OnViewManagerUserActionStateChangedImplementation(e);
            RaisePropertyChangedEvent(ViewManagerUserActionStateChanged, e);
        }

        partial void OnViewManagerUserActionStateChangedImplementation(PropertyChangedEventArgs<UserActionState> e);

        //
        // ViewSaverUserActionState dependency property
        //
        
        public static readonly DependencyProperty ViewSaverUserActionStateProperty = DependencyProperty.Register( "ViewSaverUserActionState", typeof(UserActionState), typeof(ManagementList), new PropertyMetadata( UserActionState.Enabled, ViewSaverUserActionStateProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the user interaction state of the view saver.")]
        [Localizability(LocalizationCategory.None)]
        public UserActionState ViewSaverUserActionState
        {
            get
            {
                return (UserActionState) GetValue(ViewSaverUserActionStateProperty);
            }
            set
            {
                SetValue(ViewSaverUserActionStateProperty,value);
            }
        }

        static private void ViewSaverUserActionStateProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementList obj = (ManagementList) o;
            obj.OnViewSaverUserActionStateChanged( new PropertyChangedEventArgs<UserActionState>((UserActionState)e.OldValue, (UserActionState)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<UserActionState>> ViewSaverUserActionStateChanged;

        
        protected virtual void OnViewSaverUserActionStateChanged(PropertyChangedEventArgs<UserActionState> e)
        {
            OnViewSaverUserActionStateChangedImplementation(e);
            RaisePropertyChangedEvent(ViewSaverUserActionStateChanged, e);
        }

        partial void OnViewSaverUserActionStateChangedImplementation(PropertyChangedEventArgs<UserActionState> e);

        
        private void RaisePropertyChangedEvent<T>(EventHandler<PropertyChangedEventArgs<T>> eh, PropertyChangedEventArgs<T> e)
        {
            if (eh != null)
            {
                eh(this,e);
            }
        }

        //
        // OnApplyTemplate
        //

        
        public override void OnApplyTemplate()
        {
            PreOnApplyTemplate();
            base.OnApplyTemplate();
            this.viewManager = WpfHelp.GetTemplateChild<ListOrganizer>(this,"PART_ViewManager");
            this.viewSaver = WpfHelp.GetTemplateChild<PickerBase>(this,"PART_ViewSaver");
            PostOnApplyTemplate();
        }

        partial void PreOnApplyTemplate();

        partial void PostOnApplyTemplate();

        //
        // Static constructor
        //

        
        static ManagementList()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ManagementList), new FrameworkPropertyMetadata(typeof(ManagementList)));
            CommandManager.RegisterClassCommandBinding( typeof(ManagementList), new CommandBinding( ManagementList.ClearFilterCommand, ClearFilterCommand_CommandExecuted, ClearFilterCommand_CommandCanExecute ));
            CommandManager.RegisterClassCommandBinding( typeof(ManagementList), new CommandBinding( ManagementList.SaveViewCommand, SaveViewCommand_CommandExecuted, SaveViewCommand_CommandCanExecute ));
            CommandManager.RegisterClassCommandBinding( typeof(ManagementList), new CommandBinding( ManagementList.StartFilterCommand, StartFilterCommand_CommandExecuted, StartFilterCommand_CommandCanExecute ));
            CommandManager.RegisterClassCommandBinding( typeof(ManagementList), new CommandBinding( ManagementList.StopFilterCommand, StopFilterCommand_CommandExecuted, StopFilterCommand_CommandCanExecute ));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

        //
        // CreateAutomationPeer
        //
        
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new ExtendedFrameworkElementAutomationPeer(this,AutomationControlType.Pane);
        }

    }
}
#endregion
