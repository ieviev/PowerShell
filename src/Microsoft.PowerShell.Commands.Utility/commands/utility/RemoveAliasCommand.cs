// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Remove, "Alias", DefaultParameterSetName = "Default", HelpUri = "https://go.microsoft.com/fwlink/?linkid=2097127")]
    [Alias("ral")]
    public class RemoveAliasCommand : PSCmdlet
    {
        #region Parameters

        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Name { get; set; }

        
        [Parameter]
        [ArgumentCompleter(typeof(ScopeArgumentCompleter))]
        public string Scope { get; set; }

        
        [Parameter]
        public SwitchParameter Force { get; set; }

        #endregion Parameters

        #region Command code

        
        protected override void ProcessRecord()
        {
            foreach (string aliasName in Name)
            {
                AliasInfo existingAlias = null;
                if (string.IsNullOrEmpty(Scope))
                {
                    existingAlias = SessionState.Internal.GetAlias(aliasName);
                }
                else
                {
                    existingAlias = SessionState.Internal.GetAliasAtScope(aliasName, Scope);
                }

                if (existingAlias != null)
                {
                    SessionState.Internal.RemoveAlias(aliasName, Force);
                }
                else
                {
                    ItemNotFoundException notAliasFound = new(StringUtil.Format(AliasCommandStrings.NoAliasFound, "name", aliasName));
                    ErrorRecord error = new(notAliasFound, "ItemNotFoundException", ErrorCategory.ObjectNotFound, aliasName);
                    WriteError(error);
                }
            }
        }
        #endregion Command code
    }
}
