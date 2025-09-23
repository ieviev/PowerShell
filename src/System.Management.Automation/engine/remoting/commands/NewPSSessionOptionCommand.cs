// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.New, "PSSessionOption", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096488", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(PSSessionOption))]
    public sealed class NewPSSessionOptionCommand : PSCmdlet
    {
        #region Parameters (specific to PSSessionOption)

#if !UNIX
        
        [Parameter]
        public int MaximumRedirection
        {
            get { return _maximumRedirection.Value; }

            set { _maximumRedirection = value; }
        }

        private int? _maximumRedirection;

        
        [Parameter]
        public SwitchParameter NoCompression { get; set; }

        
        [Parameter]
        public SwitchParameter NoMachineProfile { get; set; }

        
        [Parameter]
        [ValidateNotNull]
        public CultureInfo Culture { get; set; }

        
        [Parameter]
        [ValidateNotNull]
        public CultureInfo UICulture { get; set; }

        
        [Parameter]
        public int MaximumReceivedDataSizePerCommand
        {
            get { return _maxRecvdDataSizePerCommand.Value; }

            set { _maxRecvdDataSizePerCommand = value; }
        }

        private int? _maxRecvdDataSizePerCommand;

        
        [Parameter]
        public int MaximumReceivedObjectSize
        {
            get { return _maxRecvdObjectSize.Value; }

            set { _maxRecvdObjectSize = value; }
        }

        private int? _maxRecvdObjectSize;

        
        [Parameter]
        public OutputBufferingMode OutputBufferingMode { get; set; }

        
        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public int MaxConnectionRetryCount { get; set; }

        
        [Parameter]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSPrimitiveDictionary ApplicationArguments { get; set; }

        
        [Parameter]
        [Alias("OpenTimeoutMSec")]
        [ValidateRange(0, Int32.MaxValue)]
        public int OpenTimeout
        {
            get
            {
                return _openTimeout ?? RunspaceConnectionInfo.DefaultOpenTimeout;
            }

            set
            {
                _openTimeout = value;
            }
        }

        private int? _openTimeout;

        
        [Parameter]
        [Alias("CancelTimeoutMSec")]
        [ValidateRange(0, Int32.MaxValue)]
        public int CancelTimeout
        {
            get
            {
                return _cancelTimeout ?? BaseTransportManager.ClientCloseTimeoutMs;
            }

            set
            {
                _cancelTimeout = value;
            }
        }

        private int? _cancelTimeout;

        
        [Parameter]
        [ValidateRange(-1, Int32.MaxValue)]
        [Alias("IdleTimeoutMSec")]
        public int IdleTimeout
        {
            get
            {
                return _idleTimeout ?? RunspaceConnectionInfo.DefaultIdleTimeout;
            }

            set
            {
                _idleTimeout = value;
            }
        }

        private int? _idleTimeout;
#endif

        #endregion Parameters

        #region Parameters copied from New-WSManSessionOption

#if !UNIX
        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public ProxyAccessType ProxyAccessType { get; set; } = ProxyAccessType.None;

        
        [Parameter]
        public AuthenticationMechanism ProxyAuthentication { get; set; } = AuthenticationMechanism.Negotiate;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Credential]
        public PSCredential ProxyCredential { get; set; }
#endif

        
        [Parameter]
        public SwitchParameter SkipCACheck
        {
            get { return _skipcacheck; }

            set { _skipcacheck = value; }
        }

        private bool _skipcacheck;

        
        [Parameter]
        public SwitchParameter SkipCNCheck
        {
            get { return _skipcncheck; }

            set { _skipcncheck = value; }
        }

        private bool _skipcncheck;

#if !UNIX

        
        [Parameter]
        public SwitchParameter SkipRevocationCheck
        {
            get { return _skiprevocationcheck; }

            set { _skiprevocationcheck = value; }
        }

        private bool _skiprevocationcheck;

        
        [Parameter]
        [Alias("OperationTimeoutMSec")]
        [ValidateRange(0, Int32.MaxValue)]
        public int OperationTimeout
        {
            get
            {
                return _operationtimeout ?? BaseTransportManager.ClientDefaultOperationTimeoutMs;
            }

            set
            {
                _operationtimeout = value;
            }
        }

        private int? _operationtimeout;

        
        [Parameter]
        public SwitchParameter NoEncryption
        {
            get
            {
                return _noencryption;
            }

            set
            {
                _noencryption = value;
            }
        }

        private bool _noencryption;

        
        [Parameter]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "UTF")]
        public SwitchParameter UseUTF16
        {
            get
            {
                return _useutf16;
            }

            set
            {
                _useutf16 = value;
            }
        }

        private bool _useutf16;

        
        [Parameter]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        public SwitchParameter IncludePortInSPN
        {
            get { return _includePortInSPN; }

            set { _includePortInSPN = value; }
        }

        private bool _includePortInSPN;

#endif

        #endregion

        #region Implementation

        
        protected override void BeginProcessing()
        {
            PSSessionOption result = new PSSessionOption();
            // Begin: WSMan specific options
#if !UNIX
            result.ProxyAccessType = this.ProxyAccessType;
            result.ProxyAuthentication = this.ProxyAuthentication;
            result.ProxyCredential = this.ProxyCredential;
#endif
            result.SkipCACheck = this.SkipCACheck;
            result.SkipCNCheck = this.SkipCNCheck;
#if !UNIX
            result.SkipRevocationCheck = this.SkipRevocationCheck;
            if (_operationtimeout.HasValue)
            {
                result.OperationTimeout = TimeSpan.FromMilliseconds(_operationtimeout.Value);
            }

            result.NoEncryption = this.NoEncryption;
            result.UseUTF16 = this.UseUTF16;
            result.IncludePortInSPN = this.IncludePortInSPN;
            // End: WSMan specific options
            if (_maximumRedirection.HasValue)
            {
                result.MaximumConnectionRedirectionCount = this.MaximumRedirection;
            }

            result.NoCompression = this.NoCompression.IsPresent;
            result.NoMachineProfile = this.NoMachineProfile.IsPresent;

            result.MaximumReceivedDataSizePerCommand = _maxRecvdDataSizePerCommand;
            result.MaximumReceivedObjectSize = _maxRecvdObjectSize;

            if (this.Culture != null)
            {
                result.Culture = this.Culture;
            }

            if (this.UICulture != null)
            {
                result.UICulture = this.UICulture;
            }

            if (_openTimeout.HasValue)
            {
                result.OpenTimeout = TimeSpan.FromMilliseconds(_openTimeout.Value);
            }

            if (_cancelTimeout.HasValue)
            {
                result.CancelTimeout = TimeSpan.FromMilliseconds(_cancelTimeout.Value);
            }

            if (_idleTimeout.HasValue)
            {
                result.IdleTimeout = TimeSpan.FromMilliseconds(_idleTimeout.Value);
            }

            result.OutputBufferingMode = OutputBufferingMode;

            result.MaxConnectionRetryCount = MaxConnectionRetryCount;

            if (this.ApplicationArguments != null)
            {
                result.ApplicationArguments = this.ApplicationArguments;
            }
#endif

            this.WriteObject(result);
        }

        #endregion Methods
    }
}
