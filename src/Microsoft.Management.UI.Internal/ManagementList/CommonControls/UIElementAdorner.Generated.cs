// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{

    
    [Localizability(LocalizationCategory.None)]
    partial class UIElementAdorner
    {
        //
        // Child dependency property
        //
        
        public static readonly DependencyProperty ChildProperty = DependencyProperty.Register( "Child", typeof(UIElement), typeof(UIElementAdorner), new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions. AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure , ChildProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the child element.")]
        [Localizability(LocalizationCategory.None)]
        public UIElement Child
        {
            get
            {
                return (UIElement) GetValue(ChildProperty);
            }
            set
            {
                SetValue(ChildProperty,value);
            }
        }

        static private void ChildProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            UIElementAdorner obj = (UIElementAdorner) o;
            obj.OnChildChanged( new PropertyChangedEventArgs<UIElement>((UIElement)e.OldValue, (UIElement)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<UIElement>> ChildChanged;

        
        private void RaiseChildChanged(PropertyChangedEventArgs<UIElement> e)
        {
            var eh = this.ChildChanged;
            if (eh != null)
            {
                eh(this,e);
            }
        }

        
        protected virtual void OnChildChanged(PropertyChangedEventArgs<UIElement> e)
        {
            OnChildChangedImplementation(e);
            RaiseChildChanged(e);
        }

        partial void OnChildChangedImplementation(PropertyChangedEventArgs<UIElement> e);

    }
}
#endregion
