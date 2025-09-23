// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Tracing;
using System.Runtime.CompilerServices;
using Microsoft.PowerShell.Telemetry;

namespace System.Management.Automation
{
    
    public class ExperimentalFeature
    {
        #region Const Members

        internal const string EngineSource = "PSEngine";
        internal const string PSFeedbackProvider = "PSFeedbackProvider";
        internal const string PSNativeWindowsTildeExpansion = nameof(PSNativeWindowsTildeExpansion);
        internal const string PSRedirectToVariable = "PSRedirectToVariable";
        internal const string PSSerializeJSONLongEnumAsNumber = nameof(PSSerializeJSONLongEnumAsNumber);

        #endregion

        #region Instance Members

        
        public string Name { get; }

        
        public string Description { get; }

        
        public string Source { get; }

        
        public bool Enabled { get; private set; }

        
        /// <param name="name">The name of the experimental feature.</param>
        /// <param name="description">A description of the experimental feature.</param>
        /// <param name="source">The source where the experimental feature is defined.</param>
        /// <param name="isEnabled">Indicate whether the experimental feature is enabled.</param>
        internal ExperimentalFeature(string name, string description, string source, bool isEnabled)
        {
            Name = name;
            Description = description;
            Source = source;
            Enabled = isEnabled;
        }

        
        /// <param name="name">The name of the experimental feature.</param>
        /// <param name="description">A description of the experimental feature.</param>
        private ExperimentalFeature(string name, string description)
            : this(name, description, source: EngineSource, isEnabled: false)
        {
        }

        #endregion

        #region Static Members

        
        internal static readonly ReadOnlyCollection<ExperimentalFeature> EngineExperimentalFeatures;

        
        internal static readonly ReadOnlyDictionary<string, ExperimentalFeature> EngineExperimentalFeatureMap;

        
        internal static readonly ReadOnlyBag<string> EnabledExperimentalFeatureNames;

        
        static ExperimentalFeature()
        {
            // Initialize the readonly collection 'EngineExperimentalFeatures'.
            var engineFeatures = new ExperimentalFeature[] {
                
                new ExperimentalFeature(
                    name: "PSSubsystemPluginModel",
                    description: "A plugin model for registering and un-registering PowerShell subsystems"),
                new ExperimentalFeature(
                    name: "PSLoadAssemblyFromNativeCode",
                    description: "Expose an API to allow assembly loading from native code"),
                new ExperimentalFeature(
                    name: PSFeedbackProvider,
                    description: "Replace the hard-coded suggestion framework with the extensible feedback provider"),
                new ExperimentalFeature(
                    name: PSNativeWindowsTildeExpansion,
                    description: "On windows, expand unquoted tilde (`~`) with the user's current home folder."),
                new ExperimentalFeature(
                    name: PSRedirectToVariable,
                    description: "Add support for redirecting to the variable drive"),
                new ExperimentalFeature(
                    name: PSSerializeJSONLongEnumAsNumber,
                    description: "Serialize enums based on long or ulong as an numeric value rather than the string representation when using ConvertTo-Json."
                )
            };

            EngineExperimentalFeatures = new ReadOnlyCollection<ExperimentalFeature>(engineFeatures);

            // Initialize the readonly dictionary 'EngineExperimentalFeatureMap'.
            var engineExpFeatureMap = engineFeatures.ToDictionary(static f => f.Name, StringComparer.OrdinalIgnoreCase);
            EngineExperimentalFeatureMap = new ReadOnlyDictionary<string, ExperimentalFeature>(engineExpFeatureMap);

            // Initialize the readonly hashset 'EnabledExperimentalFeatureNames'.
            // The initialization of 'EnabledExperimentalFeatureNames' is deliberately made in the type initializer so that:
            //   1. 'EnabledExperimentalFeatureNames' can be declared as readonly;
            //   2. No need to deal with initialization from multiple threads;
            //   3. We don't need to decide where/when to read the config file for the enabled experimental features,
            //      instead, it will be done when the type is used for the first time, which is always earlier than
            //      any experimental features take effect.
            string[] enabledFeatures = Array.Empty<string>();
            try
            {
                enabledFeatures = PowerShellConfig.Instance.GetExperimentalFeatures();
            }
            catch (Exception e) when (LogException(e)) { }

            EnabledExperimentalFeatureNames = ProcessEnabledFeatures(enabledFeatures);
        }

        
        private static void SendTelemetryForDeactivatedFeatures(ReadOnlyBag<string> enabledFeatures)
        {
            foreach (var feature in EngineExperimentalFeatures)
            {
                if (!enabledFeatures.Contains(feature.Name))
                {
                    ApplicationInsightsTelemetry.SendTelemetryMetric(TelemetryType.ExperimentalEngineFeatureDeactivation, feature.Name);
                }
            }
        }

        
        private static ReadOnlyBag<string> ProcessEnabledFeatures(string[] enabledFeatures)
        {
            if (enabledFeatures.Length == 0)
            {
                return ReadOnlyBag<string>.Empty;
            }

            var list = new List<string>(enabledFeatures.Length);
            foreach (string name in enabledFeatures)
            {
                if (IsModuleFeatureName(name))
                {
                    list.Add(name);
                    ApplicationInsightsTelemetry.SendTelemetryMetric(TelemetryType.ExperimentalModuleFeatureActivation, name);
                }
                else if (IsEngineFeatureName(name))
                {
                    if (EngineExperimentalFeatureMap.TryGetValue(name, out ExperimentalFeature feature))
                    {
                        feature.Enabled = true;
                        list.Add(name);
                        ApplicationInsightsTelemetry.SendTelemetryMetric(TelemetryType.ExperimentalEngineFeatureActivation, name);
                    }
                    else
                    {
                        string message = StringUtil.Format(Logging.EngineExperimentalFeatureNotFound, name);
                        LogError(PSEventId.ExperimentalFeature_InvalidName, name, message);
                    }
                }
                else
                {
                    string message = StringUtil.Format(Logging.InvalidExperimentalFeatureName, name);
                    LogError(PSEventId.ExperimentalFeature_InvalidName, name, message);
                }
            }

            ReadOnlyBag<string> features = new(new HashSet<string>(list, StringComparer.OrdinalIgnoreCase));
            SendTelemetryForDeactivatedFeatures(features);
            return features;
        }

        
        private static bool LogException(Exception e)
        {
            LogError(PSEventId.ExperimentalFeature_ReadConfig_Error, e.GetType().FullName, e.Message, e.StackTrace);
            return false;
        }

        
        private static void LogError(PSEventId eventId, params object[] args)
        {
            
        }

        
        internal static bool IsEngineFeatureName(string featureName)
        {
            return featureName.Length > 2 && !featureName.Contains('.') && featureName.StartsWith("PS", StringComparison.Ordinal);
        }

        
        /// <param name="featureName">The feature name to check.</param>
        /// <param name="moduleName">When specified, we check if the feature name matches the module name.</param>
        internal static bool IsModuleFeatureName(string featureName, string moduleName = null)
        {
            // Feature names cannot start with a dot
            if (featureName.StartsWith('.'))
            {
                return false;
            }

            // Feature names must contain a dot, but not at the end
            int lastDotIndex = featureName.LastIndexOf('.');
            if (lastDotIndex == -1 || lastDotIndex == featureName.Length - 1)
            {
                return false;
            }

            if (moduleName == null)
            {
                return true;
            }

            // If the module name is given, it must match the prefix of the feature name (up to the last dot).
            var moduleNamePart = featureName.AsSpan(0, lastDotIndex);
            return moduleNamePart.Equals(moduleName.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }

        
        internal static ExperimentAction GetActionToTake(string experimentName, ExperimentAction experimentAction)
        {
            if (experimentName == null || experimentAction == ExperimentAction.None)
            {
                // If either the experiment name or action is not defined, then return 'Show' by default.
                // This could happen to 'ParameterAttribute' when no experimental related field is declared.
                return ExperimentAction.Show;
            }

            ExperimentAction action = experimentAction;
            if (!IsEnabled(experimentName))
            {
                action = (action == ExperimentAction.Hide) ? ExperimentAction.Show : ExperimentAction.Hide;
            }

            return action;
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnabled(string featureName)
        {
            return false;
            // return EnabledExperimentalFeatureNames.Contains(featureName);
        }

        #endregion
    }

    
    public enum ExperimentAction
    {
        
        None = 0,

        
        Hide = 1,

        
        Show = 2
    }

    
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ExperimentalAttribute : ParsingBaseAttribute
    {
        
        public string ExperimentName { get; }

        
        public ExperimentAction ExperimentAction { get; }

        
        public ExperimentalAttribute(string experimentName, ExperimentAction experimentAction)
        {
            ValidateArguments(experimentName, experimentAction);
            ExperimentName = experimentName;
            ExperimentAction = experimentAction;
        }

        
        private ExperimentalAttribute() { }

        
        internal static readonly ExperimentalAttribute None = new ExperimentalAttribute();

        
        internal static void ValidateArguments(string experimentName, ExperimentAction experimentAction)
        {
            if (string.IsNullOrEmpty(experimentName))
            {
                const string paramName = nameof(experimentName);
                throw PSTraceSource.NewArgumentNullException(paramName, Metadata.ArgumentNullOrEmpty, paramName);
            }

            if (experimentAction == ExperimentAction.None)
            {
                const string paramName = nameof(experimentAction);
                const string invalidMember = nameof(ExperimentAction.None);
                string validMembers = StringUtil.Format("{0}, {1}", ExperimentAction.Hide, ExperimentAction.Show);
                throw PSTraceSource.NewArgumentException(paramName, Metadata.InvalidEnumArgument, invalidMember, paramName, validMembers);
            }
        }

        internal bool ToHide => EffectiveAction == ExperimentAction.Hide;

        internal bool ToShow => EffectiveAction == ExperimentAction.Show;

        
        private ExperimentAction EffectiveAction
        {
            get
            {
                if (_effectiveAction == ExperimentAction.None)
                {
                    _effectiveAction = ExperimentalFeature.GetActionToTake(ExperimentName, ExperimentAction);
                }

                return _effectiveAction;
            }
        }

        private ExperimentAction _effectiveAction = ExperimentAction.None;
    }
}
