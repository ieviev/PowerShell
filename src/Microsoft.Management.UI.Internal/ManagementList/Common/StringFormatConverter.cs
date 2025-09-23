// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class StringFormatConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, Object parameter, CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            string str = (string)value;
            string formatString = (string)parameter;
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            return string.Format(culture, formatString, str);
        }

        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
