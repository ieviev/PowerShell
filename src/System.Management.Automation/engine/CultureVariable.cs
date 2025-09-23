// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    internal class PSCultureVariable : PSVariable
    {
        
        internal PSCultureVariable()
            : base(SpecialVariables.PSCulture, true, ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope,
                   RunspaceInit.DollarPSCultureDescription)
        {
        }

        
        public override object Value
        {
            get
            {
                DebuggerCheckVariableRead();
                return System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            }
        }
    }

    
    internal class PSUICultureVariable : PSVariable
    {
        
        internal PSUICultureVariable()
            : base(SpecialVariables.PSUICulture, true, ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope,
                   RunspaceInit.DollarPSUICultureDescription)
        {
        }

        
        public override object Value
        {
            get
            {
                DebuggerCheckVariableRead();
                return System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
            }
        }
    }
}
