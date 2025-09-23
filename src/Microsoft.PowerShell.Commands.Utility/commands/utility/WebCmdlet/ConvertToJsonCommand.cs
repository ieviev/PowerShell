// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Threading;

using Newtonsoft.Json;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsData.ConvertTo, "Json", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096925", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public class ConvertToJsonCommand : PSCmdlet, IDisposable
    {
        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [AllowNull]
        public object InputObject { get; set; }

        private int _depth = 2;

        private readonly CancellationTokenSource _cancellationSource = new();

        
        [Parameter]
        [ValidateRange(0, 100)]
        public int Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }

        
        [Parameter]
        public SwitchParameter Compress { get; set; }

        
        [Parameter]
        public SwitchParameter EnumsAsStrings { get; set; }

        
        [Parameter]
        public SwitchParameter AsArray { get; set; }

        
        [Parameter]
        public StringEscapeHandling EscapeHandling { get; set; } = StringEscapeHandling.Default;

        
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        
        /// <param name="disposing">
        /// Specified as true when Dispose() was called, false if this is called from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationSource.Dispose();
            }
        }
        
        private readonly List<object> _inputObjects = new();

        
        protected override void ProcessRecord()
        {
            _inputObjects.Add(InputObject);
        }

        
        protected override void EndProcessing()
        {
            if (_inputObjects.Count > 0)
            {
                object objectToProcess = (_inputObjects.Count > 1 || AsArray) ? (_inputObjects.ToArray() as object) : _inputObjects[0];

                var context = new JsonObject.ConvertToJsonContext(
                    Depth,
                    EnumsAsStrings.IsPresent,
                    Compress.IsPresent,
                    EscapeHandling,
                    targetCmdlet: this,
                    _cancellationSource.Token);

                // null is returned only if the pipeline is stopping (e.g. ctrl+c is signaled).
                // in that case, we shouldn't write the null to the output pipe.
                string output = JsonObject.ConvertToJson(objectToProcess, in context);
                if (output != null)
                {
                    WriteObject(output);
                }
            }
        }

        
        protected override void StopProcessing()
        {
            _cancellationSource.Cancel();
        }
    }
}
