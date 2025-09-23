// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    internal sealed partial class SessionStateInternal
    {
        #region cmdlets

        
        internal CmdletInfo GetCmdlet(string cmdletName)
        {
            return GetCmdlet(cmdletName, CommandOrigin.Internal);
        }

        
        internal CmdletInfo GetCmdlet(string cmdletName, CommandOrigin origin)
        {
            CmdletInfo result = null;
            if (string.IsNullOrEmpty(cmdletName))
            {
                return null;
            }

            // Use the scope enumerator to find the alias using the
            // appropriate scoping rules

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                result = scope.GetCmdlet(cmdletName);

                if (result != null)
                {
                    // Now check the visibility of the cmdlet...
                    SessionState.ThrowIfNotVisible(origin, result);

                    // Make sure the cmdlet isn't private or if it is that the current
                    // scope is the same scope the cmdlet was retrieved from.

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

        
        internal CmdletInfo GetCmdletAtScope(string cmdletName, string scopeID)
        {
            CmdletInfo result = null;
            if (string.IsNullOrEmpty(cmdletName))
            {
                return null;
            }

            SessionStateScope scope = GetScopeByID(scopeID);
            result = scope.GetCmdlet(cmdletName);

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

        
        internal IDictionary<string, List<CmdletInfo>> GetCmdletTable()
        {
            Dictionary<string, List<CmdletInfo>> result =
                new Dictionary<string, List<CmdletInfo>>(StringComparer.OrdinalIgnoreCase);

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                foreach (KeyValuePair<string, List<CmdletInfo>> entry in scope.CmdletTable)
                {
                    if (!result.ContainsKey(entry.Key))
                    {
                        // Make sure the cmdlet isn't private or if it is that the current
                        // scope is the same scope the alias was retrieved from.

                        List<CmdletInfo> toBeAdded = new List<CmdletInfo>();
                        foreach (CmdletInfo cmdletInfo in entry.Value)
                        {
                            if ((cmdletInfo.Options & ScopedItemOptions.Private) == 0 ||
                                scope == _currentScope)
                            {
                                toBeAdded.Add(cmdletInfo);
                            }
                        }

                        result.Add(entry.Key, toBeAdded);
                    }
                }
            }

            return result;
        }

        
        internal IDictionary<string, List<CmdletInfo>> GetCmdletTableAtScope(string scopeID)
        {
            Dictionary<string, List<CmdletInfo>> result =
                new Dictionary<string, List<CmdletInfo>>(StringComparer.OrdinalIgnoreCase);

            SessionStateScope scope = GetScopeByID(scopeID);

            foreach (KeyValuePair<string, List<CmdletInfo>> entry in scope.CmdletTable)
            {
                // Make sure the alias isn't private or if it is that the current
                // scope is the same scope the alias was retrieved from.
                List<CmdletInfo> toBeAdded = new List<CmdletInfo>();
                foreach (CmdletInfo cmdletInfo in entry.Value)
                {
                    if ((cmdletInfo.Options & ScopedItemOptions.Private) == 0 ||
                        scope == _currentScope)
                    {
                        toBeAdded.Add(cmdletInfo);
                    }
                }

                result.Add(entry.Key, toBeAdded);
            }

            return result;
        }

        internal void RemoveCmdlet(string name, int index, bool force)
        {
            RemoveCmdlet(name, index, force, CommandOrigin.Internal);
        }

        
        internal void RemoveCmdlet(string name, int index, bool force, CommandOrigin origin)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            // Use the scope enumerator to find an existing function

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                CmdletInfo cmdletInfo =
                    scope.GetCmdlet(name);

                if (cmdletInfo != null)
                {
                    // Make sure the cmdlet isn't private or if it is that the current
                    // scope is the same scope the cmdlet was retrieved from.

                    if ((cmdletInfo.Options & ScopedItemOptions.Private) != 0 &&
                        scope != _currentScope)
                    {
                        cmdletInfo = null;
                    }
                    else
                    {
                        scope.RemoveCmdlet(name, index, force);
                        break;
                    }
                }
            }
        }

        
        internal void RemoveCmdletEntry(string name, bool force)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            // Use the scope enumerator to find an existing function

            SessionStateScopeEnumerator scopeEnumerator =
                new SessionStateScopeEnumerator(_currentScope);

            foreach (SessionStateScope scope in scopeEnumerator)
            {
                CmdletInfo cmdletInfo =
                    scope.GetCmdlet(name);

                if (cmdletInfo != null)
                {
                    // Make sure the cmdlet isn't private or if it is that the current
                    // scope is the same scope the cmdlet was retrieved from.

                    if ((cmdletInfo.Options & ScopedItemOptions.Private) != 0 &&
                        scope != _currentScope)
                    {
                        cmdletInfo = null;
                    }
                    else
                    {
                        scope.RemoveCmdletEntry(name, force);
                        break;
                    }
                }
            }
        }

        #endregion cmdlets
    }
}
