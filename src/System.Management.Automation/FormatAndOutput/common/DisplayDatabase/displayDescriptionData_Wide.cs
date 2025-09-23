// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// this file contains the data structures for the in memory database
// containing display and formatting information

using System.Collections.Generic;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region Wide View Definitions

    
    internal sealed class WideControlBody : ControlBody
    {
        
        internal int columns = 0;

        
        internal WideControlEntryDefinition defaultEntryDefinition = null;

        
        internal List<WideControlEntryDefinition> optionalEntryList = new List<WideControlEntryDefinition>();
    }

    
    internal sealed class WideControlEntryDefinition
    {
        
        internal AppliesTo appliesTo = null;

        
        internal List<FormatToken> formatTokenList = new List<FormatToken>();
    }

    #endregion
}

namespace System.Management.Automation
{
    
    public sealed class WideControl : PSControl
    {
        
        public List<WideControlEntryItem> Entries { get; internal set; }

        
        public bool AutoSize { get; set; }

        
        public uint Columns { get; internal set; }

        
        public static WideControlBuilder Create(bool outOfBand = false, bool autoSize = false, uint columns = 0)
        {
            var control = new WideControl { OutOfBand = false, AutoSize = autoSize, Columns = columns };
            return new WideControlBuilder(control);
        }

        internal override void WriteToXml(FormatXmlWriter writer)
        {
            writer.WriteWideControl(this);
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

        
        public WideControl()
        {
            Entries = new List<WideControlEntryItem>();
        }

        internal WideControl(WideControlBody widecontrolbody, ViewDefinition viewDefinition) : this()
        {
            OutOfBand = viewDefinition.outOfBand;
            GroupBy = PSControlGroupBy.Get(viewDefinition.groupBy);

            AutoSize = widecontrolbody.autosize.GetValueOrDefault();
            Columns = (uint)widecontrolbody.columns;

            Entries.Add(new WideControlEntryItem(widecontrolbody.defaultEntryDefinition));

            foreach (WideControlEntryDefinition definition in widecontrolbody.optionalEntryList)
            {
                Entries.Add(new WideControlEntryItem(definition));
            }
        }

        
        public WideControl(IEnumerable<WideControlEntryItem> wideEntries) : this()
        {
            if (wideEntries == null)
                throw PSTraceSource.NewArgumentNullException(nameof(wideEntries));

            foreach (WideControlEntryItem entryItem in wideEntries)
            {
                this.Entries.Add(entryItem);
            }
        }

        
        public WideControl(IEnumerable<WideControlEntryItem> wideEntries, uint columns) : this()
        {
            if (wideEntries == null)
                throw PSTraceSource.NewArgumentNullException(nameof(wideEntries));

            foreach (WideControlEntryItem entryItem in wideEntries)
            {
                this.Entries.Add(entryItem);
            }

            this.Columns = columns;
        }

        
        public WideControl(uint columns) : this()
        {
            this.Columns = columns;
        }
    }

    
    public sealed class WideControlEntryItem
    {
        
        public DisplayEntry DisplayEntry { get; internal set; }

        
        public List<string> SelectedBy
        {
            get
            {
                EntrySelectedBy ??= new EntrySelectedBy { TypeNames = new List<string>() };
                return EntrySelectedBy.TypeNames;
            }
        }

        
        public EntrySelectedBy EntrySelectedBy { get; internal set; }

        
        public string FormatString { get; internal set; }

        internal WideControlEntryItem()
        {
        }

        internal WideControlEntryItem(WideControlEntryDefinition definition) : this()
        {
            if (definition.formatTokenList[0] is FieldPropertyToken fpt)
            {
                DisplayEntry = new DisplayEntry(fpt.expression);
                FormatString = fpt.fieldFormattingDirective.formatString;
            }

            if (definition.appliesTo != null)
            {
                EntrySelectedBy = EntrySelectedBy.Get(definition.appliesTo.referenceList);
            }
        }

        
        public WideControlEntryItem(DisplayEntry entry) : this()
        {
            if (entry == null)
                throw PSTraceSource.NewArgumentNullException(nameof(entry));
            this.DisplayEntry = entry;
        }

        
        public WideControlEntryItem(DisplayEntry entry, IEnumerable<string> selectedBy) : this()
        {
            if (entry == null)
                throw PSTraceSource.NewArgumentNullException(nameof(entry));
            if (selectedBy == null)
                throw PSTraceSource.NewArgumentNullException(nameof(selectedBy));

            this.DisplayEntry = entry;
            this.EntrySelectedBy = EntrySelectedBy.Get(selectedBy, null);
        }

        internal bool SafeForExport()
        {
            return DisplayEntry.SafeForExport() && (EntrySelectedBy == null || EntrySelectedBy.SafeForExport());
        }

        internal bool CompatibleWithOldPowerShell()
        {
            // Old versions of PowerShell don't know anything about FormatString or conditions in EntrySelectedBy.
            return FormatString == null &&
                   (EntrySelectedBy == null || EntrySelectedBy.CompatibleWithOldPowerShell());
        }
    }

    public sealed class WideControlBuilder
    {
        private readonly WideControl _control;

        internal WideControlBuilder(WideControl control)
        {
            _control = control;
        }

        
        public WideControlBuilder GroupByProperty(string property, CustomControl customControl = null, string label = null)
        {
            _control.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(property, DisplayEntryValueType.Property),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        
        public WideControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl = null, string label = null)
        {
            _control.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(scriptBlock, DisplayEntryValueType.ScriptBlock),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        public WideControlBuilder AddScriptBlockEntry(string scriptBlock, string format = null, IEnumerable<string> entrySelectedByType = null, IEnumerable<DisplayEntry> entrySelectedByCondition = null)
        {
            var entry = new WideControlEntryItem(new DisplayEntry(scriptBlock, DisplayEntryValueType.ScriptBlock))
            {
                EntrySelectedBy = EntrySelectedBy.Get(entrySelectedByType, entrySelectedByCondition)
            };
            _control.Entries.Add(entry);
            return this;
        }

        public WideControlBuilder AddPropertyEntry(string propertyName, string format = null, IEnumerable<string> entrySelectedByType = null, IEnumerable<DisplayEntry> entrySelectedByCondition = null)
        {
            var entry = new WideControlEntryItem(new DisplayEntry(propertyName, DisplayEntryValueType.Property))
            {
                EntrySelectedBy = EntrySelectedBy.Get(entrySelectedByType, entrySelectedByCondition)
            };
            _control.Entries.Add(entry);
            return this;
        }

        public WideControl EndWideControl()
        {
            return _control;
        }
    }
}
