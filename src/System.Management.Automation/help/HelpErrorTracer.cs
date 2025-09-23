// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    
    internal class HelpErrorTracer
    {
        
        internal sealed class TraceFrame : IDisposable
        {
            // Following are help context information
            private readonly string _helpFile = string.Empty;

            // ErrorRecords accumulated during the help content loading.
            private readonly Collection<ErrorRecord> _errors = new Collection<ErrorRecord>();

            private readonly HelpErrorTracer _helpTracer;
            
            internal TraceFrame(HelpErrorTracer helpTracer, string helpFile)
            {
                _helpTracer = helpTracer;
                _helpFile = helpFile;
            }

            
            internal void TraceError(ErrorRecord errorRecord)
            {
                if (_helpTracer.HelpSystem.VerboseHelpErrors)
                    _errors.Add(errorRecord);
            }

            
            internal void TraceErrors(Collection<ErrorRecord> errorRecords)
            {
                if (_helpTracer.HelpSystem.VerboseHelpErrors)
                {
                    foreach (ErrorRecord errorRecord in errorRecords)
                    {
                        _errors.Add(errorRecord);
                    }
                }
            }

            
            public void Dispose()
            {
                if (_helpTracer.HelpSystem.VerboseHelpErrors && _errors.Count > 0)
                {
                    ErrorRecord errorRecord = new ErrorRecord(new ParentContainsErrorRecordException("Help Load Error"), "HelpLoadError", ErrorCategory.SyntaxError, null);
                    errorRecord.ErrorDetails = new ErrorDetails(typeof(HelpErrorTracer).Assembly, "HelpErrors", "HelpLoadError", _helpFile, _errors.Count);
                    _helpTracer.HelpSystem.LastErrors.Add(errorRecord);

                    foreach (ErrorRecord error in _errors)
                    {
                        _helpTracer.HelpSystem.LastErrors.Add(error);
                    }
                }

                _helpTracer.PopFrame(this);
            }
        }

        internal HelpSystem HelpSystem { get; }

        internal HelpErrorTracer(HelpSystem helpSystem)
        {
            if (helpSystem == null)
            {
                throw PSTraceSource.NewArgumentNullException("HelpSystem");
            }

            HelpSystem = helpSystem;
        }

        
        private readonly List<TraceFrame> _traceFrames = new List<TraceFrame>();

        
        internal IDisposable Trace(string helpFile)
        {
            TraceFrame traceFrame = new TraceFrame(this, helpFile);

            _traceFrames.Add(traceFrame);

            return traceFrame;
        }

        
        internal void TraceError(ErrorRecord errorRecord)
        {
            if (_traceFrames.Count == 0)
                return;

            TraceFrame traceFrame = _traceFrames[_traceFrames.Count - 1];

            traceFrame.TraceError(errorRecord);
        }

        
        internal void TraceErrors(Collection<ErrorRecord> errorRecords)
        {
            if (_traceFrames.Count == 0)
                return;

            TraceFrame traceFrame = _traceFrames[_traceFrames.Count - 1];

            traceFrame.TraceErrors(errorRecords);
        }

        internal void PopFrame(TraceFrame traceFrame)
        {
            if (_traceFrames.Count == 0)
                return;

            TraceFrame lastFrame = _traceFrames[_traceFrames.Count - 1];

            if (lastFrame == traceFrame)
            {
                _traceFrames.RemoveAt(_traceFrames.Count - 1);
            }
        }

        
        internal bool IsOn
        {
            get
            {
                return (_traceFrames.Count > 0 && this.HelpSystem.VerboseHelpErrors);
            }
        }
    }
}
