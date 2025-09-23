// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell
{
    
    public static class ConsoleShell
    {
        
        /// <param name="bannerText">Banner text to be displayed by ConsoleHost.</param>
        /// <param name="helpText">Help text for minishell. This is displayed on 'minishell -?'.</param>
        /// <param name="args">Commandline parameters specified by user.</param>
        /// <returns>An integer value which should be used as exit code for the process.</returns>
        public static int Start(string? bannerText, string? helpText, string[] args)
        {
            return StartImpl(
                initialSessionState: InitialSessionState.CreateDefault2(),
                bannerText,
                helpText,
                args,
                issProvided: false);
        }

        
        /// <param name="initialSessionState">InitialSessionState to be used by the ConsoleHost.</param>
        /// <param name="bannerText">Banner text to be displayed by ConsoleHost.</param>
        /// <param name="helpText">Help text for the shell.</param>
        /// <param name="args">Commandline parameters specified by user.</param>
        /// <returns>An integer value which should be used as exit code for the process.</returns>
        public static int Start(InitialSessionState initialSessionState, string? bannerText, string? helpText, string[] args)
        {
            return StartImpl(
                initialSessionState,
                bannerText,
                helpText,
                args,
                issProvided: true);
        }

        
        /// <param name="initialSessionState">InitialSessionState to be used by the ConsoleHost.</param>
        /// <param name="bannerText">Banner text to be displayed by ConsoleHost.</param>
        /// <param name="helpText">Help text for the shell.</param>
        /// <param name="args">Commandline parameters specified by user.</param>
        /// <param name="issProvided">True when the InitialSessionState object is provided by caller.</param>
        /// <returns>An integer value which should be used as exit code for the process.</returns>
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
