// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    public class FilterInfo : FunctionInfo
    {
        #region ctor

        
        /// <param name="name">
        /// The name of the filter.
        /// </param>
        /// <param name="filter">
        /// The ScriptBlock for the filter
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the filter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="filter"/> is null.
        /// </exception>
        internal FilterInfo(string name, ScriptBlock filter, ExecutionContext context) : this(name, filter, context, null)
        {
        }

        
        /// <param name="name">
        /// The name of the filter.
        /// </param>
        /// <param name="filter">
        /// The ScriptBlock for the filter
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the filter.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the filter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="filter"/> is null.
        /// </exception>
        internal FilterInfo(string name, ScriptBlock filter, ExecutionContext context, string helpFile)
            : base(name, filter, context, helpFile)
        {
            SetCommandType(CommandTypes.Filter);
        }

        
        /// <param name="name">
        /// The name of the filter.
        /// </param>
        /// <param name="filter">
        /// The ScriptBlock for the filter
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the filter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="filter"/> is null.
        /// </exception>
        internal FilterInfo(string name, ScriptBlock filter, ScopedItemOptions options, ExecutionContext context) : this(name, filter, options, context, null)
        {
        }

        
        /// <param name="name">
        /// The name of the filter.
        /// </param>
        /// <param name="filter">
        /// The ScriptBlock for the filter
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the filter.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the filter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="filter"/> is null.
        /// </exception>
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
