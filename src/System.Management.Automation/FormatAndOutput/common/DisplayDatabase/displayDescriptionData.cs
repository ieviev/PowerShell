// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// this file contains the data structures for the in memory database
// containing display and formatting information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    internal enum EnumerableExpansion
    {
        
        CoreOnly,

        
        EnumOnly,

        
        Both,
    }

    #region Type Info Database

    internal sealed partial class TypeInfoDataBase
    {
        // define the sections corresponding the XML file
        internal DefaultSettingsSection defaultSettingsSection = new DefaultSettingsSection();
        internal TypeGroupsSection typeGroupSection = new TypeGroupsSection();
        internal ViewDefinitionsSection viewDefinitionsSection = new ViewDefinitionsSection();
        internal FormatControlDefinitionHolder formatControlDefinitionHolder = new FormatControlDefinitionHolder();

        
        internal DisplayResourceManagerCache displayResourceManagerCache = new DisplayResourceManagerCache();
    }

    internal sealed class DatabaseLoadingInfo
    {
        internal string fileDirectory = null;
        internal string filePath = null;
        internal bool isFullyTrusted = false;
        internal bool isProductCode = false;
        internal string xPath = null;
        internal DateTime loadTime = DateTime.Now;
    }
    #endregion

    #region Default Settings

#if _LATER
    internal class SettableOnceValue<T>
    {
        SettableOnceValue (T defaultValue)
        {
            this._default = defaultValue;
        }

        internal void f(T x)
        {
            Nullable<T> y = x;
            this._value = y;
            // this._value = (Nullable<T>)x;
        }

        internal T Value
        {
        
            get
            {
                if (_value != null)
                    return this._value.Value;
                return _default;
            }
        }

        private Nullable<T> _value;
        private T _default;
    }
#endif

    internal sealed class DefaultSettingsSection
    {
        internal bool MultilineTables
        {
            get
            {
                if (_multilineTables.HasValue)
                    return _multilineTables.Value;
                return false;
            }

            set
            {
                if (!_multilineTables.HasValue)
                {
                    _multilineTables = value;
                }
            }
        }

        private bool? _multilineTables;

        internal FormatErrorPolicy formatErrorPolicy = new FormatErrorPolicy();
        internal ShapeSelectionDirectives shapeSelectionDirectives = new ShapeSelectionDirectives();
        internal List<EnumerableExpansionDirective> enumerableExpansionDirectiveList = new List<EnumerableExpansionDirective>();
    }

    internal sealed class FormatErrorPolicy
    {
        
        internal bool ShowErrorsAsMessages
        {
            get
            {
                if (_showErrorsAsMessages.HasValue)
                    return _showErrorsAsMessages.Value;
                return false;
            }

            set
            {
                if (!_showErrorsAsMessages.HasValue)
                {
                    _showErrorsAsMessages = value;
                }
            }
        }

        private bool? _showErrorsAsMessages;

        
        internal bool ShowErrorsInFormattedOutput
        {
            get
            {
                if (_showErrorsInFormattedOutput.HasValue)
                    return _showErrorsInFormattedOutput.Value;
                return false;
            }

            set
            {
                if (!_showErrorsInFormattedOutput.HasValue)
                {
                    _showErrorsInFormattedOutput = value;
                }
            }
        }

        private bool? _showErrorsInFormattedOutput;

        
        internal string errorStringInFormattedOutput = "#ERR";

        
        internal string formatErrorStringInFormattedOutput = "#FMTERR";
    }

    internal sealed class ShapeSelectionDirectives
    {
        internal int PropertyCountForTable
        {
            get
            {
                if (_propertyCountForTable.HasValue)
                    return _propertyCountForTable.Value;
                return 4;
            }

            set
            {
                if (!_propertyCountForTable.HasValue)
                {
                    _propertyCountForTable = value;
                }
            }
        }

        private int? _propertyCountForTable;

        internal List<FormatShapeSelectionOnType> formatShapeSelectionOnTypeList = new List<FormatShapeSelectionOnType>();
    }

    internal enum FormatShape { Table, List, Wide, Complex, Undefined }

    internal abstract class FormatShapeSelectionBase
    {
        internal FormatShape formatShape = FormatShape.Undefined;
    }

    internal sealed class FormatShapeSelectionOnType : FormatShapeSelectionBase
    {
        internal AppliesTo appliesTo;
    }

    internal sealed class EnumerableExpansionDirective
    {
        internal EnumerableExpansion enumerableExpansion = EnumerableExpansion.EnumOnly;
        internal AppliesTo appliesTo;
    }

    #endregion

    #region Type Groups Definitions

    internal sealed class TypeGroupsSection
    {
        internal List<TypeGroupDefinition> typeGroupDefinitionList = new List<TypeGroupDefinition>();
    }

    internal sealed class TypeGroupDefinition
    {
        internal string name;
        internal List<TypeReference> typeReferenceList = new List<TypeReference>();
    }

    internal abstract class TypeOrGroupReference
    {
        internal string name;

        
        internal ExpressionToken conditionToken = null;
    }

    internal sealed class TypeReference : TypeOrGroupReference
    {
    }

    internal sealed class TypeGroupReference : TypeOrGroupReference
    {
    }

    #endregion

    #region Elementary Tokens

    internal abstract class FormatToken
    {
    }

    internal sealed class TextToken : FormatToken
    {
        internal string text;
        internal StringResourceReference resource;
    }

    internal sealed class NewLineToken : FormatToken
    {
        internal int count = 1;
    }

    internal sealed class FrameToken : FormatToken
    {
        
        internal ComplexControlItemDefinition itemDefinition = new ComplexControlItemDefinition();

        
        internal FrameInfoDefinition frameInfoDefinition = new FrameInfoDefinition();
    }

    internal sealed class FrameInfoDefinition
    {
        
        internal int leftIndentation = 0;

        
        internal int rightIndentation = 0;

        
        internal int firstLine = 0;
    }

    internal sealed class ExpressionToken
    {
        internal ExpressionToken() { }

        internal ExpressionToken(string expressionValue, bool isScriptBlock)
        {
            this.expressionValue = expressionValue;
            this.isScriptBlock = isScriptBlock;
        }

        internal bool isScriptBlock;
        internal string expressionValue;
    }

    internal abstract class PropertyTokenBase : FormatToken
    {
        
        internal ExpressionToken conditionToken = null;

        internal ExpressionToken expression = new ExpressionToken();
        internal bool enumerateCollection = false;
    }

    internal sealed class CompoundPropertyToken : PropertyTokenBase
    {
        
        internal ControlBase control = null;
    }

    internal sealed class FieldPropertyToken : PropertyTokenBase
    {
        internal FieldFormattingDirective fieldFormattingDirective = new FieldFormattingDirective();
    }

    internal sealed class FieldFormattingDirective
    {
        internal string formatString = null; // optional
        internal bool isTable = false;
    }

    #endregion Elementary Tokens

    #region Control Definitions: common data

    
    internal abstract class ControlBase
    {
        internal static string GetControlShapeName(ControlBase control)
        {
            if (control is TableControlBody)
            {
                return nameof(FormatShape.Table);
            }

            if (control is ListControlBody)
            {
                return nameof(FormatShape.List);
            }

            if (control is WideControlBody)
            {
                return nameof(FormatShape.Wide);
            }

            if (control is ComplexControlBody)
            {
                return nameof(FormatShape.Complex);
            }

            return string.Empty;
        }

        
        /// <returns></returns>
        internal virtual ControlBase Copy()
        {
            System.Management.Automation.Diagnostics.Assert(false,
                "This should never be called directly on the base. Let the derived class implement this method.");
            return this;
        }
    }

    
    internal sealed class ControlReference : ControlBase
    {
        
        internal string name = null;

        
        internal Type controlType = null;
    }

    
    internal abstract class ControlBody : ControlBase
    {
        
        internal bool? autosize = null;

        
        internal bool repeatHeader = false;
    }

    
    internal sealed class ControlDefinition
    {
        
        internal string name = null;

        
        internal ControlBody controlBody = null;
    }

    #endregion

    #region View Definitions: common data
    internal sealed class ViewDefinitionsSection
    {
        internal List<ViewDefinition> viewDefinitionList = new List<ViewDefinition>();
    }

    internal sealed partial class AppliesTo
    {
        // it can contain either a type or type group reference
        internal List<TypeOrGroupReference> referenceList = new List<TypeOrGroupReference>();
    }

    internal sealed class GroupBy
    {
        internal StartGroup startGroup = new StartGroup();
        // NOTE: extension point for describing:
        // * end group statistics
        // * end group footer
        // This can be done with defining a new Type called EndGroup with fields
        // such as stat and footer.

    }

    internal sealed class StartGroup
    {
        
        internal ExpressionToken expression = null;

        
        internal ControlBase control = null;

        
        internal TextToken labelTextToken = null;
    }

    
    internal sealed class FormatControlDefinitionHolder
    {
        
        internal List<ControlDefinition> controlDefinitionList = new List<ControlDefinition>();
    }

    
    internal sealed class ViewDefinition
    {
        internal DatabaseLoadingInfo loadingInfo;

        
        internal string name;

        
        internal AppliesTo appliesTo = new AppliesTo();

        
        internal GroupBy groupBy;

        
        internal FormatControlDefinitionHolder formatControlDefinitionHolder = new FormatControlDefinitionHolder();

        
        internal ControlBase mainControl;

        
        internal bool outOfBand;

        
        internal bool isHelpFormatter;

        internal Guid InstanceId { get; private set; }

        internal ViewDefinition()
        {
            InstanceId = Guid.NewGuid();
        }
    }

    
    internal abstract class FormatDirective
    {
    }

    #endregion

    #region Localized Resources

    internal sealed class StringResourceReference
    {
        internal DatabaseLoadingInfo loadingInfo = null;
        internal string assemblyName = null;
        internal string assemblyLocation = null;
        internal string baseName = null;
        internal string resourceId = null;
    }

    #endregion
}

namespace System.Management.Automation
{
    
    public sealed class ExtendedTypeDefinition
    {
        
        public string TypeName
        {
            get { return TypeNames[0]; }
        }

        
        public List<string> TypeNames { get; internal set; }

        
        public List<FormatViewDefinition> FormatViewDefinition { get; internal set; }

        
        /// <returns></returns>
        public override string ToString()
        {
            return TypeName;
        }

        
        /// <param name="typeName"></param>
        /// <param name="viewDefinitions"></param>
        public ExtendedTypeDefinition(string typeName, IEnumerable<FormatViewDefinition> viewDefinitions) : this()
        {
            if (string.IsNullOrEmpty(typeName))
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));
            if (viewDefinitions == null)
                throw PSTraceSource.NewArgumentNullException(nameof(viewDefinitions));

            TypeNames.Add(typeName);
            foreach (FormatViewDefinition definition in viewDefinitions)
            {
                FormatViewDefinition.Add(definition);
            }
        }

        
        /// <param name="typeName"></param>
        public ExtendedTypeDefinition(string typeName) : this()
        {
            if (string.IsNullOrEmpty(typeName))
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));

            TypeNames.Add(typeName);
        }

        internal ExtendedTypeDefinition()
        {
            FormatViewDefinition = new List<FormatViewDefinition>();
            TypeNames = new List<string>();
        }
    }

    
    [DebuggerDisplay("{Name}")]
    public sealed class FormatViewDefinition
    {
        
        public string Name { get; }

        
        public PSControl Control { get; }

        
        internal Guid InstanceId { get; set; }

        internal FormatViewDefinition(string name, PSControl control, Guid instanceid)
        {
            Name = name;
            Control = control;
            InstanceId = instanceid;
        }

        /// <summary/>
        public FormatViewDefinition(string name, PSControl control)
        {
            if (string.IsNullOrEmpty(name))
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            if (control == null)
                throw PSTraceSource.NewArgumentNullException(nameof(control));

            Name = name;
            Control = control;
            InstanceId = Guid.NewGuid();
        }
    }

    
    public abstract class PSControl
    {
        
        public PSControlGroupBy GroupBy { get; set; }

        
        public bool OutOfBand { get; set; }

        internal abstract void WriteToXml(FormatXmlWriter writer);

        internal virtual bool SafeForExport()
        {
            return GroupBy == null || GroupBy.IsSafeForExport();
        }

        internal virtual bool CompatibleWithOldPowerShell()
        {
            // This is too strict, the GroupBy would just be ignored by the remote
            // PowerShell, but that's still wrong.
            // OutOfBand is also ignored by old PowerShell, but it's of less importance.

            return GroupBy == null;
        }
    }

    
    public sealed class PSControlGroupBy
    {
        
        public DisplayEntry Expression { get; set; }

        
        public string Label { get; set; }

        
        public CustomControl CustomControl { get; set; }

        internal bool IsSafeForExport()
        {
            return (Expression == null || Expression.SafeForExport()) &&
                   (CustomControl == null || CustomControl.SafeForExport());
        }

        internal static PSControlGroupBy Get(GroupBy groupBy)
        {
            if (groupBy != null)
            {
                // TODO - groupBy.startGroup.control
                var expressionToken = groupBy.startGroup.expression;
                return new PSControlGroupBy
                {
                    Expression = new DisplayEntry(expressionToken),
                    Label = groupBy.startGroup.labelTextToken?.text
                };
            }

            return null;
        }
    }

    
    public sealed class DisplayEntry
    {
        
        public DisplayEntryValueType ValueType { get; internal set; }

        
        public string Value { get; internal set; }

        internal DisplayEntry() { }

        
        public DisplayEntry(string value, DisplayEntryValueType type)
        {
            if (string.IsNullOrEmpty(value))
                if (value == null || type == DisplayEntryValueType.Property)
                    throw PSTraceSource.NewArgumentNullException(nameof(value));

            Value = value;
            ValueType = type;
        }

        /// <summary/>
        public override string ToString()
        {
            return (ValueType == DisplayEntryValueType.Property ? "property: " : "script: ") + Value;
        }

        internal DisplayEntry(ExpressionToken expression)
        {
            Value = expression.expressionValue;
            ValueType = expression.isScriptBlock ? DisplayEntryValueType.ScriptBlock : DisplayEntryValueType.Property;

            if (string.IsNullOrEmpty(Value))
                if (Value == null || ValueType == DisplayEntryValueType.Property)
                    throw PSTraceSource.NewArgumentNullException("value");
        }

        internal bool SafeForExport()
        {
            return ValueType != DisplayEntryValueType.ScriptBlock;
        }
    }

    
    public sealed class EntrySelectedBy
    {
        
        public List<string> TypeNames { get; set; }

        
        public List<DisplayEntry> SelectionCondition { get; set; }

        internal static EntrySelectedBy Get(IEnumerable<string> entrySelectedByType, IEnumerable<DisplayEntry> entrySelectedByCondition)
        {
            EntrySelectedBy result = null;
            if (entrySelectedByType != null || entrySelectedByCondition != null)
            {
                result = new EntrySelectedBy();
                bool isEmpty = true;
                if (entrySelectedByType != null)
                {
                    result.TypeNames = new List<string>(entrySelectedByType);
                    if (result.TypeNames.Count > 0)
                        isEmpty = false;
                }

                if (entrySelectedByCondition != null)
                {
                    result.SelectionCondition = new List<DisplayEntry>(entrySelectedByCondition);
                    if (result.SelectionCondition.Count > 0)
                        isEmpty = false;
                }

                if (isEmpty)
                    return null;
            }

            return result;
        }

        internal static EntrySelectedBy Get(List<TypeOrGroupReference> references)
        {
            EntrySelectedBy result = null;
            if (references != null && references.Count > 0)
            {
                result = new EntrySelectedBy();
                foreach (TypeOrGroupReference tr in references)
                {
                    if (tr.conditionToken != null)
                    {
                        result.SelectionCondition ??= new List<DisplayEntry>();

                        result.SelectionCondition.Add(new DisplayEntry(tr.conditionToken));
                        continue;
                    }

                    if (tr is TypeGroupReference)
                        continue;

                    result.TypeNames ??= new List<string>();

                    result.TypeNames.Add(tr.name);
                }
            }

            return result;
        }

        internal bool SafeForExport()
        {
            if (SelectionCondition == null)
                return true;

            foreach (var cond in SelectionCondition)
            {
                if (!cond.SafeForExport())
                    return false;
            }

            return true;
        }

        internal bool CompatibleWithOldPowerShell()
        {
            // Old versions of PowerShell know nothing about selection conditions.
            return SelectionCondition == null || SelectionCondition.Count == 0;
        }
    }

    
    public enum Alignment
    {
        
        Undefined = 0,

        
        Left = 1,

        
        Center = 2,

        
        Right = 3,
    }

    
    public enum DisplayEntryValueType
    {
        
        Property = 0,

        
        ScriptBlock = 1,
    }
}
