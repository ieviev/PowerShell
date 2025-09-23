// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;

namespace System.Management.Automation
{
    internal sealed class SessionStateScopeEnumerator : IEnumerator<SessionStateScope>, IEnumerable<SessionStateScope>
    {
        
        internal SessionStateScopeEnumerator(SessionStateScope scope)
        {
            Diagnostics.Assert(scope != null, "Caller to verify scope argument");
            _initialScope = scope;
        }

        
        public bool MoveNext()
        {
            // On the first call to MoveNext the enumerator should be before
            // the first scope in the lookup and then advance to the first
            // scope in the lookup

            _currentEnumeratedScope = _currentEnumeratedScope == null ? _initialScope : _currentEnumeratedScope.Parent;

            // If the current scope is the global scope there is nowhere else
            // to do the lookup, so return false.
            return (_currentEnumeratedScope != null);
        }

        
        public void Reset()
        {
            _currentEnumeratedScope = null;
        }

        
        SessionStateScope IEnumerator<SessionStateScope>.Current
        {
            get
            {
                if (_currentEnumeratedScope == null)
                {
                    throw PSTraceSource.NewInvalidOperationException();
                }

                return _currentEnumeratedScope;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return ((IEnumerator<SessionStateScope>)this).Current;
            }
        }

        
        System.Collections.Generic.IEnumerator<SessionStateScope> System.Collections.Generic.IEnumerable<SessionStateScope>.GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        public void Dispose()
        {
            Reset();
        }

        private readonly SessionStateScope _initialScope;
        private SessionStateScope _currentEnumeratedScope;
    }
}
