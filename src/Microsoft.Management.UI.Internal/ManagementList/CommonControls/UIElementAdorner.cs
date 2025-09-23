// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    internal partial class UIElementAdorner : Adorner
    {
        private VisualCollection children;

        
        public UIElementAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            this.children = new VisualCollection(this);
        }

        
        protected override Visual GetVisualChild(int index)
        {
            return this.children[index];
        }

        
        protected override int VisualChildrenCount
        {
            get
            {
                return this.children.Count;
            }
        }

        
        protected override Size MeasureOverride(Size constraint)
        {
            if (this.Child != null)
            {
                this.Child.Measure(constraint);
                return this.Child.DesiredSize;
            }
            else
            {
                return base.MeasureOverride(constraint);
            }
        }

        
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.Child != null)
            {
                Point location = new Point(0, 0);
                Rect rect = new Rect(location, finalSize);
                this.Child.Arrange(rect);
                return this.Child.RenderSize;
            }
            else
            {
                return base.ArrangeOverride(finalSize);
            }
        }

        partial void OnChildChangedImplementation(PropertyChangedEventArgs<UIElement> e)
        {
            if (e.OldValue != null)
            {
                this.children.Remove(e.OldValue);
                this.RemoveLogicalChild(e.OldValue);
            }

            if (this.Child != null)
            {
                this.children.Add(this.Child);
                this.AddLogicalChild(this.Child);
            }
        }
    }
}
