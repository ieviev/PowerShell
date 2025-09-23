// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Internal;
using System.Text;
using System.Threading;

namespace System.Management.Automation.Configuration
{
    
    public enum ConfigScope
    {
        
        AllUsers = 0,

        
        CurrentUser = 1
    }

    
    internal sealed class PowerShellConfig
    {
        private const string ConfigFileName = "powershell.config.json";
        private const string ExecutionPolicyDefaultShellKey = "Microsoft.PowerShell:ExecutionPolicy";
        private const string DisableImplicitWinCompatKey = "DisableImplicitWinCompat";
        private const string WindowsPowerShellCompatibilityModuleDenyListKey = "WindowsPowerShellCompatibilityModuleDenyList";
        private const string WindowsPowerShellCompatibilityNoClobberModuleListKey = "WindowsPowerShellCompatibilityNoClobberModuleList";

        // Provide a singleton
        internal static readonly PowerShellConfig Instance = new PowerShellConfig();

        // The json file containing system-wide configuration settings.
        // When passed as a pwsh command-line option, overrides the system wide configuration file.
        private string systemWideConfigFile;
        private string systemWideConfigDirectory;

        // The json file containing the per-user configuration settings.
        private readonly string perUserConfigFile;
        private readonly string perUserConfigDirectory;

        private readonly ReaderWriterLockSlim fileLock;

        private PowerShellConfig()
        {
            // Sets the system-wide configuration file.
            systemWideConfigDirectory = Utils.DefaultPowerShellAppBase;
            systemWideConfigFile = Path.Combine(systemWideConfigDirectory, ConfigFileName);

            // Sets the per-user configuration directory
            // Note: This directory may or may not exist depending upon the execution scenario.
            // Writes will attempt to create the directory if it does not already exist.
            perUserConfigDirectory = Platform.ConfigDirectory;
            perUserConfigFile = Path.Combine(perUserConfigDirectory, ConfigFileName);

            fileLock = new ReaderWriterLockSlim();
        }

        private string GetConfigFilePath(ConfigScope scope)
        {
            return (scope == ConfigScope.CurrentUser) ? perUserConfigFile : systemWideConfigFile;
        }

        
        internal void SetSystemConfigFilePath(string value)
        {
            if (!string.IsNullOrEmpty(value) && !File.Exists(value))
            {
                throw new FileNotFoundException(value);
            }

            FileInfo info = new FileInfo(value);
            systemWideConfigFile = info.FullName;
            systemWideConfigDirectory = info.Directory.FullName;
        }

        
        internal string GetModulePath(ConfigScope scope)
        {
            string modulePath = ReadValueFromFile<string>(scope, Constants.PSModulePathEnvVar);
            if (!string.IsNullOrEmpty(modulePath))
            {
                modulePath = Environment.ExpandEnvironmentVariables(modulePath);
            }

            return modulePath;
        }

        
        internal string GetExecutionPolicy(ConfigScope scope, string shellId)
        {
            string key = GetExecutionPolicySettingKey(shellId);
            string execPolicy = ReadValueFromFile<string>(scope, key);
            return string.IsNullOrEmpty(execPolicy) ? null : execPolicy;
        }

        internal void RemoveExecutionPolicy(ConfigScope scope, string shellId)
        {
       
        }

        internal void SetExecutionPolicy(ConfigScope scope, string shellId, string executionPolicy)
        {
        
        }

        private static string GetExecutionPolicySettingKey(string shellId)
        {
            return string.Equals(shellId, Utils.DefaultPowerShellShellID, StringComparison.Ordinal)
                ? ExecutionPolicyDefaultShellKey
                : string.Concat(shellId, ":", "ExecutionPolicy");
        }

        
        internal string[] GetExperimentalFeatures()
        {
            string[] features = ReadValueFromFile(ConfigScope.CurrentUser, "ExperimentalFeatures", Array.Empty<string>());

            if (features.Length == 0)
            {
                features = ReadValueFromFile(ConfigScope.AllUsers, "ExperimentalFeatures", Array.Empty<string>());
            }

            return features;
        }

        
        internal void SetExperimentalFeatures(ConfigScope scope, string featureName, bool setEnabled)
        {
            var features = new List<string>(GetExperimentalFeatures());
            bool containsFeature = features.Contains(featureName);
            if (setEnabled && !containsFeature)
            {
                features.Add(featureName);
                WriteValueToFile<string[]>(scope, "ExperimentalFeatures", features.ToArray());
            }
            else if (!setEnabled && containsFeature)
            {
                features.Remove(featureName);
                WriteValueToFile<string[]>(scope, "ExperimentalFeatures", features.ToArray());
            }
        }

        internal bool IsImplicitWinCompatEnabled()
        {
            bool settingValue = ReadValueFromFile<bool?>(ConfigScope.CurrentUser, DisableImplicitWinCompatKey)
                ?? ReadValueFromFile<bool?>(ConfigScope.AllUsers, DisableImplicitWinCompatKey)
                ?? false;

            return !settingValue;
        }

        internal string[] GetWindowsPowerShellCompatibilityModuleDenyList()
        {
            return ReadValueFromFile<string[]>(ConfigScope.CurrentUser, WindowsPowerShellCompatibilityModuleDenyListKey)
                ?? ReadValueFromFile<string[]>(ConfigScope.AllUsers, WindowsPowerShellCompatibilityModuleDenyListKey);
        }

        internal string[] GetWindowsPowerShellCompatibilityNoClobberModuleList()
        {
            return ReadValueFromFile<string[]>(ConfigScope.CurrentUser, WindowsPowerShellCompatibilityNoClobberModuleListKey)
                ?? ReadValueFromFile<string[]>(ConfigScope.AllUsers, WindowsPowerShellCompatibilityNoClobberModuleListKey);
        }

        
        internal PowerShellPolicies GetPowerShellPolicies(ConfigScope scope)
        {
            return ReadValueFromFile<PowerShellPolicies>(scope, nameof(PowerShellPolicies));
        }

#if UNIX
        
        internal string GetSysLogIdentity()
        {
            string identity = ReadValueFromFile<string>(ConfigScope.AllUsers, "LogIdentity");

            if (string.IsNullOrEmpty(identity) ||
                identity.Equals(LogDefaultValue, StringComparison.OrdinalIgnoreCase))
            {
                identity = "powershell";
            }

            return identity;
        }

        
        internal PSLevel GetLogLevel()
        {
            string levelName = ReadValueFromFile<string>(ConfigScope.AllUsers, "LogLevel");
            PSLevel level;

            if (string.IsNullOrEmpty(levelName) ||
                levelName.Equals(LogDefaultValue, StringComparison.OrdinalIgnoreCase) ||
                !Enum.TryParse<PSLevel>(levelName, true, out level))
            {
                level = PSLevel.Informational;
            }

            return level;
        }

        
        private static readonly char[] s_valueSeparators = new char[] {' ', ',', '|'};

        
        private const string LogDefaultValue = "default";

        
        internal PSChannel GetLogChannels()
        {
            string values = ReadValueFromFile<string>(ConfigScope.AllUsers, "LogChannels");

            PSChannel result = 0;
            if (!string.IsNullOrEmpty(values))
            {
                string[] names = values.Split(s_valueSeparators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string name in names)
                {
                    if (name.Equals(LogDefaultValue, StringComparison.OrdinalIgnoreCase))
                    {
                        result = 0;
                        break;
                    }

                    PSChannel value;
                    if (Enum.TryParse<PSChannel>(name, true, out value))
                    {
                        result |= value;
                    }
                }
            }

            if (result == 0)
            {
                result = System.Management.Automation.Tracing.PSSysLogProvider.DefaultChannels;
            }

            return result;
        }

        
        internal PSKeyword GetLogKeywords()
        {
            string values = ReadValueFromFile<string>(ConfigScope.AllUsers, "LogKeywords");

            PSKeyword result = 0;
            if (!string.IsNullOrEmpty(values))
            {
                string[] names = values.Split(s_valueSeparators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string name in names)
                {
                    if (name.Equals(LogDefaultValue, StringComparison.OrdinalIgnoreCase))
                    {
                        result = 0;
                        break;
                    }

                    PSKeyword value;
                    if (Enum.TryParse<PSKeyword>(name, true, out value))
                    {
                        result |= value;
                    }
                }
            }

            if (result == 0)
            {
                result = System.Management.Automation.Tracing.PSSysLogProvider.DefaultKeywords;
            }

            return result;
        }
#endif // UNIX

        
        private T ReadValueFromFile<T>(ConfigScope scope, string key, T defaultValue = default)
        {
            return defaultValue;
        }

        private static FileStream OpenFileStreamWithRetry(string fullPath, FileMode mode, FileAccess access, FileShare share)
        {
            const int MaxTries = 5;
            for (int numTries = 0; numTries < MaxTries; numTries++)
            {
                try
                {
                    return new FileStream(fullPath, mode, access, share);
                }
                catch (IOException)
                {
                    if (numTries == (MaxTries - 1))
                    {
                        throw;
                    }

                    Thread.Sleep(50);
                }
            }

            throw new IOException(nameof(OpenFileStreamWithRetry));
        }

        
        private void UpdateValueInFile<T>(ConfigScope scope, string key, T value, bool addValue)
        {
            
        }

        
        private void WriteValueToFile<T>(ConfigScope scope, string key, T value)
        {
           
        }

        
        private void RemoveValueFromFile<T>(ConfigScope scope, string key)
        {
            
        }
    }

    #region GroupPolicy Configs

    
    internal sealed class PowerShellPolicies
    {
        public ScriptExecution ScriptExecution { get; set; }

        public ScriptBlockLogging ScriptBlockLogging { get; set; }

        public ModuleLogging ModuleLogging { get; set; }

        public ProtectedEventLogging ProtectedEventLogging { get; set; }

        public Transcription Transcription { get; set; }

        public UpdatableHelp UpdatableHelp { get; set; }

        public ConsoleSessionConfiguration ConsoleSessionConfiguration { get; set; }
    }

    internal abstract class PolicyBase { }

    
    internal sealed class ScriptExecution : PolicyBase
    {
        public string ExecutionPolicy { get; set; }

        public bool? EnableScripts { get; set; }
    }

    
    internal sealed class ScriptBlockLogging : PolicyBase
    {
        public bool? EnableScriptBlockInvocationLogging { get; set; }

        public bool? EnableScriptBlockLogging { get; set; }
    }

    
    internal sealed class ModuleLogging : PolicyBase
    {
        public bool? EnableModuleLogging { get; set; }

        public string[] ModuleNames { get; set; }
    }

    
    internal sealed class Transcription : PolicyBase
    {
        public bool? EnableTranscripting { get; set; }

        public bool? EnableInvocationHeader { get; set; }

        public string OutputDirectory { get; set; }
    }

    
    internal sealed class UpdatableHelp : PolicyBase
    {
        public bool? EnableUpdateHelpDefaultSourcePath { get; set; }

        public string DefaultSourcePath { get; set; }
    }

    
    internal sealed class ConsoleSessionConfiguration : PolicyBase
    {
        public bool? EnableConsoleSessionConfiguration { get; set; }

        public string ConsoleSessionConfigurationName { get; set; }
    }

    
    internal sealed class ProtectedEventLogging : PolicyBase
    {
        public bool? EnableProtectedEventLogging { get; set; }

        public string[] EncryptionCertificate { get; set; }
    }

    #endregion
}
