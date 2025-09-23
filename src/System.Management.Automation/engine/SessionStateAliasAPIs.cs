// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    internal sealed partial class SessionStateInternal
    {
        #region aliases

        
        internal void AddSessionStateEntry(SessionStateAliasEntry entry, string scopeID)
        {
            AliasInfo alias = new AliasInfo(entry.Name, entry.Definition, this.ExecutionContext, entry.Options)
            {
                Visibility = entry.Visibility,
                Module = entry.Module,
                Description = entry.Description
            };

            // Create alias in the global scope...
            this.SetAliasItemAtScope(alias, scopeID, true, CommandOrigin.Internal);
        }

        
        internal IDictionary<string, AliasInfo> GetAliasTable()
        {
            // On 7.0 version we have 132 aliases so we set a larger number to reduce re-allocations.
            const int InitialAliasCount = 150;
            Dictionary<string, AliasInfo> result =
                new Dictionary<string, AliasInfo>(InitialAliasCount, StringComparer.OrdinalIgnoreCase);

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                foreach (AliasInfo entry in scope.AliasTable)
                {
                    if (!result.ContainsKey(entry.Name))
                    {
                        // Make sure the alias isn't private or if it is that the current
                        // scope is the same scope the alias was retrieved from.

                        if ((entry.Options & ScopedItemOptions.Private) == 0 ||
                            scope == _currentScope)
                        {
                            result.Add(entry.Name, entry);
                        }
                    }
                }
            }

            return result;
        }

        
        internal IDictionary<string, AliasInfo> GetAliasTableAtScope(string scopeID)
        {
            Dictionary<string, AliasInfo> result =
                new Dictionary<string, AliasInfo>(StringComparer.OrdinalIgnoreCase);

            SessionStateScope scope = GetScopeByID(scopeID);

            foreach (AliasInfo entry in scope.AliasTable)
            {
                // Make sure the alias isn't private or if it is that the current
                // scope is the same scope the alias was retrieved from.

                if ((entry.Options & ScopedItemOptions.Private) == 0 ||
                    scope == _currentScope)
                {
                    result.Add(entry.Name, entry);
                }
            }

            return result;
        }

        
        internal List<AliasInfo> ExportedAliases { get; } = new List<AliasInfo>();

        
        internal AliasInfo GetAlias(string aliasName, CommandOrigin origin)
        {
            AliasInfo result = null;
            if (string.IsNullOrEmpty(aliasName))
            {
                return null;
            }

            // Use the scope enumerator to find the alias using the
            // appropriate scoping rules

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                result = scope.GetAlias(aliasName);

                if (result != null)
                {
                    // Now check the visibility of the variable...
                    SessionState.ThrowIfNotVisible(origin, result);

                    // Make sure the alias isn't private or if it is that the current
                    // scope is the same scope the alias was retrieved from.

                    if ((result.Options & ScopedItemOptions.Private) != 0 &&
                        scope != _currentScope)
                    {
                        result = null;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        
        internal AliasInfo GetAlias(string aliasName)
        {
            return GetAlias(aliasName, CommandOrigin.Internal);
        }

        
        internal AliasInfo GetAliasAtScope(string aliasName, string scopeID)
        {
            AliasInfo result = null;
            if (string.IsNullOrEmpty(aliasName))
            {
                return null;
            }

            SessionStateScope scope = GetScopeByID(scopeID);
            result = scope.GetAlias(aliasName);

            // Make sure the alias isn't private or if it is that the current
            // scope is the same scope the alias was retrieved from.

            if (result != null &&
                (result.Options & ScopedItemOptions.Private) != 0 &&
                 scope != _currentScope)
            {
                result = null;
            }

            return result;
        }

        
        internal AliasInfo SetAliasValue(string aliasName, string value, bool force, CommandOrigin origin)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw PSTraceSource.NewArgumentException(nameof(aliasName));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw PSTraceSource.NewArgumentException(nameof(value));
            }

            AliasInfo info = _currentScope.SetAliasValue(aliasName, value, this.ExecutionContext, force, origin);

            return info;
        }

        
        internal AliasInfo SetAliasValue(string aliasName, string value, bool force)
        {
            return SetAliasValue(aliasName, value, force, CommandOrigin.Internal);
        }

        
        internal AliasInfo SetAliasValue(
            string aliasName,
            string value,
            ScopedItemOptions options,
            bool force,
            CommandOrigin origin)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw PSTraceSource.NewArgumentException(nameof(aliasName));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw PSTraceSource.NewArgumentException(nameof(value));
            }

            AliasInfo info = _currentScope.SetAliasValue(aliasName, value, options, this.ExecutionContext, force, origin);

            return info;
        }

        
        internal AliasInfo SetAliasValue(
            string aliasName,
            string value,
            ScopedItemOptions options,
            bool force)
        {
            return SetAliasValue(aliasName, value, options, force, CommandOrigin.Internal);
        }

        
        internal AliasInfo SetAliasItem(AliasInfo alias, bool force, CommandOrigin origin)
        {
            if (alias == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(alias));
            }

            AliasInfo info = _currentScope.SetAliasItem(alias, force, origin);

            return info;
        }

        
        internal AliasInfo SetAliasItemAtScope(AliasInfo alias, string scopeID, bool force, CommandOrigin origin)
        {
            if (alias == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(alias));
            }

            // If the "private" scope was specified, make sure the options contain
            // the Private flag

            if (string.Equals(scopeID, StringLiterals.Private, StringComparison.OrdinalIgnoreCase))
            {
                alias.Options |= ScopedItemOptions.Private;
            }

            SessionStateScope scope = GetScopeByID(scopeID);

            AliasInfo info = scope.SetAliasItem(alias, force, origin);

            return info;
        }

        
        internal AliasInfo SetAliasItemAtScope(AliasInfo alias, string scopeID, bool force)
        {
            return SetAliasItemAtScope(alias, scopeID, force, CommandOrigin.Internal);
        }

        
        internal void RemoveAlias(string aliasName, bool force)
        {
            if (string.IsNullOrEmpty(aliasName))
            {
                throw PSTraceSource.NewArgumentException(nameof(aliasName));
            }

            // Use the scope enumerator to find an existing function

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                AliasInfo alias =
                    scope.GetAlias(aliasName);

                if (alias != null)
                {
                    // Make sure the alias isn't private or if it is that the current
                    // scope is the same scope the alias was retrieved from.

                    if ((alias.Options & ScopedItemOptions.Private) != 0 &&
                        scope != _currentScope)
                    {
                        alias = null;
                    }
                    else
                    {
                        scope.RemoveAlias(aliasName, force);

                        break;
                    }
                }
            }
        }

        
        internal IEnumerable<string> GetAliasesByCommandName(string command)
        {
            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                foreach (string alias in scope.GetAliasesByCommandName(command))
                {
                    yield return alias;
                }
            }

            yield break;
        }

        #endregion aliases
    }
}
