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

    
    /// <param name="level"></param>
    /// <param name="keywords"></param>
    /// <returns></returns>
    /// <remarks>
    /// Typically, a provider does not call this method to determine whether a session requested the specified event;
    /// the provider simply writes the event, and ETW determines whether the event is logged to a session. A provider
    /// may want to call this function if the provider needs to perform extra work to generate the event. In this case,
    ///  calling this function first to determine if a session requested the event or not, may save resources and time.
    /// </remarks>
    internal bool IsEnabled(PSLevel level, PSKeyword keywords)
    {
        return false;
    }

    
    /// <param name="logContext"></param>
    /// <param name="eventId"></param>
    /// <param name="exception"></param>
    /// <param name="additionalInfo"></param>
    internal override void LogEngineHealthEvent(LogContext logContext, int eventId, Exception exception,
        Dictionary<string, string> additionalInfo)
    {
    }

    
    /// <param name="state">This the action performed in AmsiUtil class, like init, scan, etc</param>
    /// <param name="context">The amsiContext handled - Session pair</param>
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
        WriteEvent(PSEventId.WDAC_Audit, PSChannel.Operational, PSOpcode.Method, PSLevel.Informational, PSTask.WDAC,
            (PSKeyword)0x0, title, message, fqid);
    }

    
    /// <param name="logContext"></param>
    /// <param name="newState"></param>
    /// <param name="previousState"></param>
    internal override void LogEngineLifecycleEvent(LogContext logContext, EngineState newState,
        EngineState previousState)
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
    internal override void LogSettingsEvent(LogContext logContext, string variableName, string value,
        string previousValue)
    {
    }

    
    /// <returns></returns>
    internal override bool UseLoggingVariables()
    {
        return false;
    }

    
    /// <param name="id">Event id.</param>
    /// <param name="channel"></param>
    /// <param name="opcode"></param>
    /// <param name="task"></param>
    /// <param name="logContext">Log context.</param>
    /// <param name="payLoad"></param>
    internal void WriteEvent(PSEventId id, PSChannel channel, PSOpcode opcode, PSTask task, LogContext logContext,
        string payLoad)
    {
    }

    
    /// <param name="id"></param>
    /// <param name="channel"></param>
    /// <param name="opcode"></param>
    /// <param name="level"></param>
    /// <param name="task"></param>
    /// <param name="keyword"></param>
    /// <param name="args"></param>
    internal void WriteEvent(PSEventId id, PSChannel channel, PSOpcode opcode, PSLevel level, PSTask task,
        PSKeyword keyword, params object[] args)
    {
    }

    
    internal void WriteTransferEvent(Guid parentActivityId)
    {
    }

    
    /// <param name="newActivityId">The GUID identifying the activity.</param>
    internal void SetActivityIdForCurrentThread(Guid newActivityId)
    {
    }
}
