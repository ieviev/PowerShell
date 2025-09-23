// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public static class CustomTypeComparer
    {
        private static Dictionary<Type, object> comparers = new Dictionary<Type, object>();

        
        static CustomTypeComparer()
        {
            comparers.Add(typeof(DateTime), new DateTimeApproximationComparer());
        }

        
        public static int Compare<T>(T value1, T value2) where T : IComparable
        {
            IComparer<T> comparer;
            if (TryGetCustomComparer<T>(out comparer) == false)
            {
                return value1.CompareTo(value2);
            }

            return comparer.Compare(value1, value2);
        }

        private static bool TryGetCustomComparer<T>(out IComparer<T> comparer) where T : IComparable
        {
            comparer = null;

            object uncastComparer = null;
            if (comparers.TryGetValue(typeof(T), out uncastComparer) == false)
            {
                return false;
            }

            Debug.Assert(uncastComparer is IComparer<T>, "must be IComparer");
            comparer = (IComparer<T>)uncastComparer;

            return true;
        }
    }
}
