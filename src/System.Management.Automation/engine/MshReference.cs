// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Dynamic;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;

namespace System.Management.Automation
{
    
    public class PSReference
    {
        private object _value;

        
        public PSReference(object value)
        {
            _value = value;
        }

        
        public object Value
        {
            get
            {
                PSVariable variable = _value as PSVariable;

                if (variable != null)
                {
                    return variable.Value;
                }

                return _value;
            }

            set
            {
                PSVariable variable = _value as PSVariable;

                if (variable != null)
                {
                    variable.Value = value;
                    return;
                }

                _value = value;
            }
        }

        internal static readonly CallSite<Func<CallSite, object, object, object>> CreatePsReferenceInstance =
                CallSite<Func<CallSite, object, object, object>>.Create(PSCreateInstanceBinder.Get(new CallInfo(1), null));

        internal static PSReference CreateInstance(object value, Type typeOfValue)
        {
            Type psReferType = typeof(PSReference<>).MakeGenericType(typeOfValue);
            return (PSReference)CreatePsReferenceInstance.Target.Invoke(CreatePsReferenceInstance, psReferType, value);
        }
    }

    internal class PSReference<T> : PSReference
    {
        public PSReference(object value) : base(value)
        {
        }
    }
}
