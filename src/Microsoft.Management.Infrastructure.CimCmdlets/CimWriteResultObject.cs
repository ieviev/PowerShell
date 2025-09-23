// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal sealed class CimWriteResultObject : CimBaseAction
    {
        
        public CimWriteResultObject(object result, XOperationContextBase theContext)
        {
            this.Result = result;
            this.Context = theContext;
        }

        
        public override void Execute(CmdletOperationBase cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");
            cmdlet.WriteObject(Result, this.Context);
        }

        #region members
        
        internal object Result { get; }
        #endregion
    }
}
