// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    public class ItemPropertyCommandBase : CoreCommandWithCredentialsBase
    {
        #region Parameters

        
        [Parameter]
        public override string Filter
        {
            get
            {
                return base.Filter;
            }

            set
            {
                base.Filter = value;
            }
        }

        
        [Parameter]
        public override string[] Include
        {
            get
            {
                return base.Include;
            }

            set
            {
                base.Include = value;
            }
        }

        
        [Parameter]
        public override string[] Exclude
        {
            get
            {
                return base.Exclude;
            }

            set
            {
                base.Exclude = value;
            }
        }
        #endregion Parameters

        #region parameter data

        
        internal string[] paths = Array.Empty<string>();

        #endregion parameter data
    }
}
