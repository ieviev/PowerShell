// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class DateTimeApproximationComparer : IComparer<DateTime>
    {
        
        public int Compare(DateTime value1, DateTime value2)
        {
            DateTime roundedX;
            DateTime roundedY;
            GetRoundedValues(value1, value2, out roundedX, out roundedY);

            return roundedX.CompareTo(roundedY);
        }

        private static void GetRoundedValues(DateTime value1, DateTime value2, out DateTime roundedValue1, out DateTime roundedValue2)
        {
            roundedValue1 = value1;
            roundedValue2 = value2;

            bool hasTimeComponent = HasTimeComponent(value1);

            int hour = hasTimeComponent ? value1.Hour : value2.Hour;
            int minute = hasTimeComponent ? value1.Minute : value2.Minute;
            int second = hasTimeComponent ? value1.Second : value2.Second;

            roundedValue1 = new DateTime(value1.Year, value1.Month, value1.Day, hour, minute, second);
            roundedValue2 = new DateTime(value2.Year, value2.Month, value2.Day, value2.Hour, value2.Minute, value2.Second);
        }

        private static bool HasTimeComponent(DateTime value)
        {
            bool hasNoTimeComponent = value.Hour == 0
                && value.Minute == 0
                && value.Second == 0
                && value.Millisecond == 0;

            return !hasNoTimeComponent;
        }
    }
}
