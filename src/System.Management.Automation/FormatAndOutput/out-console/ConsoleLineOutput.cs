// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Host;

using Dbg = System.Management.Automation.Diagnostics;

// interfaces for host interaction

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    
    internal class DisplayCellsHost : DisplayCells
    {
        internal DisplayCellsHost(PSHostRawUserInterface rawUserInterface)
        {
            _rawUserInterface = rawUserInterface;
        }

        internal override int Length(string str, int offset)
        {
            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            if (offset < 0 || offset >= str.Length)
            {
                throw PSTraceSource.NewArgumentException(nameof(offset));
            }

            try
            {
                var valueStrDec = new ValueStringDecorated(str);
                if (valueStrDec.IsDecorated)
                {
                    str = valueStrDec.ToString(OutputRendering.PlainText);
                }

                int length = 0;
                for (; offset < str.Length; offset++)
                {
                    length += _rawUserInterface.LengthInBufferCells(str[offset]);
                }

                return length;
            }
            catch
            {
                // thrown when external host rawui is not implemented, in which case
                // we will fallback to the default value.
                return base.Length(str, offset);
            }
        }

        internal override int Length(char character)
        {
            try
            {
                return _rawUserInterface.LengthInBufferCells(character);
            }
            catch
            {
                // thrown when external host rawui is not implemented, in which case
                // we will fallback to the default value.
                return base.Length(character);
            }
        }

        private readonly PSHostRawUserInterface _rawUserInterface;
    }

    
    internal sealed class ConsoleLineOutput : LineOutput
    {
        #region tracer
        [TraceSource("ConsoleLineOutput", "ConsoleLineOutput")]
        internal static readonly PSTraceSource tracer = PSTraceSource.GetTracer("ConsoleLineOutput", "ConsoleLineOutput");
        #endregion tracer

        
        private static readonly HashSet<string> s_psHost = new(StringComparer.Ordinal) { "ConsoleHost", "Visual Studio Code Host" };

        #region LineOutput implementation
        
        internal override int ColumnNumber
        {
            get
            {
                CheckStopProcessing();
                PSHostRawUserInterface raw = _console.RawUI;

                // IMPORTANT NOTE: we subtract one because
                // we want to make sure the console's last column
                // is never considered written. This causes the writing
                // logic to always call WriteLine(), making sure a CR
                // is inserted.

                try
                {
                    return _forceNewLine ? raw.BufferSize.Width - 1 : raw.BufferSize.Width;
                }
                catch
                {
                    // thrown when external host rawui is not implemented, in which case
                    // we will fallback to the default value.
                }

                return _forceNewLine ? _fallbackRawConsoleColumnNumber - 1 : _fallbackRawConsoleColumnNumber;
            }
        }

        
        internal override int RowNumber
        {
            get
            {
                CheckStopProcessing();
                PSHostRawUserInterface raw = _console.RawUI;

                try
                {
                    return raw.WindowSize.Height;
                }
                catch
                {
                    // thrown when external host rawui is not implemented, in which case
                    // we will fallback to the default value.
                }

                return _fallbackRawConsoleRowNumber;
            }
        }

        
        internal override void WriteLine(string s)
        {
            CheckStopProcessing();

            // delegate the action to the helper,
            // that will properly break the string into
            // screen lines
            _writeLineHelper.WriteLine(s, this.ColumnNumber);
        }

        internal override DisplayCells DisplayCells
        {
            get
            {
                CheckStopProcessing();
                if (_displayCellsHost != null)
                {
                    return _displayCellsHost;
                }

                // fall back if we do not have a Msh host specific instance
                return _displayCellsDefault;
            }
        }
        #endregion

        
        internal ConsoleLineOutput(PSHost host, bool paging, TerminatingErrorContext errorContext)
        {
            if (host == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(host));
            }

            if (errorContext == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(errorContext));
            }

            _console = host.UI;
            _errorContext = errorContext;

            if (paging)
            {
                tracer.WriteLine("paging is needed");

                // If we need to do paging, instantiate a prompt handler that will take care of the screen interaction
                string promptString = StringUtil.Format(FormatAndOut_out_xxx.ConsoleLineOutput_PagingPrompt);
                _prompt = new PromptHandler(promptString, this);
            }

            if (!s_psHost.Contains(host.Name) && _console.RawUI is not null)
            {
                // set only if we have a valid raw interface
                tracer.WriteLine("there is a valid raw interface");
                _displayCellsHost = new DisplayCellsHost(_console.RawUI);
            }

            // instantiate the helper to do the line processing when ILineOutput.WriteXXX() is called
            WriteLineHelper.WriteCallback wl = new WriteLineHelper.WriteCallback(this.OnWriteLine);
            WriteLineHelper.WriteCallback w = new WriteLineHelper.WriteCallback(this.OnWrite);

            if (_forceNewLine)
            {
                _writeLineHelper = new WriteLineHelper(false, wl, null, this.DisplayCells);
            }
            else
            {
                _writeLineHelper = new WriteLineHelper(false, wl, w, this.DisplayCells);
            }
        }

        
        private void OnWriteLine(string s)
        {
            // Do any default transcription.
            _console.TranscribeResult(s);

            switch (this.WriteStream)
            {
                case WriteStreamType.Error:
                    _console.WriteErrorLine(s);
                    break;

                case WriteStreamType.Warning:
                    _console.WriteWarningLine(s);
                    break;

                case WriteStreamType.Verbose:
                    _console.WriteVerboseLine(s);
                    break;

                case WriteStreamType.Debug:
                    _console.WriteDebugLine(s);
                    break;

                default:
                    // If the host is in "transcribe only"
                    // mode (due to an implicitly added call to Out-Default -Transcribe),
                    // then don't call the actual host API.
                    if (!_console.TranscribeOnly)
                    {
                        _console.WriteLine(s);
                    }

                    break;
            }

            LineWrittenEvent();
        }

        
        private void OnWrite(string s)
        {
            switch (this.WriteStream)
            {
                case WriteStreamType.Error:
                    _console.WriteErrorLine(s);
                    break;

                case WriteStreamType.Warning:
                    _console.WriteWarningLine(s);
                    break;

                case WriteStreamType.Verbose:
                    _console.WriteVerboseLine(s);
                    break;

                case WriteStreamType.Debug:
                    _console.WriteDebugLine(s);
                    break;

                default:
                    _console.Write(s);
                    break;
            }

            LineWrittenEvent();
        }

        
        private void LineWrittenEvent()
        {
            // check to avoid reentrancy from the prompt handler
            // writing during the PromptUser() call
            if (_disableLineWrittenEvent)
                return;

            // if there is no prompting, we are done
            if (_prompt == null)
                return;

            // increment the count of lines written to the screen
            _linesWritten++;

            // check if we need to put out a prompt
            if (this.NeedToPrompt)
            {
                // put out the prompt
                _disableLineWrittenEvent = true;
                PromptHandler.PromptResponse response = _prompt.PromptUser(_console);
                _disableLineWrittenEvent = false;

                switch (response)
                {
                    case PromptHandler.PromptResponse.NextPage:
                        {
                            // reset the counter, since we are starting a new page
                            _linesWritten = 0;
                        }

                        break;
                    case PromptHandler.PromptResponse.NextLine:
                        {
                            // roll back the counter by one, since we allow one more line
                            _linesWritten--;
                        }

                        break;
                    case PromptHandler.PromptResponse.Quit:
                        // 1021203-2005/05/09-JonN
                        // HaltCommandException will cause the command
                        // to stop, but not be reported as an error.
                        throw new HaltCommandException();
                }
            }
        }

        
        private bool NeedToPrompt
        {
            get
            {
                // NOTE: we recompute all the time to take into account screen resizing
                int rawRowNumber = this.RowNumber;

                if (rawRowNumber <= 0)
                {
                    // something is wrong, there is no real estate, we suppress prompting
                    return false;
                }

                // the prompt will occupy some lines, so we need to subtract them form the total
                // screen line count
                int computedPromptLines = _prompt.ComputePromptLines(this.DisplayCells, this.ColumnNumber);
                int availableLines = this.RowNumber - computedPromptLines;

                if (availableLines <= 0)
                {
                    tracer.WriteLine("No available Lines; suppress prompting");
                    // something is wrong, there is no real estate, we suppress prompting
                    return false;
                }

                return _linesWritten >= availableLines;
            }
        }

        #region Private Members
        
        private sealed class PromptHandler
        {
            
            internal PromptHandler(string s, ConsoleLineOutput cmdlet)
            {
                if (string.IsNullOrEmpty(s))
                    throw PSTraceSource.NewArgumentNullException(nameof(s));

                _promptString = s;
                _callingCmdlet = cmdlet;
            }

            
            internal int ComputePromptLines(DisplayCells displayCells, int cols)
            {
                // split the prompt string into lines
                _actualPrompt = StringManipulationHelper.GenerateLines(displayCells, _promptString, cols, cols);
                return _actualPrompt.Count;
            }

            
            internal enum PromptResponse
            {
                NextPage,
                NextLine,
                Quit
            }

            
            internal PromptResponse PromptUser(PSHostUserInterface console)
            {
                // NOTE: assume the values passed to ComputePromptLines are still valid

                // write out the prompt line(s). The last one will not have a new line
                // at the end because we leave the prompt at the end of the line
                for (int k = 0; k < _actualPrompt.Count; k++)
                {
                    if (k < (_actualPrompt.Count - 1))
                        console.WriteLine(_actualPrompt[k]); // intermediate line(s)
                    else
                        console.Write(_actualPrompt[k]); // last line
                }

                while (true)
                {
                    _callingCmdlet.CheckStopProcessing();
                    KeyInfo ki = console.RawUI.ReadKey(ReadKeyOptions.IncludeKeyUp | ReadKeyOptions.NoEcho);
                    char key = ki.Character;
                    if (key == 'q' || key == 'Q')
                    {
                        // need to move to the next line since we accepted input, add a newline
                        console.WriteLine();
                        return PromptResponse.Quit;
                    }
                    else if (key == ' ')
                    {
                        // need to move to the next line since we accepted input, add a newline
                        console.WriteLine();
                        return PromptResponse.NextPage;
                    }
                    else if (key == '\r')
                    {
                        // need to move to the next line since we accepted input, add a newline
                        console.WriteLine();
                        return PromptResponse.NextLine;
                    }
                }
            }

            
            private StringCollection _actualPrompt;

            
            private readonly string _promptString;

            
            private readonly ConsoleLineOutput _callingCmdlet = null;
        }

        
        private readonly bool _forceNewLine = true;

        
        private readonly int _fallbackRawConsoleColumnNumber = 80;

        
        private readonly int _fallbackRawConsoleRowNumber = 40;

        private readonly WriteLineHelper _writeLineHelper;

        
        private readonly PromptHandler _prompt = null;

        
        private long _linesWritten = 0;

        
        private bool _disableLineWrittenEvent = false;

        
        private readonly PSHostUserInterface _console = null;

        
        private readonly DisplayCells _displayCellsHost;

        
        private readonly TerminatingErrorContext _errorContext = null;

        #endregion
    }
}
