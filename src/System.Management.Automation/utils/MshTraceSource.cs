// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define TRACE

using System.Reflection;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    
    public partial class PSTraceSource
    {
        
        private static readonly object s_getTracerLock = new object();

        
        internal static PSTraceSource GetTracer(
            string name,
            string description)
        {
            return PSTraceSource.GetTracer(name, description, true);
        }

        
        internal static PSTraceSource GetTracer(
            string name,
            string description,
            bool traceHeaders)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            lock (PSTraceSource.s_getTracerLock)
            {
                PSTraceSource result = null;

                // See if we can find an PSTraceSource for this category in the catalog.
                PSTraceSource.TraceCatalog.TryGetValue(name, out result);

                // If it's not already in the catalog, see if we can find it in the
                // pre-configured trace source list

                if (result == null)
                {
                    string keyName = name;
                    if (!PSTraceSource.PreConfiguredTraceSource.ContainsKey(keyName))
                    {
                        if (keyName.Length > 16)
                        {
                            keyName = keyName.Substring(0, 16);
                            if (!PSTraceSource.PreConfiguredTraceSource.ContainsKey(keyName))
                            {
                                keyName = null;
                            }
                        }
                        else
                        {
                            keyName = null;
                        }
                    }

                    if (keyName != null)
                    {
                        // Get the pre-configured trace source from the catalog
                        PSTraceSource preconfiguredSource = PSTraceSource.PreConfiguredTraceSource[keyName];

                        result = PSTraceSource.GetNewTraceSource(keyName, description, traceHeaders);
                        result.Options = preconfiguredSource.Options;
                        result.Listeners.Clear();
                        result.Listeners.AddRange(preconfiguredSource.Listeners);

                        // Add it to the TraceCatalog
                        PSTraceSource.TraceCatalog.Add(keyName, result);

                        // Remove it from the pre-configured catalog
                        PSTraceSource.PreConfiguredTraceSource.Remove(keyName);
                    }
                }

                // Even if there was a PSTraceSource in the catalog, let's replace
                // it with an PSTraceSource to get the added functionality. Anyone using
                // a StructuredTraceSource should be able to do so even with the PSTraceSource
                // instance.

                if (result == null)
                {
                    result = PSTraceSource.GetNewTraceSource(name, description, traceHeaders);
                    PSTraceSource.TraceCatalog[result.FullName] = result;
                }

                if (result.Options != PSTraceSourceOptions.None &&
                    traceHeaders)
                {
                    result.TraceGlobalAppDomainHeader();

                    // Trace the object specific tracer information
                    result.TracerObjectHeader(Assembly.GetCallingAssembly());
                }

                return result;
            }
        }

        internal static PSTraceSource GetNewTraceSource(
            string name,
            string description,
            bool traceHeaders)
        {
            // Note, all callers should have already verified the name before calling this
            // API, so this exception should never be exposed to an end-user.
            ArgumentException.ThrowIfNullOrEmpty(name);

            // Keep the fullName as it was passed, but truncate or pad
            // the category name to 16 characters.  This allows for
            // uniform output

            string fullName = name;
            
            PSTraceSource result =
                new PSTraceSource(
                    fullName,
                    name,
                    description,
                    traceHeaders);
            return result;
        }

        #region TraceFlags.New*Exception methods/helpers

        
        internal static PSArgumentNullException NewArgumentNullException(string paramName)
        {
            ArgumentException.ThrowIfNullOrEmpty(paramName);

            string message = StringUtil.Format(AutomationExceptions.ArgumentNull, paramName);
            var e = new PSArgumentNullException(paramName, message);

            return e;
        }

        
        internal static PSArgumentNullException NewArgumentNullException(
            string paramName, string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw NewArgumentNullException(nameof(paramName));
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException(nameof(resourceString));
            }

            string message = StringUtil.Format(resourceString, args);

            // Note that the paramName param comes first
            var e = new PSArgumentNullException(paramName, message);

            return e;
        }

        
        internal static PSArgumentException NewArgumentException(string paramName)
        {
            ArgumentException.ThrowIfNullOrEmpty(paramName);

            string message = StringUtil.Format(AutomationExceptions.Argument, paramName);

            // Note that the message param comes first
            var e = new PSArgumentException(message, paramName);

            return e;
        }

        
        internal static PSArgumentException NewArgumentException(
            string paramName, string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw NewArgumentNullException(nameof(paramName));
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException(nameof(resourceString));
            }

            string message = StringUtil.Format(resourceString, args);

            // Note that the message param comes first
            var e = new PSArgumentException(message, paramName);

            return e;
        }

        
        internal static PSInvalidOperationException NewInvalidOperationException()
        {
            string message = StringUtil.Format(AutomationExceptions.InvalidOperation,
                    new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name);
            var e = new PSInvalidOperationException(message);

            return e;
        }

        
        internal static PSInvalidOperationException NewInvalidOperationException(
            string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException(nameof(resourceString));
            }

            string message = StringUtil.Format(resourceString, args);

            var e = new PSInvalidOperationException(message);
            return e;
        }

        
        internal static PSInvalidOperationException NewInvalidOperationException(
            Exception innerException,
            string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException(nameof(resourceString));
            }

            string message = StringUtil.Format(resourceString, args);

            var e = new PSInvalidOperationException(message, innerException);
            return e;
        }

        
        internal static PSNotSupportedException NewNotSupportedException()
        {
            string message = StringUtil.Format(AutomationExceptions.NotSupported,
                new System.Diagnostics.StackTrace().GetFrame(0).ToString());
            var e = new PSNotSupportedException(message);

            return e;
        }

        
        internal static PSNotSupportedException NewNotSupportedException(
            string resourceString,
            params object[] args)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException(nameof(resourceString));
            }

            string message = StringUtil.Format(resourceString, args);
            var e = new PSNotSupportedException(message);

            return e;
        }

        
        internal static PSNotImplementedException NewNotImplementedException()
        {
            string message = StringUtil.Format(AutomationExceptions.NotImplemented,
                new System.Diagnostics.StackTrace().GetFrame(0).ToString());
            var e = new PSNotImplementedException(message);

            return e;
        }

        
        internal static PSArgumentOutOfRangeException NewArgumentOutOfRangeException(string paramName, object actualValue)
        {
            ArgumentException.ThrowIfNullOrEmpty(paramName);

            string message = StringUtil.Format(AutomationExceptions.ArgumentOutOfRange, paramName);
            var e = new PSArgumentOutOfRangeException(paramName, actualValue, message);

            return e;
        }

        
        internal static PSArgumentOutOfRangeException NewArgumentOutOfRangeException(
            string paramName, object actualValue, string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw NewArgumentNullException(nameof(paramName));
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException(nameof(resourceString));
            }

            string message = StringUtil.Format(resourceString, args);
            var e = new PSArgumentOutOfRangeException(paramName, actualValue, message);

            return e;
        }

        
        internal static PSObjectDisposedException NewObjectDisposedException(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                throw NewArgumentNullException(nameof(objectName));
            }

            string message = StringUtil.Format(AutomationExceptions.ObjectDisposed, objectName);
            var e = new PSObjectDisposedException(objectName, message);

            return e;
        }

        #endregion TraceFlags.New*Exception methods/helpers
    }
}
