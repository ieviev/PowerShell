// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Management.Automation.Internal;
using Microsoft.Win32;

namespace System.Management.Automation
{
    
    public static partial class Platform
    {
        
        public static bool IsLinux
        {
            get
            {
                return OperatingSystem.IsLinux();
            }
        }

        
        public static bool IsMacOS
        {
            get
            {
                return OperatingSystem.IsMacOS();
            }
        }

        
        public static bool IsWindows
        {
            get
            {
                return OperatingSystem.IsWindows();
            }
        }

        
        public static bool IsCoreCLR
        {
            get
            {
                return true;
            }
        }

        
        public static bool IsNanoServer
        {
            get
            {
#if UNIX
                return false;
#else
                if (_isNanoServer.HasValue)
                {
                    return _isNanoServer.Value;
                }

                _isNanoServer = false;
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Server\ServerLevels"))
                {
                    if (regKey != null)
                    {
                        object value = regKey.GetValue("NanoServer");
                        if (value != null && regKey.GetValueKind("NanoServer") == RegistryValueKind.DWord)
                        {
                            _isNanoServer = (int)value == 1;
                        }
                    }
                }

                return _isNanoServer.Value;
#endif
            }
        }

        
        public static bool IsIoT
        {
            get
            {
#if UNIX
                return false;
#else
                if (_isIoT.HasValue)
                {
                    return _isIoT.Value;
                }

                _isIoT = false;
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (regKey != null)
                    {
                        object value = regKey.GetValue("ProductName");
                        if (value != null && regKey.GetValueKind("ProductName") == RegistryValueKind.String)
                        {
                            _isIoT = string.Equals("IoTUAP", (string)value, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }

                return _isIoT.Value;
#endif
            }
        }

        
        public static bool IsWindowsDesktop
        {
            get
            {
#if UNIX
                return false;
#else
                if (_isWindowsDesktop.HasValue)
                {
                    return _isWindowsDesktop.Value;
                }

                _isWindowsDesktop = !IsNanoServer && !IsIoT;
                return _isWindowsDesktop.Value;
#endif
            }
        }

        
        public static bool IsStaSupported
        {
            get
            {
#if UNIX
                return false;
#else
                return _isStaSupported.Value;
#endif
            }
        }

#if UNIX
        // Gets the location for cache and config folders.
        internal static readonly string CacheDirectory = Platform.SelectProductNameForDirectory(Platform.XDG_Type.CACHE);
        internal static readonly string ConfigDirectory = Platform.SelectProductNameForDirectory(Platform.XDG_Type.CONFIG);
#else
        // Gets the location for cache and config folders.
        internal static readonly string CacheDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\PowerShell";
        internal static readonly string ConfigDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\PowerShell";

        private static readonly Lazy<bool> _isStaSupported = new Lazy<bool>(() =>
        {
            int result = Interop.Windows.CoInitializeEx(IntPtr.Zero, Interop.Windows.COINIT_APARTMENTTHREADED);

            // If 0 is returned the thread has been initialized for the first time
            // as an STA and thus supported and needs to be uninitialized.
            if (result > 0)
            {
                Interop.Windows.CoUninitialize();
            }

            return result != Interop.Windows.E_NOTIMPL;
        });

        private static bool? _isNanoServer = null;
        private static bool? _isIoT = null;
        private static bool? _isWindowsDesktop = null;
#endif

        // format files
        internal static readonly string[] FormatFileNames = new string[]
        {
            "Certificate.format.ps1xml",
            "Diagnostics.format.ps1xml",
            "DotNetTypes.format.ps1xml",
            "Event.format.ps1xml",
            "FileSystem.format.ps1xml",
            "Help.format.ps1xml",
            "HelpV3.format.ps1xml",
            "PowerShellCore.format.ps1xml",
            "PowerShellTrace.format.ps1xml",
            "Registry.format.ps1xml",
            "WSMan.format.ps1xml"
        };

        
        internal static class CommonEnvVariableNames
        {
#if UNIX
            internal const string Home = "HOME";
#else
            internal const string Home = "USERPROFILE";
#endif
        }

#if UNIX
        private static string s_tempHome = null;

        
        private static string GetHomeOrCreateTempHome()
        {
            const string tempHomeFolderName = "pwsh-{0}-98288ff9-5712-4a14-9a11-23693b9cd91a";

            string envHome = Environment.GetEnvironmentVariable("HOME") ?? s_tempHome;
            if (envHome is not null)
            {
                return envHome;
            }

            try
            {
                s_tempHome = Path.Combine(Path.GetTempPath(), StringUtil.Format(tempHomeFolderName, Environment.UserName));
                Directory.CreateDirectory(s_tempHome);
            }
            catch (UnauthorizedAccessException)
            {
                // Directory creation may fail if the account doesn't have filesystem permission such as some service accounts.
                // Return an empty string in this case so the process working directory will be used.
                s_tempHome = string.Empty;
            }

            return s_tempHome;
        }

        
        public enum XDG_Type
        {
            
            CONFIG,
            
            CACHE,
            
            DATA,
            
            USER_MODULES,
            
            SHARED_MODULES,
            
            DEFAULT
        }

        
        public static string SelectProductNameForDirectory(XDG_Type dirpath)
        {
            // TODO: XDG_DATA_DIRS implementation as per GitHub issue #1060

            string xdgconfighome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            string xdgdatahome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            string xdgcachehome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            string envHome = GetHomeOrCreateTempHome();

            string xdgConfigHomeDefault = Path.Combine(envHome, ".config", "powershell");
            string xdgDataHomeDefault = Path.Combine(envHome, ".local", "share", "powershell");
            string xdgModuleDefault = Path.Combine(xdgDataHomeDefault, "Modules");
            string xdgCacheDefault = Path.Combine(envHome, ".cache", "powershell");

            try
            {
                switch (dirpath)
                {
                    case XDG_Type.CONFIG:
                        // Use 'XDG_CONFIG_HOME' if it's set, otherwise use the default path.
                        return string.IsNullOrEmpty(xdgconfighome)
                            ? xdgConfigHomeDefault
                            : Path.Combine(xdgconfighome, "powershell");

                    case XDG_Type.DATA:
                        // Use 'XDG_DATA_HOME' if it's set, otherwise use the default path.
                        if (string.IsNullOrEmpty(xdgdatahome))
                        {
                            // Create the default data directory if it doesn't exist.
                            Directory.CreateDirectory(xdgDataHomeDefault);
                            return xdgDataHomeDefault;
                        }
                        return Path.Combine(xdgdatahome, "powershell");

                    case XDG_Type.USER_MODULES:
                        // Use 'XDG_DATA_HOME' if it's set, otherwise use the default path.
                        if (string.IsNullOrEmpty(xdgdatahome))
                        {
                            Directory.CreateDirectory(xdgModuleDefault);
                            return xdgModuleDefault;
                        }
                        return Path.Combine(xdgdatahome, "powershell", "Modules");

                    case XDG_Type.SHARED_MODULES:
                        return "/usr/local/share/powershell/Modules";

                    case XDG_Type.CACHE:
                        // Use 'XDG_CACHE_HOME' if it's set, otherwise use the default path.
                        if (string.IsNullOrEmpty(xdgcachehome))
                        {
                            Directory.CreateDirectory(xdgCacheDefault);
                            return xdgCacheDefault;
                        }

                        string cachePath = Path.Combine(xdgcachehome, "powershell");
                        Directory.CreateDirectory(cachePath);
                        return cachePath;

                    case XDG_Type.DEFAULT:
                        // Use 'xdgConfigHomeDefault' for 'XDG_Type.DEFAULT' and create the directory if it doesn't exist.
                        Directory.CreateDirectory(xdgConfigHomeDefault);
                        return xdgConfigHomeDefault;

                    default:
                        throw new InvalidOperationException("Unreachable code.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Directory creation may fail if the account doesn't have filesystem permission such as some service accounts.
                // Return an empty string in this case so the process working directory will be used.
                return string.Empty;
            }
        }
#endif

        
        internal static string GetFolderPath(Environment.SpecialFolder folder)
        {
#if UNIX
            return folder switch
            {
                Environment.SpecialFolder.ProgramFiles => Directory.Exists("/bin") ? "/bin" : string.Empty,
                Environment.SpecialFolder.MyDocuments => GetHomeOrCreateTempHome(),
                _ => throw new NotSupportedException()
            };
#else
            return Environment.GetFolderPath(folder);
#endif
        }

        // Platform methods prefixed NonWindows are:
        // - non-windows by the definition of the IsWindows method above
        // - here, because porting to Linux and other operating systems
        //   should not move the original Windows code out of the module
        //   it belongs to, so this way the windows code can remain in it's
        //   original source file and only the non-windows code has been moved
        //   out here
        // - only to be used with the IsWindows feature query, and only if
        //   no other more specific feature query makes sense

        internal static bool NonWindowsIsHardLink(FileSystemInfo fileInfo)
        {
            return Unix.IsHardLink(fileInfo);
        }

        internal static string NonWindowsGetUserFromPid(int path)
        {
            return Unix.NativeMethods.GetUserFromPid(path);
        }

        internal static string NonWindowsInternalGetLinkType(FileSystemInfo fileInfo)
        {
            if (fileInfo.Attributes.HasFlag(System.IO.FileAttributes.ReparsePoint))
            {
                return "SymbolicLink";
            }

            if (NonWindowsIsHardLink(fileInfo))
            {
                return "HardLink";
            }

            return null;
        }

        internal static bool NonWindowsCreateSymbolicLink(string path, string target)
        {
            // Linux doesn't care if target is a directory or not
            return Unix.NativeMethods.CreateSymLink(path, target) == 0;
        }

        internal static bool NonWindowsCreateHardLink(string path, string strTargetPath)
        {
            return Unix.NativeMethods.CreateHardLink(path, strTargetPath) == 0;
        }

        internal static unsafe bool NonWindowsSetDate(DateTime dateToUse)
        {
            Unix.NativeMethods.UnixTm tm = Unix.NativeMethods.DateTimeToUnixTm(dateToUse);
            return Unix.NativeMethods.SetDate(&tm) == 0;
        }

        internal static bool NonWindowsIsSameFileSystemItem(string pathOne, string pathTwo)
        {
            return Unix.NativeMethods.IsSameFileSystemItem(pathOne, pathTwo);
        }

        internal static bool NonWindowsGetInodeData(string path, out ValueTuple<ulong, ulong> inodeData)
        {
            var result = Unix.NativeMethods.GetInodeData(path, out ulong device, out ulong inode);

            inodeData = (device, inode);
            return result == 0;
        }

        internal static bool NonWindowsIsExecutable(string path)
        {
            return Unix.NativeMethods.IsExecutable(path);
        }

        internal static uint NonWindowsGetThreadId()
        {
            return Unix.NativeMethods.GetCurrentThreadId();
        }

        internal static int NonWindowsGetProcessParentPid(int pid)
        {
            return IsMacOS ? Unix.NativeMethods.GetPPid(pid) : Unix.GetProcFSParentPid(pid);
        }

        internal static bool NonWindowsKillProcess(int pid)
        {
            return Unix.NativeMethods.KillProcess(pid);
        }

        internal static int NonWindowsWaitPid(int pid, bool nohang)
        {
            return Unix.NativeMethods.WaitPid(pid, nohang);
        }

        // Please note that `Win32Exception(Marshal.GetLastWin32Error())`
        // works *correctly* on Linux in that it creates an exception with
        // the string perror would give you for the last set value of errno.
        // No manual mapping is required. .NET Core maps the Linux errno
        // to a PAL value and calls strerror_r underneath to generate the message.

        
        internal static partial class Unix
        {
            private static readonly Dictionary<int, string> usernameCache = new();
            private static readonly Dictionary<int, string> groupnameCache = new();

            
            public enum ItemType
            {
                
                Directory,

                
                File,

                
                SymbolicLink,

                
                BlockDevice,

                
                CharacterDevice,

                
                NamedPipe,

                
                Socket,
            }

            
            public enum StatMask
            {
                
                OwnerModeMask = 0x1C0,

                
                OwnerRead = 0x100,

                
                OwnerWrite = 0x080,

                
                OwnerExecute = 0x040,

                
                GroupModeMask = 0x038,

                
                GroupRead = 0x20,

                
                GroupWrite = 0x10,

                
                GroupExecute = 0x8,

                
                OtherModeMask = 0x007,

                
                OtherRead = 0x004,

                
                OtherWrite = 0x002,

                
                OtherExecute = 0x001,

                
                SetStickyMask = 0x200,

                
                SetGidMask = 0x400,

                
                SetUidMask = 0x800,
            }

            
            public class CommonStat
            {
                
                public long Inode;

                
                public int Mode;

                
                public int UserId;

                
                public int GroupId;

                
                public int HardlinkCount;

                
                public long Size;

                
                public DateTime AccessTime;

                
                public DateTime ModifiedTime;

                
                public DateTime StatusChangeTime;

                
                public long BlockSize;

                
                public int DeviceId;

                
                public int NumberOfBlocks;

                
                public ItemType ItemType;

                
                public bool IsSetUid;

                
                public bool IsSetGid;

                
                public bool IsSticky;

                private const char CanRead = 'r';
                private const char CanWrite = 'w';
                private const char CanExecute = 'x';
                private const char NoPerm = '-';
                private const char SetAndExec = 's';
                private const char SetAndNotExec = 'S';
                private const char StickyAndExec = 't';
                private const char StickyAndNotExec = 'T';

                // The item type and the character representation for the first element in the stat string
                private static readonly Dictionary<ItemType, char> itemTypeTable = new()
                {
                    { ItemType.BlockDevice,     'b' },
                    { ItemType.CharacterDevice, 'c' },
                    { ItemType.Directory,       'd' },
                    { ItemType.File,            '-' },
                    { ItemType.NamedPipe,       'p' },
                    { ItemType.Socket,          's' },
                    { ItemType.SymbolicLink,    'l' },
                };

                // We'll create a few common mode strings here to reduce allocations and improve performance a bit.
                private const string OwnerReadGroupReadOtherRead = "-r--r--r--";
                private const string OwnerReadWriteGroupReadOtherRead = "-rw-r--r--";
                private const string DirectoryOwnerFullGroupReadExecOtherReadExec = "drwxr-xr-x";

                
                public string GetModeString()
                {
                    // On an Ubuntu system (docker), these 3 are roughly 70% of all the permissions
                    if ((Mode & 0xFFF) == 292)
                    {
                        return OwnerReadGroupReadOtherRead;
                    }

                    if ((Mode & 0xFFF) == 420)
                    {
                       return OwnerReadWriteGroupReadOtherRead;
                    }

                    if (ItemType == ItemType.Directory & (Mode & 0xFFF) == 493)
                    {
                        return DirectoryOwnerFullGroupReadExecOtherReadExec;
                    }

                    Span<char> modeCharacters = stackalloc char[10];
                    modeCharacters[0] = itemTypeTable[ItemType];
                    bool isExecutable;

                    UnixFileMode modeInfo = (UnixFileMode)Mode;
                    modeCharacters[1] = modeInfo.HasFlag(UnixFileMode.UserRead) ? CanRead : NoPerm;
                    modeCharacters[2] = modeInfo.HasFlag(UnixFileMode.UserWrite) ? CanWrite : NoPerm;
                    isExecutable = modeInfo.HasFlag(UnixFileMode.UserExecute);
                    modeCharacters[3] = modeInfo.HasFlag(UnixFileMode.SetUser) ? (isExecutable ? SetAndExec : SetAndNotExec) : (isExecutable ? CanExecute : NoPerm);

                    modeCharacters[4] = modeInfo.HasFlag(UnixFileMode.GroupRead) ? CanRead : NoPerm;
                    modeCharacters[5] = modeInfo.HasFlag(UnixFileMode.GroupWrite) ? CanWrite : NoPerm;
                    isExecutable = modeInfo.HasFlag(UnixFileMode.GroupExecute);
                    modeCharacters[6] = modeInfo.HasFlag(UnixFileMode.SetGroup) ? (isExecutable ? SetAndExec : SetAndNotExec) : (isExecutable ? CanExecute : NoPerm);

                    modeCharacters[7] = modeInfo.HasFlag(UnixFileMode.OtherRead) ? CanRead : NoPerm;
                    modeCharacters[8] = modeInfo.HasFlag(UnixFileMode.OtherWrite) ? CanWrite : NoPerm;
                    isExecutable = modeInfo.HasFlag(UnixFileMode.OtherExecute);
                    modeCharacters[9] = modeInfo.HasFlag(UnixFileMode.StickyBit) ? (isExecutable ? StickyAndExec : StickyAndNotExec) : (isExecutable ? CanExecute : NoPerm);

                    return new string(modeCharacters);
                }

                
                public string GetUserName()
                {
                    if (usernameCache.TryGetValue(UserId, out string username))
                    {
                        return username;
                    }

                    // Get and add the user name to the cache so we don't need to
                    // have a pinvoke for each file.
                    username = NativeMethods.GetPwUid(UserId);
                    usernameCache.Add(UserId, username);

                    return username;
                }

                
                public string GetGroupName()
                {
                    if (groupnameCache.TryGetValue(GroupId, out string groupname))
                    {
                        return groupname;
                    }

                    // Get and add the group name to the cache so we don't need to
                    // have a pinvoke for each file.
                    groupname = NativeMethods.GetGrGid(GroupId);
                    groupnameCache.Add(GroupId, groupname);

                    return groupname;
                }
            }

            // This is a helper that attempts to map errno into a PowerShell ErrorCategory
            internal static ErrorCategory GetErrorCategory(int errno)
            {
                return (ErrorCategory)Unix.NativeMethods.GetErrorCategory(errno);
            }

            
            public static bool IsHardLink(FileSystemInfo fs)
            {
                if (!fs.Exists || (fs.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    return false;
                }

                int count;
                string filePath = fs.FullName;
                int ret = NativeMethods.GetLinkCount(filePath, out count);
                if (ret == 0)
                {
                    return count > 1;
                }

                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            
            private static CommonStat CopyStatStruct(NativeMethods.CommonStatStruct css)
            {
                CommonStat cs = new();
                cs.Inode = css.Inode;
                cs.Mode = css.Mode;
                cs.UserId = css.UserId;
                cs.GroupId = css.GroupId;
                cs.HardlinkCount = css.HardlinkCount;
                cs.Size = css.Size;

                // These can sometime throw if we get too large a number back (seen on Raspbian).
                // As a fallback, set the time to UnixEpoch.
                try
                {
                    cs.AccessTime = DateTime.UnixEpoch.AddSeconds(css.AccessTime).ToLocalTime();
                }
                catch
                {
                    cs.AccessTime = DateTime.UnixEpoch.ToLocalTime();
                }

                try
                {
                    cs.ModifiedTime = DateTime.UnixEpoch.AddSeconds(css.ModifiedTime).ToLocalTime();
                }
                catch
                {
                    cs.ModifiedTime = DateTime.UnixEpoch.ToLocalTime();
                }

                try
                {
                    cs.StatusChangeTime = DateTime.UnixEpoch.AddSeconds(css.StatusChangeTime).ToLocalTime();
                }
                catch
                {
                    cs.StatusChangeTime = DateTime.UnixEpoch.ToLocalTime();
                }

                cs.BlockSize = css.BlockSize;
                cs.DeviceId = css.DeviceId;
                cs.NumberOfBlocks = css.NumberOfBlocks;

                if (css.IsDirectory == 1)
                {
                    cs.ItemType = ItemType.Directory;
                }
                else if (css.IsFile == 1)
                {
                    cs.ItemType = ItemType.File;
                }
                else if (css.IsSymbolicLink == 1)
                {
                    cs.ItemType = ItemType.SymbolicLink;
                }
                else if (css.IsBlockDevice == 1)
                {
                    cs.ItemType = ItemType.BlockDevice;
                }
                else if (css.IsCharacterDevice == 1)
                {
                    cs.ItemType = ItemType.CharacterDevice;
                }
                else if (css.IsNamedPipe == 1)
                {
                    cs.ItemType = ItemType.NamedPipe;
                }
                else
                {
                    cs.ItemType = ItemType.Socket;
                }

                cs.IsSetUid = css.IsSetUid == 1;
                cs.IsSetGid = css.IsSetGid == 1;
                cs.IsSticky = css.IsSticky == 1;

                return cs;
            }

            
            public static CommonStat GetLStat(string path)
            {
                NativeMethods.CommonStatStruct css;
                if (NativeMethods.GetCommonLStat(path, out css) == 0)
                {
                    return CopyStatStruct(css);
                }

                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            
            public static CommonStat GetStat(string path)
            {
                NativeMethods.CommonStatStruct css;
                if (NativeMethods.GetCommonStat(path, out css) == 0)
                {
                    return CopyStatStruct(css);
                }

                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            
            public static int GetProcFSParentPid(int pid)
            {
                const int invalidPid = -1;

                // read /proc/<pid>/status
                // Row beginning with PPid: \d is the parent process id.
                // This used to check /proc/<pid>/stat but that file was meant
                // to be a space delimited line but it contains a value which
                // could contain spaces itself. Using the status file is a lot
                // simpler because each line contains a record with a simple
                // label.
                // https://github.com/PowerShell/PowerShell/issues/17541#issuecomment-1159911577
                var path = $"/proc/{pid}/status";
                try
                {
                    using FileStream fs = File.OpenRead(path);
                    using StreamReader sr = new(fs);
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!line.StartsWith("PPid:\t", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string[] lineSplit = line.Split('\t', 2, StringSplitOptions.RemoveEmptyEntries);
                        if (lineSplit.Length != 2)
                        {
                            continue;
                        }

                        if (int.TryParse(lineSplit[1].Trim(), out var ppid))
                        {
                            return ppid;
                        }
                    }

                    return invalidPid;
                }
                catch (Exception)
                {
                    return invalidPid;
                }
            }

            
            internal static partial class NativeMethods
            {
                private const string psLib = "libpsl-native";

                // Ansi is a misnomer, it is hardcoded to UTF-8 on Linux and macOS
                // C bools are 1 byte and so must be marshalled as I1

                [LibraryImport(psLib)]
                internal static partial int GetErrorCategory(int errno);

                [LibraryImport(psLib)]
                internal static partial int GetPPid(int pid);

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
                internal static partial int GetLinkCount(string filePath, out int linkCount);

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static partial bool IsExecutable(string filePath);

                [LibraryImport(psLib)]
                internal static partial uint GetCurrentThreadId();

                [LibraryImport(psLib)]
                [return: MarshalAs(UnmanagedType.Bool)]
                internal static partial bool KillProcess(int pid);

                [LibraryImport(psLib)]
                internal static partial int WaitPid(int pid, [MarshalAs(UnmanagedType.Bool)] bool nohang);

                // This is the struct `private_tm` from setdate.h in libpsl-native.
                // Packing is set to 4 to match the unmanaged declaration.
                // https://github.com/PowerShell/PowerShell-Native/blob/c5575ceb064e60355b9fee33eabae6c6d2708d14/src/libpsl-native/src/setdate.h#L23
                [StructLayout(LayoutKind.Sequential, Pack = 4)]
                internal unsafe struct UnixTm
                {
                    
                    internal int tm_sec;

                    
                    internal int tm_min;

                    
                    internal int tm_hour;

                    
                    internal int tm_mday;

                    
                    internal int tm_mon;

                    
                    internal int tm_year;

                    
                    internal int tm_wday;

                    
                    internal int tm_yday;

                    
                    internal int tm_isdst;
                }

                // We need a way to convert a DateTime to a unix date.
                internal static UnixTm DateTimeToUnixTm(DateTime date)
                {
                    UnixTm tm;
                    tm.tm_sec = date.Second;
                    tm.tm_min = date.Minute;
                    tm.tm_hour = date.Hour;
                    tm.tm_mday = date.Day;
                    tm.tm_mon = date.Month - 1; // needs to be 0 indexed
                    tm.tm_year = date.Year - 1900; // years since 1900
                    tm.tm_wday = 0; // this is ignored by mktime
                    tm.tm_yday = 0; // this is also ignored
                    tm.tm_isdst = date.IsDaylightSavingTime() ? 1 : 0;
                    return tm;
                }

                [LibraryImport(psLib, SetLastError = true)]
                internal static unsafe partial int SetDate(UnixTm* tm);

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8)]
                internal static partial int CreateSymLink(string filePath, string target);

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8)]
                internal static partial int CreateHardLink(string filePath, string target);

                [LibraryImport(psLib)]
                [return: MarshalAs(UnmanagedType.LPStr)]
                internal static partial string GetUserFromPid(int pid);

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8)]
                [return: MarshalAs(UnmanagedType.I1)]
                internal static partial bool IsSameFileSystemItem(string filePathOne, string filePathTwo);

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8)]
                internal static partial int GetInodeData(string path, out ulong device, out ulong inode);

                
                [StructLayout(LayoutKind.Sequential)]
                internal struct CommonStatStruct
                {
                    
                    internal long Inode;

                    
                    internal int Mode;

                    
                    internal int UserId;

                    
                    internal int GroupId;

                    
                    internal int HardlinkCount;

                    
                    internal long Size;

                    
                    internal long AccessTime;

                    
                    internal long ModifiedTime;

                    
                    internal long StatusChangeTime;

                    
                    internal long BlockSize;

                    
                    internal int DeviceId;

                    
                    internal int NumberOfBlocks;

                    
                    internal int IsDirectory;

                    
                    internal int IsFile;

                    
                    internal int IsSymbolicLink;

                    
                    internal int IsBlockDevice;

                    
                    internal int IsCharacterDevice;

                    
                    internal int IsNamedPipe;

                    
                    internal int IsSocket;

                    
                    internal int IsSetUid;

                    
                    internal int IsSetGid;

                    
                    internal int IsSticky;
                }

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
                internal static unsafe partial int GetCommonLStat(string filePath, out CommonStatStruct cs);

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
                internal static unsafe partial int GetCommonStat(string filePath, out CommonStatStruct cs);

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8)]
                internal static partial string GetPwUid(int id);

                [LibraryImport(psLib, StringMarshalling = StringMarshalling.Utf8)]
                internal static partial string GetGrGid(int id);
            }
        }
    }
}
