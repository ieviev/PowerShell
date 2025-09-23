// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterExpressionOperandNode : FilterExpressionNode
    {
        #region Properties

        
        public FilterRule Rule
        {
            get;
            protected set;
        }

        #endregion Properties

        #region Ctor

        
        public FilterExpressionOperandNode(FilterRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            this.Rule = rule;
        }

        #endregion Ctor

        #region Public Methods

        
        public override bool Evaluate(object item)
        {
            Debug.Assert(this.Rule != null, "rule is not null");

            return this.Rule.Evaluate(item);
        }

        #endregion Public Methods
    }
}
