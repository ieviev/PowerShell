// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if UNIX

using System.Diagnostics.Eventing;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Text;
using System.Collections.Generic;

namespace System.Management.Automation.Tracing
{
    
    internal class PSSysLogProvider : LogProvider
    {
        private static readonly SysLogProvider s_provider;

        // by default, do not include channel bits
        internal const PSKeyword DefaultKeywords = (PSKeyword)(0x00FFFFFFFFFFFFFF);

        // the default enabled channel(s)
        internal const PSChannel DefaultChannels = PSChannel.Operational;

        
        static PSSysLogProvider()
        {
            s_provider = new SysLogProvider(PowerShellConfig.Instance.GetSysLogIdentity(),
                                            PowerShellConfig.Instance.GetLogLevel(),
                                            PowerShellConfig.Instance.GetLogKeywords(),
                                            PowerShellConfig.Instance.GetLogChannels());
        }

        
        [ThreadStatic]
        private static StringBuilder t_payloadBuilder;

        private static StringBuilder PayloadBuilder
        {
            get
            {
                // NOTE: Thread static fields must be explicitly initialized for each thread.
                t_payloadBuilder ??= new StringBuilder(200);

                return t_payloadBuilder;
            }
        }

        
        internal bool IsEnabled(PSLevel level, PSKeyword keywords)
        {
            return s_provider.IsEnabled(level, keywords);
        }

        
        internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<string, string> additionalInfo)
        {
            StringBuilder payload = PayloadBuilder;
            payload.Clear();

            AppendException(payload, exception);
            payload.AppendLine();
            AppendAdditionalInfo(payload, additionalInfo);

            WriteEvent(PSEventId.Engine_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        
        internal override void LogAmsiUtilStateEvent(string state, string context)
        {
            WriteEvent(PSEventId.Amsi_Init, PSChannel.Analytic, PSOpcode.Method, PSLevel.Informational, PSTask.Amsi, (PSKeyword)0x0, state, context);
        }

        
        internal override void LogWDACQueryEvent(
            string queryName,
            string fileName,
            int querySuccess,
            int queryResult)
        {
            WriteEvent(PSEventId.WDAC_Query, PSChannel.Analytic, PSOpcode.Method, PSLevel.Informational, PSTask.WDAC, (PSKeyword)0x0, queryName, fileName, querySuccess, queryResult);
        }

        
        internal override void LogWDACAuditEvent(
            string title,
            string message,
            string fqid)
        {
            WriteEvent(PSEventId.WDAC_Audit, PSChannel.Operational, PSOpcode.Method, PSLevel.Informational, PSTask.WDAC, (PSKeyword)0x0, title, message, fqid);
        }

        
        internal override void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                StringBuilder payload = PayloadBuilder;
                payload.Clear();

                payload.AppendLine(StringUtil.Format(EtwLoggingStrings.EngineStateChange, previousState.ToString(), newState.ToString()));

                PSTask task = PSTask.EngineStart;

                if (newState == EngineState.Stopped ||
                    newState == EngineState.OutOfService ||
                    newState == EngineState.None ||
                    newState == EngineState.Degraded)
                {
                    task = PSTask.EngineStop;
                }

                WriteEvent(PSEventId.Engine_Lifecycle, PSChannel.Analytic, PSOpcode.Method, task, logContext, payload.ToString());
            }
        }

        
        internal override void LogCommandHealthEvent(LogContext logContext, Exception exception)
        {
            StringBuilder payload = PayloadBuilder;
            payload.Clear();

            AppendException(payload, exception);

            WriteEvent(PSEventId.Command_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        
        internal override void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                LogContext logContext = getLogContext();
                StringBuilder payload = PayloadBuilder;
                payload.Clear();

                if (logContext.CommandType != null)
                {
                    if (logContext.CommandType.Equals(StringLiterals.Script, StringComparison.OrdinalIgnoreCase))
                    {
                        payload.AppendLine(StringUtil.Format(EtwLoggingStrings.ScriptStateChange, newState.ToString()));
                    }
                    else
                    {
                        payload.AppendLine(StringUtil.Format(EtwLoggingStrings.CommandStateChange, logContext.CommandName, newState.ToString()));
                    }
                }

                PSTask task = PSTask.CommandStart;

                if (newState == CommandState.Stopped ||
                    newState == CommandState.Terminated)
                {
                    task = PSTask.CommandStop;
                }

                WriteEvent(PSEventId.Command_Lifecycle, PSChannel.Analytic, PSOpcode.Method, task, logContext, payload.ToString());
            }
        }

        
        internal override void LogPipelineExecutionDetailEvent(LogContext logContext, List<string> pipelineExecutionDetail)
        {
            StringBuilder payload = PayloadBuilder;
            payload.Clear();

            if (pipelineExecutionDetail != null)
            {
                foreach (string detail in pipelineExecutionDetail)
                {
                    payload.AppendLine(detail);
                }
            }

            WriteEvent(PSEventId.Pipeline_Detail, PSChannel.Operational, PSOpcode.Method, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        
        internal override void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception)
        {
            StringBuilder payload = PayloadBuilder;
            payload.Clear();

            AppendException(payload, exception);
            payload.AppendLine();

            Dictionary<string, string> additionalInfo = new Dictionary<string, string>();

            additionalInfo.Add(EtwLoggingStrings.ProviderNameString, providerName);

            AppendAdditionalInfo(payload, additionalInfo);

            WriteEvent(PSEventId.Provider_Health, PSChannel.Operational, PSOpcode.Exception, PSTask.ExecutePipeline, logContext, payload.ToString());
        }

        
        internal override void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                StringBuilder payload = PayloadBuilder;
                payload.Clear();

                payload.AppendLine(StringUtil.Format(EtwLoggingStrings.ProviderStateChange, providerName, newState.ToString()));

                PSTask task = PSTask.ProviderStart;

                if (newState == ProviderState.Stopped)
                {
                    task = PSTask.ProviderStop;
                }

                WriteEvent(PSEventId.Provider_Lifecycle, PSChannel.Analytic, PSOpcode.Method, task, logContext, payload.ToString());
            }
        }

        
        internal override void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue)
        {
            if (IsEnabled(PSLevel.Informational, PSKeyword.Cmdlets | PSKeyword.UseAlwaysAnalytic))
            {
                StringBuilder payload = PayloadBuilder;
                payload.Clear();

                if (previousValue == null)
                {
                    payload.AppendLine(StringUtil.Format(EtwLoggingStrings.SettingChangeNoPrevious, variableName, value));
                }
                else
                {
                    payload.AppendLine(StringUtil.Format(EtwLoggingStrings.SettingChange, variableName, previousValue, value));
                }

                WriteEvent(PSEventId.Settings, PSChannel.Analytic, PSOpcode.Method, PSTask.ExecutePipeline, logContext, payload.ToString());
            }
        }

        
        internal override bool UseLoggingVariables()
        {
            return false;
        }

        
        internal void WriteEvent(PSEventId id, PSChannel channel, PSOpcode opcode, PSTask task, LogContext logContext, string payLoad)
        {
            s_provider.Log(id, channel, task, opcode, GetPSLevelFromSeverity(logContext.Severity), DefaultKeywords,
                           LogContextToString(logContext),
                           GetPSLogUserData(logContext.ExecutionContext),
                           payLoad);
        }

        
        internal void WriteEvent(PSEventId id, PSChannel channel, PSOpcode opcode, PSLevel level, PSTask task, PSKeyword keyword, params object[] args)
        {
            s_provider.Log(id, channel, task, opcode, level, keyword, args);
        }

        
        internal void WriteTransferEvent(Guid parentActivityId)
        {
            s_provider.LogTransfer(parentActivityId);
        }

        
        internal void SetActivityIdForCurrentThread(Guid newActivityId)
        {
            s_provider.SetActivity(newActivityId);
        }
    }
}

#endif // UNIX
