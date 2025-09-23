// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Management.Automation;

namespace Microsoft.PowerShell
{
    
    [RunInstaller(true)]
    public sealed class PSManagementPSSnapIn : PSSnapIn
    {
        
        public PSManagementPSSnapIn()
            : base()
        {
        }

        
        public override string Name
        {
            get
            {
                return RegistryStrings.ManagementMshSnapinName;
            }
        }

        
        public override string Vendor
        {
            get
            {
                return "Microsoft";
            }
        }

        
        public override string VendorResource
        {
            get
            {
                return "ManagementMshSnapInResources,Vendor";
            }
        }

        
        public override string Description
        {
            get
            {
                return "This PSSnapIn contains general management cmdlets used to manage Windows components.";
            }
        }

        
        public override string DescriptionResource
        {
            get
            {
                return "ManagementMshSnapInResources,Description";
            }
        }
    }
}
