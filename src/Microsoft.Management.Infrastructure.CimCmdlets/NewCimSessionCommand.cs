// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using Microsoft.Management.Infrastructure.Options;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    [Alias("ncms")]
    [Cmdlet(VerbsCommon.New, "CimSession", DefaultParameterSetName = CredentialParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227967")]
    [OutputType(typeof(CimSession))]
    public sealed class NewCimSessionCommand : CimBaseCommand
    {
        #region cmdlet parameters

        
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CredentialParameterSet)]
        public PasswordAuthenticationMechanism Authentication
        {
            get
            {
                return authentication;
            }

            set
            {
                authentication = value;
                authenticationSet = true;
            }
        }

        private PasswordAuthenticationMechanism authentication;
        private bool authenticationSet = false;

        
        [Parameter(Position = 1, ParameterSetName = CredentialParameterSet)]
        [Credential]
        public PSCredential Credential { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CertificateParameterSet)]
        public string CertificateThumbprint { get; set; }

        
        [Alias(AliasCN, AliasServerName)]
        [Parameter(
            Position = 0,
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        
        [Alias(AliasOT)]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public uint OperationTimeoutSec
        {
            get
            {
                return operationTimeout;
            }

            set
            {
                operationTimeout = value;
                operationTimeoutSet = true;
            }
        }

        private uint operationTimeout;
        internal bool operationTimeoutSet = false;

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter SkipTestConnection { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public uint Port
        {
            get
            {
                return port;
            }

            set
            {
                port = value;
                portSet = true;
            }
        }

        private uint port;
        private bool portSet = false;

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public Microsoft.Management.Infrastructure.Options.CimSessionOptions SessionOption { get; set; }

        #endregion

        #region cmdlet processing methods

        
        protected override void BeginProcessing()
        {
            cimNewSession = new CimNewSession();
            this.CmdletOperation = new CmdletOperationTestCimSession(this, this.cimNewSession);
            this.AtBeginProcess = false;
        }

        
        protected override void ProcessRecord()
        {
            CimSessionOptions outputOptions;
            CimCredential outputCredential;
            BuildSessionOptions(out outputOptions, out outputCredential);
            cimNewSession.NewCimSession(this, outputOptions, outputCredential);
            cimNewSession.ProcessActions(this.CmdletOperation);
        }

        
        protected override void EndProcessing()
        {
            cimNewSession.ProcessRemainActions(this.CmdletOperation);
        }
        #endregion

        #region helper methods

        
        internal void BuildSessionOptions(out CimSessionOptions outputOptions, out CimCredential outputCredential)
        {
            DebugHelper.WriteLogEx();

            CimSessionOptions options = null;
            if (this.SessionOption != null)
            {
                // clone the sessionOption object
                if (this.SessionOption is WSManSessionOptions)
                {
                    options = new WSManSessionOptions(this.SessionOption as WSManSessionOptions);
                }
                else
                {
                    options = new DComSessionOptions(this.SessionOption as DComSessionOptions);
                }
            }

            outputOptions = null;
            outputCredential = null;
            if (options != null)
            {
                if (options is DComSessionOptions dcomOptions)
                {
                    bool conflict = false;
                    string parameterName = string.Empty;
                    if (this.CertificateThumbprint != null)
                    {
                        conflict = true;
                        parameterName = @"CertificateThumbprint";
                    }

                    if (portSet)
                    {
                        conflict = true;
                        parameterName = @"Port";
                    }

                    if (conflict)
                    {
                        ThrowConflictParameterWasSet(@"New-CimSession", parameterName, @"DComSessionOptions");
                        return;
                    }
                }
            }

            if (portSet || (this.CertificateThumbprint != null))
            {
                WSManSessionOptions wsmanOptions = (options == null) ? new WSManSessionOptions() : options as WSManSessionOptions;
                if (portSet)
                {
                    wsmanOptions.DestinationPort = this.Port;
                    portSet = false;
                }

                if (this.CertificateThumbprint != null)
                {
                    CimCredential credentials = new(CertificateAuthenticationMechanism.Default, this.CertificateThumbprint);
                    wsmanOptions.AddDestinationCredentials(credentials);
                }

                options = wsmanOptions;
            }

            if (this.operationTimeoutSet)
            {
                if (options != null)
                {
                    options.Timeout = TimeSpan.FromSeconds((double)this.OperationTimeoutSec);
                }
            }

            if (this.authenticationSet || (this.Credential != null))
            {
                PasswordAuthenticationMechanism authentication = this.authenticationSet ? this.Authentication : PasswordAuthenticationMechanism.Default;
                if (this.authenticationSet)
                {
                    this.authenticationSet = false;
                }

                CimCredential credentials = CreateCimCredentials(this.Credential, authentication, @"New-CimSession", @"Authentication");
                if (credentials == null)
                {
                    return;
                }

                DebugHelper.WriteLog("Credentials: {0}", 1, credentials);
                outputCredential = credentials;
                if (options != null)
                {
                    DebugHelper.WriteLog("Add credentials to option: {0}", 1, options);
                    options.AddDestinationCredentials(credentials);
                }
            }

            DebugHelper.WriteLogEx("Set outputOptions: {0}", 1, outputOptions);
            outputOptions = options;
        }

        #endregion

        #region private members

        
        private CimNewSession cimNewSession;

        #endregion

        #region IDisposable
        
        protected override void DisposeInternal()
        {
            base.DisposeInternal();

            // Dispose managed resources.
            this.cimNewSession?.Dispose();
        }
        #endregion
    }
}
