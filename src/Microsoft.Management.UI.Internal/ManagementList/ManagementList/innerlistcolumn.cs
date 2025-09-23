// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.Management.UI.Internal
{
    
    public partial class InnerListColumn : GridViewColumn
    {
        #region constructor

        
        static InnerListColumn()
        {
            WidthProperty.OverrideMetadata(typeof(InnerListColumn), new FrameworkPropertyMetadata(null, WidthProperty_CoerceProperty));
        }

        
        private InnerListColumn()
        {
            // This constructor intentionally left blank
        }

        
        /// <param name="dataDescription">The property description for this column's data.</param>
        public InnerListColumn(UIPropertyGroupDescription dataDescription)
            : this(dataDescription, true, true)
        {
            // This constructor just calls another constructor to create a visible column with a simple binding.
        }

        
        /// <param name="dataDescription">The property description for this column's data.</param>
        /// <param name="isVisible">Whether the column is initially visible.</param>
        public InnerListColumn(UIPropertyGroupDescription dataDescription, bool isVisible)
            : this(dataDescription, isVisible, true)
        {
            // This constructor just calls another constructor to create a column with a simple binding.
        }

        
        /// <param name="dataDescription">The description of the data this column is bound to.</param>
        /// <param name="isVisible">Whether the column is initially visible.</param>
        /// <param name="createDefaultBinding">Whether the column should create a default binding using the specified data's property.</param>
        public InnerListColumn(UIPropertyGroupDescription dataDescription, bool isVisible, bool createDefaultBinding)
        {
            ArgumentNullException.ThrowIfNull(dataDescription);

            GridViewColumnHeader header = new GridViewColumnHeader();
            header.Content = dataDescription.DisplayContent;
            header.DataContext = this;

            Binding automationNameBinding = new Binding("DataDescription.DisplayName");
            automationNameBinding.Source = this;
            header.SetBinding(AutomationProperties.NameProperty, automationNameBinding);

            this.Visible = isVisible;
            this.Header = header;
            this.DataDescription = dataDescription;

            if (createDefaultBinding)
            {
                var defaultBinding = new Binding(dataDescription.PropertyName.Replace("/", " ").Replace(".", " "));
                defaultBinding.StringFormat = GetDefaultStringFormat(dataDescription.DataType);
                defaultBinding.ConverterCulture = CultureInfo.CurrentCulture;

                this.DisplayMemberBinding = defaultBinding;
            }
        }
        #endregion constructor

        #region private methods
        #region private static methods

        private static object WidthProperty_CoerceProperty(DependencyObject d, object baseValue)
        {
            InnerListColumn ilc = (InnerListColumn)d;

            if (((double)baseValue) < ilc.MinWidth)
            {
                return ilc.MinWidth;
            }

            return baseValue;
        }

        partial void OnMinWidthChangedImplementation(PropertyChangedEventArgs<double> e)
        {
            this.CoerceValue(WidthProperty);
        }

        static partial void MinWidthProperty_ValidatePropertyImplementation(double value, ref bool isValid)
        {
            isValid = (value >= 0.0)
                && !double.IsNaN(value)
                && !double.IsPositiveInfinity(value);
        }

        
        /// <param name="type">The type to get a string format for.</param>
        /// <returns>A default string format for the specified type.</returns>
        private static string GetDefaultStringFormat(Type type)
        {
            if (type.IsEnum)
            {
                return InvariantResources.ManagementListDefaultColumnFormatString;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.DateTime:
                    return InvariantResources.ManagementListDateTimeColumnFormatString;
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return InvariantResources.ManagementListIntegerColumnFormatString;
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return InvariantResources.ManagementListFloatColumnFormatString;
                default:
                    return InvariantResources.ManagementListDefaultColumnFormatString;
            }
        }

        #endregion private static methods
        #endregion private methods

        #region ToString
        
        /// <returns>A string to represent the instance of this class.</returns>
        public override string ToString()
        {
            return this.DataDescription.ToString();
        }
        #endregion ToString
    }
}
