// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DataErrorInfoValidationResult : ValidationResult
    {
        #region Properties

        
        public bool IsUserVisible
        {
            get;
            private set;
        }

        
        public string ErrorMessage
        {
            get;
            private set;
        }

        private static readonly DataErrorInfoValidationResult valid = new DataErrorInfoValidationResult(true, null, string.Empty);

        
        public static new DataErrorInfoValidationResult ValidResult
        {
            get
            {
                return valid;
            }
        }

        #endregion Properties

        #region Ctor

        
        public DataErrorInfoValidationResult(bool isValid, object errorContent, string errorMessage)
            : base(isValid, errorContent)
        {
            this.IsUserVisible = !string.IsNullOrEmpty(errorMessage);
            this.ErrorMessage = errorMessage ?? string.Empty;
        }

        #endregion Ctor
    }
}
