// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell
{
    
    [StructLayout(LayoutKind.Explicit)]
    internal sealed class PropVariant : IDisposable
    {
        // This is actually a VarEnum value, but the VarEnum type requires 4 bytes instead of the expected 2.
        [FieldOffset(0)]
        private readonly ushort _valueType;

        [FieldOffset(8)]
        private readonly IntPtr _ptr;

        
        internal PropVariant(string value)
        {
            if (value == null)
            {
                throw new ArgumentException("PropVariantNullString", nameof(value));
            }

            _valueType = (ushort)VarEnum.VT_LPWSTR;
            _ptr = Marshal.StringToCoTaskMemUni(value);
        }

        
        public void Dispose()
        {
            PropVariantNativeMethods.PropVariantClear(this);

            GC.SuppressFinalize(this);
        }

        
        ~PropVariant()
        {
            Dispose();
        }

        private static class PropVariantNativeMethods
        {
            [DllImport("Ole32.dll", PreserveSig = false)]
            internal static extern void PropVariantClear([In, Out] PropVariant pvar);
        }
    }
}
