// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Text;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsData.Out, "String", DefaultParameterSetName = "NoNewLineFormatting", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097024", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public class OutStringCommand : FrontEndCommandBase
    {
        #region Command Line Parameters
        
        [Parameter(ParameterSetName = "StreamFormatting")]
        public SwitchParameter Stream
        {
            get { return _stream; }

            set { _stream = value; }
        }

        private bool _stream;

        
        [ValidateRange(2, int.MaxValue)]
        [Parameter]
        public int Width
        {
            get { return (_width != null) ? _width.Value : 0; }

            set { _width = value; }
        }

        private int? _width = null;

        
        [Parameter(ParameterSetName = "NoNewLineFormatting")]
        public SwitchParameter NoNewline
        {
            get { return _noNewLine; }

            set { _noNewLine = value; }
        }

        private bool _noNewLine = false;

        #endregion

        
        public OutStringCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        
        protected override void BeginProcessing()
        {
            // set up the LineOutput interface
            OutputManagerInner outInner = (OutputManagerInner)this.implementation;

            outInner.LineOutput = InstantiateLineOutputInterface();

            // finally call the base class for general hookup
            base.BeginProcessing();
        }

        
        private LineOutput InstantiateLineOutputInterface()
        {
            // set up the streaming text writer
            StreamingTextWriter.WriteLineCallback callback = new(this.OnWriteLine);

            _writer = new StreamingTextWriter(callback, Host.CurrentCulture);

            // compute the # of columns available
            int computedWidth = int.MaxValue;

            if (_width != null)
            {
                // use the value from the command line
                computedWidth = _width.Value;
            }

            // use it to create and initialize the Line Output writer
            TextWriterLineOutput twlo = new(_writer, computedWidth);

            // finally have the LineOutput interface extracted
            return (LineOutput)twlo;
        }

        
        private void OnWriteLine(string s)
        {
            if (_stream)
            {
                this.WriteObject(s);
            }
            else
            {
                if (_noNewLine)
                {
                    _buffer.Append(s);
                }
                else
                {
                    _buffer.AppendLine(s);
                }
            }
        }

        
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            _writer.Flush();
        }

        
        protected override void EndProcessing()
        {
            base.EndProcessing();

            // close the writer
            _writer.Flush();
            _writer.Dispose();

            if (!_stream)
                this.WriteObject(_buffer.ToString());
        }

        
        private StreamingTextWriter _writer = null;

        
        private readonly StringBuilder _buffer = new();
    }
}
