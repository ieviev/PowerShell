// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation.Internal
{
    
    public sealed class CommonParameters
    {
        #region ctor

        
        internal CommonParameters(MshCommandRuntime commandRuntime)
        {
            if (commandRuntime == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandRuntime));
            }

            _commandRuntime = commandRuntime;
        }

        #endregion ctor

        #region parameters

        
        [Parameter]
        [Alias("vb")]
        public SwitchParameter Verbose
        {
            get { return _commandRuntime.Verbose; }

            set { _commandRuntime.Verbose = value; }
        }

        
        [Parameter]
        [Alias("db")]
        public SwitchParameter Debug
        {
            get { return _commandRuntime.Debug; }

            set { _commandRuntime.Debug = value; }
        }

        
        [Parameter]
        [Alias("ea")]
        public ActionPreference ErrorAction
        {
            get { return _commandRuntime.ErrorAction; }

            set { _commandRuntime.ErrorAction = value; }
        }

        
        [Parameter]
        [Alias("wa")]
        public ActionPreference WarningAction
        {
            get { return _commandRuntime.WarningPreference; }

            set { _commandRuntime.WarningPreference = value; }
        }

        
        [Parameter]
        [Alias("infa")]
        public ActionPreference InformationAction
        {
            get { return _commandRuntime.InformationPreference; }

            set { _commandRuntime.InformationPreference = value; }
        }

        
        [Parameter]
        [Alias("proga")]
        public ActionPreference ProgressAction
        {
            get { return _commandRuntime.ProgressPreference; }

            set { _commandRuntime.ProgressPreference = value; }
        }

        
        [Parameter]
        [Alias("ev")]
        [ValidateVariableName]
        public string ErrorVariable
        {
            get { return _commandRuntime.ErrorVariable; }

            set { _commandRuntime.ErrorVariable = value; }
        }

        
        [Parameter]
        [Alias("wv")]
        [ValidateVariableName]
        public string WarningVariable
        {
            get { return _commandRuntime.WarningVariable; }

            set { _commandRuntime.WarningVariable = value; }
        }

        
        [Parameter]
        [Alias("iv")]
        [ValidateVariableName]
        public string InformationVariable
        {
            get { return _commandRuntime.InformationVariable; }

            set { _commandRuntime.InformationVariable = value; }
        }

        
        [Parameter]
        [Alias("ov")]
        [ValidateVariableName]
        public string OutVariable
        {
            get { return _commandRuntime.OutVariable; }

            set { _commandRuntime.OutVariable = value; }
        }

        
        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        [Alias("ob")]
        public int OutBuffer
        {
            get { return _commandRuntime.OutBuffer; }

            set { _commandRuntime.OutBuffer = value; }
        }

        
        [Parameter]
        [Alias("pv")]
        [ValidateVariableName]
        public string PipelineVariable
        {
            get { return _commandRuntime.PipelineVariable; }

            set { _commandRuntime.PipelineVariable = value; }
        }

        #endregion parameters

        private readonly MshCommandRuntime _commandRuntime;

        internal class ValidateVariableName : ValidateArgumentsAttribute
        {
            protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
            {
                string varName = arguments as string;
                if (varName != null)
                {
                    if (varName.StartsWith('+'))
                    {
                        varName = varName.Substring(1);
                    }

                    VariablePath silp = new VariablePath(varName);
                    if (!silp.IsVariable)
                    {
                        throw new ValidationMetadataException(
                            "ArgumentNotValidVariableName",
                            null,
                            Metadata.ValidateVariableName, varName);
                    }
                }
            }
        }
    }
}
