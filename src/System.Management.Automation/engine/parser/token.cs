// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Text;

namespace System.Management.Automation.Language
{
    
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum TokenKind
    {
        // When adding any new tokens, be sure to update the following tables:
        //     * TokenTraits.StaticTokenFlags
        //     * TokenTraits.Text

        #region Unclassified Tokens

        
        Unknown = 0,

        
        Variable = 1,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        SplattedVariable = 2,

        
        Parameter = 3,

        
        Number = 4,

        
        Label = 5,

        
        Identifier = 6,

        
        Generic = 7,

        
        NewLine = 8,

        
        LineContinuation = 9,

        
        Comment = 10,

        
        EndOfInput = 11,

        #endregion Unclassified Tokens

        #region Strings

        
        StringLiteral = 12,

        
        StringExpandable = 13,

        
        HereStringLiteral = 14,

        
        HereStringExpandable = 15,

        #endregion Strings

        #region Punctuators

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        LParen = 16,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        RParen = 17,

        
        LCurly = 18,

        
        RCurly = 19,

        
        LBracket = 20,

        
        RBracket = 21,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        AtParen = 22,

        
        AtCurly = 23,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        DollarParen = 24,

        
        Semi = 25,

        #endregion Punctuators

        #region Operators

        
        AndAnd = 26,

        
        OrOr = 27,

        
        Ampersand = 28,

        
        Pipe = 29,

        
        Comma = 30,

        
        MinusMinus = 31,

        
        PlusPlus = 32,

        
        DotDot = 33,

        
        ColonColon = 34,

        
        Dot = 35,

        
        Exclaim = 36,

        
        Multiply = 37,

        
        Divide = 38,

        
        Rem = 39,

        
        Plus = 40,

        
        Minus = 41,

        
        Equals = 42,

        
        PlusEquals = 43,

        
        MinusEquals = 44,

        
        MultiplyEquals = 45,

        
        DivideEquals = 46,

        
        RemainderEquals = 47,

        
        Redirection = 48,

        
        RedirectInStd = 49,

        
        Format = 50,

        
        Not = 51,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Bnot = 52,

        
        And = 53,

        
        Or = 54,

        
        Xor = 55,

        
        Band = 56,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Bor = 57,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Bxor = 58,

        
        Join = 59,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ieq = 60,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ine = 61,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ige = 62,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Igt = 63,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ilt = 64,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ile = 65,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ilike = 66,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Inotlike = 67,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Imatch = 68,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Inotmatch = 69,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ireplace = 70,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Icontains = 71,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Inotcontains = 72,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Iin = 73,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Inotin = 74,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Isplit = 75,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ceq = 76,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cne = 77,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cge = 78,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cgt = 79,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Clt = 80,

        
        Cle = 81,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Clike = 82,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cnotlike = 83,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cmatch = 84,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cnotmatch = 85,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Creplace = 86,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ccontains = 87,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cnotcontains = 88,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cin = 89,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cnotin = 90,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Csplit = 91,

        
        Is = 92,

        
        IsNot = 93,

        
        As = 94,

        
        PostfixPlusPlus = 95,

        
        PostfixMinusMinus = 96,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Shl = 97,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Shr = 98,

        
        Colon = 99,

        
        QuestionMark = 100,

        
        QuestionQuestionEquals = 101,

        
        QuestionQuestion = 102,

        
        QuestionDot = 103,

        
        QuestionLBracket = 104,

        #endregion Operators

        #region Keywords

        
        Begin = 119,

        
        Break = 120,

        
        Catch = 121,

        
        Class = 122,

        
        Continue = 123,

        
        Data = 124,

        
        Define = 125,

        
        Do = 126,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Dynamicparam = 127,

        
        Else = 128,

        
        ElseIf = 129,

        
        End = 130,

        
        Exit = 131,

        
        Filter = 132,

        
        Finally = 133,

        
        For = 134,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Foreach = 135,

        
        From = 136,

        
        Function = 137,

        
        If = 138,

        
        In = 139,

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Param = 140,

        
        Process = 141,

        
        Return = 142,

        
        Switch = 143,

        
        Throw = 144,

        
        Trap = 145,

        
        Try = 146,

        
        Until = 147,

        
        Using = 148,

        
        Var = 149,

        
        While = 150,

        
        Workflow = 151,

        
        Parallel = 152,

        
        Sequence = 153,

        
        InlineScript = 154,

        
        Configuration = 155,

        
        DynamicKeyword = 156,

        
        Public = 157,

        
        Private = 158,

        
        Static = 159,

        
        Interface = 160,

        
        Enum = 161,

        
        Namespace = 162,

        
        Module = 163,

        
        Type = 164,

        
        Assembly = 165,

        
        Command = 166,

        
        Hidden = 167,

        
        Base = 168,

        
        Default = 169,

        
        Clean = 170,

        #endregion Keywords
    }

    
    [Flags]
    public enum TokenFlags
    {
        
        None = 0x00000000,

        #region Precedence Values

        
        BinaryPrecedenceLogical = 0x1,

        
        BinaryPrecedenceBitwise = 0x2,

        
        BinaryPrecedenceComparison = 0x5,

        
        BinaryPrecedenceCoalesce = 0x7,

        
        BinaryPrecedenceAdd = 0x9,

        
        BinaryPrecedenceMultiply = 0xa,

        
        BinaryPrecedenceFormat = 0xc,

        
        BinaryPrecedenceRange = 0xd,

        #endregion Precedence Values

        
        BinaryPrecedenceMask = 0x0000000f,

        
        Keyword = 0x00000010,

        
        ScriptBlockBlockName = 0x00000020,

        
        BinaryOperator = 0x00000100,

        
        UnaryOperator = 0x00000200,

        
        CaseSensitiveOperator = 0x00000400,

        
        TernaryOperator = 0x00000800,

        
        SpecialOperator = 0x00001000,

        
        AssignmentOperator = 0x00002000,

        
        ParseModeInvariant = 0x00008000,

        
        TokenInError = 0x00010000,

        
        DisallowedInRestrictedMode = 0x00020000,

        
        PrefixOrPostfixOperator = 0x00040000,

        
        CommandName = 0x00080000,

        
        MemberName = 0x00100000,

        
        TypeName = 0x00200000,

        
        AttributeName = 0x00400000,

        
        // Some operators that could be marked with this flag aren't because the current implementation depends
        // on the execution context (e.g. -split, -join, -like, etc.)
        // If the operator is culture sensitive (e.g. -f), then it shouldn't be marked as suitable for constant
        // folding because evaluation of the operator could differ if the thread's culture changes.
        CanConstantFold = 0x00800000,

        
        StatementDoesntSupportAttributes = 0x01000000,
    }

    
    public static class TokenTraits
    {
        private static readonly TokenFlags[] s_staticTokenFlags = new TokenFlags[]
        {
            #region Flags for unclassified tokens

             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,

             TokenFlags.None,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,

            #endregion Flags for unclassified tokens

            #region Flags for strings

             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,

            #endregion Flags for strings

            #region Flags for punctuators

             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.None,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,

            #endregion Flags for punctuators

            #region Flags for operators

             TokenFlags.ParseModeInvariant,
             TokenFlags.ParseModeInvariant,
             TokenFlags.SpecialOperator | TokenFlags.ParseModeInvariant,
             TokenFlags.SpecialOperator | TokenFlags.ParseModeInvariant,
             TokenFlags.UnaryOperator | TokenFlags.ParseModeInvariant,
             TokenFlags.UnaryOperator | TokenFlags.PrefixOrPostfixOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.UnaryOperator | TokenFlags.PrefixOrPostfixOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceRange | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.SpecialOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.SpecialOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceMultiply | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceMultiply | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceMultiply | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceAdd | TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceAdd | TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
             TokenFlags.AssignmentOperator,
             TokenFlags.AssignmentOperator,
             TokenFlags.AssignmentOperator,
             TokenFlags.AssignmentOperator,
             TokenFlags.AssignmentOperator,
             TokenFlags.AssignmentOperator,
             TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.ParseModeInvariant | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceFormat | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
             TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceLogical | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceLogical | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceLogical | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceBitwise | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceBitwise | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceBitwise | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.UnaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.UnaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
             TokenFlags.BinaryOperator | TokenFlags.UnaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.UnaryOperator | TokenFlags.PrefixOrPostfixOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.UnaryOperator | TokenFlags.PrefixOrPostfixOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CanConstantFold,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CanConstantFold,
             TokenFlags.SpecialOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.TernaryOperator | TokenFlags.DisallowedInRestrictedMode,
           TokenFlags.AssignmentOperator,
             TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceCoalesce,
             TokenFlags.SpecialOperator | TokenFlags.DisallowedInRestrictedMode,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,
             TokenFlags.None,

            #endregion Flags for operators

            #region Flags for keywords

             TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword,
             TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,

            #endregion Flags for keywords
        };

        private static readonly string[] s_tokenText = new string[]
        {
            #region Text for unclassified tokens

             "unknown",
             "var",
             "@var",
             "param",
             "number",
             "label",
             "ident",

             "generic",
             "newline",
             "line continuation",
             "comment",
             "eof",

            #endregion Text for unclassified tokens

            #region Text for strings

             "sqstr",
             "dqstr",
             "sq here string",
             "dq here string",

            #endregion Text for strings

            #region Text for punctuators

             "(",
             ")",
             "{",
             "}",
             "[",
             "]",
             "@(",
             "@{",
             "$(",
             ";",

            #endregion Text for punctuators

            #region Text for operators

             "&&",
             "||",
             "&",
             "|",
             ",",
             "--",
             "++",
             "..",
             "::",
             ".",
             "!",
             "*",
             "/",
             "%",
             "+",
             "-",
             "=",
             "+=",
             "-=",
             "*=",
             "/=",
             "%=",
             "redirection",
             "<",
             "-f",
             "-not",
             "-bnot",
             "-and",
             "-or",
             "-xor",
             "-band",
             "-bor",
             "-bxor",
             "-join",
             "-eq",
             "-ne",
             "-ge",
             "-gt",
             "-lt",
             "-le",
             "-ilike",
             "-inotlike",
             "-imatch",
             "-inotmatch",
             "-ireplace",
             "-icontains",
             "-inotcontains",
             "-iin",
             "-inotin",
             "-isplit",
             "-ceq",
             "-cne",
             "-cge",
             "-cgt",
             "-clt",
             "-cle",
             "-clike",
             "-cnotlike",
             "-cmatch",
             "-cnotmatch",
             "-creplace",
             "-ccontains",
             "-cnotcontains",
             "-cin",
             "-cnotin",
             "-csplit",
             "-is",
             "-isnot",
             "-as",
             "++",
             "--",
             "-shl",
             "-shr",
             ":",
             "?",
           "??=",
             "??",
             "?.",
             "?[",
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,
             string.Empty,

            #endregion Text for operators

            #region Text for keywords

             "begin",
             "break",
             "catch",
             "class",
             "continue",
             "data",
             "define",
             "do",
             "dynamicparam",
             "else",
             "elseif",
             "end",
             "exit",
             "filter",
             "finally",
             "for",
             "foreach",
             "from",
             "function",
             "if",
             "in",
             "param",
             "process",
             "return",
             "switch",
             "throw",
             "trap",
             "try",
             "until",
             "using",
             "var",
             "while",
             "workflow",
             "parallel",
             "sequence",
             "inlinescript",
             "configuration",
             "<dynamic keyword>",
             "public",
             "private",
             "static",
             "interface",
             "enum",
             "namespace",
             "module",
             "type",
             "assembly",
             "command",
             "hidden",
             "base",
             "default",
             "clean",

            #endregion Text for keywords
        };

#if DEBUG
        static TokenTraits()
        {
            Diagnostics.Assert(
                s_staticTokenFlags.Length == ((int)TokenKind.Clean + 1),
                "Table size out of sync with enum - _staticTokenFlags");
            Diagnostics.Assert(
                s_tokenText.Length == ((int)TokenKind.Clean + 1),
                "Table size out of sync with enum - _tokenText");
            // Some random assertions to make sure the enum and the traits are in sync
            Diagnostics.Assert(GetTraits(TokenKind.Begin) == (TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName),
                               "Table out of sync with enum - flags Begin");
            Diagnostics.Assert(GetTraits(TokenKind.Workflow) == (TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes),
                               "Table out of sync with enum - flags Workflow");
            Diagnostics.Assert(GetTraits(TokenKind.Sequence) == (TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes),
                               "Table out of sync with enum - flags Sequence");
            Diagnostics.Assert(GetTraits(TokenKind.Shr) == (TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CanConstantFold),
                               "Table out of sync with enum - flags Shr");
            Diagnostics.Assert(s_tokenText[(int)TokenKind.Shr].Equals("-shr", StringComparison.OrdinalIgnoreCase),
                               "Table out of sync with enum - text Shr");
        }
#endif

        
        public static TokenFlags GetTraits(this TokenKind kind)
        {
            return s_staticTokenFlags[(int)kind];
        }

        
        public static bool HasTrait(this TokenKind kind, TokenFlags flag)
        {
            return (GetTraits(kind) & flag) != TokenFlags.None;
        }

        internal static int GetBinaryPrecedence(this TokenKind kind)
        {
            Diagnostics.Assert(HasTrait(kind, TokenFlags.BinaryOperator), "Token doesn't have binary precedence.");
            return (int)(s_staticTokenFlags[(int)kind] & TokenFlags.BinaryPrecedenceMask);
        }

        
        public static string Text(this TokenKind kind)
        {
            return s_tokenText[(int)kind];
        }
    }

    
    public class Token
    {
        private TokenKind _kind;
        private TokenFlags _tokenFlags;
        private readonly InternalScriptExtent _scriptExtent;

        internal Token(InternalScriptExtent scriptExtent, TokenKind kind, TokenFlags tokenFlags)
        {
            _scriptExtent = scriptExtent;
            _kind = kind;
            _tokenFlags = tokenFlags | kind.GetTraits();
        }

        internal void SetIsCommandArgument()
        {
            // Rather than expose the setter, we have an explicit method to change a token so that it is
            // considered a command argument.  This prevent arbitrary changes to the kind which should be safer.
            if (_kind != TokenKind.Identifier)
            {
                _kind = TokenKind.Generic;
            }
        }

        
        public string Text { get { return _scriptExtent.Text; } }

        
        public TokenFlags TokenFlags { get { return _tokenFlags; } internal set { _tokenFlags = value; } }

        
        public TokenKind Kind { get { return _kind; } }

        
        public bool HasError { get { return (_tokenFlags & TokenFlags.TokenInError) != 0; } }

        
        public IScriptExtent Extent { get { return _scriptExtent; } }

        
        public override string ToString()
        {
            return (_kind == TokenKind.EndOfInput) ? "<eof>" : Text;
        }

        internal virtual string ToDebugString(int indent)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{StringUtil.Padding(indent)}{_kind}: <{Text}>");
        }
    }

    
    public class NumberToken : Token
    {
        private readonly object _value;

        internal NumberToken(InternalScriptExtent scriptExtent, object value, TokenFlags tokenFlags)
            : base(scriptExtent, TokenKind.Number, tokenFlags)
        {
            _value = value;
        }

        internal override string ToDebugString(int indent)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}: <{2}> Value:<{3}> Type:<{4}>",
                StringUtil.Padding(indent),
                Kind,
                Text,
                _value,
                _value.GetType().Name);
        }

        
        public object Value { get { return _value; } }
    }

    
    public class ParameterToken : Token
    {
        private readonly string _parameterName;
        private readonly bool _usedColon;

        internal ParameterToken(InternalScriptExtent scriptExtent, string parameterName, bool usedColon)
            : base(scriptExtent, TokenKind.Parameter, TokenFlags.None)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(parameterName), "parameterName can't be null or empty");
            _parameterName = parameterName;
            _usedColon = usedColon;
        }

        
        public string ParameterName { get { return _parameterName; } }

        
        public bool UsedColon { get { return _usedColon; } }

        internal override string ToDebugString(int indent)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}: <-{2}{3}>",
                StringUtil.Padding(indent),
                Kind,
                _parameterName,
                _usedColon ? ":" : string.Empty);
        }
    }

    
    public class VariableToken : Token
    {
        internal VariableToken(InternalScriptExtent scriptExtent, VariablePath path, TokenFlags tokenFlags, bool splatted)
            : base(scriptExtent, splatted ? TokenKind.SplattedVariable : TokenKind.Variable, tokenFlags)
        {
            VariablePath = path;
        }

        
        public string Name { get { return VariablePath.UnqualifiedPath; } }

        
        public VariablePath VariablePath { get; }

        internal override string ToDebugString(int indent)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}: <{2}> Name:<{3}>",
                StringUtil.Padding(indent),
                Kind,
                Text,
                Name);
        }
    }

    
    public abstract class StringToken : Token
    {
        internal StringToken(InternalScriptExtent scriptExtent, TokenKind kind, TokenFlags tokenFlags, string value)
            : base(scriptExtent, kind, tokenFlags)
        {
            Value = value;
        }

        
        public string Value { get; }

        internal override string ToDebugString(int indent)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}: <{2}> Value:<{3}>",
                StringUtil.Padding(indent),
                Kind,
                Text,
                Value);
        }
    }

    
    public class StringLiteralToken : StringToken
    {
        internal StringLiteralToken(InternalScriptExtent scriptExtent, TokenFlags flags, TokenKind tokenKind, string value)
            : base(scriptExtent, tokenKind, flags, value)
        {
        }
    }

    
    public class StringExpandableToken : StringToken
    {
        private ReadOnlyCollection<Token> _nestedTokens;

        internal StringExpandableToken(InternalScriptExtent scriptExtent, TokenKind tokenKind, string value, string formatString, List<Token> nestedTokens, TokenFlags flags)
            : base(scriptExtent, tokenKind, flags, value)
        {
            if (nestedTokens != null && nestedTokens.Count > 0)
            {
                _nestedTokens = new ReadOnlyCollection<Token>(nestedTokens.ToArray());
            }

            FormatString = formatString;
        }

        internal static void ToDebugString(ReadOnlyCollection<Token> nestedTokens,
                                           StringBuilder sb, int indent)
        {
            Diagnostics.Assert(nestedTokens != null, "caller to verify");

            foreach (Token token in nestedTokens)
            {
                sb.Append(Environment.NewLine);
                sb.Append(token.ToDebugString(indent + 4));
            }
        }

        
        public ReadOnlyCollection<Token> NestedTokens
        {
            get { return _nestedTokens; }

            internal set { _nestedTokens = value; }
        }

        internal string FormatString { get; }

        internal override string ToDebugString(int indent)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.ToDebugString(indent));
            if (_nestedTokens != null)
            {
                ToDebugString(_nestedTokens, sb, indent);
            }

            return sb.ToString();
        }
    }

    
    public class LabelToken : Token
    {
        internal LabelToken(InternalScriptExtent scriptExtent, TokenFlags tokenFlags, string labelText)
            : base(scriptExtent, TokenKind.Label, tokenFlags)
        {
            LabelText = labelText;
        }

        
        public string LabelText { get; }
    }

    
    public abstract class RedirectionToken : Token
    {
        internal RedirectionToken(InternalScriptExtent scriptExtent, TokenKind kind)
            : base(scriptExtent, kind, TokenFlags.None)
        {
        }
    }

    
    public class InputRedirectionToken : RedirectionToken
    {
        internal InputRedirectionToken(InternalScriptExtent scriptExtent)
            : base(scriptExtent, TokenKind.RedirectInStd)
        {
        }
    }

    
    public class MergingRedirectionToken : RedirectionToken
    {
        internal MergingRedirectionToken(InternalScriptExtent scriptExtent, RedirectionStream from, RedirectionStream to)
            : base(scriptExtent, TokenKind.Redirection)
        {
            this.FromStream = from;
            this.ToStream = to;
        }

        
        public RedirectionStream FromStream { get; }

        
        public RedirectionStream ToStream { get; }
    }

    
    public class FileRedirectionToken : RedirectionToken
    {
        internal FileRedirectionToken(InternalScriptExtent scriptExtent, RedirectionStream from, bool append)
            : base(scriptExtent, TokenKind.Redirection)
        {
            this.FromStream = from;
            this.Append = append;
        }

        
        public RedirectionStream FromStream { get; }

        
        public bool Append { get; }
    }

    internal class UnscannedSubExprToken : StringLiteralToken
    {
        internal UnscannedSubExprToken(InternalScriptExtent scriptExtent, TokenFlags tokenFlags, string value, BitArray skippedCharOffsets)
            : base(scriptExtent, tokenFlags, TokenKind.StringLiteral, value)
        {
            this.SkippedCharOffsets = skippedCharOffsets;
        }

        internal BitArray SkippedCharOffsets { get; }
    }
}
