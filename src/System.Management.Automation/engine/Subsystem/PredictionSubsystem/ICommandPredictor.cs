// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Threading;

namespace System.Management.Automation.Subsystem.Prediction
{
    
    public interface ICommandPredictor : ISubsystem
    {
        
        Dictionary<string, string>? ISubsystem.FunctionsToDefine => null;

        
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="context">The <see cref="PredictionContext"/> object to be used for prediction.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the prediction.</param>
        /// <returns>An instance of <see cref="SuggestionPackage"/>.</returns>
        SuggestionPackage GetSuggestion(PredictionClient client, PredictionContext context, CancellationToken cancellationToken);

        
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="feedback">A specific type of feedback.</param>
        /// <returns>True or false, to indicate whether the specific feedback is accepted.</returns>
        bool CanAcceptFeedback(PredictionClient client, PredictorFeedbackKind feedback) => false;

        
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="session">The mini-session where the displayed suggestions came from.</param>
        /// <param name="countOrIndex">
        /// When the value is greater than 0, it's the number of displayed suggestions from the list returned in <paramref name="session"/>, starting from the index 0.
        /// When the value is less than or equal to 0, it means a single suggestion from the list got displayed, and the index is the absolute value.
        /// </param>
        void OnSuggestionDisplayed(PredictionClient client, uint session, int countOrIndex) { }

        
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="session">Represents the mini-session where the accepted suggestion came from.</param>
        /// <param name="acceptedSuggestion">The accepted suggestion text.</param>
        void OnSuggestionAccepted(PredictionClient client, uint session, string acceptedSuggestion) { }

        
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="history">History command lines provided as references for prediction.</param>
        void OnCommandLineAccepted(PredictionClient client, IReadOnlyList<string> history) { }

        
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="commandLine">The last accepted command line.</param>
        /// <param name="success">Shows whether the execution was successful.</param>
        void OnCommandLineExecuted(PredictionClient client, string commandLine, bool success) { }
    }

    
    public enum PredictorFeedbackKind
    {
        
        SuggestionDisplayed,

        
        SuggestionAccepted,

        
        CommandLineAccepted,

        
        CommandLineExecuted,
    }

    
    public enum PredictionClientKind
    {
        
        Terminal,

        
        Editor,
    }

    
    public sealed class PredictionClient
    {
        
        public string Name { get; }

        
        public PredictionClientKind Kind { get; }

        
        public PathInfo? CurrentLocation { get; set; }

        
        /// <param name="name">Name of the interactive client.</param>
        /// <param name="kind">Kind of the interactive client.</param>
        public PredictionClient(string name, PredictionClientKind kind)
        {
            Name = name;
            Kind = kind;
        }
    }

    
    public sealed class PredictionContext
    {
        
        public Ast InputAst { get; }

        
        public IReadOnlyList<Token> InputTokens { get; }

        
        public IScriptPosition CursorPosition { get; }

        
        public Token? TokenAtCursor { get; }

        
        public IReadOnlyList<Ast> RelatedAsts { get; }

        
        /// <param name="inputAst">The <see cref="Ast"/> object from parsing the current command line input.</param>
        /// <param name="inputTokens">The <see cref="Token"/> objects from parsing the current command line input.</param>
        public PredictionContext(Ast inputAst, Token[] inputTokens)
        {
            ArgumentNullException.ThrowIfNull(inputAst);
            ArgumentNullException.ThrowIfNull(inputTokens);

            var cursor = inputAst.Extent.EndScriptPosition;
            var astContext = CompletionAnalysis.ExtractAstContext(inputAst, inputTokens, cursor);

            InputAst = inputAst;
            InputTokens = inputTokens;
            CursorPosition = cursor;
            TokenAtCursor = astContext.TokenAtCursor;
            RelatedAsts = astContext.RelatedAsts;
        }

        
        /// <param name="input">The user input.</param>
        /// <returns>A <see cref="PredictionContext"/> object.</returns>
        public static PredictionContext Create(string input)
        {
            ArgumentException.ThrowIfNullOrEmpty(input);

            Ast ast = Parser.ParseInput(input, out Token[] tokens, out _);
            return new PredictionContext(ast, tokens);
        }
    }

    
    public sealed class PredictiveSuggestion
    {
        
        public string SuggestionText { get; }

        
        public string? ToolTip { get; }

        
        /// <param name="suggestion">The predictive suggestion text.</param>
        public PredictiveSuggestion(string suggestion)
            : this(suggestion, toolTip: null)
        {
        }

        
        /// <param name="suggestion">The predictive suggestion text.</param>
        /// <param name="toolTip">The tooltip of the suggestion.</param>
        public PredictiveSuggestion(string suggestion, string? toolTip)
        {
            ArgumentException.ThrowIfNullOrEmpty(suggestion);

            SuggestionText = suggestion;
            ToolTip = toolTip;
        }
    }

    
    public struct SuggestionPackage
    {
        
        public uint? Session { get; }

        
        public List<PredictiveSuggestion>? SuggestionEntries { get; }

        
        /// <param name="suggestionEntries">The suggestions to return.</param>
        public SuggestionPackage(List<PredictiveSuggestion> suggestionEntries)
        {
            Requires.NotNullOrEmpty(suggestionEntries, nameof(suggestionEntries));

            Session = null;
            SuggestionEntries = suggestionEntries;
        }

        
        /// <param name="session">The mini-session where suggestions came from.</param>
        /// <param name="suggestionEntries">The suggestions to return.</param>
        public SuggestionPackage(uint session, List<PredictiveSuggestion> suggestionEntries)
        {
            Requires.NotNullOrEmpty(suggestionEntries, nameof(suggestionEntries));

            Session = session;
            SuggestionEntries = suggestionEntries;
        }
    }
}
