// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation.Host
{
    #region Ancillary types.

    // I would have preferred to make these nested types within PSHostRawUserInterface, but that
    // is evidently discouraged by the .net design guidelines.

    
    public
    struct Coordinates
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private int x;
        private int y;

        #endregion

        
        public int X
        {
            get { return x; }

            set { x = value; }
        }

        
        public int Y
        {
            get { return y; }

            set { y = value; }
        }

        
        /// <param name="x">
        /// The X coordinate
        /// </param>
        /// <param name="y">
        /// The Y coordinate
        /// </param>
        public
        Coordinates(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        
        /// <returns>
        /// "a,b" where a and b are the values of the X and Y properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"{X},{Y}");
        }

        
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="objB"/> is Coordinates and its X and Y values are the same as those of this instance,
        /// false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is Coordinates)
            {
                result = this == ((Coordinates)obj);
            }

            return result;
        }

        
        /// <returns>
        /// Hash code for this instance.
        /// </returns>
        public override
        int
        GetHashCode()
        {
            // idea: consider X the high-order part of a 64-bit in, and Y the lower order half.  Then use the int64.GetHashCode.

            UInt64 i64 = 0;

            if (X < 0)
            {
                if (X == Int32.MinValue)
                {
                    // add one and invert to avoid an overflow.

                    i64 = (UInt64)(-1 * (X + 1));
                }
                else
                {
                    i64 = (UInt64)(-X);
                }
            }
            else
            {
                i64 = (UInt64)X;
            }

            // rotate 32 bits to the left.

            i64 *= 0x100000000U;

            // mask in Y

            if (Y < 0)
            {
                if (Y == Int32.MinValue)
                {
                    i64 += (UInt64)(-1 * (Y + 1));
                }
                else
                {
                    i64 += (UInt64)(-Y);
                }
            }
            else
            {
                i64 += (UInt64)Y;
            }

            int result = i64.GetHashCode();

            return result;
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective X and Y values are the same, false otherwise.
        /// </returns>
        public static
        bool
        operator ==(Coordinates first, Coordinates second)
        {
            bool result = first.X == second.X && first.Y == second.Y;

            return result;
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective either X or Y field is not the same, false otherwise.
        /// </returns>
        public static
        bool
        operator !=(Coordinates first, Coordinates second)
        {
            return !(first == second);
        }
    }

    
    public
    struct Size
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private int width;
        private int height;

        #endregion

        
        public int Width
        {
            get { return width; }

            set { width = value; }
        }

        
        public int Height
        {
            get { return height; }

            set { height = value; }
        }

        
        /// <param name="width">
        /// The Width
        /// </param>
        /// <param name="height">
        /// The Height
        /// </param>
        public
        Size(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        
        /// <returns>
        /// "a,b" where a and b are the values of the Width and Height properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"{Width},{Height}");
        }

        
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj"/> is Size and its Width and Height values are the same as those of this instance,
        /// false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is Size)
            {
                result = this == ((Size)obj);
            }

            return result;
        }

        
        /// <returns>
        /// Hash code for this instance.
        /// 
        /// </returns>
        public override
        int
        GetHashCode()
        {
            // idea: consider Width the high-order part of a 64-bit in, and Height the lower order half.  Then use the int64.GetHashCode.

            UInt64 i64 = 0;

            if (Width < 0)
            {
                if (Width == Int32.MinValue)
                {
                    // add one and invert to avoid an overflow.

                    i64 = (UInt64)(-1 * (Width + 1));
                }
                else
                {
                    i64 = (UInt64)(-Width);
                }
            }
            else
            {
                i64 = (UInt64)Width;
            }

            // rotate 32 bits to the left.

            i64 *= 0x100000000U;

            // mask in Height

            if (Height < 0)
            {
                if (Height == Int32.MinValue)
                {
                    i64 += (UInt64)(-1 * (Height + 1));
                }
                else
                {
                    i64 += (UInt64)(-Height);
                }
            }
            else
            {
                i64 += (UInt64)Height;
            }

            int result = i64.GetHashCode();

            return result;
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective Width and Height fields are the same, false otherwise.
        /// </returns>
        public static
        bool
        operator ==(Size first, Size second)
        {
            bool result = first.Width == second.Width && first.Height == second.Height;

            return result;
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective Width and Height fields are not the same, false otherwise.
        /// </returns>
        public static
        bool
        operator !=(Size first, Size second)
        {
            return !(first == second);
        }
    }

    
    [Flags]
    public
    enum
    ReadKeyOptions
    {
        
        AllowCtrlC = 0x0001,

        
        NoEcho = 0x0002,

        
        IncludeKeyDown = 0x0004,

        
        IncludeKeyUp = 0x0008
    }

    
    [Flags]
    public
    enum ControlKeyStates
    {
        
        RightAltPressed = 0x0001,

        
        LeftAltPressed = 0x0002,

        
        RightCtrlPressed = 0x0004,

        
        LeftCtrlPressed = 0x0008,

        
        ShiftPressed = 0x0010,

        
        NumLockOn = 0x0020,

        
        ScrollLockOn = 0x0040,

        
        CapsLockOn = 0x0080,

        
        EnhancedKey = 0x0100
    }

    
    public
    struct KeyInfo
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private int virtualKeyCode;
        private char character;
        private ControlKeyStates controlKeyState;
        private bool keyDown;

        #endregion

        
        public int VirtualKeyCode
        {
            get { return virtualKeyCode; }

            set { virtualKeyCode = value; }
        }

        
        public char Character
        {
            get { return character; }

            set { character = value; }
        }

        
        public ControlKeyStates ControlKeyState
        {
            get { return controlKeyState; }

            set { controlKeyState = value; }
        }

        
        public bool KeyDown
        {
            get { return keyDown; }

            set { keyDown = value; }
        }

        
        /// <param name="virtualKeyCode">
        /// The virtual key code
        /// </param>
        /// <param name="ch">
        /// The character
        /// </param>
        /// <param name="controlKeyState">
        /// The control key state
        /// </param>
        /// <param name="keyDown">
        /// Whether the key is pressed or released
        /// </param>
        public
        KeyInfo
        (
            int virtualKeyCode,
            char ch,
            ControlKeyStates controlKeyState,
            bool keyDown
        )
        {
            this.virtualKeyCode = virtualKeyCode;
            this.character = ch;
            this.controlKeyState = controlKeyState;
            this.keyDown = keyDown;
        }

        
        /// <returns>
        /// "a,b,c,d" where a, b, c, and d are the values of the VirtualKeyCode, Character, ControlKeyState, and KeyDown properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"{VirtualKeyCode},{Character},{ControlKeyState},{KeyDown}");
        }
        
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj"/> is KeyInfo and its VirtualKeyCode, Character, ControlKeyState, and KeyDown values are the
        /// same as those of this instance, false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is KeyInfo)
            {
                result = this == ((KeyInfo)obj);
            }

            return result;
        }

        
        /// <returns>
        /// Hash code for this instance.
        /// 
        /// </returns>
        public override
        int
        GetHashCode()
        {
            // idea: consider KeyDown (true == 1, false == 0) the highest-order nibble,
            //                ControlKeyState the second to fourth highest-order nibbles
            //                VirtualKeyCode the lower-order nibbles of a 32-bit int,
            //       Then use the UInt32.GetHashCode.

            UInt32 i32 = KeyDown ? 0x10000000U : 0;

            // mask in ControlKeyState
            i32 |= ((uint)ControlKeyState) << 16;

            // mask in the VirtualKeyCode
            i32 |= (UInt32)VirtualKeyCode;

            return i32.GetHashCode();
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective Character, ControlKeyStates , KeyDown, and VirtualKeyCode fields
        /// are the same, false otherwise.
        /// </returns>
        /// <exception/>
        public static
        bool
        operator ==(KeyInfo first, KeyInfo second)
        {
            bool result = first.Character == second.Character && first.ControlKeyState == second.ControlKeyState &&
                          first.KeyDown == second.KeyDown && first.VirtualKeyCode == second.VirtualKeyCode;

            return result;
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective Character, ControlKeyStates , KeyDown, or VirtualKeyCode fields
        /// are the different, false otherwise.
        /// </returns>
        /// <exception/>
        public static
        bool
        operator !=(KeyInfo first, KeyInfo second)
        {
            return !(first == second);
        }
    }

    
    public
    struct Rectangle
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private int left;
        private int top;
        private int right;
        private int bottom;

        #endregion

        
        public int Left
        {
            get { return left; }

            set { left = value; }
        }

        
        public int Top
        {
            get { return top; }

            set { top = value; }
        }

        
        public int Right
        {
            get { return right; }

            set { right = value; }
        }

        
        public int Bottom
        {
            get { return bottom; }

            set { bottom = value; }
        }

        
        /// <param name="left">
        /// The left side of the rectangle
        /// </param>
        /// <param name="top">
        /// The top of the rectangle
        /// </param>
        /// <param name="right">
        /// The right side of the rectangle
        /// </param>
        /// <param name="bottom">
        /// The bottom of the rectangle
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="right"/> is less than <paramref name="left"/>;
        /// <paramref name="bottom"/> is less than <paramref name="top"/>
        /// </exception>
        public
        Rectangle(int left, int top, int right, int bottom)
        {
            if (right < left)
            {
                // "right" and "left" are not localizable
                throw PSTraceSource.NewArgumentException(nameof(right), MshHostRawUserInterfaceStrings.LessThanErrorTemplate, "right", "left");
            }

            if (bottom < top)
            {
                // "bottom" and "top" are not localizable
                throw PSTraceSource.NewArgumentException(nameof(bottom), MshHostRawUserInterfaceStrings.LessThanErrorTemplate, "bottom", "top");
            }

            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        
        /// <param name="upperLeft">
        /// The Coordinates of the upper left corner of the Rectangle
        /// </param>
        /// <param name="lowerRight">
        /// The Coordinates of the lower right corner of the Rectangle
        /// </param>
        /// <exception/>
        public
        Rectangle(Coordinates upperLeft, Coordinates lowerRight)
            : this(upperLeft.X, upperLeft.Y, lowerRight.X, lowerRight.Y)
        {
        }

        
        /// <returns>
        /// "a,b ; c,d" where a, b, c, and d are values of the Left, Top, Right, and Bottom properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"{Left},{Top} ; {Right},{Bottom}");
        }

        
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj"/> is Rectangle and its Left, Top, Right, and Bottom values are the same as those of this instance,
        /// false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is Rectangle)
            {
                result = this == ((Rectangle)obj);
            }

            return result;
        }

        
        /// <returns>
        /// Hash code for this instance.
        /// 
        /// </returns>
        /// <exception/>
        public override
        int
        GetHashCode()
        {
            // idea: consider (Top XOR Bottom) the high-order part of a 64-bit int,
            //                (Left XOR Right) the lower order half.  Then use the int64.GetHashCode.

            UInt64 i64 = 0;

            int upper = Top ^ Bottom;
            if (upper < 0)
            {
                if (upper == Int32.MinValue)
                {
                    // add one and invert to avoid an overflow.

                    i64 = (UInt64)(-1 * (upper + 1));
                }
                else
                {
                    i64 = (UInt64)(-upper);
                }
            }
            else
            {
                i64 = (UInt64)upper;
            }

            // rotate 32 bits to the left.

            i64 *= 0x100000000U;

            // mask in lower

            int lower = Left ^ Right;
            if (lower < 0)
            {
                if (lower == Int32.MinValue)
                {
                    i64 += (UInt64)(-1 * (lower + 1));
                }
                else
                {
                    i64 += (UInt64)(-upper);
                }
            }
            else
            {
                i64 += (UInt64)lower;
            }

            int result = i64.GetHashCode();

            return result;
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective Top, Left, Bottom, and Right fields are the same, false otherwise.
        /// </returns>
        public static
        bool
        operator ==(Rectangle first, Rectangle second)
        {
            bool result = first.Top == second.Top && first.Left == second.Left &&
             first.Bottom == second.Bottom && first.Right == second.Right;

            return result;
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective Top, Left, Bottom, and Right fields are not the same, false otherwise.
        /// </returns>
        /// <exception/>
        public static
        bool
        operator !=(Rectangle first, Rectangle second)
        {
            return !(first == second);
        }
    }

    
    public
    struct BufferCell
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private char character;
        private ConsoleColor foregroundColor;
        private ConsoleColor backgroundColor;
        private BufferCellType bufferCellType;

        #endregion

        
        public char Character
        {
            get { return character; }

            set { character = value; }
        }

        // we reuse System.ConsoleColor - it's in the core assembly, and I think it would be confusing to create another
        // essentially identical enum

        
        public ConsoleColor ForegroundColor
        {
            get { return foregroundColor; }

            set { foregroundColor = value; }
        }

        
        public ConsoleColor BackgroundColor
        {
            get { return backgroundColor; }

            set { backgroundColor = value; }
        }

        
        public BufferCellType BufferCellType
        {
            get { return bufferCellType; }

            set { bufferCellType = value; }
        }

        
        /// <param name="character">
        /// The character in this BufferCell object
        /// </param>
        /// <param name="foreground">
        /// The foreground color of this BufferCell object
        /// </param>
        /// <param name="background">
        /// The foreground color of this BufferCell object
        /// </param>
        /// <param name="bufferCellType">
        /// The type of this BufferCell object
        /// </param>
        public
        BufferCell(char character, ConsoleColor foreground, ConsoleColor background, BufferCellType bufferCellType)
        {
            this.character = character;
            this.foregroundColor = foreground;
            this.backgroundColor = background;
            this.bufferCellType = bufferCellType;
        }

        
        /// <returns>
        /// "'a' b c d" where a, b, c, and d are the values of the Character, ForegroundColor, BackgroundColor, and Type properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"'{Character}' {ForegroundColor} {BackgroundColor} {BufferCellType}");
        }

        
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj"/> is BufferCell and its Character, ForegroundColor, BackgroundColor, and BufferCellType values
        /// are the same as those of this instance, false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is BufferCell)
            {
                result = this == ((BufferCell)obj);
            }

            return result;
        }

        
        /// <returns>
        /// Hash code for this instance.
        ///
        /// </returns>
        public override
        int
        GetHashCode()
        {
            // idea: consider (ForegroundColor XOR BackgroundColor) the high-order part of a 32-bit int,
            //                and Character the lower order half.  Then use the int32.GetHashCode.

            UInt32 i32 = ((uint)(ForegroundColor ^ BackgroundColor)) << 16;

            // mask in Height

            i32 |= (UInt16)Character;
            int result = i32.GetHashCode();

            return result;
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective Character, ForegroundColor, BackgroundColor, and BufferCellType values are the same, false otherwise.
        /// </returns>
        public static
        bool
        operator ==(BufferCell first, BufferCell second)
        {
            bool result = first.Character == second.Character &&
                          first.BackgroundColor == second.BackgroundColor &&
                          first.ForegroundColor == second.ForegroundColor &&
                          first.BufferCellType == second.BufferCellType;

            return result;
        }

        
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective Character, ForegroundColor, BackgroundColor, and BufferCellType values are not the same,
        /// false otherwise.
        /// </returns>
        public static
        bool
        operator !=(BufferCell first, BufferCell second)
        {
            return !(first == second);
        }

        private const string StringsBaseName = "MshHostRawUserInterfaceStrings";
    }

    
    public enum
    BufferCellType
    {
        
        Complete,

        
        Leading,

        
        Trailing
    }

    #endregion Ancillary types

    
    /// <remarks>
    /// It models an 2-dimensional grid of cells called a Buffer.  A buffer has a visible rectangular region, called a window.
    /// Each cell of the grid has a character, a foreground color, and a background color.  When the buffer has input focus, it
    /// shows a cursor positioned in one cell.  Keystrokes can be read from the buffer and optionally echoed at the current
    /// cursor position.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Host.PSHost"/>
    /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface"/>
    public abstract
    class PSHostRawUserInterface
    {
        
        protected
        PSHostRawUserInterface()
        {
            // do nothing
        }

        
        /// 
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.BackgroundColor"/>
        public abstract
        ConsoleColor
        ForegroundColor
        {
            get;
            set;
        }

        
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ForegroundColor"/>
        public abstract
        ConsoleColor
        BackgroundColor
        {
            get;
            set;
        }

        
        /// <remarks>
        /// To write to the screen buffer without updating the cursor position, use
        /// <see cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/> or
        /// <see cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        public abstract
        Coordinates
        CursorPosition
        {
            get;
            set;
        }

        
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        public abstract
        Coordinates
        WindowPosition
        {
            get;
            set;
        }

        
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        public abstract
        int
        CursorSize
        {
            get;
            set;
        }

        
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public abstract
        Size
        BufferSize
        {
            get;
            set;
        }

        
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.BufferSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public abstract
        Size
        WindowSize
        {
            get;
            set;
        }

        
        /// <value>
        /// The largest dimensions the window can be resized to without resizing the screen buffer.
        /// </value>
        /// <remarks>
        /// Always returns a value less than or equal to
        /// <see cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.BufferSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public abstract
        Size
        MaxWindowSize
        {
            get;
        }

        
        /// <remarks>
        /// To resize the window to this dimension, use <see cref="System.Management.Automation.Host.PSHostRawUserInterface.BufferSize"/>
        /// to first check and, if necessary, adjust, the screen buffer size.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.BufferSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public abstract
        Size
        MaxPhysicalWindowSize
        {
            get;
        }

        
        /// <returns>
        /// Key stroke when a key is pressed.
        /// </returns>
        /// <example>
        ///     <code>
        ///         $Host.UI.RawUI.ReadKey()
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey(ReadKeyOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.FlushInputBuffer"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.KeyAvailable"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public
        KeyInfo
        ReadKey()
        {
            return ReadKey(ReadKeyOptions.IncludeKeyDown);
        }

        
        /// <param name="options">
        /// A bit mask of the options to be used to read the keyboard. Constants defined by
        /// <see cref="System.Management.Automation.Host.ReadKeyOptions"/>
        /// </param>
        /// <returns>
        /// Key stroke depending on the value of <paramref name="options"/>.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Neither ReadKeyOptions.IncludeKeyDown nor ReadKeyOptions.IncludeKeyUp is specified.
        /// </exception>
        /// <example>
        ///     <code>
        ///         $option = [System.Management.Automation.Host.ReadKeyOptions]"IncludeKeyDown";
        ///         $host.UI.RawUI.ReadKey($option)
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Host.ReadKeyOptions"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey(System.Management.Automation.Host.ReadKeyOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.FlushInputBuffer"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.KeyAvailable"/>
        public abstract
        KeyInfo
        ReadKey(ReadKeyOptions options);

        
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey(System.Management.Automation.Host.ReadKeyOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.KeyAvailable"/>
        public abstract
        void
        FlushInputBuffer();

        
        /// <value>
        /// True if a keystroke is waiting in the input buffer, false if not.
        /// </value>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey(ReadKeyOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.FlushInputBuffer"/>
        public abstract
        bool
        KeyAvailable
        {
            get;
        }

        
        public abstract
        string
        WindowTitle
        {
            get;
            set;
        }

        
        /// <param name="origin">
        /// The top left corner of the rectangular screen area to which <paramref name="contents"/> is copied.
        /// </param>
        /// <param name="contents">
        /// A rectangle of <see cref="System.Management.Automation.Host.BufferCell"/> objects to be copied to the
        /// screen buffer.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public abstract
        void
        SetBufferContents(Coordinates origin, BufferCell[,] contents);

        
        /// <param name="rectangle">
        /// The rectangle on the screen buffer to which <paramref name="fill"/> is copied.
        /// If all elements are -1, the entire screen buffer will be copied with <paramref name="fill"/>.
        /// </param>
        /// <param name="fill">
        /// The character and attributes used to fill <paramref name="rectangle"/>.
        /// </param>
        /// <remarks>
        /// Provided for clearing regions -- less chatty than passing an array of cells.
        /// </remarks>
        /// <example>
        ///     <code>
        ///         using System;
        ///         using System.Management.Automation;
        ///         using System.Management.Automation.Host;
        ///         namespace Microsoft.Samples.Cmdlet
        ///         {
        ///             [Cmdlet("Clear","Screen")]
        ///             public class ClearScreen : PSCmdlet
        ///             {
        ///                 protected override void BeginProcessing()
        ///                 {
        ///                     Host.UI.RawUI.SetBufferContents(new Rectangle(-1, -1, -1, -1),
        ///                         new BufferCell(' ', Host.UI.RawUI.ForegroundColor, Host.UI.RawUI.BackgroundColor))
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public abstract
        void
        SetBufferContents(Rectangle rectangle, BufferCell fill);

        
        /// <param name="rectangle">
        /// The rectangle on the screen buffer to extract.
        /// </param>
        /// <returns>
        /// An array of <see cref="System.Management.Automation.Host.BufferCell"/> objects extracted from
        /// the rectangular region of the screen buffer specified by <paramref name="rectangle"/>
        /// </returns>
        /// <remarks>
        /// If the rectangle is completely outside of the screen buffer, a BufferCell array of zero rows and column will be
        /// returned.
        ///
        /// If the rectangle is partially outside of the screen buffer, the area where the screen buffer and rectangle overlap
        /// will be read and returned. The size of the returned array is the same as that of r. Each BufferCell in the
        /// non-overlapping area of this array is set as follows:
        ///
        /// Character is the space (' ')
        /// ForegroundColor to the current foreground color, given by the ForegroundColor property of this class.
        /// BackgroundColor to the current background color, given by the BackgroundColor property of this class.
        ///
        /// The resulting array is organized in row-major order for performance reasons.  The screen buffer, however, is
        /// organized in column-major order -- e.g. you specify the column index first, then the row index second, as in (x, y).
        /// This means that a cell at screen buffer position (x, y) is in the array element [y, x].
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public abstract
        BufferCell[,]
        GetBufferContents(Rectangle rectangle);

        
        /// <param name="source">
        /// Indicates the region of the screen to be scrolled.
        /// </param>
        /// <param name="destination">
        /// Indicates the upper left coordinates of the region of the screen to receive the source region contents.  The target
        /// region is the same size as the source region.
        /// </param>
        /// <param name="clip">
        /// Indicates the region of the screen to include in the operation.  If a cell would be changed by the operation but
        /// does not fall within the clip region, it will be unchanged.
        /// </param>
        /// <param name="fill">
        /// The character and attributes to be used to fill any cells within the intersection of the source rectangle and
        /// clipping rectangle that are left "empty" by the move.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        public abstract
        void
        ScrollBufferContents
        (
            Rectangle source,
            Coordinates destination,
            Rectangle clip,
            BufferCell fill
        );

        
        /// <param name="source">
        /// The string whose substring length we want to know.
        /// </param>
        /// <param name="offset">
        /// Offset where the substring begins in <paramref name="source"/>
        /// </param>
        /// <returns>
        /// The default implementation calls <see cref="PSHostRawUserInterface.LengthInBufferCells(string)"/> method
        /// with the substring extracted from the <paramref name="source"/> string
        /// starting at the offset <paramref name="offset"/>
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public virtual
        int
        LengthInBufferCells
        (
            string source,
            int offset
        )
        {
            if (source == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(source));
            }

            // this implementation is inefficient
            // it is here to help with backcompatibility
            // it preserves the old behavior from the times
            // when there was only Length(string) overload
            string substring = offset == 0 ? source : source.Substring(offset);
            return this.LengthInBufferCells(substring);
        }

        
        /// <param name="source">
        /// The string whose length we want to know.
        /// </param>
        /// <returns>
        /// The default implementation returns the length of <paramref name="source"/>
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string, int)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public virtual
        int
        LengthInBufferCells
        (
            string source
        )
        {
            if (source == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(source));
            }

            return source.Length;
        }

        
        /// <param name="source">
        /// The character whose length we want to know.
        /// </param>
        /// <returns>
        /// The default implementation returns 1.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public virtual
        int
        LengthInBufferCells
        (
            char source
        )
        {
            return 1;
        }

        
        /// <param name="contents">
        /// String array based on which the two dimensional array of BufferCells will be created.
        /// </param>
        /// <param name="foregroundColor">
        /// Foreground color of the buffer cells in the resulting array.
        /// </param>
        /// <param name="backgroundColor">
        /// Background color of the buffer cells in the resulting array.
        /// </param>
        /// <returns>
        /// A two dimensional array of BufferCells whose characters are the same as those in <paramref name="contents"/>
        /// and whose foreground and background colors set to <paramref name="foregroundColor"/> and
        /// <paramref name="backgroundColor"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="contents"/> is null;
        /// Any string in <paramref name="contents"/> is null or empty
        /// </exception>
        /// <remarks>
        /// If a character C takes one BufferCell to display as determined by LengthInBufferCells,
        /// one BufferCell is allocated with its Character set to C and BufferCellType to BufferCell.Complete.
        /// On the other hand, if C takes two BufferCell, two adjacent BufferCells on a row in
        /// the returned array will be allocated: the first has Character set to C and BufferCellType to
        /// <see cref="System.Management.Automation.Host.BufferCellType.Leading"/> and the second
        /// Character set to (char)0 and Type to
        /// <see cref="System.Management.Automation.Host.BufferCellType.Trailing"/>. Hence, the returned
        /// BufferCell array has <paramref name="contents"/>.Length number of rows and number of columns
        /// equal to the largest number of cells a string in <paramref name="contents"/> takes. The
        /// foreground and background colors of the cells are initialized to
        /// <paramref name="foregroundColor"/> and <paramref name="backgroundColor"/>, respectively.
        /// The resulting array is suitable for use with <see cref="PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// and <see cref="PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
#pragma warning disable 56506
        public
        BufferCell[,]
        NewBufferCellArray(string[] contents, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
#pragma warning disable 56506

            if (contents == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(contents));
            }

            byte[][] charLengths = new byte[contents.Length][];
            int maxStringLengthInBufferCells = 0;
            for (int i = 0; i < contents.Length; i++)
            {
                if (string.IsNullOrEmpty(contents[i]))
                {
                    continue;
                }

                int lengthInBufferCells = 0;
                charLengths[i] = new byte[contents[i].Length];
                for (int j = 0; j < contents[i].Length; j++)
                {
                    charLengths[i][j] = (byte)LengthInBufferCells(contents[i][j]);
                    lengthInBufferCells += charLengths[i][j];
                }

                if (maxStringLengthInBufferCells < lengthInBufferCells)
                {
                    maxStringLengthInBufferCells = lengthInBufferCells;
                }
            }

            if (maxStringLengthInBufferCells <= 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(contents), MshHostRawUserInterfaceStrings.AllNullOrEmptyStringsErrorTemplate);
            }

            BufferCell[,] results = new BufferCell[contents.Length, maxStringLengthInBufferCells];
            for (int i = 0; i < contents.Length; i++)
            {
                int resultJ = 0;
                for (int j = 0; j < contents[i].Length; j++, resultJ++)
                {
                    if (charLengths[i][j] == 1)
                    {
                        results[i, resultJ] =
                            new BufferCell(contents[i][j], foregroundColor, backgroundColor, BufferCellType.Complete);
                    }
                    else if (charLengths[i][j] == 2)
                    {
                        results[i, resultJ] =
                            new BufferCell(contents[i][j], foregroundColor, backgroundColor, BufferCellType.Leading);
                        resultJ++;
                        results[i, resultJ] =
                            new BufferCell((char)0, foregroundColor, backgroundColor, BufferCellType.Trailing);
                    }
                }
                while (resultJ < maxStringLengthInBufferCells)
                {
                    results[i, resultJ] = new BufferCell(' ', foregroundColor, backgroundColor, BufferCellType.Complete);
                    resultJ++;
                }
            }

            return results;
#pragma warning restore 56506
        }
#pragma warning restore 56506

        
        /// <param name="width">
        /// The number of columns of the resulting array
        /// </param>
        /// <param name="height">
        /// The number of rows of the resulting array
        /// </param>
        /// <param name="contents">
        /// The cell to be copied to each of the elements of the resulting array.
        /// </param>
        /// <returns>
        /// A <paramref name="width"/> by <paramref name="height"/> array of BufferCells where each cell's value is
        /// based on <paramref name="contents"/>
        /// <paramref name="backgroundColor"/>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="width"/> is less than 1;
        /// <paramref name="height"/> is less than 1.
        /// </exception>
        /// <remarks>
        /// If the character takes one BufferCell to display as determined by LengthInBufferCells,
        /// one BufferCell is allocated with its Character set to the character and BufferCellType to
        /// BufferCell.Complete.
        /// On the other hand, if it takes two BufferCells, two adjacent BufferCells on a row
        /// in the returned array will be allocated: the first has Character
        /// set to the character and BufferCellType to BufferCellType.Leading and the second Character
        /// set to (char)0 and BufferCellType to BufferCellType.Trailing. Moreover, if <paramref name="width"/>
        /// is odd, the last column will just contain the leading cell.
        /// <paramref name="prototype"/>.BufferCellType is not used in creating the array.
        /// The resulting array is suitable for use with the PSHostRawUserInterface.SetBufferContents method.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public
        BufferCell[,]
        NewBufferCellArray(int width, int height, BufferCell contents)
        {
            if (width <= 0)
            {
                // "width" is not localizable
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(width), width,
                    MshHostRawUserInterfaceStrings.NonPositiveNumberErrorTemplate, "width");
            }

            if (height <= 0)
            {
                // "height" is not localizable
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(height), height,
                    MshHostRawUserInterfaceStrings.NonPositiveNumberErrorTemplate, "height");
            }

            BufferCell[,] buffer = new BufferCell[height, width];
            int charLength = LengthInBufferCells(contents.Character);
            if (charLength == 1)
            {
                for (int r = 0; r < buffer.GetLength(0); ++r)
                {
                    for (int c = 0; c < buffer.GetLength(1); ++c)
                    {
                        buffer[r, c] = contents;
                        buffer[r, c].BufferCellType = BufferCellType.Complete;
                    }
                }
            }
            else if (charLength == 2)
            {
                int normalizedWidth = width % 2 == 0 ? width : width - 1;
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < normalizedWidth; j++)
                    {
                        buffer[i, j] = contents;
                        buffer[i, j].BufferCellType = BufferCellType.Leading;
                        j++;
                        buffer[i, j] = new BufferCell((char)0,
                            contents.ForegroundColor, contents.BackgroundColor,
                            BufferCellType.Trailing);
                    }

                    if (normalizedWidth < width)
                    {
                        buffer[i, normalizedWidth] = contents;
                        buffer[i, normalizedWidth].BufferCellType = BufferCellType.Leading;
                    }
                }
            }

            return buffer;
        }

        
        /// <param name="size">
        /// The width and height of the resulting array.
        /// </param>
        /// <param name="contents">
        /// The cell to be copied to each of the elements of the resulting array.
        /// </param>
        /// <returns>
        /// An array of BufferCells whose size is <paramref name="size"/> and where each cell's value is
        /// based on <paramref name="contents"/>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="size"/>.Width or <paramref name="size"/>.Height is less than 1.
        /// </exception>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public
        BufferCell[,]
        NewBufferCellArray(Size size, BufferCell contents)
        {
            return NewBufferCellArray(size.Width, size.Height, contents);
        }
    }
}
