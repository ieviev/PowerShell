// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Security;
using System.Threading;

namespace System.Management.Automation
{
    
    internal static class MshLog
    {
        #region Initialization

        
        private static readonly ConcurrentDictionary<string, Collection<LogProvider>> s_logProviders =
            new ConcurrentDictionary<string, Collection<LogProvider>>();

        private const string _crimsonLogProviderAssemblyName = "MshCrimsonLog";
        private const string _crimsonLogProviderTypeName = "System.Management.Automation.Logging.CrimsonLogProvider";

        private static readonly Collection<string> s_ignoredCommands = new Collection<string>();

        
        static MshLog()
        {
            s_ignoredCommands.Add("Out-Lineoutput");
            s_ignoredCommands.Add("Format-Default");
        }

        
        /// <param name="shellId"></param>
        /// <returns></returns>
        private static IEnumerable<LogProvider> GetLogProvider(string shellId)
        {
            return s_logProviders.GetOrAdd(shellId, CreateLogProvider);
        }

        
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static IEnumerable<LogProvider> GetLogProvider(ExecutionContext executionContext)
        {
            if (executionContext == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(executionContext));
            }

            string shellId = executionContext.ShellID;

            return GetLogProvider(shellId);
        }

        
        /// <param name="logContext"></param>
        /// <returns></returns>
        private static IEnumerable<LogProvider> GetLogProvider(LogContext logContext)
        {
            System.Diagnostics.Debug.Assert(logContext != null);
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(logContext.ShellId));

            return GetLogProvider(logContext.ShellId);
        }

        
        /// <param name="shellId"></param>
        /// <returns></returns>
        private static Collection<LogProvider> CreateLogProvider(string shellId)
        {
            Collection<LogProvider> providers = new Collection<LogProvider>();
            // Porting note: Linux does not support ETW

            try
            {
#if UNIX
                //LogProvider sysLogProvider = new PSSysLogProvider();
                //providers.Add(sysLogProvider);
#else
                //LogProvider etwLogProvider = new 
                //providers.Add(etwLogProvider);
#endif

                return providers;
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (SecurityException)
            {
                // This exception will happen if we try to create an event source
                // (corresponding to the current running minishell)
                // when running as non-admin user. In that case, we will default
                // to dummy log.
            }

            //providers.Add(new DummyLogProvider());
            return providers;
        }

        
        /// <param name="shellId"></param>
        internal static void SetDummyLog(string shellId)
        {
            Collection<LogProvider> providers = new Collection<LogProvider> { new DummyLogProvider() };
            s_logProviders.AddOrUpdate(shellId, providers, (key, value) => providers);
        }

        #endregion

        #region Engine Health Event Logging Api

        
        /// <param name="executionContext">Execution context for the engine that is running.</param>
        /// <param name="eventId">EventId for the event to be logged.</param>
        /// <param name="exception">Exception associated with this event.</param>
        /// <param name="severity">Severity of this event.</param>
        /// <param name="additionalInfo">Additional information for this event.</param>
        /// <param name="newEngineState">New engine state.</param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                int eventId,
                                                Exception exception,
                                                Severity severity,
                                                Dictionary<string, string> additionalInfo,
                                                EngineState newEngineState)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            if (exception == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(exception));
                return;
            }

            InvocationInfo invocationInfo = null;
            if (exception is IContainsErrorRecord icer && icer.ErrorRecord != null)
            {
                invocationInfo = icer.ErrorRecord.InvocationInfo;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogEngineHealthEvent(provider, executionContext))
                {
                    provider.LogEngineHealthEvent(GetLogContext(executionContext, invocationInfo, severity), eventId, exception, additionalInfo);
                }
            }

            if (newEngineState != EngineState.None)
            {
                LogEngineLifecycleEvent(executionContext, newEngineState, invocationInfo);
            }
        }

        
        /// <param name="executionContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="severity"></param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                int eventId,
                                                Exception exception,
                                                Severity severity)
        {
            LogEngineHealthEvent(executionContext, eventId, exception, severity, null);
        }

        
        /// <param name="executionContext"></param>
        /// <param name="exception"></param>
        /// <param name="severity"></param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                Exception exception,
                                                Severity severity)
        {
            LogEngineHealthEvent(executionContext, 100, exception, severity, null);
        }

        
        /// <param name="executionContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="severity"></param>
        /// <param name="additionalInfo"></param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                int eventId,
                                                Exception exception,
                                                Severity severity,
                                                Dictionary<string, string> additionalInfo)
        {
            LogEngineHealthEvent(executionContext, eventId, exception, severity, additionalInfo, EngineState.None);
        }

        
        /// <param name="executionContext"></param>
        /// <param name="eventId"></param>
        /// <param name="exception"></param>
        /// <param name="severity"></param>
        /// <param name="newEngineState"></param>
        internal static void LogEngineHealthEvent(ExecutionContext executionContext,
                                                int eventId,
                                                Exception exception,
                                                Severity severity,
                                                EngineState newEngineState)
        {
            LogEngineHealthEvent(executionContext, eventId, exception, severity, null, newEngineState);
        }

        
        /// <param name="logContext">LogContext to be.</param>
        /// <param name="eventId">EventId for the event to be logged.</param>
        /// <param name="exception">Exception associated with this event.</param>
        /// <param name="additionalInfo">Additional information for this event.</param>
        internal static void LogEngineHealthEvent(LogContext logContext,
                                                int eventId,
                                                Exception exception,
                                                Dictionary<string, string> additionalInfo
                                                )
        {
            if (logContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(logContext));
                return;
            }

            if (exception == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(exception));
                return;
            }

            // Here execution context doesn't exist, we will have to log this event regardless.
            // Don't check NeedToLogEngineHealthEvent here.
            foreach (LogProvider provider in GetLogProvider(logContext))
            {
                provider.LogEngineHealthEvent(logContext, eventId, exception, additionalInfo);
            }
        }

        #endregion

        #region Engine Lifecycle Event Logging Api

        
        /// <param name="executionContext">Execution context for current engine instance.</param>
        /// <param name="engineState">New engine state.</param>
        /// <param name="invocationInfo">InvocationInfo for current command that is running.</param>
        internal static void LogEngineLifecycleEvent(ExecutionContext executionContext,
                                                EngineState engineState,
                                                InvocationInfo invocationInfo)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            EngineState previousState = GetEngineState(executionContext);
            if (engineState == previousState)
                return;

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogEngineLifecycleEvent(provider, executionContext))
                {
                    provider.LogEngineLifecycleEvent(GetLogContext(executionContext, invocationInfo), engineState, previousState);
                }
            }

            SetEngineState(executionContext, engineState);
        }

        
        /// <param name="executionContext"></param>
        /// <param name="engineState"></param>
        internal static void LogEngineLifecycleEvent(ExecutionContext executionContext,
                                                EngineState engineState)
        {
            LogEngineLifecycleEvent(executionContext, engineState, null);
        }

        #endregion

        #region Command Health Event Logging Api

        
        /// <param name="executionContext">Execution context for the engine that is running.</param>
        /// <param name="exception">Exception associated with this event.</param>
        /// <param name="severity">Severity of this event.</param>
        internal static void LogCommandHealthEvent(ExecutionContext executionContext,
                                                Exception exception,
                                                Severity severity
                                                )
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            if (exception == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(exception));
                return;
            }

            InvocationInfo invocationInfo = null;
            if (exception is IContainsErrorRecord icer && icer.ErrorRecord != null)
            {
                invocationInfo = icer.ErrorRecord.InvocationInfo;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogCommandHealthEvent(provider, executionContext))
                {
                    provider.LogCommandHealthEvent(GetLogContext(executionContext, invocationInfo, severity), exception);
                }
            }
        }

        #endregion

        #region Command Lifecycle Event Logging Api

        
        /// <param name="executionContext">Execution Context for the current running engine.</param>
        /// <param name="commandState">New command state.</param>
        /// <param name="invocationInfo">Invocation data for current command that is running.</param>
        internal static void LogCommandLifecycleEvent(ExecutionContext executionContext,
                                                CommandState commandState,
                                                InvocationInfo invocationInfo)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            if (invocationInfo == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(invocationInfo));
                return;
            }

            if (s_ignoredCommands.Contains(invocationInfo.MyCommand.Name))
            {
                return;
            }

            LogContext logContext = null;
            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogCommandLifecycleEvent(provider, executionContext))
                {
                    provider.LogCommandLifecycleEvent(
                        () => logContext ??= GetLogContext(executionContext, invocationInfo),
                        commandState);
                }
            }
        }

        
        /// <param name="executionContext">Execution Context for the current running engine.</param>
        /// <param name="commandState">New command state.</param>
        /// <param name="commandName">Current command that is running.</param>
        internal static void LogCommandLifecycleEvent(ExecutionContext executionContext,
                                                CommandState commandState,
                                                string commandName)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            LogContext logContext = null;
            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogCommandLifecycleEvent(provider, executionContext))
                {
                    provider.LogCommandLifecycleEvent(
                        () =>
                        {
                            if (logContext == null)
                            {
                                logContext = GetLogContext(executionContext, null);
                                logContext.CommandName = commandName;
                            }

                            return logContext;
                        }, commandState);
                }
            }
        }

        #endregion

        #region Pipeline Execution Detail Event Logging Api

        
        /// <param name="executionContext">Execution Context for the current running engine.</param>
        /// <param name="detail">Detail to be logged for this pipeline execution detail.</param>
        /// <param name="invocationInfo">Invocation data for current command that is running.</param>
        internal static void LogPipelineExecutionDetailEvent(ExecutionContext executionContext,
                                                            List<string> detail,
                                                            InvocationInfo invocationInfo)

        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogPipelineExecutionDetailEvent(provider, executionContext))
                {
                    provider.LogPipelineExecutionDetailEvent(GetLogContext(executionContext, invocationInfo), detail);
                }
            }
        }

        
        /// <param name="executionContext">Execution Context for the current running engine.</param>
        /// <param name="detail">Detail to be logged for this pipeline execution detail.</param>
        /// <param name="scriptName">Script that is currently running.</param>
        /// <param name="commandLine">Command line that is currently running.</param>
        internal static void LogPipelineExecutionDetailEvent(ExecutionContext executionContext,
                                                            List<string> detail,
                                                            string scriptName,
                                                            string commandLine)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            LogContext logContext = GetLogContext(executionContext, null);
            logContext.CommandLine = commandLine;
            logContext.ScriptName = scriptName;

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogPipelineExecutionDetailEvent(provider, executionContext))
                {
                    provider.LogPipelineExecutionDetailEvent(logContext, detail);
                }
            }
        }

        #endregion

        #region Provider Health Event Logging Api

        
        /// <param name="executionContext">Execution context for the engine that is running.</param>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="exception">Exception associated with this event.</param>
        /// <param name="severity">Severity of this event.</param>
        internal static void LogProviderHealthEvent(ExecutionContext executionContext,
                                                string providerName,
                                                Exception exception,
                                                Severity severity
                                                )
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            if (exception == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(exception));
                return;
            }

            InvocationInfo invocationInfo = null;
            if (exception is IContainsErrorRecord icer && icer.ErrorRecord != null)
            {
                invocationInfo = icer.ErrorRecord.InvocationInfo;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogProviderHealthEvent(provider, executionContext))
                {
                    provider.LogProviderHealthEvent(GetLogContext(executionContext, invocationInfo, severity), providerName, exception);
                }
            }
        }

        #endregion

        #region Provider Lifecycle Event Logging Api

        
        /// <param name="executionContext">Execution Context for current engine that is running.</param>
        /// <param name="providerName">Provider name.</param>
        /// <param name="providerState">New provider state.</param>
        internal static void LogProviderLifecycleEvent(ExecutionContext executionContext,
                                                     string providerName,
                                                     ProviderState providerState)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogProviderLifecycleEvent(provider, executionContext))
                {
                    provider.LogProviderLifecycleEvent(GetLogContext(executionContext, null), providerName, providerState);
                }
            }
        }

        #endregion

        #region Settings Event Logging Api

        
        /// <param name="executionContext">Execution context for current running engine.</param>
        /// <param name="variableName">Variable name.</param>
        /// <param name="newValue">New value for the variable.</param>
        /// <param name="previousValue">Previous value for the variable.</param>
        /// <param name="invocationInfo">Invocation data for the command that is currently running.</param>
        internal static void LogSettingsEvent(ExecutionContext executionContext,
                                            string variableName,
                                            string newValue,
                                            string previousValue,
                                            InvocationInfo invocationInfo)
        {
            if (executionContext == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(executionContext));
                return;
            }

            foreach (LogProvider provider in GetLogProvider(executionContext))
            {
                if (NeedToLogSettingsEvent(provider, executionContext))
                {
                    provider.LogSettingsEvent(GetLogContext(executionContext, invocationInfo), variableName, newValue, previousValue);
                }
            }
        }

        
        /// <param name="executionContext"></param>
        /// <param name="variableName"></param>
        /// <param name="newValue"></param>
        /// <param name="previousValue"></param>
        internal static void LogSettingsEvent(ExecutionContext executionContext,
                                            string variableName,
                                            string newValue,
                                            string previousValue)
        {
            LogSettingsEvent(executionContext, variableName, newValue, previousValue, null);
        }

        #endregion

        #region Helper Functions

        
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static EngineState GetEngineState(ExecutionContext executionContext)
        {
            return executionContext.EngineState;
        }

        
        /// <param name="executionContext"></param>
        /// <param name="engineState"></param>
        private static void SetEngineState(ExecutionContext executionContext, EngineState engineState)
        {
            executionContext.EngineState = engineState;
        }

        
        /// <param name="executionContext"></param>
        /// <param name="invocationInfo"></param>
        /// <returns></returns>
        internal static LogContext GetLogContext(ExecutionContext executionContext, InvocationInfo invocationInfo)
        {
            return GetLogContext(executionContext, invocationInfo, Severity.Informational);
        }

        
        /// <param name="executionContext"></param>
        /// <param name="invocationInfo"></param>
        /// <param name="severity"></param>
        /// <returns></returns>
        private static LogContext GetLogContext(ExecutionContext executionContext, InvocationInfo invocationInfo, Severity severity)
        {
            if (executionContext == null)
                return null;

            LogContext logContext = new LogContext();

            string shellId = executionContext.ShellID;

            logContext.ExecutionContext = executionContext;
            logContext.ShellId = shellId;
            logContext.Severity = severity.ToString();

            if (executionContext.EngineHostInterface != null)
            {
                logContext.HostName = executionContext.EngineHostInterface.Name;
                logContext.HostVersion = executionContext.EngineHostInterface.Version.ToString();
                logContext.HostId = (string)executionContext.EngineHostInterface.InstanceId.ToString();
            }

            logContext.HostApplication = string.Join(' ', Environment.GetCommandLineArgs());

            if (executionContext.CurrentRunspace != null)
            {
                logContext.EngineVersion = executionContext.CurrentRunspace.Version.ToString();
                logContext.RunspaceId = executionContext.CurrentRunspace.InstanceId.ToString();

                Pipeline currentPipeline = ((RunspaceBase)executionContext.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (currentPipeline != null)
                {
                    logContext.PipelineId = currentPipeline.InstanceId.ToString(CultureInfo.CurrentCulture);
                }
            }

            logContext.SequenceNumber = NextSequenceNumber;

            try
            {
                if (executionContext.LogContextCache.User == null)
                {
                    logContext.User = Environment.UserDomainName + "\\" + Environment.UserName;
                    executionContext.LogContextCache.User = logContext.User;
                }
                else
                {
                    logContext.User = executionContext.LogContextCache.User;
                }
            }
            catch (InvalidOperationException)
            {
                logContext.User = Logging.UnknownUserName;
            }

            if (executionContext.SessionState.PSVariable.GetValue("PSSenderInfo") is System.Management.Automation.Remoting.PSSenderInfo psSenderInfo)
            {
                logContext.ConnectedUser = psSenderInfo.UserInfo.Identity.Name;
            }

            logContext.Time = DateTime.Now.ToString(CultureInfo.CurrentCulture);

            if (invocationInfo == null)
                return logContext;

            logContext.ScriptName = invocationInfo.ScriptName;
            logContext.CommandLine = invocationInfo.Line;

            if (invocationInfo.MyCommand != null)
            {
                logContext.CommandName = invocationInfo.MyCommand.Name;
                logContext.CommandType = invocationInfo.MyCommand.CommandType.ToString();

                switch (invocationInfo.MyCommand.CommandType)
                {
                    case CommandTypes.Application:
                        logContext.CommandPath = ((ApplicationInfo)invocationInfo.MyCommand).Path;
                        break;
                    case CommandTypes.ExternalScript:
                        logContext.CommandPath = ((ExternalScriptInfo)invocationInfo.MyCommand).Path;
                        break;
                }
            }

            return logContext;
        }

        #endregion

        #region Logging Policy

        
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogEngineHealthEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogEngineHealthEventVarPath, true));
        }

        
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogEngineLifecycleEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogEngineLifecycleEventVarPath, true));
        }

        
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogCommandHealthEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogCommandHealthEventVarPath, false));
        }

        
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogCommandLifecycleEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogCommandLifecycleEventVarPath, false));
        }

        
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogPipelineExecutionDetailEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return true;
            // return LanguagePrimitives.IsTrue(executionContext.GetVariable("LogPipelineExecutionDetailEvent", false));
        }

        
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogProviderHealthEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogProviderHealthEventVarPath, true));
        }

        
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogProviderLifecycleEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogProviderLifecycleEventVarPath, true));
        }

        
        /// <param name="logProvider"></param>
        /// <param name="executionContext"></param>
        /// <returns></returns>
        private static bool NeedToLogSettingsEvent(LogProvider logProvider, ExecutionContext executionContext)
        {
            if (!logProvider.UseLoggingVariables())
            {
                return true;
            }

            return LanguagePrimitives.IsTrue(executionContext.GetVariableValue(SpecialVariables.LogSettingsEventVarPath, true));
        }

        #endregion

        #region Sequence Id Generator

        private static int s_nextSequenceNumber = 0;

        
        /// <value></value>
        private static string NextSequenceNumber
        {
            get
            {
                return Convert.ToString(Interlocked.Increment(ref s_nextSequenceNumber), CultureInfo.CurrentCulture);
            }
        }

        #endregion

        #region EventId Constants

        // General health issues.
        internal const int EVENT_ID_GENERAL_HEALTH_ISSUE = 100;

        // Dependency. resource not available
        internal const int EVENT_ID_RESOURCE_NOT_AVAILABLE = 101;
        // Connectivity. network connection failure
        internal const int EVENT_ID_NETWORK_CONNECTIVITY_ISSUE = 102;
        // Settings. fail to set some configuration settings
        internal const int EVENT_ID_CONFIGURATION_FAILURE = 103;
        // Performance. system is experiencing some performance issues
        internal const int EVENT_ID_PERFORMANCE_ISSUE = 104;
        // Security: system is experiencing some security issues
        internal const int EVENT_ID_SECURITY_ISSUE = 105;
        // Workload. system is overloaded.
        internal const int EVENT_ID_SYSTEM_OVERLOADED = 106;

        // Beta 1 only -- Unexpected Exception
        internal const int EVENT_ID_UNEXPECTED_EXCEPTION = 195;

        #endregion EventId Constants
    }

    
    internal class LogContextCache
    {
        internal string User { get; set; } = null;
    }

    #region Command State and Provider State

    
    internal enum Severity
    {
        
        None,
        
        Critical,

        
        Error,

        
        Warning,

        
        Informational
    }

    
    internal enum CommandState
    {
        
        Started = 0,

        
        Stopped = 1,

        
        Terminated = 2
    }

    
    internal enum ProviderState
    {
        
        Started = 0,

        
        Stopped = 1,
    }

    #endregion
}
