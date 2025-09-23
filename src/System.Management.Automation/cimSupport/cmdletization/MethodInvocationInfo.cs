// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.PowerShell.Cmdletization
{
    
    public sealed class MethodInvocationInfo
    {
        
        /// <param name="name">Name of the method to invoke.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <param name="returnValue">Return value of the method (ok to pass <see langword="null"/> if the method doesn't return anything).</param>
        public MethodInvocationInfo(string name, IEnumerable<MethodParameter> parameters, MethodParameter returnValue)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(parameters);
            // returnValue can be null

            MethodName = name;
            ReturnValue = returnValue;

            KeyedCollection<string, MethodParameter> mpk = new MethodParametersCollection();
            foreach (var parameter in parameters)
            {
                mpk.Add(parameter);
            }

            Parameters = mpk;
        }

        
        public string MethodName { get; }

        
        public KeyedCollection<string, MethodParameter> Parameters { get; }

        
        public MethodParameter ReturnValue { get; }

        internal IEnumerable<T> GetArgumentsOfType<T>() where T : class
        {
            List<T> result = new();
            foreach (var methodParameter in this.Parameters)
            {
                if ((methodParameter.Bindings & MethodParameterBindings.In) != MethodParameterBindings.In)
                {
                    continue;
                }

                if (methodParameter.Value is T objectInstance)
                {
                    result.Add(objectInstance);
                    continue;
                }

                if (methodParameter.Value is IEnumerable objectInstanceArray)
                {
                    foreach (object element in objectInstanceArray)
                    {
                        if (element is T objectInstance2)
                        {
                            result.Add(objectInstance2);
                        }
                    }

                    continue;
                }
            }

            return result;
        }
    }
}
