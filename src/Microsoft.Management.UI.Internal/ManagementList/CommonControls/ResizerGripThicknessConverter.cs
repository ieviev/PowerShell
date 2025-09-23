// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class ResizerGripThicknessConverter : IMultiValueConverter
    {
        
        public ResizerGripThicknessConverter()
        {
            // nothing
        }

        
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (object.ReferenceEquals(values[0], DependencyProperty.UnsetValue) ||
                object.ReferenceEquals(values[1], DependencyProperty.UnsetValue))
            {
                return DependencyProperty.UnsetValue;
            }

            var resizerVisibleGripWidth = (double)values[0];

            var gripLocation = (ResizeGripLocation)values[1];

            return Resizer.CreateGripThickness(resizerVisibleGripWidth, gripLocation);
        }

        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
