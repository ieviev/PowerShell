// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    internal abstract class ProviderNames
    {
        
        internal abstract string Environment { get; }

        
        internal abstract string Certificate { get; }

        
        internal abstract string Variable { get; }

        
        internal abstract string Alias { get; }

        
        internal abstract string Function { get; }

        
        internal abstract string FileSystem { get; }

        
        internal abstract string Registry { get; }
    }

    
    internal class SingleShellProviderNames : ProviderNames
    {
        
        internal override string Environment
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Environment";
            }
        }

        
        internal override string Certificate
        {
            get
            {
                return "Microsoft.PowerShell.Security\\Certificate";
            }
        }

        
        internal override string Variable
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Variable";
            }
        }

        
        internal override string Alias
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Alias";
            }
        }

        
        internal override string Function
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Function";
            }
        }

        
        internal override string FileSystem
        {
            get
            {
                return "Microsoft.PowerShell.Core\\FileSystem";
            }
        }

        
        internal override string Registry
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Registry";
            }
        }
    }
}
