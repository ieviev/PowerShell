// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class FilterExpressionNode : IEvaluate
    {
        
        public abstract bool Evaluate(object item);

        
        public ICollection<T> FindAll<T>()
        {
            var ts = new List<T>();

            var operandNode = this as FilterExpressionOperandNode;
            if (operandNode != null)
            {
                if (typeof(T).IsInstanceOfType(operandNode.Rule))
                {
                    object obj = operandNode.Rule;

                    ts.Add((T)obj);
                }
            }

            var operatorAndNode = this as FilterExpressionAndOperatorNode;
            if (operatorAndNode != null)
            {
                foreach (var childNode in operatorAndNode.Children)
                {
                    ts.AddRange(childNode.FindAll<T>());
                }
            }

            var operatorOrNode = this as FilterExpressionOrOperatorNode;
            if (operatorOrNode != null)
            {
                foreach (var childNode in operatorOrNode.Children)
                {
                    ts.AddRange(childNode.FindAll<T>());
                }
            }

            return ts;
        }
    }
}
