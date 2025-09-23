// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if !UNIX
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        internal enum ActivityControl : uint
        {
            
            Get = 1,

            
            Set = 2,

            
            Create = 3,

            
            GetSet = 4,

            
            CreateSet = 5
        }

        [LibraryImport("api-ms-win-eventing-provider-l1-1-0.dll")]
        internal static unsafe partial int EventActivityIdControl(ActivityControl controlCode, Guid* activityId);

        internal static unsafe int GetEventActivityIdControl(ref Guid activityId)
        {
            fixed (Guid* guidPtr = &activityId)
            {
                return EventActivityIdControl(ActivityControl.Get, guidPtr);
            }
        }
    }
}
#endif
