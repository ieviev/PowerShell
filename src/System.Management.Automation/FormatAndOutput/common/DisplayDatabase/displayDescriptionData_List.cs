// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// this file contains the data structures for the in memory database
// containing display and formatting information

using System.Collections.Generic;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region List View Definitions

    
    internal sealed class ListControlBody : ControlBody
    {
        
        internal ListControlEntryDefinition defaultEntryDefinition = null;

        
        internal List<ListControlEntryDefinition> optionalEntryList = new List<ListControlEntryDefinition>();

        internal override ControlBase Copy()
        {
            ListControlBody result = new ListControlBody();
            result.autosize = this.autosize;
            if (defaultEntryDefinition != null)
            {
                result.defaultEntryDefinition = this.defaultEntryDefinition.Copy();
            }

            foreach (ListControlEntryDefinition lced in this.optionalEntryList)
            {
                result.optionalEntryList.Add(lced);
            }

            return result;
        }
    }

    
    internal sealed class ListControlEntryDefinition
    {
        
        internal AppliesTo appliesTo = null;

        
        internal List<ListControlItemDefinition> itemDefinitionList = new List<ListControlItemDefinition>();

        
        internal ListControlEntryDefinition Copy()
        {
            ListControlEntryDefinition result = new ListControlEntryDefinition();
            result.appliesTo = this.appliesTo;
            foreach (ListControlItemDefinition lcid in this.itemDefinitionList)
            {
                result.itemDefinitionList.Add(lcid);
            }

            return result;
        }
    }

    
    internal sealed class ListControlItemDefinition
    {
        
        internal ExpressionToken conditionToken;

        
        internal TextToken label = null;

        
        internal List<FormatToken> formatTokenList = new List<FormatToken>();
    }

    #endregion
}

namespace System.Management.Automation
{
    
    public sealed class ListControl : PSControl
    {
        
        public List<ListControlEntry> Entries { get; internal set; }

        
        public static ListControlBuilder Create(bool outOfBand = false)
        {
            var list = new ListControl { OutOfBand = false };
            return new ListControlBuilder(list);
        }

        internal override void WriteToXml(FormatXmlWriter writer)
        {
            writer.WriteListControl(this);
        }

        
        internal override bool SafeForExport()
        {
            if (!base.SafeForExport())
                return false;

            foreach (var entry in Entries)
            {
                if (!entry.SafeForExport())
                    return false;
            }

            return true;
        }

        
        public ListControl()
        {
            Entries = new List<ListControlEntry>();
        }

        
        internal ListControl(ListControlBody listcontrolbody, ViewDefinition viewDefinition)
            : this()
        {
            this.GroupBy = PSControlGroupBy.Get(viewDefinition.groupBy);
            this.OutOfBand = viewDefinition.outOfBand;

            Entries.Add(new ListControlEntry(listcontrolbody.defaultEntryDefinition));

            foreach (ListControlEntryDefinition lced in listcontrolbody.optionalEntryList)
            {
                Entries.Add(new ListControlEntry(lced));
            }
        }

        
        public ListControl(IEnumerable<ListControlEntry> entries)
            : this()
        {
            if (entries == null)
                throw PSTraceSource.NewArgumentNullException(nameof(entries));
            foreach (ListControlEntry entry in entries)
            {
                this.Entries.Add(entry);
            }
        }

        internal override bool CompatibleWithOldPowerShell()
        {
            if (!base.CompatibleWithOldPowerShell())
                return false;

            foreach (var entry in Entries)
            {
                if (!entry.CompatibleWithOldPowerShell())
                    return false;
            }

            return true;
        }
    }

    
    public sealed class ListControlEntry
    {
        
        public List<ListControlEntryItem> Items { get; internal set; }

        
        public List<string> SelectedBy
        {
            get
            {
                EntrySelectedBy ??= new EntrySelectedBy { TypeNames = new List<string>() };
                return EntrySelectedBy.TypeNames;
            }
        }

        
        public EntrySelectedBy EntrySelectedBy { get; internal set; }

        
        public ListControlEntry()
        {
            Items = new List<ListControlEntryItem>();
        }

        internal ListControlEntry(ListControlEntryDefinition entrydefn)
            : this()
        {
            if (entrydefn.appliesTo != null)
            {
                EntrySelectedBy = EntrySelectedBy.Get(entrydefn.appliesTo.referenceList);
            }

            foreach (ListControlItemDefinition itemdefn in entrydefn.itemDefinitionList)
            {
                Items.Add(new ListControlEntryItem(itemdefn));
            }
        }

        
        public ListControlEntry(IEnumerable<ListControlEntryItem> listItems)
            : this()
        {
            if (listItems == null)
                throw PSTraceSource.NewArgumentNullException(nameof(listItems));
            foreach (ListControlEntryItem item in listItems)
            {
                this.Items.Add(item);
            }
        }

        
        public ListControlEntry(IEnumerable<ListControlEntryItem> listItems, IEnumerable<string> selectedBy)
        {
            if (listItems == null)
                throw PSTraceSource.NewArgumentNullException(nameof(listItems));
            if (selectedBy == null)
                throw PSTraceSource.NewArgumentNullException(nameof(selectedBy));

            EntrySelectedBy = new EntrySelectedBy { TypeNames = new List<string>(selectedBy) };
            foreach (ListControlEntryItem item in listItems)
            {
                this.Items.Add(item);
            }
        }

        internal bool SafeForExport()
        {
            foreach (var item in Items)
            {
                if (!item.SafeForExport())
                    return false;
            }

            return EntrySelectedBy == null || EntrySelectedBy.SafeForExport();
        }

        internal bool CompatibleWithOldPowerShell()
        {
            foreach (var item in Items)
            {
                if (!item.CompatibleWithOldPowerShell())
                    return false;
            }

            return EntrySelectedBy == null || EntrySelectedBy.CompatibleWithOldPowerShell();
        }
    }

    
    public sealed class ListControlEntryItem
    {
        
        public string Label { get; internal set; }

        
        public DisplayEntry DisplayEntry { get; internal set; }

        public DisplayEntry ItemSelectionCondition { get; internal set; }

        
        public string FormatString { get; internal set; }

        internal ListControlEntryItem()
        {
        }

        internal ListControlEntryItem(ListControlItemDefinition definition)
        {
            if (definition.label != null)
            {
                Label = definition.label.text;
            }

            if (definition.formatTokenList[0] is FieldPropertyToken fpt)
            {
                if (fpt.fieldFormattingDirective.formatString != null)
                {
                    FormatString = fpt.fieldFormattingDirective.formatString;
                }

                DisplayEntry = new DisplayEntry(fpt.expression);
                if (definition.conditionToken != null)
                {
                    ItemSelectionCondition = new DisplayEntry(definition.conditionToken);
                }
            }
        }

        
        public ListControlEntryItem(string label, DisplayEntry entry)
        {
            this.Label = label;
            this.DisplayEntry = entry;
        }

        internal bool SafeForExport()
        {
            return DisplayEntry.SafeForExport() &&
                   (ItemSelectionCondition == null || ItemSelectionCondition.SafeForExport());
        }

        internal bool CompatibleWithOldPowerShell()
        {
            // Old versions of PowerShell know nothing about ItemSelectionCondition.
            return ItemSelectionCondition == null;
        }
    }

    public class ListEntryBuilder
    {
        private readonly ListControlBuilder _listBuilder;
        internal ListControlEntry _listEntry;

        internal ListEntryBuilder(ListControlBuilder listBuilder, ListControlEntry listEntry)
        {
            _listBuilder = listBuilder;
            _listEntry = listEntry;
        }

        private ListEntryBuilder AddItem(string value, string label, DisplayEntryValueType kind, string format)
        {
            if (string.IsNullOrEmpty(value))
                throw PSTraceSource.NewArgumentNullException("property");

            _listEntry.Items.Add(new ListControlEntryItem
            {
                DisplayEntry = new DisplayEntry(value, kind),
                Label = label,
                FormatString = format
            });

            return this;
        }

        
        public ListEntryBuilder AddItemScriptBlock(string scriptBlock, string label = null, string format = null)
        {
            return AddItem(scriptBlock, label, DisplayEntryValueType.ScriptBlock, format);
        }

        
        public ListEntryBuilder AddItemProperty(string property, string label = null, string format = null)
        {
            return AddItem(property, label, DisplayEntryValueType.Property, format);
        }

        
        public ListControlBuilder EndEntry()
        {
            return _listBuilder;
        }
    }

    
    public class ListControlBuilder
    {
        internal ListControl _list;

        internal ListControlBuilder(ListControl list)
        {
            _list = list;
        }

        
        public ListControlBuilder GroupByProperty(string property, CustomControl customControl = null, string label = null)
        {
            _list.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(property, DisplayEntryValueType.Property),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        
        public ListControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl = null, string label = null)
        {
            _list.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(scriptBlock, DisplayEntryValueType.ScriptBlock),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        
        public ListEntryBuilder StartEntry(IEnumerable<string> entrySelectedByType = null, IEnumerable<DisplayEntry> entrySelectedByCondition = null)
        {
            var listEntry = new ListControlEntry
            {
                EntrySelectedBy = EntrySelectedBy.Get(entrySelectedByType, entrySelectedByCondition)
            };
            _list.Entries.Add(listEntry);
            return new ListEntryBuilder(this, listEntry);
        }

        
        public ListControl EndList()
        {
            return _list;
        }
    }
}
