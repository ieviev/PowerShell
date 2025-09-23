// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    
    public class ScriptInfo : CommandInfo, IScriptCommandInfo
    {
        #region ctor

        
        internal ScriptInfo(string name, ScriptBlock script, ExecutionContext context)
            : base(name, CommandTypes.Script, context)
        {
            if (script == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(script));
            }

            this.ScriptBlock = script;
        }

        
        internal ScriptInfo(ScriptInfo other)
            : base(other)
        {
            this.ScriptBlock = other.ScriptBlock;
        }

        
        internal override CommandInfo CreateGetCommandCopy(object[] argumentList)
        {
            ScriptInfo copy = new ScriptInfo(this) { IsGetCommandCopy = true, Arguments = argumentList };
            return copy;
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.ScriptCommand; }
        }

        
        public ScriptBlock ScriptBlock { get; }

        // Path

        
        public override string Definition
        {
            get
            {
                return ScriptBlock.ToString();
            }
        }

        
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get { return ScriptBlock.OutputType; }
        }

        
        public override string ToString()
        {
            return ScriptBlock.ToString();
        }

        
        internal override bool ImplementsDynamicParameters
        {
            get { return ScriptBlock.HasDynamicParameters; }
        }

        
        internal override CommandMetadata CommandMetadata
        {
            get
            {
                return _commandMetadata ??=
                    new CommandMetadata(this.ScriptBlock, this.Name, LocalPipeline.GetExecutionContextFromTLS());
            }
        }

        private CommandMetadata _commandMetadata;
    }
}
