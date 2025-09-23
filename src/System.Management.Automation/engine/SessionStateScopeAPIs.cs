// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings
#pragma warning disable 56500

namespace System.Management.Automation
{
    
    internal sealed partial class SessionStateInternal
    {
        
        private SessionStateScope _currentScope;

        
        internal const string ScopeParameterName = "Scope";

        
        internal SessionStateScope GetScopeByID(string scopeID)
        {
            SessionStateScope result = _currentScope;

            if (!string.IsNullOrEmpty(scopeID))
            {
                if (string.Equals(
                        scopeID,
                        StringLiterals.Global,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result = GlobalScope;
                }
                else if (string.Equals(
                            scopeID,
                            StringLiterals.Local,
                            StringComparison.OrdinalIgnoreCase))
                {
                    result = _currentScope;
                }
                else if (string.Equals(
                            scopeID,
                            StringLiterals.Private,
                            StringComparison.OrdinalIgnoreCase))
                {
                    result = _currentScope;
                }
                else if (string.Equals(
                            scopeID,
                            StringLiterals.Script,
                            StringComparison.OrdinalIgnoreCase))
                {
                    // Get the current script scope from the stack.
                    result = _currentScope.ScriptScope;
                }
                else
                {
                    // Since the scope is not any of the special scopes
                    // try parsing it as an ID

                    try
                    {
                        int scopeNumericID = Int32.Parse(scopeID, System.Globalization.CultureInfo.CurrentCulture);

                        if (scopeNumericID < 0)
                        {
                            throw PSTraceSource.NewArgumentOutOfRangeException(ScopeParameterName, scopeID);
                        }

                        result = GetScopeByID(scopeNumericID) ?? _currentScope;
                    }
                    catch (FormatException)
                    {
                        throw PSTraceSource.NewArgumentException(ScopeParameterName, AutomationExceptions.InvalidScopeIdArgument, ScopeParameterName);
                    }
                    catch (OverflowException)
                    {
                        throw PSTraceSource.NewArgumentOutOfRangeException(ScopeParameterName, scopeID);
                    }
                }
            }

            return result;
        }

        
        internal SessionStateScope GetScopeByID(int scopeID)
        {
            SessionStateScope processingScope = _currentScope;
            int originalID = scopeID;

            while (scopeID > 0 && processingScope != null)
            {
                processingScope = processingScope.Parent;
                scopeID--;
            }

            if (processingScope == null && scopeID >= 0)
            {
                ArgumentOutOfRangeException outOfRange =
                    PSTraceSource.NewArgumentOutOfRangeException(
                        ScopeParameterName,
                        originalID,
                        SessionStateStrings.ScopeIDExceedsAvailableScopes,
                        originalID);
                throw outOfRange;
            }

            return processingScope;
        }

        
        internal SessionStateScope GlobalScope { get; }

        
        internal SessionStateScope ModuleScope { get; }

        
        internal SessionStateScope CurrentScope
        {
            get
            {
                return _currentScope;
            }

            set
            {
                Diagnostics.Assert(
                    value != null,
                    "A null scope should never be set");
#if DEBUG
                // This code is ifdef'd for DEBUG because it may pose a significant
                // performance hit and is only really required to validate our internal
                // code. There is no way anyone outside the Monad codebase can cause
                // these error conditions to be hit.

                // Need to make sure the new scope is in the global scope lineage

                SessionStateScope scope = value;
                bool inGlobalScopeLineage = false;

                while (scope != null)
                {
                    if (scope == GlobalScope)
                    {
                        inGlobalScopeLineage = true;
                        break;
                    }

                    scope = scope.Parent;
                }

                Diagnostics.Assert(
                    inGlobalScopeLineage,
                    "The scope specified to be set in CurrentScope is not in the global scope lineage. All scopes must originate from the global scope.");
#endif

                _currentScope = value;
            }
        }

        
        internal SessionStateScope ScriptScope { get { return _currentScope.ScriptScope; } }

        
        internal SessionStateScope NewScope(bool isScriptScope)
        {
            Diagnostics.Assert(
                _currentScope != null,
                "The currentScope should always be set.");

            // Create the new child scope.

            SessionStateScope newScope = new SessionStateScope(_currentScope);

            if (isScriptScope)
            {
                newScope.ScriptScope = newScope;
            }

            return newScope;
        }

        
        internal void RemoveScope(SessionStateScope scope)
        {
            Diagnostics.Assert(
                _currentScope != null,
                "The currentScope should always be set.");

            if (scope == GlobalScope)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            StringLiterals.Global,
                            SessionStateCategory.Scope,
                            "GlobalScopeCannotRemove",
                            SessionStateStrings.GlobalScopeCannotRemove);

                throw e;
            }

            // Give the provider a chance to cleanup the drive data associated
            // with drives in this scope

            foreach (PSDriveInfo drive in scope.Drives)
            {
                if (drive == null)
                {
                    continue;
                }

                CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

                // Call CanRemoveDrive to give the provider a chance to cleanup
                // but ignore the return value and exceptions

                try
                {
                    CanRemoveDrive(drive, context);
                }
                catch (LoopFlowException)
                {
                    throw;
                }
                catch (PipelineStoppedException)
                {
                    throw;
                }
                catch (ActionPreferenceStopException)
                {
                    throw;
                }
                catch (Exception) // Catch-all OK, 3rd party callout.
                {
                    // Ignore all exceptions from the provider as we are
                    // going to force the removal anyway
                }
            }

            scope.RemoveAllDrives();

            // If the scope being removed is the current scope,
            // then it must be removed from the tree.

            if (scope == _currentScope && _currentScope.Parent != null)
            {
                _currentScope = _currentScope.Parent;
            }

            scope.Parent = null;
        }
    }
}

#pragma warning restore 56500
