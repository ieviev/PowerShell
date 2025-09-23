// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    public class ConfigurationInfo : FunctionInfo
    {
        #region ctor

        
        internal ConfigurationInfo(string name, ScriptBlock configuration, ExecutionContext context) : this(name, configuration, context, null)
        {
        }

        
        internal ConfigurationInfo(string name, ScriptBlock configuration, ExecutionContext context, string helpFile)
            : base(name, configuration, context, helpFile)
        {
            SetCommandType(CommandTypes.Configuration);
        }

        
        internal ConfigurationInfo(string name, ScriptBlock configuration, ScopedItemOptions options, ExecutionContext context) : this(name, configuration, options, context, null)
        {
        }

        
        internal ConfigurationInfo(string name, ScriptBlock configuration, ScopedItemOptions options, ExecutionContext context, string helpFile, bool isMetaConfig)
            : base(name, configuration, options, context, helpFile)
        {
            SetCommandType(CommandTypes.Configuration);
            IsMetaConfiguration = isMetaConfig;
        }

        
        internal ConfigurationInfo(string name, ScriptBlock configuration, ScopedItemOptions options, ExecutionContext context, string helpFile)
            : this(name, configuration, options, context, helpFile, false)
        {
        }

        
        internal ConfigurationInfo(ConfigurationInfo other)
            : base(other)
        {
        }

        
        internal ConfigurationInfo(string name, ConfigurationInfo other)
            : base(name, other)
        {
        }

        
        internal override CommandInfo CreateGetCommandCopy(object[] arguments)
        {
            var copy = new ConfigurationInfo(this) { IsGetCommandCopy = true, Arguments = arguments };
            return copy;
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Configuration; }
        }

        
        public bool IsMetaConfiguration
        { get; internal set; }
    }
}
