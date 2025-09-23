// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands
{
    
    public class EnableDisableExperimentalFeatureCommandBase : PSCmdlet
    {
        
        [Parameter(ValueFromPipelineByPropertyName = true, Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(ExperimentalFeatureNameCompleter))]
        public string[] Name { get; set; }

        
        [Parameter]
        public ConfigScope Scope { get; set; } = ConfigScope.CurrentUser;

        
        protected override void EndProcessing()
        {
            WriteWarning(ExperimentalFeatureStrings.ExperimentalFeaturePending);
        }
    }

    
    [Cmdlet(VerbsLifecycle.Enable, "ExperimentalFeature", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2046964")]
    public class EnableExperimentalFeatureCommand : EnableDisableExperimentalFeatureCommandBase
    {
        
        protected override void ProcessRecord()
        {
            ExperimentalFeatureConfigHelper.UpdateConfig(this, Name, Scope, enable: true);
        }
    }

    
    [Cmdlet(VerbsLifecycle.Disable, "ExperimentalFeature", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2046963")]
    public class DisableExperimentalFeatureCommand : EnableDisableExperimentalFeatureCommandBase
    {
        
        protected override void ProcessRecord()
        {
            ExperimentalFeatureConfigHelper.UpdateConfig(this, Name, Scope, enable: false);
        }
    }

    internal static class ExperimentalFeatureConfigHelper
    {
        internal static void UpdateConfig(PSCmdlet cmdlet, string[] name, ConfigScope scope, bool enable)
        {
            IEnumerable<WildcardPattern> namePatterns = SessionStateUtilities.CreateWildcardsFromStrings(name, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
            GetExperimentalFeatureCommand getExperimentalFeatureCommand = new GetExperimentalFeatureCommand();
            getExperimentalFeatureCommand.Context = cmdlet.Context;
            bool foundFeature = false;
            foreach (ExperimentalFeature feature in getExperimentalFeatureCommand.GetAvailableExperimentalFeatures(namePatterns))
            {
                foundFeature = true;
                if (!cmdlet.ShouldProcess(feature.Name))
                {
                    return;
                }

                PowerShellConfig.Instance.SetExperimentalFeatures(scope, feature.Name, enable);
            }

            if (!foundFeature)
            {
                string errMsg = string.Format(CultureInfo.InvariantCulture, ExperimentalFeatureStrings.ExperimentalFeatureNameNotFound, name);
                cmdlet.WriteError(new ErrorRecord(new ItemNotFoundException(errMsg), "ItemNotFoundException", ErrorCategory.ObjectNotFound, name));
                return;
            }
        }
    }

    
    public class ExperimentalFeatureNameCompleter : IArgumentCompleter
    {
        
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            SortedSet<string> expirmentalFeatures = new(StringComparer.OrdinalIgnoreCase);

            foreach (ExperimentalFeature feature in GetExperimentalFeatures())
            {
                expirmentalFeatures.Add(feature.Name);
            }

            return CompletionHelpers.GetMatchingResults(wordToComplete, expirmentalFeatures);
        }

        private static Collection<ExperimentalFeature> GetExperimentalFeatures()
        {
            using var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);
            ps.AddCommand("Get-ExperimentalFeature");
            return ps.Invoke<ExperimentalFeature>();
        }
    }
}
