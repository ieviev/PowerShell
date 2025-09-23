// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;

namespace Microsoft.Management.UI.Internal
{

    
    [Localizability(LocalizationCategory.None)]
    partial class ManagementListTitle
    {
        //
        // List dependency property
        //
        
        public static readonly DependencyProperty ListProperty = DependencyProperty.Register( "List", typeof(ManagementList), typeof(ManagementListTitle), new PropertyMetadata( null, ListProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the list this title is for. This is a dependency property.")]
        [Localizability(LocalizationCategory.None)]
        public ManagementList List
        {
            get
            {
                return (ManagementList) GetValue(ListProperty);
            }
            set
            {
                SetValue(ListProperty,value);
            }
        }

        static private void ListProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementListTitle obj = (ManagementListTitle) o;
            obj.OnListChanged( new PropertyChangedEventArgs<ManagementList>((ManagementList)e.OldValue, (ManagementList)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<ManagementList>> ListChanged;

        
        protected virtual void OnListChanged(PropertyChangedEventArgs<ManagementList> e)
        {
            OnListChangedImplementation(e);
            RaisePropertyChangedEvent(ListChanged, e);
        }

        partial void OnListChangedImplementation(PropertyChangedEventArgs<ManagementList> e);

        //
        // ListStatus dependency property
        //
        
        public static readonly DependencyProperty ListStatusProperty = DependencyProperty.Register( "ListStatus", typeof(string), typeof(ManagementListTitle), new PropertyMetadata( string.Empty, ListStatusProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets the status of the list. This is a dependency property.")]
        [Localizability(LocalizationCategory.Text, Modifiability=Modifiability.Modifiable, Readability=Readability.Readable)]
        public string ListStatus
        {
            get
            {
                return (string) GetValue(ListStatusProperty);
            }
            set
            {
                SetValue(ListStatusProperty,value);
            }
        }

        static private void ListStatusProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementListTitle obj = (ManagementListTitle) o;
            obj.OnListStatusChanged( new PropertyChangedEventArgs<string>((string)e.OldValue, (string)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<string>> ListStatusChanged;

        
        protected virtual void OnListStatusChanged(PropertyChangedEventArgs<string> e)
        {
            OnListStatusChangedImplementation(e);
            RaisePropertyChangedEvent(ListStatusChanged, e);
        }

        partial void OnListStatusChangedImplementation(PropertyChangedEventArgs<string> e);

        //
        // Title dependency property
        //
        
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register( "Title", typeof(string), typeof(ManagementListTitle), new PropertyMetadata( string.Empty, TitleProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the title. This is a dependency property.")]
        [Localizability(LocalizationCategory.Text, Modifiability=Modifiability.Modifiable, Readability=Readability.Readable)]
        public string Title
        {
            get
            {
                return (string) GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty,value);
            }
        }

        static private void TitleProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementListTitle obj = (ManagementListTitle) o;
            obj.OnTitleChanged( new PropertyChangedEventArgs<string>((string)e.OldValue, (string)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<string>> TitleChanged;

        
        protected virtual void OnTitleChanged(PropertyChangedEventArgs<string> e)
        {
            OnTitleChangedImplementation(e);
            RaisePropertyChangedEvent(TitleChanged, e);
        }

        partial void OnTitleChangedImplementation(PropertyChangedEventArgs<string> e);

        //
        // TotalItemCount dependency property
        //
        
        public static readonly DependencyProperty TotalItemCountProperty = DependencyProperty.Register( "TotalItemCount", typeof(int), typeof(ManagementListTitle), new PropertyMetadata( 0, TotalItemCountProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the number of items in the list before filtering is applied. This is a dependency property.")]
        [Localizability(LocalizationCategory.None)]
        public int TotalItemCount
        {
            get
            {
                return (int) GetValue(TotalItemCountProperty);
            }
            set
            {
                SetValue(TotalItemCountProperty,value);
            }
        }

        static private void TotalItemCountProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ManagementListTitle obj = (ManagementListTitle) o;
            obj.OnTotalItemCountChanged( new PropertyChangedEventArgs<int>((int)e.OldValue, (int)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<int>> TotalItemCountChanged;

        
        protected virtual void OnTotalItemCountChanged(PropertyChangedEventArgs<int> e)
        {
            OnTotalItemCountChangedImplementation(e);
            RaisePropertyChangedEvent(TotalItemCountChanged, e);
        }

        partial void OnTotalItemCountChangedImplementation(PropertyChangedEventArgs<int> e);

        
        private void RaisePropertyChangedEvent<T>(EventHandler<PropertyChangedEventArgs<T>> eh, PropertyChangedEventArgs<T> e)
        {
            if (eh != null)
            {
                eh(this,e);
            }
        }

        //
        // Static constructor
        //

        
        static ManagementListTitle()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ManagementListTitle), new FrameworkPropertyMetadata(typeof(ManagementListTitle)));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

        //
        // CreateAutomationPeer
        //
        
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new ExtendedFrameworkElementAutomationPeer(this,AutomationControlType.StatusBar);
        }

    }
}
#endregion
