// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ValidatingValueToGenericParameterTypeConverter : IValueConverter
    {
        
        public static ValidatingValueToGenericParameterTypeConverter Instance
        {
            get
            {
                return new ValidatingValueToGenericParameterTypeConverter();
            }
        }

        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return typeof(string);
            }

            return value.GetType().GetGenericArguments()[0];
        }

        
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
