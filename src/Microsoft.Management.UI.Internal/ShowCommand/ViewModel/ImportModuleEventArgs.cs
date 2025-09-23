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
