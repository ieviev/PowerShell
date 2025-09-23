// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    
    public class RuntimeDefinedParameter
    {
        
        public RuntimeDefinedParameter()
        {
        }

        
        public RuntimeDefinedParameter(string name, Type parameterType, Collection<Attribute> attributes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            if (parameterType == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(parameterType));
            }

            _name = name;
            _parameterType = parameterType;

            if (attributes != null)
            {
                Attributes = attributes;
            }
        }

        
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException("name");
                }

                _name = value;
            }
        }

        private string _name = string.Empty;

        
        public Type ParameterType
        {
            get
            {
                return _parameterType;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                _parameterType = value;
            }
        }

        private Type _parameterType;

        
        public object Value
        {
            get
            {
                return _value;
            }

            set
            {
                this.IsSet = true;
                _value = value;
            }
        }

        private object _value;

        
        public bool IsSet { get; set; }

        
        public Collection<Attribute> Attributes { get; } = new Collection<Attribute>();

        
        internal bool IsDisabled()
        {
            bool hasParameterAttribute = false;
            bool hasEnabledParamAttribute = false;

            foreach (Attribute attr in Attributes)
            {
                if (attr is ParameterAttribute paramAttribute)
                {
                    hasParameterAttribute = true;
                    hasEnabledParamAttribute = true;
                }
            }

            // If one or more parameter attributes are declared but none is enabled,
            // then we consider the parameter is disabled.
            return hasParameterAttribute && !hasEnabledParamAttribute;
        }
    }

    
    public class RuntimeDefinedParameterDictionary : Dictionary<string, RuntimeDefinedParameter>
    {
        
        public RuntimeDefinedParameterDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        
        public string HelpFile
        {
            get { return _helpFile; }

            set { _helpFile = string.IsNullOrEmpty(value) ? string.Empty : value; }
        }

        private string _helpFile = string.Empty;

        
        public object Data { get; set; }

        internal static readonly RuntimeDefinedParameter[] EmptyParameterArray = Array.Empty<RuntimeDefinedParameter>();
    }
}
