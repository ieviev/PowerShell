// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;

namespace Microsoft.WSMan.Management
{
    
    [Cmdlet(VerbsCommon.New, "WSManSessionOption", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096845")]
    [OutputType(typeof(SessionOption))]
    public class NewWSManSessionOptionCommand : PSCmdlet
    {
        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public ProxyAccessType ProxyAccessType
        {
            get
            {
                return _proxyaccesstype;
            }

            set
            {
                _proxyaccesstype = value;
            }
        }

        private ProxyAccessType _proxyaccesstype;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public ProxyAuthentication ProxyAuthentication
        {
            get
            {
                return proxyauthentication;
            }

            set
            {
                proxyauthentication = value;
            }
        }

        private ProxyAuthentication proxyauthentication;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential ProxyCredential
        {
            get
            {
                return _proxycredential;
            }

            set
            {
                _proxycredential = value;
            }
        }

        private PSCredential _proxycredential;

        
        [Parameter]
        public SwitchParameter SkipCACheck
        {
            get
            {
                return skipcacheck;
            }

            set
            {
                skipcacheck = value;
            }
        }

        private bool skipcacheck;

        
        [Parameter]
        public SwitchParameter SkipCNCheck
        {
            get
            {
                return skipcncheck;
            }

            set
            {
                skipcncheck = value;
            }
        }

        private bool skipcncheck;

        
        [Parameter]
        public SwitchParameter SkipRevocationCheck
        {
            get
            {
                return skiprevocationcheck;
            }

            set
            {
                skiprevocationcheck = value;
            }
        }

        private bool skiprevocationcheck;

        
        [Parameter]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        [ValidateRange(0, int.MaxValue)]
        public int SPNPort
        {
            get
            {
                return spnport;
            }

            set
            {
                spnport = value;
            }
        }

        private int spnport;

        
        [Parameter]
        [Alias("OperationTimeoutMSec")]
        [ValidateRange(0, int.MaxValue)]
        public int OperationTimeout
        {
            get
            {
                return operationtimeout;
            }

            set
            {
                operationtimeout = value;
            }
        }

        private int operationtimeout;

        
        [Parameter]
        public SwitchParameter NoEncryption
        {
            get
            {
                return noencryption;
            }

            set
            {
                noencryption = value;
            }
        }

        private bool noencryption;

        
        [Parameter]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "UTF")]
        public SwitchParameter UseUTF16
        {
            get
            {
                return useutf16;
            }

            set
            {
                useutf16 = value;
            }
        }

        private bool useutf16;

        
        protected override void BeginProcessing()
        {
            WSManHelper helper = new WSManHelper(this);

            if (proxyauthentication.Equals(ProxyAuthentication.Basic) || proxyauthentication.Equals(ProxyAuthentication.Digest))
            {
                if (_proxycredential == null)
                {
                    InvalidOperationException ex = new InvalidOperationException(helper.GetResourceMsgFromResourcetext("NewWSManSessionOptionCred"));
                    ErrorRecord er = new ErrorRecord(ex, "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }
            }

            if ((_proxycredential != null) && (proxyauthentication == 0))
            {
                InvalidOperationException ex = new InvalidOperationException(helper.GetResourceMsgFromResourcetext("NewWSManSessionOptionAuth"));
                ErrorRecord er = new ErrorRecord(ex, "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                WriteError(er);
                return;
            }

            // Creating the Session Object
            SessionOption objSessionOption = new SessionOption();

            objSessionOption.SPNPort = spnport;
            objSessionOption.UseUtf16 = useutf16;
            objSessionOption.SkipCNCheck = skipcncheck;
            objSessionOption.SkipCACheck = skipcacheck;
            objSessionOption.OperationTimeout = operationtimeout;
            objSessionOption.SkipRevocationCheck = skiprevocationcheck;
            // Proxy Settings
            objSessionOption.ProxyAccessType = _proxyaccesstype;
            objSessionOption.ProxyAuthentication = proxyauthentication;

            if (noencryption)
            {
                objSessionOption.UseEncryption = false;
            }

            if (_proxycredential != null)
            {
                NetworkCredential nwCredentials = _proxycredential.GetNetworkCredential();
                objSessionOption.ProxyCredential = nwCredentials;
            }

            WriteObject(objSessionOption);
        }
    }
}
