// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// ----------------------------------------------------------------------
//  Contents:  Entry points for managed PowerShell plugin worker used to
//  host powershell in a WSMan service.
// ----------------------------------------------------------------------

using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Remoting.Server;
using System.Management.Automation.Remoting.WSMan;
using System.Management.Automation.Tracing;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Consolidation of constants for uniformity.
    /// </summary>
    internal static class WSManPluginConstants
    {
        internal const int ExitCodeSuccess = 0x00000000;
        internal const int ExitCodeFailure = 0x00000001;

        internal const string CtrlCSignal = "powershell/signal/crtl_c";

        // The following are the only supported streams in PowerShell remoting.
        // see WSManNativeApi.cs. These are duplicated here to save on
        // Marshalling time.
        internal const string SupportedInputStream = "stdin";
        internal const string SupportedOutputStream = "stdout";
        internal const string SupportedPromptResponseStream = "pr";
        internal const string PowerShellStartupProtocolVersionName = "protocolversion";
        internal const string PowerShellStartupProtocolVersionValue = "2.0";
        internal const string PowerShellOptionPrefix = "PS_";

        internal const int WSManPluginParamsGetRequestedLocale = 5;
        internal const int WSManPluginParamsGetRequestedDataLocale = 6;
    }

    /// <summary>
    /// Definitions of HRESULT error codes that are passed to the client.
    /// 0x8054.... means that it is a PowerShell HRESULT. The PowerShell facility
    /// is 84 (0x54).
    /// </summary>
    internal enum WSManPluginErrorCodes : int
    {
        NullPluginContext = -2141976624, // 0x805407D0
        PluginContextNotFound = -2141976623, // 0x805407D1

        NullInvalidInput = -2141975624, // 0x80540BB8
        NullInvalidStreamSets = -2141975623, // 0x80540BB9
        SessionCreationFailed = -2141975622, // 0x80540BBA
        NullShellContext = -2141975621, // 0x80540BBB
        InvalidShellContext = -2141975620, // 0x80540BBC
        InvalidCommandContext = -2141975619, // 0x80540BBD
        InvalidInputStream = -2141975618, // 0x80540BBE
        InvalidInputDatatype = -2141975617, // 0x80540BBF
        InvalidOutputStream = -2141975616, // 0x80540BC0
        InvalidSenderDetails = -2141975615, // 0x80540BC1
        ShutdownRegistrationFailed = -2141975614, // 0x80540BC2
        ReportContextFailed = -2141975613, // 0x80540BC3
        InvalidArgSet = -2141975612, // 0x80540BC4
        ProtocolVersionNotMatch = -2141975611, // 0x80540BC5
        OptionNotUnderstood = -2141975610, // 0x80540BC6
        ProtocolVersionNotFound = -2141975609, // 0x80540BC7

        ManagedException = -2141974624, // 0x80540FA0
        PluginOperationClose = -2141974623, // 0x80540FA1
        PluginConnectNoNegotiationData = -2141974622, // 0x80540FA2
        PluginConnectOperationFailed = -2141974621, // 0x80540FA3

        NoError = 0,
        OutOfMemory = -2147024882  // 0x8007000E
    }

    /// <summary>
    /// Class that holds plugin + shell context information used to handle
    /// shutdown notifications.
    ///
    /// Explicit destruction and release of the IntPtrs is not required because
    /// their lifetime is managed by WinRM.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal class WSManPluginOperationShutdownContext // TODO: Rename to OperationShutdownContext when removing the MC++ module.
    {
        #region Internal Members

        internal IntPtr pluginContext;
        internal IntPtr shellContext;
        internal IntPtr commandContext;
        internal bool isReceiveOperation;
        internal bool isShuttingDown;

        #endregion

        #region Constructors

        internal WSManPluginOperationShutdownContext(
            IntPtr plgContext,
            IntPtr shContext,
            IntPtr cmdContext,
            bool isRcvOp)
        {
            pluginContext = plgContext;
            shellContext = shContext;
            commandContext = cmdContext;
            isReceiveOperation = isRcvOp;
            isShuttingDown = false;
        }

        #endregion
    }

    /// <summary>
    /// Represents the logical grouping of all actions required to handle the
    /// lifecycle of shell sessions through the WinRM plugin.
    /// </summary>
    internal class WSManPluginInstance
    {
        #region Private Members

        private readonly Dictionary<IntPtr, WSManPluginShellSession> _activeShellSessions;
        private readonly object _syncObject;
        private static readonly Dictionary<IntPtr, WSManPluginInstance> s_activePlugins = new Dictionary<IntPtr, WSManPluginInstance>();

        /// <summary>
        /// Enables dependency injection after the static constructor is called.
        /// This may be overridden in unit tests to enable different behavior.
        /// It is static because static instances of this class use the facade. Otherwise,
        /// it would be passed in via a parameterized constructor.
        /// </summary>
        internal static readonly IWSManNativeApiFacade wsmanPinvokeStatic = new WSManNativeApiFacade();

        #endregion

        #region Constructor and Destructor

        internal WSManPluginInstance()
        {
            _activeShellSessions = new Dictionary<IntPtr, WSManPluginShellSession>();
            _syncObject = new object();
        }

        /// <summary>
        /// Static constructor to listen to unhandled exceptions
        /// from the AppDomain and log the errors
        /// Note: It is not necessary to instantiate IWSManNativeApi here because it is not used.
        /// </summary>
        static WSManPluginInstance()
        {
            // NOTE - the order is important here:
            // because handler from WindowsErrorReporting is going to terminate the process
            // we want it to fire last

#if !CORECLR
            // Register our remoting handler for crashes
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(WSManPluginInstance.UnhandledExceptionHandler);
#endif
        }

        #endregion

        /// <summary>
        /// Create a new shell in the plugin context.
        /// </summary>
        /// <param name="pluginContext"></param>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="extraInfo"></param>
        /// <param name="startupInfo"></param>
        /// <param name="inboundShellInformation"></param>
        internal void CreateShell(
            IntPtr pluginContext,
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            string extraInfo,
            WSManNativeApi.WSManShellStartupInfo_UnToMan startupInfo,
            WSManNativeApi.WSManData_UnToMan inboundShellInformation)
        {
            

            return;
        }

        /// <summary>
        /// This gets called on a thread pool thread once Shutdown wait handle is notified.
        /// </summary>
        /// <param name="context"></param>
        internal void CloseShellOperation(
            WSManPluginOperationShutdownContext context)
        {
        }

        internal void CloseCommandOperation(
            WSManPluginOperationShutdownContext context)
        {
           
        }

        /// <summary>
        /// Adds shell session to activeShellSessions store and returns the id
        /// at which the session is added.
        /// </summary>
        /// <param name="newShellSession"></param>
        private static void AddToActiveShellSessions(
            WSManPluginShellSession newShellSession)
        {
           
        }

        /// <summary>
        /// Retrieves a WSManPluginShellSession if matched.
        /// </summary>
        /// <param name="key">Shell context (WSManPluginRequest.unmanagedHandle).</param>
        /// <returns>Null WSManPluginShellSession if not matched. The object if matched.</returns>
        private WSManPluginShellSession GetFromActiveShellSessions(
            IntPtr key)
        {
            lock (_syncObject)
            {
                WSManPluginShellSession result;
                _activeShellSessions.TryGetValue(key, out result);
                return result;
            }
        }

        /// <summary>
        /// Removes a WSManPluginShellSession from tracking.
        /// </summary>
        /// <param name="keyToDelete">IntPtr of a WSManPluginRequest structure.</param>
        private static void DeleteFromActiveShellSessions(
            IntPtr keyToDelete)
        {
           
        }

        /// <summary>
        /// Triggers a shell close from an event handler.
        /// </summary>
        /// <param name="source">Shell context.</param>
        /// <param name="e"></param>
        private void HandleShellSessionClosed(
            object source,
            EventArgs e)
        {
            DeleteFromActiveShellSessions((IntPtr)source);
        }

        /// <summary>
        /// Helper function to validate incoming values.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="shellContext"></param>
        /// <param name="inputFunctionName"></param>
        /// <returns></returns>
        private static bool validateIncomingContexts(
            WSManNativeApi.WSManPluginRequest requestDetails,
            IntPtr shellContext,
            string inputFunctionName)
        {
            
            return true;
        }

        /// <summary>
        /// Create a new command in the shell context.
        /// </summary>
        /// <param name="pluginContext"></param>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="shellContext"></param>
        /// <param name="commandLine"></param>
        /// <param name="arguments"></param>
        internal void CreateCommand(
            IntPtr pluginContext,
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            IntPtr shellContext,
            string commandLine,
            WSManNativeApi.WSManCommandArgSet arguments)
        {
            
        }

        internal void StopCommand(
            WSManNativeApi.WSManPluginRequest requestDetails,
            IntPtr shellContext,
            IntPtr commandContext)
        {
            
        }

        internal void Shutdown()
        {
            
        }

        /// <summary>
        /// Connect.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="shellContext"></param>
        /// <param name="commandContext"></param>
        /// <param name="inboundConnectInformation"></param>
        internal void ConnectShellOrCommand(
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            WSManNativeApi.WSManData_UnToMan inboundConnectInformation)
        {
            
        }

        /// <summary>
        /// Send data to the shell / command specified.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="shellContext"></param>
        /// <param name="commandContext"></param>
        /// <param name="stream"></param>
        /// <param name="inboundData"></param>
        internal void SendOneItemToShellOrCommand(
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            string stream,
            WSManNativeApi.WSManData_UnToMan inboundData)
        {
            
        }

        /// <summary>
        /// Unlock the shell / command specified so that the shell / command
        /// starts sending data to the client.
        /// </summary>
        /// <param name="pluginContext"></param>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="shellContext"></param>
        /// <param name="commandContext"></param>
        /// <param name="streamSet"></param>
        internal void EnableShellOrCommandToSendDataToClient(
            IntPtr pluginContext,
            WSManNativeApi.WSManPluginRequest requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            WSManNativeApi.WSManStreamIDSet_UnToMan streamSet)
        {
            
        }

        /// <summary>
        /// Used to create PSPrincipal object from senderDetails struct.
        /// </summary>
        /// <param name="senderDetails"></param>
        /// <returns></returns>
        private static PSSenderInfo GetPSSenderInfo(
            WSManNativeApi.WSManSenderDetails senderDetails)
        {
            // senderDetails will not be null.
            return null;
        }

        private const string WSManRunAsClientTokenName = "__WINRM_RUNAS_CLIENT_TOKEN__";
        /// <summary>
        /// Helper method to retrieve the WSMan client token from the __WINRM_RUNAS_CLIENT_TOKEN__
        /// environment variable, which is set in the WSMan layer for Virtual or RunAs accounts.
        /// </summary>
        /// <returns>ClientToken IntPtr.</returns>
        private static IntPtr GetRunAsClientToken()
        {
            string clientTokenStr = System.Environment.GetEnvironmentVariable(WSManRunAsClientTokenName);
            if (clientTokenStr != null)
            {
                // Remove the token value from the environment variable
                System.Environment.SetEnvironmentVariable(WSManRunAsClientTokenName, null);

                int clientTokenInt;
                if (int.TryParse(clientTokenStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out clientTokenInt))
                {
                    return new IntPtr(clientTokenInt);
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Was private. Made protected internal for easier testing.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <returns></returns>
        protected internal bool EnsureOptionsComply(
            WSManNativeApi.WSManPluginRequest requestDetails)
        {
            WSManNativeApi.WSManOption[] options = requestDetails.operationInfo.optionSet.options;
            bool isProtocolVersionDeclared = false;

            for (int i = 0; i < options.Length; i++) // What about requestDetails.operationInfo.optionSet.optionsCount? It is a hold over from the C++ API. Safer is Length.
            {
                WSManNativeApi.WSManOption option = options[i];

                if (string.Equals(option.name, WSManPluginConstants.PowerShellStartupProtocolVersionName, StringComparison.Ordinal))
                {
                    if (!EnsureProtocolVersionComplies(requestDetails, option.value))
                    {
                        return false;
                    }

                    isProtocolVersionDeclared = true;
                }

                if (string.Compare(option.name, 0, WSManPluginConstants.PowerShellOptionPrefix, 0, WSManPluginConstants.PowerShellOptionPrefix.Length, StringComparison.Ordinal) == 0)
                {
                    if (option.mustComply)
                    {
                        ReportOperationComplete(
                            requestDetails,
                            WSManPluginErrorCodes.OptionNotUnderstood,
                            StringUtil.Format(
                                RemotingErrorIdStrings.WSManPluginOptionNotUnderstood,
                                option.name,
                                System.Management.Automation.PSVersionInfo.GitCommitId,
                                WSManPluginConstants.PowerShellStartupProtocolVersionValue));
                        return false;
                    }
                }
            }

            if (!isProtocolVersionDeclared)
            {
                ReportOperationComplete(
                    requestDetails,
                    WSManPluginErrorCodes.ProtocolVersionNotFound,
                    StringUtil.Format(
                        RemotingErrorIdStrings.WSManPluginProtocolVersionNotFound,
                        WSManPluginConstants.PowerShellStartupProtocolVersionName,
                        System.Management.Automation.PSVersionInfo.GitCommitId,
                        WSManPluginConstants.PowerShellStartupProtocolVersionValue));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies that the protocol version is in the correct syntax and supported.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="clientVersionString"></param>
        /// <returns></returns>
        protected internal bool EnsureProtocolVersionComplies(
            WSManNativeApi.WSManPluginRequest requestDetails,
            string clientVersionString)
        {
            if (string.Equals(clientVersionString, WSManPluginConstants.PowerShellStartupProtocolVersionValue, StringComparison.Ordinal))
            {
                return true;
            }

            // Check if major versions are equal and server's minor version is smaller..
            // if so client's version is supported by the server. The understanding is
            // that minor version changes do not break the protocol.
            System.Version clientVersion = Utils.StringToVersion(clientVersionString);
            System.Version serverVersion = Utils.StringToVersion(WSManPluginConstants.PowerShellStartupProtocolVersionValue);

            if ((clientVersion != null) && (serverVersion != null) &&
                (clientVersion.Major == serverVersion.Major) &&
                (clientVersion.Minor >= serverVersion.Minor))
            {
                return true;
            }

            ReportOperationComplete(
                requestDetails,
                WSManPluginErrorCodes.ProtocolVersionNotMatch,
                StringUtil.Format(
                    RemotingErrorIdStrings.WSManPluginProtocolVersionNotMatch,
                    WSManPluginConstants.PowerShellStartupProtocolVersionValue,
                    System.Management.Automation.PSVersionInfo.GitCommitId,
                    clientVersionString));
            return false;
        }

        /// <summary>
        /// Static func to take care of unmanaged to managed transitions.
        /// </summary>
        /// <param name="pluginContext"></param>
        /// <param name="requestDetails"></param>
        /// <param name="flags"></param>
        /// <param name="extraInfo"></param>
        /// <param name="startupInfo"></param>
        /// <param name="inboundShellInformation"></param>
        internal static void PerformWSManPluginShell(
            IntPtr pluginContext, // PVOID
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            string extraInfo,
            IntPtr startupInfo, // WSMAN_SHELL_STARTUP_INFO*
            IntPtr inboundShellInformation) // WSMAN_DATA*
        {
            
        }

        internal static void PerformWSManPluginCommand(
            IntPtr pluginContext,
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            IntPtr shellContext, // PVOID
            [MarshalAs(UnmanagedType.LPWStr)] string commandLine,
            IntPtr arguments) // WSMAN_COMMAND_ARG_SET*
        {
            
        }

        internal static void PerformWSManPluginConnect(
            IntPtr pluginContext,
            IntPtr requestDetails,
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            IntPtr inboundConnectInformation)
        {
            
        }

        internal static void PerformWSManPluginSend(
            IntPtr pluginContext,
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            IntPtr shellContext, // PVOID
            IntPtr commandContext, // PVOID
            string stream,
            IntPtr inboundData) // WSMAN_DATA*
        {
            
        }

        internal static void PerformWSManPluginReceive(
            IntPtr pluginContext, // PVOID
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            IntPtr shellContext,
            IntPtr commandContext,
            IntPtr streamSet) // WSMAN_STREAM_ID_SET*
        {
            
        }

        internal static void PerformWSManPluginSignal(
            IntPtr pluginContext, // PVOID
            IntPtr requestDetails, // WSMAN_PLUGIN_REQUEST*
            int flags,
            IntPtr shellContext, // PVOID
            IntPtr commandContext, // PVOID
            string code)
        {
            
        }

        /// <summary>
        /// Close the operation specified by the supplied context.
        /// </summary>
        /// <param name="context"></param>
        internal static void PerformCloseOperation(
            WSManPluginOperationShutdownContext context)
        {
            
        }

        /// <summary>
        /// Performs deinitialization during shutdown.
        /// </summary>
        /// <param name="pluginContext"></param>
        internal static void PerformShutdown(
            IntPtr pluginContext)
        {
            
        }

        private static WSManPluginInstance GetFromActivePlugins(IntPtr pluginContext)
        {
            return null;
        }

        private static void AddToActivePlugins(IntPtr pluginContext, WSManPluginInstance plugin)
        {
            lock (s_activePlugins)
            {
                if (!s_activePlugins.ContainsKey(pluginContext))
                {
                    s_activePlugins.Add(pluginContext, plugin);
                    return;
                }
            }
        }

        #region Utilities

        /// <summary>
        /// Report operation complete to WSMan and supply a reason (if any)
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="errorCode"></param>
        internal static void ReportWSManOperationComplete(
            WSManNativeApi.WSManPluginRequest requestDetails,
            WSManPluginErrorCodes errorCode)
        {
            Dbg.Assert(requestDetails != null, "requestDetails cannot be null in operation complete.");

            

            ReportOperationComplete(requestDetails.unmanagedHandle, errorCode);
        }

        /// <summary>
        /// Extract message from exception (if any) and report operation complete with it to WSMan.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="reasonForClose"></param>
        internal static void ReportWSManOperationComplete(
            WSManNativeApi.WSManPluginRequest requestDetails,
            Exception reasonForClose)
        {
            Dbg.Assert(requestDetails != null, "requestDetails cannot be null in operation complete.");
        }

        /// <summary>
        /// Sets thread properties like UI Culture, Culture etc..This is needed as code is transitioning from
        /// unmanaged heap to managed heap...and thread properties are not set correctly during this
        /// transition.
        /// Currently WSMan provider supplies only UI Culture related data..so only UI Culture is set.
        /// </summary>
        /// <param name="requestDetails"></param>
        internal static void SetThreadProperties(
            WSManNativeApi.WSManPluginRequest requestDetails)
        {
            // requestDetails cannot not be null.
            Dbg.Assert(requestDetails != null, "requestDetails cannot be null");

            // IntPtr nativeLocaleData = IntPtr.Zero;
            WSManNativeApi.WSManDataStruct outputStruct = new WSManNativeApi.WSManDataStruct();
            int hResult = wsmanPinvokeStatic.WSManPluginGetOperationParameters(
                requestDetails.unmanagedHandle,
                WSManPluginConstants.WSManPluginParamsGetRequestedLocale,
                outputStruct);
            // ref nativeLocaleData);
            bool retrievingLocaleSucceeded = (hResult == 0);
            WSManNativeApi.WSManData_UnToMan localeData = WSManNativeApi.WSManData_UnToMan.UnMarshal(outputStruct); // nativeLocaleData

            // IntPtr nativeDataLocaleData = IntPtr.Zero;
            hResult = wsmanPinvokeStatic.WSManPluginGetOperationParameters(
                requestDetails.unmanagedHandle,
                WSManPluginConstants.WSManPluginParamsGetRequestedDataLocale,
                outputStruct);
            // ref nativeDataLocaleData);
            bool retrievingDataLocaleSucceeded = (hResult == (int)WSManPluginErrorCodes.NoError);
            WSManNativeApi.WSManData_UnToMan dataLocaleData = WSManNativeApi.WSManData_UnToMan.UnMarshal(outputStruct); // nativeDataLocaleData

            // Set the UI Culture
            try
            {
                if (retrievingLocaleSucceeded && (localeData.Type == (uint)WSManNativeApi.WSManDataType.WSMAN_DATA_TYPE_TEXT))
                {
                    CultureInfo uiCultureToUse = new CultureInfo(localeData.Text);
                    Thread.CurrentThread.CurrentUICulture = uiCultureToUse;
                }
            }
            // ignore if there is any exception constructing the culture..
            catch (ArgumentException)
            {
            }

            // Set the Culture
            try
            {
                if (retrievingDataLocaleSucceeded && (dataLocaleData.Type == (uint)WSManNativeApi.WSManDataType.WSMAN_DATA_TYPE_TEXT))
                {
                    CultureInfo cultureToUse = new CultureInfo(dataLocaleData.Text);
                    Thread.CurrentThread.CurrentCulture = cultureToUse;
                }
            }
            // ignore if there is any exception constructing the culture..
            catch (ArgumentException)
            {
            }
        }

#if !CORECLR
        /// <summary>
        /// Handle any unhandled exceptions that get raised in the AppDomain
        /// This will log the exception into Crimson logs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal static void UnhandledExceptionHandler(
            object sender,
            UnhandledExceptionEventArgs args)
        {
            // args can never be null.
            Exception exception = (Exception)args.ExceptionObject;

            

            
        }
#endif

        /// <summary>
        /// Alternate wrapper for WSManPluginOperationComplete. TODO: Needed? I could easily use the handle instead and get rid of this? It is only for easier refactoring...
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage">Pre-formatted localized string.</param>
        /// <returns></returns>
        internal static void ReportOperationComplete(
            WSManNativeApi.WSManPluginRequest requestDetails,
            WSManPluginErrorCodes errorCode,
            string errorMessage)
        {
            if (requestDetails != null)
            {
                ReportOperationComplete(requestDetails.unmanagedHandle, errorCode, errorMessage);
            }
            // else cannot report if requestDetails is null.
        }

        /// <summary>
        /// Wrapper for WSManPluginOperationComplete. It performs validation prior to making the call.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="errorCode"></param>
        internal static void ReportOperationComplete(
            WSManNativeApi.WSManPluginRequest requestDetails,
            WSManPluginErrorCodes errorCode)
        {
            if (requestDetails != null &&
                requestDetails.unmanagedHandle != IntPtr.Zero)
            {
                wsmanPinvokeStatic.WSManPluginOperationComplete(
                    requestDetails.unmanagedHandle,
                    0,
                    (int)errorCode,
                    null);
            }
            // else cannot report if requestDetails is null.
        }

        /// <summary>
        /// Wrapper for WSManPluginOperationComplete. It performs validation prior to making the call.
        /// </summary>
        /// <param name="requestDetails"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        internal static void ReportOperationComplete(
            IntPtr requestDetails,
            WSManPluginErrorCodes errorCode,
            string errorMessage = "")
        {
            if (requestDetails == IntPtr.Zero)
            {
                // cannot report if requestDetails is null.
                return;
            }

            wsmanPinvokeStatic.WSManPluginOperationComplete(
                requestDetails,
                0,
                (int)errorCode,
                errorMessage);

            return;
        }

        #endregion
    }
}
