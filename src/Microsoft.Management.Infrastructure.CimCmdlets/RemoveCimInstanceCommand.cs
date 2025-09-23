// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    [Alias("rcim")]
    [Cmdlet(
        VerbsCommon.Remove,
        "CimInstance",
        SupportsShouldProcess = true,
        DefaultParameterSetName = CimBaseCommand.CimInstanceComputerSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227964")]
    public class RemoveCimInstanceCommand : CimBaseCommand
    {
        #region constructor

        
        public RemoveCimInstanceCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters
        
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public CimSession[] CimSession
        {
            get
            {
                return cimSession;
            }

            set
            {
                cimSession = value;
                base.SetParameter(value, nameCimSession);
            }
        }

        private CimSession[] cimSession;

        
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        public Uri ResourceUri
        {
            get
            {
                return resourceUri;
            }

            set
            {
                this.resourceUri = value;
                base.SetParameter(value, nameResourceUri);
            }
        }

        private Uri resourceUri;

        
        [Alias(AliasCN, AliasServerName)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName
        {
            get
            {
                return computername;
            }

            set
            {
                computername = value;
                base.SetParameter(value, nameComputerName);
            }
        }

        private string[] computername;

        
        [Parameter(
            Position = 1,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [Parameter(
            Position = 1,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        public string Namespace
        {
            get
            {
                return nameSpace;
            }

            set
            {
                nameSpace = value;
                base.SetParameter(value, nameNamespace);
            }
        }

        private string nameSpace;

        
        [Alias(AliasOT)]
        [Parameter]
        public uint OperationTimeoutSec { get; set; }

        
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        [Alias(CimBaseCommand.AliasCimInstance)]
        public CimInstance InputObject
        {
            get
            {
                return CimInstance;
            }

            set
            {
                CimInstance = value;
                base.SetParameter(value, nameCimInstance);
            }
        }

        
        internal CimInstance CimInstance { get; private set; }

        
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        public string Query
        {
            get
            {
                return query;
            }

            set
            {
                query = value;
                base.SetParameter(value, nameQuery);
            }
        }

        private string query;

        
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.QueryComputerSet)]
        public string QueryDialect
        {
            get
            {
                return querydialect;
            }

            set
            {
                querydialect = value;
                base.SetParameter(value, nameQueryDialect);
            }
        }

        private string querydialect;

        #endregion

        #region cmdlet methods

        
        protected override void BeginProcessing()
        {
            CimRemoveCimInstance cimRemoveInstance = this.GetOperationAgent() ?? CreateOperationAgent();

            this.CmdletOperation = new CmdletOperationRemoveCimInstance(this, cimRemoveInstance);
            this.AtBeginProcess = false;
        }

        
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            CimRemoveCimInstance cimRemoveInstance = this.GetOperationAgent();
            cimRemoveInstance.RemoveCimInstance(this);
            cimRemoveInstance.ProcessActions(this.CmdletOperation);
        }

        
        protected override void EndProcessing()
        {
            CimRemoveCimInstance cimRemoveInstance = this.GetOperationAgent();
            cimRemoveInstance?.ProcessRemainActions(this.CmdletOperation);
        }

        #endregion

        #region helper methods

        
        private CimRemoveCimInstance GetOperationAgent()
        {
            return this.AsyncOperation as CimRemoveCimInstance;
        }

        
        /// <returns></returns>
        private CimRemoveCimInstance CreateOperationAgent()
        {
            CimRemoveCimInstance cimRemoveInstance = new();
            this.AsyncOperation = cimRemoveInstance;
            return cimRemoveInstance;
        }

        #endregion

        #region private members

        #region const string of parameter names
        internal const string nameCimSession = "CimSession";
        internal const string nameComputerName = "ComputerName";
        internal const string nameResourceUri = "ResourceUri";
        internal const string nameNamespace = "Namespace";
        internal const string nameCimInstance = "InputObject";
        internal const string nameQuery = "Query";
        internal const string nameQueryDialect = "QueryDialect";
        #endregion

        
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, true),
                                 }
            },
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, false),
                                 }
            },
            {
                nameNamespace, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                 }
            },
            {
                nameCimInstance, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, true),
                                 }
            },
            {
                nameQuery, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, true),
                                 }
            },
            {
                nameQueryDialect, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                 }
            },
            {
                nameResourceUri, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, false),
                                 }
            },
        };

        
        private static readonly Dictionary<string, ParameterSetEntry> parameterSets = new()
        {
            {   CimBaseCommand.CimInstanceComputerSet, new ParameterSetEntry(1, true)     },
            {   CimBaseCommand.CimInstanceSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.QueryComputerSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.QuerySessionSet, new ParameterSetEntry(2)     },
        };
        #endregion
    }
}
