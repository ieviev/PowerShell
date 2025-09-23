// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;
using System.Xml;

namespace System.Management.Automation
{
    
    internal class MamlCommandHelpInfo : BaseCommandHelpInfo
    {
        
        internal MamlCommandHelpInfo(PSObject helpObject, HelpCategory helpCategory)
            : base(helpCategory)
        {
            _fullHelpObject = helpObject;

            this.ForwardHelpCategory = HelpCategory.Provider;

            this.AddCommonHelpProperties();
            // set user defined data
            if (helpObject.Properties["Component"] != null)
            {
                _component = helpObject.Properties["Component"].Value as string;
            }

            if (helpObject.Properties["Role"] != null)
            {
                _role = helpObject.Properties["Role"].Value as string;
            }

            if (helpObject.Properties["Functionality"] != null)
            {
                _functionality = helpObject.Properties["Functionality"].Value as string;
            }
        }

        
        private MamlCommandHelpInfo(XmlNode xmlNode, HelpCategory helpCategory) : base(helpCategory)
        {
            MamlNode mamlNode = new MamlNode(xmlNode);
            _fullHelpObject = mamlNode.PSObject;

            this.Errors = mamlNode.Errors;

            // The type name hierarchy for mshObject doesn't necessary
            // reflect the hierarchy in source code. From display's point of
            // view MamlCommandHelpInfo is derived from HelpInfo.

            _fullHelpObject.TypeNames.Clear();
            if (helpCategory == HelpCategory.DscResource)
            {
                _fullHelpObject.TypeNames.Add("DscResourceHelpInfo");
            }
            else
            {
                _fullHelpObject.TypeNames.Add("MamlCommandHelpInfo");
                _fullHelpObject.TypeNames.Add("HelpInfo");
            }

            this.ForwardHelpCategory = HelpCategory.Provider;
        }

        
        internal void OverrideProviderSpecificHelpWithGenericHelp(HelpInfo genericHelpInfo)
        {
            PSObject genericHelpMaml = genericHelpInfo.FullHelp;
            MamlUtil.OverrideName(_fullHelpObject, genericHelpMaml);
            MamlUtil.OverridePSTypeNames(_fullHelpObject, genericHelpMaml);
            MamlUtil.PrependSyntax(_fullHelpObject, genericHelpMaml);
            MamlUtil.PrependDetailedDescription(_fullHelpObject, genericHelpMaml);
            MamlUtil.OverrideParameters(_fullHelpObject, genericHelpMaml);
            MamlUtil.PrependNotes(_fullHelpObject, genericHelpMaml);
            MamlUtil.AddCommonProperties(_fullHelpObject, genericHelpMaml);
        }

        #region Basic Help Properties

        private readonly PSObject _fullHelpObject;

        
        internal override PSObject FullHelp
        {
            get
            {
                return _fullHelpObject;
            }
        }

        
        private string Examples
        {
            get
            {
                return ExtractTextForHelpProperty(this.FullHelp, "Examples");
            }
        }

        
        private string Parameters
        {
            get
            {
                return ExtractTextForHelpProperty(this.FullHelp, "Parameters");
            }
        }

        
        private string Notes
        {
            get
            {
                return ExtractTextForHelpProperty(this.FullHelp, "alertset");
            }
        }

        #endregion

        #region Component, Role, Features

        // Component, Role, Functionality are required by exchange for filtering
        // help contents to be returned from help system.
        //
        // Following is how this is going to work,
        //    1. Each command will optionally include component, role and functionality
        //       information. This information is discovered from help content
        //       from xml tags <component>, <role>, <functionality> respectively
        //       as part of command metadata.
        //    2. From command line, end user can request help for commands for
        //       particular component, role and functionality using parameters like
        //       -component, -role, -functionality.
        //    3. At runtime, help engine will match against component/role/functionality
        //       criteria before returning help results.
        //

        private string _component = null;
        
        internal override string Component
        {
            get
            {
                return _component;
            }
        }

        private string _role = null;
        
        internal override string Role
        {
            get
            {
                return _role;
            }
        }

        private string _functionality = null;
        
        internal override string Functionality
        {
            get
            {
                return _functionality;
            }
        }

        internal void SetAdditionalDataFromHelpComment(string component, string functionality, string role)
        {
            _component = component;
            _functionality = functionality;
            _role = role;

            // component,role,functionality is part of common help..
            // Update these properties as we have new data now..
            this.UpdateUserDefinedDataProperties();
        }

        
        internal void AddUserDefinedData(UserDefinedHelpData userDefinedData)
        {
            if (userDefinedData == null)
                return;

            string propertyValue;
            if (userDefinedData.Properties.TryGetValue("component", out propertyValue))
            {
                _component = propertyValue;
            }

            if (userDefinedData.Properties.TryGetValue("role", out propertyValue))
            {
                _role = propertyValue;
            }

            if (userDefinedData.Properties.TryGetValue("functionality", out propertyValue))
            {
                _functionality = propertyValue;
            }

            // component,role,functionality is part of common help..
            // Update these properties as we have new data now..
            this.UpdateUserDefinedDataProperties();
        }

        #endregion

        #region Load

        
        internal static MamlCommandHelpInfo Load(XmlNode xmlNode, HelpCategory helpCategory)
        {
            MamlCommandHelpInfo mamlCommandHelpInfo = new MamlCommandHelpInfo(xmlNode, helpCategory);

            if (string.IsNullOrEmpty(mamlCommandHelpInfo.Name))
                return null;

            mamlCommandHelpInfo.AddCommonHelpProperties();

            return mamlCommandHelpInfo;
        }

        #endregion

        #region Provider specific help

#if V2
        
        internal MamlCommandHelpInfo MergeProviderSpecificHelp(PSObject cmdletHelp, PSObject[] dynamicParameterHelp)
        {
            if (this._fullHelpObject == null)
                return null;

            MamlCommandHelpInfo result = (MamlCommandHelpInfo)this.MemberwiseClone();

            // We will need to use a deep clone of _fullHelpObject
            // to avoid _fullHelpObject being get terminated.
            result._fullHelpObject = this._fullHelpObject.Copy();

            if (cmdletHelp != null)
                result._fullHelpObject.Properties.Add(new PSNoteProperty("PS_Cmdlet", cmdletHelp));

            if (dynamicParameterHelp != null)
                result._fullHelpObject.Properties.Add(new PSNoteProperty("PS_DynamicParameters", dynamicParameterHelp));

            return result;
        }
#endif

        #endregion

        #region Helper Methods and Overloads

        
        private static string ExtractTextForHelpProperty(PSObject psObject, string propertyName)
        {
            if (psObject == null)
                return string.Empty;

            if (psObject.Properties[propertyName] == null ||
                psObject.Properties[propertyName].Value == null)
            {
                return string.Empty;
            }

            return ExtractText(PSObject.AsPSObject(psObject.Properties[propertyName].Value));
        }

        
        private static string ExtractText(PSObject psObject)
        {
            if (psObject == null)
            {
                return string.Empty;
            }

            // I think every cmdlet description should at least have 400 characters...
            // so starting with this assumption..I did an average of all the cmdlet
            // help content available at the time of writing this code and came up
            // with this number.
            StringBuilder result = new StringBuilder(400);
            foreach (PSPropertyInfo propertyInfo in psObject.Properties)
            {
                string typeNameOfValue = propertyInfo.TypeNameOfValue;
                switch (typeNameOfValue.ToLowerInvariant())
                {
                    case "system.boolean":
                    case "system.int32":
                    case "system.object":
                    case "system.object[]":
                        continue;
                    case "system.string":
                        result.Append((string)LanguagePrimitives.ConvertTo(propertyInfo.Value,
                            typeof(string), CultureInfo.InvariantCulture));
                        break;
                    case "system.management.automation.psobject[]":
                        PSObject[] items = (PSObject[])LanguagePrimitives.ConvertTo(
                                propertyInfo.Value,
                                typeof(PSObject[]),
                                CultureInfo.InvariantCulture);
                        foreach (PSObject item in items)
                        {
                            result.Append(ExtractText(item));
                        }

                        break;
                    case "system.management.automation.psobject":
                        result.Append(ExtractText(PSObject.AsPSObject(propertyInfo.Value)));
                        break;
                    default:
                        result.Append(ExtractText(PSObject.AsPSObject(propertyInfo.Value)));
                        break;
                }
            }

            return result.ToString();
        }

        
        internal override bool MatchPatternInContent(WildcardPattern pattern)
        {
            System.Management.Automation.Diagnostics.Assert(pattern != null, "pattern cannot be null");

            string synopsis = Synopsis;
            if ((!string.IsNullOrEmpty(synopsis)) && (pattern.IsMatch(synopsis)))
            {
                return true;
            }

            string detailedDescription = DetailedDescription;
            if ((!string.IsNullOrEmpty(detailedDescription)) && (pattern.IsMatch(detailedDescription)))
            {
                return true;
            }

            string examples = Examples;
            if ((!string.IsNullOrEmpty(examples)) && (pattern.IsMatch(examples)))
            {
                return true;
            }

            string notes = Notes;
            if ((!string.IsNullOrEmpty(notes)) && (pattern.IsMatch(notes)))
            {
                return true;
            }

            string parameters = Parameters;
            if ((!string.IsNullOrEmpty(parameters)) && (pattern.IsMatch(parameters)))
            {
                return true;
            }

            return false;
        }

        internal MamlCommandHelpInfo Copy()
        {
            MamlCommandHelpInfo result = new MamlCommandHelpInfo(_fullHelpObject.Copy(), this.HelpCategory);
            return result;
        }

        internal MamlCommandHelpInfo Copy(HelpCategory newCategoryToUse)
        {
            MamlCommandHelpInfo result = new MamlCommandHelpInfo(_fullHelpObject.Copy(), newCategoryToUse);
            result.FullHelp.Properties["Category"].Value = newCategoryToUse.ToString();
            return result;
        }

        #endregion
    }
}
