// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    /// <remarks>
    /// </remarks>
    [Cmdlet(VerbsCommon.Get, "ChildItem", DefaultParameterSetName = "Items", SupportsTransactions = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096492")]
    public class GetChildItemCommand : CoreCommandBase
    {
        
        /// <remarks>
        /// The "Items" parameter set includes the following parameters:
        ///     -filter
        ///     -recurse
        /// </remarks>
        private const string childrenSet = "Items";
        private const string literalChildrenSet = "LiteralItems";

        #region Command parameters

        
        [Parameter(Position = 0, ParameterSetName = childrenSet,
                   ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Path
        {
            get
            {
                return _paths;
            }

            set
            {
                _paths = value;
            }
        }

        
        [Parameter(ParameterSetName = literalChildrenSet,
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get
            {
                return _paths;
            }

            set
            {
                base.SuppressWildcardExpansion = true;
                _paths = value;
            }
        }

        
        [Parameter(Position = 1)]
        public override string Filter
        {
            get
            {
                return base.Filter;
            }

            set
            {
                base.Filter = value;
            }
        }

        
        [Parameter]
        public override string[] Include
        {
            get
            {
                return base.Include;
            }

            set
            {
                base.Include = value;
            }
        }

        
        [Parameter]
        public override string[] Exclude
        {
            get
            {
                return base.Exclude;
            }

            set
            {
                base.Exclude = value;
            }
        }

        
        [Parameter]
        [Alias("s", "r")]
        public SwitchParameter Recurse
        {
            get
            {
                return _recurse;
            }

            set
            {
                _recurse = value;
            }
        }

        
        [Parameter]
        public uint Depth
        {
            get
            {
                return _depth;
            }

            set
            {
                _depth = value;
                this.Recurse = true; // Bug 2391925 - Get-ChildItem -Depth should auto-set -Recurse
            }
        }

        
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get
            {
                return base.Force;
            }

            set
            {
                base.Force = value;
            }
        }

        
        [Parameter]
        public SwitchParameter Name
        {
            get
            {
                return _childNames;
            }

            set
            {
                _childNames = value;
            }
        }

        
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            object result = null;
            string path = string.Empty;

            if (_paths != null && _paths.Length > 0)
            {
                path = _paths[0];
            }
            else
            {
                path = ".";
            }

            switch (ParameterSetName)
            {
                case childrenSet:
                case literalChildrenSet:
                    if (Name)
                    {
                        result = InvokeProvider.ChildItem.GetChildNamesDynamicParameters(path, context);
                    }
                    else
                    {
                        result = InvokeProvider.ChildItem.GetChildItemsDynamicParameters(path, Recurse, context);
                    }

                    break;

                default:
                    result = InvokeProvider.ChildItem.GetChildItemsDynamicParameters(path, Recurse, context);
                    break;
            }

            return result;
        }

        #endregion Command parameters

        #region command data

        
        private string[] _paths;

        
        private bool _recurse;

        
        private uint _depth = uint.MaxValue;

        
        private bool _childNames = false;

        #endregion command data

        #region command code

        
        protected override void ProcessRecord()
        {
            CmdletProviderContext currentContext = CmdletProviderContext;

            if (_paths == null || _paths.Length == 0)
            {
                _paths = new string[] { string.Empty };
            }

            foreach (string path in _paths)
            {
                switch (ParameterSetName)
                {
                    case childrenSet:
                    case literalChildrenSet:
                        try
                        {
                            if (Name)
                            {
                                // Get the names of the child items using the static namespace method.
                                // The child names should be written directly to the pipeline using the
                                // context.WriteObject method.

                                InvokeProvider.ChildItem.GetNames(path, ReturnContainers.ReturnMatchingContainers, Recurse, Depth, currentContext);
                            }
                            else
                            {
                                // Get the children using the static namespace method.
                                // The children should be written directly to the pipeline using
                                // the context.WriteObject method.

                                InvokeProvider.ChildItem.Get(path, Recurse, Depth, currentContext);
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

                        break;

                    default:
                        Dbg.Diagnostics.Assert(
                            false,
                            "Only one of the specified parameter sets should be called.");
                        break;
                }
            }
        }

        #endregion command code
    }
}
