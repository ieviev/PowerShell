// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.



using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    
    //
    //  1. Design
    //
    //  PSParser class is a public wrapper class of internal Parser class. It is mail goal
    //  is to provide a public interface for parsing a script into a collection of tokens.
    //
    //  Design of this class is made up of two parts,
    //
    //      1. interface part: which implement the static public interface for parsing a script.
    //      2. logic part: which implement the parsing logic for parsing.
    //
    //  2. Interface
    //
    //  The only public interface provided by this class is the static member
    //
    //     static Collection<PSToken> Parse(string script, out Collection<PSParseError> errors)
    //
    //  3. Parsing Logic
    //
    //  Script parsing is done through instances of PSParser object. Each PSParser object
    //  wraps an internal Parser object. It is PSParser object's responsibility to
    //      a. setup local runspace and retrieve internal Parser object from it.
    //      b. call internal parser for actual parsing
    //      c. translate parsing result from internal Token and RuntimeException type
    //         into public PSToken and PSParseError type.
    //
    public sealed class PSParser
    {
        
        private PSParser()
        {
        }

        #region Parsing Logic

        private readonly List<Language.Token> _tokenList = new List<Language.Token>();
        private Language.ParseError[] _errors;

        private void Parse(string script)
        {
            try
            {
                var parser = new Language.Parser { ProduceV2Tokens = true };
                parser.Parse(null, script, _tokenList, out _errors, ParseMode.Default);
            }
            catch (Exception)
            {
            }
        }

        
        private Collection<PSToken> Tokens
        {
            get
            {
                Collection<PSToken> resultTokens = new Collection<PSToken>();
                // Skip the last token, it's always EOF.
                for (int i = 0; i < _tokenList.Count - 1; i++)
                {
                    var token = _tokenList[i];
                    resultTokens.Add(new PSToken(token));
                }

                return resultTokens;
            }
        }

        
        private Collection<PSParseError> Errors
        {
            get
            {
                Collection<PSParseError> resultErrors = new Collection<PSParseError>();
                foreach (var error in _errors)
                {
                    resultErrors.Add(new PSParseError(error));
                }

                return resultErrors;
            }
        }

        #endregion

        #region Public API

        
        public static Collection<PSToken> Tokenize(string script, out Collection<PSParseError> errors)
        {
            if (script == null)
                throw PSTraceSource.NewArgumentNullException(nameof(script));

            PSParser psParser = new PSParser();

            psParser.Parse(script);
            errors = psParser.Errors;

            return psParser.Tokens;
        }

        
        public static Collection<PSToken> Tokenize(object[] script, out Collection<PSParseError> errors)
        {
            if (script == null)
                throw PSTraceSource.NewArgumentNullException(nameof(script));

            StringBuilder sb = new StringBuilder();
            foreach (object obj in script)
            {
                if (obj != null)
                {
                    sb.AppendLine(obj.ToString());
                }
            }

            return Tokenize(sb.ToString(), out errors);
        }

        #endregion
    }
}
