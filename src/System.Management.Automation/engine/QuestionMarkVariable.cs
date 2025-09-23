// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    internal class QuestionMarkVariable : PSVariable
    {
        
        /// <param name="context">
        /// Execution context
        /// </param>
        internal QuestionMarkVariable(ExecutionContext context)
            : base(SpecialVariables.Question, true, ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope, RunspaceInit.DollarHookDescription)
        {
            _context = context;
        }

        private readonly ExecutionContext _context;

        
        public override object Value
        {
            get
            {
                DebuggerCheckVariableRead();
                return _context.QuestionMarkVariableValue;
            }

            set
            {
                // Call base's setter to force an error (because the variable is readonly).
                base.Value = value;
            }
        }
    }
}
