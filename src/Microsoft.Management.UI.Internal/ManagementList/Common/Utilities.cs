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

        
        public static string NullCheckTrim(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.Trim();
            }

            return value;
        }

        // A separate copy of ResortObservableCollection is in ADMUX Utility.cs

        
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
