// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    
    public enum ExportAliasFormat
    {
        
        Csv,

        
        Script
    }

    
    [Cmdlet(VerbsData.Export, "Alias", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096597")]
    [OutputType(typeof(AliasInfo))]
    public class ExportAliasCommand : PSCmdlet
    {
        #region Parameters

        
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByPath")]
        public string Path
        {
            get { return _path; }

            set { _path = value ?? "."; }
        }

        private string _path = ".";

        
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByLiteralPath")]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get
            {
                return _path;
            }

            set
            {
                if (value == null)
                {
                    _path = ".";
                }
                else
                {
                    _path = value;
                    _isLiteralPath = true;
                }
            }
        }

        private bool _isLiteralPath = false;

        
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        public string[] Name
        {
            get { return _names; }

            set { _names = value ?? new string[] { "*" }; }
        }

        private string[] _names = new string[] { "*" };

        
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }

            set
            {
                _passThru = value;
            }
        }

        private bool _passThru;

        
        [Parameter]
        public ExportAliasFormat As { get; set; } = ExportAliasFormat.Csv;

        
        [Parameter]
        public SwitchParameter Append
        {
            get
            {
                return _append;
            }

            set
            {
                _append = value;
            }
        }

        private bool _append;

        
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        private bool _force;

        
        [Parameter]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber
        {
            get
            {
                return _noclobber;
            }

            set
            {
                _noclobber = value;
            }
        }

        private bool _noclobber;

        
        [Parameter]
        public string Description { get; set; }

        
        [Parameter]
        [ArgumentCompleter(typeof(ScopeArgumentCompleter))]
        public string Scope { get; set; }

        #endregion Parameters

        #region Command code

        
        protected override void ProcessRecord()
        {
            // First get the alias table (from the proper scope if necessary)
            IDictionary<string, AliasInfo> aliasTable = null;

            if (!string.IsNullOrEmpty(Scope))
            {
                // This can throw PSArgumentException and PSArgumentOutOfRangeException
                // but just let them go as this is terminal for the pipeline and the
                // exceptions are already properly adorned with an ErrorRecord.

                aliasTable = SessionState.Internal.GetAliasTableAtScope(Scope);
            }
            else
            {
                aliasTable = SessionState.Internal.GetAliasTable();
            }

            foreach (string aliasName in _names)
            {
                bool resultFound = false;

                // Create the name pattern

                WildcardPattern namePattern =
                    WildcardPattern.Get(
                        aliasName,
                        WildcardOptions.IgnoreCase);

                // Now loop through the table and write out any aliases that
                // match the name and don't match the exclude filters and are
                // visible to the caller...
                CommandOrigin origin = MyInvocation.CommandOrigin;
                foreach (KeyValuePair<string, AliasInfo> tableEntry in aliasTable)
                {
                    if (!namePattern.IsMatch(tableEntry.Key))
                    {
                        continue;
                    }

                    if (SessionState.IsVisible(origin, tableEntry.Value))
                    {
                        resultFound = true;
                        _matchingAliases.Add(tableEntry.Value);
                    }
                }

                if (!resultFound &&
                    !WildcardPattern.ContainsWildcardCharacters(aliasName))
                {
                    // Need to write an error if the user tries to get an alias
                    // that doesn't exist and they are not globbing.

                    ItemNotFoundException itemNotFound =
                        new(
                            aliasName,
                            "AliasNotFound",
                            SessionStateStrings.AliasNotFound);

                    WriteError(
                        new ErrorRecord(
                            itemNotFound.ErrorRecord,
                            itemNotFound));
                }
            }
        }

        
        protected override void EndProcessing()
        {
            StreamWriter writer = null;
            FileInfo readOnlyFileInfo = null;
            try
            {
                if (ShouldProcess(Path))
                {
                    writer = OpenFile(out readOnlyFileInfo);
                }

                if (writer != null)
                    WriteHeader(writer);

                // Now write out the aliases

                foreach (AliasInfo alias in _matchingAliases)
                {
                    string line = null;
                    if (this.As == ExportAliasFormat.Csv)
                    {
                        line = GetAliasLine(alias, "\"{0}\",\"{1}\",\"{2}\",\"{3}\"");
                    }
                    else
                    {
                        line = GetAliasLine(alias, "set-alias -Name:\"{0}\" -Value:\"{1}\" -Description:\"{2}\" -Option:\"{3}\"");
                    }

                    writer?.WriteLine(line);

                    if (PassThru)
                    {
                        WriteObject(alias);
                    }
                }
            }
            finally
            {
                writer?.Dispose();
                // reset the read-only attribute
                if (readOnlyFileInfo != null)
                    readOnlyFileInfo.Attributes |= FileAttributes.ReadOnly;
            }
        }

        
        private readonly Collection<AliasInfo> _matchingAliases = new();

        private static string GetAliasLine(AliasInfo alias, string formatString)
        {
            // Using the invariant culture here because we don't want the
            // file to vary based on locale.

            string result =
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    formatString,
                    alias.Name,
                    alias.Definition,
                    alias.Description,
                    alias.Options);

            return result;
        }

        private void WriteHeader(StreamWriter writer)
        {
            WriteFormattedResourceString(writer, AliasCommandStrings.ExportAliasHeaderTitle);

            string user = Environment.UserName;
            WriteFormattedResourceString(writer, AliasCommandStrings.ExportAliasHeaderUser, user);

            DateTime now = DateTime.Now;
            WriteFormattedResourceString(writer, AliasCommandStrings.ExportAliasHeaderDate, now);

            string machine = Environment.MachineName;
            WriteFormattedResourceString(writer, AliasCommandStrings.ExportAliasHeaderMachine, machine);

            // Now write the description if there is one

            if (Description != null)
            {
                // First we need to break up the description on newlines and add a
                // # for each line.

                Description = Description.Replace("\n", "\n# ");

                // Now write out the description
                writer.WriteLine("#");
                writer.Write("# ");
                writer.WriteLine(Description);
            }
        }

        private static void WriteFormattedResourceString(
            StreamWriter writer,
            string resourceId,
            params object[] args)
        {
            string line = StringUtil.Format(resourceId, args);

            writer.Write("# ");

            writer.WriteLine(line);
        }

        
        private StreamWriter OpenFile(out FileInfo readOnlyFileInfo)
        {
            StreamWriter result = null;
            FileStream file = null;
            readOnlyFileInfo = null;

            PathUtils.MasterStreamOpen(
                this,
                this.Path,
                EncodingConversion.Unicode,
                false, // defaultEncoding
                Append,
                Force,
                NoClobber,
                out file,
                out result,
                out readOnlyFileInfo,
                _isLiteralPath
                );

            return result;
        }

        private void ThrowFileOpenError(Exception e, string pathWithError)
        {
            string message = StringUtil.Format(AliasCommandStrings.ExportAliasFileOpenFailed, pathWithError, e.Message);

            ErrorRecord errorRecord = new(
                e,
                "FileOpenFailure",
                ErrorCategory.OpenError,
                pathWithError);

            errorRecord.ErrorDetails = new ErrorDetails(message);
            this.ThrowTerminatingError(errorRecord);
        }

        #endregion Command code
    }
}
