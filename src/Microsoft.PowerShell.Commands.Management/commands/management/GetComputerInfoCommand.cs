// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Management.Infrastructure;
using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    using Extensions;

    #region GetComputerInfoCommand cmdlet implementation
    
    [Cmdlet(VerbsCommon.Get, "ComputerInfo",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096810")]
    [Alias("gin")]
    [OutputType(typeof(ComputerInfo), typeof(PSObject))]
    public class GetComputerInfoCommand : PSCmdlet
    {
        #region Inner Types
        private sealed class OSInfoGroup
        {
            public WmiOperatingSystem os;
            public HotFix[] hotFixes;
            public WmiPageFileUsage[] pageFileUsage;
            public string halVersion;
            public TimeSpan? upTime;
            public RegWinNtCurrentVersion regCurVer;
        }

        private sealed class SystemInfoGroup
        {
            public WmiBaseBoard baseboard;
            public WmiBios bios;
            public WmiComputerSystem computer;
            public Processor[] processors;
            public NetworkAdapter[] networkAdapters;
        }

        private sealed class HyperVInfo
        {
            public bool? Present;
            public bool? VMMonitorModeExtensions;
            public bool? SecondLevelAddressTranslation;
            public bool? VirtualizationFirmwareEnabled;
            public bool? DataExecutionPreventionAvailable;
        }

        private sealed class DeviceGuardInfo
        {
            public DeviceGuardSmartStatus status;
            public DeviceGuard deviceGuard;
        }

        private sealed class MiscInfoGroup
        {
            public ulong? physicallyInstalledMemory;
            public string timeZone;
            public string logonServer;
            public FirmwareType? firmwareType;
            public PowerPlatformRole? powerPlatformRole;
            public WmiKeyboard[] keyboards;
            public HyperVInfo hyperV;
            public ServerLevel? serverLevel;
            public DeviceGuardInfo deviceGuard;
        }
        #endregion Inner Types

        #region Static Data and Constants
        private const string activity = "Get-ComputerInfo";
        private const string localMachineName = null;
        #endregion Static Data and Constants

        #region Instance Data
        private readonly string _machineName = localMachineName;  // we might need to have cmdlet work on another machine

        
        private List<string> _namedProperties = null;
        #endregion Instance Data

        #region Parameters
        
        /// <remarks>
        /// <para>
        /// Any named properties that are not recognized are ignored. If no
        /// recognized properties are provided the cmdlet returns an empty
        /// PSCustomObject.
        /// </para>
        /// <para>
        /// If a provided wild-card pattern contains only an asterisk ("*"),
        /// the cmdlet will operate as if the parameter were not given at all
        /// and will return a fully-populated ComputerInfo object.
        /// </para>
        /// </remarks>
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Property { get; set; }
        #endregion Parameters

        #region Cmdlet Overrides
        
        protected override void BeginProcessing()
        {
            // if the Property parameter was given, determine the requested
            // property names
            if (Property != null && Property.Length > 0)
            {
                try
                {
                    _namedProperties = CollectPropertyNames(Property);
                }
                catch (WildcardPatternException ex)
                {
                    WriteError(new ErrorRecord(ex, "WildcardPattern", ErrorCategory.InvalidArgument, this));
                }
            }
        }

        
        protected override void ProcessRecord()
        {
            // if the user provided property names but no matching properties
            // were found, return an empty custom object
            if (_namedProperties != null && _namedProperties.Count == 0)
            {
                WriteObject(new PSObject());
                return;
            }

            MiscInfoGroup miscInfo = null;
            var osInfo = new OSInfoGroup();
            var systemInfo = new SystemInfoGroup();
            var now = DateTime.Now;

            using (var session = CimSession.Create(_machineName))
            {
                UpdateProgress(ComputerInfoResources.LoadingOperationSystemInfo);

                osInfo.os = session.GetFirst<WmiOperatingSystem>(CIMHelper.ClassNames.OperatingSystem);
                osInfo.pageFileUsage = session.GetAll<WmiPageFileUsage>(CIMHelper.ClassNames.PageFileUsage);

                if (osInfo.os != null)
                {
                    osInfo.halVersion = GetHalVersion(session, osInfo.os.SystemDirectory);

                    if (osInfo.os.LastBootUpTime != null)
                        osInfo.upTime = now - osInfo.os.LastBootUpTime.Value;
                }

                UpdateProgress(ComputerInfoResources.LoadingHotPatchInfo);
                osInfo.hotFixes = session.GetAll<HotFix>(CIMHelper.ClassNames.HotFix);

                UpdateProgress(ComputerInfoResources.LoadingRegistryInfo);
                osInfo.regCurVer = RegistryInfo.GetWinNtCurrentVersion();

                UpdateProgress(ComputerInfoResources.LoadingBiosInfo);
                systemInfo.bios = session.GetFirst<WmiBios>(CIMHelper.ClassNames.Bios);

                UpdateProgress(ComputerInfoResources.LoadingMotherboardInfo);
                systemInfo.baseboard = session.GetFirst<WmiBaseBoard>(CIMHelper.ClassNames.BaseBoard);

                UpdateProgress(ComputerInfoResources.LoadingComputerInfo);
                systemInfo.computer = session.GetFirst<WmiComputerSystem>(CIMHelper.ClassNames.ComputerSystem);
                miscInfo = GetOtherInfo(session);

                UpdateProgress(ComputerInfoResources.LoadingProcessorInfo);
                systemInfo.processors = GetProcessors(session);

                UpdateProgress(ComputerInfoResources.LoadingNetworkAdapterInfo);
                systemInfo.networkAdapters = GetNetworkAdapters(session);

                UpdateProgress(null);   // close the progress bar
            }

            var infoOutput = CreateFullOutputObject(systemInfo, osInfo, miscInfo);

            if (_namedProperties != null)
            {
                // var output = CreateCustomOutputObject(namedProperties, systemInfo, osInfo, miscInfo);
                var output = CreateCustomOutputObject(infoOutput, _namedProperties);

                WriteObject(output);
            }
            else
            {
                WriteObject(infoOutput);
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        
        /// <param name="status">
        /// Text to be displayed in status bar
        /// </param>
        private void UpdateProgress(string status)
        {
            ProgressRecord progress = new(0, activity, status ?? ComputerResources.ProgressStatusCompleted);
            progress.RecordType = status == null ? ProgressRecordType.Completed : ProgressRecordType.Processing;

            WriteProgress(progress);
        }

        
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object
        /// representing the CIM session to query.
        /// </param>
        /// <param name="systemDirectory">
        /// Path to the system directory, which should contain the hal.dll file.
        /// </param>
        private static string GetHalVersion(CimSession session, string systemDirectory)
        {
            string halVersion = null;

            try
            {
                var halPath = CIMHelper.EscapePath(System.IO.Path.Combine(systemDirectory, "hal.dll"));
                var query = string.Create(CultureInfo.InvariantCulture, $"SELECT * FROM CIM_DataFile Where Name='{halPath}'");
                var instance = session.QueryFirstInstance(query);

                if (instance != null)
                    halVersion = instance.CimInstanceProperties["Version"].Value.ToString();
            }
            catch (Exception)
            {
                // On any error, fall through to the return
            }

            return halVersion;
        }

        
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object representing
        /// a CIM session.
        /// </param>
        /// <returns>
        /// An array of NetworkAdapter objects.
        /// </returns>
        /// <remarks>
        /// This method matches network adapters associated network adapter configurations.
        /// The returned array contains entries only for matched adapter/configuration objects.
        /// </remarks>
        private static NetworkAdapter[] GetNetworkAdapters(CimSession session)
        {
            var adaptersMsft = session.GetAll<WmiMsftNetAdapter>(CIMHelper.MicrosoftNetworkAdapterNamespace, CIMHelper.ClassNames.MicrosoftNetworkAdapter);
            var adapters = session.GetAll<WmiNetworkAdapter>(CIMHelper.ClassNames.NetworkAdapter);
            var configs = session.GetAll<WmiNetworkAdapterConfiguration>(CIMHelper.ClassNames.NetworkAdapterConfiguration);

            var list = new List<NetworkAdapter>();

            if (adapters != null && configs != null)
            {
                var configDict = new Dictionary<uint, WmiNetworkAdapterConfiguration>();

                foreach (var config in configs)
                {
                    if (config.Index != null)
                        configDict[config.Index.Value] = config;
                }

                if (configDict.Count > 0)
                {
                    foreach (var adapter in adapters)
                    {
                        // Only include adapters that have a non-null connection status
                        // and a non-null index
                        if (adapter.NetConnectionStatus != null
                            && adapter.Index != null)
                        {
                            if (configDict.ContainsKey(adapter.Index.Value))
                            {
                                var config = configDict[adapter.Index.Value];
                                var nwAdapter = new NetworkAdapter
                                {
                                    Description = adapter.Description,
                                    ConnectionID = adapter.NetConnectionID
                                };

                                var status = EnumConverter<NetConnectionStatus>.Convert(adapter.NetConnectionStatus);
                                nwAdapter.ConnectionStatus = status == null ? NetConnectionStatus.Other
                                                                            : status.Value;

                                if (nwAdapter.ConnectionStatus == NetConnectionStatus.Connected)
                                {
                                    nwAdapter.DHCPEnabled = config.DHCPEnabled;
                                    nwAdapter.DHCPServer = config.DHCPServer;
                                    nwAdapter.IPAddresses = config.IPAddress;
                                }

                                list.Add(nwAdapter);
                            }
                        }
                    }
                }
            }

            return list.ToArray();
        }

        
        /// <param name="session"></param>
        /// <returns></returns>
        private static Processor[] GetProcessors(CimSession session)
        {
            var processors = session.GetAll<WmiProcessor>(CIMHelper.ClassNames.Processor);

            if (processors != null)
            {
                var list = new List<Processor>();

                foreach (var processor in processors)
                {
                    var proc = new Processor();

                    proc.AddressWidth = processor.AddressWidth;
                    proc.Architecture = EnumConverter<CpuArchitecture>.Convert(processor.Architecture);
                    proc.Availability = EnumConverter<CpuAvailability>.Convert(processor.Availability);
                    proc.CpuStatus = EnumConverter<CpuStatus>.Convert(processor.CpuStatus);
                    proc.CurrentClockSpeed = processor.CurrentClockSpeed;
                    proc.DataWidth = processor.DataWidth;
                    proc.Description = processor.Description;
                    proc.Manufacturer = processor.Manufacturer;
                    proc.MaxClockSpeed = processor.MaxClockSpeed;
                    proc.Name = processor.Name;
                    proc.NumberOfCores = processor.NumberOfCores;
                    proc.NumberOfLogicalProcessors = processor.NumberOfLogicalProcessors;
                    proc.ProcessorID = processor.ProcessorId;
                    proc.ProcessorType = EnumConverter<ProcessorType>.Convert(processor.ProcessorType);
                    proc.Role = processor.Role;
                    proc.SocketDesignation = processor.SocketDesignation;
                    proc.Status = processor.Status;

                    list.Add(proc);
                }

                return list.ToArray();
            }

            return null;
        }

        private static bool CheckDeviceGuardLicense()
        {
            const string propertyName = "CodeIntegrity-AllowConfigurablePolicy";

            // DeviceGuard is supported on all versions of PowerShell that execute on "full" SKUs
            if (Platform.IsWindows &&
                !(Platform.IsNanoServer || Platform.IsIoT))
            {
                try
                {
                    int policy = 0;

                    if (Native.SLGetWindowsInformationDWORD(propertyName, out policy) == Native.S_OK
                        && policy == 1)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // if we fail to load the native dll or if the call fails
                    // catastrophically there's not much we can do except to
                    // consider there to be no license.
                }
            }

            return false;
        }

        
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object representing
        /// a CIM session.
        /// </param>
        /// <returns>
        /// A <see cref="DeviceGuard"/> object containing information related to
        /// the Device Guard feature
        /// </returns>
        private static DeviceGuardInfo GetDeviceGuard(CimSession session)
        {
            DeviceGuard guard = null;
            var status = DeviceGuardSmartStatus.Off;

            if (CheckDeviceGuardLicense())
            {
                var wmiGuard = session.GetFirst<WmiDeviceGuard>(CIMHelper.DeviceGuardNamespace,
                                                                CIMHelper.ClassNames.DeviceGuard);

                if (wmiGuard != null)
                {
                    var smartStatus = EnumConverter<DeviceGuardSmartStatus>.Convert((int?)wmiGuard.VirtualizationBasedSecurityStatus ?? 0);
                    if (smartStatus != null)
                    {
                        status = (DeviceGuardSmartStatus)smartStatus;
                    }

                    guard = wmiGuard.AsOutputType;
                }
            }

            return new DeviceGuardInfo
            {
                status = status,
                deviceGuard = guard
            };
        }

        
        private static bool? GetBooleanProperty(CimInstance instance, string propertyName)
        {
            if (instance != null)
            {
                try
                {
                    var property = instance.CimInstanceProperties[propertyName];

                    if (property != null && property.Value != null)
                        return (bool)property.Value;
                }
                catch (Exception)
                {
                    // just in case the cast fails
                    // fall through to the null return
                }
            }

            return null;
        }

        
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object representing
        /// a CIM session.
        /// </param>
        /// <returns>
        /// A <see cref="HyperVInfo"/> object containing information related to
        /// HyperVisor
        /// </returns>
        private static HyperVInfo GetHyperVisorInfo(CimSession session)
        {
            HyperVInfo info = new();
            bool ok = false;
            CimInstance instance = null;

            using (instance = session.QueryFirstInstance(CIMHelper.WqlQueryAll(CIMHelper.ClassNames.ComputerSystem)))
            {
                if (instance != null)
                {
                    info.Present = GetBooleanProperty(instance, "HypervisorPresent");
                    ok = true;
                }
            }

            // don't bother checking requirements if the HyperV in present
            // when the HyperV is present, the requirements values are misleading
            if (ok && info.Present != null && info.Present.Value)
                return info;

            using (instance = session.QueryFirstInstance(CIMHelper.WqlQueryAll(CIMHelper.ClassNames.OperatingSystem)))
            {
                if (instance != null)
                {
                    info.DataExecutionPreventionAvailable = GetBooleanProperty(instance, "DataExecutionPrevention_Available");
                    ok = true;
                }
            }

            using (instance = session.QueryFirstInstance(CIMHelper.WqlQueryAll(CIMHelper.ClassNames.Processor)))
            {
                if (instance != null)
                {
                    info.SecondLevelAddressTranslation = GetBooleanProperty(instance, "SecondLevelAddressTranslationExtensions");
                    info.VirtualizationFirmwareEnabled = GetBooleanProperty(instance, "VirtualizationFirmwareEnabled");
                    info.VMMonitorModeExtensions = GetBooleanProperty(instance, "VMMonitorModeExtensions");
                    ok = true;
                }
            }

            return ok ? info : null;
        }

        
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object representing
        /// a CIM session.
        /// </param>
        /// <returns>
        /// A <see cref="MiscInfoGroup"/> object containing miscellaneous
        /// system information
        /// </returns>
        private static MiscInfoGroup GetOtherInfo(CimSession session)
        {
            var rv = new MiscInfoGroup();

            // get platform role
            try
            {
                // TODO: Local machine only. Check for that?
                uint powerRole = Native.PowerDeterminePlatformRoleEx(Native.POWER_PLATFORM_ROLE_V2);
                if (powerRole >= (uint)PowerPlatformRole.MaximumEnumValue)
                    rv.powerPlatformRole = PowerPlatformRole.Unspecified;
                else
                    rv.powerPlatformRole = EnumConverter<PowerPlatformRole>.Convert((int)powerRole);
            }
            catch (Exception)
            {
                // probably failed to load the DLL with PowerDeterminePlatformRoleEx
                // either way, move on
            }

            // get secure-boot info
            // TODO: Local machine only? Check for that?
            rv.firmwareType = GetFirmwareType();

            // get amount of memory physically installed
            // TODO: Local machine only. Check for that?
            rv.physicallyInstalledMemory = GetPhysicallyInstalledSystemMemory();

            // get time zone
            // we'll use .Net's TimeZoneInfo for now. systeminfo uses Caption from Win32_TimeZone
            var tzi = TimeZoneInfo.Local;
            if (tzi != null)
                rv.timeZone = tzi.DisplayName;

            rv.logonServer = RegistryInfo.GetLogonServer();

            rv.keyboards = session.GetAll<WmiKeyboard>(CIMHelper.ClassNames.Keyboard);

            rv.hyperV = GetHyperVisorInfo(session);

            var serverLevels = RegistryInfo.GetServerLevels();
            uint value;

            if (serverLevels.TryGetValue("NanoServer", out value) && value == 1)
            {
                rv.serverLevel = ServerLevel.NanoServer;
            }
            else if (serverLevels.TryGetValue("ServerCore", out value) && value == 1)
            {
                rv.serverLevel = ServerLevel.ServerCore;

                if (serverLevels.TryGetValue("Server-Gui-Mgmt", out value) && value == 1)
                {
                    rv.serverLevel = ServerLevel.ServerCoreWithManagementTools;

                    if (serverLevels.TryGetValue("Server-Gui-Shell", out value) && value == 1)
                        rv.serverLevel = ServerLevel.FullServer;
                }
            }

            rv.deviceGuard = GetDeviceGuard(session);

            return rv;
        }

        
        /// <returns>
        /// null if unsuccessful, otherwise FirmwareType enum specifying
        /// the firmware type.
        /// </returns>
        private static FirmwareType? GetFirmwareType()
        {
            try
            {
                FirmwareType firmwareType;

                if (Native.GetFirmwareType(out firmwareType))
                    return firmwareType;
            }
            catch (Exception)
            {
                // Probably failed to load the DLL or to file the function entry point.
                // Fail silently
            }

            return null;
        }

        
        /// <returns>
        /// null if unsuccessful, otherwise the amount of physically installed memory.
        /// </returns>
        private static ulong? GetPhysicallyInstalledSystemMemory()
        {
            try
            {
                ulong memory;
                if (Native.GetPhysicallyInstalledSystemMemory(out memory))
                    return memory;
            }
            catch (Exception)
            {
                // Probably failed to load the DLL or to file the function entry point.
                // Fail silently
            }

            return null;
        }

        
        /// <param name="systemInfo">
        /// A <see cref="SystemInfoGroup"/> object containing system-related info
        /// such as BIOS, mother-board, computer system, etc.
        /// </param>
        /// <param name="osInfo">
        /// An <see cref="OSInfoGroup"/> object containing operating-system information.
        /// </param>
        /// <param name="otherInfo">
        /// A <see cref="MiscInfoGroup"/> object containing other information to be reported.
        /// </param>
        /// <returns>
        /// A new ComputerInfo object to be output to PowerShell.
        /// </returns>
        private static ComputerInfo CreateFullOutputObject(SystemInfoGroup systemInfo, OSInfoGroup osInfo, MiscInfoGroup otherInfo)
        {
            var output = new ComputerInfo();

            var regCurVer = osInfo.regCurVer;
            if (regCurVer != null)
            {
                output.WindowsBuildLabEx = regCurVer.BuildLabEx;
                output.WindowsCurrentVersion = regCurVer.CurrentVersion;
                output.WindowsEditionId = regCurVer.EditionId;
                output.WindowsInstallationType = regCurVer.InstallationType;
                output.WindowsInstallDateFromRegistry = regCurVer.InstallDate;
                output.WindowsProductId = regCurVer.ProductId;
                output.WindowsProductName = regCurVer.ProductName;
                output.WindowsRegisteredOrganization = regCurVer.RegisteredOrganization;
                output.WindowsRegisteredOwner = regCurVer.RegisteredOwner;
                output.WindowsSystemRoot = regCurVer.SystemRoot;
                output.WindowsVersion = regCurVer.ReleaseId;
                output.WindowsUBR = regCurVer.UBR;
            }

            var os = osInfo.os;
            if (os != null)
            {
                output.OsName = os.Caption;
                output.OsBootDevice = os.BootDevice;
                output.OsBuildNumber = os.BuildNumber;
                output.OsBuildType = os.BuildType;
                output.OsCodeSet = os.CodeSet;
                output.OsCountryCode = os.CountryCode;
                output.OsCSDVersion = os.CSDVersion;
                output.OsCurrentTimeZone = os.CurrentTimeZone;
                output.OsDataExecutionPreventionAvailable = os.DataExecutionPrevention_Available;
                output.OsDataExecutionPrevention32BitApplications = os.DataExecutionPrevention_32BitApplications;
                output.OsDataExecutionPreventionDrivers = os.DataExecutionPrevention_Drivers;
                output.OsDataExecutionPreventionSupportPolicy =
                    EnumConverter<DataExecutionPreventionSupportPolicy>.Convert(os.DataExecutionPrevention_SupportPolicy);
                output.OsDebug = os.Debug;

                output.OsDistributed = os.Distributed;
                output.OsEncryptionLevel = EnumConverter<OSEncryptionLevel>.Convert((int?)os.EncryptionLevel);
                output.OsForegroundApplicationBoost = EnumConverter<ForegroundApplicationBoost>.Convert(os.ForegroundApplicationBoost);
                output.OsTotalSwapSpaceSize = os.TotalSwapSpaceSize;
                output.OsTotalVisibleMemorySize = os.TotalVisibleMemorySize;
                output.OsFreePhysicalMemory = os.FreePhysicalMemory;
                output.OsFreeSpaceInPagingFiles = os.FreeSpaceInPagingFiles;
                output.OsTotalVirtualMemorySize = os.TotalVirtualMemorySize;
                output.OsFreeVirtualMemory = os.FreeVirtualMemory;
                if (os.TotalVirtualMemorySize != null && os.FreeVirtualMemory != null)
                    output.OsInUseVirtualMemory = os.TotalVirtualMemorySize - os.FreeVirtualMemory;
                output.OsInstallDate = os.InstallDate;
                output.OsLastBootUpTime = os.LastBootUpTime;
                output.OsLocalDateTime = os.LocalDateTime;
                output.OsLocaleID = os.Locale;
                output.OsManufacturer = os.Manufacturer;
                output.OsMaxNumberOfProcesses = os.MaxNumberOfProcesses;
                output.OsMaxProcessMemorySize = os.MaxProcessMemorySize;
                output.OsMuiLanguages = os.MUILanguages;
                output.OsNumberOfLicensedUsers = os.NumberOfLicensedUsers;
                output.OsNumberOfProcesses = os.NumberOfProcesses;
                output.OsNumberOfUsers = os.NumberOfUsers;
                output.OsOperatingSystemSKU = EnumConverter<OperatingSystemSKU>.Convert((int?)os.OperatingSystemSKU);
                output.OsOrganization = os.Organization;
                output.OsArchitecture = os.OSArchitecture;
                output.OsLanguage = os.LanguageName;
                output.OsProductSuites = os.ProductSuites;
                output.OsOtherTypeDescription = os.OtherTypeDescription;
                output.OsPAEEnabled = os.PAEEnabled;
                output.OsPortableOperatingSystem = os.PortableOperatingSystem;
                output.OsPrimary = os.Primary;
                output.OsProductType = EnumConverter<ProductType>.Convert((int?)os.ProductType);
                output.OsRegisteredUser = os.RegisteredUser;
                output.OsSerialNumber = os.SerialNumber;
                output.OsServicePackMajorVersion = os.ServicePackMajorVersion;
                output.OsServicePackMinorVersion = os.ServicePackMinorVersion;
                output.OsSizeStoredInPagingFiles = os.SizeStoredInPagingFiles;
                output.OsStatus = os.Status;
                output.OsSuites = os.Suites;
                output.OsSystemDevice = os.SystemDevice;
                output.OsSystemDirectory = os.SystemDirectory;
                output.OsSystemDrive = os.SystemDrive;
                output.OsType = EnumConverter<OSType>.Convert(os.OSType);
                output.OsVersion = os.Version;
                output.OsWindowsDirectory = os.WindowsDirectory;

                output.OsHardwareAbstractionLayer = osInfo.halVersion;
                output.OsLocale = os.GetLocale();
                output.OsUptime = osInfo.upTime;
                output.OsHotFixes = osInfo.hotFixes;

                var pageFileUsage = osInfo.pageFileUsage;
                if (pageFileUsage != null)
                {
                    output.OsPagingFiles = new string[pageFileUsage.Length];

                    for (int i = 0; i < pageFileUsage.Length; i++)
                        output.OsPagingFiles[i] = pageFileUsage[i].Caption;
                }
            }

            var bios = systemInfo.bios;
            if (bios != null)
            {
                output.BiosCharacteristics = bios.BiosCharacteristics;
                output.BiosBuildNumber = bios.BuildNumber;
                output.BiosBIOSVersion = bios.BIOSVersion;
                output.BiosCaption = bios.Caption;
                output.BiosCodeSet = bios.CodeSet;
                output.BiosCurrentLanguage = bios.CurrentLanguage;
                output.BiosDescription = bios.Description;
                output.BiosEmbeddedControllerMajorVersion = bios.EmbeddedControllerMajorVersion;
                output.BiosEmbeddedControllerMinorVersion = bios.EmbeddedControllerMinorVersion;
                output.BiosIdentificationCode = bios.IdentificationCode;
                output.BiosInstallableLanguages = bios.InstallableLanguages;
                output.BiosInstallDate = bios.InstallDate;
                output.BiosLanguageEdition = bios.LanguageEdition;
                output.BiosListOfLanguages = bios.ListOfLanguages;
                output.BiosManufacturer = bios.Manufacturer;
                output.BiosName = bios.Name;
                output.BiosOtherTargetOS = bios.OtherTargetOS;
                output.BiosPrimaryBIOS = bios.PrimaryBIOS;
                output.BiosReleaseDate = bios.ReleaseDate;
                output.BiosSerialNumber = bios.SerialNumber;
                output.BiosSMBIOSBIOSVersion = bios.SMBIOSBIOSVersion;
                output.BiosSMBIOSMajorVersion = bios.SMBIOSMajorVersion;
                output.BiosSMBIOSMinorVersion = bios.SMBIOSMinorVersion;
                output.BiosSMBIOSPresent = bios.SMBIOSPresent;
                output.BiosSoftwareElementState = EnumConverter<SoftwareElementState>.Convert(bios.SoftwareElementState);
                output.BiosStatus = bios.Status;
                output.BiosSystemBiosMajorVersion = bios.SystemBiosMajorVersion;
                output.BiosSystemBiosMinorVersion = bios.SystemBiosMinorVersion;
                output.BiosTargetOperatingSystem = bios.TargetOperatingSystem;
                output.BiosVersion = bios.Version;

                if (otherInfo != null)
                    output.BiosFirmwareType = otherInfo.firmwareType;
            }

            var computer = systemInfo.computer;
            if (computer != null)
            {
                output.CsAdminPasswordStatus = EnumConverter<HardwareSecurity>.Convert(computer.AdminPasswordStatus);
                output.CsAutomaticManagedPagefile = computer.AutomaticManagedPagefile;
                output.CsAutomaticResetBootOption = computer.AutomaticResetBootOption;
                output.CsAutomaticResetCapability = computer.AutomaticResetCapability;
                output.CsBootOptionOnLimit = EnumConverter<BootOptionAction>.Convert(computer.BootOptionOnLimit);
                output.CsBootOptionOnWatchDog = EnumConverter<BootOptionAction>.Convert(computer.BootOptionOnWatchDog);
                output.CsBootROMSupported = computer.BootROMSupported;
                output.CsBootStatus = computer.BootStatus;
                output.CsBootupState = computer.BootupState;
                output.CsCaption = computer.Caption;
                output.CsChassisBootupState = EnumConverter<SystemElementState>.Convert(computer.ChassisBootupState);
                output.CsChassisSKUNumber = computer.ChassisSKUNumber;
                output.CsCurrentTimeZone = computer.CurrentTimeZone;
                output.CsDaylightInEffect = computer.DaylightInEffect;
                output.CsDescription = computer.Description;
                output.CsDNSHostName = computer.DNSHostName;
                output.CsDomain = computer.Domain;
                output.CsDomainRole = EnumConverter<DomainRole>.Convert(computer.DomainRole);
                output.CsEnableDaylightSavingsTime = computer.EnableDaylightSavingsTime;
                output.CsFrontPanelResetStatus = EnumConverter<HardwareSecurity>.Convert(computer.FrontPanelResetStatus);
                output.CsHypervisorPresent = computer.HypervisorPresent;
                output.CsInfraredSupported = computer.InfraredSupported;
                output.CsInitialLoadInfo = computer.InitialLoadInfo;
                output.CsInstallDate = computer.InstallDate;
                output.CsKeyboardPasswordStatus = EnumConverter<HardwareSecurity>.Convert(computer.KeyboardPasswordStatus);
                output.CsLastLoadInfo = computer.LastLoadInfo;
                output.CsManufacturer = computer.Manufacturer;
                output.CsModel = computer.Model;
                output.CsName = computer.Name;
                output.CsNetworkAdapters = systemInfo.networkAdapters;
                output.CsNetworkServerModeEnabled = computer.NetworkServerModeEnabled;
                output.CsNumberOfLogicalProcessors = computer.NumberOfLogicalProcessors;
                output.CsNumberOfProcessors = computer.NumberOfProcessors;
                output.CsProcessors = systemInfo.processors;
                output.CsOEMStringArray = computer.OEMStringArray;
                output.CsPartOfDomain = computer.PartOfDomain;
                output.CsPauseAfterReset = computer.PauseAfterReset;
                output.CsPCSystemType = EnumConverter<PCSystemType>.Convert(computer.PCSystemType);
                output.CsPCSystemTypeEx = EnumConverter<PCSystemTypeEx>.Convert(computer.PCSystemTypeEx);
                output.CsPowerManagementCapabilities = computer.GetPowerManagementCapabilities();
                output.CsPowerManagementSupported = computer.PowerManagementSupported;
                output.CsPowerOnPasswordStatus = EnumConverter<HardwareSecurity>.Convert(computer.PowerOnPasswordStatus);
                output.CsPowerState = EnumConverter<PowerState>.Convert(computer.PowerState);
                output.CsPowerSupplyState = EnumConverter<SystemElementState>.Convert(computer.PowerSupplyState);
                output.CsPrimaryOwnerContact = computer.PrimaryOwnerContact;
                output.CsPrimaryOwnerName = computer.PrimaryOwnerName;
                output.CsResetCapability = EnumConverter<ResetCapability>.Convert(computer.ResetCapability);
                output.CsResetCount = computer.ResetCount;
                output.CsResetLimit = computer.ResetLimit;
                output.CsRoles = computer.Roles;
                output.CsStatus = computer.Status;
                output.CsSupportContactDescription = computer.SupportContactDescription;
                output.CsSystemFamily = computer.SystemFamily;
                output.CsSystemSKUNumber = computer.SystemSKUNumber;
                output.CsSystemType = computer.SystemType;
                output.CsThermalState = EnumConverter<SystemElementState>.Convert(computer.ThermalState);
                output.CsTotalPhysicalMemory = computer.TotalPhysicalMemory;
                output.CsUserName = computer.UserName;
                output.CsWakeUpType = EnumConverter<WakeUpType>.Convert(computer.WakeUpType);
                output.CsWorkgroup = computer.Workgroup;

                if (otherInfo != null)
                {
                    output.CsPhysicallyInstalledMemory = otherInfo.physicallyInstalledMemory;
                }
            }

            if (otherInfo != null)
            {
                output.TimeZone = otherInfo.timeZone;
                output.LogonServer = otherInfo.logonServer;
                output.PowerPlatformRole = otherInfo.powerPlatformRole;

                if (otherInfo.keyboards.Length > 0)
                {
                    // TODO: handle multiple keyboards?
                    // there might be several keyboards found. For the moment
                    // we display info for only one

                    string layout = otherInfo.keyboards[0].Layout;

                    output.KeyboardLayout = Conversion.GetLocaleName(layout);
                }

                if (otherInfo.hyperV != null)
                {
                    output.HyperVisorPresent = otherInfo.hyperV.Present;
                    output.HyperVRequirementDataExecutionPreventionAvailable = otherInfo.hyperV.DataExecutionPreventionAvailable;
                    output.HyperVRequirementSecondLevelAddressTranslation = otherInfo.hyperV.SecondLevelAddressTranslation;
                    output.HyperVRequirementVirtualizationFirmwareEnabled = otherInfo.hyperV.VirtualizationFirmwareEnabled;
                    output.HyperVRequirementVMMonitorModeExtensions = otherInfo.hyperV.VMMonitorModeExtensions;
                }

                output.OsServerLevel = otherInfo.serverLevel;

                var deviceGuardInfo = otherInfo.deviceGuard;
                if (deviceGuardInfo != null)
                {
                    output.DeviceGuardSmartStatus = deviceGuardInfo.status;

                    var deviceGuard = deviceGuardInfo.deviceGuard;
                    if (deviceGuard != null)
                    {
                        output.DeviceGuardRequiredSecurityProperties = deviceGuard.RequiredSecurityProperties;
                        output.DeviceGuardAvailableSecurityProperties = deviceGuard.AvailableSecurityProperties;
                        output.DeviceGuardSecurityServicesConfigured = deviceGuard.SecurityServicesConfigured;
                        output.DeviceGuardSecurityServicesRunning = deviceGuard.SecurityServicesRunning;
                        output.DeviceGuardCodeIntegrityPolicyEnforcementStatus = deviceGuard.CodeIntegrityPolicyEnforcementStatus;
                        output.DeviceGuardUserModeCodeIntegrityPolicyEnforcementStatus = deviceGuard.UserModeCodeIntegrityPolicyEnforcementStatus;
                    }
                }
            }

            return output;
        }

        
        /// <param name="info">
        /// A <see cref="ComputerInfo"/> containing all the acquired system information
        /// </param>
        /// <param name="namedProperties">
        /// A list of property names to be included in the returned object
        /// </param>
        /// <returns>
        /// A new PSObject with the properties specified in the <paramref name="namedProperties"/>
        /// parameter
        /// </returns>
        private static PSObject CreateCustomOutputObject(ComputerInfo info, List<string> namedProperties)
        {
            var rv = new PSObject();

            if (info != null && namedProperties != null && namedProperties.Count > 0)
            {
                // Walk the list of named properties, find a matching property in the
                // info object, and create a new property on the results object
                // with the associated value.
                var type = info.GetType();

                foreach (var propertyName in namedProperties)
                {
                    var propInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                    if (propInfo != null)
                    {
                        object value = propInfo.GetValue(info);
                        rv.Properties.Add(new PSNoteProperty(propertyName, value));
                    }
                }
            }

            return rv;
        }

        
        /// <returns></returns>
        private static List<string> GetComputerInfoPropertyNames()
        {
            var rv = new List<string>();
            var type = typeof(ComputerInfo);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                rv.Add(prop.Name);

            return rv;
        }

        
        /// <param name="propertyNames">
        /// List of known property names
        /// </param>
        /// <param name="pattern">
        /// The wild-card pattern used to perform globbing
        /// </param>
        /// <returns></returns>
        private static List<string> ExpandWildcardPropertyNames(List<string> propertyNames, string pattern)
        {
            var rv = new List<string>();

            var wcp = new WildcardPattern(pattern, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);

            foreach (var name in propertyNames)
                if (wcp.IsMatch(name))
                    rv.Add(name);

            return rv;
        }

        
        /// <param name="requestedProperties"></param>
        /// <returns>
        /// </returns>
        private static List<string> CollectPropertyNames(string[] requestedProperties)
        {
            // A quick scan through the requested properties to make sure
            // we want to use user-specified properties
            foreach (var name in requestedProperties)
            {
                if (WildcardPattern.ContainsWildcardCharacters(name))
                {
                    if (name == "*")
                        return null;    // we treat a wild-card pattern of "*" as if no properties were named
                }
            }

            var availableProperties = GetComputerInfoPropertyNames();
            var rv = new List<string>();

            // walk though the requested properties again, expanding and collecting property names
            foreach (var name in requestedProperties)
            {
                if (WildcardPattern.ContainsWildcardCharacters(name))
                {
                    foreach (var matchedName in ExpandWildcardPropertyNames(availableProperties, name))
                        if (!rv.Contains(matchedName))
                            rv.Add(matchedName);
                }
                else
                {
                    // find a matching property name via case-insensitive string comparison
                    Predicate<string> pred = (s) =>
                                                {
                                                    return string.Equals(s,
                                                                          name,
                                                                          StringComparison.OrdinalIgnoreCase);
                                                };
                    var propertyName = availableProperties.Find(pred);

                    // add the properly-cased name, if found, to the list
                    if (propertyName != null && !rv.Contains(propertyName))
                        rv.Add(propertyName);
                }
            }

            return rv;
        }
        #endregion Private Methods
    }
    #endregion GetComputerInfoCommand cmdlet implementation

    #region Helper classes
    internal static class Conversion
    {
        
        /// <param name="hexString">
        /// A string containing the text to be parsed.
        /// </param>
        /// <param name="value">
        /// An integer into which the parsed value is stored. If the string
        /// cannot be converted, this parameter is set to 0.
        /// </param>
        /// <returns>
        /// Returns true if the conversion was successful, false otherwise.
        /// </returns>
        /// <remarks>
        /// The hexString parameter must contain a hexadecimal value, with no
        /// base-indication prefix. For example, the string "0409" will be
        /// parsed into the base-10 integer value 1033, while the string "0x0409"
        /// will fail to parse due to the "0x" base-indication prefix.
        /// </remarks>
        internal static bool TryParseHex(string hexString, out uint value)
        {
            try
            {
                value = Convert.ToUInt32(hexString, 16);
                return true;
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
        }

        
        /// <param name="locale">
        /// A string containing WMI's notion (usually) of a locale.
        /// </param>
        /// <returns>
        /// A CultureInfo object if successful, null otherwise.
        /// </returns>
        /// <remarks>
        /// This method first tries to convert the string to a hex value
        /// and get the CultureInfo object from that value.
        /// Failing that it attempts to retrieve the CultureInfo object
        /// using the locale string as passed.
        /// </remarks>
        internal static string GetLocaleName(string locale)
        {
            CultureInfo culture = null;

            if (locale != null)
            {
                try
                {
                    // The "locale" must contain a hexadecimal value, with no
                    // base-indication prefix. For example, the string "0409" will be
                    // parsed into the base-10 integer value 1033, while the string "0x0409"
                    // will fail to parse due to the "0x" base-indication prefix.
                    if (uint.TryParse(locale, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint localeNum))
                    {
                        culture = CultureInfo.GetCultureInfo((int)localeNum);
                    }

                    // If TryParse failed we'll try using the original string as culture name
                    culture ??= CultureInfo.GetCultureInfo(locale);
                }
                catch (Exception)
                {
                    culture = null;
                }
            }

            return culture?.Name;
        }

        
        /// <param name="seconds">Number of seconds since the Unix epoch.</param>
        /// <returns>
        /// A DateTime object representing the date and time represented by the
        /// <paramref name="seconds"/> parameter.
        /// </returns>
        internal static DateTime UnixSecondsToDateTime(long seconds)
        {
#if false   // requires .NET 4.6 or higher
            return DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;
#else
            const int DaysPerYear = 365;
            const int DaysPer4Years = DaysPerYear * 4 + 1;
            const int DaysPer100Years = DaysPer4Years * 25 - 1;
            const int DaysPer400Years = DaysPer100Years * 4 + 1;
            const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear;
            const long UnixEpochTicks = TimeSpan.TicksPerDay * DaysTo1970;

            long ticks = seconds * TimeSpan.TicksPerSecond + UnixEpochTicks;

            return new DateTimeOffset(ticks, TimeSpan.Zero).DateTime;
#endif
        }
    }

    
    /// <typeparam name="T">
    /// The type of enum to be the destination of the conversion.
    /// </typeparam>
    internal static class EnumConverter<T> where T : struct, IConvertible
    {
        // The converter object
        private static readonly Func<int, T?> s_convert = MakeConverter();

        
        /// <param name="value">
        /// The integer value to be converted to the specified enum type.
        /// </param>
        /// <returns>
        /// A Nullable<typeparamref name="T"/> enum object. If the value
        /// is convertible to a valid enum value, the returned object's
        /// value will contain the converted value, otherwise the returned
        /// object will be null.
        /// </returns>
        internal static T? Convert(int? value)
        {
            try
            {
                if (value.HasValue)
                    return s_convert(value.Value);
            }
            catch (Exception)
            {
                // nothing should go wrong, but just in case
                // fall through to the return null below
            }

            return (T?)null;
        }

        
        /// <returns>
        /// A generic Func{} object to convert an int to the specified enum type.
        /// </returns>
        internal static Func<int, T?> MakeConverter()
        {
            var param = Expression.Parameter(typeof(int));
            var method = Expression.Lambda<Func<int, T?>>
                            (Expression.Convert(param, typeof(T?)), param);

            return method.Compile();
        }
    }

    internal static class RegistryInfo
    {
        public static Dictionary<string, uint> GetServerLevels()
        {
            const string keyPath = @"Software\Microsoft\Windows NT\CurrentVersion\Server\ServerLevels";

            var rv = new Dictionary<string, uint>();

            using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    foreach (var name in key.GetValueNames())
                    {
                        if (key.GetValueKind(name) == RegistryValueKind.DWord)
                        {
                            var val = key.GetValue(name);
                            rv.Add(name, Convert.ToUInt32(val));
                        }
                    }
                }
            }

            return rv;
        }

        public static string GetLogonServer()
        {
            const string valueName = "LOGONSERVER";
            const string keyPath = "Volatile Environment";

            using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key != null)
                    return (string)key.GetValue(valueName, null);
            }

            return null;
        }

        public static RegWinNtCurrentVersion GetWinNtCurrentVersion()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
            {
                if (key != null)
                {
                    object temp = key.GetValue("InstallDate");

                    return new RegWinNtCurrentVersion()
                    {
                        BuildLabEx = (string)key.GetValue("BuildLabEx"),
                        CurrentVersion = (string)key.GetValue("CurrentVersion"),
                        EditionId = (string)key.GetValue("EditionID"),
                        InstallationType = (string)key.GetValue("InstallationType"),
                        InstallDate = temp == null ? (DateTime?)null
                                                                : Conversion.UnixSecondsToDateTime((long)(int)temp),
                        ProductId = (string)key.GetValue("ProductId"),
                        ProductName = (string)key.GetValue("ProductName"),
                        RegisteredOrganization = (string)key.GetValue("RegisteredOrganization"),
                        RegisteredOwner = (string)key.GetValue("RegisteredOwner"),
                        SystemRoot = (string)key.GetValue("SystemRoot"),
                        ReleaseId = (string)key.GetValue("ReleaseId"),
                        UBR = (int?)key.GetValue("UBR")
                    };
                }
            }

            return null;
        }
    }
    #endregion Helper classes

    #region Intermediate WMI classes
    
    internal abstract class WmiClassBase
    {
        
        /// <param name="lcid">
        /// A nullable integer containing the language ID for the desired language.
        /// </param>
        /// <returns>
        /// A string containing the display name of the language identified by
        /// the language parameter. If the language parameter is null or has a
        /// value that is not a valid language ID, the method returns null.
        /// </returns>
        protected static string GetLanguageName(uint? lcid)
        {
            if (lcid != null && lcid >= 0)
            {
                try
                {
                    return CultureInfo.GetCultureInfo((int)lcid.Value).Name;
                }
                catch
                {
                }
            }

            return null;
        }
    }

#pragma warning disable 649 // fields and properties in these class are assigned dynamically
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiBaseBoard
    {
        public string Caption;
        public string[] ConfigOptions;
        public float? Depth;
        public string Description;
        public float? Height;
        public bool? HostingBoard;
        public bool? HotSwappable;
        public DateTime? InstallDate;
        public string Manufacturer;
        public string Model;
        public string Name;
        public string OtherIdentifyingInfo;
        public string PartNumber;
        public bool? PoweredOn;
        public string Product;
        public bool? Removable;
        public bool? Replaceable;
        public string RequirementsDescription;
        public bool? RequiresDaughterBoard;
        public string SerialNumber;
        public string SKU;
        public string SlotLayout;
        public bool? SpecialRequirements;
        public string Status;
        public string Tag;
        public string Version;
        public float? Weight;
        public float? Width;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiBios : WmiClassBase
    {
        public ushort[] BiosCharacteristics;
        public string[] BIOSVersion;
        public string BuildNumber;
        public string Caption;
        public string CodeSet;
        public string CurrentLanguage;
        public string Description;
        public byte? EmbeddedControllerMajorVersion;
        public byte? EmbeddedControllerMinorVersion;
        public string IdentificationCode;
        public ushort? InstallableLanguages;
        public DateTime? InstallDate;
        public string LanguageEdition;
        public string[] ListOfLanguages;
        public string Manufacturer;
        public string Name;
        public string OtherTargetOS;
        public bool? PrimaryBIOS;
        public DateTime? ReleaseDate;
        public string SerialNumber;
        public string SMBIOSBIOSVersion;
        public ushort? SMBIOSMajorVersion;
        public ushort? SMBIOSMinorVersion;
        public bool? SMBIOSPresent;
        public ushort? SoftwareElementState;
        public string Status;
        public byte? SystemBiosMajorVersion;
        public byte? SystemBiosMinorVersion;
        public ushort? TargetOperatingSystem;
        public string Version;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiComputerSystem
    {
        public ushort? AdminPasswordStatus;
        public bool? AutomaticManagedPagefile;
        public bool? AutomaticResetBootOption;
        public bool? AutomaticResetCapability;
        public ushort? BootOptionOnLimit;
        public ushort? BootOptionOnWatchDog;
        public bool? BootROMSupported;
        public string BootupState;
        public ushort[] BootStatus;
        public string Caption;
        public ushort? ChassisBootupState;
        public string ChassisSKUNumber;
        public short? CurrentTimeZone;
        public bool? DaylightInEffect;
        public string Description;
        public string DNSHostName;
        public string Domain;
        public ushort? DomainRole;
        public bool? EnableDaylightSavingsTime;
        public ushort? FrontPanelResetStatus;
        public bool? HypervisorPresent;
        public bool? InfraredSupported;
        public string InitialLoadInfo;
        public DateTime? InstallDate;
        public ushort? KeyboardPasswordStatus;
        public string LastLoadInfo;
        public string Manufacturer;
        public string Model;
        public string Name;
        public bool? NetworkServerModeEnabled;
        public uint? NumberOfLogicalProcessors;
        public uint? NumberOfProcessors;
        public string[] OEMStringArray;
        public bool? PartOfDomain;
        public long? PauseAfterReset;
        public ushort? PCSystemType;
        public ushort? PCSystemTypeEx;
        public ushort[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public ushort? PowerOnPasswordStatus;
        public ushort? PowerState;
        public ushort? PowerSupplyState;
        public string PrimaryOwnerContact;
        public string PrimaryOwnerName;
        public ushort? ResetCapability;
        public short? ResetCount;
        public short? ResetLimit;
        public string[] Roles;
        public string Status;
        public string[] SupportContactDescription;
        public string SystemFamily;
        public string SystemSKUNumber;
        public string SystemType;
        public ushort? ThermalState;
        public ulong? TotalPhysicalMemory;
        public string UserName;
        public ushort? WakeUpType;
        public string Workgroup;

        public PowerManagementCapabilities[] GetPowerManagementCapabilities()
        {
            if (PowerManagementCapabilities != null)
            {
                var list = new List<PowerManagementCapabilities>();

                foreach (var cap in PowerManagementCapabilities)
                {
                    var val = EnumConverter<PowerManagementCapabilities>.Convert(cap);

                    if (val != null)
                        list.Add(val.Value);
                }

                return list.ToArray();
            }

            return null;
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiDeviceGuard
    {
        public uint[] AvailableSecurityProperties;
        public uint? CodeIntegrityPolicyEnforcementStatus;
        public uint? UsermodeCodeIntegrityPolicyEnforcementStatus;
        public uint[] RequiredSecurityProperties;
        public uint[] SecurityServicesConfigured;
        public uint[] SecurityServicesRunning;
        public uint? VirtualizationBasedSecurityStatus;

        public DeviceGuard AsOutputType
        {
            get
            {
                var guard = new DeviceGuard();

                var status = EnumConverter<DeviceGuardSmartStatus>.Convert((int?)VirtualizationBasedSecurityStatus);
                if (status != null && status != DeviceGuardSmartStatus.Off)
                {
                    var listHardware = new List<DeviceGuardHardwareSecure>();
                    for (int i = 0; i < RequiredSecurityProperties.Length; i++)
                    {
                        var temp = EnumConverter<DeviceGuardHardwareSecure>.Convert((int?)RequiredSecurityProperties[i]);

                        if (temp != null)
                            listHardware.Add(temp.Value);
                    }

                    guard.RequiredSecurityProperties = listHardware.ToArray();

                    listHardware.Clear();
                    for (int i = 0; i < AvailableSecurityProperties.Length; i++)
                    {
                        var temp = EnumConverter<DeviceGuardHardwareSecure>.Convert((int?)AvailableSecurityProperties[i]);

                        if (temp != null)
                            listHardware.Add(temp.Value);
                    }

                    guard.AvailableSecurityProperties = listHardware.ToArray();

                    var listSoftware = new List<DeviceGuardSoftwareSecure>();
                    for (int i = 0; i < SecurityServicesConfigured.Length; i++)
                    {
                        var temp = EnumConverter<DeviceGuardSoftwareSecure>.Convert((int?)SecurityServicesConfigured[i]);

                        if (temp != null)
                            listSoftware.Add(temp.Value);
                    }

                    guard.SecurityServicesConfigured = listSoftware.ToArray();

                    listSoftware.Clear();
                    for (int i = 0; i < SecurityServicesRunning.Length; i++)
                    {
                        var temp = EnumConverter<DeviceGuardSoftwareSecure>.Convert((int?)SecurityServicesRunning[i]);

                        if (temp != null)
                            listSoftware.Add(temp.Value);
                    }

                    guard.SecurityServicesRunning = listSoftware.ToArray();
                }

                var configCiStatus = EnumConverter<DeviceGuardConfigCodeIntegrityStatus>.Convert((int?)CodeIntegrityPolicyEnforcementStatus);
                var userModeCiStatus = EnumConverter<DeviceGuardConfigCodeIntegrityStatus>.Convert((int?)UsermodeCodeIntegrityPolicyEnforcementStatus);
                guard.CodeIntegrityPolicyEnforcementStatus = configCiStatus;
                guard.UserModeCodeIntegrityPolicyEnforcementStatus = userModeCiStatus;

                return guard;
            }
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiKeyboard
    {
        public ushort? Availability;
        public string Caption;
        public uint? ConfigManagerErrorCode;
        public bool? ConfigManagerUserConfig;
        public string Description;
        public string DeviceID;
        public bool? ErrorCleared;
        public string ErrorDescription;
        public DateTime? InstallDate;
        public bool? IsLocked;
        public uint? LastErrorCode;
        public string Layout;
        public string Name;
        public ushort? NumberOfFunctionKeys;
        public ushort? Password;
        public string PNPDeviceID;
        public ushort[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public string Status;
        public ushort? StatusInfo;
        public string SystemCreationClassName;
        public string SystemName;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WMiLogicalMemory
    {
        // TODO: fill this in!!!
        public uint? TotalPhysicalMemory;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiMsftNetAdapter
    {
        public string Caption;
        public string Description;
        public DateTime? InstallDate;
        public string Name;
        public string Status;
        public ushort? Availability;
        public uint? ConfigManagerErrorCode;
        public bool? ConfigManagerUserConfig;
        public string DeviceID;
        public bool? ErrorCleared;
        public string ErrorDescription;
        public uint? LastErrorCode;
        public string PNPDeviceID;
        public ushort[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public ushort? StatusInfo;
        public string SystemCreationClassName;
        public string SystemName;
        public ulong? Speed;
        public ulong? MaxSpeed;
        public ulong? RequestedSpeed;
        public ushort? UsageRestriction;
        public ushort? PortType;
        public string OtherPortType;
        public string OtherNetworkPortType;
        public ushort? PortNumber;
        public ushort? LinkTechnology;
        public string OtherLinkTechnology;
        public string PermanentAddress;
        public string[] NetworkAddresses;
        public bool? FullDuplex;
        public bool? AutoSense;
        public ulong? SupportedMaximumTransmissionUnit;
        public ulong? ActiveMaximumTransmissionUnit;
        public string InterfaceDescription;
        public string InterfaceName;
        public ulong? NetLuid;
        public string InterfaceGuid;
        public uint? InterfaceIndex;
        public string DeviceName;
        public uint? NetLuidIndex;
        public bool? Virtual;
        public bool? Hidden;
        public bool? NotUserRemovable;
        public bool? IMFilter;
        public uint? InterfaceType;
        public bool? HardwareInterface;
        public bool? WdmInterface;
        public bool? EndPointInterface;
        public bool? iSCSIInterface;
        public uint? State;
        public uint? NdisMedium;
        public uint? NdisPhysicalMedium;
        public uint? InterfaceOperationalStatus;
        public bool? OperationalStatusDownDefaultPortNotAuthenticated;
        public bool? OperationalStatusDownMediaDisconnected;
        public bool? OperationalStatusDownInterfacePaused;
        public bool? OperationalStatusDownLowPowerState;
        public uint? InterfaceAdminStatus;
        public uint? MediaConnectState;
        public uint? MtuSize;
        public ushort? VlanID;
        public ulong? TransmitLinkSpeed;
        public ulong? ReceiveLinkSpeed;
        public bool? PromiscuousMode;
        public bool? DeviceWakeUpEnable;
        public bool? ConnectorPresent;
        public uint? MediaDuplexState;
        public string DriverDate;
        public ulong? DriverDateData;
        public string DriverVersionString;
        public string DriverName;
        public string DriverDescription;
        public ushort? MajorDriverVersion;
        public ushort? MinorDriverVersion;
        public byte? DriverMajorNdisVersion;
        public byte? DriverMinorNdisVersion;
        public string PnPDeviceID;
        public string DriverProvider;
        public string ComponentID;
        public uint[] LowerLayerInterfaceIndices;
        public uint[] HigherLayerInterfaceIndices;
        public bool? AdminLocked;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiNetworkAdapter
    {
        public string AdapterType;
        public ushort? AdapterTypeID;
        public bool? AutoSense;
        public ushort? Availability;
        public string Caption;
        public uint? ConfigManagerErrorCode;
        public bool? ConfigManagerUserConfig;
        public string Description;
        public string DeviceID;
        public bool? ErrorCleared;
        public string ErrorDescription;
        public string GUID;
        public uint? Index;
        public DateTime? InstallDate;
        public bool? Installed;
        public uint? InterfaceIndex;
        public uint? LastErrorCode;
        public string MACAddress;
        public string Manufacturer;
        public uint? MaxNumberControlled;
        public ulong? MaxSpeed;
        public string Name;
        public string NetConnectionID;
        public ushort? NetConnectionStatus;
        public bool? NetEnabled;
        public string[] NetworkAddresses;
        public string PermanentAddress;
        public bool? PhysicalAdapter;
        public string PNPDeviceID;
        public ushort[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public string ProductName;
        public string ServiceName;
        public ulong? Speed;
        public string Status;
        public ushort? StatusInfo;
        public string SystemCreationClassName;
        public string SystemName;
        public DateTime? TimeOfLastReset;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiNetworkAdapterConfiguration
    {
        public bool? ArpAlwaysSourceRoute;
        public bool? ArpUseEtherSNAP;
        public string Caption;
        public string DatabasePath;
        public bool? DeadGWDetectEnabled;
        public string[] DefaultIPGateway;
        public byte? DefaultTOS;
        public byte? DefaultTTL;
        public string Description;
        public bool? DHCPEnabled;
        public DateTime? DHCPLeaseExpires;
        public DateTime? DHCPLeaseObtained;
        public string DHCPServer;
        public string DNSDomain;
        public string[] DNSDomainSuffixSearchOrder;
        public bool? DNSEnabledForWINSResolution;
        public string DNSHostName;
        public string[] DNSServerSearchOrder;
        public bool? DomainDNSRegistrationEnabled;
        public uint? ForwardBufferMemory;
        public bool? FullDNSRegistrationEnabled;
        public ushort[] GatewayCostMetric;
        public byte? IGMPLevel;
        public uint? Index;
        public uint? InterfaceIndex;
        public string[] IPAddress;
        public uint? IPConnectionMetric;
        public bool? IPEnabled;
        public bool? IPFilterSecurityEnabled;
        public bool? IPPortSecurityEnabled;
        public string[] IPSecPermitIPProtocols;
        public string[] IPSecPermitTCPPorts;
        public string[] IPSecPermitUDPPorts;
        public string[] IPSubnet;
        public bool? IPUseZeroBroadcast;
        public string IPXAddress;
        public bool? IPXEnabled;
        public uint[] IPXFrameType;
        public uint? IPXMediaType;
        public string[] IPXNetworkNumber;
        public string IPXVirtualNetNumber;
        public uint? KeepAliveInterval;
        public uint? KeepAliveTime;
        public string MACAddress;
        public uint? MTU;
        public uint? NumForwardPackets;
        public bool? PMTUBHDetectEnabled;
        public bool? PMTUDiscoveryEnabled;
        public string ServiceName;
        public string SettingID;
        public uint? TcpipNetbiosOptions;
        public uint? TcpMaxConnectRetransmissions;
        public uint? TcpMaxDataRetransmissions;
        public uint? TcpNumConnections;
        public bool? TcpUseRFC1122UrgentPointer;
        public ushort? TcpWindowSize;
        public bool? WINSEnableLMHostsLookup;
        public string WINSHostLookupFile;
        public string WINSPrimaryServer;
        public string WINSScopeID;
        public string WINSSecondaryServer;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiOperatingSystem : WmiClassBase
    {
        #region Fields
        public string BootDevice;
        public string BuildNumber;
        public string BuildType;
        public string Caption;
        public string CodeSet;
        public string CountryCode;
        public string CSDVersion;
        public string CSName;
        public short? CurrentTimeZone;
        public bool? DataExecutionPrevention_Available;
        public bool? DataExecutionPrevention_32BitApplications;
        public bool? DataExecutionPrevention_Drivers;
        public byte? DataExecutionPrevention_SupportPolicy;
        public bool? Debug;
        public string Description;
        public bool? Distributed;
        public uint? EncryptionLevel;
        public byte? ForegroundApplicationBoost;
        public ulong? FreePhysicalMemory;
        public ulong? FreeSpaceInPagingFiles;
        public ulong? FreeVirtualMemory;
        public DateTime? InstallDate;
        public DateTime? LastBootUpTime;
        public DateTime? LocalDateTime;
        public string Locale;
        public string Manufacturer;
        public uint? MaxNumberOfProcesses;
        public ulong? MaxProcessMemorySize;
        public string[] MUILanguages;
        public string Name;
        public uint? NumberOfLicensedUsers;
        public uint? NumberOfProcesses;
        public uint? NumberOfUsers;
        public uint? OperatingSystemSKU;
        public string Organization;
        public string OSArchitecture;
        public uint? OSLanguage;
        public uint? OSProductSuite;
        public ushort? OSType;
        public string OtherTypeDescription;
        public bool? PAEEnabled;
        public bool? PortableOperatingSystem;
        public bool? Primary;
        public uint? ProductType;
        public string RegisteredUser;
        public string SerialNumber;
        public ushort? ServicePackMajorVersion;
        public ushort? ServicePackMinorVersion;
        public ulong? SizeStoredInPagingFiles;
        public string Status;
        public uint? SuiteMask;
        public string SystemDevice;
        public string SystemDirectory;
        public string SystemDrive;
        public ulong? TotalSwapSpaceSize;
        public ulong? TotalVirtualMemorySize;
        public ulong? TotalVisibleMemorySize;
        public string Version;
        public string WindowsDirectory;
        #endregion Fields

        #region Public Properties
        public string LanguageName
        {
            get { return GetLanguageName(OSLanguage); }
        }

        public OSProductSuite[] ProductSuites
        {
            get { return MakeProductSuites(OSProductSuite); }
        }

        public OSProductSuite[] Suites
        {
            get { return MakeProductSuites(SuiteMask); }
        }
        #endregion Public Properties

        #region Public Methods
        public string GetLocale()
        {
            return Conversion.GetLocaleName(Locale);
        }
        #endregion Public Methods

        #region Private Methods
        private static OSProductSuite[] MakeProductSuites(uint? suiteMask)
        {
            if (suiteMask == null)
                return null;

            var mask = suiteMask.Value;
            var list = new List<OSProductSuite>();

            foreach (OSProductSuite suite in Enum.GetValues<OSProductSuite>())
                if ((mask & (uint)suite) != 0)
                    list.Add(suite);

            return list.ToArray();
        }
        #endregion Private Methods
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiPageFileUsage
    {
        public uint? AllocatedBaseSize;
        public string Caption;
        public uint? CurrentUsage;
        public string Description;
        public DateTime? InstallDate;
        public string Name;
        public uint? PeakUsage;
        public string Status;
        public bool? TempPageFile;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiProcessor
    {
        public ushort? AddressWidth;
        public ushort? Architecture;
        public string AssetTag;
        public ushort? Availability;
        public string Caption;
        public uint? Characteristics;
        public uint? ConfigManagerErrorCode;
        public bool? ConfigManagerUserConfig;
        public ushort? CpuStatus;
        public uint? CurrentClockSpeed;
        public ushort? CurrentVoltage;
        public ushort? DataWidth;
        public string Description;
        public string DeviceID;
        public bool? ErrorCleared;
        public string ErrorDescription;
        public uint? ExtClock;
        public ushort? Family;
        public DateTime? InstallDate;
        public uint? L2CacheSize;
        public uint? L2CacheSpeed;
        public uint? L3CacheSize;
        public uint? L3CacheSpeed;
        public uint? LastErrorCode;
        public ushort? Level;
        public ushort? LoadPercentage;
        public string Manufacturer;
        public uint? MaxClockSpeed;
        public string Name;
        public uint? NumberOfCores;
        public uint? NumberOfEnabledCore;
        public uint? NumberOfLogicalProcessors;
        public string OtherFamilyDescription;
        public string PartNumber;
        public string PNPDeviceID;
        public ushort[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public string ProcessorId;
        public ushort? ProcessorType;
        public ushort? Revision;
        public string Role;
        public bool? SecondLevelAddressTranslationExtensions;
        public string SerialNumber;
        public string SocketDesignation;
        public string Status;
        public ushort? StatusInfo;
        public string Stepping;
        public string SystemName;
        public uint? ThreadCount;
        public string UniqueId;
        public ushort? UpgradeMethod;
        public string Version;
        public bool? VirtualizationFirmwareEnabled;
        public bool? VMMonitorModeExtensions;
        public uint? VoltageCaps;
    }

#pragma warning restore 649
    #endregion Intermediate WMI classes

    #region Other Intermediate classes
    internal class RegWinNtCurrentVersion
    {
        public string BuildLabEx;
        public string CurrentVersion;
        public string EditionId;
        public string InstallationType;
        public DateTime? InstallDate;
        public string ProductId;
        public string ProductName;
        public string RegisteredOrganization;
        public string RegisteredOwner;
        public string SystemRoot;
        public string ReleaseId;
        public int? UBR;
    }
    #endregion Other Intermediate classes

    #region Output components
    #region Classes comprising the output object
    
    public class DeviceGuard
    {
        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardHardwareSecure[] RequiredSecurityProperties { get; internal set; }
        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardHardwareSecure[] AvailableSecurityProperties { get; internal set; }
        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardSoftwareSecure[] SecurityServicesConfigured { get; internal set; }
        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardSoftwareSecure[] SecurityServicesRunning { get; internal set; }
        
        public DeviceGuardConfigCodeIntegrityStatus? CodeIntegrityPolicyEnforcementStatus { get; internal set; }

        
        public DeviceGuardConfigCodeIntegrityStatus? UserModeCodeIntegrityPolicyEnforcementStatus { get; internal set; }
    }

    
    public class HotFix
    {
        
        public string HotFixID
        {
            get;
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Class is instantiated directly from a CIM instance")]
            internal set;
        }

        
        public string Description
        {
            get;
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Class is instantiated directly from a CIM instance")]
            internal set;
        }

        
        public string InstalledOn
        {
            get;
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Class is instantiated directly from a CIM instance")]
            internal set;
        }

        
        public string FixComments
        {
            get;
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Class is instantiated directly from a CIM instance")]
            internal set;
        }
    }

    
    public class NetworkAdapter
    {
        
        public string Description { get; internal set; }
        
        public string ConnectionID { get; internal set; }
        
        public bool? DHCPEnabled { get; internal set; }
        
        public string DHCPServer { get; internal set; }
        
        public NetConnectionStatus ConnectionStatus { get; internal set; }
        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] IPAddresses { get; internal set; }
    }

    
    public class Processor
    {
        
        public string Name { get; internal set; }
        
        public string Manufacturer { get; internal set; }
        
        public string Description { get; internal set; }
        
        public CpuArchitecture? Architecture { get; internal set; }
        
        public ushort? AddressWidth { get; internal set; }
        
        public ushort? DataWidth { get; internal set; }
        
        public uint? MaxClockSpeed { get; internal set; }
        
        public uint? CurrentClockSpeed { get; internal set; }
        
        /// <remarks>
        /// A core is a physical processor on the integrated circuit
        /// </remarks>
        public uint? NumberOfCores { get; internal set; }
        
        /// <remarks>
        /// For processors capable of hyperthreading, this value includes only the
        /// processors which have hyperthreading enabled
        /// </remarks>
        public uint? NumberOfLogicalProcessors { get; internal set; }
        
        /// <remarks>
        /// For an x86 class CPU, the field format depends on the processor support
        /// of the CPUID instruction. If the instruction is supported, the property
        /// contains 2 (two) DWORD formatted values. The first is an offset of 08h-0Bh,
        /// which is the EAX value that a CPUID instruction returns with input EAX set
        /// to 1. The second is an offset of 0Ch-0Fh, which is the EDX value that the
        /// instruction returns. Only the first two bytes of the property are significant
        /// and contain the contents of the DX register at CPU reset—all others are set
        /// to 0 (zero), and the contents are in DWORD format
        /// </remarks>
        public string ProcessorID { get; internal set; }
        
        public string SocketDesignation { get; internal set; }
        
        public ProcessorType? ProcessorType { get; internal set; }
        
        public string Role { get; internal set; }
        
        public string Status { get; internal set; }
        
        public CpuStatus? CpuStatus { get; internal set; }
        
        public CpuAvailability? Availability { get; internal set; }
    }

    
    public class ComputerInfo
    {
        #region Registry
        
        public string WindowsBuildLabEx { get; internal set; }

        
        public string WindowsCurrentVersion { get; internal set; }

        
        public string WindowsEditionId { get; internal set; }

        
        public string WindowsInstallationType { get; internal set; }

        
        public DateTime? WindowsInstallDateFromRegistry { get; internal set; }

        
        public string WindowsProductId { get; internal set; }

        
        public string WindowsProductName { get; internal set; }

        
        public string WindowsRegisteredOrganization { get; internal set; }

        
        public string WindowsRegisteredOwner { get; internal set; }

        
        public string WindowsSystemRoot { get; internal set; }

        
        public string WindowsVersion { get; internal set; }

        
        public int? WindowsUBR { get; internal set; }
        #endregion Registry

        #region BIOS
        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ushort[] BiosCharacteristics { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] BiosBIOSVersion { get; internal set; }

        
        public string BiosBuildNumber { get; internal set; }

        
        public string BiosCaption { get; internal set; }

        
        public string BiosCodeSet { get; internal set; }

        
        public string BiosCurrentLanguage { get; internal set; }

        
        public string BiosDescription { get; internal set; }

        
        public short? BiosEmbeddedControllerMajorVersion { get; internal set; }

        
        public short? BiosEmbeddedControllerMinorVersion { get; internal set; }

        
        /// <remarks>
        /// This is acquired via the GetFirmwareType Windows API function
        /// </remarks>
        public FirmwareType? BiosFirmwareType { get; internal set; }

        
        public string BiosIdentificationCode { get; internal set; }

        
        public ushort? BiosInstallableLanguages { get; internal set; }

        
        // TODO: do we want this? On my system this is null
        public DateTime? BiosInstallDate { get; internal set; }

        
        public string BiosLanguageEdition { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] BiosListOfLanguages { get; internal set; }

        
        public string BiosManufacturer { get; internal set; }

        
        public string BiosName { get; internal set; }

        
        public string BiosOtherTargetOS { get; internal set; }

        
        public bool? BiosPrimaryBIOS { get; internal set; }

        
        public DateTime? BiosReleaseDate { get; internal set; }

        
        public string BiosSerialNumber { get; internal set; }

        
        public string BiosSMBIOSBIOSVersion { get; internal set; }

        
        public ushort? BiosSMBIOSMajorVersion { get; internal set; }

        
        public ushort? BiosSMBIOSMinorVersion { get; internal set; }

        
        public bool? BiosSMBIOSPresent { get; internal set; }

        
        public SoftwareElementState? BiosSoftwareElementState { get; internal set; }

        
        public string BiosStatus { get; internal set; }

        
        public ushort? BiosSystemBiosMajorVersion { get; internal set; }

        
        public ushort? BiosSystemBiosMinorVersion { get; internal set; }

        
        public ushort? BiosTargetOperatingSystem { get; internal set; }

        
        public string BiosVersion { get; internal set; }
        #endregion BIOS

        #region Computer System
        
        // public AdminPasswordStatus? CsAdminPasswordStatus { get; internal set; }

        public HardwareSecurity? CsAdminPasswordStatus { get; internal set; }

        
        public bool? CsAutomaticManagedPagefile { get; internal set; }

        
        public bool? CsAutomaticResetBootOption { get; internal set; }

        
        public bool? CsAutomaticResetCapability { get; internal set; }

        
        public BootOptionAction? CsBootOptionOnLimit { get; internal set; }

        
        public BootOptionAction? CsBootOptionOnWatchDog { get; internal set; }

        
        public bool? CsBootROMSupported { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ushort[] CsBootStatus { get; internal set; }

        
        public string CsBootupState { get; internal set; }

        
        public string CsCaption { get; internal set; }  // TODO: remove this? Same as CsName???

        
        // public ChassisBootupState? CsChassisBootupState { get; internal set; }

        public SystemElementState? CsChassisBootupState { get; internal set; }

        
        public string CsChassisSKUNumber { get; internal set; }

        
        public short? CsCurrentTimeZone { get; internal set; }

        
        public bool? CsDaylightInEffect { get; internal set; }

        
        public string CsDescription { get; internal set; }

        
        public string CsDNSHostName { get; internal set; }

        
        /// <remarks>
        /// If the computer is not part of a domain, then the name of the workgroup is returned
        /// </remarks>
        public string CsDomain { get; internal set; }

        
        public DomainRole? CsDomainRole { get; internal set; }

        
        public bool? CsEnableDaylightSavingsTime { get; internal set; }

        
        // public FrontPanelResetStatus? CsFrontPanelResetStatus { get; internal set; }

        public HardwareSecurity? CsFrontPanelResetStatus { get; internal set; }

        
        public bool? CsHypervisorPresent { get; internal set; }

        
        public bool? CsInfraredSupported { get; internal set; }

        
        public string CsInitialLoadInfo { get; internal set; }

        
        public DateTime? CsInstallDate { get; internal set; }

        
        // public KeyboardPasswordStatus? CsKeyboardPasswordStatus { get; internal set; }

        public HardwareSecurity? CsKeyboardPasswordStatus { get; internal set; }

        
        public string CsLastLoadInfo { get; internal set; }

        
        public string CsManufacturer { get; internal set; }

        
        public string CsModel { get; internal set; }

        
        public string CsName { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public NetworkAdapter[] CsNetworkAdapters { get; internal set; }

        
        public bool? CsNetworkServerModeEnabled { get; internal set; }

        
        public uint? CsNumberOfLogicalProcessors { get; internal set; }

        
        /// <remarks>
        /// This is the number of enabled processors for a system, which
        /// does not include the disabled processors. If a computer system
        /// has two physical processors each containing two logical processors,
        /// then the value of CsNumberOfProcessors is 2 and CsNumberOfLogicalProcessors
        /// is 4. The processors may be multicore or they may be hyperthreading processors
        /// </remarks>
        public uint? CsNumberOfProcessors { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Processor[] CsProcessors { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CsOEMStringArray { get; internal set; }

        
        public bool? CsPartOfDomain { get; internal set; }

        
        public long? CsPauseAfterReset { get; internal set; }

        
        public PCSystemType? CsPCSystemType { get; internal set; }

        
        public PCSystemTypeEx? CsPCSystemTypeEx { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PowerManagementCapabilities[] CsPowerManagementCapabilities { get; internal set; }

        
        /// <remarks>
        /// This property does not indicate that power management features are
        /// enabled currently, but it does indicate that the logical device is
        /// capable of power management
        /// </remarks>
        public bool? CsPowerManagementSupported { get; internal set; }

        
        // public PowerOnPasswordStatus? CsPowerOnPasswordStatus { get; internal set; }

        public HardwareSecurity? CsPowerOnPasswordStatus { get; internal set; }

        
        public PowerState? CsPowerState { get; internal set; }

        
        // public PowerSupplyState? CsPowerSupplyState { get; internal set; }

        public SystemElementState? CsPowerSupplyState { get; internal set; }

        
        public string CsPrimaryOwnerContact { get; internal set; }

        
        public string CsPrimaryOwnerName { get; internal set; }

        
        public ResetCapability? CsResetCapability { get; internal set; }

        
        public short? CsResetCount { get; internal set; }

        
        public short? CsResetLimit { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CsRoles { get; internal set; }

        
        public string CsStatus { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CsSupportContactDescription { get; internal set; }

        
        public string CsSystemFamily { get; internal set; }

        
        public string CsSystemSKUNumber { get; internal set; }

        
        public string CsSystemType { get; internal set; }

        
        // public ThermalState? CsThermalState { get; internal set; }

        public SystemElementState? CsThermalState { get; internal set; }

        
        /// <remarks>
        /// Be aware that, under some circumstances, this property may not
        /// return an accurate value for the physical memory. For example,
        /// it is not accurate if the BIOS is using some of the physical memory
        /// </remarks>
        public ulong? CsTotalPhysicalMemory { get; internal set; }

        
        public ulong? CsPhysicallyInstalledMemory { get; internal set; }

        
        /// <remarks>
        /// In a terminal services session, CsUserName is the name of the user
        /// that is logged on to the console—not the user logged on during the
        /// terminal service session
        /// </remarks>
        public string CsUserName { get; internal set; }

        
        public WakeUpType? CsWakeUpType { get; internal set; }

        
        public string CsWorkgroup { get; internal set; }
        #endregion Computer System

        #region Operating System
        
        public string OsName { get; internal set; }

        
        public OSType? OsType { get; internal set; }

        
        public OperatingSystemSKU? OsOperatingSystemSKU { get; internal set; }

        
        public string OsVersion { get; internal set; }

        
        public string OsCSDVersion { get; internal set; }

        
        public string OsBuildNumber { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public HotFix[] OsHotFixes { get; internal set; }

        
        public string OsBootDevice { get; internal set; }

        
        public string OsSystemDevice { get; internal set; }

        
        public string OsSystemDirectory { get; internal set; }

        
        public string OsSystemDrive { get; internal set; }

        
        public string OsWindowsDirectory { get; internal set; }

        
        /// <remarks>
        /// Values are based on international phone dialing prefixes—also
        /// referred to as IBM country/region codes
        /// </remarks>
        public string OsCountryCode { get; internal set; }

        
        public short? OsCurrentTimeZone { get; internal set; }

        
        /// <remarks>
        /// A language identifier is a standard international numeric abbreviation
        /// for a country/region. Each language has a unique language identifier (LANGID),
        /// a 16-bit value that consists of a primary language identifier and a secondary
        /// language identifier
        /// </remarks>
        public string OsLocaleID { get; internal set; }   // From Win32_OperatingSystem.Locale

        
        public string OsLocale { get; internal set; }

        
        public DateTime? OsLocalDateTime { get; internal set; }

        
        public DateTime? OsLastBootUpTime { get; internal set; }

        
        public TimeSpan? OsUptime { get; internal set; }

        
        public string OsBuildType { get; internal set; }

        
        public string OsCodeSet { get; internal set; }

        
        public bool? OsDataExecutionPreventionAvailable { get; internal set; }

        
        public bool? OsDataExecutionPrevention32BitApplications { get; internal set; }

        
        public bool? OsDataExecutionPreventionDrivers { get; internal set; }

        
        public DataExecutionPreventionSupportPolicy? OsDataExecutionPreventionSupportPolicy { get; internal set; }

        
        public bool? OsDebug { get; internal set; }

        
        public bool? OsDistributed { get; internal set; }

        
        public OSEncryptionLevel? OsEncryptionLevel { get; internal set; }

        
        public ForegroundApplicationBoost? OsForegroundApplicationBoost { get; internal set; }

        
        /// <remarks>
        /// This value does not necessarily indicate the true amount of
        /// physical memory, but what is reported to the operating system
        /// as available to it.
        /// </remarks>
        public ulong? OsTotalVisibleMemorySize { get; internal set; }

        
        public ulong? OsFreePhysicalMemory { get; internal set; }

        
        public ulong? OsTotalVirtualMemorySize { get; internal set; }

        
        public ulong? OsFreeVirtualMemory { get; internal set; }

        
        public ulong? OsInUseVirtualMemory { get; internal set; }

        
        /// <remarks>
        /// This value may be NULL (unspecified) if the swap space is not
        /// distinguished from page files. However, some operating systems
        /// distinguish these concepts. For example, in UNIX, whole processes
        /// can be swapped out when the free page list falls and remains below
        /// a specified amount
        /// </remarks>
        public ulong? OsTotalSwapSpaceSize { get; internal set; }

        
        public ulong? OsSizeStoredInPagingFiles { get; internal set; }

        
        public ulong? OsFreeSpaceInPagingFiles { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] OsPagingFiles { get; internal set; }

        
        public string OsHardwareAbstractionLayer { get; internal set; }

        
        public DateTime? OsInstallDate { get; internal set; }

        
        public string OsManufacturer { get; internal set; }

        
        public uint? OsMaxNumberOfProcesses { get; internal set; }

        
        public ulong? OsMaxProcessMemorySize { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] OsMuiLanguages { get; internal set; }

        
        public uint? OsNumberOfLicensedUsers { get; internal set; }

        
        public uint? OsNumberOfProcesses { get; internal set; }

        
        public uint? OsNumberOfUsers { get; internal set; }

        
        public string OsOrganization { get; internal set; }

        
        public string OsArchitecture { get; internal set; }

        
        public string OsLanguage { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public OSProductSuite[] OsProductSuites { get; internal set; }

        
        public string OsOtherTypeDescription { get; internal set; }

        
        public bool? OsPAEEnabled { get; internal set; }

        
        public bool? OsPortableOperatingSystem { get; internal set; }

        
        public bool? OsPrimary { get; internal set; }

        
        public ProductType? OsProductType { get; internal set; }

        
        public string OsRegisteredUser { get; internal set; }

        
        public string OsSerialNumber { get; internal set; }

        
        public ushort? OsServicePackMajorVersion { get; internal set; }

        
        public ushort? OsServicePackMinorVersion { get; internal set; }

        
        public string OsStatus { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public OSProductSuite[] OsSuites { get; internal set; }

        
        public ServerLevel? OsServerLevel { get; internal set; }
        #endregion Operating System

        #region Misc Info
        
        public string KeyboardLayout { get; internal set; }

        
        public string TimeZone { get; internal set; }

        
        public string LogonServer { get; internal set; }

        
        public PowerPlatformRole? PowerPlatformRole { get; internal set; }

        
        public bool? HyperVisorPresent { get; internal set; }

        
        public bool? HyperVRequirementDataExecutionPreventionAvailable { get; internal set; }

        
        public bool? HyperVRequirementSecondLevelAddressTranslation { get; internal set; }

        
        public bool? HyperVRequirementVirtualizationFirmwareEnabled { get; internal set; }

        
        public bool? HyperVRequirementVMMonitorModeExtensions { get; internal set; }

        
        public DeviceGuardSmartStatus? DeviceGuardSmartStatus { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardHardwareSecure[] DeviceGuardRequiredSecurityProperties { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardHardwareSecure[] DeviceGuardAvailableSecurityProperties { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardSoftwareSecure[] DeviceGuardSecurityServicesConfigured { get; internal set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardSoftwareSecure[] DeviceGuardSecurityServicesRunning { get; internal set; }

        
        public DeviceGuardConfigCodeIntegrityStatus? DeviceGuardCodeIntegrityPolicyEnforcementStatus { get; internal set; }

        
        public DeviceGuardConfigCodeIntegrityStatus? DeviceGuardUserModeCodeIntegrityPolicyEnforcementStatus { get; internal set; }
        #endregion Misc Info
    }
    #endregion Classes comprising the output object

    #region Enums used in the output objects
    
    public enum AdminPasswordStatus
    {
        
        Disabled = 0,

        
        Enabled = 1,

        
        NotImplemented = 2,

        
        Unknown = 3
    }

    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum BootOptionAction
    {
        // <summary>
        // This value is reserved
        // </summary>
        // Reserved = 0,

        
        OperatingSystem = 1,

        
        SystemUtilities = 2,

        
        DoNotReboot = 3
    }

    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum SystemElementState
    {
        
        Other = 1,

        
        Unknown = 2,

        
        Safe = 3,

        
        Warning = 4,

        
        Critical = 5,

        
        NonRecoverable = 6
    }

    
    public enum CpuArchitecture
    {
        
        x86 = 0,

        
        MIPs = 1,

        
        Alpha = 2,

        
        PowerPC = 3,

        
        ARM = 5,

        
        ia64 = 6,

        
        x64 = 9
    }

    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum CpuAvailability
    {
        
        Other = 1,

        
        Unknown = 2,

        
        RunningOrFullPower = 3,

        
        Warning = 4,

        
        InTest = 5,

        
        NotApplicable = 6,

        
        PowerOff = 7,

        
        OffLine = 8,

        
        OffDuty = 9,

        
        Degraded = 10,

        
        NotInstalled = 11,

        
        InstallError = 12,

        
        PowerSaveUnknown = 13,

        
        PowerSaveLowPowerMode = 14,

        
        PowerSaveStandby = 15,

        
        PowerCycle = 16,

        
        PowerSaveWarning = 17,

        
        Paused = 18,

        
        NotReady = 19,

        
        NotConfigured = 20,

        
        Quiesced = 21
    }

    
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "The underlying MOF definition is not a bit field.")]
    public enum CpuStatus
    {
        
        Unknown = 0,

        
        Enabled = 1,

        
        DisabledByUser = 2,

        
        DisabledByBIOS = 3,

        
        Idle = 4,

        // <summary>
        // This value is reserved
        // </summary>
        // Reserved_5 = 5,

        // <summary>
        // This value is reserved
        // </summary>
        // Reserved_6 = 6,

        
        Other = 7
    }

    
    public enum DataExecutionPreventionSupportPolicy
    {
        // Unknown     = -1,

        
        AlwaysOff = 0,

        
        AlwaysOn = 1,

        
        OptIn = 2,

        
        OptOut = 3
    }

    
    public enum DeviceGuardSmartStatus
    {
        
        Off = 0,

        
        Configured = 1,

        
        Running = 2
    }

    
    public enum DeviceGuardConfigCodeIntegrityStatus
    {
        
        Off = 0,

        
        AuditMode = 1,

        
        EnforcementMode = 2
    }

    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum DeviceGuardHardwareSecure
    {
        
        BaseVirtualizationSupport = 1,

        
        SecureBoot = 2,

        
        DMAProtection = 3,

        
        SecureMemoryOverwrite = 4,
        
        
        UEFICodeReadonly = 5,

        
        SMMSecurityMitigations = 6,

        
        ModeBasedExecutionControl = 7
    }

    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum DeviceGuardSoftwareSecure
    {
        
        CredentialGuard = 1,

        
        HypervisorEnforcedCodeIntegrity = 2
    }

    
    public enum DomainRole
    {
        
        StandaloneWorkstation = 0,

        
        MemberWorkstation = 1,

        
        StandaloneServer = 2,

        
        MemberServer = 3,

        
        BackupDomainController = 4,

        
        PrimaryDomainController = 5
    }

    
    public enum FirmwareType
    {
        
        Unknown = 0,

        
        Bios = 1,

        
        Uefi = 2,

        
        Max = 3
    }

    
    public enum ForegroundApplicationBoost
    {
        
        None = 0,

        
        Minimum = 1,

        
        Maximum = 2
    }

    
    public enum FrontPanelResetStatus
    {
        
        Disabled = 0,

        
        Enabled = 1,

        
        NotImplemented = 2,

        
        Unknown = 3
    }

    
    public enum HardwareSecurity
    {
        
        Disabled = 0,

        
        Enabled = 1,

        
        NotImplemented = 2,

        
        Unknown = 3
    }

    
    public enum NetConnectionStatus
    {
        
        Disconnected = 0,

        
        Connecting = 1,

        
        Connected = 2,

        
        Disconnecting = 3,

        
        HardwareNotPresent = 4,

        
        HardwareDisabled = 5,

        
        HardwareMalfunction = 6,

        
        MediaDisconnected = 7,

        
        Authenticating = 8,

        
        AuthenticationSucceeded = 9,

        
        AuthenticationFailed = 10,

        
        InvalidAddress = 11,

        
        CredentialsRequired = 12,

        
        Other = 13
    }

    
    public enum OSEncryptionLevel
    {
        
        Encrypt40Bits = 0,

        
        Encrypt128Bits = 1,

        
        EncryptNBits = 2
    }

    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    [FlagsAttribute]
    public enum OSProductSuite
    {
        
        SmallBusinessServer = 0x0001,

        
        Server2008Enterprise = 0x0002,

        
        BackOfficeComponents = 0x0004,

        
        CommunicationsServer = 0x0008,

        
        TerminalServices = 0x0010,

        
        SmallBusinessServerRestricted = 0x0020,

        
        WindowsEmbedded = 0x0040,

        
        DatacenterEdition = 0x0080,

        
        TerminalServicesSingleSession = 0x0100,

        
        HomeEdition = 0x0200,

        
        WebServerEdition = 0x0400,

        
        StorageServerEdition = 0x2000,

        
        ComputeClusterEdition = 0x4000
    }

    
    public enum OperatingSystemSKU
    {
        
        Undefined = 0,

        
        UltimateEdition = 1,

        
        HomeBasicEdition = 2,

        
        HomePremiumEdition = 3,

        
        EnterpriseEdition = 4,

        
        HomeBasicNEdition = 5,

        
        BusinessEdition = 6,

        
        StandardServerEdition = 7,

        
        DatacenterServerEdition = 8,

        
        SmallBusinessServerEdition = 9,

        
        EnterpriseServerEdition = 10,

        
        StarterEdition = 11,

        
        DatacenterServerCoreEdition = 12,

        
        StandardServerCoreEdition = 13,

        
        EnterpriseServerCoreEdition = 14,

        
        EnterpriseServerIA64Edition = 15,

        
        BusinessNEdition = 16,

        
        WebServerEdition = 17,

        
        ClusterServerEdition = 18,

        
        HomeServerEdition = 19,

        
        StorageExpressServerEdition = 20,

        
        StorageStandardServerEdition = 21,

        
        StorageWorkgroupServerEdition = 22,

        
        StorageEnterpriseServerEdition = 23,

        
        ServerForSmallBusinessEdition = 24,

        
        SmallBusinessServerPremiumEdition = 25,

        
        TBD = 26,

        
        WindowsEnterprise = 27,

        
        WindowsUltimate = 28,

        
        WebServerCore = 29,

        
        ServerFoundation = 33,

        
        WindowsHomeServer = 34,

        
        WindowsServerStandardNoHyperVFull = 36,

        
        WindowsServerDatacenterNoHyperVFull = 37,

        
        WindowsServerEnterpriseNoHyperVFull = 38,

        
        WindowsServerDatacenterNoHyperVCore = 39,

        
        WindowsServerStandardNoHyperVCore = 40,

        
        WindowsServerEnterpriseNoHyperVCore = 41,

        
        MicrosoftHyperVServer = 42,

        
        StorageServerExpressCore = 43,

        
        StorageServerStandardCore = 44,

        
        StorageServerWorkgroupCore = 45,

        
        StorageServerEnterpriseCore = 46,

        
        WindowsSmallBusinessServer2011Essentials = 50,

        
        SmallBusinessServerPremiumCore = 63,

        
        WindowsServerHyperCoreV = 64,

        
        WindowsThinPC = 87,

        
        WindowsEmbeddedIndustry = 89,

        
        WindowsRT = 97,

        
        WindowsHome = 101,

        
        WindowsProfessionalWithMediaCenter = 103,

        
        WindowsMobile = 104,

        
        WindowsEmbeddedHandheld = 118,

        
        WindowsIotCore = 123
    }

    
    public enum OSType
    {
        
        Unknown = 0,

        
        Other = 1,

        
        MACROS = 2,

        
        ATTUNIX = 3,

        
        DGUX = 4,

        
        DECNT = 5,

        
        DigitalUNIX = 6,

        
        OpenVMS = 7,

        
        HPUX = 8,

        
        AIX = 9,

        
        MVS = 10,

        
        OS400 = 11,

        
        OS2 = 12,

        
        JavaVM = 13,

        
        MSDOS = 14,

        
        WIN3x = 15,

        
        WIN95 = 16,

        
        WIN98 = 17,

        
        WINNT = 18,

        
        WINCE = 19,

        
        NCR3000 = 20,

        
        NetWare = 21,

        
        OSF = 22,

        
        DC_OS = 23,

        
        ReliantUNIX = 24,

        
        SCOUnixWare = 25,

        
        SCOOpenServer = 26,

        
        Sequent = 27,

        
        IRIX = 28,

        
        Solaris = 29,

        
        SunOS = 30,

        
        U6000 = 31,

        
        ASERIES = 32,

        
        TandemNSK = 33,

        
        TandemNT = 34,

        
        BS2000 = 35,

        
        LINUX = 36,

        
        Lynx = 37,

        
        XENIX = 38,

        
        VM_ESA = 39,

        
        InteractiveUNIX = 40,

        
        BSDUNIX = 41,

        
        FreeBSD = 42,

        
        NetBSD = 43,

        
        GNUHurd = 44,

        
        OS9 = 45,

        
        MACHKernel = 46,

        
        Inferno = 47,

        
        QNX = 48,

        
        EPOC = 49,

        
        IxWorks = 50,

        
        VxWorks = 51,

        
        MiNT = 52,

        
        BeOS = 53,

        
        HP_MPE = 54,

        
        NextStep = 55,

        
        PalmPilot = 56,

        
        Rhapsody = 57,

        
        Windows2000 = 58,

        
        Dedicated = 59,

        
        OS_390 = 60,

        
        VSE = 61,

        
        TPF = 62
    }

    
    public enum PCSystemType
    {
        
        Unspecified = 0,

        
        Desktop = 1,

        
        Mobile = 2,

        
        Workstation = 3,

        
        EnterpriseServer = 4,

        
        SOHOServer = 5,

        
        AppliancePC = 6,

        
        PerformanceServer = 7,

        
        Maximum = 8
    }

    
    // TODO: conflate these two enums???
    public enum PCSystemTypeEx
    {
        
        Unspecified = 0,

        
        Desktop = 1,

        
        Mobile = 2,

        
        Workstation = 3,

        
        EnterpriseServer = 4,

        
        SOHOServer = 5,

        
        AppliancePC = 6,

        
        PerformanceServer = 7,

        
        Slate = 8,

        
        Maximum = 9
    }

    
    public enum PowerManagementCapabilities
    {
        
        Unknown = 0,

        
        NotSupported = 1,

        
        Disabled = 2,

        
        Enabled = 3,

        
        PowerSavingModesEnteredAutomatically = 4,

        
        PowerStateSettable = 5,

        
        PowerCyclingSupported = 6,

        
        TimedPowerOnSupported = 7
    }

    
    public enum PowerState
    {
        
        Unknown = 0,

        
        FullPower = 1,

        
        PowerSaveLowPowerMode = 2,

        
        PowerSaveStandby = 3,

        
        PowerSaveUnknown = 4,

        
        PowerCycle = 5,

        
        PowerOff = 6,

        
        PowerSaveWarning = 7,

        
        PowerSaveHibernate = 8,

        
        PowerSaveSoftOff = 9
    }

    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum ProcessorType
    {
        
        Other = 1,

        
        Unknown = 2,

        
        CentralProcessor = 3,

        
        MathProcessor = 4,

        
        DSPProcessor = 5,

        
        VideoProcessor = 6
    }

    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum ResetCapability
    {
        
        Other = 1,

        
        Unknown = 2,

        
        Disabled = 3,

        
        Enabled = 4,

        
        NotImplemented = 5
    }

    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum WakeUpType
    {
        // <summary>
        // This value is reserved
        // </summary>
        // Reserved = 0,

        
        Other = 1,

        
        Unknown = 2,

        
        APMTimer = 3,

        
        ModemRing = 4,

        
        LANRemote = 5,

        
        PowerSwitch = 6,

        
        PCIPME = 7,

        
        ACPowerRestored = 8
    }

    
    public enum PowerPlatformRole
    {
        
        Unspecified = 0,

        
        Desktop = 1,

        
        Mobile = 2,

        
        Workstation = 3,

        
        EnterpriseServer = 4,

        
        SOHOServer = 5,

        
        AppliancePC = 6,

        
        PerformanceServer = 7,    // v1 last supported

        
        Slate = 8,    // v2 last supported

        
        MaximumEnumValue
    }

    
    public enum ProductType
    {
        
        Unknown = 0,    // this value is not specified in Win32_OperatingSystem, but may prove useful

        
        WorkStation = 1,

        
        DomainController = 2,

        
        Server = 3
    }

    
    public enum ServerLevel
    {
        
        Unknown = 0,

        
        NanoServer,

        
        ServerCore,

        
        ServerCoreWithManagementTools,

        
        FullServer
    }

    
    public enum SoftwareElementState
    {
        
        Deployable = 0,

        
        Installable = 1,

        
        Executable = 2,

        
        Running = 3
    }
    #endregion Enums used in the output objects
    #endregion Output components

    #region Native
    internal static partial class Native
    {
        private static class PInvokeDllNames
        {
            public const string GetPhysicallyInstalledSystemMemoryDllName = "api-ms-win-core-sysinfo-l1-2-1.dll";
            public const string PowerDeterminePlatformRoleExDllName = "api-ms-win-power-base-l1-1-0.dll";
            public const string GetFirmwareTypeDllName = "api-ms-win-core-kernel32-legacy-l1-1-1";
        }

        public const int LOCALE_NAME_MAX_LENGTH = 85;
        public const uint POWER_PLATFORM_ROLE_V1 = 0x1;
        public const uint POWER_PLATFORM_ROLE_V2 = 0x2;

        public const uint S_OK = 0;

        
        /// <param name="version">The version of the POWER_PLATFORM_ROLE enumeration for the platform.</param>
        /// <returns>POWER_PLATFORM_ROLE enumeration.</returns>
        [LibraryImport(PInvokeDllNames.PowerDeterminePlatformRoleExDllName, EntryPoint = "PowerDeterminePlatformRoleEx")]
        public static partial uint PowerDeterminePlatformRoleEx(uint version);

        
        /// <param name="MemoryInKilobytes"></param>
        /// <returns></returns>
        [LibraryImport(PInvokeDllNames.GetPhysicallyInstalledSystemMemoryDllName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetPhysicallyInstalledSystemMemory(out ulong MemoryInKilobytes);

        
        /// <param name="firmwareType">
        /// A reference to a <see cref="FirmwareType"/> enumeration to contain
        /// the resultant firmware type
        /// </param>
        /// <returns></returns>
        [LibraryImport(PInvokeDllNames.GetFirmwareTypeDllName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetFirmwareType(out FirmwareType firmwareType);

        
        /// <param name="licenseProperty">Name of the licensing property to get.</param>
        /// <param name="propertyValue">Out parameter for the value.</param>
        /// <returns>An hresult indicating success or failure.</returns>
        [LibraryImport("slc.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int SLGetWindowsInformationDWORD(string licenseProperty, out int propertyValue);
    }
    #endregion Native
}

#endif
