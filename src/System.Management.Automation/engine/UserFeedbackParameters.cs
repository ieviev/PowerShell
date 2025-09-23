// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    
    public sealed class PagingParameters
    {
        #region ctor

        internal PagingParameters(MshCommandRuntime commandRuntime)
        {
            if (commandRuntime == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandRuntime));
            }

            commandRuntime.PagingParameters = this;
        }

        #endregion ctor

        #region parameters

        
        [Parameter]
        public SwitchParameter IncludeTotalCount { get; set; }

        
        [Parameter]
        public UInt64 Skip { get; set; }

        
        [Parameter]
        public UInt64 First { get; set; } = UInt64.MaxValue;

        #endregion parameters

        #region emitting total count

        
        /// <param name="totalCount">A total count of objects that the cmdlet would return without paging.</param>
        /// <param name="accuracy">
        /// accuracy of the <paramref name="totalCount"/> parameter.
        /// <c>1.0</c> means 100% accurate;
        /// <c>0.0</c> means that total count is unknown;
        /// anything in-between means that total count is estimated
        /// </param>
        /// <returns>An object that represents a total count of objects that the cmdlet would return without paging.</returns>
        public PSObject NewTotalCount(UInt64 totalCount, double accuracy)
        {
            PSObject result = new PSObject(totalCount);

            string toStringMethodBody = string.Format(
                CultureInfo.CurrentCulture,
                @"
                    $totalCount = $this.PSObject.BaseObject
                    switch ($this.Accuracy) {{
                        {{ $_ -ge 1.0 }} {{ '{0}' -f $totalCount }}
                        {{ $_ -le 0.0 }} {{ '{1}' -f $totalCount }}
                        default          {{ '{2}' -f $totalCount }}
                    }}
                ",
                CodeGeneration.EscapeSingleQuotedStringContent(CommandBaseStrings.PagingSupportAccurateTotalCountTemplate),
                CodeGeneration.EscapeSingleQuotedStringContent(CommandBaseStrings.PagingSupportUnknownTotalCountTemplate),
                CodeGeneration.EscapeSingleQuotedStringContent(CommandBaseStrings.PagingSupportEstimatedTotalCountTemplate));
            PSScriptMethod toStringMethod = new PSScriptMethod("ToString", ScriptBlock.Create(toStringMethodBody));
            result.Members.Add(toStringMethod);

            accuracy = Math.Max(0.0, Math.Min(1.0, accuracy));
            PSNoteProperty statusProperty = new PSNoteProperty("Accuracy", accuracy);
            result.Members.Add(statusProperty);

            return result;
        }

        #endregion emitting total count
    }
}

namespace System.Management.Automation.Internal
{
    
    public sealed class ShouldProcessParameters
    {
        #region ctor

        
        /// <param name="commandRuntime">
        /// The instance of the command that the parameters should set the
        /// user feedback properties on when the parameters get bound.
        /// </param>
        internal ShouldProcessParameters(MshCommandRuntime commandRuntime)
        {
            if (commandRuntime == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandRuntime));
            }

            _commandRuntime = commandRuntime;
        }
        #endregion ctor

        #region parameters

        
        [Parameter]
        [Alias("wi")]
        public SwitchParameter WhatIf
        {
            get
            {
                return _commandRuntime.WhatIf;
            }

            set
            {
                _commandRuntime.WhatIf = value;
            }
        }

        
        [Parameter]
        [Alias("cf")]
        public SwitchParameter Confirm
        {
            get
            {
                return _commandRuntime.Confirm;
            }

            set
            {
                _commandRuntime.Confirm = value;
            }
        }
        #endregion parameters

        private readonly MshCommandRuntime _commandRuntime;
    }

    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "These are only exposed by way of the PowerShell cmdlets that surface them.")]
    public sealed class TransactionParameters
    {
        #region ctor

        
        /// <param name="commandRuntime">
        /// The instance of the command that the parameters should set the
        /// user feedback properties on when the parameters get bound.
        /// </param>
        internal TransactionParameters(MshCommandRuntime commandRuntime)
        {
            _commandRuntime = commandRuntime;
        }
        #endregion ctor

        #region parameters

        
        [Parameter]
        [Alias("usetx")]
        public SwitchParameter UseTransaction
        {
            get
            {
                return _commandRuntime.UseTransaction;
            }

            set
            {
                _commandRuntime.UseTransaction = value;
            }
        }

        #endregion parameters

        private readonly MshCommandRuntime _commandRuntime;
    }
}
