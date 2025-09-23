// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;

namespace Microsoft.PowerShell.Commands
{
    
    public class PSPropertyExpressionResult
    {
        
        public PSPropertyExpressionResult(object res, PSPropertyExpression re, Exception e)
        {
            Result = res;
            ResolvedExpression = re;
            Exception = e;
        }

        
        public object Result { get; } = null;

        
        public PSPropertyExpression ResolvedExpression { get; } = null;

        
        public Exception Exception { get; } = null;
    }

    
    public class PSPropertyExpression
    {
        
        public PSPropertyExpression(string s)
            : this(s, false)
        {
        }

        
        public PSPropertyExpression(string s, bool isResolved)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(s));
            }

            _stringValue = s;
            _isResolved = isResolved;
        }

        
        public PSPropertyExpression(ScriptBlock scriptBlock)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(scriptBlock));
            }

            Script = scriptBlock;
        }

        
        public ScriptBlock Script { get; } = null;

        
        public override string ToString()
        {
            if (Script != null)
                return Script.ToString();

            return _stringValue;
        }

        
        public List<PSPropertyExpression> ResolveNames(PSObject target)
        {
            return ResolveNames(target, true);
        }

        
        public bool HasWildCardCharacters
        {
            get
            {
                if (Script != null)
                    return false;
                return WildcardPattern.ContainsWildcardCharacters(_stringValue);
            }
        }

        
        public List<PSPropertyExpression> ResolveNames(PSObject target, bool expand)
        {
            List<PSPropertyExpression> retVal = new List<PSPropertyExpression>();

            if (_isResolved)
            {
                retVal.Add(this);
                return retVal;
            }

            if (Script != null)
            {
                // script block, just add it to the list and be done
                PSPropertyExpression ex = new PSPropertyExpression(Script);

                ex._isResolved = true;
                retVal.Add(ex);
                return retVal;
            }

            // If the object passed in is a hashtable, then turn it into a PSCustomObject so
            // that property expressions can work on it.
            var wrappedTarget = IfHashtableWrapAsPSCustomObject(target, out bool wasHashtable);

            // we have a string value
            IEnumerable<PSMemberInfo> members;
            if (HasWildCardCharacters)
            {
                // get the members first: this will expand the globbing on each parameter
                members = wrappedTarget.Members.Match(
                    _stringValue,
                    PSMemberTypes.Properties | PSMemberTypes.PropertySet | PSMemberTypes.Dynamic);

                // if target was a hashtable and no result is found from the keys, then use property value if available
                if (wasHashtable && !members.Any())
                {
                    members = target.Members.Match(
                        _stringValue,
                        PSMemberTypes.Properties | PSMemberTypes.PropertySet | PSMemberTypes.Dynamic);
                }
            }
            else
            {
                // we have no globbing: try an exact match, because this is quicker.
                PSMemberInfo x = wrappedTarget.Members[_stringValue];

                if (x == null)
                {
                    if (wasHashtable)
                    {
                        x = target.Members[_stringValue];
                    }
                    else if (wrappedTarget.BaseObject is System.Dynamic.IDynamicMetaObjectProvider)
                    {
                        // We could check if GetDynamicMemberNames includes the name...  but
                        // GetDynamicMemberNames is only a hint, not a contract, so we'd want
                        // to attempt the binding whether it's in there or not.
                        x = new PSDynamicMember(_stringValue);
                    }
                }

                List<PSMemberInfo> temp = new List<PSMemberInfo>();
                if (x != null)
                {
                    temp.Add(x);
                }

                members = temp;
            }

            // we now have a list of members, we have to expand property sets
            // and remove duplicates
            List<PSMemberInfo> temporaryMemberList = new List<PSMemberInfo>();

            foreach (PSMemberInfo member in members)
            {
                // it can be a property set
                if (member is PSPropertySet propertySet)
                {
                    if (expand)
                    {
                        // NOTE: we expand the property set under the
                        // assumption that it contains property names that
                        // do not require any further expansion
                        Collection<string> references = propertySet.ReferencedPropertyNames;

                        for (int j = 0; j < references.Count; j++)
                        {
                            ReadOnlyPSMemberInfoCollection<PSPropertyInfo> propertyMembers =
                                                target.Properties.Match(references[j]);
                            for (int jj = 0; jj < propertyMembers.Count; jj++)
                            {
                                temporaryMemberList.Add(propertyMembers[jj]);
                            }
                        }
                    }
                }
                // it can be a property
                else if (member is PSPropertyInfo)
                {
                    temporaryMemberList.Add(member);
                }
                // it can be a dynamic member
                else if (member is PSDynamicMember)
                {
                    temporaryMemberList.Add(member);
                }
            }

            var allMembers = new HashSet<string>();

            // build the list of unique values: remove the possible duplicates
            // from property set expansion
            foreach (PSMemberInfo m in temporaryMemberList)
            {
                if (!allMembers.Contains(m.Name))
                {
                    PSPropertyExpression ex = new PSPropertyExpression(m.Name);

                    ex._isResolved = true;
                    retVal.Add(ex);
                    allMembers.Add(m.Name);
                }
            }

            return retVal;
        }

        
        public List<PSPropertyExpressionResult> GetValues(PSObject target)
        {
            return GetValues(target, true, true);
        }

        
        public List<PSPropertyExpressionResult> GetValues(PSObject target, bool expand, bool eatExceptions)
        {
            List<PSPropertyExpressionResult> retVal = new List<PSPropertyExpressionResult>();

            // process the script case
            if (Script != null)
            {
                PSPropertyExpression scriptExpression = new PSPropertyExpression(Script);
                PSPropertyExpressionResult r = scriptExpression.GetValue(target, eatExceptions);
                retVal.Add(r);
                return retVal;
            }

            foreach (PSPropertyExpression resolvedName in ResolveNames(target, expand))
            {
                PSPropertyExpressionResult result = resolvedName.GetValue(target, eatExceptions);
                retVal.Add(result);
            }

            return retVal;
        }

        #region Private Members

        private CallSite<Func<CallSite, object, object>> _getValueDynamicSite;

        private PSPropertyExpressionResult GetValue(PSObject target, bool eatExceptions)
        {
            try
            {
                object result = null;

                if (Script != null)
                {
                    result = Script.DoInvokeReturnAsIs(
                        useLocalScope: true,
                        errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe,
                        dollarUnder: target,
                        input: AutomationNull.Value,
                        scriptThis: AutomationNull.Value,
                        args: Array.Empty<object>());
                }
                else
                {
                    _getValueDynamicSite ??=
                        CallSite<Func<CallSite, object, object>>.Create(
                            PSGetMemberBinder.Get(
                                _stringValue,
                                classScope: (Type)null,
                                @static: false));

                    result = _getValueDynamicSite.Target.Invoke(_getValueDynamicSite, target);
                }

                return new PSPropertyExpressionResult(result, this, null);
            }
            catch (RuntimeException e)
            {
                if (eatExceptions)
                {
                    return new PSPropertyExpressionResult(null, this, e);
                }
                else
                {
                    throw;
                }
            }
        }

        private static PSObject IfHashtableWrapAsPSCustomObject(PSObject target, out bool wrapped)
        {
            wrapped = false;

            // If the object passed in is a hashtable, then turn it into a PSCustomObject so
            // that property expressions can work on it.
            if (PSObject.Base(target) is Hashtable targetAsHash)
            {
                wrapped = true;
                return (PSObject)(LanguagePrimitives.ConvertPSObjectToType(
                    targetAsHash,
                    typeof(PSObject),
                    recursion: false,
                    formatProvider: null,
                    ignoreUnknownMembers: true));
            }

            return target;
        }

        // private members
        private readonly string _stringValue;
        private bool _isResolved = false;

        #endregion Private Members
    }
}
