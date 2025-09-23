// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace System.Management.Automation
{
    
    public enum CompletionResultType
    {
        
        Text = 0,

        
        History = 1,

        
        Command = 2,

        
        ProviderItem = 3,

        
        ProviderContainer = 4,

        
        Property = 5,

        
        Method = 6,

        
        ParameterName = 7,

        
        ParameterValue = 8,

        
        Variable = 9,

        
        Namespace = 10,

        
        Type = 11,

        
        Keyword = 12,

        
        DynamicKeyword = 13,

        // If a new enum is added, there is a range test that uses DynamicKeyword for parameter validation
        // that needs to be updated to use the new enum.
        // We can't use a "MaxValue" enum because it's value would preclude ever adding a new enum.
    }

    
    public class CompletionResult
    {
        
        private readonly string _completionText;

        
        private readonly string _listItemText;

        
        private readonly string _toolTip;

        
        private readonly CompletionResultType _resultType;

        
        private static readonly CompletionResult s_nullInstance = new CompletionResult();

        
        public string CompletionText
        {
            get
            {
                if (this == s_nullInstance)
                {
                    throw PSTraceSource.NewInvalidOperationException(TabCompletionStrings.NoAccessToProperties);
                }

                return _completionText;
            }
        }

        
        public string ListItemText
        {
            get
            {
                if (this == s_nullInstance)
                {
                    throw PSTraceSource.NewInvalidOperationException(TabCompletionStrings.NoAccessToProperties);
                }

                return _listItemText;
            }
        }

        
        public CompletionResultType ResultType
        {
            get
            {
                if (this == s_nullInstance)
                {
                    throw PSTraceSource.NewInvalidOperationException(TabCompletionStrings.NoAccessToProperties);
                }

                return _resultType;
            }
        }

        
        public string ToolTip
        {
            get
            {
                if (this == s_nullInstance)
                {
                    throw PSTraceSource.NewInvalidOperationException(TabCompletionStrings.NoAccessToProperties);
                }

                return _toolTip;
            }
        }

        
        internal static CompletionResult Null
        {
            get { return s_nullInstance; }
        }

        
        /// <param name="completionText">The text to be used as the auto completion result.</param>
        /// <param name="listItemText">The text to be displayed in a list.</param>
        /// <param name="resultType">The type of completion result.</param>
        /// <param name="toolTip">The text for the tooltip with details to be displayed about the object.</param>
        public CompletionResult(string completionText, string listItemText, CompletionResultType resultType, string toolTip)
        {
            if (string.IsNullOrEmpty(completionText))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(completionText));
            }

            if (string.IsNullOrEmpty(listItemText))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(listItemText));
            }

            if (resultType < CompletionResultType.Text || resultType > CompletionResultType.DynamicKeyword)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(resultType), resultType);
            }

            if (string.IsNullOrEmpty(toolTip))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(toolTip));
            }

            _completionText = completionText;
            _listItemText = listItemText;
            _toolTip = toolTip;
            _resultType = resultType;
        }

        
        /// <param name="completionText">Completion text.</param>
        public CompletionResult(string completionText)
            : this(completionText, completionText, CompletionResultType.Text, completionText)
        {
        }

        
        /// <remarks>
        /// This can be used in argument completion, to indicate that the completion attempt has gone through the
        /// native command argument completion methods.
        /// </remarks>
        private CompletionResult() { }
    }
}
