// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerShell.Cmdletization
{
    
    [Flags]
    public enum MethodParameterBindings
    {
        
        In = 1,

        
        Out = 2,

        
        Error = 4,
    }

    
    public sealed class MethodParameter
    {
        
        public string Name { get; set; }

        
        public Type ParameterType { get; set; }

        
        public string ParameterTypeName { get; set; }

        
        public MethodParameterBindings Bindings { get; set; }

        
        public object Value { get; set; }

        
        public bool IsValuePresent { get; set; }
        // TODO/FIXME: this should be renamed to ValueExplicitlySpecified or something like this
    }
}
