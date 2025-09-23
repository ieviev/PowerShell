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
    
    [Alias("gcai")]
    [Cmdlet(VerbsCommon.Get,
        GetCimAssociatedInstanceCommand.Noun,
        DefaultParameterSetName = CimBaseCommand.ComputerSetName,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227958")]
    [OutputType(typeof(CimInstance))]
    public class GetCimAssociatedInstanceCommand : CimBaseCommand
    {
        #region constructor

        
        public GetCimAssociatedInstanceCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters

        
        [Parameter(
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public string Association { get; set; }

        
        [Parameter]
        public string ResultClassName { get; set; }

        
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true)]
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

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Namespace { get; set; }

        
        [Alias(AliasOT)]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public uint OperationTimeoutSec { get; set; }

        
        [Parameter]
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
            ParameterSetName = ComputerSetName)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName
        {
            get
            {
                return computerName;
            }

            set
            {
                computerName = value;
                base.SetParameter(value, nameComputerName);
            }
        }

        private string[] computerName;

        
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = SessionSetName)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Microsoft.Management.Infrastructure.CimSession[] CimSession
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

        private Microsoft.Management.Infrastructure.CimSession[] cimSession;

        
        [Parameter]
        public SwitchParameter KeyOnly { get; set; }

        #endregion

        #region cmdlet methods

        
        protected override void BeginProcessing()
        {
            this.CmdletOperation = new CmdletOperationBase(this);
            this.AtBeginProcess = false;
        }

        
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            CimGetAssociatedInstance operation = this.GetOperationAgent() ?? this.CreateOperationAgent();

            operation.GetCimAssociatedInstance(this);
            operation.ProcessActions(this.CmdletOperation);
        }

        
        protected override void EndProcessing()
        {
            CimGetAssociatedInstance operation = this.GetOperationAgent();
            operation?.ProcessRemainActions(this.CmdletOperation);
        }

        #endregion

        #region helper methods

        
        private CimGetAssociatedInstance GetOperationAgent()
        {
            return this.AsyncOperation as CimGetAssociatedInstance;
        }

        
        private CimGetAssociatedInstance CreateOperationAgent()
        {
            this.AsyncOperation = new CimGetAssociatedInstance();
            return GetOperationAgent();
        }

        #endregion

        #region internal const strings

        
        internal const string Noun = @"CimAssociatedInstance";

        #endregion

        #region private members

        #region const string of parameter names
        internal const string nameCimInstance = "InputObject";
        internal const string nameComputerName = "ComputerName";
        internal const string nameCimSession = "CimSession";
        internal const string nameResourceUri = "ResourceUri";
        #endregion

        
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ComputerSetName, false),
                                 }
            },
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.SessionSetName, true),
                                 }
            },
            {
                nameCimInstance, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ComputerSetName, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.SessionSetName, true),
                                 }
            },
            {
                nameResourceUri, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ComputerSetName, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.SessionSetName, false),
                                 }
            },
        };

        
        private static readonly Dictionary<string, ParameterSetEntry> parameterSets = new()
        {
            {   CimBaseCommand.SessionSetName, new ParameterSetEntry(2, false)     },
            {   CimBaseCommand.ComputerSetName, new ParameterSetEntry(1, true)     },
        };
        #endregion
    }
}
