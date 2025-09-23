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

namespace Microsoft.PowerShell.Telemetry
{
    
    internal enum TelemetryType
    {
        
        ApplicationType,

        
        ModuleLoad,

        
        WinCompatModuleLoad,

        
        ExperimentalEngineFeatureDeactivation,

        
        ExperimentalEngineFeatureActivation,

        
        ExperimentalFeatureUse,

        
        ExperimentalModuleFeatureDeactivation,

        
        ExperimentalModuleFeatureActivation,

        
        PowerShellCreate,

        
        RemoteSessionOpen,

        
        FeatureUse,
    }

    
    public static class ApplicationInsightsTelemetry
    {
        // The string for SubsystermRegistration
        internal const string s_subsystemRegistration = "Subsystem.Registration";

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

        
        public static bool CanSendTelemetry { get; private set; } = false;

        
        static ApplicationInsightsTelemetry()
        {
        }

        
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

        
        /// <param name="telemetryType">The type of telemetry that we'll be sending.</param>
        /// <param name="moduleInfo">The module to report. If it is not allowed, then it is set to 'anonymous'.</param>
        internal static void SendModuleTelemetryMetric(TelemetryType telemetryType, PSModuleInfo moduleInfo)
        {
        }

        
        /// <param name="telemetryType">The type of telemetry that we'll be sending.</param>
        /// <param name="moduleName">The module name to report. If it is not allowed, then it is set to 'anonymous'.</param>
        internal static void SendModuleTelemetryMetric(TelemetryType telemetryType, string moduleName)
        {
        }

        
        /// <param name="metricId">The type of telemetry that we'll be sending.</param>
        /// <param name="data">The specific details about the telemetry.</param>
        /// <param name="value">The count of instances for the telemetry payload.</param>
        internal static void SendTelemetryMetric(TelemetryType metricId, string data, double value = 1.0)
        {
        }

        
        /// <param name="featureName">The name of the feature.</param>
        /// <param name="detail">The details about the feature use.</param>
        /// <param name="value">The value to report when sending the payload.</param>
        internal static void SendUseTelemetry(string featureName, string detail, double value = 1.0)
        {
        }

        
        /// <param name="featureName">The name of the experimental feature.</param>
        /// <param name="detail">The details about the experimental feature use.</param>
        internal static void SendExperimentalUseData(string featureName, string detail)
        {
        }

        
        /// <param name="mode">The "mode" of the startup.</param>
        /// <param name="parametersUsed">The parameter bitmap used when starting.</param>
        internal static void SendPSCoreStartupTelemetry(string mode, double parametersUsed)
        {
        }
    }
}
