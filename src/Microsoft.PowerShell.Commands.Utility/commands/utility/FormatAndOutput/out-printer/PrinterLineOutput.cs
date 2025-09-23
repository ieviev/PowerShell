// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    
    internal sealed class PrinterLineOutput : LineOutput
    {
        #region LineOutput implementation

        
        internal override bool RequiresBuffering { get { return true; } }

        
        internal override void ExecuteBufferPlayBack(DoPlayBackCall playback)
        {
            _playbackCall = playback;
            DoPrint();
        }

        
        internal override int ColumnNumber
        {
            get
            {
                CheckStopProcessing();
                return _deviceColumns;
            }
        }

        
        internal override int RowNumber
        {
            get
            {
                CheckStopProcessing();
                return _deviceRows;
            }
        }

        
        internal override void WriteLine(string s)
        {
            CheckStopProcessing();

            // Remove all ANSI escape sequences before sending out to the printer.
            s = new ValueStringDecorated(s).ToString(OutputRendering.PlainText);
            WriteRawText(s);
        }

        
        internal override void WriteRawText(string s)
        {
            CheckStopProcessing();

            // Delegate the action to the helper, that will properly break the string into screen lines.
            _writeLineHelper.WriteLine(s, ColumnNumber);
        }

        #endregion

        
        static PrinterLineOutput()
        {
            // This default must be loaded from a resource file as different
            // cultures will have different defaults and the localizer would
            // know the default for different cultures.
            s_defaultPrintFontName = OutPrinterDisplayStrings.DefaultPrintFontName;
        }

        
        internal PrinterLineOutput(string printerName)
        {
            _printerName = printerName;

            // instantiate the helper to do the line processing when LineOutput.WriteXXX() is called
            WriteLineHelper.WriteCallback wl = new(this.OnWriteLine);
            WriteLineHelper.WriteCallback w = new(this.OnWrite);

            _writeLineHelper = new WriteLineHelper(true, wl, w, this.DisplayCells);
        }

        
        private void OnWriteLine(string s)
        {
            _lines.Enqueue(s);
        }

        
        private void OnWrite(string s)
        {
            _lines.Enqueue(s);
        }

        
        private void DoPrint()
        {
            try
            {
                // create a new print document object and set the printer name, if available
                PrintDocument pd = new();

                if (!string.IsNullOrEmpty(_printerName))
                {
                    pd.PrinterSettings.PrinterName = _printerName;
                }

                // set up the callback mechanism
                pd.PrintPage += this.pd_PrintPage;

                // start printing
                pd.Print();
            }
            finally
            {
                // make sure we do not leak the font
                if (_printFont != null)
                {
                    _printFont.Dispose();
                    _printFont = null;
                }
            }
        }

        
        private void CreateFont(Graphics g)
        {
            if (_printFont != null)
                return;

            // create the font

            // do we have a specified font?
            if (string.IsNullOrEmpty(_printFontName))
            {
                _printFontName = s_defaultPrintFontName;
            }

            if (_printFontSize <= 0)
            {
                _printFontSize = DefaultPrintFontSize;
            }

            _printFont = new Font(_printFontName, _printFontSize);
            VerifyFont(g);
        }

        
        private void VerifyFont(Graphics g)
        {
            // check if the font is fixed pitch
            // HEURISTICS:
            // we compute the length of two strings, one made of "large" characters
            // one made of "narrow" ones. If they are the same length, we assume that
            // the font is fixed pitch.
            const string large = "ABCDEF";
            float wLarge = g.MeasureString(large, _printFont).Width / large.Length;
            const string narrow = ".;'}l|";
            float wNarrow = g.MeasureString(narrow, _printFont).Width / narrow.Length;

            if (Math.Abs((float)(wLarge - wNarrow)) < 0.001F)
            {
                // we passed the test
                return;
            }

            // just get back to the default, since it's not fixed pitch
            _printFont.Dispose();
            _printFont = new Font(s_defaultPrintFontName, DefaultPrintFontSize);
        }

        
        private void pd_PrintPage(object sender, PrintPageEventArgs ev)
        {
            float yPos = 0; // GDI+ coordinate down the page
            int linesPrinted = 0; // linesPrinted
            float leftMargin = ev.MarginBounds.Left;
            float topMargin = ev.MarginBounds.Top;

            CreateFont(ev.Graphics);

            // compute the height of a line of text
            float lineHeight = _printFont.GetHeight(ev.Graphics);

            // Work out the number of lines per page
            // Use the MarginBounds on the event to do this
            float linesPerPage = ev.MarginBounds.Height / _printFont.GetHeight(ev.Graphics);

            if (!_printingInitialized)
            {
                // on the first page we have to initialize the metrics for LineOutput

                // work out the number of columns per page assuming fixed pitch font
                const string s = "ABCDEF";
                float w = ev.Graphics.MeasureString(s, _printFont).Width / s.Length;
                float columnsPerPage = ev.MarginBounds.Width / w;

                _printingInitialized = true;
                _deviceRows = (int)linesPerPage;
                _deviceColumns = (int)columnsPerPage;

                // now that we initialized the column and row count for the LineOutput
                // interface we can tell the outputter to playback from cache to do the
                // proper computations of line widths
                // returning from this call, the string queue on this object is full of
                // lines of text to print
                _playbackCall();
            }

            // now iterate over the file printing out each line
            while ((linesPrinted < linesPerPage) && (_lines.Count > 0))
            {
                // get the string to be printed
                string line = _lines.Dequeue();

                // compute the Y position where to draw
                yPos = topMargin + (linesPrinted * lineHeight);

                // do the actual drawing
                ev.Graphics.DrawString(line, _printFont, Brushes.Black, leftMargin, yPos, new StringFormat());
                linesPrinted++;
            }

            // If we have more lines then print another page
            ev.HasMorePages = _lines.Count > 0;
        }

        
        private bool _printingInitialized = false;

        
        private DoPlayBackCall _playbackCall;

        
        private readonly string _printerName = null;

        
        private string _printFontName = null;

        
        private int _printFontSize = 0;

        
        private static readonly string s_defaultPrintFontName;

        
        private const int DefaultPrintFontSize = 8;

        
        private int _deviceColumns = 80;

        // number of rows per sheet
        private int _deviceRows = 40;

        
        private readonly Queue<string> _lines = new();

        
        private Font _printFont = null;

        private readonly WriteLineHelper _writeLineHelper;
    }
}
