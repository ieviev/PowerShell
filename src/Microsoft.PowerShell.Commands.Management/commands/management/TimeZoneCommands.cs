// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "TimeZone", DefaultParameterSetName = "Name",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096904")]
    [OutputType(typeof(TimeZoneInfo))]
    [Alias("gtz")]
    public class GetTimeZoneCommand : PSCmdlet
    {
        #region Parameters

        
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Id")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Id { get; set; }

        
        [Parameter(Mandatory = true, ParameterSetName = "ListAvailable")]
        public SwitchParameter ListAvailable { get; set; }

        
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "Name")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name { get; set; }

        #endregion Parameters

        
        protected override void ProcessRecord()
        {
            // make sure we've got the latest time zone settings
            TimeZoneInfo.ClearCachedData();

            if (this.ParameterSetName.Equals("ListAvailable", StringComparison.OrdinalIgnoreCase))
            {
                // output the list of all available time zones
                WriteObject(TimeZoneInfo.GetSystemTimeZones(), true);
            }
            else if (this.ParameterSetName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                // lookup each time zone id
                foreach (string tzid in Id)
                {
                    try
                    {
                        WriteObject(TimeZoneInfo.FindSystemTimeZoneById(tzid));
                    }
                    catch (TimeZoneNotFoundException e)
                    {
                        WriteError(new ErrorRecord(e, TimeZoneHelper.TimeZoneNotFoundError,
                            ErrorCategory.InvalidArgument, "Id"));
                    }
                }
            }
            else // ParameterSetName == "Name"
            {
                if (Name != null)
                {
                    // lookup each time zone name (or wildcard pattern)
                    foreach (string tzname in Name)
                    {
                        TimeZoneInfo[] timeZones = TimeZoneHelper.LookupSystemTimeZoneInfoByName(tzname);
                        if (timeZones.Length > 0)
                        {
                            // manually process each object in the array, so if there is only a single
                            // entry then the returned type is TimeZoneInfo and not TimeZoneInfo[], and
                            // it can be pipelined to Set-TimeZone more easily
                            foreach (TimeZoneInfo timeZone in timeZones)
                            {
                                WriteObject(timeZone);
                            }
                        }
                        else
                        {
                            string message = string.Format(CultureInfo.InvariantCulture,
                                TimeZoneResources.TimeZoneNameNotFound, tzname);

                            Exception e = new TimeZoneNotFoundException(message);
                            WriteError(new ErrorRecord(e, TimeZoneHelper.TimeZoneNotFoundError,
                                ErrorCategory.InvalidArgument, "Name"));
                        }
                    }
                }
                else
                {
                    // return the current system local time zone
                    WriteObject(TimeZoneInfo.Local);
                }
            }
        }
    }

#if !UNIX

    
    [Cmdlet(VerbsCommon.Set, "TimeZone",
        SupportsShouldProcess = true,
        DefaultParameterSetName = "Name",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097056")]
    [OutputType(typeof(TimeZoneInfo))]
    [Alias("stz")]
    public class SetTimeZoneCommand : PSCmdlet
    {
        #region string constants

        private const string TimeZoneTarget = "Local System";

        #endregion string constants

        #region Parameters

        
        [Parameter(Mandatory = true, ParameterSetName = "Id", ValueFromPipelineByPropertyName = true)]
        public string Id { get; set; }

        
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "InputObject", ValueFromPipeline = true)]
        public TimeZoneInfo InputObject { get; set; }

        
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Name")]
        public string Name { get; set; }

        
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        #endregion Parameters

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "Since Name is not a parameter of this method, it confuses FXCop. It is the appropriate value for the exception.")]
        protected override void ProcessRecord()
        {
            // make sure we've got fresh data, in case the requested time zone was added
            // to the system (registry) after our process was started
            TimeZoneInfo.ClearCachedData();

            // acquire a TimeZoneInfo if one wasn't supplied.
            if (this.ParameterSetName.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    InputObject = TimeZoneInfo.FindSystemTimeZoneById(Id);
                }
                catch (TimeZoneNotFoundException e)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        e,
                        TimeZoneHelper.TimeZoneNotFoundError,
                        ErrorCategory.InvalidArgument,
                        "Id"));
                }
            }
            else if (this.ParameterSetName.Equals("Name", StringComparison.OrdinalIgnoreCase))
            {
                // lookup the time zone name and make sure we have one (and only one) match
                TimeZoneInfo[] timeZones = TimeZoneHelper.LookupSystemTimeZoneInfoByName(Name);
                if (timeZones.Length == 0)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        TimeZoneResources.TimeZoneNameNotFound, Name);
                    Exception e = new TimeZoneNotFoundException(message);
                    ThrowTerminatingError(new ErrorRecord(e,
                        TimeZoneHelper.TimeZoneNotFoundError,
                        ErrorCategory.InvalidArgument,
                        "Name"));
                }
                else if (timeZones.Length > 1)
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        TimeZoneResources.MultipleMatchingTimeZones, Name);
                    ThrowTerminatingError(new ErrorRecord(
                            new PSArgumentException(message, "Name"),
                            TimeZoneHelper.MultipleMatchingTimeZonesError,
                            ErrorCategory.InvalidArgument,
                            "Name"));
                }
                else
                {
                    InputObject = timeZones[0];
                }
            }
            else // ParameterSetName == "InputObject"
            {
                try
                {
                    // a TimeZoneInfo object was supplied, so use it to make sure we can find
                    // a backing system time zone, otherwise it's an error condition
                    InputObject = TimeZoneInfo.FindSystemTimeZoneById(InputObject.Id);
                }
                catch (TimeZoneNotFoundException e)
                {
                    ThrowTerminatingError(new ErrorRecord(
                        e,
                        TimeZoneHelper.TimeZoneNotFoundError,
                        ErrorCategory.InvalidArgument,
                        "InputObject"));
                }
            }

            if (ShouldProcess(TimeZoneTarget))
            {
                bool acquireAccess = false;
                try
                {
                    // check to see if permission to set the time zone is already enabled for this process
                    if (!HasAccess)
                    {
                        // acquire permissions to set the timezone
                        SetAccessToken(true);
                        acquireAccess = true;
                    }
                }
                catch (Win32Exception e)
                {
                    ThrowTerminatingError(new ErrorRecord(e,
                        TimeZoneHelper.InsufficientPermissionsError,
                        ErrorCategory.PermissionDenied, null));
                }

                try
                {
                    // construct and populate a new DYNAMIC_TIME_ZONE_INFORMATION structure
                    NativeMethods.DYNAMIC_TIME_ZONE_INFORMATION dtzi = new();
                    dtzi.Bias -= (int)InputObject.BaseUtcOffset.TotalMinutes;
                    dtzi.StandardName = InputObject.StandardName;
                    dtzi.DaylightName = InputObject.DaylightName;
                    dtzi.TimeZoneKeyName = InputObject.Id;

                    // Request time zone transition information for the current year
                    NativeMethods.TIME_ZONE_INFORMATION tzi = new();
                    if (!NativeMethods.GetTimeZoneInformationForYear((ushort)DateTime.Now.Year, ref dtzi, ref tzi))
                    {
                        ThrowWin32Error();
                    }

                    // copy over the transition times
                    dtzi.StandardBias = tzi.StandardBias;
                    dtzi.StandardDate = tzi.StandardDate;
                    dtzi.DaylightBias = tzi.DaylightBias;
                    dtzi.DaylightDate = tzi.DaylightDate;

                    // set the new local time zone for the system
                    if (!NativeMethods.SetDynamicTimeZoneInformation(ref dtzi))
                    {
                        ThrowWin32Error();
                    }

                    // broadcast a WM_SETTINGCHANGE notification message to all top-level windows so that they
                    // know to update their notion of the current system time (and time zone) if applicable
                    int result = 0;
                    NativeMethods.SendMessageTimeout((IntPtr)NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE,
                        (IntPtr)0, "intl", NativeMethods.SMTO_ABORTIFHUNG, 5000, ref result);

                    // clear the time zone data or this PowerShell session
                    // will not recognize the new time zone settings
                    TimeZoneInfo.ClearCachedData();

                    if (PassThru.IsPresent)
                    {
                        // return the TimeZoneInfo object for the (new) current local time zone
                        WriteObject(TimeZoneInfo.Local);
                    }
                }
                catch (Win32Exception e)
                {
                    ThrowTerminatingError(new ErrorRecord(e,
                        TimeZoneHelper.SetTimeZoneFailedError,
                        ErrorCategory.FromStdErr, null));
                }
                finally
                {
                    if (acquireAccess)
                    {
                        // reset the permissions
                        SetAccessToken(false);
                    }
                }
            }
            else
            {
                if (PassThru.IsPresent)
                {
                    // show the user the time zone settings that would have been used.
                    WriteObject(InputObject);
                }
            }
        }

        #region Helper functions

        
        protected bool HasAccess
        {
            get
            {
                bool hasAccess = false;

                // open the access token for the current process
                IntPtr hToken = IntPtr.Zero;
                IntPtr hProcess = NativeMethods.GetCurrentProcess();
                if (!NativeMethods.OpenProcessToken(hProcess,
                    NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY, ref hToken))
                {
                    ThrowWin32Error();
                }

                try
                {
                    // setup the privileges being checked
                    NativeMethods.PRIVILEGE_SET ps = new()
                    {
                        PrivilegeCount = 1,
                        Control = 1,
                        Luid = 0,
                        Attributes = NativeMethods.SE_PRIVILEGE_ENABLED,
                    };

                    // lookup the Luid of the SeTimeZonePrivilege
                    if (!NativeMethods.LookupPrivilegeValue(null, NativeMethods.SE_TIME_ZONE_NAME, ref ps.Luid))
                    {
                        ThrowWin32Error();
                    }

                    // set the privilege for the open access token
                    if (!NativeMethods.PrivilegeCheck(hToken, ref ps, ref hasAccess))
                    {
                        ThrowWin32Error();
                    }
                }
                finally
                {
                    NativeMethods.CloseHandle(hToken);
                }

                return (hasAccess);
            }
        }

        
        protected void SetAccessToken(bool enable)
        {
            // open the access token for the current process
            IntPtr hToken = IntPtr.Zero;
            IntPtr hProcess = NativeMethods.GetCurrentProcess();
            if (!NativeMethods.OpenProcessToken(hProcess,
                NativeMethods.TOKEN_ADJUST_PRIVILEGES | NativeMethods.TOKEN_QUERY, ref hToken))
            {
                ThrowWin32Error();
            }

            try
            {
                // setup the privileges being requested
                NativeMethods.TOKEN_PRIVILEGES tp = new()
                {
                    PrivilegeCount = 1,
                    Luid = 0,
                    Attributes = (enable ? NativeMethods.SE_PRIVILEGE_ENABLED : 0),
                };

                // lookup the Luid of the SeTimeZonePrivilege
                if (!NativeMethods.LookupPrivilegeValue(null, NativeMethods.SE_TIME_ZONE_NAME, ref tp.Luid))
                {
                    ThrowWin32Error();
                }

                // set the privilege for the open access token
                if (!NativeMethods.AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    ThrowWin32Error();
                }
            }
            finally
            {
                NativeMethods.CloseHandle(hToken);
            }
        }

        
        protected void ThrowWin32Error()
        {
            int error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error);
        }

        #endregion Helper functions

        #region Win32 interop helper

        internal static class NativeMethods
        {
            #region Native DLL locations

            private const string SetDynamicTimeZoneApiDllName = "api-ms-win-core-timezone-l1-1-0.dll";
            private const string GetTimeZoneInformationForYearApiDllName = "api-ms-win-core-timezone-l1-1-0.dll";
            private const string GetCurrentProcessApiDllName = "api-ms-win-downlevel-kernel32-l1-1-0.dll";
            private const string OpenProcessTokenApiDllName = "api-ms-win-downlevel-advapi32-l1-1-1.dll";
            private const string LookupPrivilegeTokenApiDllName = "api-ms-win-downlevel-advapi32-l4-1-0.dll";
            private const string PrivilegeCheckApiDllName = "api-ms-win-downlevel-advapi32-l1-1-1.dll";
            private const string AdjustTokenPrivilegesApiDllName = "api-ms-win-downlevel-advapi32-l1-1-1.dll";
            private const string CloseHandleApiDllName = "api-ms-win-downlevel-kernel32-l1-1-0.dll";
            private const string SendMessageTimeoutApiDllName = "ext-ms-win-rtcore-ntuser-window-ext-l1-1-0.dll";

            #endregion Native DLL locations

            #region Win32 SetDynamicTimeZoneInformation imports

            
            [StructLayout(LayoutKind.Sequential)]
            public struct SystemTime
            {
                
                [MarshalAs(UnmanagedType.U2)]
                public short Year;
                
                [MarshalAs(UnmanagedType.U2)]
                public short Month;
                
                [MarshalAs(UnmanagedType.U2)]
                public short DayOfWeek;
                
                [MarshalAs(UnmanagedType.U2)]
                public short Day;
                
                [MarshalAs(UnmanagedType.U2)]
                public short Hour;
                
                [MarshalAs(UnmanagedType.U2)]
                public short Minute;
                
                [MarshalAs(UnmanagedType.U2)]
                public short Second;
                
                [MarshalAs(UnmanagedType.U2)]
                public short Milliseconds;
            }

            
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct DYNAMIC_TIME_ZONE_INFORMATION
            {
                
                [MarshalAs(UnmanagedType.I4)]
                public int Bias;
                
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string StandardName;
                
                public SystemTime StandardDate;
                
                [MarshalAs(UnmanagedType.I4)]
                public int StandardBias;
                
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string DaylightName;
                
                public SystemTime DaylightDate;
                
                [MarshalAs(UnmanagedType.I4)]
                public int DaylightBias;
                
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
                public string TimeZoneKeyName;
                
                [MarshalAs(UnmanagedType.U1)]
                public bool DynamicDaylightTimeDisabled;
            }

            
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct TIME_ZONE_INFORMATION
            {
                
                [MarshalAs(UnmanagedType.I4)]
                public int Bias;
                
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string StandardName;
                
                public SystemTime StandardDate;
                
                [MarshalAs(UnmanagedType.I4)]
                public int StandardBias;
                
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
                public string DaylightName;
                
                public SystemTime DaylightDate;
                
                [MarshalAs(UnmanagedType.I4)]
                public int DaylightBias;
            }

            
            [DllImport(SetDynamicTimeZoneApiDllName, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetDynamicTimeZoneInformation([In] ref DYNAMIC_TIME_ZONE_INFORMATION lpTimeZoneInformation);

            [DllImport(GetTimeZoneInformationForYearApiDllName, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetTimeZoneInformationForYear([In] ushort wYear, [In] ref DYNAMIC_TIME_ZONE_INFORMATION pdtzi, ref TIME_ZONE_INFORMATION ptzi);

            #endregion Win32 SetDynamicTimeZoneInformation imports

            #region Win32 AdjustTokenPrivilege imports

            
            public const int TOKEN_QUERY = 0x00000008;

            
            public const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

            
            public const int SE_PRIVILEGE_ENABLED = 0x00000002;

            
            public const string SE_TIME_ZONE_NAME = "SeTimeZonePrivilege"; // https://msdn.microsoft.com/library/bb530716(VS.85).aspx

            
            [DllImport(GetCurrentProcessApiDllName, ExactSpelling = true)]
            public static extern IntPtr GetCurrentProcess();

            
            [DllImport(OpenProcessTokenApiDllName, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

            
            [DllImport(LookupPrivilegeTokenApiDllName, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref long lpLuid);

            
            [DllImport(PrivilegeCheckApiDllName, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool PrivilegeCheck(IntPtr ClientToken, ref PRIVILEGE_SET RequiredPrivileges, ref bool pfResult);

            
            [DllImport(AdjustTokenPrivilegesApiDllName, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
                ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

            
            [DllImport(CloseHandleApiDllName, ExactSpelling = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

            
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct PRIVILEGE_SET
            {
                public int PrivilegeCount;
                public int Control;
                public long Luid;
                public int Attributes;
            }

            
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct TOKEN_PRIVILEGES
            {
                public int PrivilegeCount;
                public long Luid;
                public int Attributes;
            }

            #endregion Win32 AdjustTokenPrivilege imports

            #region Win32 SendMessage imports

            
            public const int WM_SETTINGCHANGE = 0x001A;

            
            public const int HWND_BROADCAST = (-1);

            
            public const int SMTO_ABORTIFHUNG = 0x0002;

            
            [DllImport(SendMessageTimeoutApiDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, string lParam, int fuFlags, int uTimeout, ref int lpdwResult);

            #endregion Win32 SendMessage imports
        }

        #endregion Win32 interop helper
    }

#endif
    
    internal static class TimeZoneHelper
    {
        #region Error Ids

        internal const string TimeZoneNotFoundError = "TimeZoneNotFound";
        internal const string MultipleMatchingTimeZonesError = "MultipleMatchingTimeZones";
        internal const string InsufficientPermissionsError = "InsufficientPermissions";
        internal const string SetTimeZoneFailedError = "SetTimeZoneFailed";

        #endregion Error Ids

        
        internal static TimeZoneInfo[] LookupSystemTimeZoneInfoByName(string name)
        {
            WildcardPattern namePattern = new(name, WildcardOptions.IgnoreCase);
            List<TimeZoneInfo> tzi = new();

            // get the available system time zones
            ReadOnlyCollection<TimeZoneInfo> zones = TimeZoneInfo.GetSystemTimeZones();

            // check against the standard and daylight names for each TimeZoneInfo
            foreach (TimeZoneInfo zone in zones)
            {
                if (namePattern.IsMatch(zone.StandardName) || namePattern.IsMatch(zone.DaylightName))
                {
                    tzi.Add(zone);
                }
            }

            return (tzi.ToArray());
        }
    }
}
