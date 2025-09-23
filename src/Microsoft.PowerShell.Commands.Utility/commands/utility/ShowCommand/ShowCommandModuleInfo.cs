// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    
    public class ShowCommandModuleInfo
    {
        
        public ShowCommandModuleInfo(PSModuleInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Name;
        }

        
        public ShowCommandModuleInfo(PSObject other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Members["Name"].Value as string;
        }

        
        public string Name { get; }
    }
}
