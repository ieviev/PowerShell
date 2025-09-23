// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal delegate void RetryActionCallback<T>(T item);

    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal static class WpfHelp
    {
        #region RetryActionAfterLoaded
        private static Dictionary<FrameworkElement, RetryActionAfterLoadedDataQueue> retryActionData =
            new Dictionary<FrameworkElement, RetryActionAfterLoadedDataQueue>();

        
        public static bool RetryActionAfterLoaded<T>(FrameworkElement element, RetryActionCallback<T> callback, T parameter)
        {
            if (element.IsLoaded)
            {
                return false;
            }

            RetryActionAfterLoadedDataQueue data;
            if (!retryActionData.TryGetValue(element, out data))
            {
                data = new RetryActionAfterLoadedDataQueue();
                retryActionData.Add(element, data);
            }

            data.Enqueue(callback, parameter);

            element.Loaded += Element_Loaded;
            element.ApplyTemplate();

            return true;
        }

        private static void Element_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            element.Loaded -= Element_Loaded;

            RetryActionAfterLoadedDataQueue data;
            if (!retryActionData.TryGetValue(element, out data)
                || data.IsEmpty)
            {
                throw new InvalidOperationException("Event loaded callback data expected.");
            }

            Delegate callback;
            object parameter;
            data.Dequeue(out callback, out parameter);

            if (data.IsEmpty)
            {
                retryActionData.Remove(element);
            }

            callback.DynamicInvoke(parameter);
        }

        private class RetryActionAfterLoadedDataQueue
        {
            private Queue<Delegate> callbacks = new Queue<Delegate>();
            private Queue<object> parameters = new Queue<object>();

            
            public void Enqueue(Delegate callback, object parameter)
            {
                this.callbacks.Enqueue(callback);
                this.parameters.Enqueue(parameter);
            }

            
            public void Dequeue(out Delegate callback, out object parameter)
            {
                callback = null;
                parameter = null;

                if (this.callbacks.Count < 1)
                {
                    throw new InvalidOperationException("Trying to remove when there is no data");
                }

                callback = this.callbacks.Dequeue();
                parameter = this.parameters.Dequeue();
            }

            
            public bool IsEmpty
            {
                get
                {
                    return this.callbacks.Count == 0;
                }
            }
        }
        #endregion RetryActionAfterLoaded

        #region RemoveFromParent/AddChild
        
        public static void RemoveFromParent(FrameworkElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            // If the element has already been detached, do nothing \\
            if (element.Parent == null)
            {
                return;
            }

            ContentControl parentContentControl = element.Parent as ContentControl;

            if (parentContentControl != null)
            {
                parentContentControl.Content = null;
                return;
            }

            var parentDecorator = element.Parent as Decorator;

            if (parentDecorator != null)
            {
                parentDecorator.Child = null;
                return;
            }

            ItemsControl parentItemsControl = element.Parent as ItemsControl;

            if (parentItemsControl != null)
            {
                parentItemsControl.Items.Remove(element);
                return;
            }

            Panel parentPanel = element.Parent as Panel;

            if (parentPanel != null)
            {
                parentPanel.Children.Remove(element);
                return;
            }

            var parentAdorner = element.Parent as UIElementAdorner;

            if (parentAdorner != null)
            {
                parentAdorner.Child = null;
                return;
            }

            throw new NotSupportedException("The specified value does not have a parent that supports removal.");
        }

        
        public static void AddChild(FrameworkElement parent, FrameworkElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            ArgumentNullException.ThrowIfNull(parent, nameof(element));

            ContentControl parentContentControl = parent as ContentControl;

            if (parentContentControl != null)
            {
                parentContentControl.Content = element;
                return;
            }

            var parentDecorator = parent as Decorator;

            if (parentDecorator != null)
            {
                parentDecorator.Child = element;
                return;
            }

            ItemsControl parentItemsControl = parent as ItemsControl;

            if (parentItemsControl != null)
            {
                parentItemsControl.Items.Add(element);
                return;
            }

            Panel parentPanel = parent as Panel;

            if (parentPanel != null)
            {
                parentPanel.Children.Add(element);
                return;
            }

            throw new NotSupportedException("The specified parent doesn't support children.");
        }
        #endregion RemoveFromParent/AddChild

        #region VisualChild
        
        public static T GetVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null)
            {
                return null;
            }

            var elementQueue = new Queue<DependencyObject>();
            elementQueue.Enqueue(obj);

            while (elementQueue.Count > 0)
            {
                var element = elementQueue.Dequeue();

                T item = element as T;
                if (item != null)
                {
                    return item;
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i);
                    elementQueue.Enqueue(child);
                }
            }

            return null;
        }

        
        public static List<T> FindVisualChildren<T>(DependencyObject obj)
            where T : DependencyObject
        {
            Debug.Assert(obj != null, "obj is null");

            ArgumentNullException.ThrowIfNull(obj);

            List<T> childrenOfType = new List<T>();

            // Recursively loop through children looking for children of type within their trees \\
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject childObj = VisualTreeHelper.GetChild(obj, i);
                T child = childObj as T;

                if (child != null)
                {
                    childrenOfType.Add(child);
                }
                else
                {
                    // Recurse \\
                    childrenOfType.AddRange(FindVisualChildren<T>(childObj));
                }
            }

            return childrenOfType;
        }
        #endregion VisualChild

        
        public static T FindVisualAncestorData<T>(this DependencyObject obj)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(obj);

            FrameworkElement parent = obj.FindVisualAncestor<FrameworkElement>();

            if (parent != null)
            {
                T data = parent.DataContext as T;

                if (data != null)
                {
                    return data;
                }
                else
                {
                    return parent.FindVisualAncestorData<T>();
                }
            }

            return null;
        }

        
        public static T FindVisualAncestor<T>(this DependencyObject @object) where T : class
        {
            ArgumentNullException.ThrowIfNull(@object, nameof(@object));

            DependencyObject parent = VisualTreeHelper.GetParent(@object);

            if (parent != null)
            {
                T parentObj = parent as T;

                if (parentObj != null)
                {
                    return parentObj;
                }

                return parent.FindVisualAncestor<T>();
            }

            return null;
        }

        
        public static bool TryExecute(this RoutedCommand command, object parameter, IInputElement target)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (command.CanExecute(parameter, target))
            {
                command.Execute(parameter, target);
                return true;
            }

            return false;
        }

        #region TemplateChild
        
        public static T GetOptionalTemplateChild<T>(Control templateParent, string childName) where T : FrameworkElement
        {
            ArgumentNullException.ThrowIfNull(templateParent);
            ArgumentException.ThrowIfNullOrEmpty(childName);

            object templatePart = templateParent.Template.FindName(childName, templateParent);
            T item = templatePart as T;

            if (item == null && templatePart != null)
            {
                HandleWrongTemplatePartType<T>(childName);
            }

            return item;
        }

        
        public static T GetTemplateChild<T>(Control templateParent, string childName) where T : FrameworkElement
        {
            T item = GetOptionalTemplateChild<T>(templateParent, childName);

            if (item == null)
            {
                HandleMissingTemplatePart<T>(childName);
            }

            return item;
        }

        
        private static void HandleWrongTemplatePartType<T>(string name)
        {
            throw new ApplicationException(string.Format(
                CultureInfo.CurrentCulture,
                "A template part with the name of '{0}' is not of type {1}.",
                name,
                typeof(T).Name));
        }

        
        public static void HandleMissingTemplatePart<T>(string name)
        {
            throw new ApplicationException(string.Format(
                CultureInfo.CurrentCulture,
                "A template part with the name of '{0}' and type of {1} was not found.",
                name,
                typeof(T).Name));
        }
        #endregion TemplateChild

        #region SetComponentResourceStyle
        
        public static void SetComponentResourceStyle<T>(FrameworkElement element, string keyName) where T : FrameworkElement
        {
            ComponentResourceKey styleKey = new ComponentResourceKey(typeof(T), keyName);
            element.Style = (Style)element.FindResource(styleKey);
        }
        #endregion SetComponentResourceStyle

        #region CreateRoutedPropertyChangedEventArgs
        
        public static RoutedPropertyChangedEventArgs<T> CreateRoutedPropertyChangedEventArgs<T>(DependencyPropertyChangedEventArgs propertyEventArgs)
        {
            RoutedPropertyChangedEventArgs<T> eventArgs = new RoutedPropertyChangedEventArgs<T>(
                                                                    (T)propertyEventArgs.OldValue,
                                                                    (T)propertyEventArgs.NewValue);

            return eventArgs;
        }

        
        public static RoutedPropertyChangedEventArgs<T> CreateRoutedPropertyChangedEventArgs<T>(DependencyPropertyChangedEventArgs propertyEventArgs, RoutedEvent routedEvent)
        {
            RoutedPropertyChangedEventArgs<T> eventArgs = new RoutedPropertyChangedEventArgs<T>(
                                                                    (T)propertyEventArgs.OldValue,
                                                                    (T)propertyEventArgs.NewValue,
                                                                    routedEvent);

            return eventArgs;
        }
        #endregion CreateRoutedPropertyChangedEventArgs

        #region ChangeIndex
        
        public static void ChangeIndex(ItemCollection items, object item, int newIndex)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (!items.Contains(item))
            {
                throw new ArgumentException("The specified item is not in the specified collection.", "item");
            }

            if (newIndex < 0 || newIndex > items.Count)
            {
                throw new ArgumentOutOfRangeException("newIndex", "The specified index is not valid for the specified collection.");
            }

            int oldIndex = items.IndexOf(item);

            // If the tile isn't moving, don't do anything \\
            if (newIndex == oldIndex)
            {
                return;
            }

            items.Remove(item);

            // If adding to the end, add instead of inserting \\
            if (newIndex > items.Count)
            {
                items.Add(item);
            }
            else
            {
                items.Insert(newIndex, item);
            }
        }
        #endregion ChangeIndex
    }
}
