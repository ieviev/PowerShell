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

        
        /// <remarks>
        /// This hashtable is made case-insensitive so that helpInfo can be retrieved case insensitively.
        /// </remarks>
        private readonly Hashtable _helpCache = new Hashtable(StringComparer.OrdinalIgnoreCase);

        
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>The HelpInfo found. Null if nothing is found.</returns>
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

        
        /// <value></value>
        protected bool HasCustomMatch { get; set; } = false;

        
        /// <param name="target">Target to search.</param>
        /// <param name="key">Key used in cache table.</param>
        /// <returns></returns>
        protected virtual bool CustomMatch(string target, string key)
        {
            return target == key;
        }

        
        /// <remarks>
        /// Derived class can choose to either override ExactMatchHelp method to DoExactMatchHelp method.
        /// If ExactMatchHelp is overridden, initial cache checking will be disabled by default.
        /// If DoExactMatchHelp is overridden, cache check will be done first in ExactMatchHelp before the
        /// logic in DoExactMatchHelp is in place.
        /// </remarks>
        /// <param name="helpRequest">Help request object.</param>
        internal virtual void DoExactMatchHelp(HelpRequest helpRequest)
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

        
        /// <param name="target">Target string.</param>
        /// <returns>Wild card pattern created.</returns>
        internal virtual string GetWildCardPattern(string target)
        {
            if (WildcardPattern.ContainsWildcardCharacters(target))
                return target;

            return "*" + target + "*";
        }

        
        /// <remarks>
        /// Child class can choose to override SearchHelp of DoSearchHelp depending on
        /// whether it want to reuse the logic in SearchHelp for this class.
        /// </remarks>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>A collection of help info objects.</returns>
        internal virtual IEnumerable<HelpInfo> DoSearchHelp(HelpRequest helpRequest)
        {
            yield break;
        }

        
        /// <param name="target">The key of the help entry.</param>
        /// <param name="helpInfo">HelpInfo object as the value of the help entry.</param>
        internal void AddCache(string target, HelpInfo helpInfo)
        {
            _helpCache[target] = helpInfo;
        }

        
        /// <param name="target">The key for the help entry to retrieve.</param>
        /// <returns>The HelpInfo in cache corresponding the key specified.</returns>
        internal HelpInfo GetCache(string target)
        {
            return (HelpInfo)_helpCache[target];
        }

        
        /// <value></value>
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
