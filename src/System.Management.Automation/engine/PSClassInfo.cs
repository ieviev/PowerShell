// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    
    public sealed class PSClassInfo
    {
        
        internal PSClassInfo(string name)
        {
            this.Name = name;
        }

        
        public string Name { get; }

        
        public ReadOnlyCollection<PSClassMemberInfo> Members { get; private set; }

        
        public void UpdateMembers(IList<PSClassMemberInfo> members)
        {
            if (members != null)
                this.Members = new ReadOnlyCollection<PSClassMemberInfo>(members);
        }

        
        public PSModuleInfo Module { get; internal set; }

        
        public string HelpFile { get; internal set; } = string.Empty;
    }

    
    public sealed class PSClassMemberInfo
    {
        
        internal PSClassMemberInfo(string name, string memberType, string defaultValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            this.Name = name;
            this.TypeName = memberType;
            this.DefaultValue = defaultValue;
        }

        
        public string Name { get; }

        
        public string TypeName { get; }

        
        public string DefaultValue { get; }
    }
}
