// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class ViewGroupToStringConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            CollectionViewGroup cvg = value as CollectionViewGroup;
            if (cvg == null)
            {
                throw new ArgumentException("value must be of type CollectionViewGroup", "value");
            }

            string name = (!string.IsNullOrEmpty(cvg.Name.ToString())) ? cvg.Name.ToString() : UICultureResources.GroupTitleNone;
            string display = string.Create(CultureInfo.CurrentCulture, $"{name} ({cvg.ItemCount})");

            return display;
        }

        
        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            // I can't think of nothing that could be added to the exception message
            // that would be of further help
            throw new NotSupportedException();
        }
    }
}
