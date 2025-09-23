// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class IsNotEmptyValidationRule : DataErrorInfoValidationRule
    {
        #region Properties

        private static readonly DataErrorInfoValidationResult EmptyValueResult = new DataErrorInfoValidationResult(false, null, string.Empty);

        #endregion Properties

        #region Public Methods

        
        public override DataErrorInfoValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null)
            {
                return EmptyValueResult;
            }

            Type t = value.GetType();

            if (typeof(string) == t)
            {
                return IsStringNotEmpty((string)value) ? DataErrorInfoValidationResult.ValidResult : EmptyValueResult;
            }
            else
            {
                return DataErrorInfoValidationResult.ValidResult;
            }
        }

        public override object DeepClone()
        {
            // Instance is stateless.
            // return this;
            return new IsNotEmptyValidationRule();
        }

        #endregion Public Methods

        internal static bool IsStringNotEmpty(string value)
        {
            return !(string.IsNullOrEmpty(value) || value.Trim().Length == 0);
        }
    }
}
