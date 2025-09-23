// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX
using System;
using System.Collections;
using System.Reflection;
using System.Globalization;
using System.Management.Automation;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    
    public static partial class ToStringCodeMethods
    {
        
        public static string PropertyValueCollection(PSObject instance)
        {
            if (instance == null)
                return string.Empty;

            var values = (PropertyValueCollection)instance.BaseObject;
            if (values == null)
                return string.Empty;

            if (values.Count == 1)
            {
                if (values[0] == null)
                {
                    return string.Empty;
                }

                return (PSObject.AsPSObject(values[0]).ToString());
            }

            return PSObject.ToStringEnumerable(null, (IEnumerable)values, null, null, null);
        }
    }

    
    public static class AdapterCodeMethods
    {
        #region DirectoryEntry related CodeMethods

        
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "integer")]
        public static Int64 ConvertLargeIntegerToInt64(PSObject deInstance, PSObject largeIntegerInstance)
        {
            if (largeIntegerInstance == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(largeIntegerInstance));
            }

            object largeIntObject = (object)largeIntegerInstance.BaseObject;
            Type largeIntType = largeIntObject.GetType();

            // the following code might throw exceptions,
            // engine will catch these exceptions
            int highPart = (int)largeIntType.InvokeMember("HighPart",
                BindingFlags.GetProperty | BindingFlags.Public,
                null,
                largeIntObject,
                null,
                CultureInfo.InvariantCulture);
            int lowPart = (int)largeIntType.InvokeMember("LowPart",
                BindingFlags.GetProperty | BindingFlags.Public,
                null,
                largeIntObject,
                null,
                CultureInfo.InvariantCulture);

            // LowPart is not really a signed integer. Do not try to
            // use LowPart as a signed integer or you may get intermittent
            // surprises.

            // (long)highPart << 32 | (uint)lowPart
            byte[] data = new byte[8];
            BitConverter.GetBytes(lowPart).CopyTo(data, 0);
            BitConverter.GetBytes(highPart).CopyTo(data, 4);

            return BitConverter.ToInt64(data, 0);
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dn", Justification = "DN represents valid prefix w.r.t Active Directory.")]
        public static string ConvertDNWithBinaryToString(PSObject deInstance, PSObject dnWithBinaryInstance)
        {
            if (dnWithBinaryInstance == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(dnWithBinaryInstance));
            }

            object dnWithBinaryObject = (object)dnWithBinaryInstance.BaseObject;
            Type dnWithBinaryType = dnWithBinaryObject.GetType();

            // the following code might throw exceptions,
            // engine will catch these exceptions
            string dnString = (string)dnWithBinaryType.InvokeMember("DNString",
                BindingFlags.GetProperty | BindingFlags.Public,
                null,
                dnWithBinaryObject,
                null,
                CultureInfo.InvariantCulture);

            return dnString;
        }

        #endregion
    }
}
#endif
