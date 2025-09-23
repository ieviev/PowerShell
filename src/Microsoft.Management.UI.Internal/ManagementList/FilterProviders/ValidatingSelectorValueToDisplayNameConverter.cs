// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ValidatingSelectorValueToDisplayNameConverter : IMultiValueConverter
    {
        
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (values.Length != 2)
            {
                throw new ArgumentException("Two values expected", "values");
            }

            // NOTE : null values are ok.
            object input = values[0];

            IValueConverter converter = values[1] as IValueConverter;
            if (converter == null)
            {
                throw new ArgumentException("Second value should be a IValueConverter", "values");
            }

            if (targetType != typeof(string))
            {
                throw new ArgumentException("targetType should be of type string", "targetType");
            }

            return converter.Convert(input, targetType, parameter, culture);
        }

        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
