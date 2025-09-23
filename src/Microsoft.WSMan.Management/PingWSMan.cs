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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;

namespace Microsoft.WSMan.Management
{
    #region Test-WSMAN

    
    [Cmdlet(VerbsDiagnostic.Test, "WSMan", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2097114")]
    [OutputType(typeof(XmlElement))]
    public class TestWSManCommand : AuthenticatingWSManCommand, IDisposable
    {
        
        [Parameter(Position = 0, ValueFromPipeline = true)]
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

        
        /// <remarks>
        /// Overriding to use a different default than the one in AuthenticatingWSManCommand base class
        /// </remarks>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("auth", "am")]
        public override AuthenticationMechanism Authentication
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

        private AuthenticationMechanism authentication = AuthenticationMechanism.None;

        
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(1, int.MaxValue)]
        public int Port
        {
            get { return port; }

            set { port = value; }
        }

        private int port = 0;

        
        [Parameter(ParameterSetName = "ComputerName")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public SwitchParameter UseSSL
        {
            get { return usessl; }

            set { usessl = value; }
        }

        private SwitchParameter usessl;

        
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        public string ApplicationName
        {
            get { return applicationname; }

            set { applicationname = value; }
        }

        private string applicationname = null;

        
        protected override void ProcessRecord()
        {
            WSManHelper helper = new WSManHelper(this);
            IWSManEx wsmanObject = (IWSManEx)new WSManClass();
            string connectionStr = string.Empty;
            connectionStr = helper.CreateConnectionString(null, port, computername, applicationname);
            IWSManSession m_SessionObj = null;
            try
            {
                m_SessionObj = helper.CreateSessionObject(wsmanObject, Authentication, null, Credential, connectionStr, CertificateThumbprint, usessl.IsPresent);
                m_SessionObj.Timeout = 1000; // 1 sec. we are putting this low so that Test-WSMan can return promptly if the server goes unresponsive.
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.LoadXml(m_SessionObj.Identify(0));
                WriteObject(xmldoc.DocumentElement);
            }
            catch (Exception)
            {
                try
                {
                    if (!string.IsNullOrEmpty(m_SessionObj.Error))
                    {
                        XmlDocument ErrorDoc = new XmlDocument();
                        ErrorDoc.LoadXml(m_SessionObj.Error);
                        InvalidOperationException ex = new InvalidOperationException(ErrorDoc.OuterXml);
                        ErrorRecord er = new ErrorRecord(ex, "WsManError", ErrorCategory.InvalidOperation, computername);
                        this.WriteError(er);
                    }
                }
                catch (Exception)
                { }
            }
            finally
            {
                if (m_SessionObj != null)
                    Dispose(m_SessionObj);
            }
        }

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
        Dispose(IWSManSession sessionObject)
        {
            sessionObject = null;
            this.Dispose();
        }

        #endregion IDisposable Members
    }
    #endregion
}
