// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    #region UserActionState enum

    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public enum UserActionState
    {
        
        Enabled = 0,

        
        Disabled = 1,

        
        Hidden = 2,
    }

    #endregion

    #region ControlState enum

    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public enum ControlState
    {
        
        Ready = 0,

        
        Error = 1,

        
        Refreshing = 2,
    }

    #endregion

    #region Utilities class

    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public static class Utilities
    {
        
        /// <typeparam name="T">The type to verify.</typeparam>
        /// <param name="items">The items to check.</param>
        /// <returns>Whether all of the items in <paramref name="items"/> are of type T.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public static bool AreAllItemsOfType<T>(IEnumerable items)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (object item in items)
            {
                if (item is not T)
                {
                    return false;
                }
            }

            return true;
        }

        
        /// <typeparam name="T">The type of the item to find.</typeparam>
        /// <param name="items">The <see cref="IEnumerable"/> to search.</param>
        /// <returns>The first element that matches the specified type, if found; otherwise, the default value for type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public static T Find<T>(this IEnumerable items)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (object item in items)
            {
                if (item is T)
                {
                    return (T)item;
                }
            }

            return default(T);
        }

        
        /// <param name="value">String to Trim.</param>
        /// <returns>Trimmed string.</returns>
        public static string NullCheckTrim(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.Trim();
            }

            return value;
        }

        // A separate copy of ResortObservableCollection is in ADMUX Utility.cs

        
        /// <typeparam name="T">
        /// Type of <paramref name="modify"/>.
        /// </typeparam>
        /// <param name="modify">
        /// ObservableCollection to resort to order of
        /// <paramref name="sorted"/>.
        /// </param>
        /// <param name="sorted">
        /// Order to which <paramref name="modify"/> should be resorted.
        /// All enumerated objects must be of type T.
        /// </param>
        /// <remarks>
        /// Parameter <paramref name="sorted"/> is not generic to type T
        /// since it may be a collection of a subclass of type T,
        /// and IEnumerable'subclass is not compatible with
        /// IEnumerable'baseclass.
        /// </remarks>
        public static void ResortObservableCollection<T>(
            ObservableCollection<T> modify,
            IEnumerable sorted)
        {
            int orderedPosition = 0;
            foreach (T obj in sorted)
            {
                T sortedObject = (T)obj;
                int foundIndex = modify.IndexOf(sortedObject);
                if (foundIndex >= 0)
                {
                    modify.Move(foundIndex, orderedPosition);
                    orderedPosition++;
                    if (modify.Count <= orderedPosition)
                    {
                        // All objects present are in the original order
                        break;
                    }
                }
            }
        }
    }

    #endregion
}
