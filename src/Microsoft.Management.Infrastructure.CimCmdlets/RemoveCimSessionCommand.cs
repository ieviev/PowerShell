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
    
    [Alias("rcms")]
    [Cmdlet(VerbsCommon.Remove, "CimSession",
             SupportsShouldProcess = true,
             DefaultParameterSetName = CimSessionSet,
             HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227968")]
    public sealed class RemoveCimSessionCommand : CimBaseCommand
    {
        #region constructor

        
        public RemoveCimSessionCommand()
            : base(parameters, parameterSets)
        {
        }

        #endregion

        #region parameters

        
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimSessionSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public CimSession[] CimSession
        {
            get
            {
                return cimsession;
            }

            set
            {
                cimsession = value;
                base.SetParameter(value, nameCimSession);
            }
        }

        private CimSession[] cimsession;

        
        [Alias(AliasCN, AliasServerName)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = ComputerNameSet)]
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

        
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = SessionIdSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public uint[] Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
                base.SetParameter(value, nameId);
            }
        }

        private uint[] id;

        
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = InstanceIdSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Guid[] InstanceId
        {
            get
            {
                return instanceid;
            }

            set
            {
                instanceid = value;
                base.SetParameter(value, nameInstanceId);
            }
        }

        private Guid[] instanceid;

        
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = NameSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
                base.SetParameter(value, nameName);
            }
        }

        private string[] name;

        #endregion

        
        protected override void BeginProcessing()
        {
            this.cimRemoveSession = new CimRemoveSession();
            this.AtBeginProcess = false;
        }

        
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            this.cimRemoveSession.RemoveCimSession(this);
        }

        #region private members
        
        private CimRemoveSession cimRemoveSession;

        #region const string of parameter names
        internal const string nameCimSession = "CimSession";
        internal const string nameComputerName = "ComputerName";
        internal const string nameId = "Id";
        internal const string nameInstanceId = "InstanceId";
        internal const string nameName = "Name";
        #endregion

        
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimSessionSet, true),
                                 }
            },
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ComputerNameSet, true),
                                 }
            },
            {
                nameId, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.SessionIdSet, true),
                                 }
            },
            {
                nameInstanceId, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.InstanceIdSet, true),
                                 }
            },
            {
                nameName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.NameSet, true),
                                 }
            },
        };

        
        private static readonly Dictionary<string, ParameterSetEntry> parameterSets = new()
        {
            {   CimBaseCommand.CimSessionSet, new ParameterSetEntry(1, true)     },
            {   CimBaseCommand.ComputerNameSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.SessionIdSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.InstanceIdSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.NameSet, new ParameterSetEntry(1)     },
        };
        #endregion
    }
}
