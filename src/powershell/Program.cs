// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

namespace Microsoft.PowerShell
{
    public sealed class ManagedPSEntry
    {
        public static int Main(string[] args)
        {
            return UnmanagedPSEntry.Start(args, args.Length);
        }
    }
}
