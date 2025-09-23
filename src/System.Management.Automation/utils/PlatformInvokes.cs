// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace System.Management.Automation
{
    internal static class PlatformInvokes
    {
        [StructLayout(LayoutKind.Sequential)]
        internal sealed class FILETIME
        {
            internal uint dwLowDateTime;
            internal uint dwHighDateTime;

            internal FILETIME()
            {
                dwLowDateTime = 0;
                dwHighDateTime = 0;
            }

            internal FILETIME(long fileTime)
            {
                dwLowDateTime = (uint)fileTime;
                dwHighDateTime = (uint)(fileTime >> 32);
            }

            public long ToTicks()
            {
                return ((long)dwHighDateTime << 32) + dwLowDateTime;
            }
        }

        [Flags]
        // dwDesiredAccess of CreateFile
        internal enum FileDesiredAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,
        }

        [Flags]
        // dwShareMode of CreateFile
        internal enum FileShareMode : uint
        {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004,
        }

        // dwCreationDisposition of CreateFile
        internal enum FileCreationDisposition : uint
        {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5,
        }

        [Flags]
        // dwFlagsAndAttributes
        internal enum FileAttributes : uint
        {
            ReadOnly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Write_Through = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            SessionAware = 0x00800000
        }

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class SecurityAttributes
        {
            internal int nLength;
            internal SafeLocalMemHandle lpSecurityDescriptor;
            internal bool bInheritHandle;

            internal SecurityAttributes()
            {
                this.nLength = 12;
                this.bInheritHandle = true;
                this.lpSecurityDescriptor = new SafeLocalMemHandle(IntPtr.Zero, true);
            }
        }

        internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            internal SafeLocalMemHandle()
                : base(true)
            {
            }

            internal SafeLocalMemHandle(IntPtr existingHandle, bool ownsHandle)
                : base(ownsHandle)
            {
                base.SetHandle(existingHandle);
            }

            [DllImport(PinvokeDllNames.LocalFreeDllName)]
            private static extern IntPtr LocalFree(IntPtr hMem);

            protected override bool ReleaseHandle()
            {
                return (LocalFree(base.handle) == IntPtr.Zero);
            }
        }

        
        [DllImport(PinvokeDllNames.CreateFileDllName, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateFile(
            string lpFileName,
            FileDesiredAccess dwDesiredAccess,
            FileShareMode dwShareMode,
            IntPtr lpSecurityAttributes,
            FileCreationDisposition dwCreationDisposition,
            FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        
        [DllImport(PinvokeDllNames.CloseHandleDllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(PinvokeDllNames.DosDateTimeToFileTimeDllName, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DosDateTimeToFileTime(
            short wFatDate, // _In_   WORD
            short wFatTime, // _In_   WORD
            FILETIME lpFileTime); // _Out_ LPFILETIME

        [DllImport(PinvokeDllNames.LocalFileTimeToFileTimeDllName, SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LocalFileTimeToFileTime(
            FILETIME lpLocalFileTime, // _In_   const FILETIME *
            FILETIME lpFileTime); // _Out_ LPFILETIME

        [DllImport(PinvokeDllNames.SetFileTimeDllName, SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetFileTime(
            IntPtr hFile, // _In_      HANDLE
            FILETIME lpCreationTime, // _In_opt_ const FILETIME *
            FILETIME lpLastAccessTime, // _In_opt_ const FILETIME *
            FILETIME lpLastWriteTime); // _In_opt_ const FILETIME *

        [DllImport(PinvokeDllNames.SetFileAttributesWDllName, SetLastError = false, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetFileAttributesW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName, // _In_ LPCTSTR
            FileAttributes dwFileAttributes); // _In_ DWORD

        
        internal static bool EnableTokenPrivilege(string privilegeName, ref TOKEN_PRIVILEGE oldPrivilegeState)
        {
            bool success = false;
            TOKEN_PRIVILEGE newPrivilegeState = new TOKEN_PRIVILEGE();

            // Check if the caller has the specified privilege or not
            if (LookupPrivilegeValue(null, privilegeName, ref newPrivilegeState.Privilege.Luid))
            {
                // Get the pseudo handler of the current process
                IntPtr processHandler = GetCurrentProcess();
                if (processHandler != IntPtr.Zero)
                {
                    // Get the handler of the current process's access token
                    IntPtr tokenHandler = IntPtr.Zero;
                    if (OpenProcessToken(processHandler, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandler))
                    {
                        // Check if the specified privilege is already enabled
                        PRIVILEGE_SET requiredPrivilege = new PRIVILEGE_SET();
                        requiredPrivilege.Privilege.Luid = newPrivilegeState.Privilege.Luid;
                        requiredPrivilege.PrivilegeCount = 1;
                        // PRIVILEGE_SET_ALL_NECESSARY is defined as 1
                        requiredPrivilege.Control = 1;
                        bool privilegeEnabled = false;

                        if (PrivilegeCheck(tokenHandler, ref requiredPrivilege, out privilegeEnabled) && privilegeEnabled)
                        {
                            // The specified privilege is already enabled
                            oldPrivilegeState.PrivilegeCount = 0;
                            success = true;
                        }
                        else
                        {
                            // The specified privilege is not enabled yet. Enable it.
                            newPrivilegeState.PrivilegeCount = 1;
                            newPrivilegeState.Privilege.Attributes = SE_PRIVILEGE_ENABLED;
                            int bufferSize = Marshal.SizeOf<TOKEN_PRIVILEGE>();
                            int returnSize = 0;

                            // enable the specified privilege
                            if (AdjustTokenPrivileges(tokenHandler, false, ref newPrivilegeState, bufferSize, out oldPrivilegeState, ref returnSize))
                            {
                                // AdjustTokenPrivileges returns true does not mean all specified privileges have been successfully enabled
                                int retCode = Marshal.GetLastWin32Error();
                                if (retCode == ERROR_SUCCESS)
                                {
                                    success = true;
                                }
                                else if (retCode == 1300)
                                {
                                    // 1300 - Not all privileges referenced are assigned to the caller. This means the specified privilege is not
                                    // assigned to the current user. For example, suppose the role of current caller is "User", then privilege "SeRemoteShutdownPrivilege"
                                    // is not assigned to the role. In this case, we just return true and leave the call to "Win32Shutdown" to decide
                                    // whether the permission is granted or not.
                                    // Set oldPrivilegeState.PrivilegeCount to 0 to avoid the privilege restore later (PrivilegeCount - how many privileges are modified)
                                    oldPrivilegeState.PrivilegeCount = 0;
                                    success = true;
                                }
                            }
                        }
                    }

                    // Close the token handler and the process handler
                    if (tokenHandler != IntPtr.Zero)
                    {
                        CloseHandle(tokenHandler);
                    }

                    CloseHandle(processHandler);
                }
            }

            return success;
        }

        
        internal static bool RestoreTokenPrivilege(string privilegeName, ref TOKEN_PRIVILEGE previousPrivilegeState)
        {
            // The privilege was not changed, do not need to restore it.
            if (previousPrivilegeState.PrivilegeCount == 0)
            {
                return true;
            }

            bool success = false;
            TOKEN_PRIVILEGE newState = new TOKEN_PRIVILEGE();

            // Check if the caller has the specified privilege or not. If the caller has it, check the LUID specified in previousPrivilegeState
            // to see if the previousPrivilegeState is defined for the same privilege
            if (LookupPrivilegeValue(null, privilegeName, ref newState.Privilege.Luid) &&
                newState.Privilege.Luid.HighPart == previousPrivilegeState.Privilege.Luid.HighPart &&
                newState.Privilege.Luid.LowPart == previousPrivilegeState.Privilege.Luid.LowPart)
            {
                // Get the pseudo handler of the current process
                IntPtr processHandler = GetCurrentProcess();
                if (processHandler != IntPtr.Zero)
                {
                    // Get the handler of the current process's access token
                    IntPtr tokenHandler = IntPtr.Zero;
                    if (OpenProcessToken(processHandler, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandler))
                    {
                        int bufferSize = Marshal.SizeOf<TOKEN_PRIVILEGE>();
                        int returnSize = 0;

                        // restore the privilege state back to the previous privilege state
                        if (AdjustTokenPrivileges(tokenHandler, false, ref previousPrivilegeState, bufferSize, out newState, ref returnSize))
                        {
                            if (Marshal.GetLastWin32Error() == ERROR_SUCCESS)
                            {
                                success = true;
                            }
                        }
                    }

                    if (tokenHandler != IntPtr.Zero)
                    {
                        CloseHandle(tokenHandler);
                    }

                    CloseHandle(processHandler);
                }
            }

            return success;
        }

        
        [DllImport(PinvokeDllNames.LookupPrivilegeValueDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);

        
        [DllImport(PinvokeDllNames.PrivilegeCheckDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PrivilegeCheck(IntPtr tokenHandler, ref PRIVILEGE_SET requiredPrivileges, out bool pfResult);

        
        [DllImport(PinvokeDllNames.AdjustTokenPrivilegesDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(IntPtr tokenHandler, bool disableAllPrivilege,
                                                          ref TOKEN_PRIVILEGE newPrivilegeState, int bufferLength,
                                                          out TOKEN_PRIVILEGE previousPrivilegeState,
                                                          ref int returnLength);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TOKEN_PRIVILEGE
        {
            internal uint PrivilegeCount;
            internal LUID_AND_ATTRIBUTES Privilege;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LUID
        {
            internal uint LowPart;
            internal uint HighPart;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LUID_AND_ATTRIBUTES
        {
            internal LUID Luid;
            internal uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct PRIVILEGE_SET
        {
            internal uint PrivilegeCount;
            internal uint Control;
            internal LUID_AND_ATTRIBUTES Privilege;
        }

        
        [DllImport(PinvokeDllNames.GetCurrentProcessDllName)]
        internal static extern IntPtr GetCurrentProcess();

        
        [DllImport(PinvokeDllNames.OpenProcessTokenDllName, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

        
        internal const int TOKEN_QUERY = 0x00000008;

        
        internal const int TOKEN_ALL_ACCESS = 0x001f01ff;

        internal const uint SE_PRIVILEGE_DISABLED = 0x00000000;
        internal const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        internal const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const uint SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;

        internal const int ERROR_SUCCESS = 0x0;

        #region CreateProcess for SSH Remoting

#if !UNIX

        // Fields
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        internal static readonly UInt32 GENERIC_READ = 0x80000000;
        internal static readonly UInt32 GENERIC_WRITE = 0x40000000;
        internal static readonly UInt32 FILE_ATTRIBUTE_NORMAL = 0x80000000;
        internal static readonly UInt32 CREATE_ALWAYS = 2;
        internal static readonly UInt32 FILE_SHARE_WRITE = 0x00000002;
        internal static readonly UInt32 FILE_SHARE_READ = 0x00000001;
        internal static readonly UInt32 OF_READWRITE = 0x00000002;
        internal static readonly UInt32 OPEN_EXISTING = 3;

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;

            public PROCESS_INFORMATION()
            {
                this.hProcess = IntPtr.Zero;
                this.hThread = IntPtr.Zero;
            }

            
            public void Dispose()
            {
                Dispose(true);
            }

            
            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (this.hProcess != IntPtr.Zero)
                    {
                        CloseHandle(this.hProcess);
                        this.hProcess = IntPtr.Zero;
                    }

                    if (this.hThread != IntPtr.Zero)
                    {
                        CloseHandle(this.hThread);
                        this.hThread = IntPtr.Zero;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public SafeFileHandle hStdInput;
            public SafeFileHandle hStdOutput;
            public SafeFileHandle hStdError;

            public STARTUPINFO()
            {
                this.lpReserved = IntPtr.Zero;
                this.lpDesktop = IntPtr.Zero;
                this.lpTitle = IntPtr.Zero;
                this.lpReserved2 = IntPtr.Zero;
                this.hStdInput = new SafeFileHandle(IntPtr.Zero, false);
                this.hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
                this.hStdError = new SafeFileHandle(IntPtr.Zero, false);
                this.cb = Marshal.SizeOf(this);
            }

            public void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if ((this.hStdInput != null) && !this.hStdInput.IsInvalid)
                    {
                        this.hStdInput.Dispose();
                        this.hStdInput = null;
                    }

                    if ((this.hStdOutput != null) && !this.hStdOutput.IsInvalid)
                    {
                        this.hStdOutput.Dispose();
                        this.hStdOutput = null;
                    }

                    if ((this.hStdError != null) && !this.hStdError.IsInvalid)
                    {
                        this.hStdError.Dispose();
                        this.hStdError = null;
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public SafeLocalMemHandle lpSecurityDescriptor;
            public bool bInheritHandle;

            public SECURITY_ATTRIBUTES()
            {
                this.nLength = 12;
                this.bInheritHandle = true;
                this.lpSecurityDescriptor = new SafeLocalMemHandle(IntPtr.Zero, true);
            }
        }

        [DllImport(PinvokeDllNames.CreateProcessDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcess(
            [MarshalAs(UnmanagedType.LPWStr)] string lpApplicationName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCommandLine,
            SECURITY_ATTRIBUTES lpProcessAttributes,
            SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            [MarshalAs(UnmanagedType.LPWStr)] string lpCurrentDirectory,
            STARTUPINFO lpStartupInfo,
            PROCESS_INFORMATION lpProcessInformation);

        [DllImport(PinvokeDllNames.ResumeThreadDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint ResumeThread(IntPtr threadHandle);

        internal static readonly uint RESUME_THREAD_FAILED = System.UInt32.MaxValue; // (DWORD)-1

#endif

        #endregion
    }
}
