// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    
    internal sealed class CustomSerialization
    {
        #region constructor
        
        private readonly int _depth;

        
        private readonly XmlWriter _writer;

        
        private readonly bool _notypeinformation;

        
        private CustomInternalSerializer _serializer;

        
        internal CustomSerialization(XmlWriter writer, bool notypeinformation, int depth)
        {
            if (writer == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(writer));
            }

            if (depth < 1)
            {
                throw PSTraceSource.NewArgumentException(nameof(writer), Serialization.DepthOfOneRequired);
            }

            _depth = depth;
            _writer = writer;
            _notypeinformation = notypeinformation;
            _serializer = null;
        }

        
        public static int MshDefaultSerializationDepth { get; } = 1;

        
        internal CustomSerialization(XmlWriter writer, bool notypeinformation)
            : this(writer, notypeinformation, MshDefaultSerializationDepth)
        {
        }

        #endregion constructor

        #region public methods

        private bool _firstCall = true;

        
        internal void Serialize(object source)
        {
            // Write the root element tag before writing first object.
            if (_firstCall)
            {
                _firstCall = false;
                Start();
            }

            _serializer = new CustomInternalSerializer
                               (
                                   _writer,
                                   _notypeinformation,
                                   true
                                );
            _serializer.WriteOneObject(source, null, _depth);
            _serializer = null;
        }

        
        internal void SerializeAsStream(object source)
        {
            _serializer = new CustomInternalSerializer
                               (
                                   _writer,
                                   _notypeinformation,
                                   true
                                );
            _serializer.WriteOneObject(source, null, _depth);
            _serializer = null;
        }

        
        private void Start()
        {
            CustomInternalSerializer.WriteStartElement(_writer, CustomSerializationStrings.RootElementTag);
        }

        
        internal void Done()
        {
            if (_firstCall)
            {
                _firstCall = false;
                Start();
            }

            _writer.WriteEndElement();
            _writer.Flush();
        }

        
        internal void DoneAsStream()
        {
            _writer.Flush();
        }

        internal void Stop()
        {
            CustomInternalSerializer serializer = _serializer;
            serializer?.Stop();
        }

        #endregion
    }

    
    internal sealed class CustomInternalSerializer
    {
        #region constructor

        
        private readonly XmlWriter _writer;

        
        private bool _firstcall;

        
        private readonly bool _notypeinformation;

        
        private bool _firstobjectcall = true;

        
        internal CustomInternalSerializer(XmlWriter writer, bool notypeinformation, bool isfirstcallforObject)
        {
            Dbg.Assert(writer != null, "caller should validate the parameter");

            _writer = writer;
            _notypeinformation = notypeinformation;
            _firstcall = isfirstcallforObject;
        }

        #endregion

        #region Stopping

        private bool _isStopping = false;

        
        internal void Stop()
        {
            _isStopping = true;
        }

        private void CheckIfStopping()
        {
            if (_isStopping)
            {
                throw PSTraceSource.NewInvalidOperationException(Serialization.Stopping);
            }
        }

        #endregion Stopping

        
        internal void WriteOneObject(object source, string property, int depth)
        {
            Dbg.Assert(depth >= 0, "depth should always be greater or equal to zero");

            CheckIfStopping();

            if (source == null)
            {
                WriteNull(property);
                return;
            }

            if (HandlePrimitiveKnownType(source, property))
            {
                return;
            }

            if (HandlePrimitiveKnownTypePSObject(source, property, depth))
            {
                return;
            }

            // Note: We donot use containers in depth calculation. i.e even if the
            // current depth is zero, we serialize the container. All contained items will
            // get serialized with depth zero.
            if (HandleKnownContainerTypes(source, property, depth))
            {
                return;
            }

            PSObject mshSource = PSObject.AsPSObject(source);
            // If depth is zero, complex type should be serialized as string.
            if (depth == 0 || SerializeAsString(mshSource))
            {
                HandlePSObjectAsString(mshSource, property, depth);
                return;
            }

            HandleComplexTypePSObject(mshSource, property, depth);
            return;
        }

        
        private bool HandlePrimitiveKnownType(object source, string property)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            // Check if source is of primitive known type
            TypeSerializationInfo pktInfo = KnownTypes.GetTypeSerializationInfo(source.GetType());
            if (pktInfo != null)
            {
                WriteOnePrimitiveKnownType(_writer, property, source, pktInfo);
                return true;
            }

            return false;
        }

        
        private bool HandlePrimitiveKnownTypePSObject(object source, string property, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            bool sourceHandled = false;
            if (source is PSObject moSource && !moSource.ImmediateBaseObjectIsEmpty)
            {
                // Check if baseObject is primitive known type
                object baseObject = moSource.ImmediateBaseObject;
                TypeSerializationInfo pktInfo = KnownTypes.GetTypeSerializationInfo(baseObject.GetType());
                if (pktInfo != null)
                {
                    WriteOnePrimitiveKnownType(_writer, property, baseObject, pktInfo);
                    sourceHandled = true;
                }
            }

            return sourceHandled;
        }

        private bool HandleKnownContainerTypes(object source, string property, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            ContainerType ct = ContainerType.None;
            PSObject mshSource = source as PSObject;
            IEnumerable enumerable = null;
            IDictionary dictionary = null;

            // If passed in object is PSObject with no baseobject, return false.
            if (mshSource != null && mshSource.ImmediateBaseObjectIsEmpty)
            {
                return false;
            }

            // Check if source (or baseobject in mshSource) is known container type
            GetKnownContainerTypeInfo(mshSource != null ? mshSource.ImmediateBaseObject : source, out ct,
                                      out dictionary, out enumerable);

            if (ct == ContainerType.None)
                return false;

            WriteStartOfPSObject(mshSource ?? PSObject.AsPSObject(source), property, true);
            switch (ct)
            {
                case ContainerType.Dictionary:
                    {
                        WriteDictionary(dictionary, depth);
                    }

                    break;
                case ContainerType.Stack:
                case ContainerType.Queue:
                case ContainerType.List:
                case ContainerType.Enumerable:
                    {
                        WriteEnumerable(enumerable, depth);
                    }

                    break;
                default:
                    {
                        Dbg.Assert(false, "All containers should be handled in the switch");
                    }

                    break;
            }

            // An object which is original enumerable becomes an PSObject
            // with arraylist on deserialization. So on roundtrip it will show up
            // as List.
            // We serialize properties of enumerable and on deserialization mark the object
            // as Deserialized. So if object is marked deserialized, we should write properties.
            // Note: we do not serialize the properties of IEnumerable if depth is zero.
            if (depth != 0 && (ct == ContainerType.Enumerable || (mshSource != null && mshSource.IsDeserialized)))
            {
                // Note:Depth is the depth for serialization of baseObject.
                // Depth for serialization of each property is one less.
                WritePSObjectProperties(PSObject.AsPSObject(source), depth);
            }

            // If source is PSObject, serialize notes
            if (mshSource != null)
            {
                // Serialize instanceMembers
                PSMemberInfoCollection<PSMemberInfo> instanceMembers = mshSource.InstanceMembers;
                if (instanceMembers != null)
                {
                    WriteMemberInfoCollection(instanceMembers, depth, true);
                }
            }

            _writer.WriteEndElement();

            return true;
        }

        
        private static void GetKnownContainerTypeInfo(
            object source, out ContainerType ct, out IDictionary dictionary, out IEnumerable enumerable)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            ct = ContainerType.None;
            dictionary = null;
            enumerable = null;

            dictionary = source as IDictionary;
            if (dictionary != null)
            {
                ct = ContainerType.Dictionary;
                return;
            }

            if (source is Stack)
            {
                ct = ContainerType.Stack;
                enumerable = LanguagePrimitives.GetEnumerable(source);
                Dbg.Assert(enumerable != null, "Stack is enumerable");
            }
            else if (source is Queue)
            {
                ct = ContainerType.Queue;
                enumerable = LanguagePrimitives.GetEnumerable(source);
                Dbg.Assert(enumerable != null, "Queue is enumerable");
            }
            else if (source is IList)
            {
                ct = ContainerType.List;
                enumerable = LanguagePrimitives.GetEnumerable(source);
                Dbg.Assert(enumerable != null, "IList is enumerable");
            }
            else
            {
                Type gt = source.GetType();
                if (gt.GetTypeInfo().IsGenericType)
                {
                    if (DerivesFromGenericType(gt, typeof(Stack<>)))
                    {
                        ct = ContainerType.Stack;
                        enumerable = LanguagePrimitives.GetEnumerable(source);
                        Dbg.Assert(enumerable != null, "Stack is enumerable");
                    }
                    else if (DerivesFromGenericType(gt, typeof(Queue<>)))
                    {
                        ct = ContainerType.Queue;
                        enumerable = LanguagePrimitives.GetEnumerable(source);
                        Dbg.Assert(enumerable != null, "Queue is enumerable");
                    }
                    else if (DerivesFromGenericType(gt, typeof(List<>)))
                    {
                        ct = ContainerType.List;
                        enumerable = LanguagePrimitives.GetEnumerable(source);
                        Dbg.Assert(enumerable != null, "Queue is enumerable");
                    }
                }
            }

            // Check if type is IEnumerable
            if (ct == ContainerType.None)
            {
                enumerable = LanguagePrimitives.GetEnumerable(source);
                if (enumerable != null)
                {
                    ct = ContainerType.Enumerable;
                }
            }
        }

        
        private static bool DerivesFromGenericType(Type derived, Type baseType)
        {
            Dbg.Assert(derived != null, "caller should validate the parameter");
            Dbg.Assert(baseType != null, "caller should validate the parameter");
            while (derived != null)
            {
                if (derived.GetTypeInfo().IsGenericType)
                    derived = derived.GetGenericTypeDefinition();

                if (derived == baseType)
                {
                    return true;
                }

                derived = derived.GetTypeInfo().BaseType;
            }

            return false;
        }

        #region Write PSObject

        
        private void WritePrimitiveTypePSObjectWithNotes(
            PSObject source,
            object primitive,
            TypeSerializationInfo pktInfo,
            string property,
            int depth)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            // Write start of PSObject. Since baseobject is primitive known
            // type, we do not need TypeName information.
            WriteStartOfPSObject(source, property, source.ToStringFromDeserialization != null);

            if (pktInfo != null)
            {
                WriteOnePrimitiveKnownType(_writer, null, primitive, pktInfo);
            }

            // Serialize instanceMembers
            PSMemberInfoCollection<PSMemberInfo> instanceMembers = source.InstanceMembers;
            if (instanceMembers != null)
            {
                WriteMemberInfoCollection(instanceMembers, depth, true);
            }

            _writer.WriteEndElement();
        }

        private void HandleComplexTypePSObject(PSObject source, string property, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the parameter");

            WriteStartOfPSObject(source, property, true);

            // Figure out what kind of object we are dealing with
            bool isEnum = false;
            bool isPSObject = false;

            if (!source.ImmediateBaseObjectIsEmpty)
            {
                isEnum = source.ImmediateBaseObject is Enum;
                isPSObject = source.ImmediateBaseObject is PSObject;
            }

            if (isEnum)
            {
                object baseObject = source.ImmediateBaseObject;
                foreach (PSPropertyInfo prop in source.Properties)
                {
                    WriteOneObject(System.Convert.ChangeType(baseObject, Enum.GetUnderlyingType(baseObject.GetType()), System.Globalization.CultureInfo.InvariantCulture), prop.Name, depth);
                }
            }
            else if (isPSObject)
            {
                if (_firstobjectcall)
                {
                    _firstobjectcall = false;
                    WritePSObjectProperties(source, depth);
                }
                else
                {
                    WriteOneObject(source.ImmediateBaseObject, null, depth);
                }
            }
            else
            {
                WritePSObjectProperties(source, depth);
            }

            _writer.WriteEndElement();
        }

        
        private void WriteStartOfPSObject(
            PSObject mshObject,
            string property,
            bool writeTNH)
        {
            Dbg.Assert(mshObject != null, "caller should validate the parameter");

            if (property != null)
            {
                WriteStartElement(_writer, CustomSerializationStrings.Properties);
                WriteAttribute(_writer, CustomSerializationStrings.NameAttribute, property);
            }
            else
            {
                if (_firstcall)
                {
                    WriteStartElement(_writer, CustomSerializationStrings.PSObjectTag);
                    _firstcall = false;
                }
                else
                {
                    WriteStartElement(_writer, CustomSerializationStrings.Properties);
                }
            }

            object baseObject = mshObject.BaseObject;
            if (!_notypeinformation)
                WriteAttribute(_writer, CustomSerializationStrings.TypeAttribute, baseObject.GetType().ToString());
        }

        #region membersets

        
        private static bool PSObjectHasNotes(PSObject source)
        {
            if (source.InstanceMembers != null && source.InstanceMembers.Count > 0)
            {
                return true;
            }

            return false;
        }

        
        private void WriteMemberInfoCollection(
            PSMemberInfoCollection<PSMemberInfo> me, int depth, bool writeEnclosingMemberSetElementTag)
        {
            Dbg.Assert(me != null, "caller should validate the parameter");

            foreach (PSMemberInfo info in me)
            {
                if (!info.ShouldSerialize)
                {
                    continue;
                }

                if (!(info is PSPropertyInfo property))
                {
                    continue;
                }

                WriteStartElement(_writer, CustomSerializationStrings.Properties);
                WriteAttribute(_writer, CustomSerializationStrings.NameAttribute, info.Name);
                if (!_notypeinformation)
                    WriteAttribute(_writer, CustomSerializationStrings.TypeAttribute, info.GetType().ToString());
                _writer.WriteString(property.Value.ToString());
                _writer.WriteEndElement();
            }
        }

        #endregion membersets

        #region properties

        
        private void WritePSObjectProperties(PSObject source, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the information");

            depth = GetDepthOfSerialization(source, depth);

            // Depth available for each property is one less
            --depth;
            Dbg.Assert(depth >= 0, "depth should be greater or equal to zero");
            if (source.GetSerializationMethod(null) == SerializationMethod.SpecificProperties)
            {
                PSMemberInfoInternalCollection<PSPropertyInfo> specificProperties = new();
                foreach (string propertyName in source.GetSpecificPropertiesToSerialize(null))
                {
                    PSPropertyInfo property = source.Properties[propertyName];
                    if (property != null)
                    {
                        specificProperties.Add(property);
                    }
                }

                SerializeProperties(specificProperties, CustomSerializationStrings.Properties, depth);
                return;
            }

            foreach (PSPropertyInfo prop in source.Properties)
            {
                Dbg.Assert(prop != null, "propertyCollection should only have member of type PSProperty");
                object value = AutomationNull.Value;
                // PSObject throws GetValueException if it cannot
                // get value for a property.
                try
                {
                    value = prop.Value;
                }
                catch (GetValueException)
                {
                    WritePropertyWithNullValue(_writer, prop, depth);
                    continue;
                }
                // Write the property
                if (value == null)
                {
                    WritePropertyWithNullValue(_writer, prop, depth);
                }
                else
                {
                    WriteOneObject(value, prop.Name, depth);
                }
            }
        }

        
        private void SerializeProperties(
            PSMemberInfoInternalCollection<PSPropertyInfo> propertyCollection, string name, int depth)
        {
            Dbg.Assert(propertyCollection != null, "caller should validate the parameter");
            if (propertyCollection.Count == 0)
                return;

            foreach (PSMemberInfo info in propertyCollection)
            {
                PSPropertyInfo prop = info as PSPropertyInfo;

                Dbg.Assert(prop != null, "propertyCollection should only have member of type PSProperty");

                object value = AutomationNull.Value;
                // PSObject throws GetValueException if it cannot
                // get value for a property.
                try
                {
                    value = prop.Value;
                }
                catch (GetValueException)
                {
                    continue;
                }
                // Write the property
                WriteOneObject(value, prop.Name, depth);
            }
        }

        #endregion base properties

        #endregion WritePSObject

        #region enumerable and dictionary

        
        private void WriteEnumerable(IEnumerable enumerable, int depth)
        {
            Dbg.Assert(enumerable != null, "caller should validate the parameter");

            IEnumerator enumerator = null;
            try
            {
                enumerator = enumerable.GetEnumerator();
                enumerator.Reset();
            }
            catch (Exception)
            {
                enumerator = null;
            }

            // AD has incorrect implementation of IEnumerable where they returned null
            // for GetEnumerator instead of empty enumerator
            if (enumerator != null)
            {
                while (true)
                {
                    object item = null;
                    try
                    {
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                        else
                        {
                            item = enumerator.Current;
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    WriteOneObject(item, null, depth);
                }
            }
        }

        
        private void WriteDictionary(IDictionary dictionary, int depth)
        {
            IDictionaryEnumerator dictionaryEnum = null;
            try
            {
                dictionaryEnum = (IDictionaryEnumerator)dictionary.GetEnumerator();
            }
            catch (Exception)
            {
            }

            if (dictionaryEnum != null)
            {
                while (dictionaryEnum.MoveNext())
                {
                    // Write Key
                    WriteOneObject(dictionaryEnum.Key, CustomSerializationStrings.DictionaryKey, depth);
                    // Write Value
                    WriteOneObject(dictionaryEnum.Value, CustomSerializationStrings.DictionaryValue, depth);
                }
            }
        }

        #endregion enumerable and dictionary

        #region serialize as string

        private void HandlePSObjectAsString(PSObject source, string property, int depth)
        {
            Dbg.Assert(source != null, "caller should validate the information");

            bool hasNotes = PSObjectHasNotes(source);
            string value = GetStringFromPSObject(source);

            if (value != null)
            {
                TypeSerializationInfo pktInfo = KnownTypes.GetTypeSerializationInfo(value.GetType());
                Dbg.Assert(pktInfo != null, "TypeSerializationInfo should be present for string");
                if (hasNotes)
                {
                    WritePrimitiveTypePSObjectWithNotes(source, value, pktInfo, property, depth);
                }
                else
                {
                    WriteOnePrimitiveKnownType(_writer, property, source.BaseObject, pktInfo);
                }
            }
            else
            {
                if (hasNotes)
                {
                    WritePrimitiveTypePSObjectWithNotes(source, null, null, property, depth);
                }
                else
                {
                    WriteNull(property);
                }
            }
        }

        
        private static string GetStringFromPSObject(PSObject source)
        {
            Dbg.Assert(source != null, "caller should have validated the information");

            // check if we have a well known string serialization source
            PSPropertyInfo serializationProperty = source.GetStringSerializationSource(null);
            string result = null;
            if (serializationProperty != null)
            {
                object val = serializationProperty.Value;
                if (val != null)
                {
                    try
                    {
                        // if we have a string serialization value, return it
                        result = val.ToString();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else
            {
                try
                {
                    // fall back value
                    result = source.ToString();
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        
        private static bool SerializeAsString(PSObject source)
        {
            return source.GetSerializationMethod(null) == SerializationMethod.String;
        }

        #endregion serialize as string

        
        private static int GetDepthOfSerialization(PSObject source, int depth)
        {
            if (source == null)
                return depth;

            // get the depth from the PSObject
            // NOTE: we assume that the depth out of the PSObject is > 0
            // else we consider it not set in types.ps1xml
            int objectLevelDepth = source.GetSerializationDepth(null);
            if (objectLevelDepth <= 0)
            {
                // no override at the type level
                return depth;
            }

            return objectLevelDepth;
        }

        
        private void WriteNull(string property)
        {
            if (property != null)
            {
                WriteStartElement(_writer, CustomSerializationStrings.Properties);
                WriteAttribute(_writer, CustomSerializationStrings.NameAttribute, property);
            }
            else
            {
                if (_firstcall)
                {
                    WriteStartElement(_writer, CustomSerializationStrings.PSObjectTag);
                    _firstcall = false;
                }
                else
                {
                    WriteStartElement(_writer, CustomSerializationStrings.Properties);
                }
            }

            _writer.WriteEndElement();
        }

        #region known type serialization

        private void WritePropertyWithNullValue(
            XmlWriter writer, PSPropertyInfo source, int depth)
        {
            WriteStartElement(writer, CustomSerializationStrings.Properties);
            WriteAttribute(writer, CustomSerializationStrings.NameAttribute, ((PSPropertyInfo)source).Name);
            if (!_notypeinformation)
                WriteAttribute(writer, CustomSerializationStrings.TypeAttribute, ((PSPropertyInfo)source).TypeNameOfValue);
            writer.WriteEndElement();
        }

        private void WriteObjectString(
            XmlWriter writer, string property, object source, TypeSerializationInfo entry)
        {
            if (property != null)
            {
                WriteStartElement(writer, CustomSerializationStrings.Properties);
                WriteAttribute(writer, CustomSerializationStrings.NameAttribute, property);
            }
            else
            {
                if (_firstcall)
                {
                    WriteStartElement(writer, CustomSerializationStrings.PSObjectTag);
                    _firstcall = false;
                }
                else
                {
                    WriteStartElement(writer, CustomSerializationStrings.Properties);
                }
            }

            if (!_notypeinformation)
                WriteAttribute(writer, CustomSerializationStrings.TypeAttribute, source.GetType().ToString());

            writer.WriteString(source.ToString());
            writer.WriteEndElement();
        }

        
        private void WriteOnePrimitiveKnownType(
            XmlWriter writer, string property, object source, TypeSerializationInfo entry)
        {
            WriteObjectString(writer, property, source, entry);
        }

        #endregion known type serialization

        #region misc

        
        internal static void WriteStartElement(XmlWriter writer, string elementTag)
        {
            writer.WriteStartElement(elementTag);
        }

        
        internal static void WriteAttribute(XmlWriter writer, string name, string value)
        {
            writer.WriteAttributeString(name, value);
        }

        #endregion misc
    }
}
