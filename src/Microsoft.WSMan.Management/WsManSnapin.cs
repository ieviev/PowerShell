// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.WSMan.Management
{
    #region SnapIn

    
    [RunInstaller(true)]
    public class WSManPSSnapIn : PSSnapIn
    {
        
        public WSManPSSnapIn()
            : base()
        {
        }

        
        public override string Name
        {
            get
            {
                return "WsManPSSnapIn";
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
                return "WsManPSSnapIn,Microsoft";
            }
        }

        
        public override string Description
        {
            get
            {
                return "This is a PowerShell snap-in that includes the WsMan cmdlets.";
            }
        }

        
        public override string DescriptionResource
        {
            get
            {
                return "WsManPSSnapIn,This is a PowerShell snap-in that includes the WsMan cmdlets.";
            }
        }
    }

    #endregion SnapIn
}
