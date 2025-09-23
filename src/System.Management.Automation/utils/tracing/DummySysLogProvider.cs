// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable CA1822
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Runtime.InteropServices;
using System.Text;

// ReSharper disable UnusedMember.Local

namespace System.Management.Automation.Tracing;

internal class DummySysLogProvider : LogProvider
{
    static DummySysLogProvider()
    {
    }

    
    internal bool IsEnabled(PSLevel level, PSKeyword keywords)
    {
        return false;
    }

    
    internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception,
        Dictionary<string, string> additionalInfo)
    {
    }

    
    internal override void LogAmsiUtilStateEvent(string state, string context)
    {
    }

    
    internal override void LogWDACQueryEvent(
        string queryName,
        string fileName,
        int querySuccess,
        int queryResult)
    {
    }

    
    internal override void LogWDACAuditEvent(
        string title,
        string message,
        string fqid)
    {
        WriteEvent(PSEventId.WDAC_Audit, PSChannel.Operational, PSOpcode.Method, PSLevel.Informational, PSTask.WDAC,
            (PSKeyword)0x0, title, message, fqid);
    }

    
    internal override void LogEngineLifecycleEvent(LogContext logContext, EngineState newState,
        EngineState previousState)
    {
    }

    
    internal override void LogCommandHealthEvent(LogContext logContext, Exception exception)
    {
    }

    
    internal override void LogCommandLifecycleEvent(Func<LogContext> getLogContext, CommandState newState)
    {
      
    }

    
    internal override void LogPipelineExecutionDetailEvent(LogContext logContext, List<string> pipelineExecutionDetail)
    {
    }

    
    internal override void LogProviderHealthEvent(LogContext logContext, string providerName, Exception exception)
    {
    }

    
    internal override void LogProviderLifecycleEvent(LogContext logContext, string providerName, ProviderState newState)
    {
    }

    
    internal override void LogSettingsEvent(LogContext logContext, string variableName, string value,
        string previousValue)
    {
    }

    
    internal override bool UseLoggingVariables()
    {
        return false;
    }

    
    internal void WriteEvent(PSEventId id, PSChannel channel, PSOpcode opcode, PSTask task, LogContext logContext,
        string payLoad)
    {
    }

    
    internal void WriteEvent(PSEventId id, PSChannel channel, PSOpcode opcode, PSLevel level, PSTask task,
        PSKeyword keyword, params object[] args)
    {
    }

    
    internal void WriteTransferEvent(Guid parentActivityId)
    {
    }

    
    internal void SetActivityIdForCurrentThread(Guid newActivityId)
    {
    }
}
