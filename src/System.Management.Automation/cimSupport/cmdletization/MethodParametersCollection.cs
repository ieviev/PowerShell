// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;

namespace Microsoft.PowerShell.Cmdletization
{
    
    internal sealed class MethodParametersCollection : KeyedCollection<string, MethodParameter>
    {
        
        public MethodParametersCollection()
            : base(StringComparer.Ordinal, 5)
        {
        }

        
        /// <param name="item"></param>
        /// <returns></returns>
        protected override string GetKeyForItem(MethodParameter item)
        {
            return item.Name;
        }
    }
}
