// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Globalization;
using System.Management;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "WmiObject", DefaultParameterSetName = "query",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113337", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class GetWmiObjectCommand : WmiBaseCmdlet
    {
        #region Parameters

        
        [Alias("ClassName")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "query")]
        [Parameter(Position = 1, ParameterSetName = "list")]
        [ValidateNotNullOrEmpty]
        public string Class { get; set; }

        
        [Parameter(ParameterSetName = "list")]
        public SwitchParameter Recurse { get; set; } = false;

        
        [Parameter(Position = 1, ParameterSetName = "query")]
        [ValidateNotNullOrEmpty]
        public string[] Property
        {
            get { return (string[])_property.Clone(); }

            set { _property = value; }
        }

        
        [Parameter(ParameterSetName = "query")]
        public string Filter { get; set; }

        
        [Parameter]
        public SwitchParameter Amended { get; set; }

        
        [Parameter(ParameterSetName = "WQLQuery")]
        [Parameter(ParameterSetName = "query")]
        public SwitchParameter DirectRead { get; set; }

        
        [Parameter(ParameterSetName = "list")]
        public SwitchParameter List { get; set; } = false;

        
        [Parameter(Mandatory = true, ParameterSetName = "WQLQuery")]
        public string Query { get; set; }

        #endregion Parameters

        #region parameter data

        private string[] _property = new string[] { "*" };

        #endregion parameter data

        #region Command code

        
        internal string GetQueryString()
        {
            StringBuilder returnValue = new StringBuilder("select ");
            returnValue.Append(string.Join(", ", _property));
            returnValue.Append(" from ");
            returnValue.Append(Class);
            if (!string.IsNullOrEmpty(Filter))
            {
                returnValue.Append(" where ");
                returnValue.Append(Filter);
            }

            return returnValue.ToString();
        }
        

        internal string GetFilterClassName()
        {
            if (string.IsNullOrEmpty(this.Class))
                return string.Empty;
            string filterClass = string.Copy(this.Class);
            filterClass = filterClass.Replace('*', '%');
            filterClass = filterClass.Replace('?', '_');
            return filterClass;
        }

        internal bool IsLocalizedNamespace(string sNamespace)
        {
            bool toReturn = false;
            if (sNamespace.StartsWith("ms_", StringComparison.OrdinalIgnoreCase))
            {
                toReturn = true;
            }

            return toReturn;
        }

        internal bool ValidateClassFormat()
        {
            string filterClass = this.Class;
            if (string.IsNullOrEmpty(filterClass))
                return true;
            StringBuilder newClassName = new StringBuilder();
            for (int i = 0; i < filterClass.Length; i++)
            {
                if (char.IsLetterOrDigit(filterClass[i]) ||
                    filterClass[i].Equals('[') || filterClass[i].Equals(']') ||
                    filterClass[i].Equals('*') || filterClass[i].Equals('?') ||
                    filterClass[i].Equals('-'))
                {
                    newClassName.Append(filterClass[i]);
                    continue;
                }
                else if (filterClass[i].Equals('_'))
                {
                    newClassName.Append('[');
                    newClassName.Append(filterClass[i]);
                    newClassName.Append(']');
                    continue;
                }

                return false;
            }

            this.Class = newClassName.ToString();
            return true;
        }

        
        internal ManagementObjectSearcher GetObjectList(ManagementScope scope)
        {
            StringBuilder queryStringBuilder = new StringBuilder();
            if (string.IsNullOrEmpty(this.Class))
            {
                queryStringBuilder.Append("select * from meta_class");
            }
            else
            {
                string filterClass = GetFilterClassName();
                if (filterClass == null)
                    return null;
                queryStringBuilder.Append("select * from meta_class where __class like '");
                queryStringBuilder.Append(filterClass);
                queryStringBuilder.Append("'");
            }

            ObjectQuery classQuery = new ObjectQuery(queryStringBuilder.ToString());

            EnumerationOptions enumOptions = new EnumerationOptions();
            enumOptions.EnumerateDeep = true;
            enumOptions.UseAmendedQualifiers = this.Amended;
            var searcher = new ManagementObjectSearcher(scope, classQuery, enumOptions);
            return searcher;
        }
        
        protected override void BeginProcessing()
        {
            ConnectionOptions options = GetConnectionOption();
            if (this.AsJob)
            {
                RunAsJob("Get-WMIObject");
                return;
            }
            else
            {
                if (List.IsPresent)
                {
                    if (!this.ValidateClassFormat())
                    {
                        ErrorRecord errorRecord = new ErrorRecord(
                       new ArgumentException(
                           string.Format(
                               Thread.CurrentThread.CurrentCulture,
                               "Class", this.Class)),
                       "INVALID_QUERY_IDENTIFIER",
                       ErrorCategory.InvalidArgument,
                       null);
                        errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiFilterInvalidClass", this.Class);

                        WriteError(errorRecord);
                        return;
                    }

                    foreach (string name in ComputerName)
                    {
                        if (this.Recurse.IsPresent)
                        {
                            Queue namespaceElement = new Queue();
                            namespaceElement.Enqueue(this.Namespace);
                            while (namespaceElement.Count > 0)
                            {
                                string connectNamespace = (string)namespaceElement.Dequeue();
                                ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(name, connectNamespace), options);
                                try
                                {
                                    scope.Connect();
                                }
                                catch (ManagementException e)
                                {
                                    ErrorRecord errorRecord = new ErrorRecord(
                                         e,
                                         "INVALID_NAMESPACE_IDENTIFIER",
                                         ErrorCategory.ObjectNotFound,
                                         null);
                                    errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", connectNamespace, e.Message);
                                    WriteError(errorRecord);
                                    continue;
                                }
                                catch (System.Runtime.InteropServices.COMException e)
                                {
                                    ErrorRecord errorRecord = new ErrorRecord(
                                         e,
                                         "INVALID_NAMESPACE_IDENTIFIER",
                                         ErrorCategory.ObjectNotFound,
                                         null);
                                    errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", connectNamespace, e.Message);
                                    WriteError(errorRecord);
                                    continue;
                                }
                                catch (System.UnauthorizedAccessException e)
                                {
                                    ErrorRecord errorRecord = new ErrorRecord(
                                         e,
                                         "INVALID_NAMESPACE_IDENTIFIER",
                                         ErrorCategory.ObjectNotFound,
                                         null);
                                    errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", connectNamespace, e.Message);
                                    WriteError(errorRecord);
                                    continue;
                                }

                                ManagementClass namespaceClass = new ManagementClass(scope, new ManagementPath("__Namespace"), new ObjectGetOptions());
                                foreach (ManagementBaseObject obj in namespaceClass.GetInstances())
                                {
                                    if (!IsLocalizedNamespace((string)obj["Name"]))
                                    {
                                        namespaceElement.Enqueue(connectNamespace + "\\" + obj["Name"]);
                                    }
                                }

                                ManagementObjectSearcher searcher = this.GetObjectList(scope);
                                if (searcher == null)
                                    continue;
                                foreach (ManagementBaseObject obj in searcher.Get())
                                {
                                    WriteObject(obj);
                                }
                            }
                        }
                        else
                        {
                            ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(name, this.Namespace), options);
                            try
                            {
                                scope.Connect();
                            }
                            catch (ManagementException e)
                            {
                                ErrorRecord errorRecord = new ErrorRecord(
                                     e,
                                     "INVALID_NAMESPACE_IDENTIFIER",
                                     ErrorCategory.ObjectNotFound,
                                     null);
                                errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", this.Namespace, e.Message);
                                WriteError(errorRecord);
                                continue;
                            }
                            catch (System.Runtime.InteropServices.COMException e)
                            {
                                ErrorRecord errorRecord = new ErrorRecord(
                                     e,
                                     "INVALID_NAMESPACE_IDENTIFIER",
                                     ErrorCategory.ObjectNotFound,
                                     null);
                                errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", this.Namespace, e.Message);
                                WriteError(errorRecord);
                                continue;
                            }
                            catch (System.UnauthorizedAccessException e)
                            {
                                ErrorRecord errorRecord = new ErrorRecord(
                                     e,
                                     "INVALID_NAMESPACE_IDENTIFIER",
                                     ErrorCategory.ObjectNotFound,
                                     null);
                                errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", this.Namespace, e.Message);
                                WriteError(errorRecord);
                                continue;
                            }

                            ManagementObjectSearcher searcher = this.GetObjectList(scope);
                            if (searcher == null)
                                continue;
                            foreach (ManagementBaseObject obj in searcher.Get())
                            {
                                WriteObject(obj);
                            }
                        }
                    }

                    return;
                }

                // When -List is not specified and -Recurse is specified, we need the -Class parameter to compose the right query string
                if (this.Recurse.IsPresent && string.IsNullOrEmpty(Class))
                {
                    string errorMsg = string.Format(CultureInfo.InvariantCulture, WmiResources.WmiParameterMissing, "-Class");
                    ErrorRecord er = new ErrorRecord(new InvalidOperationException(errorMsg), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                string queryString = string.IsNullOrEmpty(this.Query) ? GetQueryString() : this.Query;
                ObjectQuery query = new ObjectQuery(queryString.ToString());

                foreach (string name in ComputerName)
                {
                    try
                    {
                        ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(name, this.Namespace), options);
                        EnumerationOptions enumOptions = new EnumerationOptions();
                        enumOptions.UseAmendedQualifiers = Amended;
                        enumOptions.DirectRead = DirectRead;
                        ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query, enumOptions);
                        foreach (ManagementBaseObject obj in searcher.Get())
                        {
                            WriteObject(obj);
                        }
                    }
                    catch (ManagementException e)
                    {
                        ErrorRecord errorRecord = null;
                        if (e.ErrorCode.Equals(ManagementStatus.InvalidClass))
                        {
                            string className = GetClassNameFromQuery(queryString);
                            string errorMsg = string.Format(CultureInfo.InvariantCulture, WmiResources.WmiQueryFailure,
                                                        e.Message, className);
                            errorRecord = new ErrorRecord(new ManagementException(errorMsg), "GetWMIManagementException", ErrorCategory.InvalidType, null);
                        }
                        else if (e.ErrorCode.Equals(ManagementStatus.InvalidQuery))
                        {
                            string errorMsg = string.Format(CultureInfo.InvariantCulture, WmiResources.WmiQueryFailure,
                                                        e.Message, queryString);
                            errorRecord = new ErrorRecord(new ManagementException(errorMsg), "GetWMIManagementException", ErrorCategory.InvalidArgument, null);
                        }
                        else if (e.ErrorCode.Equals(ManagementStatus.InvalidNamespace))
                        {
                            string errorMsg = string.Format(CultureInfo.InvariantCulture, WmiResources.WmiQueryFailure,
                                                        e.Message, this.Namespace);
                            errorRecord = new ErrorRecord(new ManagementException(errorMsg), "GetWMIManagementException", ErrorCategory.InvalidArgument, null);
                        }
                        else
                        {
                            errorRecord = new ErrorRecord(e, "GetWMIManagementException", ErrorCategory.InvalidOperation, null);
                        }

                        WriteError(errorRecord);
                        continue;
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "GetWMICOMException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                        continue;
                    }
                }
            }
        }

        
        /// <param name="query"></param>
        /// <returns></returns>
        private string GetClassNameFromQuery(string query)
        {
            System.Management.Automation.Diagnostics.Assert(query.Contains("from"),
                                                            "Only get called when ErrorCode is InvalidClass, which means the query string contains 'from' and the class name");

            if (Class != null)
            {
                return Class;
            }

            int fromIndex = query.IndexOf(" from ", StringComparison.OrdinalIgnoreCase);
            string subQuery = query.Substring(fromIndex + " from ".Length);
            string className = subQuery.Split(' ')[0];
            return className;
        }

        #endregion Command code
    }
}
