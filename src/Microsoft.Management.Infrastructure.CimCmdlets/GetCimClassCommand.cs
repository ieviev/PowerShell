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
    
    [Alias("gcls")]
    [Cmdlet(VerbsCommon.Get, GetCimClassCommand.Noun, DefaultParameterSetName = ComputerSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227959")]
    [OutputType(typeof(CimClass))]
    public class GetCimClassCommand : CimBaseCommand
    {
        #region constructor

        
        public GetCimClassCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters

        
        [Parameter]
        public SwitchParameter Amended { get; set; }

        
        [Parameter(
            Position = 0,
            ValueFromPipelineByPropertyName = true)]
        public string ClassName { get; set; }

        
        [Parameter(
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public string Namespace { get; set; }

        
        [Alias(AliasOT)]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public uint OperationTimeoutSec { get; set; }

        
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = SessionSetName)]
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

        
        [Alias(AliasCN, AliasServerName)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
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

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string MethodName { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string PropertyName { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string QualifierName { get; set; }

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
            CimGetCimClass cimGetCimClass = this.GetOperationAgent() ?? CreateOperationAgent();

            cimGetCimClass.GetCimClass(this);
            cimGetCimClass.ProcessActions(this.CmdletOperation);
        }

        
        protected override void EndProcessing()
        {
            CimGetCimClass cimGetCimClass = this.GetOperationAgent();
            cimGetCimClass?.ProcessRemainActions(this.CmdletOperation);
        }

        #endregion

        #region helper methods

        
        private CimGetCimClass GetOperationAgent()
        {
            return this.AsyncOperation as CimGetCimClass;
        }

        
        private CimGetCimClass CreateOperationAgent()
        {
            CimGetCimClass cimGetCimClass = new();
            this.AsyncOperation = cimGetCimClass;
            return cimGetCimClass;
        }

        #endregion

        #region internal const strings

        
        internal const string Noun = @"CimClass";

        #endregion

        #region private members

        #region const string of parameter names
        internal const string nameCimSession = "CimSession";
        internal const string nameComputerName = "ComputerName";
        #endregion

        
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.SessionSetName, true),
                                 }
            },

            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ComputerSetName, false),
                                 }
            },
        };

        
        private static readonly Dictionary<string, ParameterSetEntry> parameterSets = new()
        {
            {   CimBaseCommand.SessionSetName, new ParameterSetEntry(1)     },
            {   CimBaseCommand.ComputerSetName, new ParameterSetEntry(0, true)     },
        };
        #endregion
    }
}
