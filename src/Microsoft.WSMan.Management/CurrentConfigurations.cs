// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Xml;

namespace Microsoft.WSMan.Management
{
    
    internal class CurrentConfigurations
    {
        
        public const string DefaultNameSpacePrefix = "defaultNameSpace";

        
        private readonly XmlDocument rootDocument;

        
        private XmlElement documentElement;

        
        private XmlNamespaceManager nameSpaceManger;

        
        private readonly IWSManSession serverSession;

        
        public IWSManSession ServerSession
        {
            get { return serverSession; }
        }

        
        public XmlDocument RootDocument
        {
            get { return this.rootDocument; }
        }

        
        /// <param name="serverSession">Current server session.</param>
        public CurrentConfigurations(IWSManSession serverSession)
        {
            ArgumentNullException.ThrowIfNull(serverSession);

            this.rootDocument = new XmlDocument();
            this.serverSession = serverSession;
        }

        
        /// <param name="responseOfGet">Plugin configuration.</param>
        /// <returns>False, if operation failed.</returns>
        public bool RefreshCurrentConfiguration(string responseOfGet)
        {
            ArgumentException.ThrowIfNullOrEmpty(responseOfGet);

            this.rootDocument.LoadXml(responseOfGet);
            this.documentElement = this.rootDocument.DocumentElement;

            this.nameSpaceManger = new XmlNamespaceManager(this.rootDocument.NameTable);
            this.nameSpaceManger.AddNamespace(CurrentConfigurations.DefaultNameSpacePrefix, this.documentElement.NamespaceURI);

            return string.IsNullOrEmpty(this.serverSession.Error);
        }

        
        /// <param name="resourceUri">Resource URI to use.</param>
        /// <returns>False, if operation is not successful.</returns>
        public void PutConfigurationOnServer(string resourceUri)
        {
            ArgumentException.ThrowIfNullOrEmpty(resourceUri);

            this.serverSession.Put(resourceUri, this.rootDocument.InnerXml, 0);
        }

        
        /// <param name="pathToNodeFromRoot">Path with namespace to the node from Root element. Must not end with '/'.</param>
        public void RemoveOneConfiguration(string pathToNodeFromRoot)
        {
            ArgumentNullException.ThrowIfNull(pathToNodeFromRoot);

            XmlNode nodeToRemove =
                this.documentElement.SelectSingleNode(
                    pathToNodeFromRoot,
                    this.nameSpaceManger);

            if (nodeToRemove != null)
            {
                if (nodeToRemove is XmlAttribute)
                {
                    RemoveAttribute(nodeToRemove as XmlAttribute);
                }
            }
            else
            {
                throw new ArgumentException("Node is not present in the XML, Please give valid XPath", nameof(pathToNodeFromRoot));
            }
        }

        
        /// <param name="pathToNodeFromRoot">Path with namespace to the node from Root element. Must not end with '/'.</param>
        /// <param name="configurationName">Name of the configuration with name space to update or create.</param>
        /// <param name="configurationValue">Value of the configurations.</param>
        public void UpdateOneConfiguration(string pathToNodeFromRoot, string configurationName, string configurationValue)
        {
            ArgumentNullException.ThrowIfNull(pathToNodeFromRoot);
            ArgumentException.ThrowIfNullOrEmpty(configurationName);
            ArgumentNullException.ThrowIfNull(configurationValue);

            XmlNode nodeToUpdate =
                this.documentElement.SelectSingleNode(
                    pathToNodeFromRoot,
                    this.nameSpaceManger);

            if (nodeToUpdate != null)
            {
                foreach (XmlAttribute attribute in nodeToUpdate.Attributes)
                {
                    if (attribute.Name.Equals(configurationName, StringComparison.OrdinalIgnoreCase))
                    {
                        attribute.Value = configurationValue;
                        return;
                    }
                }

                XmlNode attr = this.rootDocument.CreateNode(XmlNodeType.Attribute, configurationName, string.Empty);
                attr.Value = configurationValue;

                nodeToUpdate.Attributes.SetNamedItem(attr);
            }
        }

        
        /// <param name="pathFromRoot">Path with namespace to the node from Root element.</param>
        /// <returns>Value of the Node, or Null if no node present.</returns>
        public string GetOneConfiguration(string pathFromRoot)
        {
            ArgumentNullException.ThrowIfNull(pathFromRoot);

            XmlNode requiredNode =
                this.documentElement.SelectSingleNode(
                    pathFromRoot,
                    this.nameSpaceManger);

            if (requiredNode != null)
            {
                return requiredNode.Value;
            }

            return null;
        }

        
        /// <param name="attributeToRemove">Attribute to Remove.</param>
        private static void RemoveAttribute(XmlAttribute attributeToRemove)
        {
            XmlElement ownerElement = attributeToRemove.OwnerElement;
            ownerElement.RemoveAttribute(attributeToRemove.Name);
        }
    }
}
