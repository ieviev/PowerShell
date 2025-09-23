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

        
        internal sealed override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            if (!this.CacheFullyLoaded)
            {
                LoadCache();
            }

            this.CacheFullyLoaded = true;

            return base.ExactMatchHelp(helpRequest);
        }

        
        internal sealed override void DoExactMatchHelp(HelpRequest helpRequest)
        {
        }

        
        internal sealed override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            if (!this.CacheFullyLoaded)
            {
                LoadCache();
            }

            this.CacheFullyLoaded = true;

            return base.SearchHelp(helpRequest, searchOnlyContent);
        }

        
        internal sealed override IEnumerable<HelpInfo> DoSearchHelp(HelpRequest helpRequest)
        {
            return null;
        }

        
        internal virtual void LoadCache()
        {
        }
    }
}
