// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal.Host
{
    internal
    class InternalHostRawUserInterface : PSHostRawUserInterface
    {
        internal
        InternalHostRawUserInterface(PSHostRawUserInterface externalRawUI, InternalHost parentHost)
        {
            // externalRawUI may be null
            _externalRawUI = externalRawUI;
            _parentHost = parentHost;
        }

        internal
        void
        ThrowNotInteractive()
        {
            // It might be interesting to do something like
            // GetCallingMethodAndParameters here and display the name,
            // but I don't want to put that in mainline non-trace code.
            string message = HostInterfaceExceptionsStrings.HostFunctionNotImplemented;
            HostException e = new HostException(
                message,
                null,
                "HostFunctionNotImplemented",
                ErrorCategory.NotImplemented);
            throw e;
        }

        
        public override
        ConsoleColor
        ForegroundColor
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                ConsoleColor result = _externalRawUI.ForegroundColor;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.ForegroundColor = value;
            }
        }

        
        public override
        ConsoleColor
        BackgroundColor
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                ConsoleColor result = _externalRawUI.BackgroundColor;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.BackgroundColor = value;
            }
        }

        
        public override
        Coordinates
        CursorPosition
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Coordinates result = _externalRawUI.CursorPosition;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.CursorPosition = value;
            }
        }

        
        public override
        Coordinates
        WindowPosition
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Coordinates result = _externalRawUI.WindowPosition;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.WindowPosition = value;
            }
        }

        
        public override
        int
        CursorSize
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                int result = _externalRawUI.CursorSize;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.CursorSize = value;
            }
        }

        
        public override
        Size
        BufferSize
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = _externalRawUI.BufferSize;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.BufferSize = value;
            }
        }

        
        public override
        Size
        WindowSize
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = _externalRawUI.WindowSize;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.WindowSize = value;
            }
        }

        
        public override
        Size
        MaxWindowSize
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = _externalRawUI.MaxWindowSize;

                return result;
            }
        }

        
        public override
        Size
        MaxPhysicalWindowSize
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = _externalRawUI.MaxPhysicalWindowSize;

                return result;
            }
        }

        
        public override
        KeyInfo
        ReadKey(ReadKeyOptions options)
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            KeyInfo result = new KeyInfo();
            try
            {
                result = _externalRawUI.ReadKey(options);
            }
            catch (PipelineStoppedException)
            {
                // PipelineStoppedException is thrown by host when it wants
                // to stop the pipeline.
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_parentHost.Context.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                {
                    throw;
                }

                lpl.Stopper.Stop();
            }

            return result;
        }

        
        public override
        void
        FlushInputBuffer()
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            _externalRawUI.FlushInputBuffer();
        }

        
        public override
        bool
        KeyAvailable
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                bool result = _externalRawUI.KeyAvailable;

                return result;
            }
        }

        
        public override
        string
        WindowTitle
        {
            get
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                string result = _externalRawUI.WindowTitle;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.WindowTitle = value;
            }
        }

        
        public override
        void
        SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            _externalRawUI.SetBufferContents(origin, contents);
        }

        
        public override
        void
        SetBufferContents(Rectangle r, BufferCell fill)
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            _externalRawUI.SetBufferContents(r, fill);
        }

        
        public override
        BufferCell[,]
        GetBufferContents(Rectangle r)
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            return _externalRawUI.GetBufferContents(r);
        }

        
        public override
        void
        ScrollBufferContents
        (
            Rectangle source,
            Coordinates destination,
            Rectangle clip,
            BufferCell fill
        )
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            _externalRawUI.ScrollBufferContents(source, destination, clip, fill);
        }

        
        public override int LengthInBufferCells(string str)
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            return _externalRawUI.LengthInBufferCells(str);
        }

        
        public override int LengthInBufferCells(string str, int offset)
        {
            Dbg.Assert(offset >= 0, "offset >= 0");
            Dbg.Assert(string.IsNullOrEmpty(str) || (offset < str.Length), "offset < str.Length");

            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            return _externalRawUI.LengthInBufferCells(str, offset);
        }

        
        public override
        int
        LengthInBufferCells(char character)
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            return _externalRawUI.LengthInBufferCells(character);
        }

        private readonly PSHostRawUserInterface _externalRawUI;
        private readonly InternalHost _parentHost;
    }
}
