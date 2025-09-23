// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class VisualToAncestorDataConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(value);

            ArgumentNullException.ThrowIfNull(parameter);

            Type dataType = (Type)parameter;

            if (dataType.IsClass == false)
            {
                throw new ArgumentException("The specified value is not a class type.", "parameter");
            }

            DependencyObject obj = (DependencyObject)value;
            MethodInfo findVisualAncestorDataMethod = typeof(WpfHelp).GetMethod("FindVisualAncestorData");
            MethodInfo genericFindVisualAncestorDataMethod = findVisualAncestorDataMethod.MakeGenericMethod(dataType);

            return genericFindVisualAncestorDataMethod.Invoke(null, new object[] { obj });
        }

        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
