// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    
    internal class AliasHelpProvider : HelpProvider
    {
        
        internal AliasHelpProvider(HelpSystem helpSystem) : base(helpSystem)
        {
            _sessionState = helpSystem.ExecutionContext.SessionState;
            _commandDiscovery = helpSystem.ExecutionContext.CommandDiscovery;
            _context = helpSystem.ExecutionContext;
        }

        private readonly ExecutionContext _context;

        
        private readonly SessionState _sessionState;

        
        private readonly CommandDiscovery _commandDiscovery;

        #region Common Properties

        
        internal override string Name
        {
            get
            {
                return "Alias Help Provider";
            }
        }

        
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.Alias;
            }
        }

        #endregion

        #region Help Provider Interface

        
        internal override IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            CommandInfo commandInfo = null;

            try
            {
                commandInfo = _commandDiscovery.LookupCommandInfo(helpRequest.Target);
            }
            catch (CommandNotFoundException)
            {
                // CommandNotFoundException is expected here if target doesn't match any
                // commandlet. Just ignore this exception and bail out.
            }

            if ((commandInfo != null) && (commandInfo.CommandType == CommandTypes.Alias))
            {
                AliasInfo aliasInfo = (AliasInfo)commandInfo;

                HelpInfo helpInfo = AliasHelpInfo.GetHelpInfo(aliasInfo);
                if (helpInfo != null)
                {
                    yield return helpInfo;
                }
            }
        }

        
        internal override IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent)
        {
            // aliases do not have help content...so doing nothing in that case
            if (!searchOnlyContent)
            {
                string target = helpRequest.Target;
                string pattern = target;
                var allAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!WildcardPattern.ContainsWildcardCharacters(target))
                {
                    pattern += "*";
                }

                WildcardPattern matcher = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                IDictionary<string, AliasInfo> aliasTable = _sessionState.Internal.GetAliasTable();

                foreach (string name in aliasTable.Keys)
                {
                    if (matcher.IsMatch(name))
                    {
                        HelpRequest exactMatchHelpRequest = helpRequest.Clone();
                        exactMatchHelpRequest.Target = name;
                        // Duplicates??
                        foreach (HelpInfo helpInfo in ExactMatchHelp(exactMatchHelpRequest))
                        {
                            // Component/Role/Functionality match is done only for SearchHelp
                            // as "get-help * -category alias" should not forward help to
                            // CommandHelpProvider..(ExactMatchHelp does forward help to
                            // CommandHelpProvider)
                            if (!Match(helpInfo, helpRequest))
                            {
                                continue;
                            }

                            if (allAliases.Contains(name))
                            {
                                continue;
                            }

                            allAliases.Add(name);

                            yield return helpInfo;
                        }
                    }
                }

                CommandSearcher searcher =
                        new CommandSearcher(
                            pattern,
                            SearchResolutionOptions.ResolveAliasPatterns, CommandTypes.Alias,
                            _context);

                while (searcher.MoveNext())
                {
                    CommandInfo current = ((IEnumerator<CommandInfo>)searcher).Current;

                    if (_context.CurrentPipelineStopping)
                    {
                        yield break;
                    }

                    AliasInfo alias = current as AliasInfo;

                    if (alias != null)
                    {
                        string name = alias.Name;
                        HelpRequest exactMatchHelpRequest = helpRequest.Clone();
                        exactMatchHelpRequest.Target = name;

                        // Duplicates??
                        foreach (HelpInfo helpInfo in ExactMatchHelp(exactMatchHelpRequest))
                        {
                            // Component/Role/Functionality match is done only for SearchHelp
                            // as "get-help * -category alias" should not forward help to
                            // CommandHelpProvider..(ExactMatchHelp does forward help to
                            // CommandHelpProvider)
                            if (!Match(helpInfo, helpRequest))
                            {
                                continue;
                            }

                            if (allAliases.Contains(name))
                            {
                                continue;
                            }

                            allAliases.Add(name);

                            yield return helpInfo;
                        }
                    }
                }

                foreach (CommandInfo current in ModuleUtils.GetMatchingCommands(pattern, _context, helpRequest.CommandOrigin))
                {
                    if (_context.CurrentPipelineStopping)
                    {
                        yield break;
                    }

                    AliasInfo alias = current as AliasInfo;

                    if (alias != null)
                    {
                        string name = alias.Name;

                        HelpInfo helpInfo = AliasHelpInfo.GetHelpInfo(alias);

                        if (allAliases.Contains(name))
                        {
                            continue;
                        }

                        allAliases.Add(name);

                        yield return helpInfo;
                    }
                }
            }
        }

        private static bool Match(HelpInfo helpInfo, HelpRequest helpRequest)
        {
            if (helpRequest == null)
                return true;

            if ((helpRequest.HelpCategory & helpInfo.HelpCategory) == 0)
            {
                return false;
            }

            if (!Match(helpInfo.Component, helpRequest.Component))
            {
                return false;
            }

            if (!Match(helpInfo.Role, helpRequest.Role))
            {
                return false;
            }

            if (!Match(helpInfo.Functionality, helpRequest.Functionality))
            {
                return false;
            }

            return true;
        }

        private static bool Match(string target, string[] patterns)
        {
            // patterns should never be null as shell never accepts
            // empty inputs. Keeping this check as a safe measure.
            if (patterns == null || patterns.Length == 0)
                return true;

            foreach (string pattern in patterns)
            {
                if (Match(target, pattern))
                {
                    // we have a match so return true
                    return true;
                }
            }

            // We dont have a match so far..so return false
            return false;
        }

        private static bool Match(string target, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;

            if (string.IsNullOrEmpty(target))
                target = string.Empty;

            WildcardPattern matcher = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);

            return matcher.IsMatch(target);
        }

        #endregion
    }
}
