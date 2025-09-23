// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    
    /// <remarks>
    /// Instances of <see cref="RuntimeDefinedParameterDictionary"/>
    /// should be returned to cmdlet implementations of
    /// <see cref="IDynamicParameters.GetDynamicParameters"/>.
    ///
    /// It is permitted to subclass <see cref="RuntimeDefinedParameter"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    /// <seealso cref="RuntimeDefinedParameterDictionary"/>
    /// <seealso cref="IDynamicParameters"/>
    /// <seealso cref="IDynamicParameters.GetDynamicParameters"/>
    public class RuntimeDefinedParameter
    {
        
        public RuntimeDefinedParameter()
        {
        }

        
        /// <param name="name">
        /// The name of the parameter. This cannot be null or empty.
        /// </param>
        /// <param name="parameterType">
        /// The type of the parameter value. Arguments will be coerced to this type before binding.
        /// This parameter cannot be null.
        /// </param>
        /// <param name="attributes">
        /// Any parameter attributes that should be on the parameter. This can be any of the
        /// parameter attributes including but not limited to Validate*Attribute, ExpandWildcardAttribute, etc.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="parameterType"/> is null.
        /// </exception>
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

        
        /// <exception cref="ArgumentException">
        /// If <paramref name="value"/> is null or empty on set.
        /// </exception>
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

        
        /// <remarks>
        /// Arguments will be coerced to this type before being bound.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="value"/> is null.
        /// </exception>
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

        
        /// <remarks>
        /// If the value is set prior to parameter binding, the value will be
        /// reset before each pipeline object is processed.
        /// </remarks>
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

        
        /// <remarks>
        /// This can be any attribute that can be applied to a normal parameter.
        /// </remarks>
        public Collection<Attribute> Attributes { get; } = new Collection<Attribute>();

        
        internal bool IsDisabled()
        {
            bool hasParameterAttribute = false;
            bool hasEnabledParamAttribute = false;
            bool hasSeenExpAttribute = false;

            foreach (Attribute attr in Attributes)
            {
                if (!hasSeenExpAttribute && attr is ExperimentalAttribute expAttribute)
                {
                    if (expAttribute.ToHide)
                    {
                        return true;
                    }

                    hasSeenExpAttribute = true;
                }
                else if (attr is ParameterAttribute paramAttribute)
                {
                    hasParameterAttribute = true;
                    if (paramAttribute.ToHide)
                    {
                        continue;
                    }

                    hasEnabledParamAttribute = true;
                }
            }

            // If one or more parameter attributes are declared but none is enabled,
            // then we consider the parameter is disabled.
            return hasParameterAttribute && !hasEnabledParamAttribute;
        }
    }

    
    /// <remarks>
    /// Instances of <see cref="RuntimeDefinedParameterDictionary"/>
    /// should be returned to cmdlet implementations of
    /// <see cref="IDynamicParameters.GetDynamicParameters"/>.
    ///
    /// It is permitted to subclass <see cref="RuntimeDefinedParameterDictionary"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    /// <seealso cref="RuntimeDefinedParameter"/>
    /// <seealso cref="IDynamicParameters"/>
    /// <seealso cref="IDynamicParameters.GetDynamicParameters"/>
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
