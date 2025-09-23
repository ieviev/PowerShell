// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Management.Automation;

namespace Microsoft.PowerShell
{
    
    [RunInstaller(true)]
    public sealed class PSUtilityPSSnapIn : PSSnapIn
    {
        
        public PSUtilityPSSnapIn()
            : base()
        {
        }

        
        public override string Name
        {
            get
            {
                return RegistryStrings.UtilityMshSnapinName;
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
                return "UtilityMshSnapInResources,Vendor";
            }
        }

        
        public override string Description
        {
            get
            {
                return "This PSSnapIn contains utility cmdlets used to manipulate data.";
            }
        }

        
        public override string DescriptionResource
        {
            get
            {
                return "UtilityMshSnapInResources,Description";
            }
        }
    }
}
