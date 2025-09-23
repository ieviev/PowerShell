// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    
    public abstract class PSRemotingCmdlet : PSCmdlet
    {
        #region Overrides

        
        protected override void BeginProcessing()
        {
            if (!SkipWinRMCheck)
            {
                RemotingCommandUtil.CheckRemotingCmdletPrerequisites();
            }
        }

        #endregion Overrides

        #region Utility functions

        
        internal void WriteStreamObject(Action<Cmdlet> action)
        {
            action(this);
        }

        
        /// <param name="computerNames">Array of computer names to resolve.</param>
        /// <param name="resolvedComputerNames">Resolved array of machine names.</param>
        protected void ResolveComputerNames(string[] computerNames, out string[] resolvedComputerNames)
        {
            if (computerNames == null)
            {
                resolvedComputerNames = new string[1];

                resolvedComputerNames[0] = ResolveComputerName(".");
            }
            else if (computerNames.Length == 0)
            {
                resolvedComputerNames = Array.Empty<string>();
            }
            else
            {
                resolvedComputerNames = new string[computerNames.Length];

                for (int i = 0; i < resolvedComputerNames.Length; i++)
                {
                    resolvedComputerNames[i] = ResolveComputerName(computerNames[i]);
                }
            }
        }

        
        /// <param name="computerName">Computer name to resolve.</param>
        /// <returns>Resolved computer name.</returns>
        protected string ResolveComputerName(string computerName)
        {
            Diagnostics.Assert(computerName != null, "Null ComputerName");

            if (string.Equals(computerName, ".", StringComparison.OrdinalIgnoreCase))
            {
                // tracer.WriteEvent(ref PSEventDescriptors.PS_EVENT_HOSTNAMERESOLVE);
                // tracer.Dispose();
                // tracer.OperationalChannel.WriteVerbose(PSEventId.HostNameResolve, PSOpcode.Method, PSTask.CreateRunspace);
                return s_LOCALHOST;
            }
            else
            {
                return computerName;
            }
        }

        
        /// <param name="resourceString">resource String which holds the message
        /// </param>
        /// <returns>Error message loaded from appropriate resource cache.</returns>
        internal string GetMessage(string resourceString)
        {
            string message = GetMessage(resourceString, null);

            return message;
        }

        
        /// <param name="resourceString"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal string GetMessage(string resourceString, params object[] args)
        {
            string message;

            if (args != null)
            {
                message = StringUtil.Format(resourceString, args);
            }
            else
            {
                message = resourceString;
            }

            return message;
        }

        #endregion Utility functions

        #region Private Members

        private static readonly string s_LOCALHOST = "localhost";

        // private PSETWTracer tracer = PSETWTracer.GetETWTracer(PSKeyword.Cmdlets);

        #endregion Private Members

        #region Protected Members

        
        protected const string ComputerNameParameterSet = "ComputerName";

        
        protected const string ComputerInstanceIdParameterSet = "ComputerInstanceId";

        
        protected const string ContainerIdParameterSet = "ContainerId";

        
        protected const string VMIdParameterSet = "VMId";

        
        protected const string VMNameParameterSet = "VMName";

        
        protected const string SSHHostParameterSet = "SSHHost";

        
        protected const string SSHHostHashParameterSet = "SSHHostHashParam";

        
        protected const string SessionParameterSet = "Session";

        
        protected const string UseWindowsPowerShellParameterSet = "UseWindowsPowerShellParameterSet";

        
        protected const string DefaultPowerShellRemoteShellName = WSManNativeApi.ResourceURIPrefix + "Microsoft.PowerShell";

        
        protected const string DefaultPowerShellRemoteShellAppName = "WSMan";

        #endregion Protected Members

        #region Internal Members

        
        internal bool SkipWinRMCheck { get; set; } = false;

        #endregion Internal Members

        #region Protected Methods

        
        /// <returns>The shell to launch in the remote machine.</returns>
        protected string ResolveShell(string shell)
        {
            string resolvedShell;

            if (!string.IsNullOrEmpty(shell))
            {
                resolvedShell = shell;
            }
            else
            {
                resolvedShell = (string)SessionState.Internal.ExecutionContext.GetVariableValue(
                    SpecialVariables.PSSessionConfigurationNameVarPath, DefaultPowerShellRemoteShellName);
            }

            return resolvedShell;
        }

        
        /// <param name="appName">Application name to resolve.</param>
        /// <returns>Resolved appname.</returns>
        protected string ResolveAppName(string appName)
        {
            string resolvedAppName;

            if (!string.IsNullOrEmpty(appName))
            {
                resolvedAppName = appName;
            }
            else
            {
                resolvedAppName = (string)SessionState.Internal.ExecutionContext.GetVariableValue(
                    SpecialVariables.PSSessionApplicationNameVarPath,
                    DefaultPowerShellRemoteShellAppName);
            }

            return resolvedAppName;
        }

        #endregion
    }

    
    internal struct SSHConnection
    {
        public string ComputerName;
        public string UserName;
        public string KeyFilePath;
        public int Port;
        public string Subsystem;
        public int ConnectingTimeout;
        public Hashtable Options;
    }

    
    public abstract class PSRemotingBaseCmdlet : PSRemotingCmdlet
    {
        #region Enums

        
        internal enum VMState
        {
            
            Other = 1,

            
            Running = 2,

            
            Off = 3,

            
            Stopping = 4,

            
            Saved = 6,

            
            Paused = 9,

            
            Starting = 10,

            
            Reset = 11,

            
            Saving = 32773,

            
            Pausing = 32776,

            
            Resuming = 32777,

            
            FastSaved = 32779,

            
            FastSaving = 32780,

            
            ForceShutdown = 32781,

            
            ForceReboot = 32782,

            
            RunningCritical,

            
            OffCritical,

            
            StoppingCritical,

            
            SavedCritical,

            
            PausedCritical,

            
            StartingCritical,

            
            ResetCritical,

            
            SavingCritical,

            
            PausingCritical,

            
            ResumingCritical,

            
            FastSavedCritical,

            
            FastSavingCritical,
        }

#nullable enable
        
        /// <param name="value">The raw PSObject as returned by Get-VM.</param>
        /// <returns>The VMState value of the State property if present and parsable, otherwise null.</returns>
        internal VMState? GetVMStateProperty(PSObject value)
        {
            object? rawState = value.Properties["State"].Value;
            if (rawState is Enum enumState)
            {
                // If the Hyper-V module was directly importable we have the VMState enum
                // value which we can just cast to our VMState type.
                return (VMState)enumState;
            }
            else if (rawState is string stringState && Enum.TryParse(stringState, true, out VMState result))
            {
                // If the Hyper-V module was imported through implicit remoting on old
                // Windows versions we get a string back which we will try and parse
                // as the enum label.
                return result;
            }

            // Unknown scenario, this should not happen.
            string message = PSRemotingErrorInvariants.FormatResourceString(
                RemotingErrorIdStrings.HyperVFailedToGetStateUnknownType,
                rawState?.GetType()?.FullName ?? "null");
            throw new InvalidOperationException(message);
        }
#nullable disable

        #endregion

        #region Tracer

        // PSETWTracer tracer = PSETWTracer.GetETWTracer(PSKeyword.Runspace);

        #endregion Tracer

        #region Properties

        
        [Parameter(Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.SessionParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public virtual PSSession[] Session { get; set; }

        
        [Parameter(Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.ComputerNameParameterSet)]
        [Alias("Cn")]
        public virtual string[] ComputerName { get; set; }

        
        /// <remarks>If Null or empty string is specified, then localhost is assumed.
        /// The ResolveComputerNames will include this.
        /// </remarks>
        protected string[] ResolvedComputerNames { get; set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.VMIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("VMGuid")]
        public virtual Guid[] VMId { get; set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.VMNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public virtual string[] VMName { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.UriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.VMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.VMNameParameterSet)]
        [Credential()]
        public virtual PSCredential Credential
        {
            get
            {
                return _pscredential;
            }

            set
            {
                _pscredential = value;
                ValidateSpecifiedAuthentication(Credential, CertificateThumbprint, Authentication);
            }
        }

        private PSCredential _pscredential;

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.ContainerIdParameterSet)]
        [ValidateNotNullOrEmpty]
        public virtual string[] ContainerId { get; set; }

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.ContainerIdParameterSet)]
        public virtual SwitchParameter RunAsAdministrator { get; set; }

        
        /// <remarks>
        /// Currently this is being accepted as a parameter. But in future
        /// support will be added to make this a part of a policy setting.
        /// When a policy setting is in place this parameter can be used
        /// to override the policy setting
        /// </remarks>
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.SSHHostParameterSet)]
        [ValidateRange((int)1, (int)UInt16.MaxValue)]
        public virtual int Port { get; set; }

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.ComputerNameParameterSet)]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public virtual SwitchParameter UseSSL { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.ComputerNameParameterSet)]
        public virtual string ApplicationName
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

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.SessionParameterSet)]
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.UriParameterSet)]
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.ContainerIdParameterSet)]
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.VMIdParameterSet)]
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.VMNameParameterSet)]
        public virtual int ThrottleLimit { get; set; } = 0;

        
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.UriParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("URI", "CU")]
        public virtual Uri[] ConnectionUri { get; set; }

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.UriParameterSet)]
        public virtual SwitchParameter AllowRedirection
        {
            get { return _allowRedirection; }

            set { _allowRedirection = value; }
        }

        private bool _allowRedirection = false;

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.UriParameterSet)]
        [ValidateNotNull]
        public virtual PSSessionOption SessionOption
        {
            get
            {
                if (_sessionOption == null)
                {
                    object tmp = this.SessionState.PSVariable.GetValue(DEFAULT_SESSION_OPTION);
                    if (tmp == null || !LanguagePrimitives.TryConvertTo<PSSessionOption>(tmp, out _sessionOption))
                    {
                        _sessionOption = new PSSessionOption();
                    }
                }

                return _sessionOption;
            }

            set
            {
                _sessionOption = value;
            }
        }

        private PSSessionOption _sessionOption;

        internal const string DEFAULT_SESSION_OPTION = "PSSessionOption";

        // Quota related variables.
        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.UriParameterSet)]
        public virtual AuthenticationMechanism Authentication
        {
            get
            {
                return _authMechanism;
            }

            set
            {
                _authMechanism = value;
                // Validate if a user can specify this authentication.
                ValidateSpecifiedAuthentication(Credential, CertificateThumbprint, Authentication);
            }
        }

        private AuthenticationMechanism _authMechanism = AuthenticationMechanism.Default;

        
        [Parameter(ParameterSetName = NewPSSessionCommand.ComputerNameParameterSet)]
        [Parameter(ParameterSetName = NewPSSessionCommand.UriParameterSet)]
        public virtual string CertificateThumbprint
        {
            get
            {
                return _thumbPrint;
            }

            set
            {
                _thumbPrint = value;
                ValidateSpecifiedAuthentication(Credential, CertificateThumbprint, Authentication);
            }
        }

        private string _thumbPrint = null;

        #region SSHHostParameters

        
        [Parameter(Position = 0, Mandatory = true,
            ParameterSetName = PSRemotingBaseCmdlet.SSHHostParameterSet)]
        [ValidateNotNullOrEmpty()]
        public virtual string[] HostName
        {
            get;
            set;
        }

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.SSHHostParameterSet)]
        [ValidateNotNullOrEmpty()]
        public virtual string UserName
        {
            get;
            set;
        }

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.SSHHostParameterSet)]
        [ValidateNotNullOrEmpty()]
        [Alias("IdentityFilePath")]
        public virtual string KeyFilePath
        {
            get;
            set;
        }

        
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRemotingBaseCmdlet.SSHHostParameterSet)]
        public virtual string Subsystem { get; set; }

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.SSHHostParameterSet)]
        public virtual int ConnectingTimeout { get; set; } = Timeout.Infinite;

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.SSHHostParameterSet)]
        [ValidateSet("true")]
        public virtual SwitchParameter SSHTransport
        {
            get;
            set;
        }

        
        [Parameter(ParameterSetName = PSRemotingBaseCmdlet.SSHHostHashParameterSet, Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public virtual Hashtable[] SSHConnection
        {
            get;
            set;
        }

        
        [Parameter(ParameterSetName = InvokeCommandCommand.SSHHostParameterSet)]
        [ValidateNotNullOrEmpty]
        public virtual Hashtable Options { get; set; }

        #endregion

        #endregion Properties

        #region Internal Static Methods

        
        /// <exception cref="InvalidOperationException">
        /// If there is ambiguity as specified above.
        /// </exception>
        internal static void ValidateSpecifiedAuthentication(PSCredential credential, string thumbprint, AuthenticationMechanism authentication)
        {
            if ((credential != null) && (thumbprint != null))
            {
                string message = PSRemotingErrorInvariants.FormatResourceString(
                    RemotingErrorIdStrings.NewRunspaceAmbiguousAuthentication,
                        "CertificateThumbPrint", "Credential");

                throw new InvalidOperationException(message);
            }

            if ((authentication != AuthenticationMechanism.Default) && (thumbprint != null))
            {
                string message = PSRemotingErrorInvariants.FormatResourceString(
                    RemotingErrorIdStrings.NewRunspaceAmbiguousAuthentication,
                        "CertificateThumbPrint", authentication.ToString());

                throw new InvalidOperationException(message);
            }

            if ((authentication == AuthenticationMechanism.NegotiateWithImplicitCredential) &&
                (credential != null))
            {
                string message = PSRemotingErrorInvariants.FormatResourceString(
                    RemotingErrorIdStrings.NewRunspaceAmbiguousAuthentication,
                    "Credential", authentication.ToString());
                throw new InvalidOperationException(message);
            }
        }

        #endregion

        #region Internal Methods

        #region SSH Connection Strings

        private const string ComputerNameParameter = "ComputerName";
        private const string HostNameAlias = "HostName";
        private const string UserNameParameter = "UserName";
        private const string KeyFilePathParameter = "KeyFilePath";
        private const string IdentityFilePathAlias = "IdentityFilePath";
        private const string PortParameter = "Port";
        private const string SubsystemParameter = "Subsystem";
        private const string ConnectingTimeoutParameter = "ConnectingTimeout";
        private const string OptionsParameter = "Options";

        #endregion

        
        /// <param name="hostname">Host name to parse.</param>
        /// <param name="host">Resolved target host.</param>
        /// <param name="userName">Resolved target user name.</param>
        /// <param name="port">Resolved target port.</param>
        protected void ParseSshHostName(string hostname, out string host, out string userName, out int port)
        {
            host = hostname;
            userName = this.UserName;
            port = this.Port;
            try
            {
                Uri uri = new System.Uri("ssh://" + hostname);
                host = ResolveComputerName(uri.Host);
                ValidateComputerName(new string[] { host });
                if (uri.UserInfo != string.Empty)
                {
                    userName = uri.UserInfo;
                }

                if (uri.Port != -1)
                {
                    port = uri.Port;
                }
            }
            catch (UriFormatException)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new ArgumentException(PSRemotingErrorInvariants.FormatResourceString(
                        RemotingErrorIdStrings.InvalidComputerName)), "PSSessionInvalidComputerName",
                            ErrorCategory.InvalidArgument, hostname));
            }
        }

        
        /// <returns>Array of SSHConnection objects.</returns>
        internal SSHConnection[] ParseSSHConnectionHashTable()
        {
            List<SSHConnection> connections = new();
            foreach (var item in this.SSHConnection)
            {
                if (item.ContainsKey(ComputerNameParameter) && item.ContainsKey(HostNameAlias))
                {
                    throw new PSArgumentException(RemotingErrorIdStrings.SSHConnectionDuplicateHostName);
                }

                if (item.ContainsKey(KeyFilePathParameter) && item.ContainsKey(IdentityFilePathAlias))
                {
                    throw new PSArgumentException(RemotingErrorIdStrings.SSHConnectionDuplicateKeyPath);
                }

                SSHConnection connectionInfo = new();
                foreach (var key in item.Keys)
                {
                    string paramName = key as string;
                    if (string.IsNullOrEmpty(paramName))
                    {
                        throw new PSArgumentException(RemotingErrorIdStrings.InvalidSSHConnectionParameter);
                    }

                    if (paramName.Equals(ComputerNameParameter, StringComparison.OrdinalIgnoreCase) || paramName.Equals(HostNameAlias, StringComparison.OrdinalIgnoreCase))
                    {
                        var resolvedComputerName = ResolveComputerName(GetSSHConnectionStringParameter(item[paramName]));
                        ParseSshHostName(resolvedComputerName, out string host, out string userName, out int port);
                        connectionInfo.ComputerName = host;
                        if (userName != string.Empty)
                        {
                            connectionInfo.UserName = userName;
                        }

                        if (port != -1)
                        {
                            connectionInfo.Port = port;
                        }
                    }
                    else if (paramName.Equals(UserNameParameter, StringComparison.OrdinalIgnoreCase))
                    {
                        connectionInfo.UserName = GetSSHConnectionStringParameter(item[paramName]);
                    }
                    else if (paramName.Equals(KeyFilePathParameter, StringComparison.OrdinalIgnoreCase) || paramName.Equals(IdentityFilePathAlias, StringComparison.OrdinalIgnoreCase))
                    {
                        connectionInfo.KeyFilePath = GetSSHConnectionStringParameter(item[paramName]);
                    }
                    else if (paramName.Equals(PortParameter, StringComparison.OrdinalIgnoreCase))
                    {
                        connectionInfo.Port = GetSSHConnectionIntParameter(item[paramName]);
                    }
                    else if (paramName.Equals(SubsystemParameter, StringComparison.OrdinalIgnoreCase))
                    {
                        connectionInfo.Subsystem = GetSSHConnectionStringParameter(item[paramName]);
                    }
                    else if (paramName.Equals(ConnectingTimeoutParameter, StringComparison.OrdinalIgnoreCase))
                    {
                        connectionInfo.ConnectingTimeout = GetSSHConnectionIntParameter(item[paramName]);
                    }
                    else if (paramName.Equals(OptionsParameter, StringComparison.OrdinalIgnoreCase))
                    {
                        connectionInfo.Options = item[paramName] as Hashtable;
                    }
                    else
                    {
                        throw new PSArgumentException(
                            StringUtil.Format(RemotingErrorIdStrings.UnknownSSHConnectionParameter, paramName));
                    }
                }

                if (string.IsNullOrEmpty(connectionInfo.ComputerName))
                {
                    throw new PSArgumentException(RemotingErrorIdStrings.MissingRequiredSSHParameter);
                }

                connections.Add(connectionInfo);
            }

            return connections.ToArray();
        }

        #endregion

        #region Private Methods

        
        /// <remarks>This function will lead in terminating errors when any of
        /// the validations fail</remarks>
        protected void ValidateRemoteRunspacesSpecified()
        {
            Dbg.Assert(Session != null && Session.Length != 0,
                    "Remote Runspaces specified must not be null or empty");

            // Check if there are duplicates in the specified PSSession objects
            if (RemotingCommandUtil.HasRepeatingRunspaces(Session))
            {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException(
                    GetMessage(RemotingErrorIdStrings.RemoteRunspaceInfoHasDuplicates)),
                        nameof(PSRemotingErrorId.RemoteRunspaceInfoHasDuplicates),
                            ErrorCategory.InvalidArgument, Session));
            }

            // BUGBUG: The following is a bogus check
            // Check if the number of PSSession objects specified is greater
            // than the maximum allowable range
            if (RemotingCommandUtil.ExceedMaximumAllowableRunspaces(Session))
            {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException(
                    GetMessage(RemotingErrorIdStrings.RemoteRunspaceInfoLimitExceeded)),
                        nameof(PSRemotingErrorId.RemoteRunspaceInfoLimitExceeded),
                            ErrorCategory.InvalidArgument, Session));
            }
        }

        
        /// <param name="connectionInfo"></param>
        internal void UpdateConnectionInfo(WSManConnectionInfo connectionInfo)
        {
            Dbg.Assert(connectionInfo != null, "connectionInfo cannot be null.");

            connectionInfo.SetSessionOptions(this.SessionOption);

            if (!ParameterSetName.Equals(PSRemotingBaseCmdlet.UriParameterSet, StringComparison.OrdinalIgnoreCase))
            {
                // uri redirection is supported only with URI parameter set
                connectionInfo.MaximumConnectionRedirectionCount = 0;
            }

            if (!_allowRedirection)
            {
                // uri redirection required explicit user consent
                connectionInfo.MaximumConnectionRedirectionCount = 0;
            }
        }

        
        protected const string UriParameterSet = "Uri";

        
        /// <param name="computerNames">collection of computer
        /// names to validate</param>
        protected void ValidateComputerName(string[] computerNames)
        {
            foreach (string computerName in computerNames)
            {
                UriHostNameType nametype = Uri.CheckHostName(computerName);
                if (!(nametype == UriHostNameType.Dns || nametype == UriHostNameType.IPv4 ||
                    nametype == UriHostNameType.IPv6))
                {
                    ThrowTerminatingError(new ErrorRecord(
                        new ArgumentException(PSRemotingErrorInvariants.FormatResourceString(
                            RemotingErrorIdStrings.InvalidComputerName)), "PSSessionInvalidComputerName",
                                ErrorCategory.InvalidArgument, computerNames));
                }
            }
        }

        
        /// <param name="param">Parameter value to be validated.</param>
        /// <returns>Parameter value as string.</returns>
        private static string GetSSHConnectionStringParameter(object param)
        {
            string paramValue;
            try
            {
                paramValue = LanguagePrimitives.ConvertTo<string>(param);
            }
            catch (PSInvalidCastException e)
            {
                throw new PSArgumentException(e.Message, e);
            }

            if (!string.IsNullOrEmpty(paramValue))
            {
                return paramValue;
            }

            throw new PSArgumentException(RemotingErrorIdStrings.InvalidSSHConnectionParameter);
        }

        
        /// <param name="param">Parameter value to be validated.</param>
        /// <returns>Parameter value as integer.</returns>
        private static int GetSSHConnectionIntParameter(object param)
        {
            if (param == null)
            {
                throw new PSArgumentException(RemotingErrorIdStrings.InvalidSSHConnectionParameter);
            }

            try
            {
                return LanguagePrimitives.ConvertTo<int>(param);
            }
            catch (PSInvalidCastException e)
            {
                throw new PSArgumentException(e.Message, e);
            }
        }

        #endregion Private Methods

        #region Overrides

        
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            // Validate KeyFilePath parameter.
            if ((ParameterSetName == PSRemotingBaseCmdlet.SSHHostParameterSet) &&
                (this.KeyFilePath != null))
            {
                // Resolve the key file path when set.
                this.KeyFilePath = PathResolver.ResolveProviderAndPath(this.KeyFilePath, true, this, false, RemotingErrorIdStrings.FilePathNotFromFileSystemProvider);
            }

            // Validate IdleTimeout parameter.
            int idleTimeout = (int)SessionOption.IdleTimeout.TotalMilliseconds;
            if (idleTimeout != BaseTransportManager.UseServerDefaultIdleTimeout &&
                idleTimeout < BaseTransportManager.MinimumIdleTimeout)
            {
                throw new PSArgumentException(
                    StringUtil.Format(RemotingErrorIdStrings.InvalidIdleTimeoutOption,
                    idleTimeout / 1000, BaseTransportManager.MinimumIdleTimeout / 1000));
            }

            if (string.IsNullOrEmpty(_appName))
            {
                _appName = ResolveAppName(null);
            }
        }

        #endregion Overrides
    }

    
    public abstract class PSExecutionCmdlet : PSRemotingBaseCmdlet
    {
        #region Strings

        
        protected const string FilePathVMIdParameterSet = "FilePathVMId";

        
        protected const string FilePathVMNameParameterSet = "FilePathVMName";

        
        protected const string FilePathContainerIdParameterSet = "FilePathContainerId";

        
        protected const string FilePathSSHHostParameterSet = "FilePathSSHHost";

        
        protected const string FilePathSSHHostHashParameterSet = "FilePathSSHHostHash";

        #endregion

        #region Parameters

        
        [Parameter(ValueFromPipeline = true)]
        public virtual PSObject InputObject { get; set; } = AutomationNull.Value;

        
        public virtual ScriptBlock ScriptBlock
        {
            get
            {
                return _scriptBlock;
            }

            set
            {
                _scriptBlock = value;
            }
        }

        private ScriptBlock _scriptBlock;

        
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = FilePathComputerNameParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = FilePathSessionParameterSet)]
        [Parameter(Position = 1,
                   Mandatory = true,
                   ParameterSetName = FilePathUriParameterSet)]
        [ValidateNotNull]
        public virtual string FilePath
        {
            get
            {
                return _filePath;
            }

            set
            {
                _filePath = value;
            }
        }

        private string _filePath;

        
        protected bool IsLiteralPath { get; set; }

        
        [Parameter()]
        [Alias("Args")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public virtual object[] ArgumentList
        {
            get
            {
                return _args;
            }

            set
            {
                _args = value;
            }
        }

        private object[] _args;

        
        protected bool InvokeAndDisconnect { get; set; } = false;

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        protected string[] DisconnectedSessionName { get; set; }

        
        public virtual SwitchParameter EnableNetworkAccess { get; set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.VMIdParameterSet)]
        [Parameter(Position = 0, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathVMIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("VMGuid")]
        public override Guid[] VMId { get; set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.VMNameParameterSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathVMNameParameterSet)]
        [ValidateNotNullOrEmpty]
        public override string[] VMName { get; set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathContainerIdParameterSet)]
        [ValidateNotNullOrEmpty]
        public override string[] ContainerId { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.ComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.UriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathComputerNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathUriParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.ContainerIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.VMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.VMNameParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathContainerIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathVMIdParameterSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = InvokeCommandCommand.FilePathVMNameParameterSet)]
        public virtual string ConfigurationName { get; set; }

        #endregion Parameters

        #region Private Methods

        
        protected virtual void CreateHelpersForSpecifiedComputerNames()
        {
            ValidateComputerName(ResolvedComputerNames);

            // create helper objects for computer names
            RemoteRunspace remoteRunspace = null;
            string scheme = UseSSL.IsPresent ? WSManConnectionInfo.HttpsScheme : WSManConnectionInfo.HttpScheme;

            for (int i = 0; i < ResolvedComputerNames.Length; i++)
            {
                try
                {
                    WSManConnectionInfo connectionInfo = new WSManConnectionInfo();
                    connectionInfo.Scheme = scheme;
                    connectionInfo.ComputerName = ResolvedComputerNames[i];
                    connectionInfo.Port = Port;
                    connectionInfo.AppName = ApplicationName;
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

                    connectionInfo.EnableNetworkAccess = EnableNetworkAccess;

                    // Use the provided session name or create one for this remote runspace so that
                    // it can be easily identified if it becomes disconnected and is queried on the server.
                    int rsId = PSSession.GenerateRunspaceId();
                    string rsName = (DisconnectedSessionName != null && DisconnectedSessionName.Length > i) ?
                        DisconnectedSessionName[i] : PSSession.GenerateRunspaceName(out rsId);

                    remoteRunspace = new RemoteRunspace(Utils.GetTypeTableFromExecutionContextTLS(), connectionInfo,
                        this.Host, this.SessionOption.ApplicationArguments, rsName, rsId);

                    remoteRunspace.Events.ReceivedEvents.PSEventReceived += OnRunspacePSEventReceived;
                }
                catch (UriFormatException uriException)
                {
                    ErrorRecord errorRecord = new ErrorRecord(uriException, "CreateRemoteRunspaceFailed",
                            ErrorCategory.InvalidArgument, ResolvedComputerNames[i]);

                    WriteError(errorRecord);

                    continue;
                }

                Pipeline pipeline = CreatePipeline(remoteRunspace);

                IThrottleOperation operation =
                    new ExecutionCmdletHelperComputerName(remoteRunspace, pipeline, InvokeAndDisconnect);

                Operations.Add(operation);
            }
        }

        
        protected void CreateHelpersForSpecifiedSSHComputerNames()
        {
            foreach (string computerName in ResolvedComputerNames)
            {
                ParseSshHostName(computerName, out string host, out string userName, out int port);

                var sshConnectionInfo = new SSHConnectionInfo(userName, host, KeyFilePath, port, Subsystem, ConnectingTimeout, Options);
                var typeTable = TypeTable.LoadDefaultTypeFiles();
                var remoteRunspace = RunspaceFactory.CreateRunspace(sshConnectionInfo, Host, typeTable) as RemoteRunspace;
                var pipeline = CreatePipeline(remoteRunspace);

                var operation = new ExecutionCmdletHelperComputerName(remoteRunspace, pipeline);
                Operations.Add(operation);
            }
        }

        
        protected void CreateHelpersForSpecifiedSSHHashComputerNames()
        {
            var sshConnections = ParseSSHConnectionHashTable();
            foreach (var sshConnection in sshConnections)
            {
                var sshConnectionInfo = new SSHConnectionInfo(
                    sshConnection.UserName,
                    sshConnection.ComputerName,
                    sshConnection.KeyFilePath,
                    sshConnection.Port,
                    sshConnection.Subsystem,
                    sshConnection.ConnectingTimeout);
                var typeTable = TypeTable.LoadDefaultTypeFiles();
                var remoteRunspace = RunspaceFactory.CreateRunspace(sshConnectionInfo, this.Host, typeTable) as RemoteRunspace;
                var pipeline = CreatePipeline(remoteRunspace);

                var operation = new ExecutionCmdletHelperComputerName(remoteRunspace, pipeline);
                Operations.Add(operation);
            }
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        protected void CreateHelpersForSpecifiedRunspaces()
        {
            RemoteRunspace[] remoteRunspaces;
            Pipeline[] pipelines;

            // extract RemoteRunspace out of the PSSession objects
            int length = Session.Length;
            remoteRunspaces = new RemoteRunspace[length];

            for (int i = 0; i < length; i++)
            {
                remoteRunspaces[i] = (RemoteRunspace)Session[i].Runspace;
            }

            // create the set of pipelines from the RemoteRunspace objects and
            // create IREHelperRunspace helper class to create operations
            pipelines = new Pipeline[length];

            for (int i = 0; i < length; i++)
            {
                pipelines[i] = CreatePipeline(remoteRunspaces[i]);

                // create the operation object
                IThrottleOperation operation = new ExecutionCmdletHelperRunspace(pipelines[i]);
                Operations.Add(operation);
            }
        }

        
        protected void CreateHelpersForSpecifiedUris()
        {
            // create helper objects for computer names
            RemoteRunspace remoteRunspace = null;
            for (int i = 0; i < ConnectionUri.Length; i++)
            {
                try
                {
                    WSManConnectionInfo connectionInfo = new WSManConnectionInfo();

                    connectionInfo.ConnectionUri = ConnectionUri[i];
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

                    connectionInfo.EnableNetworkAccess = EnableNetworkAccess;

                    remoteRunspace = (RemoteRunspace)RunspaceFactory.CreateRunspace(connectionInfo, this.Host,
                        Utils.GetTypeTableFromExecutionContextTLS(),
                        this.SessionOption.ApplicationArguments);

                    Dbg.Assert(remoteRunspace != null,
                            "RemoteRunspace object created using URI is null");

                    remoteRunspace.Events.ReceivedEvents.PSEventReceived += OnRunspacePSEventReceived;
                }
                catch (UriFormatException e)
                {
                    WriteErrorCreateRemoteRunspaceFailed(e, ConnectionUri[i]);
                    continue;
                }
                catch (InvalidOperationException e)
                {
                    WriteErrorCreateRemoteRunspaceFailed(e, ConnectionUri[i]);
                    continue;
                }
                catch (ArgumentException e)
                {
                    WriteErrorCreateRemoteRunspaceFailed(e, ConnectionUri[i]);
                    continue;
                }

                Pipeline pipeline = CreatePipeline(remoteRunspace);

                IThrottleOperation operation =
                    new ExecutionCmdletHelperComputerName(remoteRunspace, pipeline, InvokeAndDisconnect);

                Operations.Add(operation);
            }
        }

        
        protected virtual void CreateHelpersForSpecifiedVMSession()
        {
            int inputArraySize;
            int index;
            string command;
            bool[] vmIsRunning;
            Collection<PSObject> results;

            if ((ParameterSetName == PSExecutionCmdlet.VMIdParameterSet) ||
                (ParameterSetName == PSExecutionCmdlet.FilePathVMIdParameterSet))
            {
                inputArraySize = this.VMId.Length;
                this.VMName = new string[inputArraySize];
                vmIsRunning = new bool[inputArraySize];

                for (index = 0; index < inputArraySize; index++)
                {
                    vmIsRunning[index] = false;

                    command = "Get-VM -Id $args[0]";

                    try
                    {
                        results = this.InvokeCommand.InvokeScript(
                            command, false, PipelineResultTypes.None, null, this.VMId[index]);
                    }
                    catch (CommandNotFoundException)
                    {
                        ThrowTerminatingError(
                            new ErrorRecord(
                                new ArgumentException(RemotingErrorIdStrings.HyperVModuleNotAvailable),
                                nameof(PSRemotingErrorId.HyperVModuleNotAvailable),
                                ErrorCategory.NotInstalled,
                                null));

                        return;
                    }

                    if (results.Count != 1)
                    {
                        this.VMName[index] = string.Empty;
                    }
                    else
                    {
                        this.VMName[index] = (string)results[0].Properties["VMName"].Value;

                        if (GetVMStateProperty(results[0]) == VMState.Running)
                        {
                            vmIsRunning[index] = true;
                        }
                    }
                }
            }
            else
            {
                Dbg.Assert((ParameterSetName == PSExecutionCmdlet.VMNameParameterSet) ||
                           (ParameterSetName == PSExecutionCmdlet.FilePathVMNameParameterSet),
                           "Expected ParameterSetName == VMName or FilePathVMName");

                inputArraySize = this.VMName.Length;
                this.VMId = new Guid[inputArraySize];
                vmIsRunning = new bool[inputArraySize];

                for (index = 0; index < inputArraySize; index++)
                {
                    vmIsRunning[index] = false;

                    command = "Get-VM -Name $args";

                    try
                    {
                        results = this.InvokeCommand.InvokeScript(
                            command, false, PipelineResultTypes.None, null, this.VMName[index]);
                    }
                    catch (CommandNotFoundException)
                    {
                        ThrowTerminatingError(
                            new ErrorRecord(
                                new ArgumentException(RemotingErrorIdStrings.HyperVModuleNotAvailable),
                                nameof(PSRemotingErrorId.HyperVModuleNotAvailable),
                                ErrorCategory.NotInstalled,
                                null));

                        return;
                    }

                    if (results.Count != 1)
                    {
                        this.VMId[index] = Guid.Empty;
                    }
                    else
                    {
                        this.VMId[index] = (Guid)results[0].Properties["VMId"].Value;
                        this.VMName[index] = (string)results[0].Properties["VMName"].Value;

                        if (GetVMStateProperty(results[0]) == VMState.Running)
                        {
                            vmIsRunning[index] = true;
                        }
                    }
                }
            }

            ResolvedComputerNames = this.VMName;

            for (index = 0; index < ResolvedComputerNames.Length; index++)
            {
                if ((this.VMId[index] == Guid.Empty) &&
                    ((ParameterSetName == PSExecutionCmdlet.VMNameParameterSet) ||
                     (ParameterSetName == PSExecutionCmdlet.FilePathVMNameParameterSet)))
                {
                    WriteError(
                        new ErrorRecord(
                            new ArgumentException(GetMessage(RemotingErrorIdStrings.InvalidVMNameNotSingle,
                                                             this.VMName[index])),
                            nameof(PSRemotingErrorId.InvalidVMNameNotSingle),
                            ErrorCategory.InvalidArgument,
                            null));

                    continue;
                }
                else if ((this.VMName[index] == string.Empty) &&
                         ((ParameterSetName == PSExecutionCmdlet.VMIdParameterSet) ||
                          (ParameterSetName == PSExecutionCmdlet.FilePathVMIdParameterSet)))
                {
                    WriteError(
                        new ErrorRecord(
                            new ArgumentException(GetMessage(RemotingErrorIdStrings.InvalidVMIdNotSingle,
                                                             this.VMId[index].ToString(null))),
                            nameof(PSRemotingErrorId.InvalidVMIdNotSingle),
                            ErrorCategory.InvalidArgument,
                            null));

                    continue;
                }
                else if (!vmIsRunning[index])
                {
                    WriteError(
                        new ErrorRecord(
                            new ArgumentException(GetMessage(RemotingErrorIdStrings.InvalidVMState,
                                                             this.VMName[index])),
                            nameof(PSRemotingErrorId.InvalidVMState),
                            ErrorCategory.InvalidArgument,
                            null));

                    continue;
                }

                // create helper objects for VM GUIDs or names
                RemoteRunspace remoteRunspace = null;
                VMConnectionInfo connectionInfo;

                try
                {
                    connectionInfo = new VMConnectionInfo(this.Credential, this.VMId[index], this.VMName[index], this.ConfigurationName);

                    remoteRunspace = new RemoteRunspace(Utils.GetTypeTableFromExecutionContextTLS(),
                        connectionInfo, this.Host, null, null, -1);

                    remoteRunspace.Events.ReceivedEvents.PSEventReceived += OnRunspacePSEventReceived;
                }
                catch (InvalidOperationException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForVMFailed",
                        ErrorCategory.InvalidOperation,
                        null);

                    WriteError(errorRecord);
                }
                catch (ArgumentException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForVMFailed",
                        ErrorCategory.InvalidArgument,
                        null);

                    WriteError(errorRecord);
                }

                Pipeline pipeline = CreatePipeline(remoteRunspace);

                IThrottleOperation operation =
                    new ExecutionCmdletHelperComputerName(remoteRunspace, pipeline, false);

                Operations.Add(operation);
            }
        }

        
        protected virtual void CreateHelpersForSpecifiedContainerSession()
        {
            List<string> resolvedNameList = new List<string>();

            Dbg.Assert((ParameterSetName == PSExecutionCmdlet.ContainerIdParameterSet) ||
                       (ParameterSetName == PSExecutionCmdlet.FilePathContainerIdParameterSet),
                       "Expected ParameterSetName == ContainerId or FilePathContainerId");

            foreach (var input in ContainerId)
            {
                //
                // Create helper objects for container ID or name.
                //
                RemoteRunspace remoteRunspace = null;
                ContainerConnectionInfo connectionInfo = null;

                try
                {
                    //
                    // Hyper-V container uses Hype-V socket as transport.
                    // Windows Server container uses named pipe as transport.
                    //
                    connectionInfo = ContainerConnectionInfo.CreateContainerConnectionInfo(input, RunAsAdministrator.IsPresent, this.ConfigurationName);

                    resolvedNameList.Add(connectionInfo.ComputerName);

                    connectionInfo.CreateContainerProcess();

                    remoteRunspace = new RemoteRunspace(Utils.GetTypeTableFromExecutionContextTLS(),
                        connectionInfo, this.Host, null, null, -1);

                    remoteRunspace.Events.ReceivedEvents.PSEventReceived += OnRunspacePSEventReceived;
                }
                catch (InvalidOperationException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForContainerFailed",
                        ErrorCategory.InvalidOperation,
                        null);

                    WriteError(errorRecord);
                    continue;
                }
                catch (ArgumentException e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForContainerFailed",
                        ErrorCategory.InvalidArgument,
                        null);

                    WriteError(errorRecord);
                    continue;
                }
                catch (Exception e)
                {
                    ErrorRecord errorRecord = new ErrorRecord(e,
                        "CreateRemoteRunspaceForContainerFailed",
                        ErrorCategory.InvalidOperation,
                        null);

                    WriteError(errorRecord);
                    continue;
                }

                Pipeline pipeline = CreatePipeline(remoteRunspace);

                IThrottleOperation operation =
                    new ExecutionCmdletHelperComputerName(remoteRunspace, pipeline, false);

                Operations.Add(operation);
            }

            ResolvedComputerNames = resolvedNameList.ToArray();
        }

        
        /// <param name="remoteRunspace">Runspace on which to create the pipeline.</param>
        /// <returns>A pipeline.</returns>
        internal Pipeline CreatePipeline(RemoteRunspace remoteRunspace)
        {
            // The fix to WinBlue#475223 changed how UsingExpression is handled on the client/server sides, if the remote end is PSv5
            // or later, we send the dictionary-form using values to the remote end. If the remote end is PSv3 or PSv4, then we send
            // the array-form using values if all UsingExpressions are in the same scope, otherwise, we handle the UsingExpression as
            // if the remote end is PSv2.
            string serverPsVersion = GetRemoteServerPsVersion(remoteRunspace);
            System.Management.Automation.PowerShell powershellToUse = GetPowerShellForPSv3OrLater(serverPsVersion);
            Pipeline pipeline =
                remoteRunspace.CreatePipeline(powershellToUse.Commands.Commands[0].CommandText, true);

            pipeline.Commands.Clear();

            foreach (Command command in powershellToUse.Commands.Commands)
            {
                pipeline.Commands.Add(command);
            }

            pipeline.RedirectShellErrorOutputPipe = true;

            return pipeline;
        }

        
        private static string GetRemoteServerPsVersion(RemoteRunspace remoteRunspace)
        {
            if (remoteRunspace.ConnectionInfo is not WSManConnectionInfo)
            {
                // All transport types except for WSManConnectionInfo work with 5.1 or later.
                return PSv5OrLater;
            }

            PSPrimitiveDictionary psApplicationPrivateData = remoteRunspace.GetApplicationPrivateData();
            if (psApplicationPrivateData == null)
            {
                // The remote runspace is not opened yet, or it's disconnected before the private data is retrieved.
                // In this case we cannot validate if the remote server is running PSv5 or later, so for safety purpose,
                // we will handle the $using expressions as if the remote server is PSv3Orv4.
                return PSv3Orv4;
            }

            PSPrimitiveDictionary.TryPathGet(
                psApplicationPrivateData,
                out Version serverPsVersion,
                PSVersionInfo.PSVersionTableName,
                PSVersionInfo.PSVersionName);

            // PSv5 server will return 5.0 whereas older versions will always be 2.0. As we don't care about v2
            // anymore we can use a simple ternary check here to differenciate v5 using behaviour vs v3/4.
            return serverPsVersion != null && serverPsVersion.Major >= 5 ? PSv5OrLater : PSv3Orv4;
        }

        
        internal void OnRunspacePSEventReceived(object sender, PSEventArgs e) => this.Events?.AddForwardedEvent(e);

        #endregion Private Methods

        #region Protected Members / Methods

        
        internal List<IThrottleOperation> Operations { get; } = new List<IThrottleOperation>();

        
        protected void CloseAllInputStreams()
        {
            foreach (IThrottleOperation operation in Operations)
            {
                ExecutionCmdletHelper helper = (ExecutionCmdletHelper)operation;
                helper.Pipeline.Input.Close();
            }
        }

        
        /// <param name="e">exception which is causing this error record
        /// to be written</param>
        /// <param name="uri">Uri which caused this exception.</param>
        private void WriteErrorCreateRemoteRunspaceFailed(Exception e, Uri uri)
        {
            Dbg.Assert(e is UriFormatException || e is InvalidOperationException ||
                       e is ArgumentException,
                       "Exception has to be of type UriFormatException or InvalidOperationException or ArgumentException");

            ErrorRecord errorRecord = new ErrorRecord(e, "CreateRemoteRunspaceFailed",
                ErrorCategory.InvalidArgument, uri);

            WriteError(errorRecord);
        }

        
        protected const string FilePathComputerNameParameterSet = "FilePathComputerName";

        
        protected const string LiteralFilePathComputerNameParameterSet = "LiteralFilePathComputerName";

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        protected const string FilePathSessionParameterSet = "FilePathRunspace";

        
        protected const string FilePathUriParameterSet = "FilePathUri";

        
        private const string PSv5OrLater = "PSv5OrLater";
        private const string PSv3Orv4 = "PSv3Orv4";

        private System.Management.Automation.PowerShell _powershellV2;
        private System.Management.Automation.PowerShell _powershellV3;

        
        /// <param name="filePath"></param>
        /// <param name="isLiteralPath"></param>
        /// <returns></returns>
        protected ScriptBlock GetScriptBlockFromFile(string filePath, bool isLiteralPath)
        {
            // Make sure filepath doesn't contain wildcards
            if ((!isLiteralPath) && WildcardPattern.ContainsWildcardCharacters(filePath))
            {
                throw new ArgumentException(PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.WildCardErrorFilePathParameter), nameof(filePath));
            }

            if (!filePath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.FilePathShouldPS1Extension), nameof(filePath));
            }

            // Resolve file path
            string resolvedPath = PathResolver.ResolveProviderAndPath(filePath, isLiteralPath, this, false, RemotingErrorIdStrings.FilePathNotFromFileSystemProvider);

            // read content of file
            ExternalScriptInfo scriptInfo = new ExternalScriptInfo(filePath, resolvedPath, this.Context);

            // Skip ShouldRun check for .psd1 files.
            // Use ValidateScriptInfo() for explicitly validating the checkpolicy for psd1 file.
            //
            if (!filePath.EndsWith(".psd1", StringComparison.OrdinalIgnoreCase))
            {
                this.Context.AuthorizationManager.ShouldRunInternal(scriptInfo, CommandOrigin.Internal, this.Context.EngineHostInterface);
            }

            return scriptInfo.ScriptBlock;
        }

        #endregion Protected Members / Methods

        #region Overrides

        
        protected override void BeginProcessing()
        {
            if ((ParameterSetName == PSExecutionCmdlet.VMIdParameterSet) ||
                (ParameterSetName == PSExecutionCmdlet.VMNameParameterSet) ||
                (ParameterSetName == PSExecutionCmdlet.ContainerIdParameterSet) ||
                (ParameterSetName == PSExecutionCmdlet.FilePathVMIdParameterSet) ||
                (ParameterSetName == PSExecutionCmdlet.FilePathVMNameParameterSet) ||
                (ParameterSetName == PSExecutionCmdlet.FilePathContainerIdParameterSet))
            {
                SkipWinRMCheck = true;
            }

            base.BeginProcessing();

            if (_filePath != null)
            {
                _scriptBlock = GetScriptBlockFromFile(_filePath, IsLiteralPath);
            }

            switch (ParameterSetName)
            {
                case PSExecutionCmdlet.FilePathComputerNameParameterSet:
                case PSExecutionCmdlet.LiteralFilePathComputerNameParameterSet:
                case PSExecutionCmdlet.ComputerNameParameterSet:
                    {
                        string[] resolvedComputerNames = null;
                        ResolveComputerNames(ComputerName, out resolvedComputerNames);
                        ResolvedComputerNames = resolvedComputerNames;

                        CreateHelpersForSpecifiedComputerNames();
                    }

                    break;

                case PSExecutionCmdlet.SSHHostParameterSet:
                case PSExecutionCmdlet.FilePathSSHHostParameterSet:
                    {
                        string[] resolvedComputerNames = null;
                        ResolveComputerNames(HostName, out resolvedComputerNames);
                        ResolvedComputerNames = resolvedComputerNames;

                        CreateHelpersForSpecifiedSSHComputerNames();
                    }

                    break;

                case PSExecutionCmdlet.SSHHostHashParameterSet:
                case PSExecutionCmdlet.FilePathSSHHostHashParameterSet:
                    {
                        CreateHelpersForSpecifiedSSHHashComputerNames();
                    }

                    break;

                case PSExecutionCmdlet.FilePathSessionParameterSet:
                case PSExecutionCmdlet.SessionParameterSet:
                    {
                        ValidateRemoteRunspacesSpecified();

                        CreateHelpersForSpecifiedRunspaces();
                    }

                    break;

                case PSExecutionCmdlet.FilePathUriParameterSet:
                case PSExecutionCmdlet.UriParameterSet:
                    {
                        CreateHelpersForSpecifiedUris();
                    }

                    break;

                case PSExecutionCmdlet.VMIdParameterSet:
                case PSExecutionCmdlet.VMNameParameterSet:
                case PSExecutionCmdlet.FilePathVMIdParameterSet:
                case PSExecutionCmdlet.FilePathVMNameParameterSet:
                    {
                        CreateHelpersForSpecifiedVMSession();
                    }

                    break;

                case PSExecutionCmdlet.ContainerIdParameterSet:
                case PSExecutionCmdlet.FilePathContainerIdParameterSet:
                    {
                        CreateHelpersForSpecifiedContainerSession();
                    }

                    break;
            }
        }

        #endregion Overrides

        #region "Get PowerShell instance"

        
        /// <remarks>
        /// PSv2 doesn't understand the '$using' prefix. To make UsingExpression work on PSv2 remote end, we will have to
        /// alter the script, and send the altered script to the remote end. Since the script is altered, when there is an
        /// error, the error message will show the altered script, and that could be confusing to the user. So if the remote
        /// server is PSv3 or later version, we will use a different approach to handle UsingExpression so that we can keep
        /// the script unchanged.
        ///
        /// However, on PSv3 and PSv4 remote server, it's not well supported if UsingExpressions are used in different scopes (fixed in PSv5).
        /// If the remote end is PSv3 or PSv4, and there are UsingExpressions in different scopes, then we have to revert back to the approach
        /// used for PSv2 remote server.
        /// </remarks>
        /// <returns></returns>
        private System.Management.Automation.PowerShell GetPowerShellForPSv2()
        {
            if (_powershellV2 != null) { return _powershellV2; }

            // Try to convert the scriptblock to powershell commands.
            _powershellV2 = ConvertToPowerShell();
            if (_powershellV2 != null)
            {
                // Look for EndOfStatement tokens.
                foreach (var command in _powershellV2.Commands.Commands)
                {
                    if (command.IsEndOfStatement)
                    {
                        // PSv2 cannot process this.  Revert to sending script.
                        _powershellV2 = null;
                        break;
                    }
                }

                if (_powershellV2 != null) { return _powershellV2; }
            }

            List<string> newParameterNames;
            List<object> newParameterValues;

            string scriptTextAdaptedForPSv2 = GetConvertedScript(out newParameterNames, out newParameterValues);
            _powershellV2 = System.Management.Automation.PowerShell.Create().AddScript(scriptTextAdaptedForPSv2);

            if (_args != null)
            {
                foreach (object arg in _args)
                {
                    _powershellV2.AddArgument(arg);
                }
            }

            if (newParameterNames != null)
            {
                Dbg.Assert(newParameterValues != null && newParameterNames.Count == newParameterValues.Count, "We should get the value for each using variable");
                for (int i = 0; i < newParameterNames.Count; i++)
                {
                    _powershellV2.AddParameter(newParameterNames[i], newParameterValues[i]);
                }
            }

            return _powershellV2;
        }

        
        /// <remarks>
        /// In PSv3 and PSv4, if the remote server is PSv3 or later, we generate an object array that contains the value of each using expression in
        /// the parsing order, and then pass the array to the remote end as a special argument. On the remote end, the using expressions will be indexed
        /// in the same parsing order during the variable analysis process, and the index is used to get the value of the corresponding using expression
        /// from the special array. There is a limitation in that approach -- $using cannot be used in different scopes with Invoke-Command/Start-Job
        /// (see WinBlue#475223), because the variable analysis process can only index using expressions within the same scope (this is by design), and a
        /// using expression from a different scope may be assigned with an index that conflicts with other using expressions.
        ///
        /// To fix the limitation described above, we changed to pass a dictionary with key/value pairs for the using expressions on the client side. The key
        /// is an unique base64 encoded string generated based on the text of the using expression. On the remote end, it can always get the unique key of a
        /// using expression because the text passed to the server side is the same, and thus the value of the using expression can be retrieved from the special
        /// dictionary. With this approach, $using in different scopes can be supported for Invoke-Command/Start-Job.
        ///
        /// This fix involved changes on the server side, so the fix will work only if the remote end is PSv5 or later. In order to avoid possible breaking
        /// change in 'PSv5 client - PSv3 server' and 'PSv5 client - PSv4 server' scenarios, we should keep sending the array-form using values if the remote
        /// end is PSv3 or PSv4 as long as no UsingExpression is in a different scope. If the remote end is PSv3 or PSv4 and we do have UsingExpressions
        /// in different scopes, then we will revert back to the approach we use to handle UsingExpression for PSv2 remote server.
        /// </remarks>
        /// <returns></returns>
        private System.Management.Automation.PowerShell GetPowerShellForPSv3OrLater(string serverPsVersion)
        {
            if (_powershellV3 != null) { return _powershellV3; }

            // Try to convert the scriptblock to powershell commands.
            _powershellV3 = ConvertToPowerShell();

            if (_powershellV3 != null) { return _powershellV3; }

            // Using expressions can be a variable, as well as property and / or array references. E.g.
            //
            // icm { echo $using:a }
            // icm { echo $using:a[3] }
            // icm { echo $using:a.Length }
            //
            // Semantic checks on the using statement have already validated that there are no arbitrary expressions,
            // so we'll allow these expressions in everything but NoLanguage mode.

            bool allowUsingExpressions = Context.SessionState.LanguageMode != PSLanguageMode.NoLanguage;
            object[] usingValuesInArray = null;
            IDictionary usingValuesInDict = null;

            // Value of 'serverPsVersion' should be either 'PSv3Orv4' or 'PSv5OrLater'
            if (serverPsVersion == PSv3Orv4)
            {
                usingValuesInArray = ScriptBlockToPowerShellConverter.GetUsingValuesAsArray(_scriptBlock, allowUsingExpressions, Context, null);
                if (usingValuesInArray == null)
                {
                    // 'usingValuesInArray' will be null only if there are UsingExpressions used in different scopes.
                    // PSv3 and PSv4 remote server cannot handle this, so we revert back to the approach we use for PSv2 remote end.
                    return GetPowerShellForPSv2();
                }
            }
            else
            {
                // Remote server is PSv5 or later version
                usingValuesInDict = ScriptBlockToPowerShellConverter.GetUsingValuesAsDictionary(_scriptBlock, allowUsingExpressions, Context, null);
            }

            string textOfScriptBlock = this.MyInvocation.ExpectingInput
                ? _scriptBlock.GetWithInputHandlingForInvokeCommand()
                : _scriptBlock.ToString();

            _powershellV3 = System.Management.Automation.PowerShell.Create().AddScript(textOfScriptBlock);

            if (_args != null)
            {
                foreach (object arg in _args)
                {
                    _powershellV3.AddArgument(arg);
                }
            }

            if (usingValuesInDict != null && usingValuesInDict.Count > 0)
            {
                _powershellV3.AddParameter(Parser.VERBATIM_ARGUMENT, usingValuesInDict);
            }
            else if (usingValuesInArray != null && usingValuesInArray.Length > 0)
            {
                _powershellV3.AddParameter(Parser.VERBATIM_ARGUMENT, usingValuesInArray);
            }

            return _powershellV3;
        }

        private System.Management.Automation.PowerShell ConvertToPowerShell()
        {
            System.Management.Automation.PowerShell powershell = null;

            try
            {
                // This is trusted input as long as we're in FullLanguage mode
                bool isTrustedInput = Context.LanguageMode == PSLanguageMode.FullLanguage;
                powershell = _scriptBlock.GetPowerShell(isTrustedInput, _args);
            }
            catch (ScriptBlockToPowerShellNotSupportedException)
            {
                // conversion failed, we need to send the script to the remote end.
                // since the PowerShell instance would be different according to the PSVersion of the remote end,
                // we generate it when we know which version we are talking to.
            }

            return powershell;
        }

        #endregion "Get PowerShell instance"

        #region "UsingExpression Utilities"

        
        /// <param name="newParameterNames">
        /// The new parameter names that we added to the param block
        /// </param>
        /// <param name="newParameterValues">
        /// The new parameter values that need to be added to the powershell instance
        /// </param>
        /// <returns></returns>
        private string GetConvertedScript(out List<string> newParameterNames, out List<object> newParameterValues)
        {
            newParameterNames = null; newParameterValues = null;
            string textOfScriptBlock = null;

            // Scan for the using variables
            List<VariableExpressionAst> usingVariables = GetUsingVariables(_scriptBlock);

            if (usingVariables == null || usingVariables.Count == 0)
            {
                // No using variable is used, then we don't change the script
                textOfScriptBlock = this.MyInvocation.ExpectingInput
                    ? _scriptBlock.GetWithInputHandlingForInvokeCommand()
                    : _scriptBlock.ToString();
            }
            else
            {
                newParameterNames = new List<string>();
                var paramNamesWithDollarSign = new List<string>();
                var paramUsingVars = new List<VariableExpressionAst>();
                var nameHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var varAst in usingVariables)
                {
                    VariablePath varPath = varAst.VariablePath;
                    string varName = varPath.IsDriveQualified ? $"{varPath.DriveName}_{varPath.UnqualifiedPath}" : $"{varPath.UnqualifiedPath}";
                    string paramName = UsingExpressionAst.UsingPrefix + varName;
                    string paramNameWithDollar = "$" + paramName;

                    if (!nameHashSet.Contains(paramNameWithDollar))
                    {
                        newParameterNames.Add(paramName);
                        paramNamesWithDollarSign.Add(paramNameWithDollar);
                        paramUsingVars.Add(varAst);
                        nameHashSet.Add(paramNameWithDollar);
                    }
                }

                // Retrieve the value for each using variable
                newParameterValues = GetUsingVariableValues(paramUsingVars);

                // Generate the wrapped script
                string additionalNewParams = string.Join(", ", paramNamesWithDollarSign);
                textOfScriptBlock = this.MyInvocation.ExpectingInput
                    ? _scriptBlock.GetWithInputHandlingForInvokeCommandWithUsingExpression(Tuple.Create(usingVariables, additionalNewParams))
                    : _scriptBlock.ToStringWithDollarUsingHandling(Tuple.Create(usingVariables, additionalNewParams));
            }

            return textOfScriptBlock;
        }

        
        /// <param name="paramUsingVars"></param>
        /// <returns></returns>
        private List<object> GetUsingVariableValues(List<VariableExpressionAst> paramUsingVars)
        {
            var values = new List<object>(paramUsingVars.Count);
            VariableExpressionAst currentVarAst = null;
            Version oldStrictVersion = Context.EngineSessionState.CurrentScope.StrictModeVersion;

            try
            {
                Context.EngineSessionState.CurrentScope.StrictModeVersion = PSVersionInfo.PSVersion;

                // GetExpressionValue ensures that it only does variable access when supplied a VariableExpressionAst.
                // So, this is still safe to use in ConstrainedLanguage and will not result in arbitrary code
                // execution.
                bool allowVariableAccess = Context.SessionState.LanguageMode != PSLanguageMode.NoLanguage;

                foreach (var varAst in paramUsingVars)
                {
                    currentVarAst = varAst;
                    object value = Compiler.GetExpressionValue(varAst, allowVariableAccess, Context);
                    values.Add(value);
                }
            }
            catch (RuntimeException rte)
            {
                if (rte.ErrorRecord.FullyQualifiedErrorId.Equals("VariableIsUndefined", StringComparison.Ordinal))
                {
                    throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                        currentVarAst.Extent, "UsingVariableIsUndefined", AutomationExceptions.UsingVariableIsUndefined, rte.ErrorRecord.TargetObject);
                }
            }
            finally
            {
                Context.EngineSessionState.CurrentScope.StrictModeVersion = oldStrictVersion;
            }

            return values;
        }

        
        /// <param name="localScriptBlock"></param>
        /// <returns>A list of UsingExpressionAsts ordered by the StartOffset.</returns>
        private static List<VariableExpressionAst> GetUsingVariables(ScriptBlock localScriptBlock)
        {
            ArgumentNullException.ThrowIfNull(localScriptBlock, "Caller needs to make sure the parameter value is not null");

            var allUsingExprs = UsingExpressionAstSearcher.FindAllUsingExpressions(localScriptBlock.Ast);
            return allUsingExprs.Select(static usingExpr => UsingExpressionAst.ExtractUsingVariable((UsingExpressionAst)usingExpr)).ToList();
        }

        #endregion "UsingExpression Utilities"
    }

    
    public abstract class PSRunspaceCmdlet : PSRemotingCmdlet
    {
        #region Parameters

        
        protected const string ContainerIdInstanceIdParameterSet = "ContainerIdInstanceId";

        
        protected const string VMIdInstanceIdParameterSet = "VMIdInstanceId";

        
        protected const string VMNameInstanceIdParameterSet = "VMNameInstanceId";

        
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.InstanceIdParameterSet)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public virtual Guid[] InstanceId
        {
            get
            {
                return _remoteRunspaceIds;
            }

            set
            {
                _remoteRunspaceIds = value;
            }
        }

        private Guid[] _remoteRunspaceIds;

        
        [Parameter(Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   Mandatory = true,
                   ParameterSetName = PSRunspaceCmdlet.IdParameterSet)]
        [ValidateNotNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] Id { get; set; }

        
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.NameParameterSet)]
        [ValidateNotNullOrEmpty()]
        public virtual string[] Name
        {
            get
            {
                return _names;
            }

            set
            {
                _names = value;
            }
        }

        private string[] _names;

        
        [Parameter(Position = 0,
                   Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.ComputerNameParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("Cn")]
        public virtual string[] ComputerName
        {
            get
            {
                return _computerNames;
            }

            set
            {
                _computerNames = value;
            }
        }

        private string[] _computerNames;

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.ContainerIdParameterSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.ContainerIdInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        public virtual string[] ContainerId { get; set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.VMIdParameterSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.VMIdInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        [Alias("VMGuid")]
        public virtual Guid[] VMId { get; set; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Justification = "This is by spec.")]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.VMNameParameterSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = PSRunspaceCmdlet.VMNameInstanceIdParameterSet)]
        [ValidateNotNullOrEmpty]
        public virtual string[] VMName { get; set; }

        #endregion Parameters

        #region Private / Protected Methods

        
        /// <param name="writeErrorOnNoMatch">write an error record when
        /// no matches are found</param>
        /// <param name="writeobject">if true write the object down
        /// the pipeline</param>
        /// <returns>List of matching runspaces.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        protected Dictionary<Guid, PSSession> GetMatchingRunspaces(bool writeobject,
            bool writeErrorOnNoMatch)
        {
            return GetMatchingRunspaces(writeobject, writeErrorOnNoMatch, SessionFilterState.All, null);
        }

        
        /// <param name="writeErrorOnNoMatch">write an error record when
        /// no matches are found</param>
        /// <param name="writeobject">if true write the object down
        /// the pipeline</param>
        /// <param name="filterState">Runspace state filter value.</param>
        /// <param name="configurationName">Runspace configuration name filter value.</param>
        /// <returns>List of matching runspaces.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        protected Dictionary<Guid, PSSession> GetMatchingRunspaces(bool writeobject,
            bool writeErrorOnNoMatch,
            SessionFilterState filterState,
            string configurationName)
        {
            switch (ParameterSetName)
            {
                case PSRunspaceCmdlet.ComputerNameParameterSet:
                    {
                        return GetMatchingRunspacesByComputerName(writeobject, writeErrorOnNoMatch);
                    }

                case PSRunspaceCmdlet.InstanceIdParameterSet:
                    {
                        return GetMatchingRunspacesByRunspaceId(writeobject, writeErrorOnNoMatch);
                    }

                case PSRunspaceCmdlet.NameParameterSet:
                    {
                        return GetMatchingRunspacesByName(writeobject, writeErrorOnNoMatch);
                    }

                case PSRunspaceCmdlet.IdParameterSet:
                    {
                        return GetMatchingRunspacesBySessionId(writeobject, writeErrorOnNoMatch);
                    }

                //
                // writeErrorOnNoMatch should always be false for container/vm id/name inputs
                // in Get-PSSession/Remove-PSSession cmdlets
                //

                // container id + optional session name
                case PSRunspaceCmdlet.ContainerIdParameterSet:
                    {
                        return GetMatchingRunspacesByVMNameContainerId(writeobject, filterState, configurationName, true);
                    }

                // container id + session instanceid
                case PSRunspaceCmdlet.ContainerIdInstanceIdParameterSet:
                    {
                        return GetMatchingRunspacesByVMNameContainerIdSessionInstanceId(writeobject, filterState, configurationName, true);
                    }

                // vm Guid + optional session name
                case PSRunspaceCmdlet.VMIdParameterSet:
                    {
                        return GetMatchingRunspacesByVMId(writeobject, filterState, configurationName);
                    }

                // vm Guid + session instanceid
                case PSRunspaceCmdlet.VMIdInstanceIdParameterSet:
                    {
                        return GetMatchingRunspacesByVMIdSessionInstanceId(writeobject, filterState, configurationName);
                    }

                // vm name + optional session name
                case PSRunspaceCmdlet.VMNameParameterSet:
                    {
                        return GetMatchingRunspacesByVMNameContainerId(writeobject, filterState, configurationName, false);
                    }

                // vm name + session instanceid
                case PSRunspaceCmdlet.VMNameInstanceIdParameterSet:
                    {
                        return GetMatchingRunspacesByVMNameContainerIdSessionInstanceId(writeobject, filterState, configurationName, false);
                    }
            }

            return null;
        }

        internal Dictionary<Guid, PSSession> GetAllRunspaces(bool writeobject,
            bool writeErrorOnNoMatch)
        {
            Dictionary<Guid, PSSession> matches = new Dictionary<Guid, PSSession>();
            List<PSSession> remoteRunspaceInfos = this.RunspaceRepository.Runspaces;
            foreach (PSSession remoteRunspaceInfo in remoteRunspaceInfos)
            {
                // return all remote runspace info objects
                if (writeobject)
                {
                    WriteObject(remoteRunspaceInfo);
                }
                else
                {
                    matches.Add(remoteRunspaceInfo.InstanceId, remoteRunspaceInfo);
                }
            }

            return matches;
        }

        
        /// <param name="writeErrorOnNoMatch">write an error record when
        /// no matches are found</param>
        /// <param name="writeobject">if true write the object down
        /// the pipeline</param>
        /// <returns>List of matching runspaces.</returns>
        private Dictionary<Guid, PSSession> GetMatchingRunspacesByComputerName(bool writeobject,
            bool writeErrorOnNoMatch)
        {
            if (_computerNames == null || _computerNames.Length == 0)
            {
                return GetAllRunspaces(writeobject, writeErrorOnNoMatch);
            }

            Dictionary<Guid, PSSession> matches = new Dictionary<Guid, PSSession>();

            List<PSSession> remoteRunspaceInfos = this.RunspaceRepository.Runspaces;

            // Loop through all computer-name patterns and runspaces to find matches.
            foreach (string computerName in _computerNames)
            {
                WildcardPattern computerNamePattern = WildcardPattern.Get(computerName, WildcardOptions.IgnoreCase);

                // Match the computer-name patterns against all the runspaces and remember the matches.
                bool found = false;
                foreach (PSSession remoteRunspaceInfo in remoteRunspaceInfos)
                {
                    if (computerNamePattern.IsMatch(remoteRunspaceInfo.ComputerName))
                    {
                        found = true;

                        if (writeobject)
                        {
                            WriteObject(remoteRunspaceInfo);
                        }
                        else
                        {
                            try
                            {
                                matches.Add(remoteRunspaceInfo.InstanceId, remoteRunspaceInfo);
                            }
                            catch (ArgumentException)
                            {
                                // if match already found ignore
                            }
                        }
                    }
                }

                // If no match found write an error record.
                if (!found && writeErrorOnNoMatch)
                {
                    WriteInvalidArgumentError(PSRemotingErrorId.RemoteRunspaceNotAvailableForSpecifiedComputer, RemotingErrorIdStrings.RemoteRunspaceNotAvailableForSpecifiedComputer,
                        computerName);
                }
            }

            return matches;
        }

        
        /// <param name="writeErrorOnNoMatch">write an error record when
        /// no matches are found</param>
        /// <param name="writeobject">if true write the object down
        /// the pipeline</param>
        /// <returns>List of matching runspaces.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "writeobject")]
        protected Dictionary<Guid, PSSession> GetMatchingRunspacesByName(bool writeobject,
            bool writeErrorOnNoMatch)
        {
            Dictionary<Guid, PSSession> matches = new Dictionary<Guid, PSSession>();

            List<PSSession> remoteRunspaceInfos = this.RunspaceRepository.Runspaces;

            // Loop through all computer-name patterns and runspaces to find matches.
            foreach (string name in _names)
            {
                WildcardPattern namePattern = WildcardPattern.Get(name, WildcardOptions.IgnoreCase);

                // Match the computer-name patterns against all the runspaces and remember the matches.
                bool found = false;
                foreach (PSSession remoteRunspaceInfo in remoteRunspaceInfos)
                {
                    if (namePattern.IsMatch(remoteRunspaceInfo.Name))
                    {
                        found = true;

                        if (writeobject)
                        {
                            WriteObject(remoteRunspaceInfo);
                        }
                        else
                        {
                            try
                            {
                                matches.Add(remoteRunspaceInfo.InstanceId, remoteRunspaceInfo);
                            }
                            catch (ArgumentException)
                            {
                                // if match already found ignore
                            }
                        }
                    }
                }

                // If no match found write an error record.
                if (!found && writeErrorOnNoMatch && !WildcardPattern.ContainsWildcardCharacters(name))
                {
                    WriteInvalidArgumentError(PSRemotingErrorId.RemoteRunspaceNotAvailableForSpecifiedName, RemotingErrorIdStrings.RemoteRunspaceNotAvailableForSpecifiedName,
                        name);
                }
            }

            return matches;
        }

        
        /// <param name="writeErrorOnNoMatch">write an error record when
        /// no matches are found</param>
        /// <param name="writeobject">if true write the object down
        /// the pipeline</param>
        /// <returns>List of matching runspaces.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "writeobject")]
        protected Dictionary<Guid, PSSession> GetMatchingRunspacesByRunspaceId(bool writeobject,
            bool writeErrorOnNoMatch)
        {
            Dictionary<Guid, PSSession> matches = new Dictionary<Guid, PSSession>();

            List<PSSession> remoteRunspaceInfos = this.RunspaceRepository.Runspaces;

            // Loop through all computer-name patterns and runspaces to find matches.
            foreach (Guid remoteRunspaceId in _remoteRunspaceIds)
            {
                // Match the computer-name patterns against all the runspaces and remember the matches.
                bool found = false;
                foreach (PSSession remoteRunspaceInfo in remoteRunspaceInfos)
                {
                    if (remoteRunspaceId.Equals(remoteRunspaceInfo.InstanceId))
                    {
                        found = true;

                        if (writeobject)
                        {
                            WriteObject(remoteRunspaceInfo);
                        }
                        else
                        {
                            try
                            {
                                matches.Add(remoteRunspaceInfo.InstanceId, remoteRunspaceInfo);
                            }
                            catch (ArgumentException)
                            {
                                // if match already found ignore
                            }
                        }
                    }
                }

                // If no match found write an error record.
                if (!found && writeErrorOnNoMatch)
                {
                    WriteInvalidArgumentError(PSRemotingErrorId.RemoteRunspaceNotAvailableForSpecifiedRunspaceId, RemotingErrorIdStrings.RemoteRunspaceNotAvailableForSpecifiedRunspaceId,
                        remoteRunspaceId);
                }
            }

            return matches;
        }

        
        /// <param name="writeErrorOnNoMatch">write an error record when
        /// no matches are found</param>
        /// <param name="writeobject">if true write the object down
        /// the pipeline</param>
        /// <returns>List of matching runspaces.</returns>
        private Dictionary<Guid, PSSession> GetMatchingRunspacesBySessionId(bool writeobject,
            bool writeErrorOnNoMatch)
        {
            Dictionary<Guid, PSSession> matches = new Dictionary<Guid, PSSession>();

            List<PSSession> remoteRunspaceInfos = this.RunspaceRepository.Runspaces;

            // Loop through all computer-name patterns and runspaces to find matches.
            foreach (int sessionId in Id)
            {
                // Match the computer-name patterns against all the runspaces and remember the matches.
                bool found = false;
                foreach (PSSession remoteRunspaceInfo in remoteRunspaceInfos)
                {
                    if (sessionId == remoteRunspaceInfo.Id)
                    {
                        found = true;

                        if (writeobject)
                        {
                            WriteObject(remoteRunspaceInfo);
                        }
                        else
                        {
                            try
                            {
                                matches.Add(remoteRunspaceInfo.InstanceId, remoteRunspaceInfo);
                            }
                            catch (ArgumentException)
                            {
                                // if match already found ignore
                            }
                        }
                    }
                }

                // If no match found write an error record.
                if (!found && writeErrorOnNoMatch)
                {
                    WriteInvalidArgumentError(PSRemotingErrorId.RemoteRunspaceNotAvailableForSpecifiedSessionId, RemotingErrorIdStrings.RemoteRunspaceNotAvailableForSpecifiedSessionId,
                        sessionId);
                }
            }

            return matches;
        }

        
        /// <param name="writeobject">If true write the object down the pipeline.</param>
        /// <param name="filterState">Runspace state filter value.</param>
        /// <param name="configurationName">Runspace configuration name filter value.</param>
        /// <param name="isContainer">If true the target is a container instead of virtual machine.</param>
        /// <returns>List of matching runspaces.</returns>
        private Dictionary<Guid, PSSession> GetMatchingRunspacesByVMNameContainerId(bool writeobject,
            SessionFilterState filterState,
            string configurationName,
            bool isContainer)
        {
            string[] inputNames;
            TargetMachineType computerType;
            bool supportWildChar;
            string[] sessionNames = { "*" };
            WildcardPattern configurationNamePattern =
                string.IsNullOrEmpty(configurationName) ? null : WildcardPattern.Get(configurationName, WildcardOptions.IgnoreCase);
            Dictionary<Guid, PSSession> matches = new Dictionary<Guid, PSSession>();
            List<PSSession> remoteRunspaceInfos = this.RunspaceRepository.Runspaces;

            // vm name support wild characters, while container id does not.
            // vm id does not apply in this method, which does not support wild characters either.
            if (isContainer)
            {
                inputNames = ContainerId;
                computerType = TargetMachineType.Container;
                supportWildChar = false;
            }
            else
            {
                inputNames = VMName;
                computerType = TargetMachineType.VirtualMachine;
                supportWildChar = true;
            }

            // When "-name" is not set, we use "*" that means matching all.
            if (Name != null)
            {
                sessionNames = Name;
            }

            foreach (string inputName in inputNames)
            {
                WildcardPattern inputNamePattern = WildcardPattern.Get(inputName, WildcardOptions.IgnoreCase);

                foreach (string sessionName in sessionNames)
                {
                    WildcardPattern sessionNamePattern =
                        string.IsNullOrEmpty(sessionName) ? null : WildcardPattern.Get(sessionName, WildcardOptions.IgnoreCase);

                    var matchingRunspaceInfos = remoteRunspaceInfos
                        .Where<PSSession>(session => (supportWildChar ? inputNamePattern.IsMatch(session.VMName)
                                                                      : inputName.Equals(session.ContainerId)) &&
                                                     (sessionNamePattern == null || sessionNamePattern.IsMatch(session.Name)) &&
                                                     QueryRunspaces.TestRunspaceState(session.Runspace, filterState) &&
                                                     (configurationNamePattern == null || configurationNamePattern.IsMatch(session.ConfigurationName)) &&
                                                     (session.ComputerType == computerType))
                        .ToList<PSSession>();

                    WriteOrAddMatches(matchingRunspaceInfos, writeobject, ref matches);
                }
            }

            return matches;
        }

        
        /// <param name="writeobject">If true write the object down the pipeline.</param>
        /// <param name="filterState">Runspace state filter value.</param>
        /// <param name="configurationName">Runspace configuration name filter value.</param>
        /// <param name="isContainer">If true the target is a container instead of virtual machine.</param>
        /// <returns>List of matching runspaces.</returns>
        private Dictionary<Guid, PSSession> GetMatchingRunspacesByVMNameContainerIdSessionInstanceId(bool writeobject,
            SessionFilterState filterState,
            string configurationName,
            bool isContainer)
        {
            string[] inputNames;
            TargetMachineType computerType;
            bool supportWildChar;
            WildcardPattern configurationNamePattern =
                string.IsNullOrEmpty(configurationName) ? null : WildcardPattern.Get(configurationName, WildcardOptions.IgnoreCase);
            Dictionary<Guid, PSSession> matches = new Dictionary<Guid, PSSession>();
            List<PSSession> remoteRunspaceInfos = this.RunspaceRepository.Runspaces;

            // vm name support wild characters, while container id does not.
            // vm id does not apply in this method, which does not support wild characters either.
            if (isContainer)
            {
                inputNames = ContainerId;
                computerType = TargetMachineType.Container;
                supportWildChar = false;
            }
            else
            {
                inputNames = VMName;
                computerType = TargetMachineType.VirtualMachine;
                supportWildChar = true;
            }

            foreach (string inputName in inputNames)
            {
                WildcardPattern inputNamePattern = WildcardPattern.Get(inputName, WildcardOptions.IgnoreCase);

                foreach (Guid sessionInstanceId in InstanceId)
                {
                    var matchingRunspaceInfos = remoteRunspaceInfos
                        .Where<PSSession>(session => (supportWildChar ? inputNamePattern.IsMatch(session.VMName)
                                                                      : inputName.Equals(session.ContainerId)) &&
                                                     sessionInstanceId.Equals(session.InstanceId) &&
                                                     QueryRunspaces.TestRunspaceState(session.Runspace, filterState) &&
                                                     (configurationNamePattern == null || configurationNamePattern.IsMatch(session.ConfigurationName)) &&
                                                     (session.ComputerType == computerType))
                        .ToList<PSSession>();

                    WriteOrAddMatches(matchingRunspaceInfos, writeobject, ref matches);
                }
            }

            return matches;
        }

        
        /// <param name="writeobject">If true write the object down the pipeline.</param>
        /// <param name="filterState">Runspace state filter value.</param>
        /// <param name="configurationName">Runspace configuration name filter value.</param>
        /// <returns>List of matching runspaces.</returns>
        private Dictionary<Guid, PSSession> GetMatchingRunspacesByVMId(bool writeobject,
            SessionFilterState filterState,
            string configurationName)
        {
            string[] sessionNames = { "*" };
            WildcardPattern configurationNamePattern =
                string.IsNullOrEmpty(configurationName) ? null : WildcardPattern.Get(configurationName, WildcardOptions.IgnoreCase);
            Dictionary<Guid, PSSession> matches = new Dictionary<Guid, PSSession>();
            List<PSSession> remoteRunspaceInfos = this.RunspaceRepository.Runspaces;

            // When "-name" is not set, we use "*" that means matching all .
            if (Name != null)
            {
                sessionNames = Name;
            }

            foreach (Guid vmId in VMId)
            {
                foreach (string sessionName in sessionNames)
                {
                    WildcardPattern sessionNamePattern =
                        string.IsNullOrEmpty(sessionName) ? null : WildcardPattern.Get(sessionName, WildcardOptions.IgnoreCase);

                    var matchingRunspaceInfos = remoteRunspaceInfos
                        .Where<PSSession>(session => vmId.Equals(session.VMId) &&
                                                     (sessionNamePattern == null || sessionNamePattern.IsMatch(session.Name)) &&
                                                     QueryRunspaces.TestRunspaceState(session.Runspace, filterState) &&
                                                     (configurationNamePattern == null || configurationNamePattern.IsMatch(session.ConfigurationName)) &&
                                                     (session.ComputerType == TargetMachineType.VirtualMachine))
                        .ToList<PSSession>();

                    WriteOrAddMatches(matchingRunspaceInfos, writeobject, ref matches);
                }
            }

            return matches;
        }

        
        /// <param name="writeobject">If true write the object down the pipeline.</param>
        /// <param name="filterState">Runspace state filter value.</param>
        /// <param name="configurationName">Runspace configuration name filter value.</param>
        /// <returns>List of matching runspaces.</returns>
        private Dictionary<Guid, PSSession> GetMatchingRunspacesByVMIdSessionInstanceId(bool writeobject,
            SessionFilterState filterState,
            string configurationName)
        {
            WildcardPattern configurationNamePattern =
                string.IsNullOrEmpty(configurationName) ? null : WildcardPattern.Get(configurationName, WildcardOptions.IgnoreCase);
            Dictionary<Guid, PSSession> matches = new Dictionary<Guid, PSSession>();
            List<PSSession> remoteRunspaceInfos = this.RunspaceRepository.Runspaces;

            foreach (Guid vmId in VMId)
            {
                foreach (Guid sessionInstanceId in InstanceId)
                {
                    var matchingRunspaceInfos = remoteRunspaceInfos
                        .Where<PSSession>(session => vmId.Equals(session.VMId) &&
                                                     sessionInstanceId.Equals(session.InstanceId) &&
                                                     QueryRunspaces.TestRunspaceState(session.Runspace, filterState) &&
                                                     (configurationNamePattern == null || configurationNamePattern.IsMatch(session.ConfigurationName)) &&
                                                     (session.ComputerType == TargetMachineType.VirtualMachine))
                        .ToList<PSSession>();

                    WriteOrAddMatches(matchingRunspaceInfos, writeobject, ref matches);
                }
            }

            return matches;
        }

        
        /// <param name="matchingRunspaceInfos">The matching runspaces.</param>
        /// <param name="writeobject">If true write the object down the pipeline. Otherwise, add to the list.</param>
        /// <param name="matches">The list we add the matching runspaces to.</param>
        private void WriteOrAddMatches(List<PSSession> matchingRunspaceInfos,
            bool writeobject,
            ref Dictionary<Guid, PSSession> matches)
        {
            foreach (PSSession remoteRunspaceInfo in matchingRunspaceInfos)
            {
                if (writeobject)
                {
                    WriteObject(remoteRunspaceInfo);
                }
                else
                {
                    try
                    {
                        matches.Add(remoteRunspaceInfo.InstanceId, remoteRunspaceInfo);
                    }
                    catch (ArgumentException)
                    {
                        // if match already found ignore
                    }
                }
            }
        }

        
        private void WriteInvalidArgumentError(PSRemotingErrorId errorId, string resourceString, object errorArgument)
        {
            string message = GetMessage(resourceString, errorArgument);

            WriteError(new ErrorRecord(new ArgumentException(message), errorId.ToString(),
                ErrorCategory.InvalidArgument, errorArgument));
        }

        #endregion Private / Protected Methods

        #region Protected Members

        
        protected const string InstanceIdParameterSet = "InstanceId";

        
        protected const string IdParameterSet = "Id";

        
        protected const string NameParameterSet = "Name";

        #endregion Protected Members
    }

    #region Helper Classes

    
    internal abstract class ExecutionCmdletHelper : IThrottleOperation
    {
        
        internal Pipeline Pipeline
        {
            get
            {
                return pipeline;
            }
        }

        protected Pipeline pipeline;

        
        internal Exception InternalException
        {
            get
            {
                return internalException;
            }
        }

        protected Exception internalException;

        
        internal Runspace PipelineRunspace
        {
            get;
            set;
        }

        #region Runspace Debug

        internal void ConfigureRunspaceDebugging(Runspace runspace)
        {
            if (!RunspaceDebuggingEnabled || (runspace == null) || (runspace.Debugger == null)) { return; }

            runspace.Debugger.DebuggerStop += HandleDebuggerStop;

            // Configure runspace debugger to preserve unhandled stops (wait for debugger attach)
            runspace.Debugger.UnhandledBreakpointMode = UnhandledBreakpointProcessingMode.Wait;

            if (RunspaceDebugStepInEnabled)
            {
                // Configure runspace debugger to run script in step mode
                try
                {
                    runspace.Debugger.SetDebuggerStepMode(true);
                }
                catch (PSInvalidOperationException) { }
            }
        }

        internal void CleanupRunspaceDebugging(Runspace runspace)
        {
            if ((runspace == null) || (runspace.Debugger == null)) { return; }

            runspace.Debugger.DebuggerStop -= HandleDebuggerStop;
        }

        private void HandleDebuggerStop(object sender, DebuggerStopEventArgs args)
        {
            PipelineRunspace.Debugger.DebuggerStop -= HandleDebuggerStop;

            // Forward event
            RaiseRunspaceDebugStopEvent(PipelineRunspace);

            // Signal remote session to remain stopped in debuger
            args.SuspendRemote = true;
        }

        #endregion
    }

    
    internal class ExecutionCmdletHelperRunspace : ExecutionCmdletHelper
    {
        
        internal bool ShouldUseSteppablePipelineOnServer;

        
        /// <param name="pipeline">Pipeline object associated with this operation.</param>
        internal ExecutionCmdletHelperRunspace(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            PipelineRunspace = pipeline.Runspace;
            this.pipeline.StateChanged += HandlePipelineStateChanged;
        }

        
        internal override void StartOperation()
        {
            ConfigureRunspaceDebugging(PipelineRunspace);

            try
            {
                if (ShouldUseSteppablePipelineOnServer && pipeline is RemotePipeline rPipeline)
                {
                    rPipeline.SetIsNested(true);
                    rPipeline.SetIsSteppable(true);
                }

                pipeline.InvokeAsync();
            }
            catch (InvalidRunspaceStateException e)
            {
                internalException = e;
                RaiseOperationCompleteEvent();
            }
            catch (InvalidPipelineStateException e)
            {
                internalException = e;
                RaiseOperationCompleteEvent();
            }
            catch (InvalidOperationException e)
            {
                internalException = e;
                RaiseOperationCompleteEvent();
            }
        }

        
        internal override void StopOperation()
        {
            if (pipeline.PipelineStateInfo.State == PipelineState.Running ||
                pipeline.PipelineStateInfo.State == PipelineState.Disconnected ||
                pipeline.PipelineStateInfo.State == PipelineState.NotStarted)
            {
                // If the pipeline state has reached Complete/Failed/Stopped
                // by the time control reaches here, then this operation
                // becomes a no-op. However, an OperationComplete would have
                // already been raised from the handler
                pipeline.StopAsync();
            }
            else
            {
                // will have to raise OperationComplete from here,
                // else ThrottleManager will have
                RaiseOperationCompleteEvent();
            }
        }

        internal override event EventHandler<OperationStateEventArgs> OperationComplete;

        
        /// <param name="sender">Source of this event.</param>
        /// <param name="stateEventArgs">object describing state information about the
        /// pipeline</param>
        private void HandlePipelineStateChanged(object sender, PipelineStateEventArgs stateEventArgs)
        {
            PipelineStateInfo stateInfo = stateEventArgs.PipelineStateInfo;

            switch (stateInfo.State)
            {
                case PipelineState.Running:
                case PipelineState.NotStarted:
                case PipelineState.Stopping:
                case PipelineState.Disconnected:
                    return;
            }

            RaiseOperationCompleteEvent(stateEventArgs);
        }

        
        private void RaiseOperationCompleteEvent()
        {
            RaiseOperationCompleteEvent(null);
        }

        
        /// <param name="baseEventArgs">The event args which actually
        /// raises this operation complete</param>
        private void RaiseOperationCompleteEvent(EventArgs baseEventArgs)
        {
            CleanupRunspaceDebugging(PipelineRunspace);

            if (pipeline != null)
            {
                // Dispose the pipeline object and release data and remoting resources.
                // Pipeline object remains to provide information on final state and any errors incurred.
                pipeline.StateChanged -= HandlePipelineStateChanged;
                pipeline.Dispose();
            }

            OperationStateEventArgs operationStateEventArgs =
                    new OperationStateEventArgs();
            operationStateEventArgs.OperationState =
                    OperationState.StopComplete;
            operationStateEventArgs.BaseEvent = baseEventArgs;

            OperationComplete?.SafeInvoke(this, operationStateEventArgs);
        }
    }

    
    internal class ExecutionCmdletHelperComputerName : ExecutionCmdletHelper
    {
        
        private readonly bool _invokeAndDisconnect;

        
        internal RemoteRunspace RemoteRunspace { get; private set; }

        
        /// <param name="remoteRunspace">RemoteRunspace that is associated
        /// with this operation</param>
        /// <param name="pipeline">Pipeline created from the remote runspace.</param>
        /// <param name="invokeAndDisconnect">Indicates if pipeline should be disconnected after invoking command.</param>
        internal ExecutionCmdletHelperComputerName(RemoteRunspace remoteRunspace, Pipeline pipeline, bool invokeAndDisconnect = false)
        {
            Dbg.Assert(remoteRunspace != null,
                    "RemoteRunspace reference cannot be null");
            PipelineRunspace = remoteRunspace;

            _invokeAndDisconnect = invokeAndDisconnect;

            RemoteRunspace = remoteRunspace;
            remoteRunspace.StateChanged += HandleRunspaceStateChanged;

            Dbg.Assert(pipeline != null,
                    "Pipeline cannot be null or empty");

            this.pipeline = pipeline;
            pipeline.StateChanged += HandlePipelineStateChanged;
        }

        
        internal override void StartOperation()
        {
            try
            {
                RemoteRunspace.OpenAsync();
            }
            catch (PSRemotingTransportException e)
            {
                internalException = e;
                RaiseOperationCompleteEvent();
            }
        }

        
        internal override void StopOperation()
        {
            bool needToStop = false; // indicates whether to call StopAsync

            if (pipeline.PipelineStateInfo.State == PipelineState.Running ||
                pipeline.PipelineStateInfo.State == PipelineState.NotStarted)
            {
                needToStop = true;
            }

            if (needToStop)
            {
                // If the pipeline state has reached Complete/Failed/Stopped
                // by the time control reaches here, then this operation
                // becomes a no-op. However, an OperationComplete would have
                // already been raised from the handler
                pipeline.StopAsync();
            }
            else
            {
                // raise an OperationComplete event here. Else the
                // throttle manager will not respond as it will be waiting for
                // this StopOperation to complete
                RaiseOperationCompleteEvent();
            }
        }

        internal override event EventHandler<OperationStateEventArgs> OperationComplete;

        
        /// <param name="sender">Sender of this information.</param>
        /// <param name="stateEventArgs">Object describing this event.</param>
        private void HandleRunspaceStateChanged(object sender,
                RunspaceStateEventArgs stateEventArgs)
        {
            RunspaceState state = stateEventArgs.RunspaceStateInfo.State;

            switch (state)
            {
                case RunspaceState.BeforeOpen:
                case RunspaceState.Opening:
                case RunspaceState.Closing:
                    return;

                case RunspaceState.Opened:
                    {
                        ConfigureRunspaceDebugging(RemoteRunspace);

                        // if successfully opened
                        // Call InvokeAsync() on the pipeline
                        try
                        {
                            if (_invokeAndDisconnect)
                            {
                                pipeline.InvokeAsyncAndDisconnect();
                            }
                            else
                            {
                                pipeline.InvokeAsync();
                            }
                        }
                        catch (InvalidPipelineStateException)
                        {
                            RemoteRunspace.CloseAsync();
                        }
                        catch (InvalidRunspaceStateException e)
                        {
                            internalException = e;
                            RemoteRunspace.CloseAsync();
                        }
                    }

                    break;

                case RunspaceState.Broken:
                    {
                        RaiseOperationCompleteEvent(stateEventArgs);
                    }

                    break;
                case RunspaceState.Closed:
                    {
                        // raise a OperationComplete event with
                        // StopComplete message
                        if (stateEventArgs.RunspaceStateInfo.Reason != null)
                        {
                            RaiseOperationCompleteEvent(stateEventArgs);
                        }
                        else
                        {
                            RaiseOperationCompleteEvent();
                        }
                    }

                    break;
            }
        }

        
        /// <param name="sender">Sender of this information.</param>
        /// <param name="stateEventArgs">Object describing this event.</param>
        private void HandlePipelineStateChanged(object sender,
                        PipelineStateEventArgs stateEventArgs)
        {
            PipelineState state = stateEventArgs.PipelineStateInfo.State;

            switch (state)
            {
                case PipelineState.Running:
                case PipelineState.NotStarted:
                case PipelineState.Stopping:
                    return;

                case PipelineState.Completed:
                case PipelineState.Stopped:
                case PipelineState.Failed:
                    RemoteRunspace?.CloseAsync();
                    break;
            }
        }

        
        private void RaiseOperationCompleteEvent()
        {
            RaiseOperationCompleteEvent(null);
        }

        
        /// <param name="baseEventArgs">The event args which actually
        /// raises this operation complete</param>
        private void RaiseOperationCompleteEvent(EventArgs baseEventArgs)
        {
            if (pipeline != null)
            {
                // Dispose the pipeline object and release data and remoting resources.
                // Pipeline object remains to provide information on final state and any errors incurred.
                pipeline.StateChanged -= HandlePipelineStateChanged;
                pipeline.Dispose();
            }

            if (RemoteRunspace != null)
            {
                // Dispose of the runspace object.
                RemoteRunspace.Dispose();
                RemoteRunspace = null;
            }

            OperationStateEventArgs operationStateEventArgs =
                    new OperationStateEventArgs();
            operationStateEventArgs.OperationState =
                    OperationState.StopComplete;
            operationStateEventArgs.BaseEvent = baseEventArgs;
            OperationComplete.SafeInvoke(this, operationStateEventArgs);
        }
    }

    #region Path Resolver

    
    internal static class PathResolver
    {
        
        /// <param name="path">Path to resolve.</param>
        /// <param name="isLiteralPath">True if wildcard expansion should be suppressed for this path.</param>
        /// <param name="cmdlet">reference to calling cmdlet. This will be used for
        /// for writing errors</param>
        /// <param name="allowNonexistingPaths"></param>
        /// <param name="resourceString">Resource string for error when path is not from filesystem provider.</param>
        /// <returns>A fully qualified string representing filename.</returns>
        internal static string ResolveProviderAndPath(string path, bool isLiteralPath, PSCmdlet cmdlet, bool allowNonexistingPaths, string resourceString)
        {
            // First resolve path
            PathInfo resolvedPath = ResolvePath(path, isLiteralPath, allowNonexistingPaths, cmdlet);

            if (resolvedPath.Provider.ImplementingType == typeof(FileSystemProvider))
            {
                return resolvedPath.ProviderPath;
            }

            throw PSTraceSource.NewInvalidOperationException(resourceString, resolvedPath.Provider.Name);
        }

        
        /// <param name="pathToResolve">
        /// The path to be resolved. Each path may contain glob characters.
        /// </param>
        /// <param name="isLiteralPath">
        /// True if wildcard expansion should be suppressed for pathToResolve.
        /// </param>
        /// <param name="allowNonexistingPaths">
        /// If true, resolves the path even if it doesn't exist.
        /// </param>
        /// <param name="cmdlet">
        /// Calling cmdlet
        /// </param>
        /// <returns>
        /// A string representing the resolved path.
        /// </returns>
        private static PathInfo ResolvePath(
            string pathToResolve,
            bool isLiteralPath,
            bool allowNonexistingPaths,
            PSCmdlet cmdlet)
        {
            // Construct cmdletprovidercontext
            CmdletProviderContext cmdContext = new CmdletProviderContext(cmdlet);
            cmdContext.SuppressWildcardExpansion = isLiteralPath;

            Collection<PathInfo> results = new Collection<PathInfo>();

            try
            {
                // First resolve path
                Collection<PathInfo> pathInfos =
                    cmdlet.SessionState.Path.GetResolvedPSPathFromPSPath(
                        pathToResolve,
                        cmdContext);

                foreach (PathInfo pathInfo in pathInfos)
                {
                    results.Add(pathInfo);
                }
            }
            catch (PSNotSupportedException notSupported)
            {
                cmdlet.ThrowTerminatingError(
                    new ErrorRecord(
                        notSupported.ErrorRecord,
                        notSupported));
            }
            catch (System.Management.Automation.DriveNotFoundException driveNotFound)
            {
                cmdlet.ThrowTerminatingError(
                    new ErrorRecord(
                        driveNotFound.ErrorRecord,
                        driveNotFound));
            }
            catch (ProviderNotFoundException providerNotFound)
            {
                cmdlet.ThrowTerminatingError(
                    new ErrorRecord(
                        providerNotFound.ErrorRecord,
                        providerNotFound));
            }
            catch (ItemNotFoundException pathNotFound)
            {
                if (allowNonexistingPaths)
                {
                    ProviderInfo provider = null;
                    System.Management.Automation.PSDriveInfo drive = null;
                    string unresolvedPath =
                        cmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                            pathToResolve,
                            cmdContext,
                            out provider,
                            out drive);

                    PathInfo pathInfo =
                        new PathInfo(
                            drive,
                            provider,
                            unresolvedPath,
                            cmdlet.SessionState);
                    results.Add(pathInfo);
                }
                else
                {
                    cmdlet.ThrowTerminatingError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                }
            }

            if (results.Count == 1)
            {
                return results[0];
            }
            else // if (results.Count > 1)
            {
                Exception e = PSTraceSource.NewNotSupportedException();
                cmdlet.ThrowTerminatingError(
                    new ErrorRecord(e,
                    "NotSupported",
                    ErrorCategory.NotImplemented,
                    results));
                return null;
            }
        }
    }

    #endregion

    #region QueryRunspaces

    internal class QueryRunspaces
    {
        #region Constructor

        internal QueryRunspaces()
        {
            _stopProcessing = false;
        }

        #endregion

        #region Internal Methods

        
        /// <param name="connectionInfos">Collection of WSManConnectionInfo objects.</param>
        /// <param name="host">Host for PSSession objects.</param>
        /// <param name="stream">Out stream object.</param>
        /// <param name="runspaceRepository">Runspace repository.</param>
        /// <param name="throttleLimit">Throttle limit.</param>
        /// <param name="filterState">Runspace state filter value.</param>
        /// <param name="matchIds">Array of session Guids to match to.</param>
        /// <param name="matchNames">Array of session Names to match to.</param>
        /// <param name="configurationName">Configuration name to match to.</param>
        /// <returns>Collection of disconnected PSSession objects.</returns>
        internal Collection<PSSession> GetDisconnectedSessions(Collection<WSManConnectionInfo> connectionInfos, PSHost host,
                                                               ObjectStream stream, RunspaceRepository runspaceRepository,
                                                               int throttleLimit, SessionFilterState filterState,
                                                               Guid[] matchIds, string[] matchNames, string configurationName)
        {
            Collection<PSSession> filteredPSSessions = new Collection<PSSession>();

            // Create a query operation for each connection information object.
            foreach (WSManConnectionInfo connectionInfo in connectionInfos)
            {
                Runspace[] runspaces = null;

                try
                {
                    runspaces = Runspace.GetRunspaces(connectionInfo, host, BuiltInTypesTable);
                }
                catch (System.Management.Automation.RuntimeException e)
                {
                    if (e.InnerException is InvalidOperationException)
                    {
                        // The Get-WSManInstance cmdlet used to query remote computers for runspaces will throw
                        // an Invalid Operation (inner) exception if the connectInfo object is invalid, including
                        // invalid computer names.
                        // We don't want to propagate the exception so just write error here.
                        if (stream.ObjectWriter != null && stream.ObjectWriter.IsOpen)
                        {
                            int errorCode;
                            string msg = StringUtil.Format(RemotingErrorIdStrings.QueryForRunspacesFailed, connectionInfo.ComputerName, ExtractMessage(e.InnerException, out errorCode));
                            string FQEID = WSManTransportManagerUtils.GetFQEIDFromTransportError(errorCode, "RemotePSSessionQueryFailed");
                            Exception reason = new RuntimeException(msg, e.InnerException);
                            ErrorRecord errorRecord = new ErrorRecord(reason, FQEID, ErrorCategory.InvalidOperation, connectionInfo);
                            stream.ObjectWriter.Write((Action<Cmdlet>)(cmdlet => cmdlet.WriteError(errorRecord)));
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

                if (_stopProcessing)
                {
                    break;
                }

                // Add all runspaces meeting filter criteria to collection.
                if (runspaces != null)
                {
                    // Convert configuration name into shell Uri for comparison.
                    string shellUri = null;
                    if (!string.IsNullOrEmpty(configurationName))
                    {
                        shellUri = configurationName.Contains(WSManNativeApi.ResourceURIPrefix, StringComparison.OrdinalIgnoreCase)
                            ? configurationName
                            : WSManNativeApi.ResourceURIPrefix + configurationName;
                    }

                    foreach (Runspace runspace in runspaces)
                    {
                        // Filter returned runspaces by ConfigurationName if provided.
                        if (shellUri != null)
                        {
                            // Compare with returned shell Uri in connection info.
                            WSManConnectionInfo wsmanConnectionInfo = runspace.ConnectionInfo as WSManConnectionInfo;
                            if (wsmanConnectionInfo != null &&
                                !shellUri.Equals(wsmanConnectionInfo.ShellUri, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        // Check the repository for an existing viable PSSession for
                        // this runspace (based on instanceId).  Use the existing
                        // local runspace instead of the one returned from the server
                        // query.
                        PSSession existingPSSession = null;
                        if (runspaceRepository != null)
                        {
                            existingPSSession = runspaceRepository.GetItem(runspace.InstanceId);
                        }

                        if (existingPSSession != null &&
                            UseExistingRunspace(existingPSSession.Runspace, runspace))
                        {
                            if (TestRunspaceState(existingPSSession.Runspace, filterState))
                            {
                                filteredPSSessions.Add(existingPSSession);
                            }
                        }
                        else if (TestRunspaceState(runspace, filterState))
                        {
                            filteredPSSessions.Add(new PSSession(runspace as RemoteRunspace));
                        }
                    }
                }
            }

            // Return only PSSessions that match provided Ids or Names.
            if ((matchIds != null) && (filteredPSSessions.Count > 0))
            {
                Collection<PSSession> matchIdsSessions = new Collection<PSSession>();
                foreach (Guid id in matchIds)
                {
                    bool matchFound = false;
                    foreach (PSSession psSession in filteredPSSessions)
                    {
                        if (_stopProcessing)
                        {
                            break;
                        }

                        if (psSession.Runspace.InstanceId.Equals(id))
                        {
                            matchFound = true;
                            matchIdsSessions.Add(psSession);
                            break;
                        }
                    }

                    if (!matchFound && stream.ObjectWriter != null && stream.ObjectWriter.IsOpen)
                    {
                        string msg = StringUtil.Format(RemotingErrorIdStrings.SessionIdMatchFailed, id);
                        Exception reason = new RuntimeException(msg);
                        ErrorRecord errorRecord = new ErrorRecord(reason, "PSSessionIdMatchFail", ErrorCategory.InvalidOperation, id);
                        stream.ObjectWriter.Write((Action<Cmdlet>)(cmdlet => cmdlet.WriteError(errorRecord)));
                    }
                }

                // Return all found sessions.
                return matchIdsSessions;
            }
            else if ((matchNames != null) && (filteredPSSessions.Count > 0))
            {
                Collection<PSSession> matchNamesSessions = new Collection<PSSession>();
                foreach (string name in matchNames)
                {
                    WildcardPattern namePattern = WildcardPattern.Get(name, WildcardOptions.IgnoreCase);
                    bool matchFound = false;
                    foreach (PSSession psSession in filteredPSSessions)
                    {
                        if (_stopProcessing)
                        {
                            break;
                        }

                        if (namePattern.IsMatch(((RemoteRunspace)psSession.Runspace).RunspacePool.RemoteRunspacePoolInternal.Name))
                        {
                            matchFound = true;
                            matchNamesSessions.Add(psSession);
                        }
                    }

                    if (!matchFound && stream.ObjectWriter != null && stream.ObjectWriter.IsOpen)
                    {
                        string msg = StringUtil.Format(RemotingErrorIdStrings.SessionNameMatchFailed, name);
                        Exception reason = new RuntimeException(msg);
                        ErrorRecord errorRecord = new ErrorRecord(reason, "PSSessionNameMatchFail", ErrorCategory.InvalidOperation, name);
                        stream.ObjectWriter.Write((Action<Cmdlet>)(cmdlet => cmdlet.WriteError(errorRecord)));
                    }
                }

                return matchNamesSessions;
            }
            else
            {
                // Return all collected sessions.
                return filteredPSSessions;
            }
        }

        
        /// <param name="existingRunspace"></param>
        /// <param name="queriedrunspace"></param>
        /// <returns></returns>
        private static bool UseExistingRunspace(
            Runspace existingRunspace,
            Runspace queriedrunspace)
        {
            Dbg.Assert(existingRunspace != null, "Invalid parameter.");
            Dbg.Assert(queriedrunspace != null, "Invalid parameter.");

            if (existingRunspace.RunspaceStateInfo.State == RunspaceState.Broken)
            {
                return false;
            }

            if (existingRunspace.RunspaceStateInfo.State == RunspaceState.Disconnected &&
                queriedrunspace.RunspaceAvailability == RunspaceAvailability.Busy)
            {
                return false;
            }

            // Update existing runspace to have latest DisconnectedOn/ExpiresOn data.
            existingRunspace.DisconnectedOn = queriedrunspace.DisconnectedOn;
            existingRunspace.ExpiresOn = queriedrunspace.ExpiresOn;

            return true;
        }

        
        /// <param name="e">Exception.</param>
        /// <param name="errorCode">Returned WSMan error code.</param>
        /// <returns>WSMan message.</returns>
        internal static string ExtractMessage(
            Exception e,
            out int errorCode)
        {
            errorCode = 0;

            if (e == null ||
                e.Message == null)
            {
                return string.Empty;
            }

            string rtnMsg = null;
            try
            {
                System.Xml.XmlReaderSettings xmlReaderSettings = InternalDeserializer.XmlReaderSettingsForUntrustedXmlDocument.Clone();
                xmlReaderSettings.MaxCharactersInDocument = 4096;
                xmlReaderSettings.MaxCharactersFromEntities = 1024;
                xmlReaderSettings.DtdProcessing = System.Xml.DtdProcessing.Prohibit;

                using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(
                        new System.IO.StringReader(e.Message), xmlReaderSettings))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == System.Xml.XmlNodeType.Element)
                        {
                            if (reader.LocalName.Equals("Message", StringComparison.OrdinalIgnoreCase))
                            {
                                rtnMsg = reader.ReadElementContentAsString();
                            }
                            else if (reader.LocalName.Equals("WSManFault", StringComparison.OrdinalIgnoreCase))
                            {
                                string errorCodeString = reader.GetAttribute("Code");
                                if (errorCodeString != null)
                                {
                                    try
                                    {
                                        // WinRM returns both signed and unsigned 32 bit string values.  Convert to signed 32 bit integer.
                                        Int64 eCode = Convert.ToInt64(errorCodeString, System.Globalization.NumberFormatInfo.InvariantInfo);
                                        unchecked
                                        {
                                            errorCode = (int)eCode;
                                        }
                                    }
                                    catch (FormatException)
                                    { }
                                    catch (OverflowException)
                                    { }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Xml.XmlException)
            { }

            return rtnMsg ?? e.Message;
        }

        
        internal void StopAllOperations()
        {
            _stopProcessing = true;
        }

        
        /// <param name="runspace">Runspace object to test.</param>
        /// <param name="filterState">Filter state to compare.</param>
        /// <returns>Result of test.</returns>
        public static bool TestRunspaceState(Runspace runspace, SessionFilterState filterState)
        {
            bool result;

            switch (filterState)
            {
                case SessionFilterState.All:
                    result = true;
                    break;

                case SessionFilterState.Opened:
                    result = (runspace.RunspaceStateInfo.State == RunspaceState.Opened);
                    break;

                case SessionFilterState.Closed:
                    result = (runspace.RunspaceStateInfo.State == RunspaceState.Closed);
                    break;

                case SessionFilterState.Disconnected:
                    result = (runspace.RunspaceStateInfo.State == RunspaceState.Disconnected);
                    break;

                case SessionFilterState.Broken:
                    result = (runspace.RunspaceStateInfo.State == RunspaceState.Broken);
                    break;

                default:
                    Dbg.Assert(false, "Invalid SessionFilterState value.");
                    result = false;
                    break;
            }

            return result;
        }

        
        internal static TypeTable BuiltInTypesTable
        {
            get
            {
                if (s_TypeTable == null)
                {
                    lock (s_SyncObject)
                    {
                        s_TypeTable ??= TypeTable.LoadDefaultTypeFiles();
                    }
                }

                return s_TypeTable;
            }
        }

        #endregion

        #region Private Members

        private bool _stopProcessing;

        private static readonly object s_SyncObject = new object();
        private static TypeTable s_TypeTable;

        #endregion
    }

    #endregion

    #region SessionFilterState Enum

    
    public enum SessionFilterState
    {
        
        All = 0,

        
        Opened = 1,

        
        Disconnected = 2,

        
        Closed = 3,

        
        Broken = 4
    }

    #endregion

    #endregion Helper Classes
}

namespace System.Management.Automation.Remoting
{
    
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum ProxyAccessType
    {
        
        None = 0,
        
        IEConfig = 1,
        
        WinHttpConfig = 2,
        
        AutoDetect = 4,
        
        NoProxyServer = 8
    }
    
    public sealed class PSSessionOption
    {
        
        public PSSessionOption()
        {
        }

        
        public int MaximumConnectionRedirectionCount { get; set; } = WSManConnectionInfo.defaultMaximumConnectionRedirectionCount;

        
        public bool NoCompression { get; set; } = false;

        
        public bool NoMachineProfile { get; set; } = false;

        
        public ProxyAccessType ProxyAccessType { get; set; } = ProxyAccessType.None;

        
        public AuthenticationMechanism ProxyAuthentication
        {
            get
            {
                return _proxyAuthentication;
            }

            set
            {
                switch (value)
                {
                    case AuthenticationMechanism.Basic:
                    case AuthenticationMechanism.Negotiate:
                    case AuthenticationMechanism.Digest:
                        _proxyAuthentication = value;
                        break;
                    default:
                        string message = PSRemotingErrorInvariants.FormatResourceString(RemotingErrorIdStrings.ProxyAmbiguousAuthentication,
                            value,
                            nameof(AuthenticationMechanism.Basic),
                            nameof(AuthenticationMechanism.Negotiate),
                            nameof(AuthenticationMechanism.Digest));
                        throw new ArgumentException(message);
                }
            }
        }

        private AuthenticationMechanism _proxyAuthentication = AuthenticationMechanism.Negotiate;

        
        public PSCredential ProxyCredential { get; set; }

        
        public bool SkipCACheck { get; set; }

        
        public bool SkipCNCheck { get; set; }

        
        public bool SkipRevocationCheck { get; set; }

        
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMilliseconds(BaseTransportManager.ClientDefaultOperationTimeoutMs);

        
        public bool NoEncryption { get; set; }

        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "UTF")]
        public bool UseUTF16 { get; set; }

        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        public bool IncludePortInSPN { get; set; }

        
        public OutputBufferingMode OutputBufferingMode { get; set; } = WSManConnectionInfo.DefaultOutputBufferingMode;

        
        public int MaxConnectionRetryCount { get; set; } = WSManConnectionInfo.DefaultMaxConnectionRetryCount;

        
        public CultureInfo Culture { get; set; }

        
        public CultureInfo UICulture { get; set; }

        
        public int? MaximumReceivedDataSizePerCommand { get; set; }

        
        public int? MaximumReceivedObjectSize { get; set; } = 200 << 20;

        
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public PSPrimitiveDictionary ApplicationArguments { get; set; }

        
        public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromMilliseconds(RunspaceConnectionInfo.DefaultOpenTimeout);

        
        public TimeSpan CancelTimeout { get; set; } = TimeSpan.FromMilliseconds(RunspaceConnectionInfo.defaultCancelTimeout);

        
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMilliseconds(RunspaceConnectionInfo.DefaultIdleTimeout);
    }
}
