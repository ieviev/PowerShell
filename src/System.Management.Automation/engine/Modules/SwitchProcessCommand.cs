// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;

using Dbg = System.Management.Automation.Diagnostics;

#if UNIX

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Switch, "Process", HelpUri = "https://go.microsoft.com/fwlink/?linkid=2181448")]
    public sealed class SwitchProcessCommand : PSCmdlet
    {
        
        [Parameter(Position = 0, Mandatory = false, ValueFromRemainingArguments = true)]
        public string[] WithCommand { get; set; } = Array.Empty<string>();

        
        protected override void EndProcessing()
        {
            if (WithCommand.Length == 0)
            {
                return;
            }

            // execv requires command to be full path so resolve command to first match
            var command = this.SessionState.InvokeCommand.GetCommand(WithCommand[0], CommandTypes.Application);
            if (command is null)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new CommandNotFoundException(
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                CommandBaseStrings.NativeCommandNotFound,
                                WithCommand[0]
                            )
                        ),
                        "CommandNotFound",
                        ErrorCategory.InvalidArgument,
                        WithCommand[0]
                    )
                );
            }

            var execArgs = new string?[WithCommand.Length + 1];

            // execv convention is the first arg is the program name
            execArgs[0] = command.Name;

            for (int i = 1; i < WithCommand.Length; i++)
            {
                execArgs[i] = WithCommand[i];
            }

            // need null terminator at end
            execArgs[execArgs.Length - 1] = null;

            var env = Environment.GetEnvironmentVariables();
            var envBlock = new string?[env.Count + 1];
            int j = 0;
            foreach (DictionaryEntry entry in env)
            {
                envBlock[j++] = entry.Key + "=" + entry.Value;
            }

            envBlock[envBlock.Length - 1] = null;

            // setup termios for a child process as .NET modifies termios dynamically for use with ReadKey()
            ConfigureTerminalForChildProcess(true);
            int exitCode = Exec(command.Source, execArgs, envBlock);
            if (exitCode < 0)
            {
                ConfigureTerminalForChildProcess(false);
                ThrowTerminatingError(
                    new ErrorRecord(
                        new Exception(
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                CommandBaseStrings.ExecFailed,
                                Marshal.GetLastPInvokeError(),
                                string.Join(' ', WithCommand)
                            )
                        ),
                        "ExecutionFailed",
                        ErrorCategory.InvalidOperation,
                        WithCommand
                    )
                );
            }
        }

        
        [DllImport("libc",
            EntryPoint = "execve",
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi,
            SetLastError = true)]
        private static extern int Exec(string path, string?[] args, string?[] env);

        // leverage .NET runtime's native library which abstracts the need to handle different OS and architectures for termios api
        [DllImport("libSystem.Native", EntryPoint = "SystemNative_ConfigureTerminalForChildProcess")]
        private static extern void ConfigureTerminalForChildProcess([MarshalAs(UnmanagedType.Bool)] bool childUsesTerminal);
    }
}

#endif
