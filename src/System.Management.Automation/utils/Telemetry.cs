// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.PowerShell.Telemetry
{
    /// <summary>
    /// The category of telemetry.
    /// </summary>
    internal enum TelemetryType
    {
        /// <summary>
        /// Telemetry of the application type (cmdlet, script, etc).
        /// </summary>
        ApplicationType,

        /// <summary>
        /// Send telemetry when we load a module, only module names in the s_knownModules list
        /// will be reported, otherwise it will be "anonymous".
        /// </summary>
        ModuleLoad,

        /// <summary>
        /// Send telemetry when we load a module using Windows compatibility feature, only module names in the s_knownModules list
        /// will be reported, otherwise it will be "anonymous".
        /// </summary>
        WinCompatModuleLoad,

        /// <summary>
        /// Send telemetry for experimental module feature deactivation.
        /// All experimental engine features will be have telemetry.
        /// </summary>
        ExperimentalEngineFeatureDeactivation,

        /// <summary>
        /// Send telemetry for experimental module feature activation.
        /// All experimental engine features will be have telemetry.
        /// </summary>
        ExperimentalEngineFeatureActivation,

        /// <summary>
        /// Send telemetry for an experimental feature when use.
        /// </summary>
        ExperimentalFeatureUse,

        /// <summary>
        /// Send telemetry for experimental module feature deactivation.
        /// Experimental module features will send telemetry based on the module it is in.
        /// If we send telemetry for the module, we will also do so for any experimental feature
        /// in that module.
        /// </summary>
        ExperimentalModuleFeatureDeactivation,

        /// <summary>
        /// Send telemetry for experimental module feature activation.
        /// Experimental module features will send telemetry based on the module it is in.
        /// If we send telemetry for the module, we will also do so for any experimental feature
        /// in that module.
        /// </summary>
        ExperimentalModuleFeatureActivation,

        /// <summary>
        /// Send telemetry for each PowerShell.Create API.
        /// </summary>
        PowerShellCreate,

        /// <summary>
        /// Remote session creation.
        /// </summary>
        RemoteSessionOpen,
    }

    /// <summary>
    /// Set up the telemetry initializer to mask the platform specific names.
    /// </summary>
    internal class NameObscurerTelemetryInitializer : ITelemetryInitializer
    {
        // Report the platform name information as "na".
        private const string _notavailable = "na";

        /// <summary>
        /// Initialize properties we are obscuring to "na".
        /// </summary>
        /// <param name="telemetry">The instance of our telemetry.</param>
        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = _notavailable;
            telemetry.Context.GetInternalContext().NodeName = _notavailable;
            telemetry.Context.Cloud.RoleInstance = _notavailable;
        }
    }

    /// <summary>
    /// Send up telemetry for startup.
    /// </summary>
    public static class ApplicationInsightsTelemetry
    {
        // If this env var is true, yes, or 1, telemetry will NOT be sent.
        private const string _telemetryOptoutEnvVar = "POWERSHELL_TELEMETRY_OPTOUT";

        // PSCoreInsight2 telemetry key
        // private const string _psCoreTelemetryKey = "ee4b2115-d347-47b0-adb6-b19c2c763808"; // Production
        private const string _psCoreTelemetryKey = "d26a5ef4-d608-452c-a6b8-a4a55935f70d"; // V7 Preview 3

        // In the event there is a problem in creating the node identifier file, use the default identifier.
        // This can happen if we are running in a system which has a read-only filesystem.
        private static readonly Guid _defaultNodeIdentifier = new Guid("2f998828-3f4a-4741-bf50-d11c6be42f50");

        // Use "anonymous" as the string to return when you can't report a name
        private const string Anonymous = "anonymous";

        // Use '0.0' as the string for an anonymous module version
        private const string AnonymousVersion = "0.0";

        // Use 'n/a' as the string when there's no tag to report
        private const string NoTag = "n/a";

        // the telemetry failure string
        private const string _telemetryFailure = "TELEMETRY_FAILURE";

        // the unique identifier for the user, when we start we
        private static string s_uniqueUserIdentifier { get; }

        // the session identifier
        private static string s_sessionId { get; }

        /// Use a hashset for quick lookups.
        /// We send telemetry only a known set of modules and tags.
        /// If it's not in the list (initialized in the static constructor), then we report anonymous
        /// or don't report anything (in the case of tags).

        /// <summary>Gets a value indicating whether telemetry can be sent.</summary>
        public static bool CanSendTelemetry { get; private set; } = false;

        /// <summary>
        /// Initializes static members of the <see cref="ApplicationInsightsTelemetry"/> class.
        /// Static constructor determines whether telemetry is to be sent, and then
        /// sets the telemetry key and set the telemetry delivery mode.
        /// Creates the session ID and initializes the HashSet of known module names.
        /// Gets or constructs the unique identifier.
        /// </summary>
        static ApplicationInsightsTelemetry()
        {
        }

        /// <summary>
        /// Determine whether the environment variable is set and how.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="defaultValue">If the environment variable is not set, use this as the default value.</param>
        /// <returns>A boolean representing the value of the environment variable.</returns>
        private static bool GetEnvironmentVariableAsBool(string name, bool defaultValue)
        {
            var str = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }

            var boolStr = str.AsSpan();

            if (boolStr.Length == 1)
            {
                if (boolStr[0] == '1')
                {
                    return true;
                }

                if (boolStr[0] == '0')
                {
                    return false;
                }
            }

            if (boolStr.Length == 3 &&
                (boolStr[0] == 'y' || boolStr[0] == 'Y') &&
                (boolStr[1] == 'e' || boolStr[1] == 'E') &&
                (boolStr[2] == 's' || boolStr[2] == 'S'))
            {
                return true;
            }

            if (boolStr.Length == 2 &&
                (boolStr[0] == 'n' || boolStr[0] == 'N') &&
                (boolStr[1] == 'o' || boolStr[1] == 'O'))
            {
                return false;
            }

            if (boolStr.Length == 4 &&
                (boolStr[0] == 't' || boolStr[0] == 'T') &&
                (boolStr[1] == 'r' || boolStr[1] == 'R') &&
                (boolStr[2] == 'u' || boolStr[2] == 'U') &&
                (boolStr[3] == 'e' || boolStr[3] == 'E'))
            {
                return true;
            }

            if (boolStr.Length == 5 &&
                (boolStr[0] == 'f' || boolStr[0] == 'F') &&
                (boolStr[1] == 'a' || boolStr[1] == 'A') &&
                (boolStr[2] == 'l' || boolStr[2] == 'L') &&
                (boolStr[3] == 's' || boolStr[3] == 'S') &&
                (boolStr[4] == 'e' || boolStr[4] == 'E'))
            {
                return false;
            }

            return defaultValue;
        }

        /// <summary>
        /// Send module load telemetry as a metric.
        /// For modules we send the module name (if allowed), and the version.
        /// Some modules (CIM) will continue use the string alternative method.
        /// </summary>
        /// <param name="telemetryType">The type of telemetry that we'll be sending.</param>
        /// <param name="moduleInfo">The module to report. If it is not allowed, then it is set to 'anonymous'.</param>
        internal static void SendModuleTelemetryMetric(TelemetryType telemetryType, PSModuleInfo moduleInfo)
        {
        }

        /// <summary>
        /// Send module load telemetry as a metric.
        /// For modules we send the module name (if allowed), and the version.
        /// Some modules (CIM) will continue use the string alternative method.
        /// </summary>
        /// <param name="telemetryType">The type of telemetry that we'll be sending.</param>
        /// <param name="moduleName">The module name to report. If it is not allowed, then it is set to 'anonymous'.</param>
        internal static void SendModuleTelemetryMetric(TelemetryType telemetryType, string moduleName)
        {
        }

        /// <summary>
        /// Send telemetry as a metric.
        /// </summary>
        /// <param name="metricId">The type of telemetry that we'll be sending.</param>
        /// <param name="data">The specific details about the telemetry.</param>
        internal static void SendTelemetryMetric(TelemetryType metricId, string data)
        {
        }

        /// <summary>
        /// Send additional information about an experimental feature as it is used.
        /// </summary>
        /// <param name="featureName">The name of the experimental feature.</param>
        /// <param name="detail">The details about the experimental feature use.</param>
        internal static void SendExperimentalUseData(string featureName, string detail)
        {
        }

        /// <summary>
        /// Create the startup payload and send it up.
        /// This is done only once during for the console host.
        /// </summary>
        /// <param name="mode">The "mode" of the startup.</param>
        /// <param name="parametersUsed">The parameter bitmap used when starting.</param>
        internal static void SendPSCoreStartupTelemetry(string mode, double parametersUsed)
        {
        }
    }
}
