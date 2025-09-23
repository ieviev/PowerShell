// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;

namespace System.Management.Automation
{
    
    internal class MamlNode
    {
        
        internal MamlNode(XmlNode xmlNode)
        {
            _xmlNode = xmlNode;
        }

        private readonly XmlNode _xmlNode;

        
        /// <value></value>
        internal XmlNode XmlNode
        {
            get
            {
                return _xmlNode;
            }
        }

        private PSObject _mshObject;

        
        /// <value></value>
        internal PSObject PSObject
        {
            get
            {
                if (_mshObject == null)
                {
                    // There is no XSLT to convert docs to supported maml format
                    // We dont want comments etc to spoil our format.
                    // So remove all unsupported nodes before constructing help
                    // object.
                    RemoveUnsupportedNodes(_xmlNode);
                    _mshObject = GetPSObject(_xmlNode);
                }

                return _mshObject;
            }
        }

        #region Conversion of xmlNode => PSObject

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private PSObject GetPSObject(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return new PSObject();

            PSObject mshObject = null;

            if (IsAtomic(xmlNode))
            {
                mshObject = new PSObject(xmlNode.InnerText.Trim());
            }
            else if (IncludeMamlFormatting(xmlNode))
            {
                mshObject = new PSObject(GetMamlFormattingPSObjects(xmlNode));
            }
            else
            {
                mshObject = new PSObject(GetInsidePSObject(xmlNode));
                // Add typeNames to this MSHObject and create views so that
                // the output is readable. This is done only for complex nodes.
                mshObject.TypeNames.Clear();

                if (xmlNode.Attributes["type"] != null)
                {
                    if (string.Equals(xmlNode.Attributes["type"].Value, "field", StringComparison.OrdinalIgnoreCase))
                        mshObject.TypeNames.Add("MamlPSClassHelpInfo#field");
                    else if (string.Equals(xmlNode.Attributes["type"].Value, "method", StringComparison.OrdinalIgnoreCase))
                        mshObject.TypeNames.Add("MamlPSClassHelpInfo#method");
                }

                mshObject.TypeNames.Add("MamlCommandHelpInfo#" + xmlNode.LocalName);
            }

            if (xmlNode.Attributes != null)
            {
                foreach (XmlNode attribute in xmlNode.Attributes)
                {
                    mshObject.Properties.Add(new PSNoteProperty(attribute.Name, attribute.Value));
                }
            }

            return mshObject;
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private PSObject GetInsidePSObject(XmlNode xmlNode)
        {
            Hashtable properties = GetInsideProperties(xmlNode);

            PSObject mshObject = new PSObject();

            IDictionaryEnumerator enumerator = properties.GetEnumerator();

            while (enumerator.MoveNext())
            {
                mshObject.Properties.Add(new PSNoteProperty((string)enumerator.Key, enumerator.Value));
            }

            return mshObject;
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private Hashtable GetInsideProperties(XmlNode xmlNode)
        {
            Hashtable properties = new Hashtable(StringComparer.OrdinalIgnoreCase);

            if (xmlNode == null)
                return properties;

            if (xmlNode.ChildNodes != null)
            {
                foreach (XmlNode childNode in xmlNode.ChildNodes)
                {
                    AddProperty(properties, childNode.LocalName, GetPSObject(childNode));
                }
            }

            return SimplifyProperties(properties);
        }

        
        /// <param name="xmlNode">
        /// Node whose children are verified for maml.
        /// </param>
        private static void RemoveUnsupportedNodes(XmlNode xmlNode)
        {
            // Start with the first child..
            // We want to modify only children..
            // The current node is taken care by the callee..
            XmlNode childNode = xmlNode.FirstChild;
            while (childNode != null)
            {
                // We dont want Comments..so remove..
                if (childNode.NodeType == XmlNodeType.Comment)
                {
                    XmlNode nodeToRemove = childNode;
                    childNode = childNode.NextSibling;
                    // Remove this node and its children if any..
                    xmlNode.RemoveChild(nodeToRemove);
                }
                else
                {
                    // Search children...
                    RemoveUnsupportedNodes(childNode);
                    childNode = childNode.NextSibling;
                }
            }
        }

        
        /// <param name="properties">Property hashtable.</param>
        /// <param name="name">Property name.</param>
        /// <param name="mshObject">Property value.</param>
        private static void AddProperty(Hashtable properties, string name, PSObject mshObject)
        {
            ArrayList propertyValues = (ArrayList)properties[name];

            if (propertyValues == null)
            {
                propertyValues = new ArrayList();

                properties[name] = propertyValues;
            }

            if (mshObject == null)
                return;

            if (mshObject.BaseObject is PSCustomObject || !mshObject.BaseObject.GetType().Equals(typeof(PSObject[])))
            {
                propertyValues.Add(mshObject);
                return;
            }

            PSObject[] mshObjects = (PSObject[])mshObject.BaseObject;

            for (int i = 0; i < mshObjects.Length; i++)
            {
                propertyValues.Add(mshObjects[i]);
            }

            return;
        }

        
        /// <param name="properties"></param>
        /// <returns></returns>
        private static Hashtable SimplifyProperties(Hashtable properties)
        {
            if (properties == null)
                return null;

            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            IDictionaryEnumerator enumerator = properties.GetEnumerator();

            while (enumerator.MoveNext())
            {
                ArrayList propertyValues = (ArrayList)enumerator.Value;

                if (propertyValues == null || propertyValues.Count == 0)
                    continue;

                if (propertyValues.Count == 1)
                {
                    if (!IsMamlFormattingPSObject((PSObject)propertyValues[0]))
                    {
                        PSObject mshObject = (PSObject)propertyValues[0];

                        // Even for strings or other basic types, they need to be contained in PSObject in case
                        // there is attributes for this object.

                        result[enumerator.Key] = mshObject;

                        continue;
                    }
                }

                result[enumerator.Key] = propertyValues.ToArray(typeof(PSObject));
            }

            return result;
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private static bool IsAtomic(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return false;

            if (xmlNode.ChildNodes == null)
                return true;

            if (xmlNode.ChildNodes.Count > 1)
                return false;

            if (xmlNode.ChildNodes.Count == 0)
                return true;

            if (xmlNode.ChildNodes[0].GetType().Equals(typeof(XmlText)))
                return true;

            return false;
        }

        #endregion

        #region Maml formatting

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private static bool IncludeMamlFormatting(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return false;

            if (xmlNode.ChildNodes == null || xmlNode.ChildNodes.Count == 0)
                return false;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (IsMamlFormattingNode(childNode))
                {
                    return true;
                }
            }

            return false;
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private static bool IsMamlFormattingNode(XmlNode xmlNode)
        {
            if (xmlNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                return true;

            if (xmlNode.LocalName.Equals("list", StringComparison.OrdinalIgnoreCase))
                return true;

            if (xmlNode.LocalName.Equals("definitionList", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        
        /// <param name="mshObject"></param>
        /// <returns></returns>
        private static bool IsMamlFormattingPSObject(PSObject mshObject)
        {
            Collection<string> typeNames = mshObject.TypeNames;

            if (typeNames == null || typeNames.Count == 0)
                return false;

            return typeNames[typeNames.Count - 1].Equals("MamlTextItem", StringComparison.OrdinalIgnoreCase);
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private PSObject[] GetMamlFormattingPSObjects(XmlNode xmlNode)
        {
            ArrayList mshObjects = new ArrayList();

            int paraNodes = GetParaMamlNodeCount(xmlNode.ChildNodes);
            int count = 0;
            // Don't trim the content if this is an "introduction" node.
            bool trim = !string.Equals(xmlNode.Name, "maml:introduction", StringComparison.OrdinalIgnoreCase);
            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                {
                    ++count;
                    PSObject paraPSObject = GetParaPSObject(childNode, count != paraNodes, trim: trim);
                    if (paraPSObject != null)
                        mshObjects.Add(paraPSObject);
                    continue;
                }

                if (childNode.LocalName.Equals("list", StringComparison.OrdinalIgnoreCase))
                {
                    ArrayList listPSObjects = GetListPSObjects(childNode);

                    for (int i = 0; i < listPSObjects.Count; i++)
                    {
                        mshObjects.Add(listPSObjects[i]);
                    }

                    continue;
                }

                if (childNode.LocalName.Equals("definitionList", StringComparison.OrdinalIgnoreCase))
                {
                    ArrayList definitionListPSObjects = GetDefinitionListPSObjects(childNode);

                    for (int i = 0; i < definitionListPSObjects.Count; i++)
                    {
                        mshObjects.Add(definitionListPSObjects[i]);
                    }

                    continue;
                }

                // If we get here, there is some tags that is not supported by maml.
                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            return (PSObject[])mshObjects.ToArray(typeof(PSObject));
        }

        
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static int GetParaMamlNodeCount(XmlNodeList nodes)
        {
            int i = 0;

            foreach (XmlNode childNode in nodes)
            {
                if (childNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                {
                    if (childNode.InnerText.Trim().Equals(string.Empty))
                    {
                        continue;
                    }

                    ++i;
                }
            }

            return i;
        }

        
        /// <param name="node"></param>
        /// <param name="childNode"></param>
        private void WriteMamlInvalidChildNodeError(XmlNode node, XmlNode childNode)
        {
            ErrorRecord errorRecord = new ErrorRecord(new ParentContainsErrorRecordException("MamlInvalidChildNodeError"), "MamlInvalidChildNodeError", ErrorCategory.SyntaxError, null);
            errorRecord.ErrorDetails = new ErrorDetails(typeof(MamlNode).Assembly, "HelpErrors", "MamlInvalidChildNodeError", node.LocalName, childNode.LocalName, GetNodePath(node));
            this.Errors.Add(errorRecord);
        }

        
        /// <param name="node"></param>
        /// <param name="childNodeName"></param>
        /// <param name="count"></param>
        private void WriteMamlInvalidChildNodeCountError(XmlNode node, string childNodeName, int count)
        {
            ErrorRecord errorRecord = new ErrorRecord(new ParentContainsErrorRecordException("MamlInvalidChildNodeCountError"), "MamlInvalidChildNodeCountError", ErrorCategory.SyntaxError, null);
            errorRecord.ErrorDetails = new ErrorDetails(typeof(MamlNode).Assembly, "HelpErrors", "MamlInvalidChildNodeCountError", node.LocalName, childNodeName, count, GetNodePath(node));
            this.Errors.Add(errorRecord);
        }

        private static string GetNodePath(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return string.Empty;

            if (xmlNode.ParentNode == null)
                return "\\" + xmlNode.LocalName;

            return GetNodePath(xmlNode.ParentNode) + "\\" + xmlNode.LocalName + GetNodeIndex(xmlNode);
        }

        private static string GetNodeIndex(XmlNode xmlNode)
        {
            if (xmlNode == null || xmlNode.ParentNode == null)
                return string.Empty;

            int index = 0;
            int total = 0;

            foreach (XmlNode siblingNode in xmlNode.ParentNode.ChildNodes)
            {
                if (siblingNode == xmlNode)
                {
                    index = total++;
                    continue;
                }

                if (siblingNode.LocalName.Equals(xmlNode.LocalName, StringComparison.OrdinalIgnoreCase))
                {
                    total++;
                }
            }

            if (total > 1)
            {
                return "[" + index.ToString("d", CultureInfo.CurrentCulture) + "]";
            }

            return string.Empty;
        }

        
        /// <param name="xmlNode"></param>
        /// <param name="newLine"></param>
        /// <param name="trim"></param>
        /// <returns></returns>
        private static PSObject GetParaPSObject(XmlNode xmlNode, bool newLine, bool trim = true)
        {
            if (xmlNode == null)
                return null;

            if (!xmlNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                return null;

            PSObject mshObject = new PSObject();

            StringBuilder sb = new StringBuilder();

            if (newLine && !xmlNode.InnerText.Trim().Equals(string.Empty))
            {
                sb.AppendLine(xmlNode.InnerText.Trim());
            }
            else
            {
                var innerText = xmlNode.InnerText;
                if (trim)
                {
                    innerText = innerText.Trim();
                }

                sb.Append(innerText);
            }

            mshObject.Properties.Add(new PSNoteProperty("Text", sb.ToString()));

            mshObject.TypeNames.Clear();
            mshObject.TypeNames.Add("MamlParaTextItem");
            mshObject.TypeNames.Add("MamlTextItem");

            return mshObject;
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private ArrayList GetListPSObjects(XmlNode xmlNode)
        {
            ArrayList mshObjects = new ArrayList();

            if (xmlNode == null)
                return mshObjects;

            if (!xmlNode.LocalName.Equals("list", StringComparison.OrdinalIgnoreCase))
                return mshObjects;

            if (xmlNode.ChildNodes == null || xmlNode.ChildNodes.Count == 0)
                return mshObjects;

            bool ordered = IsOrderedList(xmlNode);
            int index = 1;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("listItem", StringComparison.OrdinalIgnoreCase))
                {
                    PSObject listItemPSObject = GetListItemPSObject(childNode, ordered, ref index);

                    if (listItemPSObject != null)
                        mshObjects.Add(listItemPSObject);

                    continue;
                }

                // If we get here, there is some tags that is not supported by maml.
                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            return mshObjects;
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private static bool IsOrderedList(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return false;

            if (xmlNode.Attributes == null || xmlNode.Attributes.Count == 0)
                return false;

            foreach (XmlNode attribute in xmlNode.Attributes)
            {
                if (attribute.Name.Equals("class", StringComparison.OrdinalIgnoreCase)
                    && attribute.Value.Equals("ordered", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        
        /// <param name="xmlNode"></param>
        /// <param name="ordered"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private PSObject GetListItemPSObject(XmlNode xmlNode, bool ordered, ref int index)
        {
            if (xmlNode == null)
                return null;

            if (!xmlNode.LocalName.Equals("listItem", StringComparison.OrdinalIgnoreCase))
                return null;

            string text = string.Empty;

            if (xmlNode.ChildNodes.Count > 1)
            {
                WriteMamlInvalidChildNodeCountError(xmlNode, "para", 1);
            }

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                {
                    text = childNode.InnerText.Trim();
                    continue;
                }

                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            string tag = string.Empty;
            if (ordered)
            {
                tag = index.ToString("d2", CultureInfo.CurrentCulture);
                tag += ". ";
                index++;
            }
            else
            {
                tag = "* ";
            }

            PSObject mshObject = new PSObject();

            mshObject.Properties.Add(new PSNoteProperty("Text", text));
            mshObject.Properties.Add(new PSNoteProperty("Tag", tag));

            mshObject.TypeNames.Clear();
            if (ordered)
            {
                mshObject.TypeNames.Add("MamlOrderedListTextItem");
            }
            else
            {
                mshObject.TypeNames.Add("MamlUnorderedListTextItem");
            }

            mshObject.TypeNames.Add("MamlTextItem");

            return mshObject;
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private ArrayList GetDefinitionListPSObjects(XmlNode xmlNode)
        {
            ArrayList mshObjects = new ArrayList();

            if (xmlNode == null)
                return mshObjects;

            if (!xmlNode.LocalName.Equals("definitionList", StringComparison.OrdinalIgnoreCase))
                return mshObjects;

            if (xmlNode.ChildNodes == null || xmlNode.ChildNodes.Count == 0)
                return mshObjects;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("definitionListItem", StringComparison.OrdinalIgnoreCase))
                {
                    PSObject definitionListItemPSObject = GetDefinitionListItemPSObject(childNode);

                    if (definitionListItemPSObject != null)
                        mshObjects.Add(definitionListItemPSObject);

                    continue;
                }

                // If we get here, we found some node that is not supported.
                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            return mshObjects;
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private PSObject GetDefinitionListItemPSObject(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return null;

            if (!xmlNode.LocalName.Equals("definitionListItem", StringComparison.OrdinalIgnoreCase))
                return null;

            string term = null;
            string definition = null;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("term", StringComparison.OrdinalIgnoreCase))
                {
                    term = childNode.InnerText.Trim();
                    continue;
                }

                if (childNode.LocalName.Equals("definition", StringComparison.OrdinalIgnoreCase))
                {
                    definition = GetDefinitionText(childNode);
                    continue;
                }

                // If we get here, we found some node that is not supported.
                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            if (string.IsNullOrEmpty(term))
                return null;

            PSObject mshObject = new PSObject();

            mshObject.Properties.Add(new PSNoteProperty("Term", term));
            mshObject.Properties.Add(new PSNoteProperty("Definition", definition));

            mshObject.TypeNames.Clear();
            mshObject.TypeNames.Add("MamlDefinitionTextItem");
            mshObject.TypeNames.Add("MamlTextItem");

            return mshObject;
        }

        
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        private string GetDefinitionText(XmlNode xmlNode)
        {
            if (xmlNode == null)
                return null;

            if (!xmlNode.LocalName.Equals("definition", StringComparison.OrdinalIgnoreCase))
                return null;

            if (xmlNode.ChildNodes == null || xmlNode.ChildNodes.Count == 0)
                return string.Empty;

            if (xmlNode.ChildNodes.Count > 1)
            {
                WriteMamlInvalidChildNodeCountError(xmlNode, "para", 1);
            }

            string text = string.Empty;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.LocalName.Equals("para", StringComparison.OrdinalIgnoreCase))
                {
                    text = childNode.InnerText.Trim();
                    continue;
                }

                WriteMamlInvalidChildNodeError(xmlNode, childNode);
            }

            return text;
        }

        #endregion

        #region Preformatted string processing

        
        /// <param name="text"></param>
        /// <returns></returns>
        private static string GetPreformattedText(string text)
        {
            // we are assuming tabsize=4 here.
            // It is discouraged to use tab in preformatted text.

            string noTabText = text.Replace("\t", "    ");
            string[] lines = noTabText.Split('\n');
            string[] trimedLines = TrimLines(lines);

            if (trimedLines == null || trimedLines.Length == 0)
                return string.Empty;

            int minIndentation = GetMinIndentation(trimedLines);

            string[] shortedLines = new string[trimedLines.Length];
            for (int i = 0; i < trimedLines.Length; i++)
            {
                if (IsEmptyLine(trimedLines[i]))
                {
                    shortedLines[i] = trimedLines[i];
                }
                else
                {
                    shortedLines[i] = trimedLines[i].Remove(0, minIndentation);
                }
            }

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < shortedLines.Length; i++)
            {
                result.AppendLine(shortedLines[i]);
            }

            return result.ToString();
        }

        
        /// <param name="lines">Lines to trim.</param>
        /// <returns>An string array with empty lines trimed on either end.</returns>
        private static string[] TrimLines(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return null;

            int i = 0;
            for (i = 0; i < lines.Length; i++)
            {
                if (!IsEmptyLine(lines[i]))
                    break;
            }

            int start = i;

            if (start == lines.Length)
                return null;

            for (i = lines.Length - 1; i >= start; i--)
            {
                if (!IsEmptyLine(lines[i]))
                    break;
            }

            int end = i;

            string[] result = new string[end - start + 1];
            for (i = start; i <= end; i++)
            {
                result[i - start] = lines[i];
            }

            return result;
        }

        
        /// <param name="lines"></param>
        /// <returns></returns>
        private static int GetMinIndentation(string[] lines)
        {
            int minIndentation = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (IsEmptyLine(lines[i]))
                    continue;

                int indentation = GetIndentation(lines[i]);

                if (minIndentation < 0 || indentation < minIndentation)
                    minIndentation = indentation;
            }

            return minIndentation;
        }

        
        /// <param name="line"></param>
        /// <returns></returns>
        private static int GetIndentation(string line)
        {
            if (IsEmptyLine(line))
                return 0;

            string leftTrimedLine = line.TrimStart(' ');

            return line.Length - leftTrimedLine.Length;
        }

        
        /// <param name="line"></param>
        /// <returns></returns>
        private static bool IsEmptyLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return true;

            string trimedLine = line.Trim();
            if (string.IsNullOrEmpty(trimedLine))
                return true;

            return false;
        }

        #endregion

        #region Error handling

        
        /// <value></value>
        internal Collection<ErrorRecord> Errors { get; } = new Collection<ErrorRecord>();

        #endregion
    }
}
