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

        
        /// <value>Name for help info</value>
        internal abstract string Name
        {
            get;
        }

        
        /// <value>Synopsis for help info</value>
        internal abstract string Synopsis
        {
            get;
        }

        
        /// <value>Component for help info</value>
        internal virtual string Component
        {
            get { return string.Empty; }
        }

        
        /// <value>Role for help ino</value>
        internal virtual string Role
        {
            get { return string.Empty; }
        }

        
        /// <value>Functionality for help info</value>
        internal virtual string Functionality
        {
            get { return string.Empty; }
        }

        
        /// <value>Help category for help info</value>
        internal abstract HelpCategory HelpCategory
        {
            get;
        }

        
        /// <remarks>
        /// If this is not HelpCategory.None, then some other help provider
        /// (as specified in the HelpCategory bit pattern) need
        /// to process this helpInfo before it can be returned to end user.
        /// </remarks>
        /// <value>Help category to forward this helpInfo to</value>
        internal HelpCategory ForwardHelpCategory { get; set; } = HelpCategory.None;

        
        /// <value>forward target object name</value>
        internal string ForwardTarget { get; set; } = string.Empty;

        
        /// <value>Full help object for this help item</value>
        internal abstract PSObject FullHelp
        {
            get;
        }

        
        /// <value>Short help object for this help item</value>
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

        
        /// <param name="pattern">Pattern to search for parameters.</param>
        /// <returns>A collection of parameters that match pattern.</returns>
        /// <remarks>
        /// The base method returns an empty list.
        /// </remarks>
        internal virtual PSObject[] GetParameter(string pattern)
        {
            return Array.Empty<PSObject>();
        }

        
        /// <returns>
        /// Null if no Uri is specified by the helpinfo or a
        /// valid Uri.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Specified Uri is not valid.
        /// </exception>
        internal virtual Uri GetUriForOnlineHelp()
        {
            return null;
        }

        
        /// <param name="pattern"></param>
        /// <returns></returns>
        internal virtual bool MatchPatternInContent(WildcardPattern pattern)
        {
            // this is base class implementation..derived classes can choose
            // what is best to them.
            return false;
        }

        
        /// <returns></returns>
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

        
        /// <remarks>
        /// This function wont create new properties.This will update only user-defined properties created in
        /// <paramref name="AddCommonHelpProperties"/>
        /// </remarks>
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

        
        /// <value></value>
        internal Collection<ErrorRecord> Errors { get; set; }

        #endregion
    }
}
