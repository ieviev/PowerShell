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
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;

using Dbg = System.Management.Automation;

namespace Microsoft.WSMan.Management
{
    #region Base class for cmdlets taking credential, authentication, certificatethumbprint

    
    public class AuthenticatingWSManCommand : PSCmdlet
    {
        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Credential]
        [Alias("cred", "c")]
        public virtual PSCredential Credential
        {
            get
            {
                return credential;
            }

            set
            {
                credential = value;
                ValidateSpecifiedAuthentication();
            }
        }

        private PSCredential credential;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("auth", "am")]
        public virtual AuthenticationMechanism Authentication
        {
            get
            {
                return authentication;
            }

            set
            {
                authentication = value;
                ValidateSpecifiedAuthentication();
            }
        }

        private AuthenticationMechanism authentication = AuthenticationMechanism.Default;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public virtual string CertificateThumbprint
        {
            get
            {
                return thumbPrint;
            }

            set
            {
                thumbPrint = value;
                ValidateSpecifiedAuthentication();
            }
        }

        private string thumbPrint = null;

        internal void ValidateSpecifiedAuthentication()
        {
            WSManHelper.ValidateSpecifiedAuthentication(
                this.Authentication,
                this.Credential,
                this.CertificateThumbprint);
        }
    }

    #endregion

    #region Connect-WsMan
    
    [Cmdlet(VerbsCommunications.Connect, "WSMan", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096841")]
    public class ConnectWSManCommand : AuthenticatingWSManCommand
    {
        #region Parameters

        
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        public string ApplicationName
        {
            get { return applicationname; }

            set { applicationname = value; }
        }

        private string applicationname = null;

        
        [Parameter(ParameterSetName = "ComputerName", Position = 0)]
        [Alias("cn")]
        public string ComputerName
        {
            get
            {
                return computername;
            }

            set
            {
                computername = value;
                if ((string.IsNullOrEmpty(computername)) || (computername.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computername = "localhost";
                }
            }
        }

        private string computername = null;

        
        [Parameter(ParameterSetName = "URI")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        public Uri ConnectionURI
        {
            get { return connectionuri; }

            set { connectionuri = value; }
        }

        private Uri connectionuri;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("os")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable OptionSet
        {
            get { return optionset; }

            set { optionset = value; }
        }

        private Hashtable optionset;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateRange(1, int.MaxValue)]
        public int Port
        {
            get { return port; }

            set { port = value; }
        }

        private int port = 0;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("so")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public SessionOption SessionOption
        {
            get { return sessionoption; }

            set { sessionoption = value; }
        }

        private SessionOption sessionoption;

        
        [Parameter(ParameterSetName = "ComputerName")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public SwitchParameter UseSSL
        {
            get { return usessl; }

            set { usessl = value; }
        }

        private SwitchParameter usessl;

        #endregion

        
        protected override void BeginProcessing()
        {
            WSManHelper helper = new WSManHelper(this);
            if (connectionuri != null)
            {
                try
                {
                    // always in the format http://server:port/applicationname
                    string[] constrsplit = connectionuri.OriginalString.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                    string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                    computername = constrsplit1[1].Trim();
                }
                catch (IndexOutOfRangeException)
                {
                    helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                }
            }

            string crtComputerName = computername ?? "localhost";

            if (this.SessionState.Path.CurrentProviderLocation(WSManStringLiterals.rootpath).Path.StartsWith(this.SessionState.Drive.Current.Name + ":" + WSManStringLiterals.DefaultPathSeparator + crtComputerName, StringComparison.OrdinalIgnoreCase))
            {
                helper.AssertError(helper.GetResourceMsgFromResourcetext("ConnectFailure"), false, computername);
            }

            helper.CreateWsManConnection(ParameterSetName, connectionuri, port, computername, applicationname, usessl.IsPresent, Authentication, sessionoption, Credential, CertificateThumbprint);
        }
    }
    #endregion

    #region Disconnect-WSMAN
    
    [Cmdlet(VerbsCommunications.Disconnect, "WSMan", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096839")]
    public class DisconnectWSManCommand : PSCmdlet, IDisposable
    {
        
        [Parameter(Position = 0)]
        public string ComputerName
        {
            get
            {
                return computername;
            }

            set
            {
                computername = value;
                if ((string.IsNullOrEmpty(computername)) || (computername.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computername = "localhost";
                }
            }
        }

        private string computername = null;

        #region IDisposable Members

        
        public
        void
        Dispose()
        {
            // CleanUp();
            GC.SuppressFinalize(this);
        }
        
        public
        void
        Dispose(object session)
        {
            session = null;
            this.Dispose();
        }

        #endregion IDisposable Members

        
        protected override void BeginProcessing()
        {
            WSManHelper helper = new WSManHelper(this);
            computername ??= "localhost";

            if (this.SessionState.Path.CurrentProviderLocation(WSManStringLiterals.rootpath).Path.StartsWith(WSManStringLiterals.rootpath + ":" + WSManStringLiterals.DefaultPathSeparator + computername, StringComparison.OrdinalIgnoreCase))
            {
                helper.AssertError(helper.GetResourceMsgFromResourcetext("DisconnectFailure"), false, computername);
            }

            if (computername.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                helper.AssertError(helper.GetResourceMsgFromResourcetext("LocalHost"), false, computername);
            }

            object _ws = helper.RemoveFromDictionary(computername);
            if (_ws != null)
            {
                Dispose(_ws);
            }
            else
            {
                helper.AssertError(helper.GetResourceMsgFromResourcetext("InvalidComputerName"), false, computername);
            }
        }
    }
    #endregion Disconnect-WSMAN
}
