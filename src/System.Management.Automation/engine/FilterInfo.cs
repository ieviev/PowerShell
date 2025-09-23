// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    public class FilterInfo : FunctionInfo
    {
        #region ctor

        
        internal FilterInfo(string name, ScriptBlock filter, ExecutionContext context) : this(name, filter, context, null)
        {
        }

        
        internal FilterInfo(string name, ScriptBlock filter, ExecutionContext context, string helpFile)
            : base(name, filter, context, helpFile)
        {
            SetCommandType(CommandTypes.Filter);
        }

        
        internal FilterInfo(string name, ScriptBlock filter, ScopedItemOptions options, ExecutionContext context) : this(name, filter, options, context, null)
        {
        }

        
        internal FilterInfo(string name, ScriptBlock filter, ScopedItemOptions options, ExecutionContext context, string helpFile)
            : base(name, filter, options, context, helpFile)
        {
            SetCommandType(CommandTypes.Filter);
        }

        
        internal FilterInfo(FilterInfo other)
            : base(other)
        {
        }

        
        internal FilterInfo(string name, FilterInfo other)
            : base(name, other)
        {
        }

        
        internal override CommandInfo CreateGetCommandCopy(object[] arguments)
        {
            FilterInfo copy = new FilterInfo(this);
            copy.IsGetCommandCopy = true;
            copy.Arguments = arguments;
            return copy;
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Filter; }
        }
    }
}
