// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    
    internal abstract class HelpInfo
    {
        
        internal HelpInfo()
        {
        }

        
        internal abstract string Name
        {
            get;
        }

        
        internal abstract string Synopsis
        {
            get;
        }

        
        internal virtual string Component
        {
            get { return string.Empty; }
        }

        
        internal virtual string Role
        {
            get { return string.Empty; }
        }

        
        internal virtual string Functionality
        {
            get { return string.Empty; }
        }

        
        internal abstract HelpCategory HelpCategory
        {
            get;
        }

        
        internal HelpCategory ForwardHelpCategory { get; set; } = HelpCategory.None;

        
        internal string ForwardTarget { get; set; } = string.Empty;

        
        internal abstract PSObject FullHelp
        {
            get;
        }

        
        internal PSObject ShortHelp
        {
            get
            {
                if (this.FullHelp == null)
                    return null;

                PSObject shortHelpObject = new PSObject(this.FullHelp);

                shortHelpObject.TypeNames.Clear();
                shortHelpObject.TypeNames.Add("HelpInfoShort");

                return shortHelpObject;
            }
        }

        
        internal virtual PSObject[] GetParameter(string pattern)
        {
            return Array.Empty<PSObject>();
        }

        
        internal virtual Uri GetUriForOnlineHelp()
        {
            return null;
        }

        
        internal virtual bool MatchPatternInContent(WildcardPattern pattern)
        {
            // this is base class implementation..derived classes can choose
            // what is best to them.
            return false;
        }

        
        protected void AddCommonHelpProperties()
        {
            if (this.FullHelp == null)
                return;

            if (this.FullHelp.Properties["Name"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Name", this.Name));
            }

            if (this.FullHelp.Properties["Category"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Category", this.HelpCategory.ToString()));
            }

            if (this.FullHelp.Properties["Synopsis"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Synopsis", this.Synopsis));
            }

            if (this.FullHelp.Properties["Component"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Component", this.Component));
            }

            if (this.FullHelp.Properties["Role"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Role", this.Role));
            }

            if (this.FullHelp.Properties["Functionality"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Functionality", this.Functionality));
            }
        }

        
        protected void UpdateUserDefinedDataProperties()
        {
            if (this.FullHelp == null)
                return;

            this.FullHelp.Properties.Remove("Component");
            this.FullHelp.Properties.Add(new PSNoteProperty("Component", this.Component));

            this.FullHelp.Properties.Remove("Role");
            this.FullHelp.Properties.Add(new PSNoteProperty("Role", this.Role));

            this.FullHelp.Properties.Remove("Functionality");
            this.FullHelp.Properties.Add(new PSNoteProperty("Functionality", this.Functionality));
        }

        #region Error handling

        
        internal Collection<ErrorRecord> Errors { get; set; }

        #endregion
    }
}
