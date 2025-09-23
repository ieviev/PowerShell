// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Net;
using System.Text;
using Microsoft.Management.Infrastructure.Options;
#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    #region Parameter Set Resolving Classes

    
    internal class ParameterDefinitionEntry
    {
        
        /// <param name="parameterSetName"></param>
        /// <param name="mandatory"></param>
        internal ParameterDefinitionEntry(string parameterSetName, bool mandatory)
        {
            this.IsMandatory = mandatory;
            this.ParameterSetName = parameterSetName;
        }

        
        internal string ParameterSetName { get; }

        
        internal bool IsMandatory { get; }
    }

    
    internal class ParameterSetEntry
    {
        
        /// <param name="mandatoryParameterCount"></param>
        internal ParameterSetEntry(uint mandatoryParameterCount)
        {
            this.MandatoryParameterCount = mandatoryParameterCount;
            this.IsDefaultParameterSet = false;
            reset();
        }

        
        /// <param name="toClone"></param>
        internal ParameterSetEntry(ParameterSetEntry toClone)
        {
            this.MandatoryParameterCount = toClone.MandatoryParameterCount;
            this.IsDefaultParameterSet = toClone.IsDefaultParameterSet;
            reset();
        }

        
        /// <param name="mandatoryParameterCount"></param>
        /// <param name="mandatory"></param>
        internal ParameterSetEntry(uint mandatoryParameterCount, bool isDefault)
        {
            this.MandatoryParameterCount = mandatoryParameterCount;
            this.IsDefaultParameterSet = isDefault;
            reset();
        }

        
        internal void reset()
        {
            this.SetMandatoryParameterCount = this.SetMandatoryParameterCountAtBeginProcess;
            this.IsValueSet = this.IsValueSetAtBeginProcess;
        }

        
        internal bool IsDefaultParameterSet { get; }

        
        internal uint MandatoryParameterCount { get; } = 0;

        
        internal bool IsValueSet { get; set; }

        
        internal bool IsValueSetAtBeginProcess { get; set; }

        
        internal uint SetMandatoryParameterCount { get; set; } = 0;

        
        internal uint SetMandatoryParameterCountAtBeginProcess { get; set; } = 0;
    }

    
    internal class ParameterBinder
    {
        
        /// <param name="parameters"></param>
        /// <param name="sets"></param>
        internal ParameterBinder(
            Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters,
            Dictionary<string, ParameterSetEntry> sets)
        {
            this.CloneParameterEntries(parameters, sets);
        }

        #region Two dictionaries used to determine the bound parameter set

        
        private Dictionary<string, HashSet<ParameterDefinitionEntry>> parameterDefinitionEntries;

        
        private Dictionary<string, ParameterSetEntry> parameterSetEntries;

        #endregion

        
        private List<string> parametersetNamesList = new();

        
        private readonly List<string> parameterNamesList = new();

        
        private List<string> parametersetNamesListAtBeginProcess = new();

        
        private readonly List<string> parameterNamesListAtBeginProcess = new();

        
        internal void reset()
        {
            foreach (KeyValuePair<string, ParameterSetEntry> setEntry in parameterSetEntries)
            {
                setEntry.Value.reset();
            }

            this.parametersetNamesList.Clear();
            foreach (string parametersetName in this.parametersetNamesListAtBeginProcess)
            {
                this.parametersetNamesList.Add(parametersetName);
            }

            this.parameterNamesList.Clear();
            foreach (string parameterName in this.parameterNamesListAtBeginProcess)
            {
                this.parameterNamesList.Add(parameterName);
            }
        }

        
        /// <param name="parameterName"></param>
        /// <exception cref="PSArgumentException">Throw if conflict parameter was set.</exception>
        internal void SetParameter(string parameterName, bool isBeginProcess)
        {
            DebugHelper.WriteLogEx("ParameterName = {0}, isBeginProcess = {1}", 0, parameterName, isBeginProcess);

            if (this.parameterNamesList.Contains(parameterName))
            {
                DebugHelper.WriteLogEx("ParameterName {0} is already bound ", 1, parameterName);
                return;
            }
            else
            {
                this.parameterNamesList.Add(parameterName);
                if (isBeginProcess)
                {
                    this.parameterNamesListAtBeginProcess.Add(parameterName);
                }
            }

            if (this.parametersetNamesList.Count == 0)
            {
                List<string> nameset = new();
                foreach (ParameterDefinitionEntry parameterDefinitionEntry in this.parameterDefinitionEntries[parameterName])
                {
                    DebugHelper.WriteLogEx("parameterset name = '{0}'; mandatory = '{1}'", 1, parameterDefinitionEntry.ParameterSetName, parameterDefinitionEntry.IsMandatory);
                    ParameterSetEntry psEntry = this.parameterSetEntries[parameterDefinitionEntry.ParameterSetName];
                    if (psEntry == null)
                        continue;

                    if (parameterDefinitionEntry.IsMandatory)
                    {
                        psEntry.SetMandatoryParameterCount++;
                        if (isBeginProcess)
                        {
                            psEntry.SetMandatoryParameterCountAtBeginProcess++;
                        }

                        DebugHelper.WriteLogEx("parameterset name = '{0}'; SetMandatoryParameterCount = '{1}'", 1, parameterDefinitionEntry.ParameterSetName, psEntry.SetMandatoryParameterCount);
                    }

                    if (!psEntry.IsValueSet)
                    {
                        psEntry.IsValueSet = true;
                        if (isBeginProcess)
                        {
                            psEntry.IsValueSetAtBeginProcess = true;
                        }
                    }

                    nameset.Add(parameterDefinitionEntry.ParameterSetName);
                }

                this.parametersetNamesList = nameset;
                if (isBeginProcess)
                {
                    this.parametersetNamesListAtBeginProcess = nameset;
                }
            }
            else
            {
                List<string> nameset = new();
                foreach (ParameterDefinitionEntry entry in this.parameterDefinitionEntries[parameterName])
                {
                    if (this.parametersetNamesList.Contains(entry.ParameterSetName))
                    {
                        nameset.Add(entry.ParameterSetName);
                        if (entry.IsMandatory)
                        {
                            ParameterSetEntry psEntry = this.parameterSetEntries[entry.ParameterSetName];
                            psEntry.SetMandatoryParameterCount++;
                            if (isBeginProcess)
                            {
                                psEntry.SetMandatoryParameterCountAtBeginProcess++;
                            }

                            DebugHelper.WriteLogEx("parameterset name = '{0}'; SetMandatoryParameterCount = '{1}'",
                                1,
                                entry.ParameterSetName,
                                psEntry.SetMandatoryParameterCount);
                        }
                    }
                }

                if (nameset.Count == 0)
                {
                    throw new PSArgumentException(CimCmdletStrings.UnableToResolveParameterSetName);
                }
                else
                {
                    this.parametersetNamesList = nameset;
                    if (isBeginProcess)
                    {
                        this.parametersetNamesListAtBeginProcess = nameset;
                    }
                }
            }
        }

        
        /// <returns></returns>
        internal string GetParameterSet()
        {
            DebugHelper.WriteLogEx();

            string boundParameterSetName = null;
            string defaultParameterSetName = null;
            List<string> noMandatoryParameterSet = new();

            // Looking for parameter set which have mandatory parameters
            foreach (string parameterSetName in this.parameterSetEntries.Keys)
            {
                ParameterSetEntry entry = this.parameterSetEntries[parameterSetName];
                DebugHelper.WriteLogEx(
                    "parameterset name = {0}, {1}/{2} mandatory parameters.",
                    1,
                    parameterSetName,
                    entry.SetMandatoryParameterCount,
                    entry.MandatoryParameterCount);

                // Ignore the parameter set which has no mandatory parameter firstly
                if (entry.MandatoryParameterCount == 0)
                {
                    if (entry.IsDefaultParameterSet)
                    {
                        defaultParameterSetName = parameterSetName;
                    }

                    if (entry.IsValueSet)
                    {
                        noMandatoryParameterSet.Add(parameterSetName);
                    }

                    continue;
                }

                if ((entry.SetMandatoryParameterCount == entry.MandatoryParameterCount) &&
                    this.parametersetNamesList.Contains(parameterSetName))
                {
                    if (boundParameterSetName != null)
                    {
                        throw new PSArgumentException(CimCmdletStrings.UnableToResolveParameterSetName);
                    }

                    boundParameterSetName = parameterSetName;
                }
            }

            // Looking for parameter set which has no mandatory parameters
            if (boundParameterSetName == null)
            {
                // throw if there are > 1 parameter set
                if (noMandatoryParameterSet.Count > 1)
                {
                    throw new PSArgumentException(CimCmdletStrings.UnableToResolveParameterSetName);
                }
                else if (noMandatoryParameterSet.Count == 1)
                {
                    boundParameterSetName = noMandatoryParameterSet[0];
                }
            }

            // Looking for default parameter set
            boundParameterSetName ??= defaultParameterSetName;

            // throw if still can not find the parameter set name
            if (boundParameterSetName == null)
            {
                throw new PSArgumentException(CimCmdletStrings.UnableToResolveParameterSetName);
            }

            return boundParameterSetName;
        }

        
        private void CloneParameterEntries(
            Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters,
            Dictionary<string, ParameterSetEntry> sets)
        {
            this.parameterDefinitionEntries = parameters;
            this.parameterSetEntries = new Dictionary<string, ParameterSetEntry>();
            foreach (KeyValuePair<string, ParameterSetEntry> parameterSet in sets)
            {
                this.parameterSetEntries.Add(parameterSet.Key, new ParameterSetEntry(parameterSet.Value));
            }
        }
    }

    #endregion

    
    public class CimBaseCommand : Cmdlet, IDisposable
    {
        #region resolve parameter set name
        
        internal void CheckParameterSet()
        {
            if (this.parameterBinder != null)
            {
                try
                {
                    this.ParameterSetName = this.parameterBinder.GetParameterSet();
                }
                finally
                {
                    this.parameterBinder.reset();
                }
            }

            DebugHelper.WriteLog("current parameterset is: " + this.ParameterSetName, 4);
        }

        
        /// <param name="parameterName"></param>
        internal void SetParameter(object value, string parameterName)
        {
            // Ignore the null value being set,
            // Null value could be set by caller unintentionally,
            // or by powershell to reset the parameter to default value
            // before the next parameter binding, and ProcessRecord call
            if (value == null)
            {
                return;
            }

            this.parameterBinder?.SetParameter(parameterName, this.AtBeginProcess);
        }
        #endregion

        #region constructors

        
        internal CimBaseCommand()
        {
            this.disposed = false;
            this.parameterBinder = null;
        }

        
        internal CimBaseCommand(Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters,
            Dictionary<string, ParameterSetEntry> sets)
        {
            this.disposed = false;
            this.parameterBinder = new ParameterBinder(parameters, sets);
        }

        #endregion

        #region override functions of Cmdlet

        
        protected override void StopProcessing()
        {
            Dispose();
        }

        #endregion

        #region IDisposable interface
        
        private bool disposed;

        
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        
        /// <param name="disposing">Whether it is directly called.</param>
        protected void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    DisposeInternal();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                disposed = true;
            }
        }

        
        protected virtual void DisposeInternal()
        {
            // Dispose managed resources.
            this.operation?.Dispose();
        }
        #endregion

        #region private members

        
        private readonly ParameterBinder parameterBinder;

        
        private CimAsyncOperation operation;

        
        private readonly object myLock = new();

        
        private bool atBeginProcess = true;

        internal bool AtBeginProcess
        {
            get
            {
                return this.atBeginProcess;
            }

            set
            {
                this.atBeginProcess = value;
            }
        }
        #endregion

        #region internal properties

        
        internal CimAsyncOperation AsyncOperation
        {
            get
            {
                return this.operation;
            }

            set
            {
                lock (this.myLock)
                {
                    Debug.Assert(this.operation == null, "Caller should verify that operation is null");
                    this.operation = value;
                }
            }
        }

        
        internal string ParameterSetName { get; private set; }

        
        internal virtual CmdletOperationBase CmdletOperation
        {
            get;
            set;
        }

        
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        internal void ThrowTerminatingError(Exception exception, string operation)
        {
            ErrorRecord errorRecord = new(exception, operation, ErrorCategory.InvalidOperation, this);
            this.CmdletOperation.ThrowTerminatingError(errorRecord);
        }

        #endregion

        #region internal const strings

        
        internal const string AliasCN = "CN";

        
        internal const string AliasServerName = "ServerName";

        
        internal const string AliasOT = "OT";

        
        internal const string SessionSetName = "SessionSet";

        
        internal const string ComputerSetName = "ComputerSet";

        
        internal const string ClassNameComputerSet = "ClassNameComputerSet";

        
        internal const string ResourceUriComputerSet = "ResourceUriComputerSet";

        
        internal const string CimInstanceComputerSet = "CimInstanceComputerSet";

        
        internal const string QueryComputerSet = "QueryComputerSet";

        
        internal const string ClassNameSessionSet = "ClassNameSessionSet";

        
        internal const string ResourceUriSessionSet = "ResourceUriSessionSet";

        
        internal const string CimInstanceSessionSet = "CimInstanceSessionSet";

        
        internal const string QuerySessionSet = "QuerySessionSet";

        
        internal const string CimClassComputerSet = "CimClassComputerSet";

        
        internal const string CimClassSessionSet = "CimClassSessionSet";

        #region Session related parameter set name

        internal const string ComputerNameSet = "ComputerNameSet";
        internal const string SessionIdSet = "SessionIdSet";
        internal const string InstanceIdSet = "InstanceIdSet";
        internal const string NameSet = "NameSet";
        internal const string CimSessionSet = "CimSessionSet";
        internal const string WSManParameterSet = "WSManParameterSet";
        internal const string DcomParameterSet = "DcomParameterSet";
        internal const string ProtocolNameParameterSet = "ProtocolTypeSet";
        #endregion

        #region register cimindication parameter set name
        internal const string QueryExpressionSessionSet = "QueryExpressionSessionSet";
        internal const string QueryExpressionComputerSet = "QueryExpressionComputerSet";
        #endregion

        
        internal const string CredentialParameterSet = "CredentialParameterSet";

        
        internal const string CertificateParameterSet = "CertificateParameterSet";

        
        internal const string AliasCimInstance = "CimInstance";

        #endregion

        #region internal helper function

        
        /// <param name="operationName"></param>
        /// <param name="parameterName"></param>
        /// <param name="authentication"></param>
        internal void ThrowInvalidAuthenticationTypeError(
            string operationName,
            string parameterName,
            PasswordAuthenticationMechanism authentication)
        {
            string message = string.Format(CultureInfo.CurrentUICulture, CimCmdletStrings.InvalidAuthenticationTypeWithNullCredential,
                authentication,
                ImpersonatedAuthenticationMechanism.None,
                ImpersonatedAuthenticationMechanism.Negotiate,
                ImpersonatedAuthenticationMechanism.Kerberos,
                ImpersonatedAuthenticationMechanism.NtlmDomain);
            PSArgumentOutOfRangeException exception = new(
                parameterName, authentication, message);
            ThrowTerminatingError(exception, operationName);
        }

        
        /// <param name="operationName"></param>
        /// <param name="parameterName"></param>
        /// <param name="conflictParameterName"></param>
        internal void ThrowConflictParameterWasSet(
            string operationName,
            string parameterName,
            string conflictParameterName)
        {
            string message = string.Format(CultureInfo.CurrentUICulture,
                CimCmdletStrings.ConflictParameterWasSet,
                parameterName, conflictParameterName);
            PSArgumentException exception = new(message, parameterName);
            ThrowTerminatingError(exception, operationName);
        }

        
        internal void ThrowInvalidProperty(
            IEnumerable<string> propertiesList,
            string className,
            string parameterName,
            string operationName,
            IDictionary actualValue)
        {
            StringBuilder propList = new();
            foreach (string property in propertiesList)
            {
                if (propList.Length > 0)
                {
                    propList.Append(',');
                }

                propList.Append(property);
            }

            string message = string.Format(CultureInfo.CurrentUICulture, CimCmdletStrings.CouldNotFindPropertyFromGivenClass,
                className, propList);
            PSArgumentOutOfRangeException exception = new(
                parameterName, actualValue, message);
            ThrowTerminatingError(exception, operationName);
        }

        
        /// <param name="psCredentials"></param>
        /// <param name="passwordAuthentication"></param>
        /// <returns></returns>
        internal CimCredential CreateCimCredentials(PSCredential psCredentials,
            PasswordAuthenticationMechanism passwordAuthentication,
            string operationName,
            string parameterName)
        {
            DebugHelper.WriteLogEx("PSCredential:{0}; PasswordAuthenticationMechanism:{1}; operationName:{2}; parameterName:{3}.", 0, psCredentials, passwordAuthentication, operationName, parameterName);

            CimCredential credentials = null;
            if (psCredentials != null)
            {
                NetworkCredential networkCredential = psCredentials.GetNetworkCredential();
                DebugHelper.WriteLog("Domain:{0}; UserName:{1}; Password:{2}.", 1, networkCredential.Domain, networkCredential.UserName, psCredentials.Password);
                credentials = new CimCredential(passwordAuthentication, networkCredential.Domain, networkCredential.UserName, psCredentials.Password);
            }
            else
            {
                ImpersonatedAuthenticationMechanism impersonatedAuthentication;
                switch (passwordAuthentication)
                {
                    case PasswordAuthenticationMechanism.Default:
                        impersonatedAuthentication = ImpersonatedAuthenticationMechanism.None;
                        break;
                    case PasswordAuthenticationMechanism.Negotiate:
                        impersonatedAuthentication = ImpersonatedAuthenticationMechanism.Negotiate;
                        break;
                    case PasswordAuthenticationMechanism.Kerberos:
                        impersonatedAuthentication = ImpersonatedAuthenticationMechanism.Kerberos;
                        break;
                    case PasswordAuthenticationMechanism.NtlmDomain:
                        impersonatedAuthentication = ImpersonatedAuthenticationMechanism.NtlmDomain;
                        break;
                    default:
                        ThrowInvalidAuthenticationTypeError(operationName, parameterName, passwordAuthentication);
                        return null;
                }

                credentials = new CimCredential(impersonatedAuthentication);
            }

            DebugHelper.WriteLogEx("return credential {0}", 1, credentials);
            return credentials;
        }
        #endregion
    }
}
