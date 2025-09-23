// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsEqualConverter : IMultiValueConverter
    {
        
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (values.Length != 2)
            {
                throw new ArgumentException("Two values expected", "values");
            }

            object item1 = values[0];
            object item2 = values[1];

            if (item1 == null)
            {
                return item2 == null;
            }

            if (item2 == null)
            {
                return false;
            }

            bool equal = item1.Equals(item2);
            return equal;
        }

        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
