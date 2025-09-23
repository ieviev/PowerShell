// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// this file contains the data structures for the in memory database
// containing display and formatting information

using System.Collections.Generic;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region Table View Definitions

    
    internal static class TextAlignment
    {
        internal const int Undefined = 0;
        internal const int Left = 1;
        internal const int Center = 2;
        internal const int Right = 3;
    }

    
    internal sealed class TableControlBody : ControlBody
    {
        
        internal TableHeaderDefinition header = new TableHeaderDefinition();

        
        internal TableRowDefinition defaultDefinition;

        
        internal List<TableRowDefinition> optionalDefinitionList = new List<TableRowDefinition>();

        internal override ControlBase Copy()
        {
            TableControlBody result = new TableControlBody
            {
                autosize = this.autosize,
                header = this.header.Copy()
            };
            if (defaultDefinition != null)
            {
                result.defaultDefinition = this.defaultDefinition.Copy();
            }

            foreach (TableRowDefinition trd in this.optionalDefinitionList)
            {
                result.optionalDefinitionList.Add(trd);
            }

            return result;
        }
    }

    
    internal sealed class TableHeaderDefinition
    {
        
        internal bool hideHeader;

        
        internal List<TableColumnHeaderDefinition> columnHeaderDefinitionList =
                            new List<TableColumnHeaderDefinition>();

        
        internal TableHeaderDefinition Copy()
        {
            TableHeaderDefinition result = new TableHeaderDefinition { hideHeader = this.hideHeader };
            foreach (TableColumnHeaderDefinition tchd in this.columnHeaderDefinitionList)
            {
                result.columnHeaderDefinitionList.Add(tchd);
            }

            return result;
        }
    }

    internal sealed class TableColumnHeaderDefinition
    {
        
        internal TextToken label = null;

        
        internal int alignment = TextAlignment.Undefined;

        
        internal int width = 0; // undefined
    }

    
    internal sealed class TableRowDefinition
    {
        
        internal AppliesTo appliesTo;

        
        internal bool multiLine;

        
        internal List<TableRowItemDefinition> rowItemDefinitionList = new List<TableRowItemDefinition>();

        
        internal TableRowDefinition Copy()
        {
            TableRowDefinition result = new TableRowDefinition
            {
                appliesTo = this.appliesTo,
                multiLine = this.multiLine
            };
            foreach (TableRowItemDefinition trid in this.rowItemDefinitionList)
            {
                result.rowItemDefinitionList.Add(trid);
            }

            return result;
        }
    }

    
    internal sealed class TableRowItemDefinition
    {
        
        internal int alignment = TextAlignment.Undefined;

        
        internal List<FormatToken> formatTokenList = new List<FormatToken>();
    }

    #endregion
}

namespace System.Management.Automation
{
    
    public sealed class TableControl : PSControl
    {
        
        public List<TableControlColumnHeader> Headers { get; set; }

        
        public List<TableControlRow> Rows { get; set; }

        
        public bool AutoSize { get; set; }

        
        public bool HideTableHeaders { get; set; }

        
        public static TableControlBuilder Create(bool outOfBand = false, bool autoSize = false, bool hideTableHeaders = false)
        {
            var table = new TableControl { OutOfBand = outOfBand, AutoSize = autoSize, HideTableHeaders = hideTableHeaders };
            return new TableControlBuilder(table);
        }

        
        public TableControl()
        {
            Headers = new List<TableControlColumnHeader>();
            Rows = new List<TableControlRow>();
        }

        internal override void WriteToXml(FormatXmlWriter writer)
        {
            writer.WriteTableControl(this);
        }

        
        internal override bool SafeForExport()
        {
            if (!base.SafeForExport())
                return false;

            foreach (var row in Rows)
            {
                if (!row.SafeForExport())
                    return false;
            }

            return true;
        }

        internal override bool CompatibleWithOldPowerShell()
        {
            if (!base.CompatibleWithOldPowerShell())
                return false;

            foreach (var row in Rows)
            {
                if (!row.CompatibleWithOldPowerShell())
                    return false;
            }

            return true;
        }

        internal TableControl(TableControlBody tcb, ViewDefinition viewDefinition) : this()
        {
            this.OutOfBand = viewDefinition.outOfBand;
            this.GroupBy = PSControlGroupBy.Get(viewDefinition.groupBy);

            this.AutoSize = tcb.autosize.GetValueOrDefault();
            this.HideTableHeaders = tcb.header.hideHeader;

            TableControlRow row = new TableControlRow(tcb.defaultDefinition);

            Rows.Add(row);

            foreach (TableRowDefinition rd in tcb.optionalDefinitionList)
            {
                row = new TableControlRow(rd);

                Rows.Add(row);
            }

            foreach (TableColumnHeaderDefinition hd in tcb.header.columnHeaderDefinitionList)
            {
                TableControlColumnHeader header = new TableControlColumnHeader(hd);
                Headers.Add(header);
            }
        }

        
        public TableControl(TableControlRow tableControlRow) : this()
        {
            if (tableControlRow == null)
                throw PSTraceSource.NewArgumentNullException("tableControlRows");

            this.Rows.Add(tableControlRow);
        }

        
        public TableControl(TableControlRow tableControlRow, IEnumerable<TableControlColumnHeader> tableControlColumnHeaders) : this()
        {
            if (tableControlRow == null)
                throw PSTraceSource.NewArgumentNullException("tableControlRows");
            if (tableControlColumnHeaders == null)
                throw PSTraceSource.NewArgumentNullException(nameof(tableControlColumnHeaders));

            this.Rows.Add(tableControlRow);
            foreach (TableControlColumnHeader header in tableControlColumnHeaders)
            {
                this.Headers.Add(header);
            }
        }
    }

    
    public sealed class TableControlColumnHeader
    {
        
        public string Label { get; set; }

        
        public Alignment Alignment { get; set; }

        
        public int Width { get; set; }

        internal TableControlColumnHeader(TableColumnHeaderDefinition colheaderdefinition)
        {
            if (colheaderdefinition.label != null)
            {
                Label = colheaderdefinition.label.text;
            }

            Alignment = (Alignment)colheaderdefinition.alignment;
            Width = colheaderdefinition.width;
        }

        
        public TableControlColumnHeader()
        {
        }

        
        public TableControlColumnHeader(string label, int width, Alignment alignment)
        {
            if (width < 0)
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(width), width);

            this.Label = label;
            this.Width = width;
            this.Alignment = alignment;
        }
    }

    
    public sealed class TableControlColumn
    {
        
        public Alignment Alignment { get; set; }

        
        public DisplayEntry DisplayEntry { get; set; }

        
        public string FormatString { get; internal set; }

        
        public override string ToString()
        {
            return DisplayEntry.Value;
        }

        
        public TableControlColumn()
        {
        }

        internal TableControlColumn(string text, int alignment, bool isscriptblock, string formatString)
        {
            Alignment = (Alignment)alignment;
            DisplayEntry = new DisplayEntry(text, isscriptblock ? DisplayEntryValueType.ScriptBlock : DisplayEntryValueType.Property);
            FormatString = formatString;
        }

        
        public TableControlColumn(Alignment alignment, DisplayEntry entry)
        {
            this.Alignment = alignment;
            this.DisplayEntry = entry;
        }

        internal bool SafeForExport()
        {
            return DisplayEntry.SafeForExport();
        }
    }

    
    public sealed class TableControlRow
    {
        
        public List<TableControlColumn> Columns { get; set; }

        
        public EntrySelectedBy SelectedBy { get; internal set; }

        
        public bool Wrap { get; set; }

        
        public TableControlRow()
        {
            Columns = new List<TableControlColumn>();
        }

        internal TableControlRow(TableRowDefinition rowdefinition) : this()
        {
            Wrap = rowdefinition.multiLine;
            if (rowdefinition.appliesTo != null)
            {
                SelectedBy = EntrySelectedBy.Get(rowdefinition.appliesTo.referenceList);
            }

            foreach (TableRowItemDefinition itemdef in rowdefinition.rowItemDefinitionList)
            {
                TableControlColumn column;

                if (itemdef.formatTokenList[0] is FieldPropertyToken fpt)
                {
                    column = new TableControlColumn(fpt.expression.expressionValue, itemdef.alignment,
                                    fpt.expression.isScriptBlock, fpt.fieldFormattingDirective.formatString);
                }
                else
                {
                    column = new TableControlColumn();
                }

                Columns.Add(column);
            }
        }

        
        public TableControlRow(IEnumerable<TableControlColumn> columns) : this()
        {
            if (columns == null)
                throw PSTraceSource.NewArgumentNullException(nameof(columns));
            foreach (TableControlColumn column in columns)
            {
                Columns.Add(column);
            }
        }

        internal bool SafeForExport()
        {
            foreach (var column in Columns)
            {
                if (!column.SafeForExport())
                    return false;
            }

            return SelectedBy != null && SelectedBy.SafeForExport();
        }

        internal bool CompatibleWithOldPowerShell()
        {
            // Old versions of PowerShell don't support multiple row definitions.
            return SelectedBy == null;
        }
    }

    
    public sealed class TableRowDefinitionBuilder
    {
        internal readonly TableControlBuilder _tcb;
        internal readonly TableControlRow _tcr;

        internal TableRowDefinitionBuilder(TableControlBuilder tcb, TableControlRow tcr)
        {
            _tcb = tcb;
            _tcr = tcr;
        }

        private TableRowDefinitionBuilder AddItem(string value, DisplayEntryValueType entryType, Alignment alignment, string format)
        {
            if (string.IsNullOrEmpty(value))
                throw PSTraceSource.NewArgumentException(nameof(value));

            var tableControlColumn = new TableControlColumn(alignment, new DisplayEntry(value, entryType))
            {
                FormatString = format
            };
            _tcr.Columns.Add(tableControlColumn);

            return this;
        }

        
        public TableRowDefinitionBuilder AddScriptBlockColumn(string scriptBlock, Alignment alignment = Alignment.Undefined, string format = null)
        {
            return AddItem(scriptBlock, DisplayEntryValueType.ScriptBlock, alignment, format);
        }

        
        public TableRowDefinitionBuilder AddPropertyColumn(string propertyName, Alignment alignment = Alignment.Undefined, string format = null)
        {
            return AddItem(propertyName, DisplayEntryValueType.Property, alignment, format);
        }

        
        public TableControlBuilder EndRowDefinition()
        {
            return _tcb;
        }
    }

    
    public sealed class TableControlBuilder
    {
        internal readonly TableControl _table;

        internal TableControlBuilder(TableControl table)
        {
            _table = table;
        }

        
        public TableControlBuilder GroupByProperty(string property, CustomControl customControl = null, string label = null)
        {
            _table.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(property, DisplayEntryValueType.Property),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        
        public TableControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl = null, string label = null)
        {
            _table.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(scriptBlock, DisplayEntryValueType.ScriptBlock),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        
        public TableControlBuilder AddHeader(Alignment alignment = Alignment.Undefined, int width = 0, string label = null)
        {
            _table.Headers.Add(new TableControlColumnHeader(label, width, alignment));
            return this;
        }

        
        public TableRowDefinitionBuilder StartRowDefinition(bool wrap = false, IEnumerable<string> entrySelectedByType = null, IEnumerable<DisplayEntry> entrySelectedByCondition = null)
        {
            var row = new TableControlRow { Wrap = wrap };
            if (entrySelectedByType != null || entrySelectedByCondition != null)
            {
                row.SelectedBy = new EntrySelectedBy();
                if (entrySelectedByType != null)
                {
                    row.SelectedBy.TypeNames = new List<string>(entrySelectedByType);
                }

                if (entrySelectedByCondition != null)
                {
                    row.SelectedBy.SelectionCondition = new List<DisplayEntry>(entrySelectedByCondition);
                }
            }

            _table.Rows.Add(row);
            return new TableRowDefinitionBuilder(this, row);
        }

        
        public TableControl EndTable()
        {
            return _table;
        }
    }
}
