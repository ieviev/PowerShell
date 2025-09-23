// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{

    
    [Localizability(LocalizationCategory.None)]
    partial class ScalableImageSource
    {
        //
        // AccessibleName dependency property
        //
        
        public static readonly DependencyProperty AccessibleNameProperty = DependencyProperty.Register( "AccessibleName", typeof(string), typeof(ScalableImageSource), new PropertyMetadata( null, AccessibleNameProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the accessible name of the image.  This is used by accessibility clients to describe the image, and must be localized.")]
        [Localizability(LocalizationCategory.Text, Modifiability=Modifiability.Modifiable, Readability=Readability.Readable)]
        public string AccessibleName
        {
            get
            {
                return (string) GetValue(AccessibleNameProperty);
            }
            set
            {
                SetValue(AccessibleNameProperty,value);
            }
        }

        static private void AccessibleNameProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScalableImageSource obj = (ScalableImageSource) o;
            obj.OnAccessibleNameChanged( new PropertyChangedEventArgs<string>((string)e.OldValue, (string)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<string>> AccessibleNameChanged;

        
        protected virtual void OnAccessibleNameChanged(PropertyChangedEventArgs<string> e)
        {
            OnAccessibleNameChangedImplementation(e);
            RaisePropertyChangedEvent(AccessibleNameChanged, e);
        }

        partial void OnAccessibleNameChangedImplementation(PropertyChangedEventArgs<string> e);

        //
        // Brush dependency property
        //
        
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register( "Brush", typeof(Brush), typeof(ScalableImageSource), new PropertyMetadata( null, BrushProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the source used to render the image as a vector.If this is set, the Image property will be ignored.")]
        [Localizability(LocalizationCategory.None)]
        public Brush Brush
        {
            get
            {
                return (Brush) GetValue(BrushProperty);
            }
            set
            {
                SetValue(BrushProperty,value);
            }
        }

        static private void BrushProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScalableImageSource obj = (ScalableImageSource) o;
            obj.OnBrushChanged( new PropertyChangedEventArgs<Brush>((Brush)e.OldValue, (Brush)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<Brush>> BrushChanged;

        
        protected virtual void OnBrushChanged(PropertyChangedEventArgs<Brush> e)
        {
            OnBrushChangedImplementation(e);
            RaisePropertyChangedEvent(BrushChanged, e);
        }

        partial void OnBrushChangedImplementation(PropertyChangedEventArgs<Brush> e);

        //
        // Image dependency property
        //
        
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register( "Image", typeof(ImageSource), typeof(ScalableImageSource), new PropertyMetadata( null, ImageProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the source used to render the image as a bitmap. If the Brush property is set, this will be ignored.")]
        [Localizability(LocalizationCategory.None)]
        public ImageSource Image
        {
            get
            {
                return (ImageSource) GetValue(ImageProperty);
            }
            set
            {
                SetValue(ImageProperty,value);
            }
        }

        static private void ImageProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScalableImageSource obj = (ScalableImageSource) o;
            obj.OnImageChanged( new PropertyChangedEventArgs<ImageSource>((ImageSource)e.OldValue, (ImageSource)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<ImageSource>> ImageChanged;

        
        protected virtual void OnImageChanged(PropertyChangedEventArgs<ImageSource> e)
        {
            OnImageChangedImplementation(e);
            RaisePropertyChangedEvent(ImageChanged, e);
        }

        partial void OnImageChangedImplementation(PropertyChangedEventArgs<ImageSource> e);

        //
        // Size dependency property
        //
        
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register( "Size", typeof(Size), typeof(ScalableImageSource), new PropertyMetadata( new Size(double.NaN, double.NaN), SizeProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the suggested size of the image.")]
        [Localizability(LocalizationCategory.None)]
        public Size Size
        {
            get
            {
                return (Size) GetValue(SizeProperty);
            }
            set
            {
                SetValue(SizeProperty,value);
            }
        }

        static private void SizeProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScalableImageSource obj = (ScalableImageSource) o;
            obj.OnSizeChanged( new PropertyChangedEventArgs<Size>((Size)e.OldValue, (Size)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<Size>> SizeChanged;

        
        protected virtual void OnSizeChanged(PropertyChangedEventArgs<Size> e)
        {
            OnSizeChangedImplementation(e);
            RaisePropertyChangedEvent(SizeChanged, e);
        }

        partial void OnSizeChangedImplementation(PropertyChangedEventArgs<Size> e);

        
        private void RaisePropertyChangedEvent<T>(EventHandler<PropertyChangedEventArgs<T>> eh, PropertyChangedEventArgs<T> e)
        {
            if (eh != null)
            {
                eh(this,e);
            }
        }

    }
}
#endregion
