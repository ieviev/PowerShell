// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.



using System.Management.Automation.Language;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// This is public class for representing a powershell token.
    /// </summary>
    /// <remarks>
    /// There is already an internal class Token for representing the token.
    ///
    /// This class wraps the internal Token class for providing limited information
    /// to syntax editor.
    /// </remarks>
    public sealed class PSToken
    {
        internal PSToken(Token token)
        {
            Type = GetPSTokenType(token);
            _extent = token.Extent;
            if (token is StringToken)
            {
                _content = ((StringToken)token).Value;
            }
            else if (token is VariableToken)
            {
                _content = ((VariableToken)token).VariablePath.ToString();
            }
        }

        internal PSToken(IScriptExtent extent)
        {
            Type = PSTokenType.Position;
            _extent = extent;
        }

        /// <summary>
        /// Resulting text for the token.
        /// </summary>
        /// <remarks>
        /// The text here represents the content of token. It can be the same as
        /// the text chunk within script resulting into this token, but usually is not
        /// the case.
        ///
        /// For example, -name in following command result into a parameter token.
        ///
        ///     get-process -name foo
        ///
        /// Text property in this case is 'name' instead of '-name'.
        /// </remarks>
        public string Content
        {
            get
            {
                return _content ?? _extent.Text;
            }
        }

        private readonly string _content;

        #region Token Type

        /// <summary>
        /// Map a V3 token to a V2 PSTokenType.
        /// </summary>
        /// <param name="token">The V3 token.</param>
        /// <returns>The V2 PSTokenType.</returns>
        public static PSTokenType GetPSTokenType(Token token)
        {
            if ((token.TokenFlags & TokenFlags.CommandName) != 0)
            {
                return PSTokenType.Command;
            }

            if ((token.TokenFlags & TokenFlags.MemberName) != 0)
            {
                return PSTokenType.Member;
            }

            if ((token.TokenFlags & TokenFlags.AttributeName) != 0)
            {
                return PSTokenType.Attribute;
            }

            if ((token.TokenFlags & TokenFlags.TypeName) != 0)
            {
                return PSTokenType.Type;
            }

            return s_tokenKindMapping[(int)token.Kind];
        }

        /// <summary>
        /// Token type.
        /// </summary>
        public PSTokenType Type { get; }

        private static readonly PSTokenType[] s_tokenKindMapping = new PSTokenType[]
        {
            #region Flags for unclassified tokens

             PSTokenType.Unknown,
             PSTokenType.Variable,
             PSTokenType.Variable,
             PSTokenType.CommandParameter,
             PSTokenType.Number,
             PSTokenType.LoopLabel,
             PSTokenType.CommandArgument,

             PSTokenType.CommandArgument,
             PSTokenType.NewLine,
             PSTokenType.LineContinuation,
             PSTokenType.Comment,
             PSTokenType.Unknown,

            #endregion Flags for unclassified tokens

            #region Flags for strings

             PSTokenType.String,
             PSTokenType.String,
             PSTokenType.String,
             PSTokenType.String,

            #endregion Flags for strings

            #region Flags for punctuators

             PSTokenType.GroupStart,
             PSTokenType.GroupEnd,
             PSTokenType.GroupStart,
             PSTokenType.GroupEnd,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.GroupStart,
             PSTokenType.GroupStart,
             PSTokenType.GroupStart,
             PSTokenType.StatementSeparator,

            #endregion Flags for punctuators

            #region Flags for operators

             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Operator,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,
             PSTokenType.Unknown,

            #endregion Flags for operators

            #region Flags for keywords

             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,
             PSTokenType.Keyword,

            #endregion Flags for keywords

             PSTokenType.Unknown,
        };

        #endregion

        #region Position Information

        private readonly IScriptExtent _extent;

        /// <summary>
        /// Offset of token start in script buffer.
        /// </summary>
        public int Start
        {
            get { return _extent.StartOffset; }
        }

        /// <summary>
        /// Offset of token end in script buffer.
        /// </summary>
        public int Length
        {
            get
            {
                return _extent.EndOffset - _extent.StartOffset;
            }
        }

        /// <summary>
        /// Line number of token start.
        /// </summary>
        /// <remarks>
        /// StartLine, StartColumn, EndLine, and EndColumn are 1-based,
        /// i.e., first line has a line number 1 and first character in
        /// a line has column number 1.
        /// </remarks>
        public int StartLine { get { return _extent.StartLineNumber; } }

        /// <summary>
        /// Position of token start in start line.
        /// </summary>
        public int StartColumn { get { return _extent.StartColumnNumber; } }

        /// <summary>
        /// Line number of token end.
        /// </summary>
        public int EndLine { get { return _extent.EndLineNumber; } }

        /// <summary>
        /// Position of token end in end line.
        /// </summary>
        public int EndColumn { get { return _extent.EndColumnNumber; } }

        #endregion
    }

    /// <summary>
    /// PowerShell token types.
    /// </summary>
    public enum PSTokenType
    {
        /// <summary>
        /// Unknown token.
        /// </summary>
        Unknown,

        /// <summary>
        /// <para>
        /// Command.
        /// </para>
        /// </para>
        /// For example, 'get-process' in
        ///
        ///     <c><code>get-process -name foo</code></c>
        /// </para>
        /// </summary>
        Command,

        /// <summary>
        /// <para>
        /// Command Parameter.
        /// </para>
        /// <para>
        /// For example, '-name' in
        ///
        ///     <c><code>get-process -name foo</code></c>
        /// </para>
        /// </summary>
        CommandParameter,

        /// <summary>
        /// <para>
        /// Command Argument.
        /// </para>
        /// <para>
        /// For example, 'foo' in
        ///
        ///     <c><code>get-process -name foo</code></c>
        /// </para>
        /// </summary>
        CommandArgument,

        /// <summary>
        /// <para>
        /// Number.
        /// </para>
        /// <para>
        /// For example, 12 in
        ///
        ///     <c><code>$a=12</code></c>
        /// </para>
        /// </summary>
        Number,

        /// <summary>
        /// <para>
        /// String.
        /// </para>
        /// <para>
        /// For example, "12" in
        ///
        ///     <c><code>$a="12"</code></c>
        /// </para>
        /// </summary>
        String,

        /// <summary>
        /// <para>
        /// Variable.
        /// </para>
        /// <para>
        /// <remarks>
        /// For example, $a in
        ///
        ///     <c><code>$a="12"</code></c>
        /// <para>
        /// </summary>
        Variable,

        /// <summary>
        /// <para>
        /// Property name or method name.
        /// </para>
        /// <para>
        /// For example, Name in
        ///
        ///     <c><code>$a.Name</code></c>
        /// </para>
        /// </summary>
        Member,

        /// <summary>
        /// <para>
        /// Loop label.
        /// </para>
        /// <para>
        /// For example, :loop in
        ///
        /// <c><code>
        ///     :loop
        ///     foreach($a in $b)
        ///     {
        ///         $a
        ///     }
        /// </code></c>
        /// </summary>
        LoopLabel,

        /// <summary>
        /// <para>
        /// Attributes.
        /// </para>
        /// <para>
        /// For example, Mandatory in
        ///
        ///     <c><code>param([Mandatory] $a)</code></c>
        /// </para>
        /// </summary>
        Attribute,

        /// <summary>
        /// <para>
        /// Types.
        /// </para>
        /// <para>
        /// For example, [string] in
        ///
        ///     <c><code>$a = [string] 12</code></c>
        /// </para>
        /// </summary>
        Type,

        /// <summary>
        /// <para>
        /// Operators.
        /// </para>
        /// <para>
        /// For example, + in
        ///
        ///     <c><code>$a = 1 + 2</code></c>
        /// </para>
        /// </summary>
        Operator,

        /// <summary>
        /// <para>
        /// Group Starter.
        /// </para>
        /// <para>
        /// For example, { in
        ///
        /// <c><code>
        ///     if ($a -gt 4)
        ///     {
        ///         $a++;
        ///     }
        /// </code></c>
        /// </para>
        /// </summary>
        GroupStart,

        /// <summary>
        /// <para>
        /// Group Ender.
        /// </para>
        /// <para>
        /// For example, } in
        ///
        /// <c><code>
        ///     if ($a -gt 4)
        ///     {
        ///         $a++;
        ///     }
        /// </code></c>
        /// </para>
        /// </summary>
        GroupEnd,

        /// <summary>
        /// <para>
        /// Keyword.
        /// </para>
        /// <para>
        /// For example, if in
        ///
        /// <c><code>
        ///     if ($a -gt 4)
        ///     {
        ///         $a++;
        ///     }
        /// </code></c>
        /// </para>
        /// </summary>
        Keyword,

        /// <summary>
        /// <para>
        /// Comment.
        /// </para>
        /// <para>
        /// For example, #here in
        ///
        /// <c><code>
        ///     #here
        ///     if ($a -gt 4)
        ///     {
        ///         $a++;
        ///     }
        /// </code></c>
        /// </para>
        /// </summary>
        Comment,

        /// <summary>
        /// <para>
        /// Statement separator. This is ';'
        /// </para>
        /// <para>
        /// For example, ; in
        ///
        /// <c><code>
        ///     #here
        ///     if ($a -gt 4)
        ///     {
        ///         $a++;
        ///     }
        /// </code></c>
        /// </para>
        /// </summary>
        StatementSeparator,

        /// <summary>
        /// <para>
        /// New line. This is '\n'
        /// </para>
        /// <para>
        /// For example, \n in
        ///
        /// <c><code>
        ///     #here
        ///     if ($a -gt 4)
        ///     {
        ///         $a++;
        ///     }
        /// </code></c>
        /// </para>
        /// </summary>
        NewLine,

        /// <summary>
        /// <para>
        /// Line continuation.
        /// </para>
        /// <para>
        /// For example, ` in
        ///
        /// <c><code>
        ///     get-command -name `
        ///     foo
        /// </code></c>
        /// </para>
        /// </summary>
        LineContinuation,

        /// <summary>
        /// <para>
        /// Position token.
        /// </para>
        /// <para>
        /// Position tokens are bogus tokens generated for identifying a location
        /// in the script.
        /// </para>
        /// </summary>
        Position
    }
}
