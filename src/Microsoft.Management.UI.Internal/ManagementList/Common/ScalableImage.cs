// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class ScalableImage : FrameworkElement
    {
        #region Structors

        
        public ScalableImage()
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        
        protected override Size ArrangeOverride(Size finalSize)
        {
            // If a vector is provided, specify that the control will use all available area \\
            if (this.Source != null && this.Source.Brush != null)
            {
                return finalSize;
            }

            return base.ArrangeOverride(finalSize);
        }

        
        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect renderArea = new Rect(this.RenderSize);

            // No source was provided \\
            if (this.Source == null)
            {
                return;
            }

            // Prefer the vector if it's provided \\
            if (this.Source.Brush != null)
            {
                drawingContext.DrawRectangle(this.Source.Brush, null, renderArea);
            }
            else if (this.Source.Image != null)
            {
                drawingContext.DrawImage(this.Source.Image, renderArea);
            }
        }

        
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            return ClipToBounds ? base.GetLayoutClip(layoutSlotSize) : null;
        }

        #endregion

        #region Protected Methods

        partial void OnSourceChangedImplementation(PropertyChangedEventArgs<ScalableImageSource> e)
        {
            if (e.NewValue != null)
            {
                // If a width was provided in the source, use it now \\
                if (!e.NewValue.Size.Width.Equals(double.NaN))
                {
                    this.Width = e.NewValue.Size.Width;
                }

                // If a height was provided in the source, use it now \\
                if (!e.NewValue.Size.Height.Equals(double.NaN))
                {
                    this.Height = e.NewValue.Size.Height;
                }

                // Bind the image's accessible name to the one set in the source \\
                Binding accessibleNameBinding = new Binding(ScalableImageSource.AccessibleNameProperty.Name);
                accessibleNameBinding.Source = this.Source;
                this.SetBinding(AutomationProperties.NameProperty, accessibleNameBinding);
            }
        }

        #endregion
    }
}
