// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    
    /// <remarks>
    /// The sender of this event is an object of type <see cref="PSObjectPropertyDescriptor"/>.
    /// It is permitted to subclass <see cref="SettingValueExceptionEventArgs"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class SettingValueExceptionEventArgs : EventArgs
    {
        
        /// <remarks>
        /// The default value is true, indicating that the Exception associated with this event will be thrown.
        /// </remarks>
        public bool ShouldThrow { get; set; }

        
        public Exception Exception { get; }

        
        /// <param name="exception">Exception that triggered the associated event.</param>
        internal SettingValueExceptionEventArgs(Exception exception)
        {
            Exception = exception;
            ShouldThrow = true;
        }
    }

    
    /// <remarks>
    /// The sender of this event is an object of type <see cref="PSObjectPropertyDescriptor"/>.
    /// It is permitted to subclass <see cref="GettingValueExceptionEventArgs"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class GettingValueExceptionEventArgs : EventArgs
    {
        
        public bool ShouldThrow { get; set; }

        
        public Exception Exception { get; }

        
        /// <param name="exception">Exception that triggered the associated event.</param>
        internal GettingValueExceptionEventArgs(Exception exception)
        {
            Exception = exception;
            ValueReplacement = null;
            ShouldThrow = true;
        }

        
        public object ValueReplacement { get; set; }
    }

    
    /// <remarks>
    /// It is permitted to subclass <see cref="SettingValueExceptionEventArgs"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSObjectPropertyDescriptor : PropertyDescriptor
    {
        internal event EventHandler<SettingValueExceptionEventArgs> SettingValueException;

        internal event EventHandler<GettingValueExceptionEventArgs> GettingValueException;

        internal PSObjectPropertyDescriptor(string propertyName, Type propertyType, bool isReadOnly, AttributeCollection propertyAttributes)
            : base(propertyName, Array.Empty<Attribute>())
        {
            IsReadOnly = isReadOnly;
            Attributes = propertyAttributes;
            PropertyType = propertyType;
        }

        
        public override AttributeCollection Attributes { get; }

        
        public override bool IsReadOnly { get; }

        
        /// <param name="component">This parameter is ignored for <see cref="PSObjectPropertyDescriptor"/></param>
        public override void ResetValue(object component) { }

        
        /// <param name="component">The component to test for reset capability.</param>
        /// <returns>False.</returns>
        public override bool CanResetValue(object component) { return false; }

        
        /// <param name="component">The component with the property to be examined for persistence.</param>
        /// <returns>True.</returns>
        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }

        
        /// <remarks>This property returns the <see cref="PSObject"/> type.</remarks>
        public override Type ComponentType
        {
            get { return typeof(PSObject); }
        }

        
        public override Type PropertyType { get; }

        
        /// <param name="component">The component with the property for which to retrieve the value.</param>
        /// <returns>The value of a property for a given component.</returns>
        /// <exception cref="ExtendedTypeSystemException">
        /// If the property has not been found in the component or an exception has
        /// been thrown when getting the value of the property.
        /// This Exception will only be thrown if there is no event handler for the GettingValueException
        /// event of the <see cref="PSObjectTypeDescriptor"/> that created this <see cref="PSObjectPropertyDescriptor"/>.
        /// If there is an event handler, it can prevent this exception from being thrown, by changing
        /// the ShouldThrow property of <see cref="GettingValueExceptionEventArgs"/> from its default
        /// value of true to false.
        /// </exception>
        /// <exception cref="PSArgumentNullException">If <paramref name="component"/> is null.</exception>
        /// <exception cref="PSArgumentException">If <paramref name="component"/> is not
        /// an <see cref="PSObject"/> or an <see cref="PSObjectTypeDescriptor"/>.</exception>
        public override object GetValue(object component)
        {
            if (component == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(component));
            }

            PSObject mshObj = GetComponentPSObject(component);
            PSPropertyInfo property;
            try
            {
                property = mshObj.Properties[this.Name] as PSPropertyInfo;
                if (property == null)
                {
                    PSObjectTypeDescriptor.typeDescriptor.WriteLine("Could not find property \"{0}\" to get its value.", this.Name);
                    ExtendedTypeSystemException e = new ExtendedTypeSystemException("PropertyNotFoundInPropertyDescriptorGetValue",
                        null,
                        ExtendedTypeSystem.PropertyNotFoundInTypeDescriptor, this.Name);
                    bool shouldThrow;
                    object returnValue = DealWithGetValueException(e, out shouldThrow);
                    if (shouldThrow)
                    {
                        throw e;
                    }

                    return returnValue;
                }

                return property.Value;
            }
            catch (ExtendedTypeSystemException e)
            {
                PSObjectTypeDescriptor.typeDescriptor.WriteLine("Exception getting the value of the property \"{0}\": \"{1}\".", this.Name, e.Message);
                bool shouldThrow;
                object returnValue = DealWithGetValueException(e, out shouldThrow);
                if (shouldThrow)
                {
                    throw;
                }

                return returnValue;
            }
        }

        private static PSObject GetComponentPSObject(object component)
        {
            // If you use the PSObjectTypeDescriptor directly as your object, it will be the component
            // if you use a provider, the PSObject will be the component.
            PSObject mshObj = component as PSObject;
            if (mshObj == null)
            {
                if (!(component is PSObjectTypeDescriptor descriptor))
                {
                    throw PSTraceSource.NewArgumentException(nameof(component), ExtendedTypeSystem.InvalidComponent,
                                                             "component",
                                                             nameof(PSObject),
                                                             nameof(PSObjectTypeDescriptor));
                }

                mshObj = descriptor.Instance;
            }

            return mshObj;
        }

        private object DealWithGetValueException(ExtendedTypeSystemException e, out bool shouldThrow)
        {
            GettingValueExceptionEventArgs eventArgs = new GettingValueExceptionEventArgs(e);
            if (GettingValueException != null)
            {
                GettingValueException.SafeInvoke(this, eventArgs);
                PSObjectTypeDescriptor.typeDescriptor.WriteLine(
                    "GettingValueException event has been triggered resulting in ValueReplacement:\"{0}\".",
                    eventArgs.ValueReplacement);
            }

            shouldThrow = eventArgs.ShouldThrow;
            return eventArgs.ValueReplacement;
        }

        
        /// <param name="component">The component with the property value that is to be set.</param>
        /// <param name="value">The new value.</param>
        /// <exception cref="ExtendedTypeSystemException">
        /// If the property has not been found in the component or an exception has
        /// been thrown when setting the value of the property.
        /// This Exception will only be thrown if there is no event handler for the SettingValueException
        /// event of the <see cref="PSObjectTypeDescriptor"/> that created this <see cref="PSObjectPropertyDescriptor"/>.
        /// If there is an event handler, it can prevent this exception from being thrown, by changing
        /// the ShouldThrow property of <see cref="SettingValueExceptionEventArgs"/>
        /// from its default value of true to false.
        /// </exception>
        /// <exception cref="PSArgumentNullException">If <paramref name="component"/> is null.</exception>
        /// <exception cref="PSArgumentException">If <paramref name="component"/> is not an
        /// <see cref="PSObject"/> or an <see cref="PSObjectTypeDescriptor"/>.
        /// </exception>
        public override void SetValue(object component, object value)
        {
            if (component == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(component));
            }

            PSObject mshObj = GetComponentPSObject(component);
            try
            {
                PSPropertyInfo property = mshObj.Properties[this.Name] as PSPropertyInfo;
                if (property == null)
                {
                    PSObjectTypeDescriptor.typeDescriptor.WriteLine("Could not find property \"{0}\" to set its value.", this.Name);
                    ExtendedTypeSystemException e = new ExtendedTypeSystemException("PropertyNotFoundInPropertyDescriptorSetValue",
                        null,
                        ExtendedTypeSystem.PropertyNotFoundInTypeDescriptor, this.Name);
                    bool shouldThrow;
                    DealWithSetValueException(e, out shouldThrow);
                    if (shouldThrow)
                    {
                        throw e;
                    }

                    return;
                }

                property.Value = value;
            }
            catch (ExtendedTypeSystemException e)
            {
                PSObjectTypeDescriptor.typeDescriptor.WriteLine("Exception setting the value of the property \"{0}\": \"{1}\".", this.Name, e.Message);
                bool shouldThrow;
                DealWithSetValueException(e, out shouldThrow);
                if (shouldThrow)
                {
                    throw;
                }
            }

            OnValueChanged(component, EventArgs.Empty);
        }

        private void DealWithSetValueException(ExtendedTypeSystemException e, out bool shouldThrow)
        {
            SettingValueExceptionEventArgs eventArgs = new SettingValueExceptionEventArgs(e);
            if (SettingValueException != null)
            {
                SettingValueException.SafeInvoke(this, eventArgs);
                PSObjectTypeDescriptor.typeDescriptor.WriteLine(
                    "SettingValueException event has been triggered resulting in ShouldThrow:\"{0}\".",
                    eventArgs.ShouldThrow);
            }

            shouldThrow = eventArgs.ShouldThrow;
            return;
        }
    }

    
    public class PSObjectTypeDescriptor : CustomTypeDescriptor
    {
        internal static readonly PSTraceSource typeDescriptor = PSTraceSource.GetTracer("TypeDescriptor", "Traces the behavior of PSObjectTypeDescriptor, PSObjectTypeDescriptionProvider and PSObjectPropertyDescriptor.", false);

        
        /// <remarks>
        /// The ShouldThrow property of the <see cref="SettingValueExceptionEventArgs"/> allows
        /// subscribers to prevent the exception from being thrown.
        /// </remarks>
        public event EventHandler<SettingValueExceptionEventArgs> SettingValueException;

        
        /// <remarks>
        /// The ShouldThrow property of the <see cref="GettingValueExceptionEventArgs"/> allows
        /// subscribers to prevent the exception from being thrown.
        /// </remarks>
        public event EventHandler<GettingValueExceptionEventArgs> GettingValueException;

        
        /// <param name="instance">The <see cref="PSObject"/> this class retrieves property information from.</param>
        public PSObjectTypeDescriptor(PSObject instance)
        {
            Instance = instance;
        }

        
        public PSObject Instance { get; }

        private void CheckAndAddProperty(PSPropertyInfo propertyInfo, Attribute[] attributes, ref PropertyDescriptorCollection returnValue)
        {
            using (typeDescriptor.TraceScope("Checking property \"{0}\".", propertyInfo.Name))
            {
                // WriteOnly properties are not returned in TypeDescriptor.GetProperties, so we do the same.
                if (!propertyInfo.IsGettable)
                {
                    typeDescriptor.WriteLine("Property \"{0}\" is write-only so it has been skipped.", propertyInfo.Name);
                    return;
                }

                AttributeCollection propertyAttributes = null;
                Type propertyType = typeof(object);
                if (attributes != null && attributes.Length != 0)
                {
                    PSProperty property = propertyInfo as PSProperty;
                    if (property != null)
                    {
                        DotNetAdapter.PropertyCacheEntry propertyEntry = property.adapterData as DotNetAdapter.PropertyCacheEntry;
                        if (propertyEntry == null)
                        {
                            typeDescriptor.WriteLine("Skipping attribute check for property \"{0}\" because it is an adapted property (not a .NET property).", property.Name);
                        }
                        else if (property.isDeserialized)
                        {
                            // At the moment we are not serializing attributes, so we can skip
                            // the attribute check if the property is deserialized.
                            typeDescriptor.WriteLine("Skipping attribute check for property \"{0}\" because it has been deserialized.", property.Name);
                        }
                        else
                        {
                            propertyType = propertyEntry.propertyType;
                            propertyAttributes = propertyEntry.Attributes;
                            foreach (Attribute attribute in attributes)
                            {
                                if (!propertyAttributes.Contains(attribute))
                                {
                                    typeDescriptor.WriteLine("Property \"{0}\" does not contain attribute \"{1}\" so it has been skipped.", property.Name, attribute);
                                    return;
                                }
                            }
                        }
                    }
                }

                propertyAttributes ??= new AttributeCollection();

                typeDescriptor.WriteLine("Adding property \"{0}\".", propertyInfo.Name);

                PSObjectPropertyDescriptor propertyDescriptor =
                    new PSObjectPropertyDescriptor(propertyInfo.Name, propertyType, !propertyInfo.IsSettable, propertyAttributes);

                propertyDescriptor.SettingValueException += this.SettingValueException;
                propertyDescriptor.GettingValueException += this.GettingValueException;

                returnValue.Add(propertyDescriptor);
            }
        }

        
        /// <returns>A PropertyDescriptorCollection containing the property descriptions for the <see cref="PSObject"/> represented by this type descriptor.</returns>
        public override PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(null);
        }

        
        /// <param name="attributes">An array of attributes to use as a filter. This can be a null reference (Nothing in Visual Basic).</param>
        /// <returns>A PropertyDescriptorCollection containing the property descriptions for the <see cref="PSObject"/> represented by this type descriptor.</returns>
        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            using (typeDescriptor.TraceScope("Getting properties."))
            {
                PropertyDescriptorCollection returnValue = new PropertyDescriptorCollection(null);
                if (Instance == null)
                {
                    return returnValue;
                }

                foreach (PSPropertyInfo property in Instance.Properties)
                {
                    CheckAndAddProperty(property, attributes, ref returnValue);
                }

                return returnValue;
            }
        }

        
        /// <param name="obj">The Object to compare with the current Object.</param>
        /// <returns>True if the Instance property of <paramref name="obj"/> is equal to the current Instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PSObjectTypeDescriptor other))
            {
                return false;
            }

            if (this.Instance == null || other.Instance == null)
            {
                return ReferenceEquals(this, other);
            }

            return other.Instance.Equals(this.Instance);
        }

        
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            if (this.Instance == null)
            {
                return base.GetHashCode();
            }

            return this.Instance.GetHashCode();
        }

        
        /// <returns>An <see cref="PSObjectPropertyDescriptor"/> that represents the default property for this object, or a null reference (Nothing in Visual Basic) if this object does not have properties.</returns>
        public override PropertyDescriptor GetDefaultProperty()
        {
            if (this.Instance == null)
            {
                return null;
            }

            string defaultProperty = null;
            PSMemberSet standardMembers = this.Instance.PSStandardMembers;
            if (standardMembers != null)
            {
                PSNoteProperty note = standardMembers.Properties[TypeTable.DefaultDisplayProperty] as PSNoteProperty;
                if (note != null)
                {
                    defaultProperty = note.Value as string;
                }
            }

            if (defaultProperty == null)
            {
                object[] defaultPropertyAttributes = this.Instance.BaseObject.GetType().GetCustomAttributes(typeof(DefaultPropertyAttribute), true);
                if (defaultPropertyAttributes.Length == 1)
                {
                    DefaultPropertyAttribute defaultPropertyAttribute = defaultPropertyAttributes[0] as DefaultPropertyAttribute;
                    if (defaultPropertyAttribute != null)
                    {
                        defaultProperty = defaultPropertyAttribute.Name;
                    }
                }
            }

            PropertyDescriptorCollection properties = this.GetProperties();

            if (defaultProperty != null)
            {
                // There is a defaultProperty, but let's check if it is actually one of the properties we are
                // returning in GetProperties
                foreach (PropertyDescriptor descriptor in properties)
                {
                    if (string.Equals(descriptor.Name, defaultProperty, StringComparison.OrdinalIgnoreCase))
                    {
                        return descriptor;
                    }
                }
            }

            return null;
        }

        
        /// <returns>A <see cref="TypeConverter"/> that is the converter for this object, or a null reference (Nothing in Visual Basic) if there is no <see cref="TypeConverter"/> for this object.</returns>
        public override TypeConverter GetConverter()
        {
            if (this.Instance == null)
            {
                // If we return null, some controls will have an exception saying that this
                // GetConverter returned an illegal value
                return new TypeConverter();
            }

            object baseObject = this.Instance.BaseObject;
            TypeConverter retValue = LanguagePrimitives.GetConverter(baseObject.GetType(), null) as TypeConverter ??
                                     TypeDescriptor.GetConverter(baseObject);
            return retValue;
        }

        
        /// <param name="pd">A <see cref="PropertyDescriptor"/> that represents the property whose owner is to be found.</param>
        /// <returns>An object that represents the owner of the specified property.</returns>
        public override object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this.Instance;
        }

        #region Overrides Forwarded To BaseObject

        #region ReadMe
        // This region contains methods implemented like:
        //    TypeDescriptor.OverrideName(this.Instance.BaseObject)
        // They serve the purpose of exposing Attributes and other information from the BaseObject
        // of an PSObject, since the PSObject itself does not have the concept of class (or member)
        // attributes.
        // The calls are not recursive because the BaseObject was implemented so it is never
        // another PSObject. ImmediateBaseObject or PSObject.Base could cause the call to be
        // recursive in the case of an object like "new PSObject(new PSObject())".
        // Even if we used ImmediateBaseObject, the recursion would be finite since we would
        // keep getting an ImmediatebaseObject until it ceased to be an PSObject.
        #endregion ReadMe

        
        /// <returns>An <see cref="EventDescriptor"/> that represents the default event for this object, or a null reference (Nothing in Visual Basic) if this object does not have events.</returns>
        public override EventDescriptor GetDefaultEvent()
        {
            if (this.Instance == null)
            {
                return null;
            }

            return TypeDescriptor.GetDefaultEvent(this.Instance.BaseObject);
        }

        
        /// <returns>An <see cref="EventDescriptorCollection"/> that represents the events for this component instance.</returns>
        public override EventDescriptorCollection GetEvents()
        {
            if (this.Instance == null)
            {
                return new EventDescriptorCollection(null);
            }

            return TypeDescriptor.GetEvents(this.Instance.BaseObject);
        }

        
        /// <param name="attributes">An array of type <see cref="Attribute"/> that is used as a filter.</param>
        /// <returns>An <see cref="EventDescriptorCollection"/> that represents the events for this component instance that match the given set of attributes.</returns>
        public override EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            if (this.Instance == null)
            {
                return null;
            }

            return TypeDescriptor.GetEvents(this.Instance.BaseObject, attributes);
        }

        
        /// <returns>An <see cref="AttributeCollection"/> with the attributes for this object.</returns>
        public override AttributeCollection GetAttributes()
        {
            if (this.Instance == null)
            {
                return new AttributeCollection();
            }

            return TypeDescriptor.GetAttributes(this.Instance.BaseObject);
        }

        
        /// <returns>The class name of the object, or a null reference (Nothing in Visual Basic) if the class does not have a name.</returns>
        public override string GetClassName()
        {
            if (this.Instance == null)
            {
                return null;
            }

            return TypeDescriptor.GetClassName(this.Instance.BaseObject);
        }

        
        /// <returns>The name of the object, or a null reference (Nothing in Visual Basic) if object does not have a name.</returns>
        public override string GetComponentName()
        {
            if (this.Instance == null)
            {
                return null;
            }

            return TypeDescriptor.GetComponentName(this.Instance.BaseObject);
        }

        
        /// <param name="editorBaseType">A <see cref="Type"/> that represents the editor for this object.</param>
        /// <returns>An object of the specified type that is the editor for this object, or a null reference (Nothing in Visual Basic) if the editor cannot be found.</returns>
        public override object GetEditor(Type editorBaseType)
        {
            if (this.Instance == null)
            {
                return null;
            }

            return TypeDescriptor.GetEditor(this.Instance.BaseObject, editorBaseType);
        }
        #endregion Forwarded To BaseObject
    }

    
    public class PSObjectTypeDescriptionProvider : TypeDescriptionProvider
    {
        
        /// <remarks>
        /// The ShouldThrow property of the <see cref="SettingValueExceptionEventArgs"/> allows
        /// subscribers to prevent the exception from being thrown.
        /// </remarks>
        public event EventHandler<SettingValueExceptionEventArgs> SettingValueException;

        
        /// <remarks>
        /// The ShouldThrow property of the <see cref="GettingValueExceptionEventArgs"/> allows
        /// subscribers to prevent the exception from being thrown.
        /// </remarks>
        public event EventHandler<GettingValueExceptionEventArgs> GettingValueException;

        
        public PSObjectTypeDescriptionProvider()
        {
        }

        
        /// <param name="objectType">The type of object for which to retrieve the type descriptor. If this parameter is not noll and is not the <see cref="PSObject"/>, the return of this method will be null.</param>
        /// <param name="instance">An instance of the type. If instance is null or has a type other than <see cref="PSObject"/>, this method returns null.</param>
        /// <returns>An <see cref="ICustomTypeDescriptor"/> that can provide property information for the
        /// type <see cref="PSObject"/>, or null if <paramref name="objectType"/> is not null,
        /// but has a type other than <see cref="PSObject"/>.</returns>
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            PSObject mshObj = instance as PSObject;

            #region ReadMe
            // Instance can be null, in a couple of circumstances:
            //    1) In one of the many calls to this method caused by setting the SelectedObject
            // property of a PropertyGrid.
            //    2) If, by mistake, an object[] or Collection<PSObject> is used instead of an ArrayList
            // to set the DataSource property of a DataGrid or DatagridView.
            //
            // It would be nice to throw an exception for the case 2) instructing the user to use
            // an ArrayList, but since we have case 1) and maybe others we haven't found we return
            // an PSObjectTypeDescriptor(null). PSObjectTypeDescriptor's GetProperties
            // checks for null instance and returns an empty property collection.
            // All other overrides also check for null and return some default result.
            // Case 1), which is using a PropertyGrid seems to be unaffected by these results returned
            // by PSObjectTypeDescriptor overrides when the Instance is null, so we must conclude
            // that the TypeDescriptor returned by that call where instance is null is not used
            // for anything meaningful. That null instance PSObjectTypeDescriptor is only one
            // of the many PSObjectTypeDescriptor's returned by this method in a PropertyGrid use.
            // Some of the other calls to this method are passing a valid instance and the objects
            // returned by these calls seem to be the ones used for meaningful calls in the PropertyGrid.
            //
            // It might sound strange that we are not verifying the type of objectType or of instance
            // to be PSObject, but in this PropertyGrid use that passes a null instance (case 1), if
            // we return null we have an exception flagging the return as invalid. Since we cannot
            // return null and MSDN has a note saying that we should return null instead of throwing
            // exceptions, the safest behavior seems to be creating this PSObjectTypeDescriptor with
            // null instance.
            #endregion ReadMe

            PSObjectTypeDescriptor typeDescriptor = new PSObjectTypeDescriptor(mshObj);
            typeDescriptor.SettingValueException += this.SettingValueException;
            typeDescriptor.GettingValueException += this.GettingValueException;
            return typeDescriptor;
        }
    }
}
