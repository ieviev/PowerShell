// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    internal class LogContext
    {
        #region Context Properties

        internal string Severity { get; set; } = string.Empty;

        
        internal string HostName { get; set; } = string.Empty;

        
        internal string HostApplication
        {
            get; set;
        }

        
        internal string HostVersion { get; set; } = string.Empty;

        
        internal string HostId { get; set; } = string.Empty;

        
        internal string EngineVersion { get; set; } = string.Empty;

        
        internal string RunspaceId { get; set; } = string.Empty;

        
        internal string PipelineId { get; set; } = string.Empty;

        
        internal string CommandName { get; set; } = string.Empty;

        
        internal string CommandType { get; set; } = string.Empty;

        
        internal string ScriptName { get; set; } = string.Empty;

        
        internal string CommandPath { get; set; } = string.Empty;

        
        internal string CommandLine { get; set; } = string.Empty;

        
        internal string SequenceNumber { get; set; } = string.Empty;

        
        internal string User { get; set; } = string.Empty;

        
        internal string ConnectedUser { get; set; }

        
        internal string Time { get; set; } = string.Empty;

        #endregion

        #region Shell Id

        
        internal string ShellId { get; set; }

        #endregion

        #region Execution context

        
        internal ExecutionContext ExecutionContext { get; set; }

        #endregion
    }
}
