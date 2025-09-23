// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml;

namespace System.Management.Automation
{
    
    internal class MamlClassHelpInfo : HelpInfo
    {
        
        internal MamlClassHelpInfo(PSObject helpObject, HelpCategory helpCategory)
        {
            HelpCategory = helpCategory;
            _fullHelpObject = helpObject;
        }

        
        private MamlClassHelpInfo(XmlNode xmlNode, HelpCategory helpCategory)
        {
            HelpCategory = helpCategory;

            MamlNode mamlNode = new MamlNode(xmlNode);
            _fullHelpObject = mamlNode.PSObject;

            this.Errors = mamlNode.Errors;
            _fullHelpObject.TypeNames.Clear();
            _fullHelpObject.TypeNames.Add("PSClassHelpInfo");
        }

        
        private readonly PSObject _fullHelpObject;

        #region Load

        
        internal static MamlClassHelpInfo Load(XmlNode xmlNode, HelpCategory helpCategory)
        {
            MamlClassHelpInfo mamlClassHelpInfo = new MamlClassHelpInfo(xmlNode, helpCategory);

            if (string.IsNullOrEmpty(mamlClassHelpInfo.Name))
                return null;

            mamlClassHelpInfo.AddCommonHelpProperties();

            return mamlClassHelpInfo;
        }

        #endregion

        #region Helper Methods and Overloads

        
        internal MamlClassHelpInfo Copy()
        {
            MamlClassHelpInfo result = new MamlClassHelpInfo(_fullHelpObject.Copy(), this.HelpCategory);
            return result;
        }

        
        internal MamlClassHelpInfo Copy(HelpCategory newCategoryToUse)
        {
            MamlClassHelpInfo result = new MamlClassHelpInfo(_fullHelpObject.Copy(), newCategoryToUse);
            result.FullHelp.Properties["Category"].Value = newCategoryToUse.ToString();
            return result;
        }

        internal override string Name
        {
            get
            {
                string tempName = string.Empty;
                var title = _fullHelpObject.Properties["title"];

                if (title != null && title.Value != null)
                {
                    tempName = title.Value.ToString();
                }

                return tempName;
            }
        }

        internal override string Synopsis
        {
            get
            {
                string tempSynopsis = string.Empty;
                var intro = _fullHelpObject.Properties["introduction"];

                if (intro != null && intro.Value != null)
                {
                    tempSynopsis = intro.Value.ToString();
                }

                return tempSynopsis;
            }
        }

        internal override HelpCategory HelpCategory { get; }

        internal override PSObject FullHelp
        {
            get { return _fullHelpObject; }
        }

        #endregion
    }
}
