// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    
    internal class DefaultHelpProvider : HelpFileHelpProvider
    {
        
        internal DefaultHelpProvider(HelpSystem helpSystem)
            : base(helpSystem)
        {
        }

        #region Common Properties

        
        /// <value></value>
        internal override string Name
        {
            get
            {
                return "Default Help Provider";
            }
        }

        
        /// <value></value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.DefaultHelp;
            }
        }

        #endregion

        #region Help Provider Interface

        
        /// <param name="helpRequest">Help request object.</param>
        /// <returns></returns>
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            HelpRequest defaultHelpRequest = helpRequest.Clone();
            defaultHelpRequest.Target = "default";
            return base.ExactMatchHelp(defaultHelpRequest);
        }

        #endregion
    }
}
