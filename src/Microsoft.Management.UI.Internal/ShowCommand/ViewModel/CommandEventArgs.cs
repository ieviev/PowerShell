// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public class CommandEventArgs : EventArgs
    {
        
        private CommandViewModel command;

        
        public CommandEventArgs(CommandViewModel command)
        {
            this.command = command;
        }

        
        public CommandViewModel Command
        {
            get { return this.command; }
        }
    }
}
