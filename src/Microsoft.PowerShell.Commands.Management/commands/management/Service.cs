// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX // Not built on Unix

using System;
using System.Collections.Generic;
using System.ComponentModel; // Win32Exception
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Runtime.InteropServices; // Marshal, DllImport
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.ServiceProcess;
using Dbg = System.Management.Automation.Diagnostics;
using DWORD = System.UInt32;
using NakedWin32Handle = System.IntPtr;

namespace Microsoft.PowerShell.Commands
{
    #region ServiceBaseCommand

    
    public abstract class ServiceBaseCommand : Cmdlet
    {
        #region Internal

        
        /// <param name="service">Service object to be acted on.</param>
        /// <returns>True if operation should continue, false otherwise.</returns>
        protected bool ShouldProcessServiceOperation(ServiceController service)
        {
            return ShouldProcessServiceOperation(
                service.DisplayName,
                service.ServiceName);
        }

        
        /// <param name="displayName">Display name of service to be acted on.</param>
        /// <param name="serviceName">Service name of service to be acted on.</param>
        /// <returns>True if operation should continue, false otherwise.</returns>
        protected bool ShouldProcessServiceOperation(
            string displayName, string serviceName)
        {
            string name = StringUtil.Format(ServiceResources.ServiceNameForConfirmation,
                displayName,
                serviceName);

            return ShouldProcess(name);
        }

        
        /// <param name="service"></param>
        /// <param name="innerException"></param>
        /// <param name="errorId"></param>
        /// <param name="errorMessage"></param>
        /// <param name="category"></param>
        internal void WriteNonTerminatingError(
            ServiceController service,
            Exception innerException,
            string errorId,
            string errorMessage,
            ErrorCategory category)
        {
            WriteNonTerminatingError(
                service.ServiceName,
                service.DisplayName,
                service,
                innerException,
                errorId,
                errorMessage,
                category);
        }

        
        /// <param name="serviceName"></param>
        /// <param name="displayName"></param>
        /// <param name="targetObject"></param>
        /// <param name="innerException"></param>
        /// <param name="errorId"></param>
        /// <param name="errorMessage"></param>
        /// <param name="category"></param>
        internal void WriteNonTerminatingError(
            string serviceName,
            string displayName,
            object targetObject,
            Exception innerException,
            string errorId,
            string errorMessage,
            ErrorCategory category)
        {
            string message = StringUtil.Format(errorMessage,
                serviceName,
                displayName,
                (innerException == null) ? category.ToString() : innerException.Message);

            var exception = new ServiceCommandException(message, innerException);
            exception.ServiceName = serviceName;

            WriteError(new ErrorRecord(exception, errorId, category, targetObject));
        }

        internal void SetServiceSecurityDescriptor(
            ServiceController service,
            string securityDescriptorSddl,
            NakedWin32Handle hService)
        {
            var rawSecurityDescriptor = new RawSecurityDescriptor(securityDescriptorSddl);
            RawAcl rawDiscretionaryAcl = rawSecurityDescriptor.DiscretionaryAcl;
            var discretionaryAcl = new DiscretionaryAcl(false, false, rawDiscretionaryAcl);

            byte[] rawDacl = new byte[discretionaryAcl.BinaryLength];
            discretionaryAcl.GetBinaryForm(rawDacl, 0);
            rawSecurityDescriptor.DiscretionaryAcl = new RawAcl(rawDacl, 0);
            byte[] securityDescriptorByte = new byte[rawSecurityDescriptor.BinaryLength];
            rawSecurityDescriptor.GetBinaryForm(securityDescriptorByte, 0);

            bool status = NativeMethods.SetServiceObjectSecurity(
                hService,
                SecurityInfos.DiscretionaryAcl,
                securityDescriptorByte);

            if (!status)
            {
                int lastError = Marshal.GetLastWin32Error();
                Win32Exception exception = new(lastError);
                bool accessDenied = exception.NativeErrorCode == NativeMethods.ERROR_ACCESS_DENIED;
                WriteNonTerminatingError(
                    service,
                    exception,
                    nameof(ServiceResources.CouldNotSetServiceSecurityDescriptorSddl),
                    StringUtil.Format(ServiceResources.CouldNotSetServiceSecurityDescriptorSddl, service.ServiceName, exception.Message),
                    accessDenied ? ErrorCategory.PermissionDenied : ErrorCategory.InvalidOperation);
            }
        }
        #endregion Internal
    }
    #endregion ServiceBaseCommand

    #region MultipleServiceCommandBase

    
    public abstract class MultipleServiceCommandBase : ServiceBaseCommand
    {
        #region Parameters

        
        internal enum SelectionMode
        {
            
            Default = 0,
            
            DisplayName = 1,
            
            InputObject = 2,
            
            ServiceName = 3
        }
        
        internal SelectionMode selectionMode;

        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        internal string[] serviceNames = null;

        
        [Parameter(ParameterSetName = "DisplayName", Mandatory = true)]
        public string[] DisplayName
        {
            get
            {
                return displayNames;
            }

            set
            {
                displayNames = value;
                selectionMode = SelectionMode.DisplayName;
            }
        }

        internal string[] displayNames = null;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[] Include
        {
            get
            {
                return include;
            }

            set
            {
                include = value;
            }
        }

        internal string[] include = null;

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string[] Exclude
        {
            get
            {
                return exclude;
            }

            set
            {
                exclude = value;
            }
        }

        internal string[] exclude = null;

        // 1054295-2004/12/01-JonN This also works around 1054295.
        
        /// <value>ServiceController objects</value>
        [Parameter(ParameterSetName = "InputObject", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ServiceController[] InputObject
        {
            get
            {
                return _inputObject;
            }

            set
            {
                _inputObject = value;
                selectionMode = SelectionMode.InputObject;
            }
        }

        private ServiceController[] _inputObject = null;
        #endregion Parameters

        #region Internal

        
        /// <value>
        /// An array of <see cref="ServiceController"/> components that represents all the service resources.
        /// </value>
        /// <exception cref="System.Security.SecurityException">
        /// MSDN does not document the list of exceptions,
        /// but it is reasonable to expect that SecurityException is
        /// among them.  Errors here will terminate the cmdlet.
        /// </exception>
        internal ServiceController[] AllServices => _allServices ??= ServiceController.GetServices();

        private ServiceController[] _allServices;

        internal ServiceController GetOneService(string nameOfService)
        {
            Dbg.Assert(!WildcardPattern.ContainsWildcardCharacters(nameOfService), "Caller should verify that nameOfService doesn't contain wildcard characters");

            try
            {
                var sc = new ServiceController(nameOfService);
                // This will throw if the service doesn't exist
                var unused = sc.Status;

                // No exception, then this is an existing, valid service. Return it.
                return sc;
            }
            catch (InvalidOperationException) { }
            catch (ArgumentException) { }

            return null;
        }

        
        /// <returns></returns>
        internal List<ServiceController> MatchingServices()
        {
            List<ServiceController> matchingServices;
            switch (selectionMode)
            {
                case SelectionMode.DisplayName:
                    matchingServices = MatchingServicesByDisplayName();
                    break;
                case SelectionMode.InputObject:
                    matchingServices = MatchingServicesByInput();
                    break;
                default:
                    matchingServices = MatchingServicesByServiceName();
                    break;
            }
            // 2004/12/16 Note that the services will be sorted
            //  before being stopped.  JimTru confirms that this is OK.
            matchingServices.Sort(ServiceComparison);
            return matchingServices;
        }

        // sort by servicename
        private static int ServiceComparison(ServiceController x, ServiceController y)
        {
            return string.Compare(x.ServiceName, y.ServiceName, StringComparison.OrdinalIgnoreCase);
        }

        
        /// <returns></returns>
        /// <remarks>
        /// We do not use the ServiceController(string serviceName)
        /// constructor variant, since the resultant
        /// ServiceController.ServiceName is the provided serviceName
        /// even when that differs from the real ServiceName by case.
        /// </remarks>
        private List<ServiceController> MatchingServicesByServiceName()
        {
            List<ServiceController> matchingServices = new();

            if (serviceNames == null)
            {
                foreach (ServiceController service in AllServices)
                {
                    IncludeExcludeAdd(matchingServices, service, false);
                }

                return matchingServices;
            }

            foreach (string pattern in serviceNames)
            {
                bool found = false;

                if (WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                    foreach (ServiceController service in AllServices)
                    {
                        if (!wildcard.IsMatch(service.ServiceName))
                            continue;
                        found = true;
                        IncludeExcludeAdd(matchingServices, service, true);
                    }
                }
                else
                {
                    ServiceController service = GetOneService(pattern);
                    if (service != null)
                    {
                        found = true;
                        IncludeExcludeAdd(matchingServices, service, true);
                    }
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WriteNonTerminatingError(
                        pattern,
                        string.Empty,
                        pattern,
                        null,
                        "NoServiceFoundForGivenName",
                        ServiceResources.NoServiceFoundForGivenName,
                        ErrorCategory.ObjectNotFound);
                }
            }

            return matchingServices;
        }

        
        /// <returns></returns>
        private List<ServiceController> MatchingServicesByDisplayName()
        {
            List<ServiceController> matchingServices = new();
            if (DisplayName == null)
            {
                Diagnostics.Assert(false, "null DisplayName");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (string pattern in DisplayName)
            {
                WildcardPattern wildcard =
                    WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                bool found = false;
                foreach (ServiceController service in AllServices)
                {
                    if (!wildcard.IsMatch(service.DisplayName))
                        continue;
                    found = true;
                    IncludeExcludeAdd(matchingServices, service, true);
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    WriteNonTerminatingError(
                        string.Empty,
                        pattern,
                        pattern,
                        null,
                        "NoServiceFoundForGivenDisplayName",
                        ServiceResources.NoServiceFoundForGivenDisplayName,
                        ErrorCategory.ObjectNotFound);
                }
            }

            return matchingServices;
        }

        
        /// <returns></returns>
        private List<ServiceController> MatchingServicesByInput()
        {
            List<ServiceController> matchingServices = new();
            if (InputObject == null)
            {
                Diagnostics.Assert(false, "null InputObject");
                throw PSTraceSource.NewInvalidOperationException();
            }

            foreach (ServiceController service in InputObject)
            {
                service.Refresh();
                IncludeExcludeAdd(matchingServices, service, false);
            }

            return matchingServices;
        }

        
        /// <param name="list">List of services.</param>
        /// <param name="service">Service to add to list.</param>
        /// <param name="checkDuplicates">Check list for duplicates.</param>
        private void IncludeExcludeAdd(
            List<ServiceController> list,
            ServiceController service,
            bool checkDuplicates)
        {
            if (include != null && !Matches(service, include))
                return;
            if (exclude != null && Matches(service, exclude))
                return;
            if (checkDuplicates)
            {
                foreach (ServiceController sc in list)
                {
                    if (sc.ServiceName == service.ServiceName &&
                        sc.MachineName == service.MachineName)
                    {
                        return;
                    }
                }
            }

            list.Add(service);
        }

        
        /// <param name="service"></param>
        /// <param name="matchList"></param>
        /// <returns></returns>
        private bool Matches(ServiceController service, string[] matchList)
        {
            if (matchList == null)
                throw PSTraceSource.NewArgumentNullException(nameof(matchList));
            string serviceID = (selectionMode == SelectionMode.DisplayName)
                                ? service.DisplayName
                                : service.ServiceName;
            foreach (string pattern in matchList)
            {
                WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                if (wildcard.IsMatch(serviceID))
                    return true;
            }

            return false;
        }
        #endregion Internal

    }
    #endregion MultipleServiceCommandBase

    #region GetServiceCommand
    
    [Cmdlet(VerbsCommon.Get, "Service", DefaultParameterSetName = "Default",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096496", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(ServiceController))]
    public sealed class GetServiceCommand : MultipleServiceCommandBase
    {
        #region Parameters
        
        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        [Parameter(Position = 0, ParameterSetName = "Default", ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty()]
        [Alias("ServiceName")]
        public string[] Name
        {
            get
            {
                return serviceNames;
            }

            set
            {
                serviceNames = value;
                selectionMode = SelectionMode.ServiceName;
            }
        }

        
        [Parameter]
        [Alias("DS")]
        public SwitchParameter DependentServices { get; set; }

        
        [Parameter]
        [Alias("SDO", "ServicesDependedOn")]
        public SwitchParameter RequiredServices { get; set; }

        #endregion Parameters

        #region Overrides
        
        protected override void ProcessRecord()
        {
            nint scManagerHandle = nint.Zero;
            if (!DependentServices && !RequiredServices)
            {
                // As Get-Service only works on local services we get this once
                // to retrieve extra properties added by PowerShell.
                scManagerHandle = NativeMethods.OpenSCManagerW(
                    lpMachineName: null,
                    lpDatabaseName: null,
                    dwDesiredAccess: NativeMethods.SC_MANAGER_CONNECT);
                if (scManagerHandle == nint.Zero)
                {
                    Win32Exception exception = new();
                    string message = StringUtil.Format(ServiceResources.FailToOpenServiceControlManager, exception.Message);
                    ServiceCommandException serviceException = new ServiceCommandException(message, exception);
                    ErrorRecord err = new ErrorRecord(
                        serviceException,
                        "FailToOpenServiceControlManager",
                        ErrorCategory.PermissionDenied,
                        null);
                    ThrowTerminatingError(err);
                }
            }

            try
            {
                foreach (ServiceController service in MatchingServices())
                {
                    if (!DependentServices.IsPresent && !RequiredServices.IsPresent)
                    {
                        WriteObject(AddProperties(scManagerHandle, service));
                    }
                    else
                    {
                        if (DependentServices.IsPresent)
                        {
                            foreach (ServiceController dependantserv in service.DependentServices)
                            {
                                WriteObject(dependantserv);
                            }
                        }

                        if (RequiredServices.IsPresent)
                        {
                            foreach (ServiceController servicedependedon in service.ServicesDependedOn)
                            {
                                WriteObject(servicedependedon);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (scManagerHandle != nint.Zero)
                {
                    bool succeeded = NativeMethods.CloseServiceHandle(scManagerHandle);
                    Diagnostics.Assert(succeeded, "SCManager handle close failed");
                }
            }
        }

        #endregion Overrides

#nullable enable
        
        /// <param name="scManagerHandle">Handle to the local SCManager instance.</param>
        /// <param name="service"></param>
        /// <returns>ServiceController as PSObject with UserName, Description and StartupType added.</returns>
        private static PSObject AddProperties(nint scManagerHandle, ServiceController service)
        {
            NakedWin32Handle hService = nint.Zero;

            // As these are optional values, a failure due to permissions or
            // other problem is ignored and the properties are set to null.
            bool? isDelayedAutoStart = null;
            string? binPath = null;
            string? description = null;
            string? startName = null;
            ServiceStartupType startupType = ServiceStartupType.InvalidValue;
            try
            {
                // We don't use service.ServiceHandle as that requests
                // SERVICE_ALL_ACCESS when we only need SERVICE_QUERY_CONFIG.
                hService = NativeMethods.OpenServiceW(
                    scManagerHandle,
                    service.ServiceName,
                    NativeMethods.SERVICE_QUERY_CONFIG
                );
                if (hService != nint.Zero)
                {
                    if (NativeMethods.QueryServiceConfig2(
                        hService,
                        NativeMethods.SERVICE_CONFIG_DESCRIPTION,
                        out NativeMethods.SERVICE_DESCRIPTIONW descriptionInfo))
                    {
                        description = descriptionInfo.lpDescription;
                    }

                    if (NativeMethods.QueryServiceConfig2(
                        hService,
                        NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                        out NativeMethods.SERVICE_DELAYED_AUTO_START_INFO autostartInfo))
                    {
                        isDelayedAutoStart = autostartInfo.fDelayedAutostart;
                    }

                    if (NativeMethods.QueryServiceConfig(
                        hService,
                        out NativeMethods.QUERY_SERVICE_CONFIG serviceInfo))
                    {
                        binPath = serviceInfo.lpBinaryPathName;
                        startName = serviceInfo.lpServiceStartName;
                        if (isDelayedAutoStart.HasValue)
                        {
                            startupType = NativeMethods.GetServiceStartupType(
                                (ServiceStartMode)serviceInfo.dwStartType,
                                isDelayedAutoStart.Value);
                        }
                    }
                }
            }
            finally
            {
                if (hService != IntPtr.Zero)
                {
                    bool succeeded = NativeMethods.CloseServiceHandle(hService);
                    Diagnostics.Assert(succeeded, "Failed to close service handle");
                }
            }

            PSObject serviceAsPSObj = PSObject.AsPSObject(service);
            PSNoteProperty noteProperty = new("UserName", startName);
            serviceAsPSObj.Properties.Add(noteProperty, true);
            serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#UserName");

            noteProperty = new PSNoteProperty("Description", description);
            serviceAsPSObj.Properties.Add(noteProperty, true);
            serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#Description");

            noteProperty = new PSNoteProperty("DelayedAutoStart", isDelayedAutoStart);
            serviceAsPSObj.Properties.Add(noteProperty, true);
            serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#DelayedAutoStart");

            noteProperty = new PSNoteProperty("BinaryPathName", binPath);
            serviceAsPSObj.Properties.Add(noteProperty, true);
            serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#BinaryPathName");

            noteProperty = new PSNoteProperty("StartupType", startupType);
            serviceAsPSObj.Properties.Add(noteProperty, true);
            serviceAsPSObj.TypeNames.Insert(0, "System.Service.ServiceController#StartupType");

            return serviceAsPSObj;
        }
    }
#nullable disable
    #endregion GetServiceCommand

    #region ServiceOperationBaseCommand
    
    public abstract class ServiceOperationBaseCommand : MultipleServiceCommandBase
    {
        #region Parameters
        
        /// <remarks>
        /// The ServiceName parameter is declared in subclasses,
        /// since it is optional for GetService and mandatory otherwise.
        /// </remarks>
        [Parameter(Position = 0, ParameterSetName = "Default", Mandatory = true, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true)]
        [Alias("ServiceName")]
        public string[] Name
        {
            get
            {
                return serviceNames;
            }

            set
            {
                serviceNames = value;
                selectionMode = SelectionMode.ServiceName;
            }
        }

        
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "InputObject", ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public new ServiceController[] InputObject
        {
            get
            {
                return base.InputObject;
            }

            set
            {
                base.InputObject = value;
            }
        }

        
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        #endregion Parameters

        #region Internal
        
        /// <param name="serviceController">Service on which to operate.</param>
        /// <param name="targetStatus">Desired status.</param>
        /// <param name="pendingStatus">
        /// This is the expected status while the operation is incomplete.
        /// If the service is in some other state, this means that the
        /// operation failed.
        /// </param>
        /// <param name="resourceIdPending">
        /// resourceId for a string to be written to verbose stream
        /// every 2 seconds
        /// </param>
        /// <param name="errorId">
        /// errorId for a nonterminating error if operation fails
        /// </param>
        ///  <param name="errorMessage">
        /// errorMessage for a nonterminating error if operation fails
        /// </param>
        /// <returns>True if action succeeded.</returns>
        /// <exception cref="PipelineStoppedException">
        /// WriteWarning will throw this if the pipeline has been stopped.
        /// This means that the delay between hitting CTRL-C and stopping
        /// the cmdlet should be 2 seconds at most.
        /// </exception>
        internal bool DoWaitForStatus(
            ServiceController serviceController,
            ServiceControllerStatus targetStatus,
            ServiceControllerStatus pendingStatus,
            string resourceIdPending,
            string errorId,
            string errorMessage)
        {
            while (true)
            {
                try
                {
                    // ServiceController.Start will return before the service is actually started
                    // This API will wait forever
                    serviceController.WaitForStatus(
                        targetStatus,
                        new TimeSpan(20000000) // 2 seconds
                        );
                    return true; // service reached target status
                }
                catch (System.ServiceProcess.TimeoutException) // still waiting
                {
                    if (serviceController.Status != pendingStatus
                        // NTRAID#Windows Out Of Band Releases-919945-2005/09/27-JonN
                        // Close window where service could complete at
                        // just the wrong time
                        && serviceController.Status != targetStatus)
                    {
                        WriteNonTerminatingError(serviceController, null,
                                                 errorId,
                                                 errorMessage,
                                                 ErrorCategory.OpenError);
                        return false;
                    }

                    string message = StringUtil.Format(resourceIdPending,
                        serviceController.ServiceName,
                        serviceController.DisplayName
                        );
                    // will throw PipelineStoppedException if user hit CTRL-C
                    WriteWarning(message);
                }
            }
        }

        
        /// <param name="serviceController">Service to start.</param>
        /// <returns>True if-and-only-if the service was started.</returns>
        internal bool DoStartService(ServiceController serviceController)
        {
            Exception exception = null;
            try
            {
                serviceController.Start();
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode != NativeMethods.ERROR_SERVICE_ALREADY_RUNNING)
                    exception = e;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is not Win32Exception eInner
                    || eInner.NativeErrorCode != NativeMethods.ERROR_SERVICE_ALREADY_RUNNING)
                {
                    exception = e;
                }
            }

            if (exception != null)
            {
                // This service refused to accept the start command,
                // so write a non-terminating error.
                WriteNonTerminatingError(serviceController,
                    exception,
                    "CouldNotStartService",
                    ServiceResources.CouldNotStartService,
                    ErrorCategory.OpenError);
                return false;
            }

            // ServiceController.Start will return
            // before the service is actually started.
            if (!DoWaitForStatus(
                serviceController,
                ServiceControllerStatus.Running,
                ServiceControllerStatus.StartPending,
                ServiceResources.StartingService,
                "StartServiceFailed",
                ServiceResources.StartServiceFailed))
            {
                return false;
            }

            return true;
        }

        
        /// <param name="serviceController">Service to stop.</param>
        /// <param name="force">Stop dependent services.</param>
        /// <param name="waitForServiceToStop"></param>
        /// <returns>True if-and-only-if the service was stopped.</returns>
        internal List<ServiceController> DoStopService(ServiceController serviceController, bool force, bool waitForServiceToStop)
        {
            // Ignore ServiceController.CanStop.  CanStop will be set false
            // if the service is not running, but this is not an error.

            List<ServiceController> stoppedServices = new();
            ServiceController[] dependentServices = null;

            try
            {
                dependentServices = serviceController.DependentServices;
            }
            catch (Win32Exception e)
            {
                WriteNonTerminatingError(serviceController, e,
                    "CouldNotAccessDependentServices",
                    ServiceResources.CouldNotAccessDependentServices,
                    ErrorCategory.InvalidOperation);
            }
            catch (InvalidOperationException e)
            {
                WriteNonTerminatingError(serviceController, e,
                    "CouldNotAccessDependentServices",
                    ServiceResources.CouldNotAccessDependentServices,
                    ErrorCategory.InvalidOperation);
            }

            if (!force)
            {
                if ((dependentServices != null)
                    && (dependentServices.Length > 0))
                {
                    // Check if all dependent services are stopped
                    if (!HaveAllDependentServicesStopped(dependentServices))
                    {
                        // This service has dependent services
                        //  and the force flag is not specified.
                        //  Add a non-critical error for it.
                        WriteNonTerminatingError(serviceController,
                        null,
                        "ServiceHasDependentServices",
                        ServiceResources.ServiceHasDependentServices,
                        ErrorCategory.InvalidOperation);

                        return stoppedServices;
                    }
                }
            }

            if (dependentServices != null)
            {
                foreach (ServiceController service in dependentServices)
                {
                    if ((service.Status == ServiceControllerStatus.Running ||
                        service.Status == ServiceControllerStatus.StartPending) &&
                        service.CanStop)
                    {
                        stoppedServices.Add(service);
                    }
                }
            }

            Exception exception = null;
            try
            {
                serviceController.Stop();
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode != NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                    exception = e;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is not Win32Exception eInner
                    || eInner.NativeErrorCode != NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    exception = e;
                }
            }

            if (exception != null)
            {
                // This service refused to accept the stop command,
                // so write a non-terminating error.
                WriteNonTerminatingError(serviceController,
                    exception,
                    "CouldNotStopService",
                    ServiceResources.CouldNotStopService,
                    ErrorCategory.CloseError);
                RemoveNotStoppedServices(stoppedServices);
                return stoppedServices;
            }

            // ServiceController.Stop will return
            //  before the service is actually stopped.
            if (waitForServiceToStop)
            {
                if (!DoWaitForStatus(
                    serviceController,
                    ServiceControllerStatus.Stopped,
                    ServiceControllerStatus.StopPending,
                    ServiceResources.StoppingService,
                    "StopServiceFailed",
                    ServiceResources.StopServiceFailed))
                {
                    RemoveNotStoppedServices(stoppedServices);
                    return stoppedServices;
                }
            }

            RemoveNotStoppedServices(stoppedServices);
            if ((serviceController.Status.Equals(ServiceControllerStatus.Stopped)) || (serviceController.Status.Equals(ServiceControllerStatus.StopPending)))
            {
                stoppedServices.Add(serviceController);
            }

            return stoppedServices;
        }

        
        /// <param name="dependentServices"></param>
        /// <returns>
        /// True if all dependent services are stopped
        /// False if not all dependent services are stopped
        /// </returns>
        private static bool HaveAllDependentServicesStopped(ServiceController[] dependentServices)
        {
            return Array.TrueForAll(dependentServices, static service => service.Status == ServiceControllerStatus.Stopped);
        }

        
        /// <param name="services">A list of services.</param>
        internal void RemoveNotStoppedServices(List<ServiceController> services)
        {
            // You shall not modify a collection during enumeration.
            services.RemoveAll(service =>
                service.Status != ServiceControllerStatus.Stopped &&
                service.Status != ServiceControllerStatus.StopPending);
        }

        
        /// <param name="serviceController">Service to pause.</param>
        /// <returns>True if-and-only-if the service was paused.</returns>
        internal bool DoPauseService(ServiceController serviceController)
        {
            Exception exception = null;
            bool serviceNotRunning = false;
            try
            {
                serviceController.Pause();
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    serviceNotRunning = true;
                }

                exception = e;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is Win32Exception eInner
                    && eInner.NativeErrorCode == NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    serviceNotRunning = true;
                }

                exception = e;
            }

            if (exception != null)
            {
                // This service refused to accept the pause command,
                // so write a non-terminating error.
                string resourceIdAndErrorId = ServiceResources.CouldNotSuspendService;
                if (serviceNotRunning)
                {
                    WriteNonTerminatingError(serviceController,
                        exception,
                        "CouldNotSuspendServiceNotRunning",
                        ServiceResources.CouldNotSuspendServiceNotRunning,
                        ErrorCategory.CloseError);
                }
                else if (!serviceController.CanPauseAndContinue)
                {
                    WriteNonTerminatingError(serviceController,
                        exception,
                        "CouldNotSuspendServiceNotSupported",
                        ServiceResources.CouldNotSuspendServiceNotSupported,
                        ErrorCategory.CloseError);
                }

                WriteNonTerminatingError(serviceController,
                    exception,
                    "CouldNotSuspendService",
                    ServiceResources.CouldNotSuspendService,
                    ErrorCategory.CloseError);

                return false;
            }

            // ServiceController.Pause will return
            // before the service is actually paused.
            if (!DoWaitForStatus(
                serviceController,
                ServiceControllerStatus.Paused,
                ServiceControllerStatus.PausePending,
                ServiceResources.SuspendingService,
                "SuspendServiceFailed",
                ServiceResources.SuspendServiceFailed))
            {
                return false;
            }

            return true;
        }

        
        /// <param name="serviceController">Service to resume.</param>
        /// <returns>True if-and-only-if the service was resumed.</returns>
        internal bool DoResumeService(ServiceController serviceController)
        {
            Exception exception = null;
            bool serviceNotRunning = false;
            try
            {
                serviceController.Continue();
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    serviceNotRunning = true;
                }

                exception = e;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is Win32Exception eInner
                    && eInner.NativeErrorCode == NativeMethods.ERROR_SERVICE_NOT_ACTIVE)
                {
                    serviceNotRunning = true;
                }

                exception = e;
            }

            if (exception != null)
            {
                // This service refused to accept the continue command,
                // so write a non-terminating error.
                if (serviceNotRunning)
                {
                    WriteNonTerminatingError(serviceController,
                        exception,
                        "CouldNotResumeServiceNotRunning",
                        ServiceResources.CouldNotResumeServiceNotRunning,
                        ErrorCategory.CloseError);
                }
                else if (!serviceController.CanPauseAndContinue)
                {
                    WriteNonTerminatingError(serviceController,
                        exception,
                        "CouldNotResumeServiceNotSupported",
                        ServiceResources.CouldNotResumeServiceNotSupported,
                        ErrorCategory.CloseError);
                }

                WriteNonTerminatingError(serviceController,
                    exception,
                    "CouldNotResumeService",
                    ServiceResources.CouldNotResumeService,
                    ErrorCategory.CloseError);

                return false;
            }

            // ServiceController.Continue will return
            // before the service is actually continued.
            if (!DoWaitForStatus(
                serviceController,
                ServiceControllerStatus.Running,
                ServiceControllerStatus.ContinuePending,
                ServiceResources.ResumingService,
                "ResumeServiceFailed",
                ServiceResources.ResumeServiceFailed))
            {
                return false;
            }

            return true;
        }
        #endregion Internal
    }
    #endregion ServiceOperationBaseCommand

    #region StopServiceCommand

    
    /// <remarks>
    /// Note that the services will be sorted before being stopped.
    /// PM confirms that this is OK.
    /// </remarks>
    [Cmdlet(VerbsLifecycle.Stop, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097052")]
    [OutputType(typeof(ServiceController))]
    public sealed class StopServiceCommand : ServiceOperationBaseCommand
    {
        
        [Parameter]
        public SwitchParameter Force { get; set; }

        
        [Parameter]
        public SwitchParameter NoWait { get; set; }

        
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                List<ServiceController> stoppedServices = DoStopService(serviceController, Force, !NoWait);

                if (PassThru && stoppedServices.Count > 0)
                {
                    foreach (ServiceController service in stoppedServices)
                    {
                        WriteObject(service);
                    }
                }
            }
        }
    }
    #endregion StopServiceCommand

    #region StartServiceCommand
    
    [Cmdlet(VerbsLifecycle.Start, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097051")]
    [OutputType(typeof(ServiceController))]
    public sealed class StartServiceCommand : ServiceOperationBaseCommand
    {
        
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoStartService(serviceController))
                {
                    if (PassThru)
                        WriteObject(serviceController);
                }
            }
        }
    }
    #endregion StartServiceCommand

    #region SuspendServiceCommand
    
    [Cmdlet(VerbsLifecycle.Suspend, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097053")]
    [OutputType(typeof(ServiceController))]
    public sealed class SuspendServiceCommand : ServiceOperationBaseCommand
    {
        
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoPauseService(serviceController))
                {
                    if (PassThru)
                        WriteObject(serviceController);
                }
            }
        }
    }
    #endregion SuspendServiceCommand

    #region ResumeServiceCommand
    
    [Cmdlet(VerbsLifecycle.Resume, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097150")]
    [OutputType(typeof(ServiceController))]
    public sealed class ResumeServiceCommand : ServiceOperationBaseCommand
    {
        
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                if (DoResumeService(serviceController))
                {
                    if (PassThru)
                        WriteObject(serviceController);
                }
            }
        }
    }
    #endregion ResumeServiceCommand

    #region RestartServiceCommand

    
    [Cmdlet(VerbsLifecycle.Restart, "Service", DefaultParameterSetName = "InputObject", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097059")]
    [OutputType(typeof(ServiceController))]
    public sealed class RestartServiceCommand : ServiceOperationBaseCommand
    {
        
        [Parameter]
        public SwitchParameter Force { get; set; }

        
        protected override void ProcessRecord()
        {
            foreach (ServiceController serviceController in MatchingServices())
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(serviceController))
                {
                    continue;
                }

                // Set the NoWait parameter to false since we are not adding this switch to this cmdlet.
                List<ServiceController> stoppedServices = DoStopService(serviceController, Force, true);

                if (stoppedServices.Count > 0)
                {
                    foreach (ServiceController service in stoppedServices)
                    {
                        if (DoStartService(service))
                        {
                            if (PassThru)
                                WriteObject(service);
                        }
                    }
                }
            }
        }
    }
    #endregion RestartServiceCommand

    #region SetServiceCommand

    
    [Cmdlet(VerbsCommon.Set, "Service", SupportsShouldProcess = true, DefaultParameterSetName = "Name",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097148", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(typeof(ServiceController))]
    public class SetServiceCommand : ServiceOperationBaseCommand
    {
        #region Parameters

        
        /// <value></value>
        [Parameter(Mandatory = true, ParameterSetName = "Name", Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("ServiceName", "SN")]
        public new string Name
        {
            get
            {
                return serviceName;
            }

            set
            {
                serviceName = value;
            }
        }

        internal string serviceName = null;

        
        [Parameter(Mandatory = true, ParameterSetName = "InputObject", Position = 0, ValueFromPipeline = true)]
        public new ServiceController InputObject { get; set; }

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("DN")]
        public new string DisplayName
        {
            get
            {
                return displayName;
            }

            set
            {
                displayName = value;
            }
        }

        internal string displayName = null;

        
        /// <value></value>
        [Parameter]
        [Credential()]
        public PSCredential Credential { get; set; }

        
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        internal string description = null;

        
        [Parameter]
        [Alias("StartMode", "SM", "ST", "StartType")]
        [ValidateNotNullOrEmpty]
        public ServiceStartupType StartupType
        {
            get
            {
                return startupType;
            }

            set
            {
                startupType = value;
            }
        }

        // We set the initial value to an invalid value so that we can
        // distinguish when this is and is not set.
        internal ServiceStartupType startupType = ServiceStartupType.InvalidValue;

        
        [Parameter]
        [Alias("sd")]
        [ValidateNotNullOrEmpty]
        public string SecurityDescriptorSddl
        {
            get;
            set;
        }

        
        [Parameter]
        [ValidateSetAttribute(new string[] { "Running", "Stopped", "Paused" })]
        public string Status
        {
            get
            {
                return serviceStatus;
            }

            set
            {
                serviceStatus = value;
            }
        }

        internal string serviceStatus = null;

        
        [Parameter]
        public SwitchParameter Force { get; set; }

        
        // This has been shadowed from base class and removed parameter tag to fix gcm "Set-Service" -syntax
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public new string[] Include
        {
            get
            {
                return include;
            }

            set
            {
                include = null;
            }
        }

        internal new string[] include = null;

        
        // This has been shadowed from base class and removed parameter tag to fix gcm "Set-Service" -syntax
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public new string[] Exclude
        {
            get
            {
                return exclude;
            }

            set
            {
                exclude = null;
            }
        }

        internal new string[] exclude = null;
        #endregion Parameters

        #region Overrides
        
        protected override void ProcessRecord()
        {
            ServiceController service = null;
            IntPtr password = IntPtr.Zero;
            bool objServiceShouldBeDisposed = false;

            try
            {
                if (InputObject != null)
                {
                    service = InputObject;
                    Name = service.ServiceName;
                    objServiceShouldBeDisposed = false;
                }
                else
                {
                    service = new ServiceController(serviceName);
                    objServiceShouldBeDisposed = true;
                }

                Diagnostics.Assert(!string.IsNullOrEmpty(Name), "null ServiceName");

                // "new ServiceController" will succeed even if
                // there is no such service.  This checks whether
                // the service actually exists.
                string unusedByDesign = service.DisplayName;
            }
            catch (ArgumentException ex)
            {
                // cannot use WriteNonterminatingError as service is null
                ErrorRecord er = new(ex, "ArgumentException", ErrorCategory.ObjectNotFound, Name);
                WriteError(er);
                return;
            }
            catch (InvalidOperationException ex)
            {
                // cannot use WriteNonterminatingError as service is null
                ErrorRecord er = new(ex, "InvalidOperationException", ErrorCategory.ObjectNotFound, Name);
                WriteError(er);
                return;
            }

            try // In finally we ensure dispose, if object not pipelined.
            {
                // confirm the operation first
                // this is always false if WhatIf is set
                if (!ShouldProcessServiceOperation(service))
                {
                    return;
                }

                NakedWin32Handle hScManager = IntPtr.Zero;
                NakedWin32Handle hService = IntPtr.Zero;
                IntPtr delayedAutoStartInfoBuffer = IntPtr.Zero;
                try
                {
                    hScManager = NativeMethods.OpenSCManagerW(
                        string.Empty,
                        null,
                        NativeMethods.SC_MANAGER_CONNECT
                        );

                    if (hScManager == IntPtr.Zero)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "FailToOpenServiceControlManager",
                            ServiceResources.FailToOpenServiceControlManager,
                            ErrorCategory.PermissionDenied);
                        return;
                    }

                    var access = NativeMethods.SERVICE_CHANGE_CONFIG;
                    if (!string.IsNullOrEmpty(SecurityDescriptorSddl))
                        access |= NativeMethods.WRITE_DAC | NativeMethods.WRITE_OWNER;

                    hService = NativeMethods.OpenServiceW(
                        hScManager,
                        Name,
                        access
                        );

                    if (hService == IntPtr.Zero)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "CouldNotSetService",
                            ServiceResources.CouldNotSetService,
                            ErrorCategory.PermissionDenied);
                        return;
                    }
                    // Modify startup type or display name or credential
                    if (!string.IsNullOrEmpty(DisplayName)
                        || StartupType != ServiceStartupType.InvalidValue || Credential != null)
                    {
                        DWORD dwStartType = NativeMethods.SERVICE_NO_CHANGE;
                        if (!NativeMethods.TryGetNativeStartupType(StartupType, out dwStartType))
                        {
                            WriteNonTerminatingError(StartupType.ToString(), "Set-Service", Name,
                                new ArgumentException(), "CouldNotSetService",
                                ServiceResources.UnsupportedStartupType,
                                ErrorCategory.InvalidArgument);
                            return;
                        }

                        string username = null;
                        if (Credential != null)
                        {
                            username = Credential.UserName;
                            password = Marshal.SecureStringToCoTaskMemUnicode(Credential.Password);
                        }

                        bool succeeded = NativeMethods.ChangeServiceConfigW(
                            hService,
                            NativeMethods.SERVICE_NO_CHANGE,
                            dwStartType,
                            NativeMethods.SERVICE_NO_CHANGE,
                            null,
                            null,
                            IntPtr.Zero,
                            null,
                            username,
                            password,
                            DisplayName
                            );
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Win32Exception exception = new(lastError);
                            WriteNonTerminatingError(
                                service,
                                exception,
                                "CouldNotSetService",
                                ServiceResources.CouldNotSetService,
                                ErrorCategory.PermissionDenied);
                            return;
                        }
                    }

                    NativeMethods.SERVICE_DESCRIPTIONW sd = new();
                    sd.lpDescription = Description;
                    int size = Marshal.SizeOf(sd);
                    IntPtr buffer = Marshal.AllocCoTaskMem(size);
                    Marshal.StructureToPtr(sd, buffer, false);

                    bool status = NativeMethods.ChangeServiceConfig2W(
                        hService,
                        NativeMethods.SERVICE_CONFIG_DESCRIPTION,
                        buffer);

                    if (!status)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "CouldNotSetServiceDescription",
                            ServiceResources.CouldNotSetServiceDescription,
                            ErrorCategory.PermissionDenied);
                    }

                    // Set the delayed auto start
                    NativeMethods.SERVICE_DELAYED_AUTO_START_INFO ds = new();
                    ds.fDelayedAutostart = StartupType == ServiceStartupType.AutomaticDelayedStart;
                    size = Marshal.SizeOf(ds);
                    delayedAutoStartInfoBuffer = Marshal.AllocCoTaskMem(size);
                    Marshal.StructureToPtr(ds, delayedAutoStartInfoBuffer, false);

                    status = NativeMethods.ChangeServiceConfig2W(
                        hService,
                        NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                        delayedAutoStartInfoBuffer);

                    if (!status)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            Name,
                            DisplayName,
                            Name,
                            exception,
                            "CouldNotSetServiceDelayedAutoStart",
                            ServiceResources.CouldNotSetServiceDelayedAutoStart,
                            ErrorCategory.PermissionDenied);
                    }

                    // Handle the '-Status' parameter
                    if (!string.IsNullOrEmpty(Status))
                    {
                        if (Status.Equals("Running", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!service.Status.Equals(ServiceControllerStatus.Running))
                            {
                                if (service.Status.Equals(ServiceControllerStatus.Paused))
                                    // resume service
                                    DoResumeService(service);
                                else
                                    // start service
                                    DoStartService(service);
                            }
                        }
                        else if (Status.Equals("Stopped", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!service.Status.Equals(ServiceControllerStatus.Stopped))
                            {
                                // Check for the dependent services as set-service dont have force parameter
                                ServiceController[] dependentServices = service.DependentServices;

                                if ((!Force) && (dependentServices != null) && (dependentServices.Length > 0))
                                {
                                    WriteNonTerminatingError(service, null, "ServiceHasDependentServicesNoForce", ServiceResources.ServiceHasDependentServicesNoForce, ErrorCategory.InvalidOperation);
                                    return;
                                }

                                // Stop service, pass 'true' to the force parameter as we have already checked for the dependent services.
                                DoStopService(service, Force, waitForServiceToStop: true);
                            }
                        }
                        else if (Status.Equals("Paused", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!service.Status.Equals(ServiceControllerStatus.Paused))
                            {
                                DoPauseService(service);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(SecurityDescriptorSddl))
                    {
                        SetServiceSecurityDescriptor(service, SecurityDescriptorSddl, hService);
                    }

                    if (PassThru.IsPresent)
                    {
                        // To display the service, refreshing the service would not show the display name after updating
                        ServiceController displayservice = new(Name);
                        WriteObject(displayservice);
                    }
                }
                finally
                {
                    if (delayedAutoStartInfoBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(delayedAutoStartInfoBuffer);
                    }

                    if (hService != IntPtr.Zero)
                    {
                        bool succeeded = NativeMethods.CloseServiceHandle(hService);
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Win32Exception exception = new(lastError);
                            WriteNonTerminatingError(
                                service,
                                exception,
                                "CouldNotSetServiceDescription",
                                ServiceResources.CouldNotSetServiceDescription,
                                ErrorCategory.PermissionDenied);
                        }
                    }

                    if (hScManager != IntPtr.Zero)
                    {
                        bool succeeded = NativeMethods.CloseServiceHandle(hScManager);
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Win32Exception exception = new(lastError);
                            WriteNonTerminatingError(
                                service,
                                exception,
                                "CouldNotSetServiceDescription",
                                ServiceResources.CouldNotSetServiceDescription,
                                ErrorCategory.PermissionDenied);
                        }
                    }
                }
            }
            finally
            {
                if (password != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(password);
                }

                if (objServiceShouldBeDisposed)
                {
                    service.Dispose();
                }
            }
        }
        #endregion Overrides

    }
    #endregion SetServiceCommand

    #region NewServiceCommand
    
    [Cmdlet(VerbsCommon.New, "Service", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096905")]
    [OutputType(typeof(ServiceController))]
    public class NewServiceCommand : ServiceBaseCommand
    {
        #region Parameters
        
        /// <value></value>
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("ServiceName")]
        public string Name
        {
            get { return serviceName; }

            set { serviceName = value; }
        }

        internal string serviceName = null;

        
        /// <value></value>
        [Parameter(Position = 1, Mandatory = true)]
        [Alias("Path")]
        public string BinaryPathName
        {
            get { return binaryPathName; }

            set { binaryPathName = value; }
        }

        internal string binaryPathName = null;

        
        /// <value></value>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string DisplayName
        {
            get { return displayName; }

            set { displayName = value; }
        }

        internal string displayName = null;

        
        /// <value></value>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Description
        {
            get { return description; }

            set { description = value; }
        }

        internal string description = null;

        
        /// <value></value>
        [Parameter]
        public ServiceStartupType StartupType
        {
            get { return startupType; }

            set { startupType = value; }
        }

        internal ServiceStartupType startupType = ServiceStartupType.Automatic;

        
        /// <value></value>
        [Parameter]
        [Credential()]
        public PSCredential Credential
        {
            get { return credential; }

            set { credential = value; }
        }

        internal PSCredential credential = null;

        
        [Parameter]
        [Alias("sd")]
        [ValidateNotNullOrEmpty]
        public string SecurityDescriptorSddl
        {
            get;
            set;
        }

        
        /// <value></value>
        [Parameter]
        public string[] DependsOn
        {
            get { return dependsOn; }

            set { dependsOn = value; }
        }

        internal string[] dependsOn = null;
        #endregion Parameters

        #region Overrides
        
        protected override void BeginProcessing()
        {
            ServiceController service = null;
            Diagnostics.Assert(!string.IsNullOrEmpty(Name),
                "null ServiceName");
            Diagnostics.Assert(!string.IsNullOrEmpty(BinaryPathName),
                "null BinaryPathName");

            // confirm the operation first
            // this is always false if WhatIf is set
            if (!ShouldProcessServiceOperation(DisplayName ?? string.Empty, Name))
            {
                return;
            }

            // Connect to the service controller
            NakedWin32Handle hScManager = IntPtr.Zero;
            NakedWin32Handle hService = IntPtr.Zero;
            IntPtr password = IntPtr.Zero;
            IntPtr delayedAutoStartInfoBuffer = IntPtr.Zero;
            try
            {
                hScManager = NativeMethods.OpenSCManagerW(
                    null,
                    null,
                    NativeMethods.SC_MANAGER_CONNECT | NativeMethods.SC_MANAGER_CREATE_SERVICE
                    );
                if (hScManager == IntPtr.Zero)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    Win32Exception exception = new(lastError);
                    WriteNonTerminatingError(
                        Name,
                        DisplayName,
                        Name,
                        exception,
                        "CouldNotNewService",
                        ServiceResources.CouldNotNewService,
                        ErrorCategory.PermissionDenied);
                    return;
                }

                if (!NativeMethods.TryGetNativeStartupType(StartupType, out DWORD dwStartType))
                {
                    WriteNonTerminatingError(StartupType.ToString(), "New-Service", Name,
                        new ArgumentException(), "CouldNotNewService",
                        ServiceResources.UnsupportedStartupType,
                        ErrorCategory.InvalidArgument);
                    return;
                }
                // set up the double-null-terminated lpDependencies parameter
                IntPtr lpDependencies = IntPtr.Zero;
                if (DependsOn != null)
                {
                    int numchars = 1; // final null
                    foreach (string dependedOn in DependsOn)
                    {
                        numchars += dependedOn.Length + 1;
                    }

                    char[] doubleNullArray = new char[numchars];
                    int pos = 0;
                    foreach (string dependedOn in DependsOn)
                    {
                        Array.Copy(
                            dependedOn.ToCharArray(), 0,
                            doubleNullArray, pos,
                            dependedOn.Length
                            );
                        pos += dependedOn.Length;
                        doubleNullArray[pos++] = (char)0; // null terminator
                    }

                    doubleNullArray[pos++] = (char)0; // double-null terminator
                    Diagnostics.Assert(pos == numchars, "lpDependencies build error");
                    lpDependencies = Marshal.AllocHGlobal(
                        numchars * Marshal.SystemDefaultCharSize);
                    Marshal.Copy(doubleNullArray, 0, lpDependencies, numchars);
                }

                // set up the Credential parameter
                string username = null;
                if (Credential != null)
                {
                    username = Credential.UserName;
                    password = Marshal.SecureStringToCoTaskMemUnicode(Credential.Password);
                }

                // Create the service
                hService = NativeMethods.CreateServiceW(
                    hScManager,
                    Name,
                    DisplayName,
                    NativeMethods.SERVICE_CHANGE_CONFIG | NativeMethods.WRITE_DAC | NativeMethods.WRITE_OWNER,
                    NativeMethods.SERVICE_WIN32_OWN_PROCESS,
                    dwStartType,
                    NativeMethods.SERVICE_ERROR_NORMAL,
                    BinaryPathName,
                    null,
                    null,
                    lpDependencies,
                    username,
                    password
                    );
                if (hService == IntPtr.Zero)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    Win32Exception exception = new(lastError);
                    WriteNonTerminatingError(
                        Name,
                        DisplayName,
                        Name,
                        exception,
                        "CouldNotNewService",
                        ServiceResources.CouldNotNewService,
                        ErrorCategory.PermissionDenied);
                    return;
                }

                // Set the service description
                NativeMethods.SERVICE_DESCRIPTIONW sd = new();
                sd.lpDescription = Description;
                int size = Marshal.SizeOf(sd);
                IntPtr buffer = Marshal.AllocCoTaskMem(size);
                Marshal.StructureToPtr(sd, buffer, false);

                bool succeeded = NativeMethods.ChangeServiceConfig2W(
                    hService,
                    NativeMethods.SERVICE_CONFIG_DESCRIPTION,
                    buffer);

                if (!succeeded)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    Win32Exception exception = new(lastError);
                    WriteNonTerminatingError(
                        Name,
                        DisplayName,
                        Name,
                        exception,
                        "CouldNotNewServiceDescription",
                        ServiceResources.CouldNotNewServiceDescription,
                        ErrorCategory.PermissionDenied);
                }

                // Set the delayed auto start
                if (StartupType == ServiceStartupType.AutomaticDelayedStart)
                {
                    NativeMethods.SERVICE_DELAYED_AUTO_START_INFO ds = new();
                    ds.fDelayedAutostart = true;
                    size = Marshal.SizeOf(ds);
                    delayedAutoStartInfoBuffer = Marshal.AllocCoTaskMem(size);
                    Marshal.StructureToPtr(ds, delayedAutoStartInfoBuffer, false);

                    succeeded = NativeMethods.ChangeServiceConfig2W(
                        hService,
                        NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                        delayedAutoStartInfoBuffer);

                    if (!succeeded)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            Name,
                            DisplayName,
                            Name,
                            exception,
                            "CouldNotNewServiceDelayedAutoStart",
                            ServiceResources.CouldNotNewServiceDelayedAutoStart,
                            ErrorCategory.PermissionDenied);
                    }
                }

                // write the ServiceController for the new service
                service = new ServiceController(Name);

                if (!string.IsNullOrEmpty(SecurityDescriptorSddl))
                {
                    SetServiceSecurityDescriptor(service, SecurityDescriptorSddl, hService);
                }

                WriteObject(service);
            }
            finally
            {
                if (delayedAutoStartInfoBuffer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(delayedAutoStartInfoBuffer);
                }

                if (password != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(password);
                }

                if (hService != IntPtr.Zero)
                {
                    bool succeeded = NativeMethods.CloseServiceHandle(hService);
                    if (!succeeded)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            Name,
                            DisplayName,
                            Name,
                            exception,
                            "CouldNotNewServiceDescription",
                            ServiceResources.CouldNotNewServiceDescription,
                            ErrorCategory.PermissionDenied);
                    }
                }

                if (hScManager != IntPtr.Zero)
                {
                    bool succeeded = NativeMethods.CloseServiceHandle(hScManager);
                    if (!succeeded)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            Name,
                            DisplayName,
                            Name,
                            exception,
                            "CouldNotNewServiceDescription",
                            ServiceResources.CouldNotNewServiceDescription,
                            ErrorCategory.PermissionDenied);
                    }
                }
            }
        }
        #endregion Overrides
    }
    #endregion NewServiceCommand

    #region RemoveServiceCommand
    
    [Cmdlet(VerbsCommon.Remove, "Service", SupportsShouldProcess = true, DefaultParameterSetName = "Name", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2248980")]
    public class RemoveServiceCommand : ServiceBaseCommand
    {
        #region Parameters

        
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Name")]
        [Alias("ServiceName", "SN")]
        public string Name { get; set; }

        
        [Parameter(ValueFromPipeline = true, ParameterSetName = "InputObject")]
        public ServiceController InputObject { get; set; }

        #endregion Parameters

        #region Overrides
        
        protected override void ProcessRecord()
        {
            ServiceController service = null;
            bool objServiceShouldBeDisposed = false;
            try
            {
                if (InputObject != null)
                {
                    service = InputObject;
                    Name = service.ServiceName;
                    objServiceShouldBeDisposed = false;
                }
                else
                {
                    service = new ServiceController(Name);
                    objServiceShouldBeDisposed = true;
                }

                Diagnostics.Assert(!string.IsNullOrEmpty(Name), "null ServiceName");

                // "new ServiceController" will succeed even if there is no such service.
                // This checks whether the service actually exists.
                string unusedByDesign = service.DisplayName;
            }
            catch (ArgumentException ex)
            {
                // Cannot use WriteNonterminatingError as service is null
                ErrorRecord er = new(ex, "ArgumentException", ErrorCategory.ObjectNotFound, Name);
                WriteError(er);
                return;
            }
            catch (InvalidOperationException ex)
            {
                // Cannot use WriteNonterminatingError as service is null
                ErrorRecord er = new(ex, "InvalidOperationException", ErrorCategory.ObjectNotFound, Name);
                WriteError(er);
                return;
            }

            try // In finally we ensure dispose, if object not pipelined.
            {
                // Confirm the operation first.
                // This is always false if WhatIf is set.
                if (!ShouldProcessServiceOperation(service))
                {
                    return;
                }

                NakedWin32Handle hScManager = IntPtr.Zero;
                NakedWin32Handle hService = IntPtr.Zero;
                try
                {
                    hScManager = NativeMethods.OpenSCManagerW(
                        lpMachineName: string.Empty,
                        lpDatabaseName: null,
                        dwDesiredAccess: NativeMethods.SC_MANAGER_ALL_ACCESS
                        );
                    if (hScManager == IntPtr.Zero)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteObject(exception);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "FailToOpenServiceControlManager",
                            ServiceResources.FailToOpenServiceControlManager,
                            ErrorCategory.PermissionDenied);
                        return;
                    }

                    hService = NativeMethods.OpenServiceW(
                        hScManager,
                        Name,
                        NativeMethods.SERVICE_DELETE
                        );
                    if (hService == IntPtr.Zero)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "CouldNotRemoveService",
                            ServiceResources.CouldNotRemoveService,
                            ErrorCategory.PermissionDenied);
                        return;
                    }

                    bool status = NativeMethods.DeleteService(hService);

                    if (!status)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        Win32Exception exception = new(lastError);
                        WriteNonTerminatingError(
                            service,
                            exception,
                            "CouldNotRemoveService",
                            ServiceResources.CouldNotRemoveService,
                            ErrorCategory.PermissionDenied);
                    }
                }
                finally
                {
                    if (hService != IntPtr.Zero)
                    {
                        bool succeeded = NativeMethods.CloseServiceHandle(hService);
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Diagnostics.Assert(lastError != 0, "ErrorCode not success");
                        }
                    }

                    if (hScManager != IntPtr.Zero)
                    {
                        bool succeeded = NativeMethods.CloseServiceHandle(hScManager);
                        if (!succeeded)
                        {
                            int lastError = Marshal.GetLastWin32Error();
                            Diagnostics.Assert(lastError != 0, "ErrorCode not success");
                        }
                    }
                }
            }
            finally
            {
                if (objServiceShouldBeDisposed)
                {
                    service.Dispose();
                }
            }
        }
        #endregion Overrides
    }
    #endregion RemoveServiceCommand

    #region ServiceCommandException
    
    public class ServiceCommandException : SystemException
    {
        #region ctors
        
        /// <returns>Doesn't return.</returns>
        public ServiceCommandException()
            : base()
        {
            throw new NotImplementedException();
        }

        
        /// <param name="message"></param>
        /// <returns>Constructed object.</returns>
        public ServiceCommandException(string message)
            : base(message)
        {
        }

        
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ServiceCommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion ctors

        #region Serialization
        
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <returns>Constructed object.</returns>
        [Obsolete("Legacy serialization support is deprecated since .NET 8, hence this method is now marked as obsolete", DiagnosticId = "SYSLIB0051")]
        protected ServiceCommandException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Serialization

        #region Properties
        
        /// <value></value>
        public string ServiceName
        {
            get { return _serviceName; }

            set { _serviceName = value; }
        }

        private string _serviceName = string.Empty;
        #endregion Properties
    }
    #endregion ServiceCommandException

    #region NativeMethods
    internal static class NativeMethods
    {
        // from winuser.h
        internal const int ERROR_SERVICE_ALREADY_RUNNING = 1056;
        internal const int ERROR_SERVICE_NOT_ACTIVE = 1062;
        internal const int ERROR_INSUFFICIENT_BUFFER = 122;
        internal const DWORD ERROR_ACCESS_DENIED = 0x5;
        internal const DWORD SC_MANAGER_CONNECT = 1;
        internal const DWORD SC_MANAGER_CREATE_SERVICE = 2;
        internal const DWORD SC_MANAGER_ALL_ACCESS = 0xf003f;
        internal const DWORD SERVICE_QUERY_CONFIG = 1;
        internal const DWORD SERVICE_CHANGE_CONFIG = 2;
        internal const DWORD SERVICE_DELETE = 0x10000;
        internal const DWORD SERVICE_NO_CHANGE = 0xffffffff;
        internal const DWORD SERVICE_AUTO_START = 0x2;
        internal const DWORD SERVICE_DEMAND_START = 0x3;
        internal const DWORD SERVICE_DISABLED = 0x4;
        internal const DWORD SERVICE_CONFIG_DESCRIPTION = 1;
        internal const DWORD SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3;
        internal const DWORD SERVICE_CONFIG_SERVICE_SID_INFO = 5;
        internal const DWORD WRITE_DAC = 262144;
        internal const DWORD WRITE_OWNER = 524288;
        internal const DWORD SERVICE_WIN32_OWN_PROCESS = 0x10;
        internal const DWORD SERVICE_ERROR_NORMAL = 1;

        // from winnt.h
        [DllImport(PinvokeDllNames.OpenSCManagerWDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        NakedWin32Handle OpenSCManagerW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpMachineName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpDatabaseName,
            DWORD dwDesiredAccess
            );

        [DllImport(PinvokeDllNames.OpenServiceWDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        NakedWin32Handle OpenServiceW(
            NakedWin32Handle hSCManager,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpServiceName,
            DWORD dwDesiredAccess
            );

        [DllImport(PinvokeDllNames.QueryServiceConfigDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool QueryServiceConfigW(
            NakedWin32Handle hSCManager,
            IntPtr lpServiceConfig,
            DWORD cbBufSize,
            out DWORD pcbBytesNeeded
            );

        [DllImport(PinvokeDllNames.QueryServiceConfig2DllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool QueryServiceConfig2W(
            NakedWin32Handle hService,
            DWORD dwInfoLevel,
            IntPtr lpBuffer,
            DWORD cbBufSize,
            out DWORD pcbBytesNeeded
            );

        [DllImport(PinvokeDllNames.CloseServiceHandleDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool CloseServiceHandle(
            NakedWin32Handle hSCManagerOrService
            );

        [DllImport(PinvokeDllNames.DeleteServiceDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool DeleteService(
            NakedWin32Handle hService
            );

        [DllImport(PinvokeDllNames.ChangeServiceConfigWDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool ChangeServiceConfigW(
            NakedWin32Handle hService,
            DWORD dwServiceType,
            DWORD dwStartType,
            DWORD dwErrorControl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpBinaryPathName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpDependencies,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpServiceStartName,
            [In] IntPtr lpPassword,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpDisplayName
            );

        [DllImport(PinvokeDllNames.ChangeServiceConfig2WDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        bool ChangeServiceConfig2W(
            NakedWin32Handle hService,
            DWORD dwInfoLevel,
            IntPtr lpInfo
            );

        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_DESCRIPTIONW
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string lpDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct QUERY_SERVICE_CONFIG
        {
            internal uint dwServiceType;
            internal uint dwStartType;
            internal uint dwErrorControl;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpBinaryPathName;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpLoadOrderGroup;
            internal uint dwTagId;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpDependencies;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpServiceStartName;
            [MarshalAs(UnmanagedType.LPWStr)] internal string lpDisplayName;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_DELAYED_AUTO_START_INFO
        {
            internal bool fDelayedAutostart;
        }

        [DllImport(PinvokeDllNames.CreateServiceWDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        NakedWin32Handle CreateServiceW(
            NakedWin32Handle hSCManager,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpServiceName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpDisplayName,
            DWORD dwDesiredAccess,
            DWORD dwServiceType,
            DWORD dwStartType,
            DWORD dwErrorControl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpBinaryPathName,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpLoadOrderGroup,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpdwTagId,
            [In] IntPtr lpDependencies,
            [In, MarshalAs(UnmanagedType.LPWStr)] string lpServiceStartName,
            [In] IntPtr lpPassword
        );

        [DllImport(PinvokeDllNames.SetServiceObjectSecurityDllName, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern
        bool SetServiceObjectSecurity(
            NakedWin32Handle hSCManager,
            System.Security.AccessControl.SecurityInfos dwSecurityInformation,
            byte[] lpSecurityDescriptor
            );

        internal static bool QueryServiceConfig(NakedWin32Handle hService, out NativeMethods.QUERY_SERVICE_CONFIG configStructure)
        {
            IntPtr lpBuffer = IntPtr.Zero;
            configStructure = default(NativeMethods.QUERY_SERVICE_CONFIG);
            DWORD bufferSize, bufferSizeNeeded = 0;
            bool status = NativeMethods.QueryServiceConfigW(
                hSCManager: hService,
                lpServiceConfig: lpBuffer,
                cbBufSize: 0,
                pcbBytesNeeded: out bufferSizeNeeded);

            if (!status && Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
            {
                return status;
            }

            try
            {
                lpBuffer = Marshal.AllocCoTaskMem((int)bufferSizeNeeded);
                bufferSize = bufferSizeNeeded;

                status = NativeMethods.QueryServiceConfigW(
                    hService,
                    lpBuffer,
                    bufferSize,
                    out bufferSizeNeeded);
                configStructure = (NativeMethods.QUERY_SERVICE_CONFIG)Marshal.PtrToStructure(lpBuffer, typeof(NativeMethods.QUERY_SERVICE_CONFIG));
            }
            finally
            {
                Marshal.FreeCoTaskMem(lpBuffer);
            }

            return status;
        }

        internal static bool QueryServiceConfig2<T>(NakedWin32Handle hService, DWORD infolevel, out T configStructure)
        {
            IntPtr lpBuffer = IntPtr.Zero;
            configStructure = default(T);
            DWORD bufferSize, bufferSizeNeeded = 0;

            bool status = NativeMethods.QueryServiceConfig2W(
                hService: hService,
                dwInfoLevel: infolevel,
                lpBuffer: lpBuffer,
                cbBufSize: 0,
                pcbBytesNeeded: out bufferSizeNeeded);

            if (!status && Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
            {
                return status;
            }

            try
            {
                lpBuffer = Marshal.AllocCoTaskMem((int)bufferSizeNeeded);
                bufferSize = bufferSizeNeeded;

                status = NativeMethods.QueryServiceConfig2W(
                    hService,
                    infolevel,
                    lpBuffer,
                    bufferSize,
                    out bufferSizeNeeded);
                configStructure = (T)Marshal.PtrToStructure(lpBuffer, typeof(T));
            }
            finally
            {
                Marshal.FreeCoTaskMem(lpBuffer);
            }

            return status;
        }

        
        /// <param name="StartupType">
        /// StartupType provided by the user.
        /// </param>
        /// <param name="dwStartType">
        /// Out parameter of the native win32 StartupType
        /// </param>
        /// <returns>
        /// If a supported StartupType is provided, funciton returns true, otherwise false.
        /// </returns>
        internal static bool TryGetNativeStartupType(ServiceStartupType StartupType, out DWORD dwStartType)
        {
            bool success = true;
            dwStartType = NativeMethods.SERVICE_NO_CHANGE;
            switch (StartupType)
            {
                case ServiceStartupType.Automatic:
                case ServiceStartupType.AutomaticDelayedStart:
                    dwStartType = NativeMethods.SERVICE_AUTO_START;
                    break;
                case ServiceStartupType.Manual:
                    dwStartType = NativeMethods.SERVICE_DEMAND_START;
                    break;
                case ServiceStartupType.Disabled:
                    dwStartType = NativeMethods.SERVICE_DISABLED;
                    break;
                case ServiceStartupType.InvalidValue:
                    dwStartType = NativeMethods.SERVICE_NO_CHANGE;
                    break;
                default:
                    success = false;
                    break;
            }

            return success;
        }

        internal static ServiceStartupType GetServiceStartupType(ServiceStartMode startMode, bool delayedAutoStart)
        {
            ServiceStartupType result = ServiceStartupType.Disabled;
            switch (startMode)
            {
                case ServiceStartMode.Automatic:
                    result = delayedAutoStart ? ServiceStartupType.AutomaticDelayedStart : ServiceStartupType.Automatic;
                    break;
                case ServiceStartMode.Manual:
                    result = ServiceStartupType.Manual;
                    break;
                case ServiceStartMode.Disabled:
                    result = ServiceStartupType.Disabled;
                    break;
            }

            return result;
        }
    }
    #endregion NativeMethods

    #region ServiceStartupType
    
    public enum ServiceStartupType
    {
        
        InvalidValue = -1,
        
        Automatic = 2,
        
        Manual = 3,
        
        Disabled = 4,
        
        AutomaticDelayedStart = 10
    }
    #endregion ServiceStartupType
}

#endif // Not built on Unix
