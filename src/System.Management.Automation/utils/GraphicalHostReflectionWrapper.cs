// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Management.Automation;
    using System.Reflection;

    
    internal sealed class GraphicalHostReflectionWrapper
    {
        
        private Assembly _graphicalHostAssembly;

        
        private Type _graphicalHostHelperType;

        
        private object _graphicalHostHelperObject;

        
        private GraphicalHostReflectionWrapper()
        {
        }

        
        internal static GraphicalHostReflectionWrapper GetGraphicalHostReflectionWrapper(PSCmdlet parentCmdlet, string graphicalHostHelperTypeName)
        {
            return GraphicalHostReflectionWrapper.GetGraphicalHostReflectionWrapper(parentCmdlet, graphicalHostHelperTypeName, parentCmdlet.CommandInfo.Name);
        }

        
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Assembly.Load has been found to throw unadvertised exceptions")]
        internal static GraphicalHostReflectionWrapper GetGraphicalHostReflectionWrapper(PSCmdlet parentCmdlet, string graphicalHostHelperTypeName, string featureName)
        {
            GraphicalHostReflectionWrapper returnValue = new GraphicalHostReflectionWrapper();

            if (GraphicalHostReflectionWrapper.IsInputFromRemoting(parentCmdlet))
            {
                ErrorRecord error = new ErrorRecord(
                    new NotSupportedException(StringUtil.Format(HelpErrors.RemotingNotSupportedForFeature, featureName)),
                    "RemotingNotSupported",
                    ErrorCategory.InvalidOperation,
                    parentCmdlet);

                parentCmdlet.ThrowTerminatingError(error);
            }

            // Prepare the full assembly name.
            AssemblyName graphicalHostAssemblyName = new AssemblyName();
            graphicalHostAssemblyName.Name = "Microsoft.PowerShell.GraphicalHost";
            graphicalHostAssemblyName.Version = new Version(3, 0, 0, 0);
            graphicalHostAssemblyName.CultureInfo = new CultureInfo(string.Empty); // Neutral culture
            graphicalHostAssemblyName.SetPublicKeyToken(new byte[] { 0x31, 0xbf, 0x38, 0x56, 0xad, 0x36, 0x4e, 0x35 });

            try
            {
                returnValue._graphicalHostAssembly = Assembly.Load(graphicalHostAssemblyName);
            }
            catch (FileNotFoundException fileNotFoundEx)
            {
                // This exception is thrown if the Microsoft.PowerShell.GraphicalHost.dll could not be found (was not installed).
                string errorMessage = StringUtil.Format(
                        HelpErrors.GraphicalHostAssemblyIsNotFound,
                        featureName,
                        fileNotFoundEx.Message);

                parentCmdlet.ThrowTerminatingError(
                    new ErrorRecord(
                        new NotSupportedException(errorMessage, fileNotFoundEx),
                        "ErrorLoadingAssembly",
                        ErrorCategory.ObjectNotFound,
                        graphicalHostAssemblyName));
            }
            catch (Exception e)
            {
                parentCmdlet.ThrowTerminatingError(
                    new ErrorRecord(
                        e,
                        "ErrorLoadingAssembly",
                        ErrorCategory.ObjectNotFound,
                        graphicalHostAssemblyName));
            }

            returnValue._graphicalHostHelperType = returnValue._graphicalHostAssembly.GetType(graphicalHostHelperTypeName);

            Diagnostics.Assert(returnValue._graphicalHostHelperType != null, "the type exists in Microsoft.PowerShell.GraphicalHost");
            ConstructorInfo constructor = returnValue._graphicalHostHelperType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Array.Empty<Type>(),
                null);

            if (constructor != null)
            {
                returnValue._graphicalHostHelperObject = constructor.Invoke(Array.Empty<object>());
                Diagnostics.Assert(returnValue._graphicalHostHelperObject != null, "the constructor does not throw anything");
            }

            return returnValue;
        }

        
        internal static string EscapeBinding(string propertyName)
        {
            return propertyName.Replace("/", " ").Replace(".", " ");
        }

        
        internal object CallMethod(string methodName, params object[] arguments)
        {
            Diagnostics.Assert(_graphicalHostHelperObject != null, "there should be a constructor in order to call an instance method");
            MethodInfo method = _graphicalHostHelperType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Diagnostics.Assert(method != null, "method " + methodName + " exists in graphicalHostHelperType is verified by caller");
            return method.Invoke(_graphicalHostHelperObject, arguments);
        }

        
        internal object CallStaticMethod(string methodName, params object[] arguments)
        {
            MethodInfo method = _graphicalHostHelperType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Diagnostics.Assert(method != null, "method " + methodName + " exists in graphicalHostHelperType is verified by caller");
            return method.Invoke(null, arguments);
        }

        
        internal object GetPropertyValue(string propertyName)
        {
            Diagnostics.Assert(_graphicalHostHelperObject != null, "there should be a constructor in order to get an instance property value");
            PropertyInfo property = _graphicalHostHelperType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            Diagnostics.Assert(property != null, "property " + propertyName + " exists in graphicalHostHelperType is verified by caller");
            return property.GetValue(_graphicalHostHelperObject, Array.Empty<object>());
        }

        
        internal object GetStaticPropertyValue(string propertyName)
        {
            PropertyInfo property = _graphicalHostHelperType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Static);
            Diagnostics.Assert(property != null, "property " + propertyName + " exists in graphicalHostHelperType is verified by caller");
            return property.GetValue(null, Array.Empty<object>());
        }

        
        private static bool IsInputFromRemoting(PSCmdlet parentCmdlet)
        {
            Diagnostics.Assert(parentCmdlet.SessionState != null, "SessionState should always be available.");

            PSVariable senderInfo = parentCmdlet.SessionState.PSVariable.Get("PSSenderInfo");
            return senderInfo != null;
        }
    }
}
