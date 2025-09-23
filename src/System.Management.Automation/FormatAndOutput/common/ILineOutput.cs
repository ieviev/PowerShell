// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Text;

// interfaces for host interaction

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    
    internal class DisplayCells
    {
        
        /// <param name="str">String that may contain VT escape sequences.</param>
        /// <returns>Number of buffer cells the string needs to take.</returns>
        internal int Length(string str)
        {
            return Length(str, 0);
        }

        
        /// <param name="str">String that may contain VT escape sequences.</param>
        /// <param name="offset">
        /// When the string doesn't contain VT sequences, it's the starting index.
        /// When the string contains VT sequences, it means starting from the 'n-th' char that doesn't belong to a escape sequence.</param>
        /// <returns>Number of buffer cells the string needs to take.</returns>
        internal virtual int Length(string str, int offset)
        {
            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            var valueStrDec = new ValueStringDecorated(str);
            if (valueStrDec.IsDecorated)
            {
                str = valueStrDec.ToString(OutputRendering.PlainText);
            }

            int length = 0;
            for (; offset < str.Length; offset++)
            {
                length += CharLengthInBufferCells(str[offset]);
            }

            return length;
        }

        
        /// <param name="character"></param>
        /// <returns>Number of buffer cells the character needs to take.</returns>
        internal virtual int Length(char character)
        {
            return CharLengthInBufferCells(character);
        }

        
        /// <param name="str">String that may contain VT escape sequences.</param>
        /// <param name="displayCells">Number of buffer cells to fit in.</param>
        /// <returns>Number of non-escape-sequence characters from head of the string that can fit in the space.</returns>
        internal int TruncateTail(string str, int displayCells)
        {
            return TruncateTail(str, offset: 0, displayCells);
        }

        
        /// <param name="str">String that may contain VT escape sequences.</param>
        /// <param name="offset">
        /// When the string doesn't contain VT sequences, it's the starting index.
        /// When the string contains VT sequences, it means starting from the 'n-th' char that doesn't belong to a escape sequence.</param>
        /// <param name="displayCells">Number of buffer cells to fit in.</param>
        /// <returns>Number of non-escape-sequence characters from head of the string that can fit in the space.</returns>
        internal int TruncateTail(string str, int offset, int displayCells)
        {
            var valueStrDec = new ValueStringDecorated(str);
            if (valueStrDec.IsDecorated)
            {
                str = valueStrDec.ToString(OutputRendering.PlainText);
            }

            return GetFitLength(str, offset, displayCells, startFromHead: true);
        }

        
        /// <param name="str">String that may contain VT escape sequences.</param>
        /// <param name="displayCells">Number of buffer cells to fit in.</param>
        /// <returns>Number of non-escape-sequence characters from head of the string that should be skipped.</returns>
        internal int TruncateHead(string str, int displayCells)
        {
            var valueStrDec = new ValueStringDecorated(str);
            if (valueStrDec.IsDecorated)
            {
                str = valueStrDec.ToString(OutputRendering.PlainText);
            }

            int tailCount = GetFitLength(str, offset: 0, displayCells, startFromHead: false);
            return str.Length - tailCount;
        }

        #region Helpers

        protected static int CharLengthInBufferCells(char c)
        {
            // The following is based on http://www.cl.cam.ac.uk/~mgk25/c/wcwidth.c
            // which is derived from https://www.unicode.org/Public/UCD/latest/ucd/EastAsianWidth.txt
            bool isWide = c >= 0x1100 &&
                (c <= 0x115f || 
                 c == 0x2329 || c == 0x232a ||
                 ((uint)(c - 0x2e80) <= (0xa4cf - 0x2e80) &&
                  c != 0x303f) || 
                 ((uint)(c - 0xac00) <= (0xd7a3 - 0xac00)) || 
                 ((uint)(c - 0xf900) <= (0xfaff - 0xf900)) || 
                 ((uint)(c - 0xfe10) <= (0xfe19 - 0xfe10)) || 
                 ((uint)(c - 0xfe30) <= (0xfe6f - 0xfe30)) || 
                 ((uint)(c - 0xff00) <= (0xff60 - 0xff00)) || 
                 ((uint)(c - 0xffe0) <= (0xffe6 - 0xffe0)));

            // We can ignore these ranges because .Net strings use surrogate pairs
            // for this range and we do not handle surrogate pairs.
            // (c >= 0x20000 && c <= 0x2fffd) ||
            // (c >= 0x30000 && c <= 0x3fffd)
            return 1 + (isWide ? 1 : 0);
        }

        
        /// <param name="str">String to be displayed, which doesn't contain any VT sequences.</param>
        /// <param name="offset">Offset inside the string.</param>
        /// <param name="displayCells">Number of display cells.</param>
        /// <param name="startFromHead">If true compute from the head (i.e. k++) else from the tail (i.e. k--).</param>
        /// <returns>Number of characters that would fit.</returns>
        protected int GetFitLength(string str, int offset, int displayCells, bool startFromHead)
        {
            int filledDisplayCellsCount = 0; // number of cells that are filled in
            int charactersAdded = 0; // number of characters that fit
            int currCharDisplayLen; // scratch variable

            int k = startFromHead ? offset : str.Length - 1;
            int kFinal = startFromHead ? str.Length - 1 : offset;
            while (true)
            {
                if ((startFromHead && k > kFinal) || (!startFromHead && k < kFinal))
                {
                    break;
                }

                // compute the cell number for the current character
                currCharDisplayLen = this.Length(str[k]);

                if (filledDisplayCellsCount + currCharDisplayLen > displayCells)
                {
                    // if we added this character it would not fit, we cannot continue
                    break;
                }

                // keep adding, we fit
                filledDisplayCellsCount += currCharDisplayLen;
                charactersAdded++;

                // check if we fit exactly
                if (filledDisplayCellsCount == displayCells)
                {
                    // exact fit, we cannot add more
                    break;
                }

                k = startFromHead ? (k + 1) : (k - 1);
            }

            return charactersAdded;
        }

        #endregion
    }

    
    internal abstract class LineOutput
    {
        
        internal virtual bool RequiresBuffering { get { return false; } }

        
        internal delegate void DoPlayBackCall();

        
        internal virtual void ExecuteBufferPlayBack(DoPlayBackCall playback) { }

        
        internal abstract int ColumnNumber { get; }

        
        internal abstract int RowNumber { get; }

        
        /// <param name="s">
        ///     string to be written to the device
        /// </param>
        internal abstract void WriteLine(string s);

        
        /// <param name="s">The raw text to be written to the device.</param>
        internal virtual void WriteRawText(string s) => WriteLine(s);

        internal WriteStreamType WriteStream
        {
            get;
            set;
        }

        
        internal void StopProcessing()
        {
            _isStopping = true;
        }

        private bool _isStopping;

        internal void CheckStopProcessing()
        {
            if (!_isStopping)
                return;
            throw new PipelineStoppedException();
        }

        
        /// <value></value>
        internal virtual DisplayCells DisplayCells
        {
            get
            {
                CheckStopProcessing();
                // just return the default singleton implementation
                return _displayCellsDefault;
            }
        }

        
        protected static DisplayCells _displayCellsDefault = new DisplayCells();
    }

    
    internal class WriteLineHelper
    {
        #region callbacks

        
        /// <param name="s">String to write.</param>
        internal delegate void WriteCallback(string s);

        
        private readonly WriteCallback _writeCall = null;

        
        private readonly WriteCallback _writeLineCall = null;

        #endregion

        private readonly bool _lineWrap;

        
        /// <param name="lineWrap">True if we require line wrapping.</param>
        /// <param name="wlc">Delegate for WriteLine(), must ben non null.</param>
        /// <param name="wc">Delegate for Write(), if null, use the first parameter.</param>
        /// <param name="displayCells">Helper object for manipulating strings.</param>
        internal WriteLineHelper(bool lineWrap, WriteCallback wlc, WriteCallback wc, DisplayCells displayCells)
        {
            if (wlc == null)
                throw PSTraceSource.NewArgumentNullException(nameof(wlc));
            if (displayCells == null)
                throw PSTraceSource.NewArgumentNullException(nameof(displayCells));

            _displayCells = displayCells;
            _writeLineCall = wlc;
            _writeCall = wc ?? wlc;
            _lineWrap = lineWrap;
        }

        
        /// <param name="s">String to process.</param>
        /// <param name="cols">Width of the device.</param>
        internal void WriteLine(string s, int cols)
        {
            WriteLineInternal(s, cols);
        }

        
        /// <param name="val">String to process.</param>
        /// <param name="cols">Width of the device.</param>
        private void WriteLineInternal(string val, int cols)
        {
            if (string.IsNullOrEmpty(val))
            {
                _writeLineCall(val);
                return;
            }

            // If the output is being redirected, then we don't break val
            if (!_lineWrap)
            {
                _writeCall(val);
                return;
            }

            // check for line breaks
            List<string> lines = StringManipulationHelper.SplitLines(val);

            // process the substrings as separate lines
            for (int k = 0; k < lines.Count; k++)
            {
                // compute the display length of the string
                int displayLength = _displayCells.Length(lines[k]);

                if (displayLength < cols)
                {
                    // NOTE: this is the case where where System.Console.WriteLine() would work just fine
                    _writeLineCall(lines[k]);
                    continue;
                }

                if (displayLength == cols)
                {
                    // NOTE: this is the corner case where System.Console.WriteLine() cannot be called
                    _writeCall(lines[k]);
                    continue;
                }

                // the string does not fit, so we have to wrap around on multiple lines
                string s = lines[k];

                while (true)
                {
                    // the string is still too long to fit, write the first cols characters
                    // and go back for more wraparound
                    int headCount = _displayCells.TruncateTail(s, cols);
                    WriteLineInternal(s.VtSubstring(0, headCount), cols);

                    // chop off the first fieldWidth characters, already printed
                    s = s.VtSubstring(headCount);
                    if (_displayCells.Length(s) <= cols)
                    {
                        // if we fit, print the tail of the string and we are done
                        WriteLineInternal(s, cols);
                        break;
                    }
                }
            }
        }

        private readonly DisplayCells _displayCells;
    }

    
    internal sealed class TextWriterLineOutput : LineOutput
    {
        #region ILineOutput methods

        
        internal override int ColumnNumber
        {
            get
            {
                CheckStopProcessing();
                return _columns;
            }
        }

        
        internal override int RowNumber
        {
            get
            {
                CheckStopProcessing();
                return -1;
            }
        }

        
        /// <param name="s"></param>
        internal override void WriteLine(string s)
        {
            WriteRawText(PSHostUserInterface.GetOutputString(s, isHost: false));
        }

        
        /// <param name="s">The raw text to be written to the device.</param>
        internal override void WriteRawText(string s)
        {
            CheckStopProcessing();

            if (_suppressNewline)
            {
                _writer.Write(s);
            }
            else
            {
                _writer.WriteLine(s);
            }
        }

        #endregion

        
        /// <param name="writer">TextWriter to write to.</param>
        /// <param name="columns">Max columns widths for the text.</param>
        internal TextWriterLineOutput(TextWriter writer, int columns)
        {
            _writer = writer;
            _columns = columns;
        }

        
        /// <param name="writer">TextWriter to write to.</param>
        /// <param name="columns">Max columns widths for the text.</param>
        /// <param name="suppressNewline">False to add a newline to the end of the output string, true if not.</param>
        internal TextWriterLineOutput(TextWriter writer, int columns, bool suppressNewline)
            : this(writer, columns)
        {
            _suppressNewline = suppressNewline;
        }

        private readonly int _columns = 0;

        private readonly TextWriter _writer = null;

        private readonly bool _suppressNewline = false;
    }

    
    internal class StreamingTextWriter : TextWriter
    {
        #region tracer
        [TraceSource("StreamingTextWriter", "StreamingTextWriter")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("StreamingTextWriter", "StreamingTextWriter");
        #endregion tracer

        
        /// <param name="writeCall">Delegate to write to.</param>
        /// <param name="culture">Culture for this TextWriter.</param>
        internal StreamingTextWriter(WriteLineCallback writeCall, CultureInfo culture)
            : base(culture)
        {
            if (writeCall == null)
                throw PSTraceSource.NewArgumentNullException(nameof(writeCall));

            _writeCall = writeCall;
        }

        #region TextWriter overrides

        public override Encoding Encoding { get { return new UnicodeEncoding(); } }

        public override void WriteLine(string s)
        {
            _writeCall(s);
        }

        #endregion

        
        /// <param name="s">String to write.</param>
        internal delegate void WriteLineCallback(string s);

        
        private readonly WriteLineCallback _writeCall = null;
    }
}
