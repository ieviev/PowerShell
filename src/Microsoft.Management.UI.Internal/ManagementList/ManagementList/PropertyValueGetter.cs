// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;

namespace Microsoft.Management.UI.Internal
{
    
    public class PropertyValueGetter : IPropertyValueGetter
    {
        private const string PropertyDescriptorColumnId = "PropertyDescriptor";

        private DataTable cachedProperties;

        
        public PropertyValueGetter()
        {
            // Create the table locally first so that FxCop detects the setting of Locale \\
            var cachedProperties = new DataTable();
            cachedProperties.Locale = CultureInfo.InvariantCulture;
            var dataTypeColumn = cachedProperties.Columns.Add("Type", typeof(Type));
            var propertyNameColumn = cachedProperties.Columns.Add("PropertyName", typeof(string));
            cachedProperties.Columns.Add(PropertyDescriptorColumnId, typeof(PropertyDescriptor));

            cachedProperties.PrimaryKey = new DataColumn[] { dataTypeColumn, propertyNameColumn };

            this.cachedProperties = cachedProperties;
        }

        
        public virtual bool TryGetPropertyValue(string propertyName, object value, out object propertyValue)
        {
            propertyValue = null;

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("propertyName is empty", "propertyName");
            }

            ArgumentNullException.ThrowIfNull(value);

            PropertyDescriptor descriptor = this.GetPropertyDescriptor(propertyName, value);
            if (descriptor == null)
            {
                return false;
            }

            return this.TryGetPropertyValueInternal(descriptor, value, out propertyValue);
        }

        
        public virtual bool TryGetPropertyValue<T>(string propertyName, object value, out T propertyValue)
        {
            propertyValue = default(T);

            object uncastPropertyValue;
            if (!this.TryGetPropertyValue(propertyName, value, out uncastPropertyValue))
            {
                return false;
            }

            return FilterUtilities.TryCastItem<T>(uncastPropertyValue, out propertyValue);
        }

        private PropertyDescriptor GetPropertyDescriptor(string propertyName, object value)
        {
            var dataType = value.GetType();
            var propertyRow = this.cachedProperties.Rows.Find(new object[]
            {
                dataType,
                propertyName
            });

            PropertyDescriptor descriptor;

            if (propertyRow == null)
            {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(value);

                descriptor = properties[propertyName];
                if (descriptor != null)
                {
                    this.cachedProperties.Rows.Add(dataType, propertyName, descriptor);
                }
            }
            else
            {
                descriptor = (PropertyDescriptor)propertyRow[PropertyDescriptorColumnId];
            }

            return descriptor;
        }

        private bool TryGetPropertyValueInternal(PropertyDescriptor descriptor, object value, out object propertyValue)
        {
            propertyValue = null;
            try
            {
                propertyValue = descriptor.GetValue(value);
                return true;
            }
            catch (Exception e)
            {
                if (e is AccessViolationException || e is StackOverflowException)
                {
                    throw;
                }
            }

            return false;
        }
    }
}
