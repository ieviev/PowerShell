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
        
        /// <param name="text">Text to write.</param>
        public void WriteDebug(string text) { }

        
        /// <param name="errorRecord">Error record instance to process.</param>
        public void WriteError(ErrorRecord errorRecord)
        {
            if (errorRecord.Exception != null)
                throw errorRecord.Exception;
            else
                throw new InvalidOperationException(errorRecord.ToString());
        }

        
        /// <param name="sendToPipeline">Object to write.</param>
        public void WriteObject(object sendToPipeline)
        {
            _output.Add(sendToPipeline);
        }

        
        /// <param name="sendToPipeline">Object to write.</param>
        /// <param name="enumerateCollection">If true, the collection is enumerated, otherwise
        /// it's written as a scalar.
        /// </param>
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

        
        /// <param name="progressRecord">Progress record to write.</param>
        public void WriteProgress(ProgressRecord progressRecord) { }

        
        /// <param name="sourceId">Source ID to write for.</param>
        /// <param name="progressRecord">Record to write.</param>
        public void WriteProgress(Int64 sourceId, ProgressRecord progressRecord) { }

        
        /// <param name="text">Text to write.</param>
        public void WriteVerbose(string text) { }

        
        /// <param name="text">Text to write.</param>
        public void WriteWarning(string text) { }

        
        /// <param name="text">Text to write.</param>
        public void WriteCommandDetail(string text) { }

        
        /// <param name="informationRecord">Record to write.</param>
        public void WriteInformation(InformationRecord informationRecord) { }

        #endregion Write

        #region Should
        
        /// <param name="target">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldProcess(string target) { return true; }

        
        /// <param name="target">Ignored.</param>
        /// <param name="action">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldProcess(string target, string action) { return true; }

        
        /// <param name="verboseDescription">Ignored.</param>
        /// <param name="verboseWarning">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption) { return true; }

        
        /// <param name="verboseDescription">Ignored.</param>
        /// <param name="verboseWarning">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <param name="shouldProcessReason">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason) { shouldProcessReason = ShouldProcessReason.None; return true; }

        
        /// <param name="query">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldContinue(string query, string caption) { return true; }

        
        /// <param name="query">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <param name="yesToAll">Ignored.</param>
        /// <param name="noToAll">Ignored.</param>
        /// <returns>True.</returns>
        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll) { return true; }

        
        /// <param name="query">Ignored.</param>
        /// <param name="caption">Ignored.</param>
        /// <param name="hasSecurityImpact">Ignored.</param>
        /// <param name="yesToAll">Ignored.</param>
        /// <param name="noToAll">Ignored.</param>
        /// <returns>True.</returns>
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
        
        /// <param name="errorRecord">The error record to throw.</param>
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
