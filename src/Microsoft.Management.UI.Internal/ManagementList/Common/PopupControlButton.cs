// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Microsoft.Management.UI.Internal
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class PopupControlButton : ExpanderButton
    {
        private bool isClickInProgress = false;

        
        protected override string ExpandToolTip
        {
            get { return XamlLocalizableResources.AutoResXGen_ManagementList2_ToolTip_132; }
        }

        
        public PopupControlButton()
        {
            // nothing
        }

        
        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            this.UpdateIsPopupOpen();
        }

        
        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            this.UpdateIsPopupOpen();
        }

        private void UpdateIsPopupOpen()
        {
            this.IsPopupOpen = this.IsChecked.GetValueOrDefault();
        }

        
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            // If the mouse is captured then we need to finish updating state after the current event it processed.
            if (this.IsMouseCaptured && this.isClickInProgress)
            {
                this.isClickInProgress = false;
                this.ReleaseMouseCapture();

                this.Dispatcher.BeginInvoke(
                                         new UpdateIsCheckedDelegate(this.UpdateIsChecked),
                                         DispatcherPriority.Input,
                                         null);
            }

            base.OnPreviewMouseLeftButtonUp(e);
        }

        private delegate void UpdateIsCheckedDelegate();

        partial void OnIsPopupOpenChangedImplementation(PropertyChangedEventArgs<bool> e)
        {
            // If it looks like the button is in the act of being pressed,
            // then we don't want to update the IsChecked since the button
            // push will do it.
            //
            // However we do need to handle the case where the mouse down is on the
            // button, but mouse up isn't.
            //
            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed && this.IsPopupOpen == false)
            {
                if (this.GetIsMouseReallyOver())
                {
                    this.isClickInProgress = true;
                    this.CaptureMouse();
                }
            }

            if (this.isClickInProgress == false)
            {
                this.UpdateIsChecked();
            }
        }

        private bool GetIsMouseReallyOver()
        {
            Point pos = Mouse.PrimaryDevice.GetPosition(this);

            if ((pos.X >= 0) && (pos.X <= ActualWidth) && (pos.Y >= 0) && (pos.Y <= ActualHeight))
            {
                return true;
            }

            return false;
        }

        private void UpdateIsChecked()
        {
            this.IsChecked = this.IsPopupOpen;
        }
    }
}
