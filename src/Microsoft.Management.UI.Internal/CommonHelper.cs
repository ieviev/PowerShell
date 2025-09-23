// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;

// Specifies the location in which theme dictionaries are stored for types in an assembly.
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

namespace Microsoft.Management.UI
{
    
    internal static class CommonHelper
    {
        
        internal static void SetStartingPositionAndSize(Window target, double userSettingTop, double userSettingLeft, double userSettingWidth, double userSettingHeight, double defaultWidth, double defaultHeight, bool userSettingMaximized)
        {
            bool leftInvalid = userSettingLeft < System.Windows.SystemParameters.VirtualScreenLeft ||
                            userSettingWidth > System.Windows.SystemParameters.VirtualScreenLeft +
                            System.Windows.SystemParameters.VirtualScreenWidth;

            bool topInvalid = userSettingTop < System.Windows.SystemParameters.VirtualScreenTop ||
                userSettingTop > System.Windows.SystemParameters.VirtualScreenTop +
                            System.Windows.SystemParameters.VirtualScreenHeight;

            bool widthInvalid = userSettingWidth < 0 ||
                userSettingWidth > System.Windows.SystemParameters.VirtualScreenWidth;

            bool heightInvalid = userSettingHeight < 0 ||
                userSettingHeight > System.Windows.SystemParameters.VirtualScreenHeight;

            if (leftInvalid || topInvalid)
            {
                target.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            }
            else
            {
                target.Left = userSettingLeft;
                target.Top = userSettingTop;
            }

            // If any saved coordinate is invalid, we set the window to the default position
            if (widthInvalid || heightInvalid)
            {
                target.Width = defaultWidth;
                target.Height = defaultHeight;
            }
            else
            {
                target.Width = userSettingWidth;
                target.Height = userSettingHeight;
            }

            if (userSettingMaximized)
            {
                target.WindowState = WindowState.Maximized;
            }
        }
    }
}
