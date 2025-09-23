// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Language
{
    
    public static class CodeGeneration
    {
        
        /// <param name="value">The content to be included in a single-quoted string.</param>
        /// <returns>Content with all single-quotes escaped.</returns>
        public static string EscapeSingleQuotedStringContent(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                sb.Append(c);
                if (CharExtensions.IsSingleQuote(c))
                {
                    // double-up quotes to escape them
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        
        /// <param name="value">The content to be included in a block comment.</param>
        /// <returns>Content with all block comment characters escaped.</returns>
        public static string EscapeBlockCommentContent(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("<#", "<`#")
                .Replace("#>", "#`>");
        }

        
        /// <param name="value">The content to be included in a format string.</param>
        /// <returns>Content with all curly braces escaped.</returns>
        public static string EscapeFormatStringContent(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                sb.Append(c);
                if (CharExtensions.IsCurlyBracket(c))
                {
                    // double-up curly brackets to escape them
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        
        /// <param name="value">The content to be included as a variable name.</param>
        /// <returns>Content with all curly braces and back-ticks escaped.</returns>
        public static string EscapeVariableName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("`", "``")
                .Replace("}", "`}")
                .Replace("{", "`{");
        }
    }
}
