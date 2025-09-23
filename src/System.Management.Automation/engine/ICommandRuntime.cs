// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Management.Automation.Host;

namespace System.Management.Automation
{
    
    public interface ICommandRuntime
    {
        
        PSHost? Host { get; }
        #region Write
        
        void WriteDebug(string text);

        
        void WriteError(ErrorRecord errorRecord);

        
        void WriteObject(object? sendToPipeline);

        
        void WriteObject(object? sendToPipeline, bool enumerateCollection);

        
        void WriteProgress(ProgressRecord progressRecord);

        
        void WriteProgress(Int64 sourceId, ProgressRecord progressRecord);

        
        void WriteVerbose(string text);

        
        void WriteWarning(string text);

        
        void WriteCommandDetail(string text);

        #endregion Write

        #region Should
        
        bool ShouldProcess(string? target);

        
        bool ShouldProcess(string? target, string? action);

        
        bool ShouldProcess(string? verboseDescription, string? verboseWarning, string? caption);

        
        bool ShouldProcess(string? verboseDescription, string? verboseWarning, string? caption, out ShouldProcessReason shouldProcessReason);

        
        bool ShouldContinue(string? query, string? caption);

        
        bool ShouldContinue(string? query, string? caption, ref bool yesToAll, ref bool noToAll);

        #endregion Should

        #region Transaction Support
        
        bool TransactionAvailable();

        
        PSTransactionContext? CurrentPSTransaction { get; }
        #endregion Transaction Support

        #region Misc
        #region ThrowTerminatingError
        
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        void ThrowTerminatingError(ErrorRecord errorRecord);
        #endregion ThrowTerminatingError
        #endregion misc

    }

    
    public interface ICommandRuntime2 : ICommandRuntime
    {
        
        void WriteInformation(InformationRecord informationRecord);

        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        bool ShouldContinue(string? query, string? caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll);
    }
}
