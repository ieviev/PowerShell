// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;

namespace System.Management.Automation
{
    
    internal abstract class HelpProviderWithCache : HelpProvider
    {
        
        internal HelpProviderWithCache(HelpSystem helpSystem) : base(helpSystem)
        {
        }

        #region Help Provider Interface

        
        private readonly Hashtable _helpCache = new Hashtable(StringComparer.OrdinalIgnoreCase);

        
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            string target = helpRequest.Target;

            if (!this.HasCustomMatch)
            {
                if (_helpCache.Contains(target))
                {
                    yield return (HelpInfo)_helpCache[target];
                }
            }
            else
            {
                foreach (string key in _helpCache.Keys)
                {
                    if (CustomMatch(target, key))
                    {
                        yield return (HelpInfo)_helpCache[key];
                    }
                }
            }

            if (!this.CacheFullyLoaded)
            {
                DoExactMatchHelp(helpRequest);
                if (_helpCache.Contains(target))
                {
                    yield return (HelpInfo)_helpCache[target];
                }
            }
        }

        
        protected bool HasCustomMatch { get; set; } = false;

        
        protected virtual bool CustomMatch(string target, string key)
        {
            return target == key;
        }

        
        internal virtual void DoExactMatchHelp(HelpRequest helpRequest)
        {
        }

        
        internal override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            string target = helpRequest.Target;

            string wildcardpattern = GetWildCardPattern(target);

            HelpRequest searchHelpRequest = helpRequest.Clone();
            searchHelpRequest.Target = wildcardpattern;
            if (!this.CacheFullyLoaded)
            {
                IEnumerable<HelpInfo> result = DoSearchHelp(searchHelpRequest);
                if (result != null)
                {
                    foreach (HelpInfo helpInfoToReturn in result)
                    {
                        yield return helpInfoToReturn;
                    }
                }
            }
            else
            {
                int countOfHelpInfoObjectsFound = 0;
                WildcardPattern helpMatcher = WildcardPattern.Get(wildcardpattern, WildcardOptions.IgnoreCase);
                foreach (string key in _helpCache.Keys)
                {
                    if ((!searchOnlyContent && helpMatcher.IsMatch(key)) ||
                        (searchOnlyContent && ((HelpInfo)_helpCache[key]).MatchPatternInContent(helpMatcher)))
                    {
                        countOfHelpInfoObjectsFound++;
                        yield return (HelpInfo)_helpCache[key];
                        if (helpRequest.MaxResults > 0 && countOfHelpInfoObjectsFound >= helpRequest.MaxResults)
                        {
                            yield break;
                        }
                    }
                }
            }
        }

        
        internal virtual string GetWildCardPattern(string target)
        {
            if (WildcardPattern.ContainsWildcardCharacters(target))
                return target;

            return "*" + target + "*";
        }

        
        internal virtual IEnumerable<HelpInfo> DoSearchHelp(HelpRequest helpRequest)
        {
            yield break;
        }

        
        internal void AddCache(string target, HelpInfo helpInfo)
        {
            _helpCache[target] = helpInfo;
        }

        
        internal HelpInfo GetCache(string target)
        {
            return (HelpInfo)_helpCache[target];
        }

        
        protected internal bool CacheFullyLoaded { get; set; } = false;

        
        internal override void Reset()
        {
            base.Reset();

            _helpCache.Clear();
            CacheFullyLoaded = false;
        }

        #endregion
    }
}
