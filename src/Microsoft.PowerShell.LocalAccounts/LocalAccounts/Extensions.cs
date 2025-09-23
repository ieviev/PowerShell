// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text.RegularExpressions;

using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.LocalAccounts;

namespace System.Management.Automation.SecurityAccountsManager.Extensions
{
    
    internal static class CmdletExtensions
    {
        
        internal static SecurityIdentifier TrySid(this Cmdlet cmdlet,
                                                  string s,
                                                  bool allowSidConstants = false)
        {
            if (!allowSidConstants)
                if (!(s.Length > 2 && s.StartsWith("S-", StringComparison.Ordinal) && char.IsDigit(s[2])))
                    return null;

            SecurityIdentifier sid = null;

            try
            {
                sid = new SecurityIdentifier(s);
            }
            catch (ArgumentException)
            {
                // do nothing here, just fall through to the return
            }

            return sid;
        }
    }

    
    internal static class PSExtensions
    {
        
        internal static bool HasParameter(this PSCmdlet cmdlet, string parameterName)
        {
            var invocation = cmdlet.MyInvocation;
            if (invocation != null)
            {
                var parameters = invocation.BoundParameters;

                if (parameters != null)
                {
                    // PowerShell sets the parameter names in the BoundParameters dictionary
                    // to their "proper" casing, so we don't have to do a case-insensitive search.
                    if (parameters.ContainsKey(parameterName))
                        return true;
                }
            }

            return false;
        }
    }

    
    internal static class SidExtensions
    {
        
        internal static UInt32 GetRid(this SecurityIdentifier sid)
        {
            byte[] sidBinary = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBinary, 0);

            return System.BitConverter.ToUInt32(sidBinary, sidBinary.Length-4);
        }

        
        internal static long GetIdentifierAuthority(this SecurityIdentifier sid)
        {
            byte[] sidBinary = new byte[sid.BinaryLength];

            sid.GetBinaryForm(sidBinary, 0);

            // The Identifier Authority is six bytes wide,
            // in big-endian format, starting at the third byte
            long authority = (long) (((long)sidBinary[2]) << 40) +
                                    (((long)sidBinary[3]) << 32) +
                                    (((long)sidBinary[4]) << 24) +
                                    (((long)sidBinary[5]) << 16) +
                                    (((long)sidBinary[6]) <<  8) +
                                    (((long)sidBinary[7])      );

            return authority;
        }

        internal static bool IsMsaAccount(this SecurityIdentifier sid)
        {
            return sid.GetIdentifierAuthority() == 11;
        }
    }

    internal static class SecureStringExtensions
    {
        
        internal static string AsString(this SecureString str)
        {
#if CORECLR
            IntPtr buffer = SecureStringMarshal.SecureStringToCoTaskMemUnicode(str);
            string clear = Marshal.PtrToStringUni(buffer);
            Marshal.ZeroFreeCoTaskMemUnicode(buffer);
#else
            var bstr = Marshal.SecureStringToBSTR(str);
            string clear = Marshal.PtrToStringAuto(bstr);
            Marshal.ZeroFreeBSTR(bstr);
#endif
            return clear;
        }
    }

    internal static class ExceptionExtensions
    {
        internal static ErrorRecord MakeErrorRecord(this Exception ex,
                                                    string errorId,
                                                    ErrorCategory errorCategory,
                                                    object target = null)
        {
            return new ErrorRecord(ex, errorId, errorCategory, target);
        }

        internal static ErrorRecord MakeErrorRecord(this Exception ex, object target = null)
        {
            // This part is somewhat less than beautiful, but it prevents
            // having to have multiple exception handlers in every cmdlet command.
            var exTemp = ex as LocalAccountsException;

            if (exTemp != null)
                return MakeErrorRecord(exTemp, target ?? exTemp.Target);

            return new ErrorRecord(ex,
                                   Strings.UnspecifiedError,
                                   ErrorCategory.NotSpecified,
                                   target);
        }

        internal static ErrorRecord MakeErrorRecord(this LocalAccountsException ex, object target = null)
        {
            return ex.MakeErrorRecord(ex.ErrorName, ex.ErrorCategory, target ?? ex.Target);
        }
    }
}
