// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    
    public class ShowCommandParameterSetInfo
    {
        
        public ShowCommandParameterSetInfo(CommandParameterSetInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Name;
            this.IsDefault = other.IsDefault;
            this.Parameters = other.Parameters.Select(static x => new ShowCommandParameterInfo(x)).ToArray();
        }

        
        public ShowCommandParameterSetInfo(PSObject other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Members["Name"].Value as string;
            this.IsDefault = (bool)(other.Members["IsDefault"].Value);
            var parameters = (other.Members["Parameters"].Value as PSObject).BaseObject as System.Collections.ArrayList;
            this.Parameters = ShowCommandCommandInfo.GetObjectEnumerable(parameters).Cast<PSObject>().Select(static x => new ShowCommandParameterInfo(x)).ToArray();
        }

        
        public string Name { get; }

        
        public bool IsDefault { get; }

        
        public ICollection<ShowCommandParameterInfo> Parameters { get; }
    }
}
