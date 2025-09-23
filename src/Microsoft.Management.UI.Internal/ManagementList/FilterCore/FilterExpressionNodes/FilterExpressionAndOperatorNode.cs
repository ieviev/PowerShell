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

        
        /// <param name="children">
        /// A collection of children which will be added to the
        /// FilterExpressionAndOperatorNode's Children collection.
        /// </param>
        public FilterExpressionAndOperatorNode(IEnumerable<FilterExpressionNode> children)
        {
            ArgumentNullException.ThrowIfNull(children);

            this.children.AddRange(children);
        }

        #endregion Ctor

        #region Public Methods

        
        /// <param name="item">
        /// The item to evaluate against.
        /// </param>
        /// <returns>
        /// True if all FilterExpressionNode children evaluate to true,
        /// false otherwise.
        /// </returns>
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
