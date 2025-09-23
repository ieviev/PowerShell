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
    
    [Cmdlet(VerbsLifecycle.Invoke, "WSManAction", DefaultParameterSetName = "URI", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096843")]
    [OutputType(typeof(XmlElement))]
    public class InvokeWSManActionCommand : AuthenticatingWSManCommand, IDisposable
    {
        
        [Parameter(Mandatory = true,
                  Position = 1)]
        [ValidateNotNullOrEmpty]
        public string Action
        {
            get { return action; }

            set { action = value; }
        }

        private string action;

        
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        public string ApplicationName
        {
            get { return applicationname; }

            set { applicationname = value; }
        }

        private string applicationname = null;

        
        [Parameter(ParameterSetName = "ComputerName")]
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
        [Alias("CURI", "CU")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        public Uri ConnectionURI
        {
            get { return connectionuri; }

            set { connectionuri = value; }
        }

        private Uri connectionuri;

        
        [Parameter]
        [Alias("Path")]
        [ValidateNotNullOrEmpty]
        public string FilePath
        {
            get { return filepath; }

            set { filepath = value; }
        }

        private string filepath;

        
        [Parameter(ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("os")]
        public Hashtable OptionSet
        {
            get { return optionset; }

            set { optionset = value; }
        }

        private Hashtable optionset;

        
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(1, int.MaxValue)]
        public int Port
        {
            get { return port; }

            set { port = value; }
        }

        private int port = 0;

        
        [Parameter(Position = 2,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable SelectorSet
        {
            get { return selectorset; }

            set { selectorset = value; }
        }

        private Hashtable selectorset;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("so")]
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

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable ValueSet
        {
            get { return valueset; }

            set { valueset = value; }
        }

        private Hashtable valueset;

        
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("ruri")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        public Uri ResourceURI
        {
            get { return resourceuri; }

            set { resourceuri = value; }
        }

        private Uri resourceuri;

        private WSManHelper helper;
        private readonly IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
        private IWSManSession m_session = null;
        private string connectionStr = string.Empty;

        
        protected override void BeginProcessing()
        {
            helper = new WSManHelper(this);

            helper.WSManOp = "invoke";

            // create the connection string
            connectionStr = helper.CreateConnectionString(connectionuri, port, computername, applicationname);
        }

        
        protected override void ProcessRecord()
        {
            try
            {
                // create the resourcelocator object
                IWSManResourceLocator m_resource = helper.InitializeResourceLocator(optionset, selectorset, null, null, m_wsmanObject, resourceuri);

                // create the session object
                m_session = helper.CreateSessionObject(m_wsmanObject, Authentication, sessionoption, Credential, connectionStr, CertificateThumbprint, usessl.IsPresent);

                string rootNode = helper.GetRootNodeName(helper.WSManOp, m_resource.ResourceUri, action);
                string input = helper.ProcessInput(m_wsmanObject, filepath, helper.WSManOp, rootNode, valueset, m_resource, m_session);
                string resultXml = m_session.Invoke(action, m_resource, input, 0);

                XmlDocument xmldoc = new XmlDocument();
                xmldoc.LoadXml(resultXml);
                WriteObject(xmldoc.DocumentElement);
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_wsmanObject.Error))
                {
                    helper.AssertError(m_wsmanObject.Error, true, resourceuri);
                }

                if (!string.IsNullOrEmpty(m_session.Error))
                {
                    helper.AssertError(m_session.Error, true, resourceuri);
                }

                if (m_session != null)
                    Dispose(m_session);
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

        
        protected override void EndProcessing()
        {
            //  WSManHelper helper = new WSManHelper();
            helper.CleanUp();
        }
    }
}
