// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.Win32;

#pragma warning disable 1591, 1572, 1571, 1573, 1587, 1570, 0067

#region PS_STUBS
// Include PS types that are not needed for PowerShell on CSS

namespace System.Management.Automation
{
    #region PSTransaction

    
    public sealed class PSTransactionContext : IDisposable
    {
        internal PSTransactionContext(Internal.PSTransactionManager transactionManager) { }

        public void Dispose() { }
    }

    
    public enum RollbackSeverity
    {
        
        Error,

        
        TerminatingError,

        
        Never
    }

    #endregion PSTransaction
}

namespace System.Management.Automation.Internal
{
    
    internal sealed class PSTransactionManager : IDisposable
    {
        
        internal bool HasTransaction
        {
            get
            {
                return false;
            }
        }

        
        internal bool IsLastTransactionCommitted
        {
            get
            {
                throw new NotImplementedException("IsLastTransactionCommitted");
            }
        }

        
        internal bool IsLastTransactionRolledBack
        {
            get
            {
                throw new NotImplementedException("IsLastTransactionRolledBack");
            }
        }

        
        internal RollbackSeverity RollbackPreference
        {
            get
            {
                throw new NotImplementedException("RollbackPreference");
            }
        }

        
        internal static IDisposable GetEngineProtectionScope()
        {
            return null;
        }

        
        internal void Rollback(bool suppressErrors)
        {
            throw new NotImplementedException("Rollback");
        }

        public void Dispose() { }
    }
}

namespace Microsoft.PowerShell.Commands.Internal
{
    using System.Security.AccessControl;
    using System.Security.Principal;

    #region TransactedRegistryKey

    internal abstract class TransactedRegistryKey : IDisposable
    {
        public void Dispose() { }

        public void SetValue(string name, object value)
        {
            throw new NotImplementedException("SetValue(string name, obj value) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            throw new NotImplementedException("SetValue(string name, obj value, RegistryValueKind valueKind) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public string[] GetValueNames()
        {
            throw new NotImplementedException("GetValueNames() is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public void DeleteValue(string name)
        {
            throw new NotImplementedException("DeleteValue(string name) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public string[] GetSubKeyNames()
        {
            throw new NotImplementedException("GetSubKeyNames() is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public TransactedRegistryKey CreateSubKey(string subkey)
        {
            throw new NotImplementedException("CreateSubKey(string subkey) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public TransactedRegistryKey OpenSubKey(string name, bool writable)
        {
            throw new NotImplementedException("OpenSubKey(string name, bool writeable) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public void DeleteSubKeyTree(string subkey)
        {
            throw new NotImplementedException("DeleteSubKeyTree(string subkey) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public object GetValue(string name)
        {
            throw new NotImplementedException("GetValue(string name) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public object GetValue(string name, object defaultValue, RegistryValueOptions options)
        {
            throw new NotImplementedException("GetValue(string name, object defaultValue, RegistryValueOptions options) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public RegistryValueKind GetValueKind(string name)
        {
            throw new NotImplementedException("GetValueKind(string name) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public void Close()
        {
            throw new NotImplementedException("Close() is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public abstract string Name { get; }

        public abstract int SubKeyCount { get; }

        public void SetAccessControl(ObjectSecurity securityDescriptor)
        {
            throw new NotImplementedException("SetAccessControl(ObjectSecurity securityDescriptor) is not implemented. TransactedRegistry related APIs should not be used.");
        }

        public ObjectSecurity GetAccessControl(AccessControlSections includeSections)
        {
            throw new NotImplementedException("GetAccessControl(AccessControlSections includeSections) is not implemented. TransactedRegistry related APIs should not be used.");
        }
    }

    internal sealed class TransactedRegistry
    {
        internal static readonly TransactedRegistryKey LocalMachine;
        internal static readonly TransactedRegistryKey ClassesRoot;
        internal static readonly TransactedRegistryKey Users;
        internal static readonly TransactedRegistryKey CurrentConfig;
        internal static readonly TransactedRegistryKey CurrentUser;
    }

    internal sealed class TransactedRegistrySecurity : ObjectSecurity
    {
        public override Type AccessRightType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Type AccessRuleType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Type AuditRuleType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
        {
            throw new NotImplementedException();
        }

        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
        {
            throw new NotImplementedException();
        }

        protected override bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified)
        {
            throw new NotImplementedException();
        }

        protected override bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified)
        {
            throw new NotImplementedException();
        }
    }

    #endregion TransactedRegistryKey
}

#endregion PS_STUBS

// -- Will port the actual PS component [update: Not necessarily porting all PS components listed here]
#region TEMPORARY

namespace System.Management.Automation.Internal
{
    using Microsoft.PowerShell.Commands;

    
    internal static class PowerShellModuleAssemblyAnalyzer
    {
        internal static BinaryAnalysisResult AnalyzeModuleAssembly(string path, out Version assemblyVersion)
        {
            assemblyVersion = new Version("0.0.0.0");
            return null;
        }
    }
}

namespace System.Management.Automation
{
    using Microsoft.Win32;

    #region RegistryStringResourceIndirect

    internal sealed class RegistryStringResourceIndirect : IDisposable
    {
        internal static RegistryStringResourceIndirect GetResourceIndirectReader()
        {
            return new RegistryStringResourceIndirect();
        }

        
        public void Dispose()
        {
        }

        internal string GetResourceStringIndirect(
            string assemblyName,
            string modulePath,
            string baseNameRIDPair)
        {
            throw new NotImplementedException("RegistryStringResourceIndirect.GetResourceStringIndirect - 3 params");
        }

        internal string GetResourceStringIndirect(
            RegistryKey key,
            string valueName,
            string assemblyName,
            string modulePath)
        {
            throw new NotImplementedException("RegistryStringResourceIndirect.GetResourceStringIndirect - 4 params");
        }
    }

    #endregion
}

#if UNIX

namespace System.Management.Automation.ComInterop
{
    using System.Dynamic;
    using System.Runtime.InteropServices;

    
    internal static class ComBinder
    {
        
        public static bool TryBindGetIndex(GetIndexBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result)
        {
            result = null;
            return false;
        }

        
        public static bool TryBindSetIndex(SetIndexBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, DynamicMetaObject value, out DynamicMetaObject result)
        {
            result = null;
            return false;
        }

        
        public static bool TryBindGetMember(GetMemberBinder binder, DynamicMetaObject instance, out DynamicMetaObject result, bool delayInvocation)
        {
            result = null;
            return false;
        }

        
        public static bool TryBindSetMember(SetMemberBinder binder, DynamicMetaObject instance, DynamicMetaObject value, out DynamicMetaObject result)
        {
            result = null;
            return false;
        }

        
        public static bool TryBindInvokeMember(InvokeMemberBinder binder, bool isSetProperty, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result)
        {
            result = null;
            return false;
        }
    }

    internal static class VarEnumSelector
    {
        internal static Type GetTypeForVarEnum(VarEnum vt)
        {
            throw new PlatformNotSupportedException();
        }
    }
}

namespace System.Management.Automation.Security
{
    
    public sealed class SystemPolicy
    {
        private SystemPolicy() { }

        
        internal static void LogWDACAuditMessage(
            ExecutionContext context,
            string title,
            string message,
            string fqid,
            bool dropIntoDebugger = false)
        {
        }

        
        public static SystemEnforcementMode GetSystemLockdownPolicy()
        {
            return SystemEnforcementMode.None;
        }

        
        public static SystemEnforcementMode GetLockdownPolicy(string path, System.Runtime.InteropServices.SafeHandle handle)
        {
            return SystemEnforcementMode.None;
        }

        internal static bool IsClassInApprovedList(Guid clsid)
        {
            throw new NotImplementedException("SystemPolicy.IsClassInApprovedList not implemented");
        }

        
        public static SystemScriptFileEnforcement GetFilePolicyEnforcement(
            string filePath,
            System.IO.FileStream fileStream)
        {
            return SystemScriptFileEnforcement.None;
        }
    }

    
    public enum SystemEnforcementMode
    {
        None = 0,

        Audit = 1,

        Enforce = 2
    }

    
    public enum SystemScriptFileEnforcement
    {
        
        None = 0,

        
        Block = 1,

        
        Allow = 2,

        
        AllowConstrained = 3,

        
        AllowConstrainedAudit = 4
    }
}

// Porting note: Tracing is absolutely not available on Linux
namespace System.Management.Automation.Tracing
{
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation.Internal;

    
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public abstract class EtwActivity
    {
        
        public static bool SetActivityId(Guid activityId)
        {
            return false;
        }

        
        public static Guid CreateActivityId()
        {
            return Guid.Empty;
        }

        
        public static Guid GetActivityId()
        {
            return Guid.Empty;
        }
    }

    public enum PowerShellTraceTask
    {
        
        None = 0,

        
        CreateRunspace = 1,

        
        ExecuteCommand = 2,

        
        Serialization = 3,

        
        PowerShellConsoleStartup = 4,
    }

    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1028")]
    [Flags]
    public enum PowerShellTraceKeywords : ulong
    {
        
        None = 0,

        
        Runspace = 0x1,

        
        Pipeline = 0x2,

        
        Protocol = 0x4,

        
        Transport = 0x8,

        
        Host = 0x10,

        
        Cmdlets = 0x20,

        
        Serializer = 0x40,

        
        Session = 0x80,

        
        ManagedPlugIn = 0x100,

        
        UseAlwaysDebug = 0x2000000000000000,

        
        UseAlwaysOperational = 0x8000000000000000,

        
        UseAlwaysAnalytic = 0x4000000000000000,
    }

    public sealed partial class Tracer : System.Management.Automation.Tracing.EtwActivity
    {
        static Tracer() { }

        public void EndpointRegistered(string endpointName, string endpointType, string registeredBy)
        {
        }

        public void EndpointUnregistered(string endpointName, string unregisteredBy)
        {
        }

        public void EndpointDisabled(string endpointName, string disabledBy)
        {
        }

        public void EndpointEnabled(string endpointName, string enabledBy)
        {
        }

        public void EndpointModified(string endpointName, string modifiedBy)
        {
        }

        public void BeginContainerParentJobExecution(Guid containerParentJobInstanceId)
        {
        }

        public void BeginProxyJobExecution(Guid proxyJobInstanceId)
        {
        }

        public void ProxyJobRemoteJobAssociation(Guid proxyJobInstanceId, Guid containerParentJobInstanceId)
        {
        }

        public void EndProxyJobExecution(Guid proxyJobInstanceId)
        {
        }

        public void BeginProxyJobEventHandler(Guid proxyJobInstanceId)
        {
        }

        public void EndProxyJobEventHandler(Guid proxyJobInstanceId)
        {
        }

        public void BeginProxyChildJobEventHandler(Guid proxyChildJobInstanceId)
        {
        }

        public void EndContainerParentJobExecution(Guid containerParentJobInstanceId)
        {
        }
    }

    public sealed class PowerShellTraceSource : IDisposable
    {
        internal PowerShellTraceSource(PowerShellTraceTask task, PowerShellTraceKeywords keywords)
        {
        }

        public void Dispose()
        {
        }

        public bool WriteMessage(string message)
        {
            return false;
        }

        
        public bool WriteMessage(string message1, string message2)
        {
            return false;
        }

        
        public bool WriteMessage(string message, Guid instanceId)
        {
            return false;
        }

        
        public void WriteMessage(string className, string methodName, Guid workflowId, string message, params string[] parameters)
        {
            return;
        }

        
        public void WriteMessage(string className, string methodName, Guid workflowId, Job job, string message, params string[] parameters)
        {
            return;
        }

        public bool TraceException(Exception exception)
        {
            return false;
        }
    }

    
    public static class PowerShellTraceSourceFactory
    {
        
        public static PowerShellTraceSource GetTraceSource()
        {
            return new PowerShellTraceSource(PowerShellTraceTask.None, PowerShellTraceKeywords.None);
        }

        
        public static PowerShellTraceSource GetTraceSource(PowerShellTraceTask task)
        {
            return new PowerShellTraceSource(task, PowerShellTraceKeywords.None);
        }

        
        public static PowerShellTraceSource GetTraceSource(PowerShellTraceTask task, PowerShellTraceKeywords keywords)
        {
            return new PowerShellTraceSource(task, keywords);
        }
    }
}

#endif

namespace Microsoft.PowerShell
{
    internal static class NativeCultureResolver
    {
        internal static void SetThreadUILanguage(Int16 langId) { }

        internal static CultureInfo UICulture
        {
            get
            {
                return CultureInfo.CurrentUICulture; // this is actually wrong, but until we port "hostifaces\NativeCultureResolver.cs" to Nano, this will do and will help avoid build break.
            }
        }

        internal static CultureInfo Culture
        {
            get
            {
                return CultureInfo.CurrentCulture; // this is actually wrong, but until we port "hostifaces\NativeCultureResolver.cs" to Nano, this will do and will help avoid build break.
            }
        }
    }
}

#endregion TEMPORARY

#pragma warning restore 1591, 1572, 1571, 1573, 1587, 1570, 0067
