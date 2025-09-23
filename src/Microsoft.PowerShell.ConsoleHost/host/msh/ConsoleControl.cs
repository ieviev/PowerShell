// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

// Implementation notes: In the functions that take ConsoleHandle parameters, we only assert that the handle is valid and not
// closed, as opposed to doing a check and throwing an exception.  This is because the win32 APIs that those functions wrap will
// fail on invalid/closed handles, and the check for API failure will throw the exception.
//
// On the use of DangerousGetHandle: If the handle has been invalidated, then the API we pass it to will return an error.  These
// handles should not be exposed to recycling attacks (because they are not exposed at all), but if they were, the worse they
// could do is diddle with the console buffer.

using System;
using System.Buffers;
using System.Text;
using System.Runtime.InteropServices;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

using ConsoleHandle = Microsoft.Win32.SafeHandles.SafeFileHandle;

using WORD = System.UInt16;
using ULONG = System.UInt32;
using DWORD = System.UInt32;
using NakedWin32Handle = System.IntPtr;
using HWND = System.IntPtr;
using HDC = System.IntPtr;

#endif

using System.Diagnostics.CodeAnalysis;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    
    internal static class ConsoleControl
    {
#if !UNIX
        #region structs

        internal enum InputRecordEventTypes : ushort
        {
            // from wincon.h.  These look like bit flags, but of course they could not really be used that way, since it would
            // not make sense to have more than one of the INPUT_RECORD union members "in effect" at any one time.

            KEY_EVENT = 0x0001,
            MOUSE_EVENT = 0x0002,
            WINDOW_BUFFER_SIZE_EVENT = 0x0004,
            MENU_EVENT = 0x0008,
            FOCUS_EVENT = 0x0010
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT_RECORD
        {
            internal WORD EventType;
            internal KEY_EVENT_RECORD KeyEvent;
        }

        [Flags]
        internal enum ControlKeyStates : uint
        {
            // From wincon.h.
            RIGHT_ALT_PRESSED = 0x0001, // the right alt key is pressed.
            LEFT_ALT_PRESSED = 0x0002, // the left alt key is pressed.
            RIGHT_CTRL_PRESSED = 0x0004, // the right ctrl key is pressed.
            LEFT_CTRL_PRESSED = 0x0008, // the left ctrl key is pressed.
            SHIFT_PRESSED = 0x0010, // the shift key is pressed.
            NUMLOCK_ON = 0x0020, // the numlock light is on.
            SCROLLLOCK_ON = 0x0040, // the scrolllock light is on.
            CAPSLOCK_ON = 0x0080, // the capslock light is on.
            ENHANCED_KEY = 0x0100  // the key is enhanced.
        }

        // LayoutKind must be Explicit
        [StructLayout(LayoutKind.Sequential)]
        internal struct KEY_EVENT_RECORD
        {
            internal bool KeyDown;

            internal WORD RepeatCount;

            internal WORD VirtualKeyCode;

            internal WORD VirtualScanCode;

            internal char UnicodeChar;

            internal DWORD ControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct COORD
        {
            internal short X;

            internal short Y;

            public override string ToString()
            {
                return string.Create(CultureInfo.InvariantCulture, $"{X},{Y}");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CONSOLE_READCONSOLE_CONTROL
        {
            // from public/internal/windows/inc/winconp.h
            internal ULONG nLength;

            internal ULONG nInitialChars;

            internal ULONG dwCtrlWakeupMask;

            internal  ULONG dwControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CONSOLE_FONT_INFO_EX
        {
            internal int cbSize;
            internal int nFont;
            internal short FontWidth;
            internal short FontHeight;
            internal int FontFamily;
            internal int FontWeight;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string FontFace;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CHAR_INFO
        {
            internal ushort UnicodeChar;

            internal WORD Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SMALL_RECT
        {
            internal short Left;

            internal short Top;

            internal short Right;

            internal short Bottom;

            public override string ToString()
            {
                return string.Create(CultureInfo.InvariantCulture, $"{Left},{Top},{Right},{Bottom}");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CONSOLE_SCREEN_BUFFER_INFO
        {
            internal COORD BufferSize;

            internal COORD CursorPosition;

            internal WORD Attributes;

            internal SMALL_RECT WindowRect;

            internal COORD MaxWindowSize;

            // NTRAID#Windows Out Of Band Releases-938428-2006/07/17-jwh
            // Bring the total size of the struct to 24 bytes.
            internal DWORD Padding;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CONSOLE_CURSOR_INFO
        {
            internal DWORD Size;

            internal bool Visible;

            public override string ToString()
            {
                return string.Create(CultureInfo.InvariantCulture, $"Size: {Size}, Visible: {Visible}");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct FONTSIGNATURE
        {
            // From public\sdk\inc\wingdi.h

            // fsUsb*: A 128-bit Unicode subset bitfield (USB) identifying up to 126 Unicode subranges
            internal DWORD fsUsb0;
            internal DWORD fsUsb1;
            internal DWORD fsUsb2;
            internal DWORD fsUsb3;
            // fsCsb*: A 64-bit, code-page bitfield (CPB) that identifies a specific character set or code page.
            internal DWORD fsCsb0;
            internal DWORD fsCsb1;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CHARSETINFO
        {
            // From public\sdk\inc\wingdi.h
            internal uint ciCharset;   // Character set value.
            internal uint ciACP;       // ANSI code-page identifier.
            internal FONTSIGNATURE fs;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TEXTMETRIC
        {
            // From public\sdk\inc\wingdi.h
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
        }

        #region SentInput Data Structures

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            internal DWORD Type;
            internal MouseKeyboardHardwareInput Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MouseKeyboardHardwareInput
        {
            [FieldOffset(0)]
            internal MouseInput Mouse;

            [FieldOffset(0)]
            internal KeyboardInput Keyboard;

            [FieldOffset(0)]
            internal HardwareInput Hardware;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MouseInput
        {
            
            internal int X;

            
            internal int Y;

            
            internal DWORD MouseData;

            
            internal DWORD Flags;

            
            internal DWORD Time;

            
            internal IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KeyboardInput
        {
            
            internal WORD Vk;

            
            internal WORD Scan;

            
            internal DWORD Flags;

            
            internal DWORD Time;

            
            internal IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HardwareInput
        {
            
            internal DWORD Msg;

            
            internal WORD ParamL;

            
            internal WORD ParamH;
        }

        internal enum VirtualKeyCode : ushort
        {
            
            Left = 0x25,

            
            Return = 0x0D,
        }

        
        internal enum InputType : uint
        {
            
            Mouse = 0,

            
            Keyboard = 1,

            
            Hardware = 2,
        }

        internal enum KeyboardFlag : uint
        {
            
            ExtendedKey = 0x0001,

            
            KeyUp = 0x0002,

            
            Unicode = 0x0004,

            
            ScanCode = 0x0008
        }

        #endregion SentInput Data Structures

        #endregion structs

        #region Window Visibility
        [DllImport(PinvokeDllNames.GetConsoleWindowDllName)]
        internal static extern IntPtr GetConsoleWindow();

        internal const int SW_HIDE = 0;
        internal const int SW_SHOWNORMAL = 1;
        internal const int SW_NORMAL = 1;
        internal const int SW_SHOWMINIMIZED = 2;
        internal const int SW_SHOWMAXIMIZED = 3;
        internal const int SW_MAXIMIZE = 3;
        internal const int SW_SHOWNOACTIVATE = 4;
        internal const int SW_SHOW = 5;
        internal const int SW_MINIMIZE = 6;
        internal const int SW_SHOWMINNOACTIVE = 7;
        internal const int SW_SHOWNA = 8;
        internal const int SW_RESTORE = 9;
        internal const int SW_SHOWDEFAULT = 10;
        internal const int SW_FORCEMINIMIZE = 11;
        internal const int SW_MAX = 11;

#if !UNIX
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        internal static void SetConsoleMode(ProcessWindowStyle style)
        {
            IntPtr hwnd = GetConsoleWindow();
            Dbg.Assert(hwnd != IntPtr.Zero, "Console handle should never be zero");
            switch (style)
            {
                case ProcessWindowStyle.Hidden:
                    ShowWindow(hwnd, SW_HIDE);
                    break;
                case ProcessWindowStyle.Maximized:
                    ShowWindow(hwnd, SW_MAXIMIZE);
                    break;
                case ProcessWindowStyle.Minimized:
                    ShowWindow(hwnd, SW_MINIMIZE);
                    break;
                case ProcessWindowStyle.Normal:
                    ShowWindow(hwnd, SW_NORMAL);
                    break;
            }
        }
#endif
        #endregion

        #region Input break handler (Ctrl-C, Ctrl-Break)

        
        internal enum ConsoleBreakSignal : uint
        {
            // These correspond to the CRTL_XXX_EVENT #defines in public/sdk/inc/wincon.h

            CtrlC = 0,
            CtrlBreak = 1,
            Close = 2,
            Logoff = 5,

            // This only gets received by services

            Shutdown = 6,

            // None is not really a signal -- it's used to indicate that no signal exists.

            None = 0xFF
        }

        // NOTE: this delegate will be executed in its own thread

        internal delegate bool BreakHandler(ConsoleBreakSignal ConsoleBreakSignal);

        
        internal static void AddBreakHandler(BreakHandler handlerDelegate)
        {
            bool result = NativeMethods.SetConsoleCtrlHandler(handlerDelegate, true);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "AddBreakHandler",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.AddBreakHandlerExceptionMessage);
                throw e;
            }
        }

        
        internal static void RemoveBreakHandler()
        {
            bool result = NativeMethods.SetConsoleCtrlHandler(null, false);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "RemoveBreakHandler",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.RemoveBreakHandlerExceptionTemplate);
                throw e;
            }
        }

        #endregion

        #region Win32Handles

        private static readonly Lazy<ConsoleHandle> _keyboardInputHandle = new Lazy<SafeFileHandle>(() =>
            {
                var handle = NativeMethods.CreateFile(
                    "CONIN$",
                    (UInt32)(NativeMethods.AccessQualifiers.GenericRead | NativeMethods.AccessQualifiers.GenericWrite),
                    (UInt32)NativeMethods.ShareModes.ShareRead,
                    (IntPtr)0,
                    (UInt32)NativeMethods.CreationDisposition.OpenExisting,
                    0,
                    (IntPtr)0);

                if (handle == NativeMethods.INVALID_HANDLE_VALUE)
                {
                    int err = Marshal.GetLastWin32Error();

                    HostException e = CreateHostException(err, "RetreiveInputConsoleHandle",
                                                            ErrorCategory.ResourceUnavailable,
                                                            ConsoleControlStrings.GetInputModeExceptionTemplate);
                    throw e;
                }

                return new ConsoleHandle(handle, true);
            }
        );

        
        internal static ConsoleHandle GetConioDeviceHandle()
        {
            return _keyboardInputHandle.Value;
        }

        private static readonly Lazy<ConsoleHandle> _outputHandle = new Lazy<SafeFileHandle>(() =>
            {
                // We use CreateFile here instead of GetStdWin32Handle, as GetStdWin32Handle will return redirected handles
                var handle = NativeMethods.CreateFile(
                    "CONOUT$",
                    (UInt32)(NativeMethods.AccessQualifiers.GenericRead | NativeMethods.AccessQualifiers.GenericWrite),
                    (UInt32)NativeMethods.ShareModes.ShareWrite,
                    (IntPtr)0,
                    (UInt32)NativeMethods.CreationDisposition.OpenExisting,
                    0,
                    (IntPtr)0);

                if (handle == NativeMethods.INVALID_HANDLE_VALUE)
                {
                    int err = Marshal.GetLastWin32Error();

                    HostException e = CreateHostException(err, "RetreiveActiveScreenBufferConsoleHandle",
                        ErrorCategory.ResourceUnavailable, ConsoleControlStrings.GetActiveScreenBufferHandleExceptionTemplate);
                    throw e;
                }

                return new ConsoleHandle(handle, true);
            }
        );

        
        internal static ConsoleHandle GetActiveScreenBufferHandle()
        {
            return _outputHandle.Value;
        }

        #endregion

        #region Mode

        
        [Flags]
        internal enum ConsoleModes : uint
        {
            // These values from wincon.h
            // input modes
            ProcessedInput = 0x001,
            LineInput = 0x002,
            EchoInput = 0x004,
            WindowInput = 0x008,
            MouseInput = 0x010,
            Insert = 0x020,
            QuickEdit = 0x040,
            Extended = 0x080,
            AutoPosition = 0x100,
            // output modes
            ProcessedOutput = 0x001,  // yes, I know they are the same values as some flags defined above.
            WrapEndOfLine = 0x002,
            VirtualTerminal = 0x004,
            // Error getting console mode
            Unknown = 0xffffffff,
        }

        
        internal static ConsoleModes GetMode(ConsoleHandle consoleHandle)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "consoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            UInt32 m = 0;
            bool result = NativeMethods.GetConsoleMode(consoleHandle.DangerousGetHandle(), out m);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "GetConsoleMode",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.GetModeExceptionTemplate);
                throw e;
            }

            return (ConsoleModes)m;
        }

        
        internal static void SetMode(ConsoleHandle consoleHandle, ConsoleModes mode)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "consoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            bool result = NativeMethods.SetConsoleMode(consoleHandle.DangerousGetHandle(), (DWORD)mode);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "SetConsoleMode",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.SetModeExceptionTemplate);
                throw e;
            }
        }

        #endregion

        #region Input

        
        internal static string ReadConsole(
            ConsoleHandle consoleHandle,
            int initialContentLength,
            Span<char> editBuffer,
            int charactersToRead,
            bool endOnTab,
            out uint keyState)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");
            Dbg.Assert(initialContentLength < editBuffer.Length, "initialContentLength must be less than editBuffer.Length");
            Dbg.Assert(charactersToRead < editBuffer.Length, "charactersToRead must be less than editBuffer.Length");
            keyState = 0;

            CONSOLE_READCONSOLE_CONTROL control = new CONSOLE_READCONSOLE_CONTROL();

            control.nLength = (ULONG)Marshal.SizeOf(control);
            control.nInitialChars = (ULONG)initialContentLength;
            control.dwControlKeyState = 0;
            if (endOnTab)
            {
                const int TAB = 0x9;

                control.dwCtrlWakeupMask = (1 << TAB);
            }

            DWORD charsReaded = 0;

            bool result =
                NativeMethods.ReadConsole(
                    consoleHandle.DangerousGetHandle(),
                    editBuffer,
                    (DWORD)charactersToRead,
                    out charsReaded,
                    ref control);
            keyState = control.dwControlKeyState;
            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(
                    err,
                    "ReadConsole",
                    ErrorCategory.ReadError,
                    ConsoleControlStrings.ReadConsoleExceptionTemplate);
                throw e;
            }

            if (charsReaded > (uint)charactersToRead)
            {
                charsReaded = (uint)charactersToRead;
            }

            return editBuffer.Slice(0, (int)charsReaded).ToString();
        }

        
        internal static int ReadConsoleInput(ConsoleHandle consoleHandle, ref INPUT_RECORD[] buffer)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            DWORD recordsRead = 0;
            bool result =
                NativeMethods.ReadConsoleInput(
                    consoleHandle.DangerousGetHandle(),
                    buffer,
                    (DWORD)buffer.Length,
                    out recordsRead);
            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "ReadConsoleInput",
                    ErrorCategory.ReadError, ConsoleControlStrings.ReadConsoleInputExceptionTemplate);
                throw e;
            }

            return (int)recordsRead;
        }

        
        internal static int PeekConsoleInput
        (
            ConsoleHandle consoleHandle,
            ref INPUT_RECORD[] buffer
        )
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            DWORD recordsRead;
            bool result =
                NativeMethods.PeekConsoleInput(
                    consoleHandle.DangerousGetHandle(),
                    buffer,
                    (DWORD)buffer.Length,
                    out recordsRead);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "PeekConsoleInput",
                    ErrorCategory.ReadError, ConsoleControlStrings.PeekConsoleInputExceptionTemplate);
                throw e;
            }

            return (int)recordsRead;
        }

        
        internal static int GetNumberOfConsoleInputEvents(ConsoleHandle consoleHandle)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            DWORD numEvents;
            bool result = NativeMethods.GetNumberOfConsoleInputEvents(consoleHandle.DangerousGetHandle(), out numEvents);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "GetNumberOfConsoleInputEvents",
                    ErrorCategory.ReadError, ConsoleControlStrings.GetNumberOfConsoleInputEventsExceptionTemplate);
                throw e;
            }

            return (int)numEvents;
        }

        
        internal static void FlushConsoleInputBuffer(ConsoleHandle consoleHandle)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            bool result = false;
            NakedWin32Handle h = consoleHandle.DangerousGetHandle();
            result = NativeMethods.FlushConsoleInputBuffer(h);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "FlushConsoleInputBuffer",
                    ErrorCategory.ReadError, ConsoleControlStrings.FlushConsoleInputBufferExceptionTemplate);
                throw e;
            }
        }

        #endregion Input

        #region Buffer

        
        internal static CONSOLE_SCREEN_BUFFER_INFO GetConsoleScreenBufferInfo(ConsoleHandle consoleHandle)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            CONSOLE_SCREEN_BUFFER_INFO bufferInfo;
            bool result = NativeMethods.GetConsoleScreenBufferInfo(consoleHandle.DangerousGetHandle(), out bufferInfo);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "GetConsoleScreenBufferInfo",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.GetConsoleScreenBufferInfoExceptionTemplate);
                throw e;
            }

            return bufferInfo;
        }

        
        internal static void SetConsoleScreenBufferSize(ConsoleHandle consoleHandle, Size newSize)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            COORD s;

            s.X = (short)newSize.Width;
            s.Y = (short)newSize.Height;

            bool result = NativeMethods.SetConsoleScreenBufferSize(consoleHandle.DangerousGetHandle(), s);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "SetConsoleScreenBufferSize",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.SetConsoleScreenBufferSizeExceptionTemplate);
                throw e;
            }
        }

        internal static bool IsConsoleColor(ConsoleColor c)
        {
            switch (c)
            {
                case ConsoleColor.Black:
                case ConsoleColor.Blue:
                case ConsoleColor.Cyan:
                case ConsoleColor.DarkBlue:
                case ConsoleColor.DarkCyan:
                case ConsoleColor.DarkGray:
                case ConsoleColor.DarkGreen:
                case ConsoleColor.DarkMagenta:
                case ConsoleColor.DarkRed:
                case ConsoleColor.DarkYellow:
                case ConsoleColor.Gray:
                case ConsoleColor.Green:
                case ConsoleColor.Magenta:
                case ConsoleColor.Red:
                case ConsoleColor.White:
                case ConsoleColor.Yellow:
                    return true;
            }

            return false;
        }

        internal static void WORDToColor(WORD attribute, out ConsoleColor foreground, out ConsoleColor background)
        {
            // foreground color is the low-byte in the word, background color is the hi-byte.
            foreground = (ConsoleColor)(attribute & 0x0f);
            background = (ConsoleColor)((attribute & 0xf0) >> 4);
            Dbg.Assert(IsConsoleColor(foreground), "unknown color");
            Dbg.Assert(IsConsoleColor(background), "unknown color");
        }

        internal static WORD ColorToWORD(ConsoleColor foreground, ConsoleColor background)
        {
            WORD result = (WORD)(((int)background << 4) | (int)foreground);

            return result;
        }

        
        internal static void WriteConsoleOutput(ConsoleHandle consoleHandle, Coordinates origin, BufferCell[,] contents)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");
            if (contents == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(contents));
            }

            uint codePage;
            if (IsCJKOutputCodePage(out codePage))
            {
                // contentsRegion indicates the area in contents (declared below) in which
                // the data read from ReadConsoleOutput is stored.
                Rectangle contentsRegion = new Rectangle();
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo =
                    GetConsoleScreenBufferInfo(consoleHandle);

                int bufferWidth = bufferInfo.BufferSize.X;
                int bufferHeight = bufferInfo.BufferSize.Y;
                Rectangle screenRegion = new Rectangle(
                    origin.X, origin.Y,
                    Math.Min(origin.X + contents.GetLength(1) - 1, bufferWidth - 1),
                    Math.Min(origin.Y + contents.GetLength(0) - 1, bufferHeight - 1));

                contentsRegion.Left = contents.GetLowerBound(1);
                contentsRegion.Top = contents.GetLowerBound(0);
                contentsRegion.Right = contentsRegion.Left +
                    screenRegion.Right - screenRegion.Left;
                contentsRegion.Bottom = contentsRegion.Top +
                    screenRegion.Bottom - screenRegion.Top;

#if DEBUG
                // Check contents in contentsRegion
                CheckWriteConsoleOutputContents(contents, contentsRegion);
#endif

                // Identify edges and areas of identical contiguous edges in contentsRegion
                List<BufferCellArrayRowTypeRange> sameEdgeAreas = new List<BufferCellArrayRowTypeRange>();
                int firstLeftTrailingRow = -1, firstRightLeadingRow = -1;
                BuildEdgeTypeInfo(contentsRegion, contents,
                    sameEdgeAreas, out firstLeftTrailingRow, out firstRightLeadingRow);

#if DEBUG
                CheckWriteEdges(consoleHandle, codePage, origin, contents, contentsRegion,
                    bufferInfo, firstLeftTrailingRow, firstRightLeadingRow);
#endif

                foreach (BufferCellArrayRowTypeRange area in sameEdgeAreas)
                {
                    Coordinates o = new Coordinates(origin.X,
                                                    origin.Y + area.Start - contentsRegion.Top);
                    Rectangle contRegion = new Rectangle(
                        contentsRegion.Left, area.Start, contentsRegion.Right, area.End);
                    if ((area.Type & BufferCellArrayRowType.LeftTrailing) != 0)
                    {
                        contRegion.Left++;
                        o.X++;
                        if (o.X >= bufferWidth || contRegion.Right < contRegion.Left)
                        {
                            return;
                        }
                    }

                    WriteConsoleOutputCJK(consoleHandle, o, contRegion, contents, area.Type);
                }
            }
            else
            {
                WriteConsoleOutputPlain(consoleHandle, origin, contents);
            }
        }

        private static void BuildEdgeTypeInfo(
            Rectangle contentsRegion,
            BufferCell[,] contents,
            List<BufferCellArrayRowTypeRange> sameEdgeAreas,
            out int firstLeftTrailingRow,
            out int firstRightLeadingRow)
        {
            firstLeftTrailingRow = -1;
            firstRightLeadingRow = -1;
            BufferCellArrayRowType edgeType =
                GetEdgeType(contents[contentsRegion.Top, contentsRegion.Left],
                    contents[contentsRegion.Top, contentsRegion.Right]);
            for (int r = contentsRegion.Top; r <= contentsRegion.Bottom;)
            {
                BufferCellArrayRowTypeRange range;
                range.Start = r;
                range.Type = edgeType;
                if (firstLeftTrailingRow == -1 && ((range.Type & BufferCellArrayRowType.LeftTrailing) != 0))
                {
                    firstLeftTrailingRow = r;
                }

                if (firstRightLeadingRow == -1 && ((range.Type & BufferCellArrayRowType.RightLeading) != 0))
                {
                    firstRightLeadingRow = r;
                }

                while (true)
                {
                    r++;
                    if (r > contentsRegion.Bottom)
                    {
                        range.End = r - 1;
                        sameEdgeAreas.Add(range);
                        return;
                    }

                    edgeType = GetEdgeType(contents[r, contentsRegion.Left], contents[r, contentsRegion.Right]);
                    if (edgeType != range.Type)
                    {
                        range.End = r - 1;
                        sameEdgeAreas.Add(range);
                        break;
                    }
                }
            }
        }

        private static BufferCellArrayRowType GetEdgeType(BufferCell left, BufferCell right)
        {
            BufferCellArrayRowType edgeType = 0;
            if (left.BufferCellType == BufferCellType.Trailing)
            {
                edgeType |= BufferCellArrayRowType.LeftTrailing;
            }

            if (right.BufferCellType == BufferCellType.Leading)
            {
                edgeType |= BufferCellArrayRowType.RightLeading;
            }

            return edgeType;
        }

        private struct BufferCellArrayRowTypeRange
        {
            internal int Start;
            internal int End;
            internal BufferCellArrayRowType Type;
        }

        [Flags]
        private enum BufferCellArrayRowType : uint
        {
            LeftTrailing = 0x1,
            RightLeading = 0x2
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called in CHK builds")]
        internal static void CheckWriteEdges(
            ConsoleHandle consoleHandle,
            uint codePage, Coordinates origin,
            BufferCell[,] contents,
            Rectangle contentsRegion,
            ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo,
            int firstLeftTrailingRow,
            int firstRightLeadingRow)
        {
            Rectangle existingRegion = new Rectangle(0, 0, 1, contentsRegion.Bottom - contentsRegion.Top);
            if (origin.X == 0)
            {
                if (firstLeftTrailingRow >= 0)
                {
                    throw PSTraceSource.NewArgumentException(string.Create(CultureInfo.InvariantCulture, $"contents[{firstLeftTrailingRow}, {contentsRegion.Left}]"));
                }
            }
            else
            {
                // use ReadConsoleOutputCJK because checking the left and right edges of the existing output
                // is NOT needed
                BufferCell[,] leftExisting = new BufferCell[existingRegion.Bottom + 1, 2];
                ReadConsoleOutputCJK(consoleHandle, codePage,
                    new Coordinates(origin.X - 1, origin.Y), existingRegion, ref leftExisting);
                for (int r = contentsRegion.Top, i = 0; r <= contentsRegion.Bottom; r++, i++)
                {
                    if (leftExisting[r, 0].BufferCellType == BufferCellType.Leading ^
                            contents[r, contentsRegion.Left].BufferCellType == BufferCellType.Trailing)
                    {
                        throw PSTraceSource.NewArgumentException(string.Create(CultureInfo.InvariantCulture, $"contents[{r}, {contentsRegion.Left}]"));
                    }
                }
            }
            // Check right edge
            if (origin.X + (contentsRegion.Right - contentsRegion.Left) + 1 >= bufferInfo.BufferSize.X)
            {
                if (firstRightLeadingRow >= 0)
                {
                    throw PSTraceSource.NewArgumentException(string.Create(CultureInfo.InvariantCulture, $"contents[{firstRightLeadingRow}, {contentsRegion.Right}]"));
                }
            }
            else
            {
                // use ReadConsoleOutputCJK because checking the left and right edges of the existing output
                // is NOT needed
                BufferCell[,] rightExisting = new BufferCell[existingRegion.Bottom + 1, 2];
                ReadConsoleOutputCJK(consoleHandle, codePage,
                    new Coordinates(origin.X + (contentsRegion.Right - contentsRegion.Left), origin.Y), existingRegion, ref rightExisting);
                for (int r = contentsRegion.Top, i = 0; r <= contentsRegion.Bottom; r++, i++)
                {
                    if (rightExisting[r, 0].BufferCellType == BufferCellType.Leading ^
                            contents[r, contentsRegion.Right].BufferCellType == BufferCellType.Leading)
                    {
                        throw PSTraceSource.NewArgumentException(string.Create(CultureInfo.InvariantCulture, $"contents[{r}, {contentsRegion.Right}]"));
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called in CHK builds")]
        private static void CheckWriteConsoleOutputContents(BufferCell[,] contents, Rectangle contentsRegion)
        {
            for (int r = contentsRegion.Top; r <= contentsRegion.Bottom; r++)
            {
                for (int c = contentsRegion.Left; c <= contentsRegion.Right; c++)
                {
                    // Changes have been made in the following code such that 2 cell characters
                    // (Chinese, Japanese or Korean) can be in a single BufferCell structure
                    // which is complete
                    if (contents[r, c].BufferCellType == BufferCellType.Trailing &&
                        contents[r, c].Character != 0)
                    {
                        // trailing character is not 0
                        throw PSTraceSource.NewArgumentException(string.Create(CultureInfo.InvariantCulture, $"contents[{r}, {c}]"));
                    }

                    if (contents[r, c].BufferCellType == BufferCellType.Leading)
                    {
                        c++;
                        if (c > contentsRegion.Right)
                        {
                            break;
                        }

                        if (contents[r, c].Character != 0 || contents[r, c].BufferCellType != BufferCellType.Trailing)
                        {
                            // for a 2 cell character, either there is no trailing BufferCell or
                            // the trailing BufferCell's character is not 0
                            throw PSTraceSource.NewArgumentException(string.Create(CultureInfo.InvariantCulture, $"contents[{r}, {c}]"));
                        }
                    }
                }
            }
        }

        private static void WriteConsoleOutputCJK(ConsoleHandle consoleHandle, Coordinates origin, Rectangle contentsRegion, BufferCell[,] contents, BufferCellArrayRowType rowType)
        {
            Dbg.Assert(origin.X >= 0 && origin.Y >= 0,
                "origin must be within the output buffer");
            int rows = contentsRegion.Bottom - contentsRegion.Top + 1;
            int cols = contentsRegion.Right - contentsRegion.Left + 1;

            CONSOLE_FONT_INFO_EX fontInfo = GetConsoleFontInfo(consoleHandle);
            int fontType = fontInfo.FontFamily & NativeMethods.FontTypeMask;
            bool trueTypeInUse = (fontType & NativeMethods.TrueTypeFont) == NativeMethods.TrueTypeFont;

            int bufferLimit = 2 * 1024; // Limit is 8K bytes as each CHAR_INFO takes 4 bytes

            COORD bufferCoord;

            bufferCoord.X = 0;
            bufferCoord.Y = 0;

            // keeps track of which screen area write
            SMALL_RECT writeRegion;

            writeRegion.Top = (short)origin.Y;

            int rowsRemaining = rows;

            while (rowsRemaining > 0)
            {
                // Iteration of columns is nested inside iteration of rows.
                // If the size of contents exceeds the buffer limit, writing is
                // done in blocks of size equal to the bufferlimit from left to right
                // then top to bottom.
                // For each iteration of rows,
                // - writeRegion.Left and bufferSize.X are reset
                // - rowsRemaining, writeRegion.Top, writeRegion.Bottom, and bufferSize.Y
                //     are updated
                //   For each iteration of columns,
                //   - writeRegion.Left, writeRegion.Right and bufferSize.X are updated

                writeRegion.Left = (short)origin.X;

                COORD bufferSize;

                bufferSize.X = (short)Math.Min(cols, bufferLimit);
                bufferSize.Y = (short)Math.Min
                                        (
                                            rowsRemaining,
                                            bufferLimit / bufferSize.X
                                        );
                writeRegion.Bottom = (short)(writeRegion.Top + bufferSize.Y - 1);

                // atRow is at which row of contents a particular iteration is operating
                int atRow = rows - rowsRemaining + contentsRegion.Top;

                // number of columns yet to be written
                int colsRemaining = cols;
                while (colsRemaining > 0)
                {
                    writeRegion.Right = (short)(writeRegion.Left + bufferSize.X - 1);

                    // atCol is at which column of contents a particular iteration is operating
                    int atCol = cols - colsRemaining + contentsRegion.Left;
                    // if this is not the last column iteration &&
                    //   the leftmost BufferCell is a leading cell, don't write that cell
                    if (colsRemaining > bufferSize.X &&
                         contents[atRow, atCol + bufferSize.X - 1].BufferCellType == BufferCellType.Leading)
                    {
                        bufferSize.X--;
                        writeRegion.Right--;
                    }

                    CHAR_INFO[] characterBuffer = new CHAR_INFO[bufferSize.Y * bufferSize.X];

                    // copy characterBuffer to contents;
                    int characterBufferIndex = 0;
                    bool lastCharIsLeading = false;
                    BufferCell lastLeadingCell = new BufferCell();
                    for (int r = atRow; r < bufferSize.Y + atRow; r++)
                    {
                        for (int c = atCol; c < bufferSize.X + atCol; c++, characterBufferIndex++)
                        {
                            if (contents[r, c].BufferCellType == BufferCellType.Complete)
                            {
                                characterBuffer[characterBufferIndex].UnicodeChar =
                                    (ushort)contents[r, c].Character;
                                characterBuffer[characterBufferIndex].Attributes =
                                    (ushort)(ColorToWORD(contents[r, c].ForegroundColor, contents[r, c].BackgroundColor));

                                lastCharIsLeading = false;
                            }
                            else if (contents[r, c].BufferCellType == BufferCellType.Leading)
                            {
                                characterBuffer[characterBufferIndex].UnicodeChar =
                                    (ushort)contents[r, c].Character;
                                characterBuffer[characterBufferIndex].Attributes =
                                    (ushort)(ColorToWORD(contents[r, c].ForegroundColor, contents[r, c].BackgroundColor)
                                                | (ushort)NativeMethods.CHAR_INFO_Attributes.COMMON_LVB_LEADING_BYTE);

                                lastCharIsLeading = true;
                                lastLeadingCell = contents[r, c];
                            }
                            else if (contents[r, c].BufferCellType == BufferCellType.Trailing)
                            {
                                // The FontFamily is a 8-bit integer. The low-order bit (bit 0) specifies the pitch of the font.
                                // If it is 1, the font is variable pitch (or proportional). If it is 0, the font is fixed pitch
                                // (or monospace). Bits 1 and 2 specify the font type. If both bits are 0, the font is a raster font;
                                // if bit 1 is 1 and bit 2 is 0, the font is a vector font; if bit 1 is 0 and bit 2 is set, or if both
                                // bits are 1, the font is true type. Bit 3 is 1 if the font is a device font; otherwise, it is 0.
                                // We only care about the bit 1 and 2, which indicate the font type.
                                // There are only two font type defined for the Console, at
                                // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Console\.
                                //     Console\Nls           --- national language supports
                                //     Console\RasterFonts   --- raster type font
                                //     Console\TrueTypeFont  --- true type font
                                // For CJK characters, if it's TrueType, we need to output the trailing character marked with "Trailing_byte"
                                // attribute. But if it's RasterFont, we ignore the trailing character, and the "Leading_byte"/"Trailing_byte"
                                // attributes are not effective at all when reading the character from the console buffer.
                                if (lastCharIsLeading && trueTypeInUse)
                                {
                                    // For TrueType Font, we output the trailing byte with "Trailing_byte" attribute
                                    characterBuffer[characterBufferIndex].UnicodeChar = lastLeadingCell.Character;
                                    characterBuffer[characterBufferIndex].Attributes =
                                        (ushort)(ColorToWORD(contents[r, c].ForegroundColor, contents[r, c].BackgroundColor)
                                                | (ushort)NativeMethods.CHAR_INFO_Attributes.COMMON_LVB_TRAILING_BYTE);
                                }
                                else
                                {
                                    // We don't output anything for this cell if Raster font is in use, or if the last cell is not a leading byte
                                    characterBufferIndex--;
                                }

                                lastCharIsLeading = false;
                            }
                        }
                    }

                    // Now writeRegion, bufferSize and characterBuffer are updated.
                    // Call NativeMethods.WriteConsoleOutput
                    bool result;
                    if ((rowType & BufferCellArrayRowType.RightLeading) != 0 &&
                            colsRemaining == bufferSize.X)
                    {
                        COORD bSize = bufferSize;
                        bSize.X++;
                        SMALL_RECT wRegion = writeRegion;
                        wRegion.Right++;

                        result = NativeMethods.WriteConsoleOutput(
                            consoleHandle.DangerousGetHandle(),
                            characterBuffer,
                            bSize,
                            bufferCoord,
                            ref wRegion);
                    }
                    else
                    {
                        result = NativeMethods.WriteConsoleOutput(
                            consoleHandle.DangerousGetHandle(),
                            characterBuffer,
                            bufferSize,
                            bufferCoord,
                            ref writeRegion);
                    }

                    if (!result)
                    {
                        // When WriteConsoleOutput fails, half bufferLimit
                        if (bufferLimit < 2)
                        {
                            int err = Marshal.GetLastWin32Error();
                            HostException e = CreateHostException(err, "WriteConsoleOutput",
                                ErrorCategory.WriteError, ConsoleControlStrings.WriteConsoleOutputExceptionTemplate);
                            throw e;
                        }

                        bufferLimit /= 2;
                        if (cols == colsRemaining)
                        {
                            // if cols == colsRemaining, nothing is guaranteed written in this pass and
                            //  the unwritten area is still rectangular
                            bufferSize.Y = 0;
                            break;
                        }
                        else
                        {
                            // some areas have been written. This could only happen when the number of columns
                            // to write is larger than bufferLimit. In that case, the algorithm writes one row
                            // at a time => bufferSize.Y == 1. Then, we can safely leave bufferSize.Y unchanged
                            // to retry with a smaller bufferSize.X.
                            Dbg.Assert(bufferSize.Y == 1, string.Create(CultureInfo.InvariantCulture, $"bufferSize.Y should be 1, but is {bufferSize.Y}"));
                            bufferSize.X = (short)Math.Min(colsRemaining, bufferLimit);
                            continue;
                        }
                    }

                    colsRemaining -= bufferSize.X;
                    writeRegion.Left += bufferSize.X;
                    bufferSize.X = (short)Math.Min(colsRemaining, bufferLimit);
                }  // column iteration

                rowsRemaining -= bufferSize.Y;
                writeRegion.Top += bufferSize.Y;
            }  // row iteration
        }

        private static void WriteConsoleOutputPlain(ConsoleHandle consoleHandle, Coordinates origin, BufferCell[,] contents)
        {
            int rows = contents.GetLength(0);
            int cols = contents.GetLength(1);

            if ((rows <= 0) || cols <= 0)
            {
                tracer.WriteLine("contents passed in has 0 rows and columns");
                return;
            }

            int bufferLimit = 2 * 1024; // Limit is 8K bytes as each CHAR_INFO takes 4 bytes

            COORD bufferCoord;

            bufferCoord.X = 0;
            bufferCoord.Y = 0;

            // keeps track of which screen area write
            SMALL_RECT writeRegion;

            writeRegion.Top = (short)origin.Y;

            int rowsRemaining = rows;

            while (rowsRemaining > 0)
            {
                // Iteration of columns is nested inside iteration of rows.
                // If the size of contents exceeds the buffer limit, writing is
                // done in blocks of size equal to the bufferlimit from left to right
                // then top to bottom.
                // For each iteration of rows,
                // - writeRegion.Left and bufferSize.X are reset
                // - rowsRemaining, writeRegion.Top, writeRegion.Bottom, and bufferSize.Y
                //     are updated
                //   For each iteration of columns,
                //   - writeRegion.Left, writeRegion.Right and bufferSize.X are updated

                writeRegion.Left = (short)origin.X;

                COORD bufferSize;

                bufferSize.X = (short)Math.Min(cols, bufferLimit);
                bufferSize.Y = (short)Math.Min
                                        (
                                            rowsRemaining,
                                            bufferLimit / bufferSize.X
                                        );
                writeRegion.Bottom = (short)(writeRegion.Top + bufferSize.Y - 1);

                // atRow is at which row of contents a particular iteration is operating
                int atRow = rows - rowsRemaining + contents.GetLowerBound(0);

                // number of columns yet to be written
                int colsRemaining = cols;

                while (colsRemaining > 0)
                {
                    writeRegion.Right = (short)(writeRegion.Left + bufferSize.X - 1);

                    // atCol is at which column of contents a particular iteration is operating
                    int atCol = cols - colsRemaining + contents.GetLowerBound(1);
                    CHAR_INFO[] characterBuffer = new CHAR_INFO[bufferSize.Y * bufferSize.X];

                    // copy characterBuffer to contents;
                    for (int r = atRow, characterBufferIndex = 0;
                        r < bufferSize.Y + atRow; r++)
                    {
                        for (int c = atCol; c < bufferSize.X + atCol; c++, characterBufferIndex++)
                        {
                            characterBuffer[characterBufferIndex].UnicodeChar =
                                (ushort)contents[r, c].Character;
                            characterBuffer[characterBufferIndex].Attributes =
                                ColorToWORD(contents[r, c].ForegroundColor, contents[r, c].BackgroundColor);
                        }
                    }

                    // Now writeRegion, bufferSize and characterBuffer are updated.
                    // Call NativeMethods.WriteConsoleOutput
                    bool result =
                        NativeMethods.WriteConsoleOutput(
                            consoleHandle.DangerousGetHandle(),
                            characterBuffer,
                            bufferSize,
                            bufferCoord,
                            ref writeRegion);

                    if (!result)
                    {
                        // When WriteConsoleOutput fails, half bufferLimit
                        if (bufferLimit < 2)
                        {
                            int err = Marshal.GetLastWin32Error();
                            HostException e = CreateHostException(err, "WriteConsoleOutput",
                                ErrorCategory.WriteError, ConsoleControlStrings.WriteConsoleOutputExceptionTemplate);
                            throw e;
                        }

                        bufferLimit /= 2;
                        if (cols == colsRemaining)
                        {
                            // if cols == colsRemaining, nothing is guaranteed written in this pass and
                            //  the unwritten area is still rectangular
                            bufferSize.Y = 0;
                            break;
                        }
                        else
                        {
                            // some areas have been written. This could only happen when the number of columns
                            // to write is larger than bufferLimit. In that case, the algorithm writes one row
                            // at a time => bufferSize.Y == 1. Then, we can safely leave bufferSize.Y unchanged
                            // to retry with a smaller bufferSize.X.
                            Dbg.Assert(bufferSize.Y == 1, string.Create(CultureInfo.InvariantCulture, $"bufferSize.Y should be 1, but is {bufferSize.Y}"));
                            bufferSize.X = (short)Math.Min(colsRemaining, bufferLimit);
                            continue;
                        }
                    }

                    colsRemaining -= bufferSize.X;
                    writeRegion.Left += bufferSize.X;
                    bufferSize.X = (short)Math.Min(colsRemaining, bufferLimit);
                }  // column iteration

                rowsRemaining -= bufferSize.Y;
                writeRegion.Top += bufferSize.Y;
            }  // row iteration
        }

        
        internal static void ReadConsoleOutput
        (
            ConsoleHandle consoleHandle,
            Coordinates origin,
            Rectangle contentsRegion,
            ref BufferCell[,] contents
        )
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");
            uint codePage;
            if (IsCJKOutputCodePage(out codePage))
            {
                ReadConsoleOutputCJK(consoleHandle, codePage, origin, contentsRegion, ref contents);
                // check left edge
                BufferCell[,] cellArray = null;
                Coordinates checkOrigin;
                Rectangle cellArrayRegion = new Rectangle(0, 0, 1, contentsRegion.Bottom - contentsRegion.Top);
                if (origin.X > 0 && ShouldCheck(contentsRegion.Left, contents, contentsRegion))
                {
                    cellArray = new BufferCell[cellArrayRegion.Bottom + 1, 2];
                    checkOrigin = new Coordinates(origin.X - 1, origin.Y);
                    ReadConsoleOutputCJK(consoleHandle, codePage, checkOrigin,
                        cellArrayRegion, ref cellArray);
                    for (int i = 0; i <= cellArrayRegion.Bottom; i++)
                    {
                        if (cellArray[i, 0].BufferCellType == BufferCellType.Leading)
                        {
                            contents[contentsRegion.Top + i, 0].Character = (char)0;
                            contents[contentsRegion.Top + i, 0].BufferCellType = BufferCellType.Trailing;
                        }
                    }
                }

                // check right edge
                ConsoleControl.CONSOLE_SCREEN_BUFFER_INFO bufferInfo =
                    GetConsoleScreenBufferInfo(consoleHandle);
                if (origin.X + (contentsRegion.Right - contentsRegion.Left) + 1 < bufferInfo.BufferSize.X &&
                    ShouldCheck(contentsRegion.Right, contents, contentsRegion))
                {
                    cellArray ??= new BufferCell[cellArrayRegion.Bottom + 1, 2];

                    checkOrigin = new Coordinates(origin.X +
                        (contentsRegion.Right - contentsRegion.Left), origin.Y);
                    ReadConsoleOutputCJK(consoleHandle, codePage, checkOrigin,
                        cellArrayRegion, ref cellArray);
                    for (int i = 0; i <= cellArrayRegion.Bottom; i++)
                    {
                        if (cellArray[i, 0].BufferCellType == BufferCellType.Leading)
                        {
                            contents[contentsRegion.Top + i, contentsRegion.Right] = cellArray[i, 0];
                        }
                    }
                }
            }
            else
            {
                ReadConsoleOutputPlain(consoleHandle, origin, contentsRegion, ref contents);
            }
        }

        #region ReadConsoleOutput CJK
        
        private static bool ShouldCheck(int edge, BufferCell[,] contents, Rectangle contentsRegion)
        {
            for (int i = contentsRegion.Top; i <= contentsRegion.Bottom; i++)
            {
                if (contents[i, edge].Character == ' ')
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ReadConsoleOutputCJKSmall
        (
            ConsoleHandle consoleHandle,
            uint codePage,
            Coordinates origin,
            Rectangle contentsRegion,
            ref BufferCell[,] contents
        )
        {
            COORD bufferSize;
            bufferSize.X = (short)(contentsRegion.Right - contentsRegion.Left + 1);
            bufferSize.Y = (short)(contentsRegion.Bottom - contentsRegion.Top + 1);
            COORD bufferCoord;
            bufferCoord.X = 0;
            bufferCoord.Y = 0;
            CHAR_INFO[] characterBuffer = new CHAR_INFO[bufferSize.X * bufferSize.Y];
            SMALL_RECT readRegion;
            readRegion.Left = (short)origin.X;
            readRegion.Top = (short)origin.Y;
            readRegion.Right = (short)(origin.X + bufferSize.X - 1);
            readRegion.Bottom = (short)(origin.Y + bufferSize.Y - 1);

            bool result = NativeMethods.ReadConsoleOutput(
                                        consoleHandle.DangerousGetHandle(),
                                        characterBuffer,
                                        bufferSize,
                                        bufferCoord,
                                        ref readRegion);
            if (!result)
            {
                return false;
            }

            int characterBufferIndex = 0;

            for (int r = contentsRegion.Top; r <= contentsRegion.Bottom; r++)
            {
                for (int c = contentsRegion.Left; c <= contentsRegion.Right; c++, characterBufferIndex++)
                {
                    ConsoleColor fgColor, bgColor;

                    contents[r, c].Character = (char)characterBuffer[characterBufferIndex].UnicodeChar;
                    WORDToColor(characterBuffer[characterBufferIndex].Attributes,
                                out fgColor,
                                out bgColor);
                    contents[r, c].ForegroundColor = fgColor;
                    contents[r, c].BackgroundColor = bgColor;

                    // Set the attributes of the buffercells to be the same as that of the
                    // incoming CHAR_INFO. In case where the CHAR_INFO character is a
                    // trailing byte set the Character of BufferCell to 0. This is done
                    // because at a lot of places this check is being done. Having a trailing
                    // character to be 0 is by design.

                    if ((characterBuffer[characterBufferIndex].Attributes & (ushort)NativeMethods.CHAR_INFO_Attributes.COMMON_LVB_LEADING_BYTE)
                            == (ushort)NativeMethods.CHAR_INFO_Attributes.COMMON_LVB_LEADING_BYTE)
                    {
                        contents[r, c].BufferCellType = BufferCellType.Leading;
                    }
                    else if ((characterBuffer[characterBufferIndex].Attributes & (ushort)NativeMethods.CHAR_INFO_Attributes.COMMON_LVB_TRAILING_BYTE)
                            == (ushort)NativeMethods.CHAR_INFO_Attributes.COMMON_LVB_TRAILING_BYTE)
                    {
                        contents[r, c].Character = (char)0;
                        contents[r, c].BufferCellType = BufferCellType.Trailing;
                    }
                    else
                    {
                        int charLength = LengthInBufferCells(contents[r, c].Character);
                        if (charLength == 2)
                        {
                            // When it's RasterFont, the "Leading_byte"/"Trailing_byte" are not effective, we
                            // need to decide the leading byte by checking the char length.
                            contents[r, c].BufferCellType = BufferCellType.Leading;
                            c++;
                            contents[r, c].Character = (char)0;
                            contents[r, c].ForegroundColor = fgColor;
                            contents[r, c].BackgroundColor = bgColor;
                            contents[r, c].BufferCellType = BufferCellType.Trailing;
                        }
                        else
                        {
                            contents[r, c].BufferCellType = BufferCellType.Complete;
                        }
                    }
                }
            }

            return true;
        }

        
        internal static void ReadConsoleOutputCJK
        (
            ConsoleHandle consoleHandle,
            uint codePage,
            Coordinates origin,
            Rectangle contentsRegion,
            ref BufferCell[,] contents
        )
        {
            int rows = contentsRegion.Bottom - contentsRegion.Top + 1;
            int cols = contentsRegion.Right - contentsRegion.Left + 1;

            if ((rows <= 0) || cols <= 0)
            {
                tracer.WriteLine("invalid contents region");
                return;
            }

            int bufferLimit = 2 * 1024; // Limit is 8K bytes as each CHAR_INFO takes 4 bytes

            COORD bufferCoord;

            bufferCoord.X = 0;
            bufferCoord.Y = 0;

            // keeps track of which screen area is read
            SMALL_RECT readRegion;

            readRegion.Top = (short)origin.Y;

            int rowsRemaining = rows;

            while (rowsRemaining > 0)
            {
                // Iteration of columns is nested inside iteration of rows.
                // If the size of contents exceeds the buffer limit, reading is
                // done in blocks of size equal to the bufferlimit from left to right
                // then top to bottom.
                // For each iteration of rows,
                // - readRegion.Left and bufferSize.X are reset
                // - rowsRemaining, readRegion.Top, readRegion.Bottom, and bufferSize.Y
                //     are updated
                //   For each iteration of columns,
                //   - readRegion.Left, readRegion.Right and bufferSize.X are updated

                readRegion.Left = (short)origin.X;

                COORD bufferSize;
                bufferSize.X = (short)Math.Min(cols, bufferLimit);
                bufferSize.Y = (short)Math.Min
                                        (
                                            rowsRemaining,
                                            bufferLimit / bufferSize.X
                                        );
                readRegion.Bottom = (short)(readRegion.Top + bufferSize.Y - 1);

                // atContentsRow is at which row of contents a particular iteration is operating
                int atContentsRow = rows - rowsRemaining + contentsRegion.Top;

                // number of columns yet to be read
                int colsRemaining = cols;

                while (colsRemaining > 0)
                {
                    // atContentsCol is at which column of contents a particular iteration is operating
                    int atContentsCol = cols - colsRemaining + contentsRegion.Left;

                    readRegion.Right = (short)(readRegion.Left + bufferSize.X - 1);

                    // Now readRegion and bufferSize are updated.
                    Rectangle atContents = new Rectangle(atContentsCol, atContentsRow,
                                atContentsCol + bufferSize.X - 1, atContentsRow + bufferSize.Y - 1);
                    bool result =
                        ReadConsoleOutputCJKSmall(consoleHandle, codePage,
                            new Coordinates(readRegion.Left, readRegion.Top),
                            atContents,
                            ref contents);
                    if (!result)
                    {
                        // When WriteConsoleOutput fails, half bufferLimit
                        if (bufferLimit < 2)
                        {
                            int err = Marshal.GetLastWin32Error();

                            HostException e = CreateHostException(err, "ReadConsoleOutput",
                                ErrorCategory.ReadError, ConsoleControlStrings.ReadConsoleOutputExceptionTemplate);
                            throw e;
                        }
                        else
                        {
                            // if cols == colsRemaining, nothing is guaranteed read in this pass and
                            //  the unread area is still rectangular
                            bufferLimit /= 2;
                            if (cols == colsRemaining)
                            {
                                bufferSize.Y = 0;
                                break;
                            }
                            else
                            {
                                // some areas have been read. This could only happen when the number of columns
                                // to write is larger than bufferLimit. In that case, the algorithm reads one row
                                // at a time => bufferSize.Y == 1. Then, we can safely leave bufferSize.Y unchanged
                                // to retry with a smaller bufferSize.X.
                                Dbg.Assert(bufferSize.Y == 1, string.Create(CultureInfo.InvariantCulture, $"bufferSize.Y should be 1, but is {bufferSize.Y}"));
                                bufferSize.X = (short)Math.Min(colsRemaining, bufferLimit);
                                continue;
                            }
                        }
                    }

                    colsRemaining -= bufferSize.X;
                    readRegion.Left += bufferSize.X;
                    if (colsRemaining > 0 && (bufferSize.Y == 1) &&
                        (contents[atContents.Bottom, atContents.Right].Character == ' '))
                    {
                        colsRemaining++;
                        readRegion.Left--;
                    }

                    bufferSize.X = (short)Math.Min(colsRemaining, bufferLimit);
                }  // column iteration

                rowsRemaining -= bufferSize.Y;
                readRegion.Top += bufferSize.Y;
            }  // row iteration

            // The following nested loop set the value of the empty cells in contents:
            // character to ' '
            // foreground color to console's foreground color
            // background color to console's background color
            int rowIndex = contents.GetLowerBound(0);
            int rowEnd = contents.GetUpperBound(0);
            int colBegin = contents.GetLowerBound(1);
            int colEnd = contents.GetUpperBound(1);
            CONSOLE_SCREEN_BUFFER_INFO bufferInfo =
                        GetConsoleScreenBufferInfo(consoleHandle);
            ConsoleColor foreground = 0;
            ConsoleColor background = 0;

            WORDToColor(
                            bufferInfo.Attributes,
                            out foreground,
                            out background
                        );

            while (rowIndex <= rowEnd)
            {
                int colIndex = colBegin;
                while (true)
                {
                    // if contents[rowIndex,colIndex] is in contentsRegion, hence a non-empty cell,
                    // move colIndex to one past the right end of contentsRegion
                    if (contentsRegion.Top <= rowIndex && rowIndex <= contentsRegion.Bottom &&
                        contentsRegion.Left <= colIndex && colIndex <= contentsRegion.Right)
                    {
                        colIndex = contentsRegion.Right + 1;
                    }
                    // colIndex past contents last column
                    if (colIndex > colEnd)
                    {
                        break;
                    }

                    contents[rowIndex, colIndex] = new BufferCell(
                        ' ', foreground, background, BufferCellType.Complete);
                    colIndex++;
                }

                rowIndex++;
            }
        }
        #endregion ReadConsoleOutput CJK

        private static void ReadConsoleOutputPlain
        (
            ConsoleHandle consoleHandle,
            Coordinates origin,
            Rectangle contentsRegion,
            ref BufferCell[,] contents
        )
        {
            int rows = contentsRegion.Bottom - contentsRegion.Top + 1;
            int cols = contentsRegion.Right - contentsRegion.Left + 1;

            if ((rows <= 0) || cols <= 0)
            {
                tracer.WriteLine("invalid contents region");
                return;
            }

            int bufferLimit = 2 * 1024; // Limit is 8K bytes as each CHAR_INFO takes 4 bytes

            COORD bufferCoord;

            bufferCoord.X = 0;
            bufferCoord.Y = 0;

            // keeps track of which screen area read
            SMALL_RECT readRegion;

            readRegion.Top = (short)origin.Y;

            int rowsRemaining = rows;

            while (rowsRemaining > 0)
            {
                // Iteration of columns is nested inside iteration of rows.
                // If the size of contents exceeds the buffer limit, reading is
                // done in blocks of size equal to the bufferlimit from left to right
                // then top to bottom.
                // For each iteration of rows,
                // - readRegion.Left and bufferSize.X are reset
                // - rowsRemaining, readRegion.Top, readRegion.Bottom, and bufferSize.Y
                //     are updated
                //   For each iteration of columns,
                //   - readRegion.Left, readRegion.Right and bufferSize.X are updated

                readRegion.Left = (short)origin.X;

                COORD bufferSize;
                bufferSize.X = (short)Math.Min(cols, bufferLimit);
                bufferSize.Y = (short)Math.Min
                                        (
                                            rowsRemaining,
                                            bufferLimit / bufferSize.X
                                        );
                readRegion.Bottom = (short)(readRegion.Top + bufferSize.Y - 1);

                // atContentsRow is at which row of contents a particular iteration is operating
                int atContentsRow = rows - rowsRemaining + contentsRegion.Top;

                // number of columns yet to be read
                int colsRemaining = cols;

                while (colsRemaining > 0)
                {
                    readRegion.Right = (short)(readRegion.Left + bufferSize.X - 1);

                    // Now readRegion and bufferSize are updated.
                    // Call NativeMethods.ReadConsoleOutput
                    CHAR_INFO[] characterBuffer = new CHAR_INFO[bufferSize.Y * bufferSize.X];
                    bool result = NativeMethods.ReadConsoleOutput(
                                        consoleHandle.DangerousGetHandle(),
                                        characterBuffer,
                                        bufferSize,
                                        bufferCoord,
                                        ref readRegion);

                    if (!result)
                    {
                        // When WriteConsoleOutput fails, half bufferLimit
                        if (bufferLimit < 2)
                        {
                            int err = Marshal.GetLastWin32Error();

                            HostException e = CreateHostException(err, "ReadConsoleOutput",
                                ErrorCategory.ReadError, ConsoleControlStrings.ReadConsoleOutputExceptionTemplate);
                            throw e;
                        }
                        // if cols == colsRemaining, nothing is guaranteed read in this pass and
                        //  the unread area is still rectangular
                        bufferLimit /= 2;
                        if (cols == colsRemaining)
                        {
                            bufferSize.Y = 0;
                            break;
                        }
                        else
                        {
                            // some areas have been read. This could only happen when the number of columns
                            // to write is larger than bufferLimit. In that case, the algorithm reads one row
                            // at a time => bufferSize.Y == 1. Then, we can safely leave bufferSize.Y unchanged
                            // to retry with a smaller bufferSize.X.
                            Dbg.Assert(bufferSize.Y == 1, string.Create(CultureInfo.InvariantCulture, $"bufferSize.Y should be 1, but is {bufferSize.Y}"));
                            bufferSize.X = (short)Math.Min(colsRemaining, bufferLimit);
                            continue;
                        }
                    }

                    // atContentsCol is at which column of contents a particular iteration is operating
                    int atContentsCol = cols - colsRemaining + contentsRegion.Left;

                    // copy characterBuffer to contents;
                    int characterBufferIndex = 0;
                    for (int r = atContentsRow; r < bufferSize.Y + atContentsRow; r++)
                    {
                        for (int c = atContentsCol; c < bufferSize.X + atContentsCol; c++, characterBufferIndex++)
                        {
                            contents[r, c].Character = (char)characterBuffer[characterBufferIndex].UnicodeChar;
                            ConsoleColor fgColor, bgColor;
                            WORDToColor(characterBuffer[characterBufferIndex].Attributes,
                                out fgColor,
                                out bgColor);
                            contents[r, c].ForegroundColor = fgColor;
                            contents[r, c].BackgroundColor = bgColor;
                        }
                    }

                    colsRemaining -= bufferSize.X;
                    readRegion.Left += bufferSize.X;
                    bufferSize.X = (short)Math.Min(colsRemaining, bufferLimit);
                }  // column iteration

                rowsRemaining -= bufferSize.Y;
                readRegion.Top += bufferSize.Y;
            }  // row iteration

            // The following nested loop set the value of the empty cells in contents:
            // character to ' '
            // foreground color to console's foreground color
            // background color to console's background color
            int rowIndex = contents.GetLowerBound(0);
            int rowEnd = contents.GetUpperBound(0);
            int colBegin = contents.GetLowerBound(1);
            int colEnd = contents.GetUpperBound(1);
            CONSOLE_SCREEN_BUFFER_INFO bufferInfo =
                        GetConsoleScreenBufferInfo(consoleHandle);
            ConsoleColor foreground = 0;
            ConsoleColor background = 0;

            WORDToColor(
                            bufferInfo.Attributes,
                            out foreground,
                            out background
                       );

            while (rowIndex <= rowEnd)
            {
                int colIndex = colBegin;
                while (true)
                {
                    // if contents[rowIndex,colIndex] is in contentsRegion, hence a non-empty cell,
                    // move colIndex to one past the right end of contentsRegion
                    if (contentsRegion.Top <= rowIndex && rowIndex <= contentsRegion.Bottom &&
                        contentsRegion.Left <= colIndex && colIndex <= contentsRegion.Right)
                    {
                        colIndex = contentsRegion.Right + 1;
                    }
                    // colIndex past contents last column
                    if (colIndex > colEnd)
                    {
                        break;
                    }

                    contents[rowIndex, colIndex].Character = ' ';
                    contents[rowIndex, colIndex].ForegroundColor = foreground;
                    contents[rowIndex, colIndex].BackgroundColor = background;
                    colIndex++;
                }

                rowIndex++;
            }
        }

        
        internal static void FillConsoleOutputCharacter
        (
            ConsoleHandle consoleHandle,
            char character,
            int numberToWrite,
            Coordinates origin
        )
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            COORD c;

            c.X = (short)origin.X;
            c.Y = (short)origin.Y;

            bool result =
                NativeMethods.FillConsoleOutputCharacter(
                    consoleHandle.DangerousGetHandle(),
                    character,
                    (DWORD)numberToWrite,
                    c,
                    out _);
            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "FillConsoleOutputCharacter",
                    ErrorCategory.WriteError, ConsoleControlStrings.FillConsoleOutputCharacterExceptionTemplate);
                throw e;
            }
            // we don't assert that the number actually written matches the number we asked for, as the function may clip if
            // the number of cells to write extends past the end of the screen buffer.
        }

        
        internal static void FillConsoleOutputAttribute
        (
            ConsoleHandle consoleHandle,
            WORD attribute,
            int numberToWrite,
            Coordinates origin
        )
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            COORD c;

            c.X = (short)origin.X;
            c.Y = (short)origin.Y;

            bool result =
                NativeMethods.FillConsoleOutputAttribute(
                    consoleHandle.DangerousGetHandle(),
                    attribute,
                    (DWORD)numberToWrite,
                    c,
                    out _);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "FillConsoleOutputAttribute",
                    ErrorCategory.WriteError, ConsoleControlStrings.FillConsoleOutputAttributeExceptionTemplate);
                throw e;
            }
        }

        
        internal static void ScrollConsoleScreenBuffer
        (
            ConsoleHandle consoleHandle,
            SMALL_RECT scrollRectangle,
            SMALL_RECT clipRectangle,
            COORD destOrigin, CHAR_INFO fill
        )
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            bool result =
                NativeMethods.ScrollConsoleScreenBuffer(
                    consoleHandle.DangerousGetHandle(),
                    ref scrollRectangle,
                    ref clipRectangle,
                    destOrigin,
                    ref fill);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "ScrollConsoleScreenBuffer",
                    ErrorCategory.WriteError, ConsoleControlStrings.ScrollConsoleScreenBufferExceptionTemplate);
                throw e;
            }
        }

        #endregion Buffer

        #region Window

        
        internal static void SetConsoleWindowInfo(ConsoleHandle consoleHandle, bool absolute, SMALL_RECT windowInfo)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            bool result = NativeMethods.SetConsoleWindowInfo(consoleHandle.DangerousGetHandle(), absolute, ref windowInfo);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "SetConsoleWindowInfo",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.SetConsoleWindowInfoExceptionTemplate);
                throw e;
            }
        }

        
        internal static Size GetLargestConsoleWindowSize(ConsoleHandle consoleHandle)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            COORD result = NativeMethods.GetLargestConsoleWindowSize(consoleHandle.DangerousGetHandle());

            if ((result.X == 0) && (result.Y == 0))
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "GetLargestConsoleWindowSize",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.GetLargestConsoleWindowSizeExceptionTemplate);
                throw e;
            }

            return new Size(result.X, result.Y);
        }

        
        internal static string GetConsoleWindowTitle()
        {
            const int MaxWindowTitleLength = 1024;
            const DWORD bufferSize = MaxWindowTitleLength;
            DWORD result;
            StringBuilder consoleTitle = new StringBuilder((int)bufferSize);

            result = NativeMethods.GetConsoleTitle(consoleTitle, bufferSize);
            // If the result is zero, it may mean and error but it may also mean
            // that the window title has been set to null. Since we can't tell the
            // the difference, we'll just return the empty string every time.
            if (result == 0)
            {
                return string.Empty;
            }

            return consoleTitle.ToString();
        }

        private static bool s_dontsetConsoleWindowTitle;

        
        internal static void SetConsoleWindowTitle(string consoleTitle)
        {
            if (s_dontsetConsoleWindowTitle)
            {
                return;
            }

            bool result = NativeMethods.SetConsoleTitle(consoleTitle);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                // ERROR_GEN_FAILURE is returned if this api can't be used with the terminal
                if (err == 0x1f)
                {
                    tracer.WriteLine("Call to SetConsoleTitle failed: {0}", err);
                    s_dontsetConsoleWindowTitle = true;

                    // We ignore this specific error as the console can still continue to operate
                    return;
                }

                HostException e = CreateHostException(err, "SetConsoleWindowTitle",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.SetConsoleWindowTitleExceptionTemplate);
                throw e;
            }
        }

        #endregion Window

        
        internal static void WriteConsole(ConsoleHandle consoleHandle, ReadOnlySpan<char> output, bool newLine)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            if (output.Length == 0)
            {
                if (newLine)
                {
                    WriteConsole(consoleHandle, Environment.NewLine);
                }

                return;
            }

            // Native WriteConsole doesn't support output buffer longer than 64K, so we need to chop the output string if it is too long.
            // This records the chopping position in output string.
            int cursor = 0;
            // This is 64K/4 - 1 to account for possible width of each character.
            const int MaxBufferSize = 16383;
            const int MaxStackAllocSize = 512;
            ReadOnlySpan<char> outBuffer;

            // In case that a new line is required, we try to write out the last chunk and the new-line string together,
            // to avoid one extra call to 'WriteConsole' just for a new line string.
            while (cursor + MaxBufferSize < output.Length)
            {
                outBuffer = output.Slice(cursor, MaxBufferSize);
                cursor += MaxBufferSize;
                WriteConsole(consoleHandle, outBuffer);
            }

            outBuffer = output.Slice(cursor);
            if (!newLine)
            {
                WriteConsole(consoleHandle, outBuffer);
                return;
            }

            char[] rentedArray = null;
            string lineEnding = Environment.NewLine;
            int size = outBuffer.Length + lineEnding.Length;

            // We expect the 'size' will often be small, and thus optimize that case with 'stackalloc'.
            Span<char> buffer = size <= MaxStackAllocSize ? stackalloc char[size] : default;

            try
            {
                if (buffer.IsEmpty)
                {
                    rentedArray = ArrayPool<char>.Shared.Rent(size);
                    buffer = rentedArray.AsSpan().Slice(0, size);
                }

                outBuffer.CopyTo(buffer);
                lineEnding.CopyTo(buffer.Slice(outBuffer.Length));
                WriteConsole(consoleHandle, buffer);
            }
            finally
            {
                if (rentedArray is not null)
                {
                    ArrayPool<char>.Shared.Return(rentedArray);
                }
            }
        }

        private static void WriteConsole(ConsoleHandle consoleHandle, ReadOnlySpan<char> buffer)
        {
            DWORD charsWritten;
            bool result =
                NativeMethods.WriteConsole(
                    consoleHandle.DangerousGetHandle(),
                    buffer,
                    (DWORD)buffer.Length,
                    out charsWritten,
                    IntPtr.Zero);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(
                    err,
                    "WriteConsole",
                    ErrorCategory.WriteError,
                    ConsoleControlStrings.WriteConsoleExceptionTemplate);
                throw e;
            }
        }

        
        internal static void SetConsoleTextAttribute(ConsoleHandle consoleHandle, WORD attribute)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            bool result = NativeMethods.SetConsoleTextAttribute(consoleHandle.DangerousGetHandle(), attribute);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "SetConsoleTextAttribute",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.SetConsoleTextAttributeExceptionTemplate);
                throw e;
            }
        }

#endif
        #region Dealing with CJK

        // Return the length of a VT100 control sequence character in str starting
        // at the given offset.
        //
        // This code only handles the following formatting sequences, corresponding to
        // the patterns:
        //     CSI params? 'm'               // SGR: Select Graphics Rendition
        //     CSI params? '#' [{}pq]        // XTPUSHSGR ('{'), XTPOPSGR ('}'), or their aliases ('p' and 'q')
        //
        // Where:
        //     params: digit+ ((';' | ':') params)?
        //     CSI:     C0_CSI | C1_CSI
        //     C0_CSI:  \x001b '['            // ESC '['
        //     C1_CSI:  \x009b
        //
        // There are many other VT100 escape sequences, but these text attribute sequences
        // (color-related, underline, etc.) are sufficient for our formatting system.  We
        // won't handle cursor movements or other attempts at animation.
        //
        // Note that offset is adjusted past the escape sequence, or at least one
        // character forward if there is no escape sequence at the specified position.
        internal static int ControlSequenceLength(string str, ref int offset)
        {
            var start = offset;

            // First, check for the CSI:
            if ((str[offset] == (char)0x1b) && (str.Length > (offset + 1)) && (str[offset + 1] == '['))
            {
                // C0 CSI
                offset += 2;
            }
            else if (str[offset] == (char)0x9b)
            {
                // C1 CSI
                offset += 1;
            }
            else
            {
                // No CSI at the current location, so we are done looking, but we still
                // need to advance offset.
                offset += 1;
                return 0;
            }

            if (offset >= str.Length)
            {
                return 0;
            }

            // Next, handle possible numeric arguments:
            char c;
            do
            {
                c = str[offset++];
            }
            while ((offset < str.Length) && (char.IsDigit(c) || (c == ';') || (c == ':')));

            // Finally, handle the command characters for the specific sequences we
            // handle:
            if (c == 'm')
            {
                // SGR: Select Graphics Rendition
                return offset - start;
            }

            // Maybe XTPUSHSGR or XTPOPSGR, but we need to read another char. Offset is
            // already positioned on the next char (or past the end).
            if (offset >= str.Length)
            {
                return 0;
            }

            if (c == '#')
            {
                // '{' : XTPUSHSGR
                // '}' : XTPOPSGR
                // 'p' : alias for XTPUSHSGR
                // 'q' : alias for XTPOPSGR
                c = str[offset++];
                if ((c == '{') ||
                    (c == '}') ||
                    (c == 'p') ||
                    (c == 'q'))
                {
                    return offset - start;
                }
            }

            return 0;
        }

        
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults",
            MessageId = "Microsoft.PowerShell.ConsoleControl+NativeMethods.ReleaseDC(System.IntPtr,System.IntPtr)")]
        internal static int LengthInBufferCells(string str, int offset, bool checkEscapeSequences)
        {
            Dbg.Assert(offset >= 0, "offset >= 0");
            Dbg.Assert(string.IsNullOrEmpty(str) || (offset < str.Length), "offset < str.Length");

            var escapeSequenceAdjustment = 0;
            if (checkEscapeSequences)
            {
                int i = 0;
                while (i < offset)
                {
                    ControlSequenceLength(str, ref i);
                }

                // If offset != i, we're in the middle of a sequence, which the caller should avoid,
                // but we'll tolerate.
                while (i < str.Length)
                {
                    escapeSequenceAdjustment += ControlSequenceLength(str, ref i);
                }
            }

            int length = 0;
            foreach (char c in str)
            {
                length += LengthInBufferCells(c);
            }

            return length - offset - escapeSequenceAdjustment;
        }

        internal static int LengthInBufferCells(char c)
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

#if !UNIX

        
        internal static bool IsCJKOutputCodePage(out uint codePage)
        {
            codePage = NativeMethods.GetConsoleOutputCP();
            return codePage == 932 || // Japanese
                codePage == 936 || // Simplified Chinese
                codePage == 949 || // Korean
                codePage == 950;  // Traditional Chinese
        }

#endif
        #endregion Dealing with CJK

#if !UNIX

        #region Cursor

        
        internal static CONSOLE_CURSOR_INFO GetConsoleCursorInfo(ConsoleHandle consoleHandle)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            CONSOLE_CURSOR_INFO cursorInfo;

            bool result = NativeMethods.GetConsoleCursorInfo(consoleHandle.DangerousGetHandle(), out cursorInfo);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "GetConsoleCursorInfo",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.GetConsoleCursorInfoExceptionTemplate);
                throw e;
            }

            return cursorInfo;
        }

        internal static CONSOLE_FONT_INFO_EX GetConsoleFontInfo(ConsoleHandle consoleHandle)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            CONSOLE_FONT_INFO_EX fontInfo = new CONSOLE_FONT_INFO_EX();
            fontInfo.cbSize = Marshal.SizeOf(fontInfo);
            bool result = NativeMethods.GetCurrentConsoleFontEx(consoleHandle.DangerousGetHandle(), false, ref fontInfo);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "GetConsoleFontInfo",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.GetConsoleFontInfoExceptionTemplate);
                throw e;
            }

            return fontInfo;
        }

        
        internal static void SetConsoleCursorInfo(ConsoleHandle consoleHandle, CONSOLE_CURSOR_INFO cursorInfo)
        {
            Dbg.Assert(!consoleHandle.IsInvalid, "ConsoleHandle is not valid");
            Dbg.Assert(!consoleHandle.IsClosed, "ConsoleHandle is closed");

            bool result = NativeMethods.SetConsoleCursorInfo(consoleHandle.DangerousGetHandle(), ref cursorInfo);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "SetConsoleCursorInfo",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.SetConsoleCursorInfoExceptionTemplate);
                throw e;
            }
        }

        #endregion Cursor

        #region helper

        
        private static HostException CreateHostException(
            int win32Error, string errorId, ErrorCategory category, string resourceStr)
        {
            Win32Exception innerException = new Win32Exception(win32Error);
            string msg = StringUtil.Format(resourceStr, innerException.Message, win32Error);
            HostException e = new HostException(msg, innerException, errorId, category);
            return e;
        }

        #endregion helper

        #region SendInput

        internal static void MimicKeyPress(INPUT[] inputs)
        {
            Dbg.Assert(inputs != null && inputs.Length > 0, "inputs should not be null or empty");
            var numberOfSuccessfulEvents = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

            if (numberOfSuccessfulEvents == 0)
            {
                int err = Marshal.GetLastWin32Error();

                HostException e = CreateHostException(err, "SendKeyPressInput",
                    ErrorCategory.ResourceUnavailable, ConsoleControlStrings.SendKeyPressInputExceptionTemplate);
                throw e;
            }
        }

        #endregion SendInput

        
        internal static class NativeMethods
        {
            internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h

            internal const int FontTypeMask = 0x06;
            internal const int TrueTypeFont = 0x04;

            #region CreateFile

            [Flags]
            internal enum AccessQualifiers : uint
            {
                // From winnt.h
                GenericRead = 0x80000000,
                GenericWrite = 0x40000000
            }

            [Flags]
            internal enum ShareModes : uint
            {
                // From winnt.h
                ShareRead = 0x00000001,
                ShareWrite = 0x00000002
            }

            internal enum CreationDisposition : uint
            {
                // From winbase.h
                CreateNew = 1,
                CreateAlways = 2,
                OpenExisting = 3,
                OpenAlways = 4,
                TruncateExisting = 5
            }

            [DllImport(PinvokeDllNames.CreateFileDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern NakedWin32Handle CreateFile
            (
                string fileName,
                DWORD desiredAccess,
                DWORD ShareModes,
                IntPtr securityAttributes,
                DWORD creationDisposition,
                DWORD flagsAndAttributes,
                NakedWin32Handle templateFileWin32Handle
            );

            #endregion CreateFile

            #region Code Page

            [DllImport(PinvokeDllNames.GetConsoleOutputCPDllName, SetLastError = false, CharSet = CharSet.Unicode)]
            internal static extern uint GetConsoleOutputCP();

            #endregion Code Page

            [DllImport(PinvokeDllNames.GetConsoleWindowDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern HWND GetConsoleWindow();

            [DllImport(PinvokeDllNames.GetDCDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern HDC GetDC(HWND hwnd);

            [DllImport(PinvokeDllNames.ReleaseDCDllName, SetLastError = false, CharSet = CharSet.Unicode)]
            internal static extern int ReleaseDC(HWND hwnd, HDC hdc);

            [DllImport(PinvokeDllNames.FlushConsoleInputBufferDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FlushConsoleInputBuffer(NakedWin32Handle consoleInput);

            [DllImport(PinvokeDllNames.FillConsoleOutputAttributeDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FillConsoleOutputAttribute
            (
                NakedWin32Handle consoleOutput,
                WORD attribute,
                DWORD length,
                COORD writeCoord,
                out DWORD numberOfAttrsWritten
            );

            [DllImport(PinvokeDllNames.FillConsoleOutputCharacterDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FillConsoleOutputCharacter
            (
                NakedWin32Handle consoleOutput,
                char character,
                DWORD length,
                COORD writeCoord,
                out DWORD numberOfCharsWritten
            );

            [DllImport(PinvokeDllNames.WriteConsoleDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern unsafe bool WriteConsole
            (
                NakedWin32Handle consoleOutput,
                char* buffer,
                DWORD numberOfCharsToWrite,
                out DWORD numberOfCharsWritten,
                IntPtr reserved
            );

            internal static unsafe bool WriteConsole
            (
                NakedWin32Handle consoleOutput,
                ReadOnlySpan<char> buffer,
                DWORD numberOfCharsToWrite,
                out DWORD numberOfCharsWritten,
                IntPtr reserved
            )
            {
                fixed (char* bufferPtr = &MemoryMarshal.GetReference(buffer))
                {
                    return WriteConsole(consoleOutput, bufferPtr, numberOfCharsToWrite, out numberOfCharsWritten, reserved);
                }
            }

            [DllImport(PinvokeDllNames.GetConsoleTitleDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern DWORD GetConsoleTitle(StringBuilder consoleTitle, DWORD size);

            [DllImport(PinvokeDllNames.SetConsoleTitleDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleTitle(string consoleTitle);

            [DllImport(PinvokeDllNames.GetConsoleModeDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleMode(NakedWin32Handle consoleHandle, out UInt32 mode);

            [DllImport(PinvokeDllNames.GetConsoleScreenBufferInfoDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleScreenBufferInfo(NakedWin32Handle consoleHandle, out CONSOLE_SCREEN_BUFFER_INFO consoleScreenBufferInfo);

            [DllImport(PinvokeDllNames.GetLargestConsoleWindowSizeDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern COORD GetLargestConsoleWindowSize(NakedWin32Handle consoleOutput);

            [DllImport(PinvokeDllNames.ReadConsoleDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern unsafe bool ReadConsole
            (
                NakedWin32Handle consoleInput,
                char* lpBuffer,
                DWORD numberOfCharsToRead,
                out DWORD numberOfCharsRead,
                ref CONSOLE_READCONSOLE_CONTROL controlData
            );

            internal static unsafe bool ReadConsole
            (
                NakedWin32Handle consoleInput,
                Span<char> buffer,
                DWORD numberOfCharsToRead,
                out DWORD numberOfCharsRead,
                ref CONSOLE_READCONSOLE_CONTROL controlData
            )
            {
                fixed (char* bufferPtr = &MemoryMarshal.GetReference(buffer))
                {
                    return ReadConsole(consoleInput, bufferPtr, numberOfCharsToRead, out numberOfCharsRead, ref controlData);
                }
            }

            [DllImport(PinvokeDllNames.PeekConsoleInputDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PeekConsoleInput
            (
                NakedWin32Handle consoleInput,
                [Out] INPUT_RECORD[] buffer,
                DWORD length,
                out DWORD numberOfEventsRead
            );

            [DllImport(PinvokeDllNames.GetNumberOfConsoleInputEventsDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetNumberOfConsoleInputEvents(NakedWin32Handle consoleInput, out DWORD numberOfEvents);

            [DllImport(PinvokeDllNames.SetConsoleCtrlHandlerDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleCtrlHandler(BreakHandler handlerRoutine, bool add);

            [DllImport(PinvokeDllNames.SetConsoleModeDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleMode(NakedWin32Handle consoleHandle, DWORD mode);

            [DllImport(PinvokeDllNames.SetConsoleScreenBufferSizeDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleScreenBufferSize(NakedWin32Handle consoleOutput, COORD size);

            [DllImport(PinvokeDllNames.SetConsoleTextAttributeDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleTextAttribute(NakedWin32Handle consoleOutput, WORD attributes);

            [DllImport(PinvokeDllNames.SetConsoleWindowInfoDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleWindowInfo(NakedWin32Handle consoleHandle, bool absolute, ref SMALL_RECT windowInfo);

            [DllImport(PinvokeDllNames.WriteConsoleOutputDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool WriteConsoleOutput
            (
                NakedWin32Handle consoleOutput,
                CHAR_INFO[] buffer,
                COORD bufferSize,
                COORD bufferCoord,
                ref SMALL_RECT writeRegion
            );

            [DllImport(PinvokeDllNames.ReadConsoleOutputDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ReadConsoleOutput
            (
                NakedWin32Handle consoleOutput,
                [Out] CHAR_INFO[] buffer,
                COORD bufferSize,
                COORD bufferCoord,
                ref SMALL_RECT readRegion
            );

            [DllImport(PinvokeDllNames.ScrollConsoleScreenBufferDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ScrollConsoleScreenBuffer
            (
                NakedWin32Handle consoleOutput,
                ref SMALL_RECT scrollRectangle,
                ref SMALL_RECT clipRectangle,
                COORD destinationOrigin,
                ref CHAR_INFO fill
            );

            [DllImport(PinvokeDllNames.SendInputDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern UInt32 SendInput(UInt32 inputNumbers, INPUT[] inputs, int sizeOfInput);

            // There is no GetCurrentConsoleFontEx on Core
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetCurrentConsoleFontEx(NakedWin32Handle consoleOutput, bool bMaximumWindow, ref CONSOLE_FONT_INFO_EX consoleFontInfo);

            [DllImport(PinvokeDllNames.GetConsoleCursorInfoDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleCursorInfo(NakedWin32Handle consoleOutput, out CONSOLE_CURSOR_INFO consoleCursorInfo);

            [DllImport(PinvokeDllNames.SetConsoleCursorInfoDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleCursorInfo(NakedWin32Handle consoleOutput, ref CONSOLE_CURSOR_INFO consoleCursorInfo);

            [DllImport(PinvokeDllNames.ReadConsoleInputDllName, SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ReadConsoleInput
            (
                NakedWin32Handle consoleInput,
                [Out] INPUT_RECORD[] buffer,
                DWORD length,
                out DWORD numberOfEventsRead
            );

            internal enum CHAR_INFO_Attributes : uint
            {
                COMMON_LVB_LEADING_BYTE = 0x0100,
                COMMON_LVB_TRAILING_BYTE = 0x0200
            }
        }

        [TraceSourceAttribute("ConsoleControl", "Console control methods")]
        private static readonly PSTraceSource tracer = PSTraceSource.GetTracer("ConsoleControl", "Console control methods");
#endif
    }
}
