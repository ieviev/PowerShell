// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell
{
    internal sealed partial class ConsoleHost : PSHost, IDisposable
    {
        internal bool IsTranscribing
        {
            get
            {
                // no locking because the compare should be atomic

                return _isTranscribing;
            }

            set
            {
                _isTranscribing = value;
            }
        }

        private bool _isTranscribing;

        
        private readonly string _transcriptFileName = string.Empty;

        internal string StopTranscribing()
        {
            lock (_transcriptionStateLock)
            {
                if (_transcriptionWriter == null)
                {
                    return null;
                }

                // The filestream *must* be closed at the end of this method.
                // If it isn't and there is a pending IO error, the finalizer will
                // dispose the stream resulting in an IO exception on the finalizer thread
                // which will crash the process...
                try
                {
                    _transcriptionWriter.WriteLine(
                        StringUtil.Format(ConsoleHostStrings.TranscriptEpilogue, DateTime.Now));
                }
                finally
                {
                    try
                    {
                        _transcriptionWriter.Dispose();
                    }
                    finally
                    {
                        _transcriptionWriter = null;
                        _isTranscribing = false;
                    }
                }

                return _transcriptFileName;
            }
        }

        internal void WriteToTranscript(ReadOnlySpan<char> text)
        {
            WriteToTranscript(text, newLine: false);
        }

        internal void WriteLineToTranscript(ReadOnlySpan<char> text)
        {
            WriteToTranscript(text, newLine: true);
        }

        internal void WriteToTranscript(ReadOnlySpan<char> text, bool newLine)
        {
            lock (_transcriptionStateLock)
            {
                if (_isTranscribing && _transcriptionWriter != null)
                {
                    if (newLine)
                    {
                        _transcriptionWriter.WriteLine(text);
                    }
                    else
                    {
                        _transcriptionWriter.Write(text);
                    }
                }
            }
        }

        private StreamWriter _transcriptionWriter;
        private readonly object _transcriptionStateLock = new object();
    }
}   // namespace
