// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class SearchBox : Control, IFilterExpressionProvider
    {
        private SearchTextParser parser;

        
        public SearchBox()
        {
            // This constructor intentionally left blank
        }

        #region IFilterExpressionProvider Implementation

        
        public FilterExpressionNode FilterExpression
        {
            get
            {
                return SearchBox.ConvertToFilterExpression(this.Parser.Parse(this.Text));
            }
        }

        
        public bool HasFilterExpression
        {
            get
            {
                return string.IsNullOrEmpty(this.Text) == false;
            }
        }

        
        public event EventHandler FilterExpressionChanged;

        
        protected virtual void NotifyFilterExpressionChanged()
        {
            EventHandler eh = this.FilterExpressionChanged;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

        #endregion

        
        public SearchTextParser Parser
        {
            get
            {
                if (this.parser == null)
                {
                    this.parser = new SearchTextParser();
                }

                return this.parser;
            }

            set
            {
                ArgumentNullException.ThrowIfNull(value);

                this.parser = value;
            }
        }

        partial void OnTextChangedImplementation(PropertyChangedEventArgs<string> e)
        {
            this.NotifyFilterExpressionChanged();
        }

        partial void OnClearTextCanExecuteImplementation(CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.HasFilterExpression;
        }

        partial void OnClearTextExecutedImplementation(ExecutedRoutedEventArgs e)
        {
            this.Text = string.Empty;
        }

        
        protected static FilterExpressionNode ConvertToFilterExpression(ICollection<SearchTextParseResult> searchBoxItems)
        {
            ArgumentNullException.ThrowIfNull(searchBoxItems);

            if (searchBoxItems.Count == 0)
            {
                return null;
            }
            else
            {
                FilterExpressionAndOperatorNode filterExpression = new FilterExpressionAndOperatorNode();

                foreach (SearchTextParseResult item in searchBoxItems)
                {
                    filterExpression.Children.Add(new FilterExpressionOperandNode(item.FilterRule));
                }

                return filterExpression;
            }
        }
    }
}
