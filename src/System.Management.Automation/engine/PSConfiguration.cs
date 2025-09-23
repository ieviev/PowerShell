// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation.Internal;
using System.Text;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        // Note: JObject and JsonSerializer are thread safe.
        // Root Json objects corresponding to the configuration file for 'AllUsers' and 'CurrentUser' respectively.
        // They are used as a cache to avoid hitting the disk for every read operation.
        private readonly JObject[] configRoots;
        private readonly JObject emptyConfig;
        private readonly JsonSerializer serializer;

        
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

            emptyConfig = new JObject();
            configRoots = new JObject[2];
            serializer = JsonSerializer.Create(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 10 });

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
            string key = GetExecutionPolicySettingKey(shellId);
            RemoveValueFromFile<string>(scope, key);
        }

        internal void SetExecutionPolicy(ConfigScope scope, string shellId, string executionPolicy)
        {
            string key = GetExecutionPolicySettingKey(shellId);
            WriteValueToFile<string>(scope, key, executionPolicy);
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
            string fileName = GetConfigFilePath(scope);
            JObject configData = configRoots[(int)scope];

            if (configData == null)
            {
                if (File.Exists(fileName))
                {
                    try
                    {
                        // Open file for reading, but allow multiple readers
                        fileLock.EnterReadLock();

                        using var stream = OpenFileStreamWithRetry(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var jsonReader = new JsonTextReader(new StreamReader(stream));

                        configData = serializer.Deserialize<JObject>(jsonReader) ?? emptyConfig;
                    }
                    catch (Exception exc)
                    {
                        throw PSTraceSource.NewInvalidOperationException(exc, PSConfigurationStrings.CanNotConfigurationFile, args: fileName);
                    }
                    finally
                    {
                        fileLock.ExitReadLock();
                    }
                }
                else
                {
                    configData = emptyConfig;
                }

                // Set the configuration cache.
                JObject originalValue = Interlocked.CompareExchange(ref configRoots[(int)scope], configData, null);
                if (originalValue != null)
                {
                    configData = originalValue;
                }
            }

            if (configData != emptyConfig && configData.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken jToken))
            {
                return jToken.ToObject<T>(serializer) ?? defaultValue;
            }

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
            try
            {
                string fileName = GetConfigFilePath(scope);
                fileLock.EnterWriteLock();

                // Since multiple properties can be in a single file, replacement is required instead of overwrite if a file already exists.
                // Handling the read and write operations within a single FileStream prevents other processes from reading or writing the file while
                // the update is in progress. It also locks out readers during write operations.

                JObject jsonObject = null;
                using FileStream fs = OpenFileStreamWithRetry(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                // UTF8, BOM detection, and bufferSize are the same as the basic stream constructor.
                // The most important parameter here is the last one, which keeps underlying stream open after StreamReader is disposed
                // so that it can be reused for the subsequent write operation.
                using (StreamReader streamRdr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                using (JsonTextReader jsonReader = new JsonTextReader(streamRdr))
                {
                    // Safely determines whether there is content to read from the file
                    bool isReadSuccess = jsonReader.Read();
                    if (isReadSuccess)
                    {
                        // Read the stream into a root JObject for manipulation
                        jsonObject = serializer.Deserialize<JObject>(jsonReader);
                        JProperty propertyToModify = jsonObject.Property(key);

                        if (propertyToModify == null)
                        {
                            // The property doesn't exist, so add it
                            if (addValue)
                            {
                                jsonObject.Add(new JProperty(key, value));
                            }
                            // else the property doesn't exist so there is nothing to remove
                        }
                        else
                        {
                            // The property exists
                            if (addValue)
                            {
                                propertyToModify.Replace(new JProperty(key, value));
                            }
                            else
                            {
                                propertyToModify.Remove();
                            }
                        }
                    }
                    else
                    {
                        // The file doesn't already exist and we want to write to it or it exists with no content.
                        // A new file will be created that contains only this value.
                        // If the file doesn't exist and a we don't want to write to it, no action is needed.
                        if (addValue)
                        {
                            jsonObject = new JObject(new JProperty(key, value));
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                // Reset the stream position to the beginning so that the
                // changes to the file can be written to disk
                fs.Seek(0, SeekOrigin.Begin);

                // Update the file with new content
                using (StreamWriter streamWriter = new StreamWriter(fs))
                using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    // The entire document exists within the root JObject.
                    // I just need to write that object to produce the document.
                    jsonObject.WriteTo(jsonWriter);

                    // This trims the file if the file shrank. If the file grew,
                    // it is a no-op. The purpose is to trim extraneous characters
                    // from the file stream when the resultant JObject is smaller
                    // than the input JObject.
                    fs.SetLength(fs.Position);
                }

                // Refresh the configuration cache.
                Interlocked.Exchange(ref configRoots[(int)scope], jsonObject);
            }
            finally
            {
                fileLock.ExitWriteLock();
            }
        }

        
        private void WriteValueToFile<T>(ConfigScope scope, string key, T value)
        {
            if (scope == ConfigScope.CurrentUser && !Directory.Exists(perUserConfigDirectory))
            {
                Directory.CreateDirectory(perUserConfigDirectory);
            }

            UpdateValueInFile<T>(scope, key, value, true);
        }

        
        private void RemoveValueFromFile<T>(ConfigScope scope, string key)
        {
            string fileName = GetConfigFilePath(scope);
            // Optimization: If the file doesn't exist, there is nothing to remove
            if (File.Exists(fileName))
            {
                UpdateValueInFile<T>(scope, key, default(T), false);
            }
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
