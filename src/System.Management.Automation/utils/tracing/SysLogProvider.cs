// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if UNIX

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using System.Management.Automation.Internal;

namespace System.Management.Automation.Tracing
{
    
    internal class SysLogProvider
    {
        // Ensure the string pointer is not garbage collected.
        private static IntPtr _nativeSyslogIdent = IntPtr.Zero;
        private static readonly NativeMethods.SysLogPriority _facility = NativeMethods.SysLogPriority.Local0;

        private readonly byte _channelFilter;
        private readonly ulong _keywordFilter;
        private readonly byte _levelFilter;

        
        public SysLogProvider(string applicationId, PSLevel level, PSKeyword keywords, PSChannel channels)
        {
            // NOTE: This string needs to remain valid for the life of the process since the underlying API keeps
            // a reference to it.
            // FUTURE: If logging is redesigned, make these details static or a singleton since there should only be one
            // instance active.
            _nativeSyslogIdent = Marshal.StringToHGlobalAnsi(applicationId);
            NativeMethods.OpenLog(_nativeSyslogIdent, _facility);
            _keywordFilter = (ulong)keywords;
            _levelFilter = (byte)level;
            _channelFilter = (byte)channels;
            if ((_channelFilter & (ulong)PSChannel.Operational) != 0)
            {
                _keywordFilter |= (ulong)PSKeyword.UseAlwaysOperational;
            }

            if ((_channelFilter & (ulong)PSChannel.Analytic) != 0)
            {
                _keywordFilter |= (ulong)PSKeyword.UseAlwaysAnalytic;
            }
        }

        
        [ThreadStatic]
        private static StringBuilder t_messageBuilder;

        private static StringBuilder MessageBuilder
        {
            get
            {
                // NOTE: Thread static fields must be explicitly initialized for each thread.
                t_messageBuilder ??= new StringBuilder(200);

                return t_messageBuilder;
            }
        }

        
        [ThreadStatic]
        private static Guid? t_activity;

        private static Guid Activity
        {
            get
            {
                if (!t_activity.HasValue)
                {
                    // NOTE: Thread static fields must be explicitly initialized for each thread.
                    t_activity = Guid.NewGuid();
                }

                return t_activity.Value;
            }

            set
            {
                t_activity = value;
            }
        }

        
        internal bool IsEnabled(PSLevel level, PSKeyword keywords)
        {
            return ((ulong)keywords & _keywordFilter) != 0
                && ((int)level <= _levelFilter);
        }

        // NOTE: There are a number of places where PowerShell code sends analytic events
        // to the operational channel. This is a side-effect of the custom wrappers that
        // use flags that are not consistent with the event definition.
        // To ensure filtering of analytic events is consistent, both keyword and channel
        // filtering is performed to suppress analytic events.
        private bool ShouldLog(PSLevel level, PSKeyword keywords, PSChannel channel)
        {
            return (_channelFilter & (ulong)channel) != 0
                && IsEnabled(level, keywords);
        }

#region resource manager

        private static global::System.Resources.ResourceManager _resourceManager;
        private static global::System.Globalization.CultureInfo _resourceCulture;

        private static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                _resourceManager ??= new global::System.Resources.ResourceManager("System.Management.Automation.resources.EventResource", typeof(EventResource).Assembly);

                return _resourceManager;
            }
        }

        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture
        {
            get
            {
                return _resourceCulture;
            }

            set
            {
                _resourceCulture = value;
            }
        }

        private static string GetResourceString(string resourceName)
        {
            string value = ResourceManager.GetString(resourceName, Culture);
            if (string.IsNullOrEmpty(value))
            {
                value = string.Create(CultureInfo.InvariantCulture, $"Unknown resource: {resourceName}");
                Diagnostics.Assert(false, value);
            }

            return value;
        }

#endregion resource manager

        
        private static void GetEventMessage(StringBuilder sb, PSEventId eventId, params object[] args )
        {
            int parameterCount;
            string resourceName = EventResource.GetMessage((int)eventId, out parameterCount);

            if (resourceName == null)
            {
                // If an event id was specified that is not found in the event resource lookup table,
                // use a placeholder message that includes the event id.
                resourceName = EventResource.GetMissingEventMessage(out parameterCount);
                Diagnostics.Assert(false, sb.ToString());
                args = new object[] {eventId};
            }

            string resourceValue = GetResourceString(resourceName);
            if (parameterCount > 0)
            {
                sb.AppendFormat(resourceValue, args);
            }
            else
            {
                sb.Append(resourceValue);
            }
        }

#region logging

        // maps a LogLevel to an associated SysLogPriority.
        private static readonly NativeMethods.SysLogPriority[] _levels =
        {
            NativeMethods.SysLogPriority.Info,
            NativeMethods.SysLogPriority.Critical,
            NativeMethods.SysLogPriority.Error,
            NativeMethods.SysLogPriority.Warning,
            NativeMethods.SysLogPriority.Info,
            NativeMethods.SysLogPriority.Info
        };

        
        public void LogTransfer(Guid parentActivityId)
        {
            // NOTE: always log
            int threadId = Environment.CurrentManagedThreadId;
            string message = string.Format(CultureInfo.InvariantCulture,
                                           "({0}:{1:X}:{2:X}) [Transfer]:{3} {4}",
                                           PSVersionInfo.GitCommitId, threadId, PSChannel.Operational,
                                           parentActivityId.ToString("B"),
                                           Activity.ToString("B"));

            NativeMethods.SysLog(NativeMethods.SysLogPriority.Info, message);
        }

        
        public void SetActivity(Guid activity)
        {
            int threadId = Environment.CurrentManagedThreadId;
            Activity = activity;

            // NOTE: always log
            string message = string.Format(CultureInfo.InvariantCulture,
                                           "({0:X}:{1:X}:{2:X}) [Activity] {3}",
                                           PSVersionInfo.GitCommitId, threadId, PSChannel.Operational, activity.ToString("B"));
            NativeMethods.SysLog(NativeMethods.SysLogPriority.Info, message);
        }

        
        public void Log(PSEventId eventId, PSChannel channel, PSTask task, PSOpcode opcode, PSLevel level, PSKeyword keyword, params object[] args)
        {
            if (ShouldLog(level, keyword, channel))
            {
                int threadId = Environment.CurrentManagedThreadId;

                StringBuilder sb = MessageBuilder;
                sb.Clear();

                // add the message preamble
                sb.AppendFormat(CultureInfo.InvariantCulture,
                                "({0}:{1:X}:{2:X}) [{3:G}:{4:G}.{5:G}.{6:G}] ",
                                PSVersionInfo.GitCommitId, threadId, channel, eventId, task, opcode, level);

                // add the message
                GetEventMessage(sb, eventId, args);

                NativeMethods.SysLogPriority priority;
                if ((int)level <= _levels.Length)
                {
                    priority = _levels[(int)level];
                }
                else
                {
                    priority = NativeMethods.SysLogPriority.Info;
                }
                // log it.
                NativeMethods.SysLog(priority, sb.ToString());
            }
        }

#endregion logging
    }

    internal enum LogLevel : uint
    {
        Always = 0,
        Critical = 1,
        Error = 2,
        Warning = 3,
        Information = 4,
        Verbose = 5
    }

    internal static class NativeMethods
    {
        private const string libpslnative = "libpsl-native";
        
        [DllImport(libpslnative, CharSet = CharSet.Ansi, EntryPoint = "Native_SysLog")]
        internal static extern void SysLog(SysLogPriority priority, string message);

        [DllImport(libpslnative, CharSet = CharSet.Ansi, EntryPoint = "Native_OpenLog")]
        internal static extern void OpenLog(IntPtr ident, SysLogPriority facility);

        [DllImport(libpslnative, EntryPoint = "Native_CloseLog")]
        internal static extern void CloseLog();

        [Flags]
        internal enum SysLogPriority : uint
        {
            // Priorities enum values.

            
            Emergency       = 0,

            
            Alert           = 1,

            
            Critical        = 2,

            
            Error           = 3,

            
            Warning         = 4,

            
            Notice          = 5,

            
            Info            = 6,

            
            Debug           = 7,

            // Facility enum values.

            
            Kernel          = (0 << 3),

            
            User            = (1 << 3),

            
            Mail            = (2 << 3),

            
            Daemon          = (3 << 3),

            
            Authorization   = (4 << 3),

            
            Syslog          = (5 << 3),

            
            Lpr             = (6 << 3),

            
            News            = (7 << 3),

            
            Uucp            = (8 << 3),

            
            Cron            = (9 << 3),

            
            Authpriv        = (10 << 3),

            
            Ftp             = (11 << 3),

            // Reserved for system use

            
            Local0          = (16 << 3),
            
            Local1          = (17 << 3),
            
            Local2          = (18 << 3),
            
            Local3          = (19 << 3),
            
            Local4          = (20 << 3),
            
            Local5          = (21 << 3),
            
            Local6          = (22 << 3),
            
            Local7          = (23 << 3),
        }
    }
}

#endif // UNIX
