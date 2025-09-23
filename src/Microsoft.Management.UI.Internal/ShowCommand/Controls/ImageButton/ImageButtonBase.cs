// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Required by XAML")]
    public class ImageButtonBase : Grid
    {
        
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(RoutedUICommand), typeof(ImageButton));

        
        public static readonly DependencyProperty EnabledImageSourceProperty =
            DependencyProperty.Register("EnabledImageSource", typeof(ImageSource), typeof(ImageButton));

        
        public static readonly DependencyProperty DisabledImageSourceProperty =
            DependencyProperty.Register("DisabledImageSource", typeof(ImageSource), typeof(ImageButton));

        
        public ImageSource EnabledImageSource
        {
            get { return (ImageSource)GetValue(ImageButton.EnabledImageSourceProperty); }
            set { SetValue(ImageButton.EnabledImageSourceProperty, value); }
        }

        
        public ImageSource DisabledImageSource
        {
            get { return (ImageSource)GetValue(ImageButton.DisabledImageSourceProperty); }
            set { SetValue(ImageButton.DisabledImageSourceProperty, value); }
        }

        
        public RoutedUICommand Command
        {
            get { return (RoutedUICommand)GetValue(ImageButton.CommandProperty); }
            set { SetValue(ImageButton.CommandProperty, value); }
        }
    }
}
