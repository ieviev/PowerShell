// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Management.Automation.Internal;
using System.Threading;

namespace System.Management.Automation
{
    
    public abstract class Cmdlet : InternalCommand
    {
        #region public_properties

        
        public static HashSet<string> CommonParameters
        {
            get
            {
                return s_commonParameters.Value;
            }
        }

        private static readonly Lazy<HashSet<string>> s_commonParameters = new Lazy<HashSet<string>>(
            () =>
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "Verbose", "Debug", "ErrorAction", "WarningAction", "InformationAction", "ProgressAction",
                    "ErrorVariable", "WarningVariable", "OutVariable",
                    "OutBuffer", "PipelineVariable", "InformationVariable" };
            }
        );

        
        public static HashSet<string> OptionalCommonParameters
        {
            get
            {
                return s_optionalCommonParameters.Value;
            }
        }

        private static readonly Lazy<HashSet<string>> s_optionalCommonParameters = new Lazy<HashSet<string>>(
            () =>
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "WhatIf", "Confirm", "UseTransaction" };
            }
        );

        
        public bool Stopping
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return this.IsStopping;
                }
            }
        }

        
        public CancellationToken PipelineStopToken => StopToken;

        
        internal string _ParameterSetName
        {
            get { return _parameterSetName; }
        }

        
        internal void SetParameterSetName(string parameterSetName)
        {
            _parameterSetName = parameterSetName;
        }

        private string _parameterSetName = string.Empty;

        #region Override Internal

        
        internal override void DoBeginProcessing()
        {
            MshCommandRuntime mshRuntime = this.CommandRuntime as MshCommandRuntime;

            if (mshRuntime != null)
            {
                if (mshRuntime.UseTransaction &&
                   (!this.Context.TransactionManager.HasTransaction))
                {
                    string error = TransactionStrings.NoTransactionStarted;

                    if (this.Context.TransactionManager.IsLastTransactionCommitted)
                    {
                        error = TransactionStrings.NoTransactionStartedFromCommit;
                    }
                    else if (this.Context.TransactionManager.IsLastTransactionRolledBack)
                    {
                        error = TransactionStrings.NoTransactionStartedFromRollback;
                    }

                    throw new InvalidOperationException(error);
                }
            }

            this.BeginProcessing();
        }

        
        internal override void DoProcessRecord()
        {
            this.ProcessRecord();
        }

        
        internal override void DoEndProcessing()
        {
            this.EndProcessing();
        }

        
        internal override void DoStopProcessing()
        {
            this.StopProcessing();
        }

        #endregion Override Internal

        #endregion internal_members

        #region ctor

        
        protected Cmdlet()
        {
        }

        #endregion ctor

        #region public_methods

        #region Cmdlet virtuals

        
        public virtual string GetResourceString(string baseName, string resourceId)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (string.IsNullOrEmpty(baseName))
                    throw PSTraceSource.NewArgumentNullException(nameof(baseName));

                if (string.IsNullOrEmpty(resourceId))
                    throw PSTraceSource.NewArgumentNullException(nameof(resourceId));

                ResourceManager manager = ResourceManagerCache.GetResourceManager(this.GetType().Assembly, baseName);
                string retValue = null;

                try
                {
                    retValue = manager.GetString(resourceId, CultureInfo.CurrentUICulture);
                }
                catch (MissingManifestResourceException)
                {
                    throw PSTraceSource.NewArgumentException(nameof(baseName), GetErrorText.ResourceBaseNameFailure, baseName);
                }

                if (retValue == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(resourceId), GetErrorText.ResourceIdFailure, resourceId);
                }

                return retValue;
            }
        }

        #endregion Cmdlet virtuals

        #region Write

        
        public ICommandRuntime CommandRuntime
        {
            get
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    return commandRuntime;
                }
            }

            set
            {
                using (PSTransactionManager.GetEngineProtectionScope())
                {
                    commandRuntime = value;
                }
            }
        }

        
        public void WriteError(ErrorRecord errorRecord)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    commandRuntime.WriteError(errorRecord);
                else
                    throw new System.NotImplementedException("WriteError");
            }
        }
        
        public void WriteObject(object sendToPipeline)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    commandRuntime.WriteObject(sendToPipeline);
                else
                    throw new System.NotImplementedException("WriteObject");
            }
        }
        
        public void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    commandRuntime.WriteObject(sendToPipeline, enumerateCollection);
                else
                    throw new System.NotImplementedException("WriteObject");
            }
        }

        
        public void WriteVerbose(string text)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    commandRuntime.WriteVerbose(text);
                else
                    throw new System.NotImplementedException("WriteVerbose");
            }
        }

        internal bool IsWriteVerboseEnabled()
            => commandRuntime is not MshCommandRuntime mshRuntime || mshRuntime.IsWriteVerboseEnabled();

        
        public void WriteWarning(string text)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    commandRuntime.WriteWarning(text);
                else
                    throw new System.NotImplementedException("WriteWarning");
            }
        }

        internal bool IsWriteWarningEnabled()
            => commandRuntime is not MshCommandRuntime mshRuntime || mshRuntime.IsWriteWarningEnabled();

        
        public void WriteCommandDetail(string text)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    commandRuntime.WriteCommandDetail(text);
                else
                    throw new System.NotImplementedException("WriteCommandDetail");
            }
        }

        
        public void WriteProgress(ProgressRecord progressRecord)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    commandRuntime.WriteProgress(progressRecord);
                else
                    throw new System.NotImplementedException("WriteProgress");
            }
        }

        
        internal void WriteProgress(
            Int64 sourceId,
            ProgressRecord progressRecord)
        {
            if (commandRuntime != null)
                commandRuntime.WriteProgress(sourceId, progressRecord);
            else
                throw new System.NotImplementedException("WriteProgress");
        }

        internal bool IsWriteProgressEnabled()
            => commandRuntime is not MshCommandRuntime mshRuntime || mshRuntime.IsWriteProgressEnabled();

        
        public void WriteDebug(string text)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    commandRuntime.WriteDebug(text);
                else
                    throw new System.NotImplementedException("WriteDebug");
            }
        }

        internal bool IsWriteDebugEnabled()
            => commandRuntime is not MshCommandRuntime mshRuntime || mshRuntime.IsWriteDebugEnabled();

        
        public void WriteInformation(object messageData, string[] tags)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                ICommandRuntime2 commandRuntime2 = commandRuntime as ICommandRuntime2;
                if (commandRuntime2 != null)
                {
                    string source = this.MyInvocation.PSCommandPath;
                    if (string.IsNullOrEmpty(source))
                    {
                        source = this.MyInvocation.MyCommand.Name;
                    }

                    InformationRecord informationRecord = new InformationRecord(messageData, source);

                    if (tags != null)
                    {
                        informationRecord.Tags.AddRange(tags);
                    }

                    commandRuntime2.WriteInformation(informationRecord);
                }
                else
                {
                    throw new System.NotImplementedException("WriteInformation");
                }
            }
        }

        
        public void WriteInformation(InformationRecord informationRecord)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                ICommandRuntime2 commandRuntime2 = commandRuntime as ICommandRuntime2;
                if (commandRuntime2 != null)
                {
                    commandRuntime2.WriteInformation(informationRecord);
                }
                else
                {
                    throw new System.NotImplementedException("WriteInformation");
                }
            }
        }

        internal bool IsWriteInformationEnabled()
            => commandRuntime is not MshCommandRuntime mshRuntime || mshRuntime.IsWriteInformationEnabled();

        #endregion Write

        #region ShouldProcess
        
        public bool ShouldProcess(string target)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    return commandRuntime.ShouldProcess(target);
                else
                    return true;
            }
        }

        
        public bool ShouldProcess(string target, string action)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    return commandRuntime.ShouldProcess(target, action);
                else
                    return true;
            }
        }

        
        public bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    return commandRuntime.ShouldProcess(verboseDescription, verboseWarning, caption);
                else
                    return true;
            }
        }

        
        public bool ShouldProcess(
            string verboseDescription,
            string verboseWarning,
            string caption,
            out ShouldProcessReason shouldProcessReason)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    return commandRuntime.ShouldProcess(verboseDescription, verboseWarning, caption, out shouldProcessReason);
                else
                {
                    shouldProcessReason = ShouldProcessReason.None;
                    return true;
                }
            }
        }

        #endregion ShouldProcess

        #region ShouldContinue
        
        public bool ShouldContinue(string query, string caption)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    return commandRuntime.ShouldContinue(query, caption);
                else
                    return true;
            }
        }

        
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public bool ShouldContinue(
            string query, string caption, ref bool yesToAll, ref bool noToAll)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    return commandRuntime.ShouldContinue(query, caption, ref yesToAll, ref noToAll);
                else
                    return true;
            }
        }

        
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public bool ShouldContinue(
            string query, string caption, bool hasSecurityImpact, ref bool yesToAll, ref bool noToAll)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                {
                    ICommandRuntime2 runtime2 = commandRuntime as ICommandRuntime2;
                    if (runtime2 != null)
                    {
                        return runtime2.ShouldContinue(query, caption, hasSecurityImpact, ref yesToAll, ref noToAll);
                    }
                    else
                    {
                        return commandRuntime.ShouldContinue(query, caption, ref yesToAll, ref noToAll);
                    }
                }
                else
                    return true;
            }
        }

        
        internal List<object> GetResults()
        {
            // Prevent invocation of things that derive from PSCmdlet.
            if (this is PSCmdlet)
            {
                string msg = CommandBaseStrings.CannotInvokePSCmdletsDirectly;

                throw new System.InvalidOperationException(msg);
            }

            var result = new List<object>();
            if (this.commandRuntime == null)
            {
                this.CommandRuntime = new DefaultCommandRuntime(result);
            }

            this.BeginProcessing();
            this.ProcessRecord();
            this.EndProcessing();

            return result;
        }
        
        public IEnumerable Invoke()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                List<object> data = this.GetResults();
                for (int i = 0; i < data.Count; i++)
                    yield return data[i];
            }
        }

        
        public IEnumerable<T> Invoke<T>()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                List<object> data = this.GetResults();
                for (int i = 0; i < data.Count; i++)
                    yield return (T)data[i];
            }
        }

        #endregion ShouldContinue

        #region Transaction Support

        
        public bool TransactionAvailable()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (commandRuntime != null)
                    return commandRuntime.TransactionAvailable();
                else
#pragma warning suppress 56503
                    throw new System.NotImplementedException("TransactionAvailable");
            }
        }

        
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public PSTransactionContext CurrentPSTransaction
        {
            get
            {
                if (commandRuntime != null)
                    return commandRuntime.CurrentPSTransaction;
                else
                    // We want to throw in this situation, and want to use a
                    // property because it mimics the C# using(TransactionScope ...) syntax
                    throw new System.NotImplementedException("CurrentPSTransaction");
            }
        }
        #endregion Transaction Support

        #region ThrowTerminatingError
        
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                ArgumentNullException.ThrowIfNull(errorRecord);

                if (commandRuntime != null)
                {
                    commandRuntime.ThrowTerminatingError(errorRecord);
                }
                else if (errorRecord.Exception != null)
                {
                    throw errorRecord.Exception;
                }
                else
                {
                    throw new System.InvalidOperationException(errorRecord.ToString());
                }
            }
        }
        #endregion ThrowTerminatingError

        #region Exposed API Override

        
        protected virtual void BeginProcessing()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
            }
        }

        
        protected virtual void ProcessRecord()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
            }
        }

        
        protected virtual void EndProcessing()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
            }
        }

        
        protected virtual void StopProcessing()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
            }
        }

        #endregion Exposed API Override

        #endregion public_methods
    }

    
    [Flags]
    public enum ShouldProcessReason
    {
        
        None = 0x0,

        
        WhatIf = 0x1,
    }
}
