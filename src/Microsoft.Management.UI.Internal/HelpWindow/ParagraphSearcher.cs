// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    
    internal class ParagraphSearcher
    {
        
        internal static readonly Brush HighlightBrush = Brushes.Yellow;

        
        private static readonly Brush CurrentHighlightBrush = Brushes.Cyan;

        
        private Run currentHighlightedMatch;

        
        internal ParagraphSearcher()
        {
        }

        
        internal Run MoveAndHighlightNextNextMatch(bool forward, TextPointer caretPosition)
        {
            Debug.Assert(caretPosition != null, "a caret position is always valid");
            Debug.Assert(caretPosition.Parent != null && caretPosition.Parent is Run, "a caret Parent is always a valid Run");
            Run caretRun = (Run)caretPosition.Parent;

            Run currentRun;

            if (this.currentHighlightedMatch != null)
            {
                // restore the curent highlighted background to plain highlighted
                this.currentHighlightedMatch.Background = ParagraphSearcher.HighlightBrush;
            }

            // If the caret is in the end of a highlight we move to the adjacent run
            // It has to be in the end because if there is a match at the beginning of the file
            // and the caret has not been touched (so it is in the beginning of the file too)
            // we want to highlight this first match.
            // Considering the caller always set the caret to the end of the highlight
            // The condition below works well for successive searchs
            // We also need to move to the adjacent run if the caret is at the first run and we
            // are moving backwards so that a search backwards when the first run is highlighted
            // and the caret is at the beginning will wrap to the end
            if ((!forward && IsFirstRun(caretRun)) ||
                ((caretPosition.GetOffsetToPosition(caretRun.ContentEnd) == 0) && ParagraphSearcher.Ishighlighted(caretRun)))
            {
                currentRun = ParagraphSearcher.GetNextRun(caretRun, forward);
            }
            else
            {
                currentRun = caretRun;
            }

            currentRun = ParagraphSearcher.GetNextMatch(currentRun, forward);

            if (currentRun == null)
            {
                // if we could not find a next highlight wraparound
                currentRun = ParagraphSearcher.GetFirstOrLastRun(caretRun, forward);
                currentRun = ParagraphSearcher.GetNextMatch(currentRun, forward);
            }

            this.currentHighlightedMatch = currentRun;
            if (this.currentHighlightedMatch != null)
            {
                // restore the curent highlighted background to current highlighted
                this.currentHighlightedMatch.Background = ParagraphSearcher.CurrentHighlightBrush;
            }

            return currentRun;
        }

        
        internal void ResetSearch()
        {
            this.currentHighlightedMatch = null;
        }

        
        private static bool Ishighlighted(Run run)
        {
            if (run == null)
            {
                return false;
            }

            SolidColorBrush background = run.Background as SolidColorBrush;
            if (background != null && background == ParagraphSearcher.HighlightBrush)
            {
                return true;
            }

            return false;
        }

        
        private static Run GetNextRun(Run currentRun, bool forward)
        {
            Bold parentBold = currentRun.Parent as Bold;

            Inline nextInline;

            if (forward)
            {
                nextInline = parentBold != null ? ((Inline)parentBold).NextInline : currentRun.NextInline;
            }
            else
            {
                nextInline = parentBold != null ? ((Inline)parentBold).PreviousInline : currentRun.PreviousInline;
            }

            return GetRun(nextInline);
        }

        
        private static Run GetRun(Inline inline)
        {
            Bold inlineBold = inline as Bold;
            if (inlineBold != null)
            {
                return (Run)inlineBold.Inlines.FirstInline;
            }

            return (Run)inline;
        }

        
        private static Run GetNextMatch(Run currentRun, bool forward)
        {
            while (currentRun != null)
            {
                if (ParagraphSearcher.Ishighlighted(currentRun))
                {
                    return currentRun;
                }

                currentRun = ParagraphSearcher.GetNextRun(currentRun, forward);
            }

            return currentRun;
        }

        
        private static Paragraph GetParagraph(Run run)
        {
            Bold parentBold = run.Parent as Bold;
            Paragraph parentParagraph = (parentBold != null ? parentBold.Parent : run.Parent) as Paragraph;
            Debug.Assert(parentParagraph != null, "the documents we are saerching are built with ParagraphBuilder, which builds the document like this");
            return parentParagraph;
        }

        
        private static bool IsFirstRun(Run run)
        {
            Paragraph paragraph = GetParagraph(run);
            Run firstRun = ParagraphSearcher.GetRun(paragraph.Inlines.FirstInline);
            return run == firstRun;
        }

        
        private static Run GetFirstOrLastRun(Run caretRun, bool forward)
        {
            Debug.Assert(caretRun != null, "a caret run is always valid");

            Paragraph paragraph = GetParagraph(caretRun);

            Inline firstOrLastInline;
            if (forward)
            {
                firstOrLastInline = paragraph.Inlines.FirstInline;
            }
            else
            {
                firstOrLastInline = paragraph.Inlines.LastInline;
            }

            return GetRun(firstOrLastInline);
        }
    }
}
