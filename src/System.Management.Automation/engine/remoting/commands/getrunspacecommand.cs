// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "PSSession", DefaultParameterSetName = PSRunspaceCmdlet.NameParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096697", RemotingCapability = RemotingCapability.OwnedByCommand)]
    [OutputType(typeof(PSSession))]
    public class GetPSSessionCommand : PSRunspaceCmdlet, IDisposable
    {
        #region Parameters

        private const string ConnectionUriParameterSet = "ConnectionUri";
        private const string ConnectionUriInstanceIdParameterSet = "ConnectionUriInstanceId";

        
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("Cn")]
        public override string[] ComputerName { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        public string ApplicationName
        {
            get
            {
                return _appName;
            }

            set
            {
                _appName = ResolveAppName(value);
            }
        }

        private string _appName;

        
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("URI", "CU")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Uri[] ConnectionUri { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ContainerIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.ContainerIdInstanceIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.VMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.VMIdInstanceIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.VMNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                           ParameterSetName = GetPSSessionCommand.VMNameInstanceIdParameterSet)]
        public string ConfigurationName { get; set; }

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public SwitchParameter AllowRedirection
        {
            get { return _allowRedirection; }

            set { _allowRedirection = value; }
        }

        private bool _allowRedirection = false;

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.NameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public override string[] Name
        {
            get { return base.Name; }

            set { base.Name = value; }
        }

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet,
                   Mandatory = true)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.InstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ContainerIdInstanceIdParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMIdInstanceIdParameterSet,
                   Mandatory = true)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMNameInstanceIdParameterSet,
                   Mandatory = true)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public override Guid[] InstanceId
        {
            get { return base.InstanceId; }

            set { base.InstanceId = value; }
        }

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [Credential]
        public PSCredential Credential
        {
            get
            {
                return _psCredential;
            }

            set
            {
                _psCredential = value;

                PSRemotingBaseCmdlet.ValidateSpecifiedAuthentication(Credential, CertificateThumbprint, Authentication);
            }
        }

        private PSCredential _psCredential;

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public AuthenticationMechanism Authentication
        {
            get
            {
                return _authentication;
            }

            set
            {
                _authentication = value;

                PSRemotingBaseCmdlet.ValidateSpecifiedAuthentication(Credential, CertificateThumbprint, Authentication);
            }
        }

        private AuthenticationMechanism _authentication;

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public string CertificateThumbprint
        {
            get
            {
                return _thumbprint;
            }

            set
            {
                _thumbprint = value;

                PSRemotingBaseCmdlet.ValidateSpecifiedAuthentication(Credential, CertificateThumbprint, Authentication);
            }
        }

        private string _thumbprint;

        
        /// <remarks>
        /// Currently this is being accepted as a parameter. But in future
        /// support will be added to make this a part of a policy setting.
        /// When a policy setting is in place this parameter can be used
        /// to override the policy setting
        /// </remarks>
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [ValidateRange((int)1, (int)UInt16.MaxValue)]
        public int Port { get; set; }

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public SwitchParameter UseSSL { get; set; }

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public int ThrottleLimit { get; set; } = 0;

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ContainerIdInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMIdInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.VMNameInstanceIdParameterSet)]
        public SessionFilterState State { get; set; }

        
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ComputerInstanceIdParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriParameterSet)]
        [Parameter(ParameterSetName = GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)]
        public PSSessionOption SessionOption { get; set; }

        #endregion

        #region Overrides

        
        protected override void BeginProcessing()
        {
#if UNIX
            if (ComputerName?.Length > 0)
            {
                ErrorRecord err = new(
                    new NotImplementedException(
                        PSRemotingErrorInvariants.FormatResourceString(
                            RemotingErrorIdStrings.UnsupportedOSForRemoteEnumeration,
                            RuntimeInformation.OSDescription)),
                    "PSSessionComputerNameUnix",
                    ErrorCategory.NotImplemented,
                    null);
                ThrowTerminatingError(err);
            }
#endif

            base.BeginProcessing();
            ConfigurationName ??= string.Empty;
        }

        
        protected override void ProcessRecord()
        {
            if ((ParameterSetName == GetPSSessionCommand.NameParameterSet) && ((Name == null) || (Name.Length == 0)))
            {
                // that means Get-PSSession (with no parameters)..so retrieve all the runspaces.
                GetAllRunspaces(true, true);
            }
            else if (ParameterSetName == GetPSSessionCommand.ComputerNameParameterSet ||
                     ParameterSetName == GetPSSessionCommand.ComputerInstanceIdParameterSet ||
                     ParameterSetName == GetPSSessionCommand.ConnectionUriParameterSet ||
                     ParameterSetName == GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)
            {
                // Perform the remote query for each provided computer name.
                QueryForRemoteSessions();
            }
            else
            {
                GetMatchingRunspaces(true, true, this.State, this.ConfigurationName);
            }
        }

        
        protected override void EndProcessing()
        {
            _stream.ObjectWriter.Close();
        }

        
        protected override void StopProcessing()
        {
            _queryRunspaces.StopAllOperations();
        }

        #endregion Overrides

        #region Private Methods

        
        private void QueryForRemoteSessions()
        {
            // Get collection of connection objects for each computer name or
            // connection uri.
            Collection<WSManConnectionInfo> connectionInfos = GetConnectionObjects();

            // Query for sessions.
            Collection<PSSession> results = _queryRunspaces.GetDisconnectedSessions(connectionInfos, this.Host, _stream,
                                                                                        this.RunspaceRepository, ThrottleLimit,
                                                                                        State, InstanceId, Name, ConfigurationName);

            // Write any error output from stream object.
            Collection<object> streamObjects = _stream.ObjectReader.NonBlockingRead();
            foreach (object streamObject in streamObjects)
            {
                if (this.IsStopping)
                {
                    break;
                }

                WriteStreamObject((Action<Cmdlet>)streamObject);
            }

            // Write each session object.
            foreach (PSSession session in results)
            {
                if (this.IsStopping)
                {
                    break;
                }

                WriteObject(session);
            }
        }

        private Collection<WSManConnectionInfo> GetConnectionObjects()
        {
            Collection<WSManConnectionInfo> connectionInfos = new Collection<WSManConnectionInfo>();

            if (ParameterSetName == GetPSSessionCommand.ComputerNameParameterSet ||
                ParameterSetName == GetPSSessionCommand.ComputerInstanceIdParameterSet)
            {
                string scheme = UseSSL.IsPresent ? WSManConnectionInfo.HttpsScheme : WSManConnectionInfo.HttpScheme;

                foreach (string computerName in ComputerName)
                {
                    WSManConnectionInfo connectionInfo = new WSManConnectionInfo();
                    connectionInfo.Scheme = scheme;
                    connectionInfo.ComputerName = ResolveComputerName(computerName);
                    connectionInfo.AppName = ApplicationName;
                    connectionInfo.ShellUri = ConfigurationName;
                    connectionInfo.Port = Port;
                    if (CertificateThumbprint != null)
                    {
                        connectionInfo.CertificateThumbprint = CertificateThumbprint;
                    }
                    else
                    {
                        connectionInfo.Credential = Credential;
                    }

                    connectionInfo.AuthenticationMechanism = Authentication;
                    UpdateConnectionInfo(connectionInfo);

                    connectionInfos.Add(connectionInfo);
                }
            }
            else if (ParameterSetName == GetPSSessionCommand.ConnectionUriParameterSet ||
                     ParameterSetName == GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)
            {
                foreach (var connectionUri in ConnectionUri)
                {
                    WSManConnectionInfo connectionInfo = new WSManConnectionInfo();
                    connectionInfo.ConnectionUri = connectionUri;
                    connectionInfo.ShellUri = ConfigurationName;
                    if (CertificateThumbprint != null)
                    {
                        connectionInfo.CertificateThumbprint = CertificateThumbprint;
                    }
                    else
                    {
                        connectionInfo.Credential = Credential;
                    }

                    connectionInfo.AuthenticationMechanism = Authentication;
                    UpdateConnectionInfo(connectionInfo);

                    connectionInfos.Add(connectionInfo);
                }
            }

            return connectionInfos;
        }

        
        /// <param name="connectionInfo"></param>
        private void UpdateConnectionInfo(WSManConnectionInfo connectionInfo)
        {
            if (ParameterSetName != GetPSSessionCommand.ConnectionUriParameterSet &&
                ParameterSetName != GetPSSessionCommand.ConnectionUriInstanceIdParameterSet)
            {
                // uri redirection is supported only with URI parameter set
                connectionInfo.MaximumConnectionRedirectionCount = 0;
            }

            if (!_allowRedirection)
            {
                // uri redirection required explicit user consent
                connectionInfo.MaximumConnectionRedirectionCount = 0;
            }

            // Update the connectionInfo object with passed in session options.
            if (SessionOption != null)
            {
                connectionInfo.SetSessionOptions(SessionOption);
            }
        }

        #endregion

        #region IDisposable

        
        public void Dispose()
        {
            _stream.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Members

        // Object used for querying remote runspaces.
        private readonly QueryRunspaces _queryRunspaces = new QueryRunspaces();

        // Object to collect output data from multiple threads.
        private readonly ObjectStream _stream = new ObjectStream();

        #endregion
    }
}
