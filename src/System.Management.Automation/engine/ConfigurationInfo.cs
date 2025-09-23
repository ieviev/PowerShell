// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    public class ConfigurationInfo : FunctionInfo
    {
        #region ctor

        
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the configuration.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        internal ConfigurationInfo(string name, ScriptBlock configuration, ExecutionContext context) : this(name, configuration, context, null)
        {
        }

        
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the configuration.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the configuration.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        internal ConfigurationInfo(string name, ScriptBlock configuration, ExecutionContext context, string helpFile)
            : base(name, configuration, context, helpFile)
        {
            SetCommandType(CommandTypes.Configuration);
        }

        
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the configuration.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        internal ConfigurationInfo(string name, ScriptBlock configuration, ScopedItemOptions options, ExecutionContext context) : this(name, configuration, options, context, null)
        {
        }

        
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the configuration.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the configuration.
        /// </param>
        /// <param name="isMetaConfig">The configuration is a meta configuration.</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        internal ConfigurationInfo(string name, ScriptBlock configuration, ScopedItemOptions options, ExecutionContext context, string helpFile, bool isMetaConfig)
            : base(name, configuration, options, context, helpFile)
        {
            SetCommandType(CommandTypes.Configuration);
            IsMetaConfiguration = isMetaConfig;
        }

        
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the configuration.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the configuration.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
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
