// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System.Globalization;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Diagnostics.Eventing;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Tracing
{
    // pragma warning disable 16001,16003
    #region Constants

    
    public enum PowerShellTraceEvent : int
    {
        
        None = 0,

        
        HostNameResolve = 0x1001,

        
        SchemeResolve = 0x1002,

        
        ShellResolve = 0x1003,

        
        RunspaceConstructor = 0x2001,

        
        RunspacePoolConstructor = 0x2002,

        
        RunspacePoolOpen = 0x2003,

        
        OperationalTransferEventRunspacePool = 0x2004,

        
        RunspacePort = 0x2F01,

        
        AppName = 0x2F02,

        
        ComputerName = 0x2F03,

        
        Scheme = 0x2F04,

        
        TestAnalytic = 0x2F05,

        
        WSManConnectionInfoDump = 0x2F06,

        
        AnalyticTransferEventRunspacePool = 0x2F07,

        // Start: Transport related events

        
        TransportReceivedObject = 0x8001,

        
        AppDomainUnhandledExceptionAnalytic = 0x8007,

        
        TransportErrorAnalytic = 0x8008,

        
        AppDomainUnhandledException = 0x8009,

        
        TransportError = 0x8010,

        
        WSManCreateShell = 0x8011,

        
        WSManCreateShellCallbackReceived = 0x8012,

        
        WSManCloseShell = 0x8013,

        
        WSManCloseShellCallbackReceived = 0x8014,

        
        WSManSendShellInputExtended = 0x8015,

        
        WSManSendShellInputExtendedCallbackReceived = 0x8016,

        
        WSManReceiveShellOutputExtended = 0x8017,

        
        WSManReceiveShellOutputExtendedCallbackReceived = 0x8018,

        
        WSManCreateCommand = 0x8019,

        
        WSManCreateCommandCallbackReceived = 0x8020,

        
        WSManCloseCommand = 0x8021,

        
        WSManCloseCommandCallbackReceived = 0x8022,

        
        WSManSignal = 0x8023,

        
        WSManSignalCallbackReceived = 0x8024,

        
        UriRedirection = 0x8025,

        
        ServerSendData = 0x8051,

        
        ServerCreateRemoteSession = 0x8052,

        
        ReportContext = 0x8053,

        
        ReportOperationComplete = 0x8054,

        
        ServerCreateCommandSession = 0x8055,

        
        ServerStopCommand = 0x8056,

        
        ServerReceivedData = 0x8057,

        
        ServerClientReceiveRequest = 0x8058,

        
        ServerCloseOperation = 0x8059,

        
        LoadingPSCustomShellAssembly = 0x8061,

        
        LoadingPSCustomShellType = 0x8062,

        
        ReceivedRemotingFragment = 0x8063,

        
        SentRemotingFragment = 0x8064,

        
        WSManPluginShutdown = 0x8065,
        // End: Transport related events

        // Start: Serialization related events

        
        SerializerWorkflowLoadSuccess = 0x7001,

        
        SerializerWorkflowLoadFailure = 0x7002,

        
        SerializerDepthOverride = 0x7003,

        
        SerializerModeOverride = 0x7004,

        
        SerializerScriptPropertyWithoutRunspace = 0x7005,

        
        SerializerPropertyGetterFailed = 0x7006,

        
        SerializerEnumerationFailed = 0x7007,

        
        SerializerToStringFailed = 0x7008,

        
        SerializerMaxDepthWhenSerializing = 0x700A,

        
        SerializerXmlExceptionWhenDeserializing = 0x700B,

        
        SerializerSpecificPropertyMissing = 0x700C,
        // End: Serialization related events

        // Start: PerformanceTrack related events
        
        PerformanceTrackConsoleStartupStart = 0xA001,

        
        PerformanceTrackConsoleStartupStop = 0xA002,
        // End: Preftrack related events

        
        ErrorRecord = 0xB001,

        
        Exception = 0xB002,

        
        PowerShellObject = 0xB003,

        
        Job = 0xB004,

        
        TraceMessage = 0xB005,

        
        TraceWSManConnectionInfo = 0xB006,

        
        TraceMessage2 = 0xC001,

        
        TraceMessageGuid = 0xC002,
    }

    
    // pragma warning disable 16001
    public enum PowerShellTraceChannel
    {
        
        None = 0,
        
        Operational = 0x10,

        
        Analytic = 0x11,

        
        Debug = 0x12,
    }
    // pragma warning restore 16001

    
    public enum PowerShellTraceLevel
    {
        
        LogAlways = 0,

        
        Critical = 1,

        
        Error = 2,

        
        Warning = 3,

        
        Informational = 4,

        
        Verbose = 5,

        
        Debug = 20,
    }

    
    public enum PowerShellTraceOperationCode
    {
        
        None = 0,

        
        Open = 10,

        
        Close = 11,

        
        Connect = 12,

        
        Disconnect = 13,

        
        Negotiate = 14,

        
        Create = 15,

        
        Constructor = 16,

        
        Dispose = 17,

        
        EventHandler = 18,

        
        Exception = 19,

        
        Method = 20,

        
        Send = 21,

        
        Receive = 22,

        
        WorkflowLoad = 23,

        
        SerializationSettings = 24,

        
        WinInfo,

        
        WinStart,

        
        WinStop,

        
        WinDCStart,

        
        WinDCStop,

        
        WinExtension,

        
        WinReply,

        
        WinResume,

        
        WinSuspend,
    }

    
    public enum PowerShellTraceTask
    {
        
        None = 0,

        
        CreateRunspace = 1,

        
        ExecuteCommand = 2,

        
        Serialization = 3,

        
        PowerShellConsoleStartup = 4,
    }

    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1028")]
    [Flags]
    public enum PowerShellTraceKeywords : ulong
    {
        
        None = 0,

        
        Runspace = 0x1,

        
        Pipeline = 0x2,

        
        Protocol = 0x4,

        
        Transport = 0x8,

        
        Host = 0x10,

        
        Cmdlets = 0x20,

        
        Serializer = 0x40,

        
        Session = 0x80,

        
        ManagedPlugIn = 0x100,

        
        UseAlwaysDebug = 0x2000000000000000,

        
        UseAlwaysOperational = 0x8000000000000000,

        
        UseAlwaysAnalytic = 0x4000000000000000,
    }

    #endregion

    
    public abstract class BaseChannelWriter : IDisposable
    {
        private bool disposed;

        
        public virtual void Dispose()
        {
            if (!disposed)
            {
                GC.SuppressFinalize(this);
                disposed = true;
            }
        }

        
        public virtual bool TraceError(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        
        public virtual bool TraceWarning(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        
        public virtual bool TraceInformational(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        
        public virtual bool TraceVerbose(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        
        public virtual bool TraceDebug(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        
        public virtual bool TraceLogAlways(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        
        public virtual bool TraceCritical(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return true;
        }

        
        public virtual PowerShellTraceKeywords Keywords
        {
            get
            {
                return PowerShellTraceKeywords.None;
            }

            set
            {
                PowerShellTraceKeywords powerShellTraceKeywords = value;
            }
        }
    }

    
    public sealed class NullWriter : BaseChannelWriter
    {
        
        public static BaseChannelWriter Instance { get; } = new NullWriter();

        private NullWriter()
        {
        }
    }

    
    public sealed class PowerShellChannelWriter : BaseChannelWriter
    {
        private readonly PowerShellTraceChannel _traceChannel;

        
        private static readonly EventProvider _provider = new EventProvider(

        private bool disposed;
        private PowerShellTraceKeywords _keywords;

        
        public override PowerShellTraceKeywords Keywords
        {
            get
            {
                return _keywords;
            }

            set
            {
                _keywords = value;
            }
        }

        internal PowerShellChannelWriter(PowerShellTraceChannel traceChannel, PowerShellTraceKeywords keywords)
        {
            _traceChannel = traceChannel;
            _keywords = keywords;
        }

        
        public override void Dispose()
        {
            if (!disposed)
            {
                GC.SuppressFinalize(this);
                disposed = true;
            }
        }

        private bool Trace(PowerShellTraceEvent traceEvent, PowerShellTraceLevel level, PowerShellTraceOperationCode operationCode,
            PowerShellTraceTask task, params object[] args)
        {
            EventDescriptor ed = new EventDescriptor((int)traceEvent, 1, (byte)_traceChannel, (byte)level,
                                                     (byte)operationCode, (int)task, (long)_keywords);

            

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == null)
                    {
                        args[i] = string.Empty;
                    }
                }
            }

            return _provider.WriteEvent(in ed, args);
        }

        
        public override bool TraceError(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Error, operationCode, task, args);
        }

        
        public override bool TraceWarning(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Warning, operationCode, task, args);
        }

        
        public override bool TraceInformational(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Informational, operationCode, task, args);
        }

        
        public override bool TraceVerbose(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Verbose, operationCode, task, args);
        }

        
        public override bool TraceDebug(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            // TODO: There is some error thrown by the custom debug level
            // hence Informational is being used
            return Trace(traceEvent, PowerShellTraceLevel.Informational, operationCode, task, args);
        }

        
        public override bool TraceLogAlways(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.LogAlways, operationCode, task, args);
        }

        
        public override bool TraceCritical(PowerShellTraceEvent traceEvent, PowerShellTraceOperationCode operationCode, PowerShellTraceTask task, params object[] args)
        {
            return Trace(traceEvent, PowerShellTraceLevel.Critical, operationCode, task, args);
        }
    }

    
    public sealed class PowerShellTraceSource : IDisposable
    {
        private bool disposed;

        
        internal PowerShellTraceSource(PowerShellTraceTask task, PowerShellTraceKeywords keywords)
        {
            {
                DebugChannel = NullWriter.Instance;
                AnalyticChannel = NullWriter.Instance;
                OperationalChannel = NullWriter.Instance;
            }
        }

        
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                GC.SuppressFinalize(this);

                DebugChannel.Dispose();
                AnalyticChannel.Dispose();
                OperationalChannel.Dispose();
            }
        }

        
        public PowerShellTraceKeywords Keywords { get; } = PowerShellTraceKeywords.None;

        
        public PowerShellTraceTask Task { get; set; } = PowerShellTraceTask.None;

        private static bool IsEtwSupported
        {
            get
            {
                return Environment.OSVersion.Version.Major >= 6;
            }
        }

        
        public bool TraceErrorRecord(ErrorRecord errorRecord)
        {
            
        }

        
        public bool TraceException(Exception exception)
        {
            
        }

        
        public bool TracePowerShellObject(PSObject powerShellObject)
        {
        }

        
        public bool TraceJob(Job job)
        {
            
        }

        
        public bool WriteMessage(string message)
        {
           
        }

        
        public bool WriteMessage(string message1, string message2)
        {
            
        }

        
        public bool WriteMessage(string message, Guid instanceId)
        {
            
        }

        
        public void WriteMessage(string className, string methodName, Guid workflowId, string message, params string[] parameters)
        {
            
        }

        
        public void WriteMessage(string className, string methodName, Guid workflowId, Job job, string message, params string[] parameters)
        {
        }

        
        public void WriteScheduledJobStartEvent(params object[] args)
        {
            
        }

        
        public void WriteScheduledJobCompleteEvent(params object[] args)
        {
            
        }

        
        public void WriteScheduledJobErrorEvent(params object[] args)
        {
            
        }

        
        public void WriteISEExecuteScriptEvent(params object[] args)
        {
            
        }

        
        public void WriteISEExecuteSelectionEvent(params object[] args)
        {
            
        }

        
        public void WriteISEStopCommandEvent(params object[] args)
        {
            
        }

        
        public void WriteISEResumeDebuggerEvent(params object[] args)
        {
            
        }

        
        public void WriteISEStopDebuggerEvent(params object[] args)
        {
            
        }

        
        public void WriteISEDebuggerStepIntoEvent(params object[] args)
        {
            
        }

        
        public void WriteISEDebuggerStepOverEvent(params object[] args)
        {
            
        }

        
        public void WriteISEDebuggerStepOutEvent(params object[] args)
        {
            
        }

        
        public void WriteISEEnableAllBreakpointsEvent(params object[] args)
        {
            
        }

        
        public void WriteISEDisableAllBreakpointsEvent(params object[] args)
        {
            
        }

        
        public void WriteISERemoveAllBreakpointsEvent(params object[] args)
        {
            
        }

        
        public void WriteISESetBreakpointEvent(params object[] args)
        {
            
        }

        
        public void WriteISERemoveBreakpointEvent(params object[] args)
        {
            
        }

        
        public void WriteISEEnableBreakpointEvent(params object[] args)
        {
            
        }

        
        public void WriteISEDisableBreakpointEvent(params object[] args)
        {
            
        }

        
        public void WriteISEHitBreakpointEvent(params object[] args)
        {
            
        }

        
        public void WriteMessage(string className, string methodName, Guid workflowId, string activityName, Guid activityId, string message, params string[] parameters)
        {
            
        }

        
        public bool TraceWSManConnectionInfo(WSManConnectionInfo connectionInfo)
        {
            return true;
        }

        
        public BaseChannelWriter DebugChannel { get; }

        
        public BaseChannelWriter AnalyticChannel { get; }

        
        public BaseChannelWriter OperationalChannel { get; }
    }

    
    public static class PowerShellTraceSourceFactory
    {
        
        public static PowerShellTraceSource GetTraceSource()
        {
            return new PowerShellTraceSource(PowerShellTraceTask.None, PowerShellTraceKeywords.None);
        }

        
        public static PowerShellTraceSource GetTraceSource(PowerShellTraceTask task)
        {
            return new PowerShellTraceSource(task, PowerShellTraceKeywords.None);
        }

        
        public static PowerShellTraceSource GetTraceSource(PowerShellTraceTask task, PowerShellTraceKeywords keywords)
        {
            return new PowerShellTraceSource(task, keywords);
        }
    }
    // pragma warning restore 16001,16003
}

#endif
