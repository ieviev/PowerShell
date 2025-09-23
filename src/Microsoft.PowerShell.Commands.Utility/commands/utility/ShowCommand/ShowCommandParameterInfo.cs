// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    
    public class ShowCommandParameterInfo
    {
        
        public ShowCommandParameterInfo(CommandParameterInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Name;
            this.IsMandatory = other.IsMandatory;
            this.ValueFromPipeline = other.ValueFromPipeline;
            this.ParameterType = new ShowCommandParameterType(other.ParameterType);
            this.Position = other.Position;

            var validateSetAttribute = other.Attributes.Where(static x => typeof(ValidateSetAttribute).IsAssignableFrom(x.GetType())).Cast<ValidateSetAttribute>().LastOrDefault();
            if (validateSetAttribute != null)
            {
                this.HasParameterSet = true;
                this.ValidParamSetValues = validateSetAttribute.ValidValues;
            }
        }

        
        public ShowCommandParameterInfo(PSObject other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Members["Name"].Value as string;
            this.IsMandatory = (bool)(other.Members["IsMandatory"].Value);
            this.ValueFromPipeline = (bool)(other.Members["ValueFromPipeline"].Value);
            this.HasParameterSet = (bool)(other.Members["HasParameterSet"].Value);
            this.ParameterType = new ShowCommandParameterType(other.Members["ParameterType"].Value as PSObject);
            this.Position = (int)(other.Members["Position"].Value);
            if (this.HasParameterSet)
            {
                this.ValidParamSetValues = ShowCommandCommandInfo.GetObjectEnumerable((other.Members["ValidParamSetValues"].Value as PSObject).BaseObject as System.Collections.ArrayList).Cast<string>().ToList();
            }
        }

        
        public string Name { get; }

        public bool IsMandatory { get; }

        
        public bool ValueFromPipeline { get; }

        
        public ShowCommandParameterType ParameterType { get; }

        
        public IList<string> ValidParamSetValues { get; }

        
        public bool HasParameterSet { get; }

        
        public int Position { get; }
    }
}
