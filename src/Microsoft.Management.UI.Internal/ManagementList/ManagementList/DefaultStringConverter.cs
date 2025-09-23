// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DefaultStringConverter : IMultiValueConverter
    {
        
        public string DefaultValue
        {
            get;
            set;
        }

        
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (values.Length != 1)
            {
                throw new ArgumentNullException("values");
            }

            string val = values[0] as string;
            if (!string.IsNullOrEmpty(val))
            {
                return val;
            }

            return this.DefaultValue;
        }

        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[1] { Binding.DoNothing };
        }
    }
}
