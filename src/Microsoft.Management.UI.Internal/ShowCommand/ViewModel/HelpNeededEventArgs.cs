// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public class HelpNeededEventArgs : EventArgs
    {
        
        private string commandName;

        
        /// <param name="commandName">The name for the command needing help.</param>
        public HelpNeededEventArgs(string commandName)
        {
            this.commandName = commandName;
        }

        
        public string CommandName
        {
            get { return this.commandName; }
        }
    }
}
