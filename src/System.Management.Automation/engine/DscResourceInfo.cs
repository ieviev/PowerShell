// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    
    public enum ImplementedAsType
    {
        
        None = 0,

        
        PowerShell = 1,

        
        Binary = 2,

        
        Composite = 3
    }

    
    public class DscResourceInfo
    {
        
        /// <param name="name">Name of the DscResource.</param>
        /// <param name="friendlyName">FriendlyName of the DscResource.</param>
        /// <param name="path">Path of the DscResource.</param>
        /// <param name="parentPath">ParentPath of the DscResource.</param>
        /// <param name="context">The execution context for the DscResource.</param>
        internal DscResourceInfo(string name, string friendlyName, string path, string parentPath, ExecutionContext context)
        {
            this.Name = name;
            this.FriendlyName = friendlyName;
            this.Path = path;
            this.ParentPath = parentPath;
            this.Properties = new ReadOnlyCollection<DscResourcePropertyInfo>(new List<DscResourcePropertyInfo>());
        }

        
        public string Name { get; }

        
        public string ResourceType { get; set; }

        
        public string FriendlyName { get; set; }

        
        public string Path { get; set; }

        
        public string ParentPath { get; set; }

        
        public ImplementedAsType ImplementedAs { get; set; }

        
        public string CompanyName { get; set; }

        
        public ReadOnlyCollection<DscResourcePropertyInfo> Properties { get; private set; }

        
        /// <param name="properties">Updated properties.</param>
        public void UpdateProperties(IList<DscResourcePropertyInfo> properties)
        {
            if (properties != null)
                this.Properties = new ReadOnlyCollection<DscResourcePropertyInfo>(properties);
        }

        
        public PSModuleInfo Module { get; internal set; }

        
        public string HelpFile { get; internal set; } = string.Empty;

        // HelpFile
    }

    
    public sealed class DscResourcePropertyInfo
    {
        
        internal DscResourcePropertyInfo()
        {
            this.Values = new ReadOnlyCollection<string>(new List<string>());
        }

        
        public string Name { get; set; }

        
        public string PropertyType { get; set; }

        
        public bool IsMandatory { get; set; }

        
        public ReadOnlyCollection<string> Values { get; private set; }

        internal void UpdateValues(IList<string> values)
        {
            if (values != null)
                this.Values = new ReadOnlyCollection<string>(values);
        }
    }
}
