// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Host;

namespace System.Management.Automation
{
    
    internal class DefaultCommandRuntime : ICommandRuntime2
    {
        private readonly List<object> _output;
        
        public DefaultCommandRuntime(List<object> outputList)
        {
            ArgumentNullException.ThrowIfNull(outputList);

            _output = outputList;
        }

        
        public PSHost Host { get; set; }

        #region Write
        
        public void WriteDebug(string text) { }

        
        public void WriteError(ErrorRecord errorRecord)
        {
            if (errorRecord.Exception != null)
                throw errorRecord.Exception;
            else
                throw new InvalidOperationException(errorRecord.ToString());
        }

        
        public void WriteObject(object sendToPipeline)
        {
            _output.Add(sendToPipeline);
        }

        
        public void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            if (enumerateCollection)
            {
                IEnumerator e = LanguagePrimitives.GetEnumerator(sendToPipeline);
                if (e == null)
                {
                    _output.Add(sendToPipeline);
                }
                else
                {
                    while (e.MoveNext())
                    {
                        _output.Add(e.Current);
                    }
                }
            }
            else
            {
                _output.Add(sendToPipeline);
            }
        }

        
        public void WriteProgress(ProgressRecord progressRecord) { }

        
        public void WriteProgress(Int64 sourceId, ProgressRecord progressRecord) { }

        
        public void WriteVerbose(string text) { }

        
        public void WriteWarning(string text) { }

        
        public void WriteCommandDetail(string text) { }

        
        public void WriteInformation(InformationRecord informationRecord) { }

        #endregion Write

        #region Should
        
        public bool ShouldProcess(string target) { return true; }

        
        public bool ShouldProcess(string target, string action) { return true; }

        
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption) { return true; }

        
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason) { shouldProcessReason = ShouldProcessReason.None; return true; }

        
        public bool ShouldContinue(string query, string caption) { return true; }

        
        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll) { return true; }

        
        public bool ShouldContinue(string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll) { return true; }

        #endregion Should

        #region Transaction Support
        
        public bool TransactionAvailable() { return false; }

        
        public PSTransactionContext CurrentPSTransaction
        {
            get
            {
                string error = TransactionStrings.CmdletRequiresUseTx;

                // We want to throw in this situation, and want to use a
                // property because it mimics the C# using(TransactionScope ...) syntax
#pragma warning suppress 56503
                throw new InvalidOperationException(error);
            }
        }
        #endregion Transaction Support

        #region Misc
        
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            if (errorRecord.Exception != null)
            {
                throw errorRecord.Exception;
            }
            else
            {
                throw new System.InvalidOperationException(errorRecord.ToString());
            }
        }
        #endregion
    }
}
