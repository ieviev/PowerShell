// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    public class WebCmdletElementCollection : ReadOnlyCollection<PSObject>
    {
        internal WebCmdletElementCollection(IList<PSObject> list) : base(list)
        {
        }

        
        /// <param name="nameOrId"></param>
        /// <returns>Found element as PSObject.</returns>
        public PSObject? Find(string nameOrId) => FindById(nameOrId) ?? FindByName(nameOrId);

        
        /// <param name="id"></param>
        /// <returns>Found element as PSObject.</returns>
        public PSObject? FindById(string id) => Find(id, findById: true);

        
        /// <param name="name"></param>
        /// <returns>Found element as PSObject.</returns>
        public PSObject? FindByName(string name) => Find(name, findById: false);

        private PSObject? Find(string nameOrId, bool findById)
        {
            foreach (PSObject candidate in this)
            {
                var namePropInfo = candidate.Properties[(findById ? "id" : "name")];
                if (namePropInfo != null && (string)namePropInfo.Value == nameOrId)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
