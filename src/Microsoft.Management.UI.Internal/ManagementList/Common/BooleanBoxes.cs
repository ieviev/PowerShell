// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Management.UI.Internal
{
    
    internal static class BooleanBoxes
    {
        private static object trueBox = true;
        private static object falseBox = false;

        internal static object TrueBox
        {
            get
            {
                return trueBox;
            }
        }

        internal static object FalseBox
        {
            get
            {
                return falseBox;
            }
        }

        internal static object Box(bool value)
        {
            if (value)
            {
                return TrueBox;
            }
            else
            {
                return FalseBox;
            }
        }
    }
}
