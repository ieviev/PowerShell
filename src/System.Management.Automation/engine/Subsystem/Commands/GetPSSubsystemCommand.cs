// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace System.Management.Automation.Subsystem
{
    
    [Experimental("PSSubsystemPluginModel", ExperimentAction.Show)]
    [Cmdlet(VerbsCommon.Get, "PSSubsystem", DefaultParameterSetName = AllSet)]
    [OutputType(typeof(SubsystemInfo))]
    public sealed class GetPSSubsystemCommand : PSCmdlet
    {
        private const string AllSet = "GetAllSet";
        private const string TypeSet = "GetByTypeSet";
        private const string KindSet = "GetByKindSet";

        
        [Parameter(Mandatory = true, ParameterSetName = KindSet, ValueFromPipeline = true)]
        public SubsystemKind Kind { get; set; }

        
        [Parameter(Mandatory = true, ParameterSetName = TypeSet, ValueFromPipeline = true)]
        public Type? SubsystemType { get; set; }

        
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case AllSet:
                    WriteObject(SubsystemManager.GetAllSubsystemInfo(), enumerateCollection: true);
                    break;
                case KindSet:
                    WriteObject(SubsystemManager.GetSubsystemInfo(Kind));
                    break;
                case TypeSet:
                    WriteObject(SubsystemManager.GetSubsystemInfo(SubsystemType!));
                    break;

                default:
                    throw new InvalidOperationException("New parameter set is added but the switch statement is not updated.");
            }
        }
    }
}
