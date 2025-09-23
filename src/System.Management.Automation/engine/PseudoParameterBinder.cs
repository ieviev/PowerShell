// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    
    internal class RuntimeDefinedParameterBinder : ParameterBinderBase
    {
        #region ctor

        
        internal RuntimeDefinedParameterBinder(
            RuntimeDefinedParameterDictionary target,
            InternalCommand command,
            CommandLineParameters commandLineParameters)
            : base(target, command.MyInvocation, command.Context, command)
        {
            // NTRAID#Windows Out Of Band Releases-927103-2006/01/25-JonN
            foreach (var pair in target)
            {
                string key = pair.Key;
                RuntimeDefinedParameter pp = pair.Value;
                string ppName = pp?.Name;
                if (pp == null || key != ppName)
                {
                    ParameterBindingException bindingException =
                        new ParameterBindingException(
                            ErrorCategory.InvalidArgument,
                            command.MyInvocation,
                            null,
                            ppName,
                            null,
                            null,
                            ParameterBinderStrings.RuntimeDefinedParameterNameMismatch,
                            "RuntimeDefinedParameterNameMismatch",
                            key);

                    throw bindingException;
                }
            }

            this.CommandLineParameters = commandLineParameters;
        }

        #endregion ctor

        #region internal members

        
        internal new RuntimeDefinedParameterDictionary Target
        {
            get
            {
                return base.Target as RuntimeDefinedParameterDictionary;
            }

            set
            {
                base.Target = value;
            }
        }

        #region Parameter default values

        
        internal override object GetDefaultParameterValue(string name)
        {
            object result = null;
            RuntimeDefinedParameter parameter;
            if (this.Target.TryGetValue(name, out parameter) && parameter != null)
            {
                result = parameter.Value;
            }

            return result;
        }

        #endregion Parameter default values

        
        internal override void BindParameter(string name, object value, CompiledCommandParameter parameterMetadata)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            Target[name].Value = value;
            this.CommandLineParameters.Add(name, value);
        }

        #endregion Parameter binding
    }
}
