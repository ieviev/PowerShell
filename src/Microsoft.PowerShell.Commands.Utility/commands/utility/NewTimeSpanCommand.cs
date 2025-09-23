// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.New, "TimeSpan", DefaultParameterSetName = "Date",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096709", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(TimeSpan))]
    public sealed class NewTimeSpanCommand : PSCmdlet
    {
        #region parameters

        
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Date")]
        [Alias("LastWriteTime")]
        public DateTime Start
        {
            get
            {
                return _start;
            }

            set
            {
                _start = value;
                _startSpecified = true;
            }
        }

        private DateTime _start;
        private bool _startSpecified;

        
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "Date")]
        public DateTime End
        {
            get
            {
                return _end;
            }

            set
            {
                _end = value;
                _endSpecified = true;
            }
        }

        private DateTime _end;
        private bool _endSpecified = false;

        
        [Parameter(ParameterSetName = "Time")]
        public int Days { get; set; }

        
        [Parameter(ParameterSetName = "Time")]
        public int Hours { get; set; }

        
        [Parameter(ParameterSetName = "Time")]
        public int Minutes { get; set; }

        
        [Parameter(ParameterSetName = "Time")]
        public int Seconds { get; set; }

        
        [Parameter(ParameterSetName = "Time")]
        public int Milliseconds { get; set; }

        #endregion

        #region methods

        
        protected override void ProcessRecord()
        {
            // initially set start and end time to be equal
            DateTime startTime = DateTime.Now;
            DateTime endTime = startTime;
            TimeSpan result;

            switch (ParameterSetName)
            {
                case "Date":
                    if (_startSpecified)
                    {
                        startTime = Start;
                    }

                    if (_endSpecified)
                    {
                        endTime = End;
                    }

                    result = endTime.Subtract(startTime);
                    break;

                case "Time":
                    result = new TimeSpan(Days, Hours, Minutes, Seconds, Milliseconds);
                    break;

                default:
                    Dbg.Diagnostics.Assert(false, "Only one of the specified parameter sets should be called.");
                    return;
            }

            WriteObject(result);
        }
        #endregion
    }
}
