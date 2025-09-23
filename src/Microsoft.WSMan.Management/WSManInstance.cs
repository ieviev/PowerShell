// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    #region Get-WSManInstance
    
    [Cmdlet(VerbsCommon.Get, "WSManInstance", DefaultParameterSetName = "GetInstance", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096627")]
    [OutputType(typeof(XmlElement))]
    public class GetWSManInstanceCommand : AuthenticatingWSManCommand, IDisposable
    {
        #region parameter
        
        [Parameter(ParameterSetName = "GetInstance")]
        [Parameter(ParameterSetName = "Enumerate")]
        public string ApplicationName
        {
            get
            {
                return applicationname;
            }

            set
            {
                { applicationname = value; }
            }
        }

        private string applicationname = null;

        
        [Parameter(ParameterSetName = "Enumerate")]
        [Alias("UBPO", "Base")]
        public SwitchParameter BasePropertiesOnly
        {
            get
            {
                return basepropertiesonly;
            }

            set
            {
                { basepropertiesonly = value; }
            }
        }

        private SwitchParameter basepropertiesonly;

        
        [Parameter(ParameterSetName = "GetInstance")]
        [Parameter(ParameterSetName = "Enumerate")]
        [Alias("CN")]
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

        
        [Parameter(
                  ParameterSetName = "GetInstance")]
        [Parameter(
                  ParameterSetName = "Enumerate")]
        [Alias("CURI", "CU")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        public Uri ConnectionURI
        {
            get
            {
                return connectionuri;
            }

            set
            {
                { connectionuri = value; }
            }
        }

        private Uri connectionuri;

        
        [Parameter]
        public Uri Dialect
        {
            get
            {
                return dialect;
            }

            set
            {
                { dialect = value; }
            }
        }

        private Uri dialect;

        
        [Parameter(Mandatory = true,
                  ParameterSetName = "Enumerate")]
        public SwitchParameter Enumerate
        {
            get
            {
                return enumerate;
            }

            set
            {
                { enumerate = value; }
            }
        }

        private SwitchParameter enumerate;

        
        [Parameter(ParameterSetName = "Enumerate")]
        [ValidateNotNullOrEmpty]
        public string Filter
        {
            get
            {
                return filter;
            }

            set
            {
                { filter = value; }
            }
        }

        private string filter;

        
        [Parameter(ParameterSetName = "GetInstance")]
        [ValidateNotNullOrEmpty]
        public string Fragment
        {
            get
            {
                return fragment;
            }

            set
            {
                { fragment = value; }
            }
        }

        private string fragment;

        
        [Parameter(ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("OS")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable OptionSet
        {
            get
            {
                return optionset;
            }

            set
            {
                { optionset = value; }
            }
        }

        private Hashtable optionset;

        
        [Parameter(ParameterSetName = "Enumerate")]
        [Parameter(ParameterSetName = "GetInstance")]
        public int Port
        {
            get
            {
                return port;
            }

            set
            {
                { port = value; }
            }
        }

        private int port = 0;

        
        [Parameter(ParameterSetName = "Enumerate")]
        public SwitchParameter Associations
        {
            get
            {
                return associations;
            }

            set
            {
                { associations = value; }
            }
        }

        private SwitchParameter associations;

        
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [Alias("RURI")]
        public Uri ResourceURI
        {
            get
            {
                return resourceuri;
            }

            set
            {
                { resourceuri = value; }
            }
        }

        private Uri resourceuri;

        
        [Parameter(ParameterSetName = "Enumerate")]

        [ValidateNotNullOrEmpty]
        [ValidateSet(new string[] { "object", "epr", "objectandepr" })]
        [Alias("RT")]
        public string ReturnType
        {
            get
            {
                return returntype;
            }

            set
            {
                { returntype = value; }
            }
        }

        private string returntype = "object";

        
        [Parameter(
                   ParameterSetName = "GetInstance")]

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable SelectorSet
        {
            get
            {
                return selectorset;
            }

            set
            {
                { selectorset = value; }
            }
        }

        private Hashtable selectorset;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("SO")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public SessionOption SessionOption
        {
            get
            {
                return sessionoption;
            }

            set
            {
                { sessionoption = value; }
            }
        }

        private SessionOption sessionoption;

        
        [Parameter(ParameterSetName = "Enumerate")]

        public SwitchParameter Shallow
        {
            get
            {
                return shallow;
            }

            set
            {
                { shallow = value; }
            }
        }

        private SwitchParameter shallow;

        
        [Parameter(ParameterSetName = "GetInstance")]
        [Parameter(ParameterSetName = "Enumerate")]

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        [Alias("SSL")]
        public SwitchParameter UseSSL
        {
            get
            {
                return usessl;
            }

            set
            {
                { usessl = value; }
            }
        }

        private SwitchParameter usessl;

        #endregion parameter

        #region private
        private WSManHelper helper;

        private string GetFilter()
        {
            string name;
            string value;
            string[] Split = filter.Trim().Split(new char[] { '=', ';' });
            if ((Split.Length) % 2 != 0)
            {
                // mismatched property name/value pair
                return null;
            }

            filter = "<wsman:SelectorSet xmlns:wsman='http://schemas.dmtf.org/wbem/wsman/1/wsman.xsd'>";
            for (int i = 0; i < Split.Length; i += 2)
            {
                value = Split[i + 1].Substring(1, Split[i + 1].Length - 2);
                name = Split[i];
                filter = filter + "<wsman:Selector Name='" + name + "'>" + value + "</wsman:Selector>";
            }

            filter += "</wsman:SelectorSet>";
            return (filter);
        }

        private void ReturnEnumeration(IWSManEx wsmanObject, IWSManResourceLocator wsmanResourceLocator, IWSManSession wsmanSession)
        {
            string fragment;
            try
            {
                int flags = 0;
                IWSManEnumerator obj;
                if (returntype != null)
                {
                    if (returntype.Equals("object", StringComparison.OrdinalIgnoreCase))
                    {
                        flags = wsmanObject.EnumerationFlagReturnObject();
                    }
                    else if (returntype.Equals("epr", StringComparison.OrdinalIgnoreCase))
                    {
                        flags = wsmanObject.EnumerationFlagReturnEPR();
                    }
                    else
                    {
                        flags = wsmanObject.EnumerationFlagReturnObjectAndEPR();
                    }
                }

                if (shallow)
                {
                    flags |= wsmanObject.EnumerationFlagHierarchyShallow();
                }
                else if (basepropertiesonly)
                {
                    flags |= wsmanObject.EnumerationFlagHierarchyDeepBasePropsOnly();
                }
                else
                {
                    flags |= wsmanObject.EnumerationFlagHierarchyDeep();
                }

                if (dialect != null && filter != null)
                {
                    if (dialect.ToString().Equals(helper.ALIAS_WQL, StringComparison.OrdinalIgnoreCase) || dialect.ToString().Equals(helper.URI_WQL_DIALECT, StringComparison.OrdinalIgnoreCase))
                    {
                        fragment = helper.URI_WQL_DIALECT;
                        dialect = new Uri(fragment);
                    }
                    else if (dialect.ToString().Equals(helper.ALIAS_ASSOCIATION, StringComparison.OrdinalIgnoreCase) || dialect.ToString().Equals(helper.URI_ASSOCIATION_DIALECT, StringComparison.OrdinalIgnoreCase))
                    {
                        if (associations)
                        {
                            flags |= wsmanObject.EnumerationFlagAssociationInstance();
                        }
                        else
                        {
                            flags |= wsmanObject.EnumerationFlagAssociatedInstance();
                        }

                        fragment = helper.URI_ASSOCIATION_DIALECT;
                        dialect = new Uri(fragment);
                    }
                    else if (dialect.ToString().Equals(helper.ALIAS_SELECTOR, StringComparison.OrdinalIgnoreCase) || dialect.ToString().Equals(helper.URI_SELECTOR_DIALECT, StringComparison.OrdinalIgnoreCase))
                    {
                        filter = GetFilter();
                        fragment = helper.URI_SELECTOR_DIALECT;
                        dialect = new Uri(fragment);
                    }

                    obj = (IWSManEnumerator)wsmanSession.Enumerate(wsmanResourceLocator, filter, dialect.ToString(), flags);
                }
                else if (filter != null)
                {
                    fragment = helper.URI_WQL_DIALECT;
                    dialect = new Uri(fragment);
                    obj = (IWSManEnumerator)wsmanSession.Enumerate(wsmanResourceLocator, filter, dialect.ToString(), flags);
                }
                else
                {
                    obj = (IWSManEnumerator)wsmanSession.Enumerate(wsmanResourceLocator, filter, null, flags);
                }
                while (!obj.AtEndOfStream)
                {
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(obj.ReadItem());
                    WriteObject(xmldoc.FirstChild);
                }
            }
            catch (Exception ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "Exception", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }
        }
        #endregion private
        #region override
        
        protected override void ProcessRecord()
        {
            IWSManSession m_session = null;
            IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
            helper = new WSManHelper(this);
            helper.WSManOp = "Get";
            string connectionStr = null;
            connectionStr = helper.CreateConnectionString(connectionuri, port, computername, applicationname);
            if (connectionuri != null)
            {
                try
                {
                    // in the format http(s)://server[:port/applicationname]
                    string[] constrsplit = connectionuri.OriginalString.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                    string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                    computername = constrsplit1[1].Trim();
                }
                catch (IndexOutOfRangeException)
                {
                    helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                }
            }

            try
            {
                IWSManResourceLocator m_resource = helper.InitializeResourceLocator(optionset, selectorset, fragment, dialect, m_wsmanObject, resourceuri);
                m_session = helper.CreateSessionObject(m_wsmanObject, Authentication, sessionoption, Credential, connectionStr, CertificateThumbprint, usessl.IsPresent);

                if (!enumerate)
                {
                    XmlDocument xmldoc = new XmlDocument();
                    try
                    {
                        xmldoc.LoadXml(m_session.Get(m_resource, 0));
                    }
                    catch (XmlException ex)
                    {
                        helper.AssertError(ex.Message, false, computername);
                    }

                    if (!string.IsNullOrEmpty(fragment))
                    {
                        WriteObject(xmldoc.FirstChild.LocalName + "=" + xmldoc.FirstChild.InnerText);
                    }
                    else
                    {
                        WriteObject(xmldoc.FirstChild);
                    }
                }
                else
                {
                    try
                    {
                        ReturnEnumeration(m_wsmanObject, m_resource, m_session);
                    }
                    catch (Exception ex)
                    {
                        helper.AssertError(ex.Message, false, computername);
                    }
                }
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
        #endregion override
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
            helper.CleanUp();
        }
    }
    #endregion

    #region Set-WsManInstance

    
    [Cmdlet(VerbsCommon.Set, "WSManInstance", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096937")]
    [OutputType(typeof(XmlElement), typeof(string))]
    public class SetWSManInstanceCommand : AuthenticatingWSManCommand, IDisposable
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

        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [Parameter(ParameterSetName = "URI")]
        [ValidateNotNullOrEmpty]
        public Uri ConnectionURI
        {
            get { return connectionuri; }

            set { connectionuri = value; }
        }

        private Uri connectionuri;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public Uri Dialect
        {
            get { return dialect; }

            set { dialect = value; }
        }

        private Uri dialect;

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Path")]
        [ValidateNotNullOrEmpty]
        public string FilePath
        {
            get { return filepath; }

            set { filepath = value; }
        }

        private string filepath;

        
        [Parameter(ParameterSetName = "ComputerName")]
        [Parameter(ParameterSetName = "URI")]
        [ValidateNotNullOrEmpty]
        public string Fragment
        {
            get { return fragment; }

            set { fragment = value; }
        }

        private string fragment;

        
        [Parameter]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("os")]
        [ValidateNotNullOrEmpty]
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

        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Resourceuri")]

        [Parameter(Mandatory = true, Position = 0)]
        [Alias("ruri")]
        [ValidateNotNullOrEmpty]
        public Uri ResourceURI
        {
            get { return resourceuri; }

            set { resourceuri = value; }
        }

        private Uri resourceuri;

        
        [Parameter(Position = 1,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [ValidateNotNullOrEmpty]
        public Hashtable SelectorSet
        {
            get { return selectorset; }

            set { selectorset = value; }
        }

        private Hashtable selectorset;

        
        [Parameter]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("so")]
        [ValidateNotNullOrEmpty]
        public SessionOption SessionOption
        {
            get { return sessionoption; }

            set { sessionoption = value; }
        }

        private SessionOption sessionoption;

        
        [Parameter(ParameterSetName = "ComputerName")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        [Alias("ssl")]
        public SwitchParameter UseSSL
        {
            get { return usessl; }

            set { usessl = value; }
        }

        private SwitchParameter usessl;

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [ValidateNotNullOrEmpty]
        public Hashtable ValueSet
        {
            get { return valueset; }

            set { valueset = value; }
        }

        private Hashtable valueset;

        #endregion

        private WSManHelper helper;
        
        protected override void ProcessRecord()
        {
            IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
            helper = new WSManHelper(this);
            helper.WSManOp = "set";
            IWSManSession m_session = null;

            if (dialect != null)
            {
                if (dialect.ToString().Equals(helper.ALIAS_WQL, StringComparison.OrdinalIgnoreCase))
                    dialect = new Uri(helper.URI_WQL_DIALECT);
                if (dialect.ToString().Equals(helper.ALIAS_SELECTOR, StringComparison.OrdinalIgnoreCase))
                    dialect = new Uri(helper.URI_SELECTOR_DIALECT);
                if (dialect.ToString().Equals(helper.ALIAS_ASSOCIATION, StringComparison.OrdinalIgnoreCase))
                    dialect = new Uri(helper.URI_ASSOCIATION_DIALECT);
            }

            try
            {
                string connectionStr = string.Empty;
                connectionStr = helper.CreateConnectionString(connectionuri, port, computername, applicationname);
                if (connectionuri != null)
                {
                    try
                    {
                        // in the format http(s)://server[:port/applicationname]
                        string[] constrsplit = connectionuri.OriginalString.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                        string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                        computername = constrsplit1[1].Trim();
                    }
                    catch (IndexOutOfRangeException)
                    {
                        helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                    }
                }

                IWSManResourceLocator m_resource = helper.InitializeResourceLocator(optionset, selectorset, fragment, dialect, m_wsmanObject, resourceuri);
                m_session = helper.CreateSessionObject(m_wsmanObject, Authentication, sessionoption, Credential, connectionStr, CertificateThumbprint, usessl.IsPresent);
                string rootNode = helper.GetRootNodeName(helper.WSManOp, m_resource.ResourceUri, null);
                string input = helper.ProcessInput(m_wsmanObject, filepath, helper.WSManOp, rootNode, valueset, m_resource, m_session);

                XmlDocument xmldoc = new XmlDocument();
                try
                {
                    xmldoc.LoadXml(m_session.Put(m_resource, input, 0));
                }
                catch (XmlException ex)
                {
                    helper.AssertError(ex.Message, false, computername);
                }

                if (!string.IsNullOrEmpty(fragment))
                {
                    if (xmldoc.DocumentElement.ChildNodes.Count > 0)
                    {
                        foreach (XmlNode node in xmldoc.DocumentElement.ChildNodes)
                        {
                            if (node.Name.Equals(fragment, StringComparison.OrdinalIgnoreCase))
                                WriteObject(node.Name + " = " + node.InnerText);
                        }
                    }
                }
                else
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
            helper.CleanUp();
        }
    }

    #endregion

    #region Remove-WsManInstance

    
    [Cmdlet(VerbsCommon.Remove, "WSManInstance", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096721")]
    public class RemoveWSManInstanceCommand : AuthenticatingWSManCommand, IDisposable
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

        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [Parameter(ParameterSetName = "URI")]
        [ValidateNotNullOrEmpty]
        public Uri ConnectionURI
        {
            get { return connectionuri; }

            set { connectionuri = value; }
        }

        private Uri connectionuri;

        
        [Parameter]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("os")]
        [ValidateNotNullOrEmpty]
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

        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Resourceuri")]

        [Parameter(Mandatory = true, Position = 0)]
        [Alias("ruri")]
        [ValidateNotNullOrEmpty]
        public Uri ResourceURI
        {
            get { return resourceuri; }

            set { resourceuri = value; }
        }

        private Uri resourceuri;

        
        [Parameter(Position = 1, Mandatory = true,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [ValidateNotNullOrEmpty]
        public Hashtable SelectorSet
        {
            get { return selectorset; }

            set { selectorset = value; }
        }

        private Hashtable selectorset;

        
        [Parameter]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("so")]
        [ValidateNotNullOrEmpty]
        public SessionOption SessionOption
        {
            get { return sessionoption; }

            set { sessionoption = value; }
        }

        private SessionOption sessionoption;

        
        [Parameter(ParameterSetName = "ComputerName")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        [Alias("ssl")]
        public SwitchParameter UseSSL
        {
            get { return usessl; }

            set { usessl = value; }
        }

        private SwitchParameter usessl;

        #endregion

        
        protected override void ProcessRecord()
        {
            WSManHelper helper = new WSManHelper(this);
            IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
            helper.WSManOp = "remove";
            IWSManSession m_session = null;
            try
            {
                string connectionStr = string.Empty;
                connectionStr = helper.CreateConnectionString(connectionuri, port, computername, applicationname);
                if (connectionuri != null)
                {
                    try
                    {
                        // in the format http(s)://server[:port/applicationname]
                        string[] constrsplit = connectionuri.OriginalString.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                        string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                        computername = constrsplit1[1].Trim();
                    }
                    catch (IndexOutOfRangeException)
                    {
                        helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                    }
                }

                IWSManResourceLocator m_resource = helper.InitializeResourceLocator(optionset, selectorset, null, null, m_wsmanObject, resourceuri);
                m_session = helper.CreateSessionObject(m_wsmanObject, Authentication, sessionoption, Credential, connectionStr, CertificateThumbprint, usessl.IsPresent);
                string ResourceURI = helper.GetURIWithFilter(resourceuri.ToString(), null, selectorset, helper.WSManOp);
                try
                {
                    ((IWSManSession)m_session).Delete(ResourceURI, 0);
                }
                catch (Exception ex)
                {
                    helper.AssertError(ex.Message, false, computername);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_session.Error))
                {
                    helper.AssertError(m_session.Error, true, resourceuri);
                }

                if (!string.IsNullOrEmpty(m_wsmanObject.Error))
                {
                    helper.AssertError(m_wsmanObject.Error, true, resourceuri);
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
    }

    #endregion

    #region New-WsManInstance
    
    [Cmdlet(VerbsCommon.New, "WSManInstance", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096933")]
    [OutputType(typeof(XmlElement))]
    public class NewWSManInstanceCommand : AuthenticatingWSManCommand, IDisposable
    {
        
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
        [ValidateNotNullOrEmpty]
        [Alias("Path")]
        public string FilePath
        {
            get { return filepath; }

            set { filepath = value; }
        }

        private string filepath;

        
        [Parameter]
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

        
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [Alias("ruri")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        public Uri ResourceURI
        {
            get { return resourceuri; }

            set { resourceuri = value; }
        }

        private Uri resourceuri;

        
        [Parameter(Mandatory = true, Position = 1,
                   ValueFromPipeline = true)]
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
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable ValueSet
        {
            get { return valueset; }

            set { valueset = value; }
        }

        private Hashtable valueset;

        private WSManHelper helper;
        private readonly IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
        private IWSManSession m_session = null;
        private string connectionStr = string.Empty;

        
        protected override void BeginProcessing()
        {
            helper = new WSManHelper(this);
            helper.WSManOp = "new";
            connectionStr = helper.CreateConnectionString(connectionuri, port, computername, applicationname);
            if (connectionuri != null)
            {
                try
                {
                    // in the format http(s)://server[:port/applicationname]
                    string[] constrsplit = connectionuri.OriginalString.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                    string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                    computername = constrsplit1[1].Trim();
                }
                catch (IndexOutOfRangeException)
                {
                    helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                }
            }
        }

        
        protected override void ProcessRecord()
        {
            try
            {
                IWSManResourceLocator m_resource = helper.InitializeResourceLocator(optionset, selectorset, null, null, m_wsmanObject, resourceuri);
                // create the session object
                m_session = helper.CreateSessionObject(m_wsmanObject, Authentication, sessionoption, Credential, connectionStr, CertificateThumbprint, usessl.IsPresent);
                string rootNode = helper.GetRootNodeName(helper.WSManOp, m_resource.ResourceUri, null);
                string input = helper.ProcessInput(m_wsmanObject, filepath, helper.WSManOp, rootNode, valueset, m_resource, m_session);

                try
                {
                    string resultXml = m_session.Create(m_resource, input, 0);
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(resultXml);
                    WriteObject(xmldoc.DocumentElement);
                }
                catch (Exception ex)
                {
                    helper.AssertError(ex.Message, false, computername);
                }
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
                {
                    Dispose(m_session);
                }
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
            helper.CleanUp();
        }
    }

    #endregion
}
