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

        
        /// <param name="value">
        /// The <see cref="ValidatingValue{T}"/> to convert.
        /// </param>
        /// <param name="targetType">
        /// The parameter is not used.
        /// </param>
        /// <param name="parameter">
        /// The parameter is not used.
        /// </param>
        /// <param name="culture">
        /// The parameter is not used.
        /// </param>
        /// <returns>
        /// The type of value.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return typeof(string);
            }

            return value.GetType().GetGenericArguments()[0];
        }

        
        /// <param name="value">The parameter is not used.</param>
        /// <param name="targetType">The parameter is not used.</param>
        /// <param name="parameter">The parameter is not used.</param>
        /// <param name="culture">The parameter is not used.</param>
        /// <returns>The parameter is not used.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
