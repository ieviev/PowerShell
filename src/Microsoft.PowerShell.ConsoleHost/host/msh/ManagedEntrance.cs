// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.PowerShell
{
    
    public sealed class UnmanagedPSEntry
    {
        
        public static int Start([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] args, int argc)
        {
#if DEBUG
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]) && args[0]!.Equals("-isswait", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Attach the debugger to continue...");
                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }

                System.Diagnostics.Debugger.Break();
            }
#endif
            // Warm up some components concurrently on background threads.
            EarlyStartup.Init();

            // Windows Vista and later support non-traditional UI fallback ie., a
            // user on an Arabic machine can choose either French or English(US) as
            // UI fallback language.
            // CLR does not support this (non-traditional) fallback mechanism.
            // The currentUICulture returned NativeCultureResolver supports this non
            // traditional fallback on Vista. So it is important to set currentUICulture
            // in the beginning before we do anything.
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            ConsoleHost.ParseCommandLine(args);

            // NOTE: On Unix, logging depends on a command line parsing
            // and must be just after ConsoleHost.ParseCommandLine(args)
            // to allow overriding logging options.
            // 

            int exitCode = 0;
            try
            {
                // var banner = string.Format(
                //     CultureInfo.InvariantCulture,
                //     ManagedEntranceStrings.ShellBannerNonWindowsPowerShell,
                //     PSVersionInfo.GitCommitId);

                ConsoleHost.DefaultInitialSessionState = InitialSessionState.CreateDefault2();

                exitCode = ConsoleHost.Start(
                    bannerText: null,
                    helpText: ManagedEntranceStrings.UsageHelp,
                    issProvidedExternally: false);
            }
            catch (HostException e)
            {
                if (e.InnerException is Win32Exception win32e)
                {
                    // These exceptions are caused by killing conhost.exe
                    // 1236, network connection aborted by local system
                    // 0x6, invalid console handle
                    if (win32e.NativeErrorCode == 0x6 || win32e.NativeErrorCode == 1236)
                    {
                        return exitCode;
                    }
                }

                System.Environment.FailFast(e.Message, e);
            }
            catch (Exception e)
            {
                System.Environment.FailFast(e.Message, e);
            }

            return exitCode;
        }
    }
}
