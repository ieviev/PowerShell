// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    
    internal abstract class HelpProviderWithFullCache : HelpProviderWithCache
    {
        
        internal HelpProviderWithFullCache(HelpSystem helpSystem) : base(helpSystem)
        {
        }

        
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>The HelpInfo found. Null if nothing is found.</returns>
        internal sealed override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            if (!this.CacheFullyLoaded)
            {
                LoadCache();
            }

            this.CacheFullyLoaded = true;

            return base.ExactMatchHelp(helpRequest);
        }

        
        /// <param name="helpRequest">Help request object.</param>
        internal sealed override void DoExactMatchHelp(HelpRequest helpRequest)
        {
        }

        
        /// <param name="helpRequest">Help request object.</param>
        /// <param name="searchOnlyContent">
        /// If true, searches for pattern in the help content. Individual
        /// provider can decide which content to search in.
        ///
        /// If false, searches for pattern in the command names.
        /// </param>
        /// <returns>A collection of help info objects.</returns>
        internal sealed override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            if (!this.CacheFullyLoaded)
            {
                LoadCache();
            }

            this.CacheFullyLoaded = true;

            return base.SearchHelp(helpRequest, searchOnlyContent);
        }

        
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>A collection of help info objects.</returns>
        internal sealed override IEnumerable<HelpInfo> DoSearchHelp(HelpRequest helpRequest)
        {
            return null;
        }

        
        /// <remarks>
        /// This is the only member child class need to override for help search purpose.
        /// This function will be called only once (usually this happens at the first time when
        /// end user request some help in the target help category).
        /// </remarks>
        internal virtual void LoadCache()
        {
        }
    }
}
