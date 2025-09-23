// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    
    public class ShowCommandParameterType
    {
        
        public ShowCommandParameterType(Type other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.FullName = other.FullName;
            if (other.IsEnum)
            {
                this.EnumValues = new ArrayList(Enum.GetValues(other));
            }

            if (other.IsArray)
            {
                this.ElementType = new ShowCommandParameterType(other.GetElementType());
            }

            object[] attributes = other.GetCustomAttributes(typeof(FlagsAttribute), true);
            this.HasFlagAttribute = attributes.Length != 0;
            this.ImplementsDictionary = typeof(IDictionary).IsAssignableFrom(other);
        }

        
        public ShowCommandParameterType(PSObject other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.IsEnum = (bool)(other.Members["IsEnum"].Value);
            this.FullName = other.Members["FullName"].Value as string;
            this.IsArray = (bool)(other.Members["IsArray"].Value);
            this.HasFlagAttribute = (bool)(other.Members["HasFlagAttribute"].Value);
            this.ImplementsDictionary = (bool)(other.Members["ImplementsDictionary"].Value);

            if (this.IsArray)
            {
                this.ElementType = new ShowCommandParameterType(other.Members["ElementType"].Value as PSObject);
            }

            if (this.IsEnum)
            {
                this.EnumValues = (other.Members["EnumValues"].Value as PSObject).BaseObject as ArrayList;
            }
        }

        
        public string FullName { get; }

        
        public bool IsEnum { get; }

        
        public bool ImplementsDictionary { get; }

        
        public bool HasFlagAttribute { get; }

        
        public bool IsArray { get; }

        
        public ShowCommandParameterType ElementType { get; }

        
        public bool IsString
        {
            get
            {
                return string.Equals(this.FullName, "System.String", StringComparison.OrdinalIgnoreCase);
            }
        }

        
        public bool IsScriptBlock
        {
            get
            {
                return string.Equals(this.FullName, "System.Management.Automation.ScriptBlock", StringComparison.OrdinalIgnoreCase);
            }
        }

        
        public bool IsBoolean
        {
            get
            {
                return string.Equals(this.FullName, "System.Management.Automation.ScriptBlock", StringComparison.OrdinalIgnoreCase);
            }
        }

        
        public bool IsSwitch
        {
            get
            {
                return string.Equals(this.FullName, "System.Management.Automation.SwitchParameter", StringComparison.OrdinalIgnoreCase);
            }
        }

        
        public ArrayList EnumValues { get; }
    }
}
