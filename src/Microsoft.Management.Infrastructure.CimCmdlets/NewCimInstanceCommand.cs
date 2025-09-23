// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    [Alias("ncim")]
    [Cmdlet(VerbsCommon.New, "CimInstance", DefaultParameterSetName = CimBaseCommand.ClassNameComputerSet, SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227963")]
    [OutputType(typeof(CimInstance))]
    public class NewCimInstanceCommand : CimBaseCommand
    {
        #region constructor

        
        public NewCimInstanceCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters

        
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        public string ClassName
        {
            get
            {
                return className;
            }

            set
            {
                className = value;
                base.SetParameter(value, nameClassName);
            }
        }

        private string className;

        
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
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

        
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Key
        {
            get
            {
                return key;
            }

            set
            {
                key = value;
                base.SetParameter(value, nameKey);
            }
        }

        private string[] key;

        
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimClassSessionSet)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimClassComputerSet)]
        public CimClass CimClass
        {
            get
            {
                return cimClass;
            }

            set
            {
                cimClass = value;
                base.SetParameter(value, nameCimClass);
            }
        }

        private CimClass cimClass;

        
        [Parameter(
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("Arguments")]
        public IDictionary Property { get; set; }

        
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
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
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimClassSessionSet)]
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
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimClassComputerSet)]
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

        
        [Alias("Local")]
        [Parameter(
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimClassComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimClassSessionSet)]
        public SwitchParameter ClientOnly
        {
            get
            {
                return clientOnly;
            }

            set
            {
                clientOnly = value;
                base.SetParameter(value, nameClientOnly);
            }
        }

        private SwitchParameter clientOnly;

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
            this.CheckArgument();
            if (this.ClientOnly)
            {
                string conflictParameterName = null;
                if (this.ComputerName != null)
                {
                    conflictParameterName = @"ComputerName";
                }
                else if (this.CimSession != null)
                {
                    conflictParameterName = @"CimSession";
                }

                if (conflictParameterName != null)
                {
                    ThrowConflictParameterWasSet(@"New-CimInstance", conflictParameterName, @"ClientOnly");
                    return;
                }
            }

            CimNewCimInstance cimNewCimInstance = this.GetOperationAgent() ?? CreateOperationAgent();

            cimNewCimInstance.NewCimInstance(this);
            cimNewCimInstance.ProcessActions(this.CmdletOperation);
        }

        
        protected override void EndProcessing()
        {
            CimNewCimInstance cimNewCimInstance = this.GetOperationAgent();
            cimNewCimInstance?.ProcessRemainActions(this.CmdletOperation);
        }

        #endregion

        #region helper methods

        
        private CimNewCimInstance GetOperationAgent()
        {
            return this.AsyncOperation as CimNewCimInstance;
        }

        
        /// <returns></returns>
        private CimNewCimInstance CreateOperationAgent()
        {
            CimNewCimInstance cimNewCimInstance = new();
            this.AsyncOperation = cimNewCimInstance;
            return cimNewCimInstance;
        }

        
        private void CheckArgument()
        {
            switch (this.ParameterSetName)
            {
                case CimBaseCommand.ClassNameComputerSet:
                case CimBaseCommand.ClassNameSessionSet:
                    // validate the classname
                    this.className = ValidationHelper.ValidateArgumentIsValidName(nameClassName, this.className);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region private members

        #region const string of parameter names
        internal const string nameClassName = "ClassName";
        internal const string nameResourceUri = "ResourceUri";
        internal const string nameKey = "Key";
        internal const string nameCimClass = "CimClass";
        internal const string nameProperty = "Property";
        internal const string nameNamespace = "Namespace";
        internal const string nameCimSession = "CimSession";
        internal const string nameComputerName = "ComputerName";
        internal const string nameClientOnly = "ClientOnly";
        #endregion

        
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameClassName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, true),
                                 }
            },
            {
                nameResourceUri, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, true),
                                 }
            },
            {
                nameKey, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                 }
            },
            {
                nameCimClass, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassComputerSet, true),
                                 }
            },
            {
                nameNamespace, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                 }
            },
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassSessionSet, true),
                                 }
            },
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassComputerSet, false),
                                 }
            },
            {
                nameClientOnly, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassComputerSet, true),
                                 }
            },
        };

        
        private static readonly Dictionary<string, ParameterSetEntry> parameterSets = new()
        {
            {   CimBaseCommand.ClassNameSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.ClassNameComputerSet, new ParameterSetEntry(1, true)     },
            {   CimBaseCommand.CimClassSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.CimClassComputerSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.ResourceUriSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.ResourceUriComputerSet, new ParameterSetEntry(1)     },
        };
        #endregion
    }
}
