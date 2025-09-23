// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Text;

namespace System.Management.Automation
{
    
    internal abstract class LogProvider
    {
        
        internal LogProvider()
        {
        }

        #region Provider api

        
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        internal abstract void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<string, string> additionalInfo);

        
        /// <param name="logContext"></param>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        internal abstract void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState);

        
        /// <param name="logContext"></param>
        /// <param name="exception"></param>
        internal abstract void LogCommandHealthEvent(LogContext logContext, Exception exception);

        
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
        internal abstract void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState);

        
        /// <param name="logContext"></param>
        /// <param name="pipelineExecutionDetail"></param>
        internal abstract void LogPipelineExecutionDetailEvent(LogContext logContext, List<string> pipelineExecutionDetail);

        
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="exception"></param>
        internal abstract void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception);

        
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
        internal abstract void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState);

        
        /// <param name="logContext"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        internal abstract void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue);

        
        /// <param name="state">This the action performed in AmsiUtil class, like init, scan, etc.</param>
        /// <param name="context">The amsiContext handled - Session pair.</param>
        internal abstract void LogAmsiUtilStateEvent(string state, string context);

        
        /// <param name="queryName">Name of the WDAC query.</param>
        /// <param name="fileName">Name of script file for policy query. Can be null value.</param>
        /// <param name="querySuccess">Query call succeed code.</param>
        /// <param name="queryResult">Result code of WDAC query.</param>
        internal abstract void LogWDACQueryEvent(
            string queryName,
            string fileName,
            int querySuccess,
            int queryResult);

        
        /// <param name="title">Title of WDAC audit event.</param>
        /// <param name="message">WDAC audit event message.</param>
        /// <param name="fqid">FullyQualifiedId of WDAC audit event.</param>
        internal abstract void LogWDACAuditEvent(
            string title,
            string message,
            string fqid);

        
        /// <returns></returns>
        internal virtual bool UseLoggingVariables()
        {
            return true;
        }

        #endregion

        #region Shared utilities

        private static class Strings
        {
            // The strings are stored in a different class to defer loading the resources until as late
            // as possible, e.g. if logging is never on, these strings won't be loaded.
            internal static readonly string LogContextSeverity = EtwLoggingStrings.LogContextSeverity;
            internal static readonly string LogContextHostName = EtwLoggingStrings.LogContextHostName;
            internal static readonly string LogContextHostVersion = EtwLoggingStrings.LogContextHostVersion;
            internal static readonly string LogContextHostId = EtwLoggingStrings.LogContextHostId;
            internal static readonly string LogContextHostApplication = EtwLoggingStrings.LogContextHostApplication;
            internal static readonly string LogContextEngineVersion = EtwLoggingStrings.LogContextEngineVersion;
            internal static readonly string LogContextRunspaceId = EtwLoggingStrings.LogContextRunspaceId;
            internal static readonly string LogContextPipelineId = EtwLoggingStrings.LogContextPipelineId;
            internal static readonly string LogContextCommandName = EtwLoggingStrings.LogContextCommandName;
            internal static readonly string LogContextCommandType = EtwLoggingStrings.LogContextCommandType;
            internal static readonly string LogContextScriptName = EtwLoggingStrings.LogContextScriptName;
            internal static readonly string LogContextCommandPath = EtwLoggingStrings.LogContextCommandPath;
            internal static readonly string LogContextSequenceNumber = EtwLoggingStrings.LogContextSequenceNumber;
            internal static readonly string LogContextUser = EtwLoggingStrings.LogContextUser;
            internal static readonly string LogContextConnectedUser = EtwLoggingStrings.LogContextConnectedUser;
            internal static readonly string LogContextTime = EtwLoggingStrings.LogContextTime;
            internal static readonly string LogContextShellId = EtwLoggingStrings.LogContextShellId;
        }

        
        /// <param name="context"></param>
        /// <returns></returns>
        protected static string GetPSLogUserData(ExecutionContext context)
        {
            if (context == null)
            {
                return string.Empty;
            }

            object logData = context.GetVariableValue(SpecialVariables.PSLogUserDataPath);

            if (logData == null)
            {
                return string.Empty;
            }

            return logData.ToString();
        }

        
        /// <param name="sb">String builder.</param>
        /// <param name="except">Exception.</param>
        protected static void AppendException(StringBuilder sb, Exception except)
        {
            sb.AppendLine(StringUtil.Format(EtwLoggingStrings.ErrorRecordMessage, except.Message));

            if (except is IContainsErrorRecord ier)
            {
                ErrorRecord er = ier.ErrorRecord;

                if (er != null)
                {
                    sb.AppendLine(StringUtil.Format(EtwLoggingStrings.ErrorRecordId, er.FullyQualifiedErrorId));

                    ErrorDetails details = er.ErrorDetails;

                    if (details != null)
                    {
                        sb.AppendLine(StringUtil.Format(EtwLoggingStrings.ErrorRecordRecommendedAction, details.RecommendedAction));
                    }
                }
            }
        }

        
        /// <param name="sb">String builder.</param>
        /// <param name="additionalInfo">Additional information.</param>
        protected static void AppendAdditionalInfo(StringBuilder sb, Dictionary<string, string> additionalInfo)
        {
            if (additionalInfo != null)
            {
                foreach (KeyValuePair<string, string> value in additionalInfo)
                {
                    sb.AppendLine(StringUtil.Format("{0} = {1}", value.Key, value.Value));
                }
            }
        }

        
        /// <param name="severity">Error severity.</param>
        /// <returns>PS log level.</returns>
        protected static PSLevel GetPSLevelFromSeverity(string severity)
        {
            switch (severity)
            {
                case "Critical":
                case "Error":
                    return PSLevel.Error;
                case "Warning":
                    return PSLevel.Warning;
                default:
                    return PSLevel.Informational;
            }
        }

        // Estimate an approximate size to use for the StringBuilder in LogContextToString
        // Estimated length of all Strings.* values
        // Rough estimate of values
        // max path for Command path
        private const int LogContextInitialSize = 30 * 16 + 13 * 20 + 255;

        
        /// <param name="context">Log context.</param>
        /// <returns>String representation.</returns>
        protected static string LogContextToString(LogContext context)
        {
            StringBuilder sb = new StringBuilder(LogContextInitialSize);

            sb.Append(Strings.LogContextSeverity);
            sb.AppendLine(context.Severity);
            sb.Append(Strings.LogContextHostName);
            sb.AppendLine(context.HostName);
            sb.Append(Strings.LogContextHostVersion);
            sb.AppendLine(context.HostVersion);
            sb.Append(Strings.LogContextHostId);
            sb.AppendLine(context.HostId);
            sb.Append(Strings.LogContextHostApplication);
            sb.AppendLine(context.HostApplication);
            sb.Append(Strings.LogContextEngineVersion);
            sb.AppendLine(context.EngineVersion);
            sb.Append(Strings.LogContextRunspaceId);
            sb.AppendLine(context.RunspaceId);
            sb.Append(Strings.LogContextPipelineId);
            sb.AppendLine(context.PipelineId);
            sb.Append(Strings.LogContextCommandName);
            sb.AppendLine(context.CommandName);
            sb.Append(Strings.LogContextCommandType);
            sb.AppendLine(context.CommandType);
            sb.Append(Strings.LogContextScriptName);
            sb.AppendLine(context.ScriptName);
            sb.Append(Strings.LogContextCommandPath);
            sb.AppendLine(context.CommandPath);
            sb.Append(Strings.LogContextSequenceNumber);
            sb.AppendLine(context.SequenceNumber);
            sb.Append(Strings.LogContextUser);
            sb.AppendLine(context.User);
            sb.Append(Strings.LogContextConnectedUser);
            sb.AppendLine(context.ConnectedUser);
            sb.Append(Strings.LogContextShellId);
            sb.AppendLine(context.ShellId);

            return sb.ToString();
        }

        #endregion
    }

    
    internal class DummyLogProvider : LogProvider
    {
        
        internal DummyLogProvider()
        {
        }

        #region Provider api

        
        /// <param name="logContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="additionalInfo"></param>
        internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception, Dictionary<string, string> additionalInfo)
        {
        }

        
        /// <param name="logContext"></param>
        /// <param name="newState"></param>
        /// <param name="previousState"></param>
        internal override void LogEngineLifecycleEvent(LogContext logContext, EngineState newState, EngineState previousState)
        {
        }

        
        /// <param name="logContext"></param>
        /// <param name="exception"></param>
        internal override void LogCommandHealthEvent(LogContext logContext, Exception exception)
        {
        }

        
        /// <param name="getLogContext"></param>
        /// <param name="newState"></param>
        internal override void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState)
        {
        }

        
        /// <param name="logContext"></param>
        /// <param name="pipelineExecutionDetail"></param>
        internal override void LogPipelineExecutionDetailEvent(LogContext logContext, List<string> pipelineExecutionDetail)
        {
        }

        
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="exception"></param>
        internal override void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception)
        {
        }

        
        /// <param name="logContext"></param>
        /// <param name="providerName"></param>
        /// <param name="newState"></param>
        internal override void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState)
        {
        }

        
        /// <param name="logContext"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <param name="previousValue"></param>
        internal override void LogSettingsEvent(LogContext logContext, string variableName, string value, string previousValue)
        {
        }

        
        /// <param name="state">This the action performed in AmsiUtil class, like init, scan, etc.</param>
        /// <param name="context">The amsiContext handled - Session pair.</param>
        internal override void LogAmsiUtilStateEvent(string state, string context)
        {
        }

        
        /// <param name="queryName">Name of the WDAC query.</param>
        /// <param name="fileName">Name of script file for policy query. Can be null value.</param>
        /// <param name="querySuccess">Query call succeed code.</param>
        /// <param name="queryResult">Result code of WDAC query.</param>
        internal override void LogWDACQueryEvent(
            string queryName,
            string fileName,
            int querySuccess,
            int queryResult)
        {
        }

        
        /// <param name="title">Title of WDAC audit event.</param>
        /// <param name="message">WDAC audit event message.</param>
        /// <param name="fqid">FullyQualifiedId of WDAC audit event.</param>
        internal override void LogWDACAuditEvent(
            string title,
            string message,
            string fqid)
        {
        }

        #endregion
    }
}
