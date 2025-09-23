// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    public class UpdateData : PSCmdlet
    {
        
        protected const string FileParameterSet = "FileSet";

        
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = FileParameterSet)]
        [Alias("PSPath", "Path")]
        [ValidateNotNull]
        public string[] AppendPath { get; set; } = Array.Empty<string>();

        
        [Parameter(ParameterSetName = FileParameterSet)]
        [ValidateNotNull]
        public string[] PrependPath { get; set; } = Array.Empty<string>();

        private static void ReportWrongExtension(string file, string errorId, PSCmdlet cmdlet)
        {
            ErrorRecord errorRecord = new(
                PSTraceSource.NewInvalidOperationException(UpdateDataStrings.UpdateData_WrongExtension, file, "ps1xml"),
                errorId,
                ErrorCategory.InvalidArgument,
                null);
            cmdlet.WriteError(errorRecord);
        }

        private static void ReportWrongProviderType(string providerId, string errorId, PSCmdlet cmdlet)
        {
            ErrorRecord errorRecord = new(
                PSTraceSource.NewInvalidOperationException(UpdateDataStrings.UpdateData_WrongProviderError, providerId),
                errorId,
                ErrorCategory.InvalidArgument,
                null);
            cmdlet.WriteError(errorRecord);
        }

        
        internal static Collection<string> Glob(string[] files, string errorId, PSCmdlet cmdlet)
        {
            Collection<string> retValue = new();
            foreach (string file in files)
            {
                Collection<string> providerPaths;
                ProviderInfo provider = null;
                try
                {
                    providerPaths = cmdlet.SessionState.Path.GetResolvedProviderPathFromPSPath(file, out provider);
                }
                catch (SessionStateException e)
                {
                    cmdlet.WriteError(new ErrorRecord(e, errorId, ErrorCategory.InvalidOperation, file));
                    continue;
                }

                if (!provider.NameEquals(cmdlet.Context.ProviderNames.FileSystem))
                {
                    ReportWrongProviderType(provider.FullName, errorId, cmdlet);
                    continue;
                }

                foreach (string providerPath in providerPaths)
                {
                    if (!providerPath.EndsWith(".ps1xml", StringComparison.OrdinalIgnoreCase))
                    {
                        ReportWrongExtension(providerPath, "WrongExtension", cmdlet);
                        continue;
                    }

                    retValue.Add(providerPath);
                }
            }

            return retValue;
        }
    }
}
