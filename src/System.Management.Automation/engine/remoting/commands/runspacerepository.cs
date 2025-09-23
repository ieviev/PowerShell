// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    
    public class RunspaceRepository : Repository<PSSession>
    {
        #region Public Methods

        
        public List<PSSession> Runspaces
        {
            get
            {
                return Items;
            }
        }

        #endregion Public Methods

        #region Internal Methods

        
        internal RunspaceRepository() : base("runspace")
        {
        }

        
        /// <param name="item"></param>
        /// <returns></returns>
        protected override Guid GetKey(PSSession item)
        {
            if (item != null)
            {
                return item.InstanceId;
            }

            return Guid.Empty;
        }

        
        /// <param name="item">PSSession object.</param>
        internal void AddOrReplace(PSSession item)
        {
            this.Dictionary[GetKey(item)] = item;
        }

        #endregion Private Methods
    }
}
