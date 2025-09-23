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

        
        /// <param name="commandRuntime">
        /// The instance of the command that the parameters should set the
        /// user feedback properties on when the parameters get bound.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="cmdlet"/> is null.
        /// </exception>
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

        
        /// <remarks>
        /// This parameter
        /// tells the command to articulate the actions it performs while executing.
        /// </remarks>
        [Parameter]
        [Alias("vb")]
        public SwitchParameter Verbose
        {
            get { return _commandRuntime.Verbose; }

            set { _commandRuntime.Verbose = value; }
        }

        
        /// <remarks>
        /// This parameter tells the command to provide Programmer/Support type
        /// messages to understand what is really occurring and give the user the
        /// opportunity to stop or debug the situation.
        /// </remarks>
        [Parameter]
        [Alias("db")]
        public SwitchParameter Debug
        {
            get { return _commandRuntime.Debug; }

            set { _commandRuntime.Debug = value; }
        }

        
        /// <remarks>
        /// This parameter tells the command what to do when an error occurs.
        /// </remarks>
        [Parameter]
        [Alias("ea")]
        public ActionPreference ErrorAction
        {
            get { return _commandRuntime.ErrorAction; }

            set { _commandRuntime.ErrorAction = value; }
        }

        
        /// <remarks>
        /// This parameter tells the command what to do when a warning
        /// occurs.
        /// </remarks>
        [Parameter]
        [Alias("wa")]
        public ActionPreference WarningAction
        {
            get { return _commandRuntime.WarningPreference; }

            set { _commandRuntime.WarningPreference = value; }
        }

        
        /// <remarks>
        /// This parameter tells the command what to do when an informational record occurs.
        /// </remarks>
        /// 
        [Parameter]
        [Alias("infa")]
        public ActionPreference InformationAction
        {
            get { return _commandRuntime.InformationPreference; }

            set { _commandRuntime.InformationPreference = value; }
        }

        
        /// <remarks>
        /// This parameter tells the command what to do when a progress record occurs.
        /// </remarks>
        /// 
        [Parameter]
        [Alias("proga")]
        public ActionPreference ProgressAction
        {
            get { return _commandRuntime.ProgressPreference; }

            set { _commandRuntime.ProgressPreference = value; }
        }

        
        /// <remarks>
        /// This parameter tells the command which variable to populate with the errors.
        /// Use +varname to append to the variable rather than clearing it.
        /// </remarks>
        /// 
        [Parameter]
        [Alias("ev")]
        [ValidateVariableName]
        public string ErrorVariable
        {
            get { return _commandRuntime.ErrorVariable; }

            set { _commandRuntime.ErrorVariable = value; }
        }

        
        /// <remarks>
        /// This parameter tells the command which variable to populate with the warnings.
        /// Use +varname to append to the variable rather than clearing it.
        /// </remarks>
        [Parameter]
        [Alias("wv")]
        [ValidateVariableName]
        public string WarningVariable
        {
            get { return _commandRuntime.WarningVariable; }

            set { _commandRuntime.WarningVariable = value; }
        }

        
        /// <remarks>
        /// This parameter tells the command which variable to populate with the informational output.
        /// Use +varname to append to the variable rather than clearing it.
        /// </remarks>
        [Parameter]
        [Alias("iv")]
        [ValidateVariableName]
        public string InformationVariable
        {
            get { return _commandRuntime.InformationVariable; }

            set { _commandRuntime.InformationVariable = value; }
        }

        
        /// <remarks>
        /// This parameter tells the command to set all success output in the specified variable.
        /// Similar to the way -errorvariable sets all errors to a variable name.
        /// Semantically this is equivalent to :  command |set-var varname -passthru
        /// but it should be MUCH faster as there is no binding that takes place
        /// </remarks>
        [Parameter]
        [Alias("ov")]
        [ValidateVariableName]
        public string OutVariable
        {
            get { return _commandRuntime.OutVariable; }

            set { _commandRuntime.OutVariable = value; }
        }

        
        /// <remarks>
        /// This parameter configures the number of objects to buffer before calling the downstream Cmdlet
        /// </remarks>
        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        [Alias("ob")]
        public int OutBuffer
        {
            get { return _commandRuntime.OutBuffer; }

            set { _commandRuntime.OutBuffer = value; }
        }

        
        /// <remarks>
        /// This parameter defines a variable to hold the current pipeline output the command
        /// as it passes down the pipeline:
        /// Write-Output (1..10) -PipelineVariable WriteOutput | Foreach-Object { "Hello" }  |
        ///     Foreach-Object { $WriteOutput }
        /// </remarks>
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
