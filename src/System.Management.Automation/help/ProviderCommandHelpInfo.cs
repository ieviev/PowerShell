// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    
    internal class ProviderCommandHelpInfo : HelpInfo
    {
        
        private readonly HelpInfo _helpInfo;

        
        internal ProviderCommandHelpInfo(HelpInfo genericHelpInfo, ProviderContext providerContext)
        {
            Dbg.Assert(genericHelpInfo != null, "Expected genericHelpInfo != null");
            Dbg.Assert(providerContext != null, "Expected providerContext != null");

            // This should be set to None to prevent infinite forwarding.
            this.ForwardHelpCategory = HelpCategory.None;

            // Now pick which help we should show.
            MamlCommandHelpInfo providerSpecificHelpInfo =
                providerContext.GetProviderSpecificHelpInfo(genericHelpInfo.Name);
            if (providerSpecificHelpInfo == null)
            {
                _helpInfo = genericHelpInfo;
            }
            else
            {
                providerSpecificHelpInfo.OverrideProviderSpecificHelpWithGenericHelp(genericHelpInfo);
                _helpInfo = providerSpecificHelpInfo;
            }
        }

        
        internal override PSObject[] GetParameter(string pattern)
        {
            return _helpInfo.GetParameter(pattern);
        }

        
        /// <returns>
        /// Null if no Uri is specified by the helpinfo or a
        /// valid Uri.
        /// </returns>
        internal override Uri GetUriForOnlineHelp()
        {
            return _helpInfo.GetUriForOnlineHelp();
        }

        
        internal override string Name
        {
            get
            {
                return _helpInfo.Name;
            }
        }

        
        internal override string Synopsis
        {
            get
            {
                return _helpInfo.Synopsis;
            }
        }

        
        internal override HelpCategory HelpCategory
        {
            get
            {
                return _helpInfo.HelpCategory;
            }
        }

        
        internal override PSObject FullHelp
        {
            get
            {
                return _helpInfo.FullHelp;
            }
        }

        
        internal override string Component
        {
            get
            {
                return _helpInfo.Component;
            }
        }

        
        internal override string Role
        {
            get
            {
                return _helpInfo.Role;
            }
        }

        
        internal override string Functionality
        {
            get
            {
                return _helpInfo.Functionality;
            }
        }
    }
}
