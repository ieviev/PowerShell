// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management.Automation;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    
    [RunInstaller(true)]
    public class GetEventPSSnapIn : PSSnapIn
    {
        
        public GetEventPSSnapIn()
               : base()
        {
        }

        
        public override string Name
        {
            get
            {
                return "Microsoft.Powershell.GetEvent";
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
                return "GetEventResources,Vendor";
            }
        }

        
        public override string Description
        {
            get
            {
                return "This PS snap-in contains Get-WinEvent cmdlet used to read Windows event log data and configuration.";
            }
        }

        
        public override string DescriptionResource
        {
            get
            {
                return "GetEventResources,Description";
            }
        }

        
        public override string[] Types
        {
            get
            {
                return _types;
            }
        }

        private string[] _types = new string[] { "getevent.types.ps1xml" };

        
        public override string[] Formats
        {
            get
            {
                return _formats;
            }
        }

        private string[] _formats = new string[] { "Event.format.ps1xml" };
    }
}
