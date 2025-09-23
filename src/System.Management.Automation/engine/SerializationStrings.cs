// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    internal static class SerializationStrings
    {
        #region element tags

        
        internal const string RootElementTag = "Objs";

        #region PSObject

        
        internal const string PSObjectTag = "Obj";

        
        internal const string AdapterProperties = "Props";

        
        internal const string TypeNamesTag = "TN";
        
        internal const string TypeNamesItemTag = "T";
        
        internal const string TypeNamesReferenceTag = "TNRef";

        
        internal const string MemberSet = "MS";

        
        internal const string NoteProperty = "N";

        
        internal const string ToStringElementTag = "ToString";

        #endregion PSObject

        #region known container tags

        
        internal const string CollectionTag = "IE";

        
        internal const string DictionaryTag = "DCT";

        
        internal const string DictionaryEntryTag = "En";

        
        internal const string DictionaryKey = "Key";

        
        internal const string DictionaryValue = "Value";

        
        internal const string StackTag = "STK";

        
        internal const string QueueTag = "QUE";

        
        internal const string ListTag = "LST";

        #endregion known container tags

        #region primitive known type tags
        
        /// <remarks>This property is used for System.Char type</remarks>
        internal const string CharTag = "C";

        
        /// <remarks>This property is used for System.Guid type</remarks>
        internal const string GuidTag = "G";

        
        /// <remarks>This property is used for System.Boolean type</remarks>
        internal const string BooleanTag = "B";

        
        /// <remarks>This property is used for System.Byte type</remarks>
        internal const string UnsignedByteTag = "By";

        
        /// <remarks>This property is used for System.DateTime type</remarks>
        internal const string DateTimeTag = "DT";

        
        /// <remarks>This property is used for System.Decimal type</remarks>
        internal const string DecimalTag = "D";

        
        /// <remarks>This property is used for System.Double type</remarks>
        internal const string DoubleTag = "Db";

        
        /// <remarks>This property is used for System.TimeSpan type</remarks>
        internal const string DurationTag = "TS";

        
        /// <remarks>This property is used for System.Single type</remarks>
        internal const string FloatTag = "Sg";

        
        /// <remarks>This property is used for System.Int32 type</remarks>
        internal const string IntTag = "I32";

        
        /// <remarks>This property is used for System.Int64 type</remarks>
        internal const string LongTag = "I64";

        
        /// <remarks>This property is used for System.SByte type</remarks>
        internal const string ByteTag = "SB";

        
        /// <remarks>This property is used for System.Int16 type</remarks>
        internal const string ShortTag = "I16";

        
        /// <remarks>This property is used for System.IO.Stream type</remarks>
        internal const string Base64BinaryTag = "BA";

        
        /// <remarks>This property is used for System.Management.Automation.ScriptBlock type</remarks>
        internal const string ScriptBlockTag = "SBK";

        
        /// <remarks>This property is used for System.String type</remarks>
        internal const string StringTag = "S";

        
        /// <remarks>This property is used for System.Security.SecureString type</remarks>
        internal const string SecureStringTag = "SS";

        
        /// <remarks>This property is used for System.UInt16 Stream type</remarks>
        internal const string UnsignedShortTag = "U16";

        
        /// <remarks>This property is used for System.UInt32 type</remarks>
        internal const string UnsignedIntTag = "U32";

        
        /// <remarks>This property is used for System.Long type</remarks>
        internal const string UnsignedLongTag = "U64";

        
        /// <remarks>This property is used for System.Uri type</remarks>
        internal const string AnyUriTag = "URI";

        
        internal const string VersionTag = "Version";

        
        internal const string SemanticVersionTag = "SemanticVersion";

        
        internal const string XmlDocumentTag = "XD";

        
        internal const string NilTag = "Nil";

        
        /// <remarks>This property is used for a reference to a property bag</remarks>
        internal const string ReferenceTag = "Ref";

        #region progress record

        internal const string ProgressRecord = "PR";
        internal const string ProgressRecordActivityId = "AI";
        internal const string ProgressRecordParentActivityId = "PI";
        internal const string ProgressRecordActivity = "AV";
        internal const string ProgressRecordStatusDescription = "SD";
        internal const string ProgressRecordCurrentOperation = "CO";
        internal const string ProgressRecordPercentComplete = "PC";
        internal const string ProgressRecordSecondsRemaining = "SR";
        internal const string ProgressRecordType = "T";

        #endregion progress record

        #endregion primitive known type tags

        #endregion element tags

        #region attribute tags
        
        internal const string ReferenceIdAttribute = "RefId";

        
        internal const string NameAttribute = "N";

        
        internal const string VersionAttribute = "Version";

        
        internal const string StreamNameAttribute = "S";

        #endregion attribute tags

        #region namespace values

        
        internal const string MonadNamespace = "http://schemas.microsoft.com/powershell/2004/04";

        
        internal const string MonadNamespacePrefix = "ps";

        #endregion namespace values
    }
}
