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
    partial class ScalableImage
    {
        //
        // Source dependency property
        //
        
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register( "Source", typeof(ScalableImageSource), typeof(ScalableImage), new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions.AffectsRender, SourceProperty_PropertyChanged) );

        
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the ScalableImageSource used to render the image. This is a dependency property.")]
        [Localizability(LocalizationCategory.None)]
        public ScalableImageSource Source
        {
            get
            {
                return (ScalableImageSource) GetValue(SourceProperty);
            }
            set
            {
                SetValue(SourceProperty,value);
            }
        }

        static private void SourceProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScalableImage obj = (ScalableImage) o;
            obj.OnSourceChanged( new PropertyChangedEventArgs<ScalableImageSource>((ScalableImageSource)e.OldValue, (ScalableImageSource)e.NewValue) );
        }

        
        public event EventHandler<PropertyChangedEventArgs<ScalableImageSource>> SourceChanged;

        
        protected virtual void OnSourceChanged(PropertyChangedEventArgs<ScalableImageSource> e)
        {
            OnSourceChangedImplementation(e);
            RaisePropertyChangedEvent(SourceChanged, e);
        }

        partial void OnSourceChangedImplementation(PropertyChangedEventArgs<ScalableImageSource> e);

        
        private void RaisePropertyChangedEvent<T>(EventHandler<PropertyChangedEventArgs<T>> eh, PropertyChangedEventArgs<T> e)
        {
            if (eh != null)
            {
                eh(this,e);
            }
        }

        //
        // CreateAutomationPeer
        //
        
        /// <returns>
        /// An instance of the AutomationPeer.
        /// </returns>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new ExtendedFrameworkElementAutomationPeer(owner: this, controlType: AutomationControlType.Image, isControlElement: false);
        }

    }
}
#endregion
