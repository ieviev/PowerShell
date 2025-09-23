// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using Microsoft.Management.Infrastructure.Options;
#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal sealed class CimPromptUser : CimSyncAction
    {
        
        public CimPromptUser(string message,
            CimPromptType prompt)
        {
            this.Message = message;
            this.prompt = prompt;
        }

        
        /// <param name="cmdlet">
        /// cmdlet wrapper object, to which write result.
        /// <see cref="CmdletOperationBase"/> for details.
        /// </param>
        public override void Execute(CmdletOperationBase cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");

            bool yestoall = false;
            bool notoall = false;
            bool result = false;

            switch (this.prompt)
            {
                case CimPromptType.Critical:
                    // NOTES: prepare the whatif message and caption
                    try
                    {
                        result = cmdlet.ShouldContinue(Message, "caption", ref yestoall, ref notoall);
                        if (yestoall)
                        {
                            this.responseType = CimResponseType.YesToAll;
                        }
                        else if (notoall)
                        {
                            this.responseType = CimResponseType.NoToAll;
                        }
                        else if (result)
                        {
                            this.responseType = CimResponseType.Yes;
                        }
                        else if (!result)
                        {
                            this.responseType = CimResponseType.No;
                        }
                    }
                    catch
                    {
                        this.responseType = CimResponseType.NoToAll;
                        throw;
                    }
                    finally
                    {
                        // unblocking the waiting thread
                        this.OnComplete();
                    }

                    break;
                case CimPromptType.Normal:
                    try
                    {
                        result = cmdlet.ShouldProcess(Message);
                        if (result)
                        {
                            this.responseType = CimResponseType.Yes;
                        }
                        else if (!result)
                        {
                            this.responseType = CimResponseType.No;
                        }
                    }
                    catch
                    {
                        this.responseType = CimResponseType.NoToAll;
                        throw;
                    }
                    finally
                    {
                        // unblocking the waiting thread
                        this.OnComplete();
                    }

                    break;
                default:
                    break;
            }

            this.OnComplete();
        }

        #region members

        
        public string Message { get; }

        
        private readonly CimPromptType prompt;

        #endregion
    }
}
