// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.PowerShell.Commands
{
    
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Preferring Json over JSON")]
    public static class JsonObject
    {
        #region HelperTypes

        
        public readonly struct ConvertToJsonContext
        {
            
            public readonly int MaxDepth;

            
            public readonly CancellationToken CancellationToken;

            
            public readonly StringEscapeHandling StringEscapeHandling;

            
            public readonly bool EnumsAsStrings;

            
            public readonly bool CompressOutput;

            
            public readonly PSCmdlet Cmdlet;

            
            public ConvertToJsonContext(int maxDepth, bool enumsAsStrings, bool compressOutput)
                : this(maxDepth, enumsAsStrings, compressOutput, StringEscapeHandling.Default, targetCmdlet: null, CancellationToken.None)
            {
            }

            
            public ConvertToJsonContext(
                int maxDepth,
                bool enumsAsStrings,
                bool compressOutput,
                StringEscapeHandling stringEscapeHandling,
                PSCmdlet targetCmdlet,
                CancellationToken cancellationToken)
            {
                this.MaxDepth = maxDepth;
                this.CancellationToken = cancellationToken;
                this.StringEscapeHandling = stringEscapeHandling;
                this.EnumsAsStrings = enumsAsStrings;
                this.CompressOutput = compressOutput;
                this.Cmdlet = targetCmdlet;
            }
        }

        private sealed class DuplicateMemberHashSet : HashSet<string>
        {
            public DuplicateMemberHashSet(int capacity)
                : base(capacity, StringComparer.OrdinalIgnoreCase)
            {
            }
        }

        #endregion HelperTypes

        #region ConvertFromJson

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Preferring Json over JSON")]
        public static object ConvertFromJson(string input, out ErrorRecord error)
        {
            return ConvertFromJson(input, returnHashtable: false, out error);
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Preferring Json over JSON")]
        public static object ConvertFromJson(string input, bool returnHashtable, out ErrorRecord error)
        {
            return ConvertFromJson(input, returnHashtable, maxDepth: 1024, out error);
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Preferring Json over JSON")]
        public static object ConvertFromJson(string input, bool returnHashtable, int? maxDepth, out ErrorRecord error)
            => ConvertFromJson(input, returnHashtable, maxDepth, jsonDateKind: JsonDateKind.Default, out error);

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Preferring Json over JSON")]
        internal static object ConvertFromJson(string input, bool returnHashtable, int? maxDepth, JsonDateKind jsonDateKind, out ErrorRecord error)
        {
            ArgumentNullException.ThrowIfNull(input);

            DateParseHandling dateParseHandling;
            DateTimeZoneHandling dateTimeZoneHandling;
            switch (jsonDateKind)
            {
                case JsonDateKind.Default:
                    dateParseHandling = DateParseHandling.DateTime;
                    dateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
                    break;

                case JsonDateKind.Local:
                    dateParseHandling = DateParseHandling.DateTime;
                    dateTimeZoneHandling = DateTimeZoneHandling.Local;
                    break;

                case JsonDateKind.Utc:
                    dateParseHandling = DateParseHandling.DateTime;
                    dateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    break;

                case JsonDateKind.Offset:
                    dateParseHandling = DateParseHandling.DateTimeOffset;
                    dateTimeZoneHandling = DateTimeZoneHandling.Unspecified;
                    break;

                case JsonDateKind.String:
                    dateParseHandling = DateParseHandling.None;
                    dateTimeZoneHandling = DateTimeZoneHandling.Unspecified;
                    break;

                default:
                    throw new ArgumentException($"Unknown JsonDateKind value requested '{jsonDateKind}'");
            }

            error = null;
            try
            {
                var obj = JsonConvert.DeserializeObject(
                    input,
                    new JsonSerializerSettings
                    {
                        DateParseHandling = dateParseHandling,
                        DateTimeZoneHandling = dateTimeZoneHandling,

                        // This TypeNameHandling setting is required to be secure.
                        TypeNameHandling = TypeNameHandling.None,
                        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                        MaxDepth = maxDepth
                    });

                switch (obj)
                {
                    case JObject dictionary:
                        // JObject is a IDictionary
                        return returnHashtable
                                   ? PopulateHashTableFromJDictionary(dictionary, out error)
                                   : PopulateFromJDictionary(dictionary, new DuplicateMemberHashSet(dictionary.Count), out error);
                    case JArray list:
                        return returnHashtable
                                   ? PopulateHashTableFromJArray(list, out error)
                                   : PopulateFromJArray(list, out error);
                    default:
                        return obj;
                }
            }
            catch (JsonException je)
            {
                var msg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.JsonDeserializationFailed, je.Message);

                // the same as JavaScriptSerializer does
                throw new ArgumentException(msg, je);
            }
        }

        // This function is a clone of PopulateFromDictionary using JObject as an input.
        private static PSObject PopulateFromJDictionary(JObject entries, DuplicateMemberHashSet memberHashTracker, out ErrorRecord error)
        {
            error = null;
            var result = new PSObject(entries.Count);
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    var errorMsg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.EmptyKeyInJsonString);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "EmptyKeyInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                // Case sensitive duplicates should normally not occur since JsonConvert.DeserializeObject
                // does not throw when encountering duplicates and just uses the last entry.
                if (memberHashTracker.TryGetValue(entry.Key, out var maybePropertyName)
                    && string.Equals(entry.Key, maybePropertyName, StringComparison.Ordinal))
                {
                    var errorMsg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.DuplicateKeysInJsonString, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "DuplicateKeysInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                // Compare case insensitive to tell the user to use the -AsHashTable option instead.
                // This is because PSObject cannot have keys with different casing.
                if (memberHashTracker.TryGetValue(entry.Key, out var propertyName))
                {
                    var errorMsg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.KeysWithDifferentCasingInJsonString, propertyName, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "KeysWithDifferentCasingInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                // Array
                switch (entry.Value)
                {
                    case JArray list:
                        {
                            var listResult = PopulateFromJArray(list, out error);
                            if (error != null)
                            {
                                return null;
                            }

                            result.Properties.Add(new PSNoteProperty(entry.Key, listResult));
                            break;
                        }
                    case JObject dic:
                        {
                            // Dictionary
                            var dicResult = PopulateFromJDictionary(dic, new DuplicateMemberHashSet(dic.Count), out error);
                            if (error != null)
                            {
                                return null;
                            }

                            result.Properties.Add(new PSNoteProperty(entry.Key, dicResult));
                            break;
                        }
                    case JValue value:
                        {
                            result.Properties.Add(new PSNoteProperty(entry.Key, value.Value));
                            break;
                        }
                }

                memberHashTracker.Add(entry.Key);
            }

            return result;
        }

        // This function is a clone of PopulateFromList using JArray as input.
        private static ICollection<object> PopulateFromJArray(JArray list, out ErrorRecord error)
        {
            error = null;
            var result = new object[list.Count];

            for (var index = 0; index < list.Count; index++)
            {
                var element = list[index];
                switch (element)
                {
                    case JArray subList:
                        {
                            // Array
                            var listResult = PopulateFromJArray(subList, out error);
                            if (error != null)
                            {
                                return null;
                            }

                            result[index] = listResult;
                            break;
                        }
                    case JObject dic:
                        {
                            // Dictionary
                            var dicResult = PopulateFromJDictionary(dic, new DuplicateMemberHashSet(dic.Count), out error);
                            if (error != null)
                            {
                                return null;
                            }

                            result[index] = dicResult;
                            break;
                        }
                    case JValue value:
                        {
                            result[index] = value.Value;
                            break;
                        }
                }
            }

            return result;
        }

        // This function is a clone of PopulateFromDictionary using JObject as an input.
        private static Hashtable PopulateHashTableFromJDictionary(JObject entries, out ErrorRecord error)
        {
            error = null;
            OrderedHashtable result = new(entries.Count);
            foreach (var entry in entries)
            {
                // Case sensitive duplicates should normally not occur since JsonConvert.DeserializeObject
                // does not throw when encountering duplicates and just uses the last entry.
                if (result.ContainsKey(entry.Key))
                {
                    string errorMsg = string.Format(CultureInfo.CurrentCulture, WebCmdletStrings.DuplicateKeysInJsonString, entry.Key);
                    error = new ErrorRecord(
                        new InvalidOperationException(errorMsg),
                        "DuplicateKeysInJsonString",
                        ErrorCategory.InvalidOperation,
                        null);
                    return null;
                }

                switch (entry.Value)
                {
                    case JArray list:
                        {
                            // Array
                            var listResult = PopulateHashTableFromJArray(list, out error);
                            if (error != null)
                            {
                                return null;
                            }

                            result.Add(entry.Key, listResult);
                            break;
                        }
                    case JObject dic:
                        {
                            // Dictionary
                            var dicResult = PopulateHashTableFromJDictionary(dic, out error);
                            if (error != null)
                            {
                                return null;
                            }

                            result.Add(entry.Key, dicResult);
                            break;
                        }
                    case JValue value:
                        {
                            result.Add(entry.Key, value.Value);
                            break;
                        }
                }
            }

            return result;
        }

        // This function is a clone of PopulateFromList using JArray as input.
        private static ICollection<object> PopulateHashTableFromJArray(JArray list, out ErrorRecord error)
        {
            error = null;
            var result = new object[list.Count];

            for (var index = 0; index < list.Count; index++)
            {
                var element = list[index];

                switch (element)
                {
                    case JArray array:
                        {
                            // Array
                            var listResult = PopulateHashTableFromJArray(array, out error);
                            if (error != null)
                            {
                                return null;
                            }

                            result[index] = listResult;
                            break;
                        }
                    case JObject dic:
                        {
                            // Dictionary
                            var dicResult = PopulateHashTableFromJDictionary(dic, out error);
                            if (error != null)
                            {
                                return null;
                            }

                            result[index] = dicResult;
                            break;
                        }
                    case JValue value:
                        {
                            result[index] = value.Value;
                            break;
                        }
                }
            }

            return result;
        }

        #endregion ConvertFromJson

        #region ConvertToJson

        
        public static string ConvertToJson(object objectToProcess, in ConvertToJsonContext context)
        {
            try
            {
                // Pre-process the object so that it serializes the same, except that properties whose
                // values cannot be evaluated are treated as having the value null.
                _maxDepthWarningWritten = false;
                object preprocessedObject = ProcessValue(objectToProcess, currentDepth: 0, in context);
                var jsonSettings = new JsonSerializerSettings
                {
                    // This TypeNameHandling setting is required to be secure.
                    TypeNameHandling = TypeNameHandling.None,
                    MaxDepth = 1024,
                    StringEscapeHandling = context.StringEscapeHandling
                };

                if (context.EnumsAsStrings)
                {
                    jsonSettings.Converters.Add(new StringEnumConverter());
                }

                if (!context.CompressOutput)
                {
                    jsonSettings.Formatting = Formatting.Indented;
                }

                return JsonConvert.SerializeObject(preprocessedObject, jsonSettings);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private static bool _maxDepthWarningWritten;

        
        private static object ProcessValue(object obj, int currentDepth, in ConvertToJsonContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (LanguagePrimitives.IsNull(obj))
            {
                return null;
            }

            PSObject pso = obj as PSObject;

            if (pso != null)
            {
                obj = pso.BaseObject;
            }

            object rv = obj;
            bool isPurePSObj = false;
            bool isCustomObj = false;

            if (obj == NullString.Value
                || obj == DBNull.Value)
            {
                rv = null;
            }
            else if (obj is string
                    || obj is char
                    || obj is bool
                    || obj is DateTime
                    || obj is DateTimeOffset
                    || obj is Guid
                    || obj is Uri
                    || obj is double
                    || obj is float
                    || obj is decimal
                    || obj is BigInteger)
            {
                rv = obj;
            }
            else if (obj is Newtonsoft.Json.Linq.JObject jObject)
            {
                rv = jObject.ToObject<Dictionary<object, object>>();
            }
            else
            {
                Type t = obj.GetType();

                if (t.IsPrimitive || (t.IsEnum && ExperimentalFeature.IsEnabled(ExperimentalFeature.PSSerializeJSONLongEnumAsNumber)))
                {
                    rv = obj;
                }
                else if (t.IsEnum)
                {
                    // Win8:378368 Enums based on System.Int64 or System.UInt64 are not JSON-serializable
                    // because JavaScript does not support the necessary precision.
                    Type enumUnderlyingType = Enum.GetUnderlyingType(obj.GetType());
                    if (enumUnderlyingType.Equals(typeof(long)) || enumUnderlyingType.Equals(typeof(ulong)))
                    {
                        rv = obj.ToString();
                    }
                    else
                    {
                        rv = obj;
                    }
                }
                else
                {
                    if (currentDepth > context.MaxDepth)
                    {
                        if (!_maxDepthWarningWritten && context.Cmdlet != null)
                        {
                            _maxDepthWarningWritten = true;
                            string maxDepthMessage = string.Format(
                                CultureInfo.CurrentCulture,
                                WebCmdletStrings.JsonMaxDepthReached,
                                context.MaxDepth);
                            context.Cmdlet.WriteWarning(maxDepthMessage);
                        }

                        if (pso != null && pso.ImmediateBaseObjectIsEmpty)
                        {
                            // The obj is a pure PSObject, we convert the original PSObject to a string,
                            // instead of its base object in this case
                            rv = LanguagePrimitives.ConvertTo(pso, typeof(string),
                                CultureInfo.InvariantCulture);
                            isPurePSObj = true;
                        }
                        else
                        {
                            rv = LanguagePrimitives.ConvertTo(obj, typeof(string),
                                CultureInfo.InvariantCulture);
                        }
                    }
                    else
                    {
                        if (obj is IDictionary dict)
                        {
                            rv = ProcessDictionary(dict, currentDepth, in context);
                        }
                        else
                        {
                            if (obj is IEnumerable enumerable)
                            {
                                rv = ProcessEnumerable(enumerable, currentDepth, in context);
                            }
                            else
                            {
                                rv = ProcessCustomObject<JsonIgnoreAttribute>(obj, currentDepth, in context);
                                isCustomObj = true;
                            }
                        }
                    }
                }
            }

            rv = AddPsProperties(pso, rv, currentDepth, isPurePSObj, isCustomObj, in context);

            return rv;
        }

        
        private static object AddPsProperties(object psObj, object obj, int depth, bool isPurePSObj, bool isCustomObj, in ConvertToJsonContext context)
        {
            if (!(psObj is PSObject pso))
            {
                return obj;
            }

            // when isPurePSObj is true, the obj is guaranteed to be a string converted by LanguagePrimitives
            if (isPurePSObj)
            {
                return obj;
            }

            bool wasDictionary = true;

            if (obj is not IDictionary dict)
            {
                wasDictionary = false;
                dict = new Dictionary<string, object>();
                dict.Add("value", obj);
            }

            AppendPsProperties(pso, dict, depth, isCustomObj, in context);

            if (!wasDictionary && dict.Count == 1)
            {
                return obj;
            }

            return dict;
        }

        
        private static void AppendPsProperties(PSObject psObj, IDictionary receiver, int depth, bool isCustomObject, in ConvertToJsonContext context)
        {
            // if the psObj is a DateTime or String type, we don't serialize any extended or adapted properties
            if (psObj.BaseObject is string || psObj.BaseObject is DateTime)
            {
                return;
            }

            // serialize only Extended and Adapted properties..
            PSMemberInfoCollection<PSPropertyInfo> srcPropertiesToSearch =
                new PSMemberInfoIntegratingCollection<PSPropertyInfo>(psObj,
                    isCustomObject ? PSObject.GetPropertyCollection(PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted) :
                    PSObject.GetPropertyCollection(PSMemberViewTypes.Extended));

            foreach (PSPropertyInfo prop in srcPropertiesToSearch)
            {
                object value = null;
                try
                {
                    value = prop.Value;
                }
                catch (Exception)
                {
                }

                if (!receiver.Contains(prop.Name))
                {
                    receiver[prop.Name] = ProcessValue(value, depth + 1, in context);
                }
            }
        }

        
        private static object ProcessDictionary(IDictionary dict, int depth, in ConvertToJsonContext context)
        {
            Dictionary<string, object> result = new(dict.Count);

            foreach (DictionaryEntry entry in dict)
            {
                string name = entry.Key as string;
                if (name == null)
                {
                    // use the error string that matches the message from JavaScriptSerializer
                    string errorMsg = string.Format(
                        CultureInfo.CurrentCulture,
                        WebCmdletStrings.NonStringKeyInDictionary,
                        dict.GetType().FullName);

                    var exception = new InvalidOperationException(errorMsg);
                    if (context.Cmdlet != null)
                    {
                        var errorRecord = new ErrorRecord(exception, "NonStringKeyInDictionary", ErrorCategory.InvalidOperation, dict);
                        context.Cmdlet.ThrowTerminatingError(errorRecord);
                    }
                    else
                    {
                        throw exception;
                    }
                }

                result.Add(name, ProcessValue(entry.Value, depth + 1, in context));
            }

            return result;
        }

        
        private static object ProcessEnumerable(IEnumerable enumerable, int depth, in ConvertToJsonContext context)
        {
            List<object> result = new();

            foreach (object o in enumerable)
            {
                result.Add(ProcessValue(o, depth + 1, in context));
            }

            return result;
        }

        
        private static object ProcessCustomObject<T>(object o, int depth, in ConvertToJsonContext context)
        {
            Dictionary<string, object> result = new();
            Type t = o.GetType();

            foreach (FieldInfo info in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!info.IsDefined(typeof(T), true))
                {
                    object value;
                    try
                    {
                        value = info.GetValue(o);
                    }
                    catch (Exception)
                    {
                        value = null;
                    }

                    result.Add(info.Name, ProcessValue(value, depth + 1, in context));
                }
            }

            foreach (PropertyInfo info2 in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!info2.IsDefined(typeof(T), true))
                {
                    MethodInfo getMethod = info2.GetGetMethod();
                    if ((getMethod != null) && (getMethod.GetParameters().Length == 0))
                    {
                        object value;
                        try
                        {
                            value = getMethod.Invoke(o, Array.Empty<object>());
                        }
                        catch (Exception)
                        {
                            value = null;
                        }

                        result.Add(info2.Name, ProcessValue(value, depth + 1, in context));
                    }
                }
            }

            return result;
        }

        #endregion ConvertToJson
    }
}
