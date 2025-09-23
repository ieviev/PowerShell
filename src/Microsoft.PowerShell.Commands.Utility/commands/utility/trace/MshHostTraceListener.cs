// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Internal.Host;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    
    /// <remarks>
    /// This trace listener cannot be specified in the app.config file.
    /// It must be added through the add-tracelistener cmdlet.
    /// </remarks>
    internal sealed class PSHostTraceListener
        : System.Diagnostics.TraceListener
    {
        #region TraceListener constructors and disposer

        
        internal PSHostTraceListener(PSCmdlet cmdlet)
            : base(string.Empty)
        {
            if (cmdlet == null)
            {
                throw new PSArgumentNullException(nameof(cmdlet));
            }

            Diagnostics.Assert(
                cmdlet.Host.UI is InternalHostUserInterface,
                "The internal host must be available to trace");

            _ui = cmdlet.Host.UI as InternalHostUserInterface;
        }

        ~PSHostTraceListener()
        {
            Dispose(false);
        }

        
        /// <param name="disposing">
        /// True if the TraceListener is being disposed, false otherwise.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this.Close();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion TraceListener constructors and disposer

        
        /// <param name="output">
        /// The trace output to be written.
        /// </param>
        public override void Write(string output)
        {
            try
            {
                _cachedWrite.Append(output);
            }
            catch (Exception)
            {
                // Catch and ignore all exceptions while tracing
                // We don't want tracing to bring down the process.
            }
        }

        private readonly StringBuilder _cachedWrite = new();

        
        /// <param name="output">
        /// The trace output to be written.
        /// </param>
        public override void WriteLine(string output)
        {
            try
            {
                _cachedWrite.Append(output);
                _cachedWrite.Insert(0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff "));

                _ui.WriteDebugLine(_cachedWrite.ToString());
                _cachedWrite.Remove(0, _cachedWrite.Length);
            }
            catch (Exception)
            {
                // Catch and ignore all exceptions while tracing
                // We don't want tracing to bring down the process.
            }
        }

        
        private readonly InternalHostUserInterface _ui;
    }
}
