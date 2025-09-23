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
    
    [Alias("gcms")]
    [Cmdlet(VerbsCommon.Get, "CimSession", DefaultParameterSetName = ComputerNameSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227966")]
    [OutputType(typeof(CimSession))]
    public sealed class GetCimSessionCommand : CimBaseCommand
    {
        #region constructor

        
        public GetCimSessionCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters

        
        [Alias(AliasCN, AliasServerName)]
        [Parameter(Position = 0,
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

        
        [Parameter(Mandatory = true,
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

        
        [Parameter(Mandatory = true,
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

        #region cmdlet processing methods
        
        protected override void BeginProcessing()
        {
            cimGetSession = new CimGetSession();
            this.AtBeginProcess = false;
        }

        
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            cimGetSession.GetCimSession(this);
        }

        #endregion

        #region private members
        
        private CimGetSession cimGetSession;

        #region const string of parameter names
        internal const string nameComputerName = "ComputerName";
        internal const string nameId = "Id";
        internal const string nameInstanceId = "InstanceId";
        internal const string nameName = "Name";
        #endregion

        
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ComputerNameSet, false),
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
            {   CimBaseCommand.ComputerNameSet, new ParameterSetEntry(0, true)     },
            {   CimBaseCommand.SessionIdSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.InstanceIdSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.NameSet, new ParameterSetEntry(1)     },
        };
        #endregion
    }
}
