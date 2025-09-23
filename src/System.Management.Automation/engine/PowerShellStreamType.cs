// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    /// <remarks>
    /// This enumeration is a public type formerly used in PowerShell Workflow,
    /// but kept due to its generic name and public accessibility.
    /// It is not used by any other PowerShell API, and is now obsolete
    /// and should not be used if possible.
    /// </remarks>
    [Obsolete("This enum type was used only in PowerShell Workflow and is now obsolete.", error: true)]
    public enum PowerShellStreamType
    {
        
        Input = 0,

        
        Output = 1,

        
        Error = 2,

        
        Warning = 3,

        
        Verbose = 4,

        
        Debug = 5,

        
        Progress = 6,

        
        Information = 7
    }
}
