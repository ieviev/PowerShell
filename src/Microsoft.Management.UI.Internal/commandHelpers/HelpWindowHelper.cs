// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Threading;
using System.Windows;

using Microsoft.Management.UI;
using Microsoft.PowerShell.Commands.ShowCommandInternal;

namespace Microsoft.PowerShell.Commands.Internal
{
    
    internal static class HelpWindowHelper
    {
        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called from methods called using reflection")]
        private static void ShowHelpWindow(PSObject helpObj, PSCmdlet cmdlet)
        {
            Window ownerWindow = ShowCommandHelper.GetHostWindow(cmdlet);
            if (ownerWindow != null)
            {
                ownerWindow.Dispatcher.Invoke(
                    new SendOrPostCallback(
                        (_) =>
                        {
                            HelpWindow helpWindow = new HelpWindow(helpObj);
                            helpWindow.Owner = ownerWindow;
                            helpWindow.Show();

                            helpWindow.Closed += new EventHandler((sender, e) => ownerWindow.Focus());
                        }),
                        string.Empty);
                return;
            }

            Thread guiThread = new Thread(
            (ThreadStart)delegate
            {
                HelpWindow helpWindow = new HelpWindow(helpObj);
                helpWindow.ShowDialog();
            });
            guiThread.SetApartmentState(ApartmentState.STA);
            guiThread.Start();
        }
    }
}
