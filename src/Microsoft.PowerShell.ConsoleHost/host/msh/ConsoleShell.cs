// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell
{
    
    public static class ConsoleShell
    {
        
        public static int Start(string? bannerText, string? helpText, string[] args)
        {
            return StartImpl(
                initialSessionState: InitialSessionState.CreateDefault2(),
                bannerText,
                helpText,
                args,
                issProvided: false);
        }

        
        public static int Start(InitialSessionState initialSessionState, string? bannerText, string? helpText, string[] args)
        {
            return StartImpl(
                initialSessionState,
                bannerText,
                helpText,
                args,
                issProvided: true);
        }

        
        private static int StartImpl(
            InitialSessionState initialSessionState,
            string? bannerText,
            string? helpText,
            string[] args,
            bool issProvided)
        {
            if (initialSessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(initialSessionState));
            }

            if (args == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(args));
            }

            ConsoleHost.ParseCommandLine(args);
            ConsoleHost.DefaultInitialSessionState = initialSessionState;

            return ConsoleHost.Start(bannerText, helpText, issProvided);
        }
    }
}
