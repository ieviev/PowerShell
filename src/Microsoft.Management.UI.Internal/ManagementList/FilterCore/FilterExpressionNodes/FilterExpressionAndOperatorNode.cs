// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterExpressionAndOperatorNode : FilterExpressionNode
    {
        #region Properties

        private List<FilterExpressionNode> children = new List<FilterExpressionNode>();

        
        public ICollection<FilterExpressionNode> Children
        {
            get
            {
                return this.children;
            }
        }

        #endregion Properties

        #region Ctor

        
        public FilterExpressionAndOperatorNode()
        {
            // empty
        }

        
        public FilterExpressionAndOperatorNode(IEnumerable<FilterExpressionNode> children)
        {
            ArgumentNullException.ThrowIfNull(children);

            this.children.AddRange(children);
        }

        #endregion Ctor

        #region Public Methods

        
        public override bool Evaluate(object item)
        {
            if (this.Children.Count == 0)
            {
                return false;
            }

            foreach (FilterExpressionNode node in this.Children)
            {
                if (!node.Evaluate(item))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion Public Methods
    }
}
