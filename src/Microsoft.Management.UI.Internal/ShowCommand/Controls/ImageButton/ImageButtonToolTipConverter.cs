// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Needed for XAML")]
    public class ImageButtonToolTipConverter : IValueConverter
    {
        // This class is meant to be used like this in XAML:
        //  <Window xmlns:controls="clr-namespace:Microsoft.PowerShell.Commands.ShowCommandInternal" ...>
        //     ...
        //     <Window.Resources>
        //        <controls:RoutedUICommandToString x:Key="routedUICommandToString"/>
        //     </Window.Resources>
        //     ...
        //     <ContentControl ToolTip="{Binding Path=..., Converter={StaticResource routedUICommandToString}"/>
        #region IValueConverter Members

        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ImageButtonBase imageButtonBase = value as ImageButtonBase;
            if (imageButtonBase == null)
            {
                return null;
            }

            object toolTipObj = imageButtonBase.GetValue(Button.ToolTipProperty);
            if (toolTipObj != null)
            {
                return toolTipObj.ToString();
            }

            if (imageButtonBase.Command != null && !string.IsNullOrEmpty(imageButtonBase.Command.Text))
            {
                return imageButtonBase.Command.Text.Replace("_", string.Empty);
            }

            return null;
        }

        
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
