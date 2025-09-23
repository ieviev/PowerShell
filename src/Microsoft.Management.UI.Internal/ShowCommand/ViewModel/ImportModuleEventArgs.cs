// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    public class ImportModuleEventArgs : EventArgs
    {
        
        private string commandName;

        
        private string parentModuleName;

        
        private string selectedModuleName;

        
        /// <param name="commandName">The name for the command needing help.</param>
        /// <param name="parentModuleName">The name of the module containing the command.</param>
        /// <param name="selectedModuleName">
        /// the name of the module that is selected, which can be different from parentModuleName
        /// if "All" is selected
        /// </param>
        public ImportModuleEventArgs(string commandName, string parentModuleName, string selectedModuleName)
        {
            this.commandName = commandName;
            this.parentModuleName = parentModuleName;
            this.selectedModuleName = selectedModuleName;
        }

        
        public string CommandName
        {
            get { return this.commandName; }
        }

        
        public string ParentModuleName
        {
            get { return this.parentModuleName; }
        }

        
        public string SelectedModuleName
        {
            get { return this.selectedModuleName; }
        }
    }
}
