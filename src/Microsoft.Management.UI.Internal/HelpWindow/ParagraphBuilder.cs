// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    
    internal class ParagraphBuilder : INotifyPropertyChanged
    {
        
        private readonly List<TextSpan> boldSpans;

        
        private readonly List<TextSpan> highlightedSpans;

        
        private readonly StringBuilder textBuilder;

        
        private readonly Paragraph paragraph;

        
        /// <param name="paragraph">Paragraph we will be adding lines to in BuildParagraph.</param>
        internal ParagraphBuilder(Paragraph paragraph)
        {
            ArgumentNullException.ThrowIfNull(paragraph);

            this.paragraph = paragraph;
            this.boldSpans = new List<TextSpan>();
            this.highlightedSpans = new List<TextSpan>();
            this.textBuilder = new StringBuilder();
        }

        #region INotifyPropertyChanged Members
        
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        
        internal int HighlightCount
        {
            get { return this.highlightedSpans.Count; }
        }

        
        internal Paragraph Paragraph
        {
            get { return this.paragraph; }
        }

        
        internal void BuildParagraph()
        {
            this.paragraph.Inlines.Clear();

            int currentBoldIndex = 0;
            TextSpan? currentBoldSpan = this.boldSpans.Count == 0 ? (TextSpan?)null : this.boldSpans[0];
            int currentHighlightedIndex = 0;
            TextSpan? currentHighlightedSpan = this.highlightedSpans.Count == 0 ? (TextSpan?)null : this.highlightedSpans[0];

            bool currentBold = false;
            bool currentHighlighted = false;

            StringBuilder sequence = new StringBuilder();
            int i = 0;
            foreach (char c in this.textBuilder.ToString())
            {
                bool newBold = false;
                bool newHighlighted = false;

                ParagraphBuilder.MoveSpanToPosition(ref currentBoldIndex, ref currentBoldSpan, i, this.boldSpans);
                newBold = currentBoldSpan == null ? false : currentBoldSpan.Value.Contains(i);

                ParagraphBuilder.MoveSpanToPosition(ref currentHighlightedIndex, ref currentHighlightedSpan, i, this.highlightedSpans);
                newHighlighted = currentHighlightedSpan == null ? false : currentHighlightedSpan.Value.Contains(i);

                if (newBold != currentBold || newHighlighted != currentHighlighted)
                {
                    ParagraphBuilder.AddInline(this.paragraph, currentBold, currentHighlighted, sequence);
                }

                sequence.Append(c);

                currentHighlighted = newHighlighted;
                currentBold = newBold;
                i++;
            }

            ParagraphBuilder.AddInline(this.paragraph, currentBold, currentHighlighted, sequence);
        }

        
        /// <param name="search">Search string.</param>
        /// <param name="caseSensitive">True if search should be case sensitive.</param>
        /// <param name="wholeWord">True if we should search whole word only.</param>
        internal void HighlightAllInstancesOf(string search, bool caseSensitive, bool wholeWord)
        {
            this.highlightedSpans.Clear();

            if (search == null || search.Trim().Length == 0)
            {
                this.BuildParagraph();
                this.OnNotifyPropertyChanged("HighlightCount");
                return;
            }

            string text = this.textBuilder.ToString();
            StringComparison comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int start = 0;
            int match;
            while ((match = text.IndexOf(search, start, comparison)) != -1)
            {
                // false loop
                do
                {
                    if (wholeWord)
                    {
                        if (match > 0 && char.IsLetterOrDigit(text[match - 1]))
                        {
                            break;
                        }

                        if ((match + search.Length <= text.Length - 1) && char.IsLetterOrDigit(text[match + search.Length]))
                        {
                            break;
                        }
                    }

                    this.AddHighlight(match, search.Length);
                }
                while (false);

                start = match + search.Length;
            }

            this.BuildParagraph();
            this.OnNotifyPropertyChanged("HighlightCount");
        }

        
        /// <param name="str">Text to be added.</param>
        /// <param name="bold">True if the text should be bold.</param>
        internal void AddText(string str, bool bold)
        {
            ArgumentNullException.ThrowIfNull(str);

            if (str.Length == 0)
            {
                return;
            }

            if (bold)
            {
                this.boldSpans.Add(new TextSpan(this.textBuilder.Length, str.Length));
            }

            this.textBuilder.Append(str);
        }

        
        internal void ResetAllText()
        {
            this.boldSpans.Clear();
            this.highlightedSpans.Clear();
            this.textBuilder.Clear();
        }

        
        /// <param name="currentParagraph">Paragraph to add Inline to.</param>
        /// <param name="currentBold">True if text should be added in bold.</param>
        /// <param name="currentHighlighted">True if the text should be added with highlight.</param>
        /// <param name="sequence">The text to add and clear.</param>
        private static void AddInline(Paragraph currentParagraph, bool currentBold, bool currentHighlighted, StringBuilder sequence)
        {
            if (sequence.Length == 0)
            {
                return;
            }

            Run run = new Run(sequence.ToString());
            if (currentHighlighted)
            {
                run.Background = ParagraphSearcher.HighlightBrush;
            }

            Inline inline = currentBold ? (Inline)new Bold(run) : run;
            currentParagraph.Inlines.Add(inline);
            sequence.Clear();
        }

        
        /// <param name="currentSpanIndex">Current index within <paramref name="allSpans"/>.</param>
        /// <param name="currentSpan">Current span within <paramref name="allSpans"/>.</param>
        /// <param name="caracterPosition">Character position. This comes from a position within this.textBuilder.</param>
        /// <param name="allSpans">The collection of spans. This is either this.boldSpans or this.highlightedSpans.</param>
        private static void MoveSpanToPosition(ref int currentSpanIndex, ref TextSpan? currentSpan, int caracterPosition, List<TextSpan> allSpans)
        {
            if (currentSpan == null || caracterPosition <= currentSpan.Value.End)
            {
                return;
            }

            for (int newBoldIndex = currentSpanIndex + 1; newBoldIndex < allSpans.Count; newBoldIndex++)
            {
                TextSpan newBoldSpan = allSpans[newBoldIndex];
                if (caracterPosition <= newBoldSpan.End)
                {
                    currentSpanIndex = newBoldIndex;
                    currentSpan = newBoldSpan;
                    return;
                }
            }

            // there is no span ending ahead of current position, so
            // we set the current span to null to prevent unnecessary comparisons against the currentSpan
            currentSpan = null;
        }

        
        /// <param name="start">Highlight start.</param>
        /// <param name="length">Highlight length.</param>
        private void AddHighlight(int start, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, this.textBuilder.Length, nameof(length));

            this.highlightedSpans.Add(new TextSpan(start, length));
        }

        
        /// <param name="propertyName">Property name.</param>
        private void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        
        internal struct TextSpan
        {
            
            private readonly int start;

            
            private readonly int end;

            
            /// <param name="start">Index of the first character in the span.</param>
            /// <param name="length">Index of the last character in the span.</param>
            internal TextSpan(int start, int length)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(start);
                ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);

                this.start = start;
                this.end = start + length - 1;
            }

            
            internal int Start
            {
                get { return this.start; }
            }

            
            internal int End
            {
                get
                {
                    return this.end;
                }
            }

            
            /// <param name="position">Position to verify if is in the span.</param>
            /// <returns>True if the <paramref name="position"/> is between start and end (inclusive).</returns>
            internal bool Contains(int position)
            {
                return (position >= this.start) && (position <= this.end);
            }
        }
    }
}
