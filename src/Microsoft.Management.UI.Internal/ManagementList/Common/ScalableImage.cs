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
    /// <content>
    /// Partial class implementation for ScalableImage control.
    /// </content>
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

        
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used to render the control.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // If a vector is provided, specify that the control will use all available area \\
            if (this.Source != null && this.Source.Brush != null)
            {
                return finalSize;
            }

            return base.ArrangeOverride(finalSize);
        }

        
        /// <param name="drawingContext">An instance of <see cref="System.Windows.Media.DrawingContext"/> used to render the control.</param>
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

        
        /// <param name="layoutSlotSize">An instance of <see cref="System.Windows.Size"/> used for calculating an additional clip.</param>
        /// <returns>Geometry to use as an additional clip in case when element is larger than available space.</returns>
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
