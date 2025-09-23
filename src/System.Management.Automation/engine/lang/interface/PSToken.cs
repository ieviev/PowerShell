// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.



using System.Management.Automation.Language;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    
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

        
        public string Content
        {
            get
            {
                return _content ?? _extent.Text;
            }
        }

        private readonly string _content;

        #region Token Type

        
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

        
        public int Start
        {
            get { return _extent.StartOffset; }
        }

        
        public int Length
        {
            get
            {
                return _extent.EndOffset - _extent.StartOffset;
            }
        }

        
        public int StartLine { get { return _extent.StartLineNumber; } }

        
        public int StartColumn { get { return _extent.StartColumnNumber; } }

        
        public int EndLine { get { return _extent.EndLineNumber; } }

        
        public int EndColumn { get { return _extent.EndColumnNumber; } }

        #endregion
    }

    
    public enum PSTokenType
    {
        
        Unknown,

        
        Command,

        
        CommandParameter,

        
        CommandArgument,

        
        Number,

        
        String,

        
        Variable,

        
        Member,

        
        LoopLabel,

        
        Attribute,

        
        Type,

        
        Operator,

        
        GroupStart,

        
        GroupEnd,

        
        Keyword,

        
        Comment,

        
        StatementSeparator,

        
        NewLine,

        
        LineContinuation,

        
        Position
    }
}
