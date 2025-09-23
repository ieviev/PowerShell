// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Split, "Path", DefaultParameterSetName = "ParentSet", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097149")]
    [OutputType(typeof(string), ParameterSetName = new[] { leafSet,
                                                           leafBaseSet,
                                                           extensionSet,
                                                           noQualifierSet,
                                                           parentSet,
                                                           qualifierSet,
                                                           literalPathSet})]
    [OutputType(typeof(bool), ParameterSetName = new[] { isAbsoluteSet })]
    public class SplitPathCommand : CoreCommandWithCredentialsBase
    {
        #region Parameters

        
        private const string parentSet = "ParentSet";

        
        private const string leafSet = "LeafSet";

        
        private const string leafBaseSet = "LeafBaseSet";

        
        private const string extensionSet = "ExtensionSet";

        
        private const string qualifierSet = "QualifierSet";

        
        private const string noQualifierSet = "NoQualifierSet";

        
        private const string isAbsoluteSet = "IsAbsoluteSet";

        
        private const string literalPathSet = "LiteralPathSet";

        
        [Parameter(Position = 0, ParameterSetName = parentSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = leafSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = leafBaseSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = extensionSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = qualifierSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = noQualifierSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = isAbsoluteSet, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path { get; set; }

        
        [Parameter(ParameterSetName = "LiteralPathSet", Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get
            {
                return Path;
            }

            set
            {
                base.SuppressWildcardExpansion = true;
                Path = value;
            }
        }

        
        /// <value>
        /// If true the qualifier of the path will be returned.
        /// The qualifier is the drive or provider that is qualifying
        /// the PowerShell path.
        /// </value>
        [Parameter(ParameterSetName = qualifierSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Qualifier { get; set; }

        
        /// <value>
        /// If true the qualifier of the path will be returned.
        /// The qualifier is the drive or provider that is qualifying
        /// the PowerShell path.
        /// </value>
        [Parameter(ParameterSetName = noQualifierSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter NoQualifier { get; set; }

        
        /// <value>
        /// If true the parent of the path will be returned.
        /// </value>
        [Parameter(ParameterSetName = parentSet, Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Parent { get; set; } = true;

        
        /// <value>
        /// If true the leaf name of the path will be returned.
        /// </value>
        [Parameter(ParameterSetName = leafSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Leaf { get; set; }

        
        /// <value>
        /// If true the leaf base name of the path will be returned.
        /// </value>
        [Parameter(ParameterSetName = leafBaseSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter LeafBase { get; set; }

        
        /// <value>
        /// If true the extension of the path will be returned.
        /// </value>
        [Parameter(ParameterSetName = extensionSet, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Extension { get; set; }

        
        /// <value></value>
        [Parameter]
        public SwitchParameter Resolve { get; set; }

        
        [Parameter(ParameterSetName = isAbsoluteSet, Mandatory = true)]
        public SwitchParameter IsAbsolute { get; set; }

        #endregion Parameters

        #region parameter data

        #endregion parameter data

        #region Command code

        
        protected override void ProcessRecord()
        {
            StringCollection pathsToParse = new();

            if (Resolve)
            {
                CmdletProviderContext currentContext = CmdletProviderContext;

                foreach (string path in Path)
                {
                    // resolve the paths and then parse each one.

                    Collection<PathInfo> resolvedPaths;

                    try
                    {
                        resolvedPaths =
                            SessionState.Path.GetResolvedPSPathFromPSPath(path, currentContext);
                    }
                    catch (PSNotSupportedException notSupported)
                    {
                        WriteError(
                            new ErrorRecord(
                                notSupported.ErrorRecord,
                                notSupported));
                        continue;
                    }
                    catch (DriveNotFoundException driveNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                driveNotFound.ErrorRecord,
                                driveNotFound));
                        continue;
                    }
                    catch (ProviderNotFoundException providerNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                providerNotFound.ErrorRecord,
                                providerNotFound));
                        continue;
                    }
                    catch (ItemNotFoundException pathNotFound)
                    {
                        WriteError(
                            new ErrorRecord(
                                pathNotFound.ErrorRecord,
                                pathNotFound));
                        continue;
                    }

                    foreach (PathInfo resolvedPath in resolvedPaths)
                    {
                        try
                        {
                            if (InvokeProvider.Item.Exists(resolvedPath.Path, currentContext))
                            {
                                pathsToParse.Add(resolvedPath.Path);
                            }
                        }
                        catch (PSNotSupportedException notSupported)
                        {
                            WriteError(
                                new ErrorRecord(
                                    notSupported.ErrorRecord,
                                    notSupported));
                            continue;
                        }
                        catch (DriveNotFoundException driveNotFound)
                        {
                            WriteError(
                                new ErrorRecord(
                                    driveNotFound.ErrorRecord,
                                    driveNotFound));
                            continue;
                        }
                        catch (ProviderNotFoundException providerNotFound)
                        {
                            WriteError(
                                new ErrorRecord(
                                    providerNotFound.ErrorRecord,
                                    providerNotFound));
                            continue;
                        }
                        catch (ItemNotFoundException pathNotFound)
                        {
                            WriteError(
                                new ErrorRecord(
                                    pathNotFound.ErrorRecord,
                                    pathNotFound));
                            continue;
                        }
                    }
                }
            }
            else
            {
                pathsToParse.AddRange(Path);
            }

            // Now parse each path

            for (int index = 0; index < pathsToParse.Count; ++index)
            {
                string result = null;

                switch (ParameterSetName)
                {
                    case isAbsoluteSet:
                        string ignored;
                        bool isPathAbsolute =
                            SessionState.Path.IsPSAbsolute(pathsToParse[index], out ignored);

                        WriteObject(isPathAbsolute);
                        continue;

                    case qualifierSet:
                        int separatorIndex = pathsToParse[index].IndexOf(':');

                        if (separatorIndex < 0)
                        {
                            FormatException e =
                                new(
                                    StringUtil.Format(NavigationResources.ParsePathFormatError, pathsToParse[index]));
                            WriteError(
                                new ErrorRecord(
                                    e,
                                    "ParsePathFormatError", // RENAME
                                    ErrorCategory.InvalidArgument,
                                    pathsToParse[index]));
                            continue;
                        }
                        else
                        {
                            // Check to see if it is provider or drive qualified

                            if (SessionState.Path.IsProviderQualified(pathsToParse[index]))
                            {
                                // The plus 2 is for the length of the provider separator
                                // which is "::"

                                result =
                                    pathsToParse[index].Substring(
                                        0,
                                        separatorIndex + 2);
                            }
                            else
                            {
                                result =
                                    pathsToParse[index].Substring(
                                        0,
                                        separatorIndex + 1);
                            }
                        }

                        break;

                    case parentSet:
                    case literalPathSet:
                        try
                        {
                            result =
                                SessionState.Path.ParseParent(
                                    pathsToParse[index],
                                    string.Empty,
                                    CmdletProviderContext,
                                    true);
                        }
                        catch (PSNotSupportedException)
                        {
                            // Since getting the parent path is not supported,
                            // the provider must be a container, item, or drive
                            // provider.  Since the paths for these types of
                            // providers can't be split, asking for the parent
                            // is asking for an empty string.
                            result = string.Empty;
                        }

                        break;

                    case leafSet:
                    case leafBaseSet:
                    case extensionSet:
                        try
                        {
                            // default handles leafSet
                            result =
                                SessionState.Path.ParseChildName(
                                    pathsToParse[index],
                                    CmdletProviderContext,
                                    true);
                            if (LeafBase)
                            {
                                result = System.IO.Path.GetFileNameWithoutExtension(result);
                            }
                            else if (Extension)
                            {
                                result = System.IO.Path.GetExtension(result);
                            }
                        }
                        catch (PSNotSupportedException)
                        {
                            // Since getting the leaf part of a path is not supported,
                            // the provider must be a container, item, or drive
                            // provider.  Since the paths for these types of
                            // providers can't be split, asking for the leaf
                            // is asking for the specified path back.
                            result = pathsToParse[index];
                        }
                        catch (DriveNotFoundException driveNotFound)
                        {
                            WriteError(
                                new ErrorRecord(
                                    driveNotFound.ErrorRecord,
                                    driveNotFound));
                            continue;
                        }
                        catch (ProviderNotFoundException providerNotFound)
                        {
                            WriteError(
                                new ErrorRecord(
                                    providerNotFound.ErrorRecord,
                                    providerNotFound));
                            continue;
                        }

                        break;

                    case noQualifierSet:
                        result = RemoveQualifier(pathsToParse[index]);
                        break;

                    default:
                        Dbg.Diagnostics.Assert(
                            false,
                            "Only a known parameter set should be called");
                        break;
                }

                if (result != null)
                {
                    WriteObject(result);
                }
            }
        }
        #endregion Command code

        
        /// <param name="path">
        /// The path to strip the provider qualifier from.
        /// </param>
        /// <returns>
        /// The path without the qualifier.
        /// </returns>
        private string RemoveQualifier(string path)
        {
            Dbg.Diagnostics.Assert(
                path != null,
                "Path should be verified by the caller");

            string result = path;

            if (SessionState.Path.IsProviderQualified(path))
            {
                int index = path.IndexOf("::", StringComparison.Ordinal);

                if (index != -1)
                {
                    // remove the qualifier
                    result = path.Substring(index + 2);
                }
            }
            else
            {
                string driveName = string.Empty;

                if (SessionState.Path.IsPSAbsolute(path, out driveName))
                {
                    var driveNameLength = driveName.Length;
                    if (path.Length > (driveNameLength + 1) && path[driveNameLength] == ':')
                    {
                        // Remove the drive name and colon
                        result = path.Substring(driveNameLength + 1);
                    }
                }
            }

            return result;
        }
    }
}
