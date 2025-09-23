// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Management.Infrastructure;

namespace Microsoft.PowerShell.Commands
{
    using Extensions;

    internal static class CIMHelper
    {
        internal static class ClassNames
        {
            internal const string OperatingSystem = "Win32_OperatingSystem";
            internal const string PageFileUsage = "Win32_PageFileUsage";
            internal const string Bios = "Win32_BIOS";
            internal const string BaseBoard = "Win32_BaseBoard";
            internal const string ComputerSystem = "Win32_ComputerSystem";
            internal const string Keyboard = "Win32_Keyboard";
            internal const string DeviceGuard = "Win32_DeviceGuard";
            internal const string HotFix = "Win32_QuickFixEngineering";
            internal const string MicrosoftNetworkAdapter = "MSFT_NetAdapter";
            internal const string NetworkAdapter = "Win32_NetworkAdapter";
            internal const string NetworkAdapterConfiguration = "Win32_NetworkAdapterConfiguration";
            internal const string Processor = "Win32_Processor";
            internal const string PhysicalMemory = "Win32_PhysicalMemory";
            internal const string TimeZone = "Win32_TimeZone";
        }

        internal const string DefaultNamespace = @"root\cimv2";
        internal const string DeviceGuardNamespace = @"root\Microsoft\Windows\DeviceGuard";
        internal const string MicrosoftNetworkAdapterNamespace = "root/StandardCimv2";
        internal const string DefaultQueryDialect = "WQL";

        
        internal static string WqlQueryAll(string from)
        {
            return "SELECT * from " + from;
        }

        
        internal static T GetFirst<T>(CimSession session, string nameSpace, string wmiClassName) where T : class, new()
        {
            ArgumentException.ThrowIfNullOrEmpty(wmiClassName);

            try
            {
                var type = typeof(T);
                const BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                T rv = new();

                using (var instance = session.QueryFirstInstance(nameSpace, CIMHelper.WqlQueryAll(wmiClassName)))
                {
                    SetObjectDataMembers(rv, binding, instance);
                }

                return rv;
            }
            catch (Exception )
            {
                // on any error fall through to the null return below
            }

            return null;
        }

        
        internal static T[] GetAll<T>(CimSession session, string nameSpace, string wmiClassName) where T : class, new()
        {
            ArgumentException.ThrowIfNullOrEmpty(wmiClassName);
            
            var rv = new List<T>();

            try
            {
                var instances = session.QueryInstances(nameSpace, CIMHelper.WqlQueryAll(wmiClassName));

                if (instances != null)
                {
                    var type = typeof(T);
                    const BindingFlags binding = BindingFlags.Public | BindingFlags.Instance;

                    foreach (var instance in instances)
                    {
                        T objT = new();

                        using (instance)
                        {
                            SetObjectDataMembers(objT, binding, instance);
                        }

                        rv.Add(objT);
                    }
                }
            }
            catch (Exception )
            {
                // on any error we'll just fall through to the return below
            }

            return rv.ToArray();
        }

        
        internal static T[] GetAll<T>(CimSession session, string wmiClassName) where T : class, new()
        {
            return GetAll<T>(session, DefaultNamespace, wmiClassName);
        }

        internal static void SetObjectDataMember(object obj, BindingFlags binding, CimProperty cimProperty)
        {
            var type = obj.GetType();

            var pi = type.GetProperty(cimProperty.Name, binding);

            if (pi != null && pi.CanWrite)
            {
                pi.SetValue(obj, cimProperty.Value, null);
            }
            else
            {
                var fi = type.GetField(cimProperty.Name, binding);

                if (fi != null && !fi.IsInitOnly)
                {
                    fi.SetValue(obj, cimProperty.Value);
                }
            }
        }

        internal static void SetObjectDataMembers(object obj, BindingFlags binding, CimInstance instance)
        {
            foreach (var wmiProp in instance.CimInstanceProperties)
                SetObjectDataMember(obj, binding, wmiProp);
        }

        
        internal static string EscapePath(string path)
        {
            return string.Join(@"\\", path.Split('\\'));
        }
    }
}

namespace Extensions
{
    using Microsoft.PowerShell.Commands;

    internal static class CIMExtensions
    {
        
        internal static IEnumerable<CimInstance> QueryInstances(this CimSession session, string nameSpace, string query)
        {
            return session.QueryInstances(nameSpace, CIMHelper.DefaultQueryDialect, query);
        }

        
        internal static CimInstance QueryFirstInstance(this CimSession session, string nameSpace, string query)
        {
            try
            {
                var instances = session.QueryInstances(nameSpace, query);
                var enumerator = instances.GetEnumerator();

                if (enumerator.MoveNext())
                    return enumerator.Current;
            }
            catch (Exception )
            {
                // on any error, fall through to the null return below
            }

            return null;
        }

        
        internal static CimInstance QueryFirstInstance(this CimSession session, string query)
        {
            return session.QueryFirstInstance(CIMHelper.DefaultNamespace, query);
        }

        internal static T GetFirst<T>(this CimSession session, string wmiClassName) where T : class, new()
        {
            return session.GetFirst<T>(CIMHelper.DefaultNamespace, wmiClassName);
        }

        internal static T GetFirst<T>(this CimSession session, string wmiNamespace, string wmiClassName) where T : class, new()
        {
            return CIMHelper.GetFirst<T>(session, wmiNamespace, wmiClassName);
        }

        internal static T[] GetAll<T>(this CimSession session, string wmiClassName) where T : class, new()
        {
            return Microsoft.PowerShell.Commands.CIMHelper.GetAll<T>(session, wmiClassName);
        }

        internal static T[] GetAll<T>(this CimSession session, string wmiNamespace, string wmiClassName) where T : class, new()
        {
            return Microsoft.PowerShell.Commands.CIMHelper.GetAll<T>(session, wmiNamespace, wmiClassName);
        }
    }
}
