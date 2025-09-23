// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;
using System.Text;

namespace System.Management.Automation
{
    
    public class FunctionInfo : CommandInfo, IScriptCommandInfo
    {
        #region ctor

        
        internal FunctionInfo(string name, ScriptBlock function, ExecutionContext context) : this(name, function, context, null)
        {
        }

        
        internal FunctionInfo(string name, ScriptBlock function, ExecutionContext context, string helpFile) : base(name, CommandTypes.Function, context)
        {
            if (function == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(function));
            }

            _scriptBlock = function;

            CmdletInfo.SplitCmdletName(name, out _verb, out _noun);

            this.Module = function.Module;
            _helpFile = helpFile;
        }

        
        internal FunctionInfo(string name, ScriptBlock function, ScopedItemOptions options, ExecutionContext context) : this(name, function, options, context, null)
        {
        }

        
        internal FunctionInfo(string name, ScriptBlock function, ScopedItemOptions options, ExecutionContext context, string helpFile)
            : this(name, function, context, helpFile)
        {
            _options = options;
        }

        
        internal FunctionInfo(FunctionInfo other)
            : base(other)
        {
            CopyFieldsFromOther(other);
        }

        private void CopyFieldsFromOther(FunctionInfo other)
        {
            _verb = other._verb;
            _noun = other._noun;
            _scriptBlock = other._scriptBlock;
            _description = other._description;
            _options = other._options;
            _helpFile = other._helpFile;
        }

        
        internal FunctionInfo(string name, FunctionInfo other)
            : base(name, other)
        {
            CopyFieldsFromOther(other);

            // Get the verb and noun from the name
            CmdletInfo.SplitCmdletName(name, out _verb, out _noun);
        }

        
        internal override CommandInfo CreateGetCommandCopy(object[] arguments)
        {
            FunctionInfo copy = new FunctionInfo(this) { IsGetCommandCopy = true, Arguments = arguments };
            return copy;
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Function; }
        }

        
        public ScriptBlock ScriptBlock
        {
            get { return _scriptBlock; }
        }

        private ScriptBlock _scriptBlock;

        
        internal void Update(ScriptBlock newFunction, bool force, ScopedItemOptions options)
        {
            Update(newFunction, force, options, null);
            this.DefiningLanguageMode = newFunction.LanguageMode;
        }

        protected internal virtual void Update(FunctionInfo newFunction, bool force, ScopedItemOptions options, string helpFile)
        {
            Update(newFunction.ScriptBlock, force, options, helpFile);
        }

        
        internal void Update(ScriptBlock newFunction, bool force, ScopedItemOptions options, string helpFile)
        {
            if (newFunction == null)
            {
                throw PSTraceSource.NewArgumentNullException("function");
            }

            if ((_options & ScopedItemOptions.Constant) != 0)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Function,
                            "FunctionIsConstant",
                            SessionStateStrings.FunctionIsConstant);

                throw e;
            }

            if (!force && (_options & ScopedItemOptions.ReadOnly) != 0)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Function,
                            "FunctionIsReadOnly",
                            SessionStateStrings.FunctionIsReadOnly);

                throw e;
            }

            _scriptBlock = newFunction;

            this.Module = newFunction.Module;
            _commandMetadata = null;
            this._parameterSets = null;
            this.ExternalCommandMetadata = null;

            if (options != ScopedItemOptions.Unspecified)
            {
                this.Options = options;
            }

            _helpFile = helpFile;
        }

        
        public bool CmdletBinding
        {
            get
            {
                return this.ScriptBlock.UsesCmdletBinding;
            }
        }

        
        public string DefaultParameterSet
        {
            get
            {
                return this.CmdletBinding ? this.CommandMetadata.DefaultParameterSetName : null;
            }
        }

        
        public override string Definition { get { return _scriptBlock.ToString(); } }

        
        public ScopedItemOptions Options
        {
            get
            {
                return CopiedCommand == null ? _options : ((FunctionInfo)CopiedCommand).Options;
            }

            set
            {
                if (CopiedCommand == null)
                {
                    // Check to see if the function is constant, if so
                    // throw an exception because the options cannot be changed.

                    if ((_options & ScopedItemOptions.Constant) != 0)
                    {
                        SessionStateUnauthorizedAccessException e =
                            new SessionStateUnauthorizedAccessException(
                                    Name,
                                    SessionStateCategory.Function,
                                    "FunctionIsConstant",
                                    SessionStateStrings.FunctionIsConstant);

                        throw e;
                    }

                    // Now check to see if the caller is trying to set
                    // the options to constant. This is only allowed at
                    // variable creation

                    if ((value & ScopedItemOptions.Constant) != 0)
                    {
                        // user is trying to set the function to constant after
                        // creating the function. Do not allow this (as per spec).

                        SessionStateUnauthorizedAccessException e =
                            new SessionStateUnauthorizedAccessException(
                                    Name,
                                    SessionStateCategory.Function,
                                    "FunctionCannotBeMadeConstant",
                                    SessionStateStrings.FunctionCannotBeMadeConstant);

                        throw e;
                    }

                    // Ensure we are not trying to remove the AllScope option

                    if ((value & ScopedItemOptions.AllScope) == 0 &&
                        (_options & ScopedItemOptions.AllScope) != 0)
                    {
                        SessionStateUnauthorizedAccessException e =
                            new SessionStateUnauthorizedAccessException(
                                    this.Name,
                                    SessionStateCategory.Function,
                                    "FunctionAllScopeOptionCannotBeRemoved",
                                    SessionStateStrings.FunctionAllScopeOptionCannotBeRemoved);

                        throw e;
                    }

                    _options = value;
                }
                else
                {
                    ((FunctionInfo)CopiedCommand).Options = value;
                }
            }
        }

        private ScopedItemOptions _options = ScopedItemOptions.None;

        
        public string Description
        {
            get
            {
                return CopiedCommand == null ? _description : ((FunctionInfo)CopiedCommand).Description;
            }

            set
            {
                if (CopiedCommand == null)
                {
                    _description = value;
                }
                else
                {
                    ((FunctionInfo)CopiedCommand).Description = value;
                }
            }
        }

        private string _description = null;

        
        public string Verb
        {
            get
            {
                return _verb;
            }
        }

        private string _verb = string.Empty;

        
        public string Noun
        {
            get
            {
                return _noun;
            }
        }

        private string _noun = string.Empty;

        
        public string HelpFile
        {
            get
            {
                return _helpFile;
            }

            internal set
            {
                _helpFile = value;
            }
        }

        private string _helpFile = string.Empty;

        
        internal override string Syntax
        {
            get
            {
                StringBuilder synopsis = new StringBuilder();

                foreach (CommandParameterSetInfo parameterSet in ParameterSets)
                {
                    synopsis.AppendLine();
                    synopsis.AppendLine(
                        string.Format(
                            Globalization.CultureInfo.CurrentCulture,
                            "{0} {1}",
                            Name,
                            parameterSet.ToString()));
                }

                return synopsis.ToString();
            }
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

        
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get { return ScriptBlock.OutputType; }
        }
    }
}
