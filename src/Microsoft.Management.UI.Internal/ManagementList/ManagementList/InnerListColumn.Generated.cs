// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{

    
    [Localizability(LocalizationCategory.None)]
    partial class InnerListColumn
    {
        //
        // DataDescription dependency property
        //
        
        public static readonly DependencyProperty DataDescriptionProperty = DependencyProperty.Register( "DataDescription", typeof(UIPropertyGroupDescription), typeof(InnerListColumn), new PropertyMetadata( null, DataDescriptionProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the data description.")]
        [Localizability(LocalizationCategory.None)]
        public UIPropertyGroupDescription DataDescription
        {
            get
            {
                return (UIPropertyGroupDescription) GetValue(DataDescriptionProperty);
            }
            set
            {
                SetValue(DataDescriptionProperty,value);
            }
        }

        static private void DataDescriptionProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            InnerListColumn obj = (InnerListColumn) o;
            obj.OnDataDescriptionChanged( new PropertyChangedEventArgs<UIPropertyGroupDescription>((UIPropertyGroupDescription)e.OldValue, (UIPropertyGroupDescription)e.NewValue) );
        }

        
        protected virtual void OnDataDescriptionChanged(PropertyChangedEventArgs<UIPropertyGroupDescription> e)
        {
            OnDataDescriptionChangedImplementation(e);
            this.OnPropertyChanged(new PropertyChangedEventArgs("DataDescription"));
        }

        partial void OnDataDescriptionChangedImplementation(PropertyChangedEventArgs<UIPropertyGroupDescription> e);

        //
        // MinWidth dependency property
        //
        
        public static readonly DependencyProperty MinWidthProperty = DependencyProperty.Register( "MinWidth", typeof(double), typeof(InnerListColumn), new PropertyMetadata( 20.0, MinWidthProperty_PropertyChanged), MinWidthProperty_ValidateProperty );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value that dictates the minimum allowable width of the column.")]
        [Localizability(LocalizationCategory.None)]
        public double MinWidth
        {
            get
            {
                return (double) GetValue(MinWidthProperty);
            }
            set
            {
                SetValue(MinWidthProperty,value);
            }
        }

        static private void MinWidthProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            InnerListColumn obj = (InnerListColumn) o;
            obj.OnMinWidthChanged( new PropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue) );
        }

        
        protected virtual void OnMinWidthChanged(PropertyChangedEventArgs<double> e)
        {
            OnMinWidthChangedImplementation(e);
            this.OnPropertyChanged(new PropertyChangedEventArgs("MinWidth"));
        }

        partial void OnMinWidthChangedImplementation(PropertyChangedEventArgs<double> e);

        static private bool MinWidthProperty_ValidateProperty(object value)
        {
            bool isValid = false;
            MinWidthProperty_ValidatePropertyImplementation((double) value, ref isValid);
            return isValid;
        }

        static partial void MinWidthProperty_ValidatePropertyImplementation(double value, ref bool isValid);

        //
        // Required dependency property
        //
        
        public static readonly DependencyProperty RequiredProperty = DependencyProperty.Register( "Required", typeof(bool), typeof(InnerListColumn), new PropertyMetadata( BooleanBoxes.FalseBox, RequiredProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether the column may not be removed.")]
        [Localizability(LocalizationCategory.None)]
        public bool Required
        {
            get
            {
                return (bool) GetValue(RequiredProperty);
            }
            set
            {
                SetValue(RequiredProperty,BooleanBoxes.Box(value));
            }
        }

        static private void RequiredProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            InnerListColumn obj = (InnerListColumn) o;
            obj.OnRequiredChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        
        protected virtual void OnRequiredChanged(PropertyChangedEventArgs<bool> e)
        {
            OnRequiredChangedImplementation(e);
            this.OnPropertyChanged(new PropertyChangedEventArgs("Required"));
        }

        partial void OnRequiredChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // Visible dependency property
        //
        
        public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register( "Visible", typeof(bool), typeof(InnerListColumn), new PropertyMetadata( BooleanBoxes.TrueBox, VisibleProperty_PropertyChanged) );

        
        /// <remarks>
        /// Modifying the Visible property does not in itself make the column visible or not visible.  This should always be kept in sync with the Columns property.
        /// </remarks>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating whether the columns we want to have available in the list.")]
        [Localizability(LocalizationCategory.None)]
        public bool Visible
        {
            get
            {
                return (bool) GetValue(VisibleProperty);
            }
            set
            {
                SetValue(VisibleProperty,BooleanBoxes.Box(value));
            }
        }

        static private void VisibleProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            InnerListColumn obj = (InnerListColumn) o;
            obj.OnVisibleChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        
        protected virtual void OnVisibleChanged(PropertyChangedEventArgs<bool> e)
        {
            OnVisibleChangedImplementation(e);
            this.OnPropertyChanged(new PropertyChangedEventArgs("Visible"));
        }

        partial void OnVisibleChangedImplementation(PropertyChangedEventArgs<bool> e);

    }
}
#endregion
