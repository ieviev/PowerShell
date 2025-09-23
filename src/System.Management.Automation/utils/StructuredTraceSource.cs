// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define TRACE

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace System.Management.Automation
{
    #region PSTraceSourceOptions
    
    [Flags]
    public enum PSTraceSourceOptions
    {
        
        None = 0x00000000,

        
        Constructor = 0x00000001,

        
        Dispose = 0x00000002,

        
        Finalizer = 0x00000004,

        
        Method = 0x00000008,

        
        Property = 0x00000010,

        
        Delegates = 0x00000020,

        
        Events = 0x00000040,

        
        Exception = 0x00000080,

        
        Lock = 0x00000100,

        
        Error = 0x00000200,

        
        Warning = 0x00000400,

        
        Verbose = 0x00000800,

        
        WriteLine = 0x00001000,

        
        Scope = 0x00002000,

        
        Assert = 0x00004000,

        
        ExecutionFlow =
            Constructor |
            Dispose |
            Finalizer |
            Method |
            Delegates |
            Events |
            Scope,

        
        Data =
            Constructor |
            Dispose |
            Finalizer |
            Property |
            Verbose |
            WriteLine,

        
        Errors =
            Error |
            Exception,

        
        All =
            Constructor |
            Dispose |
            Finalizer |
            Method |
            Property |
            Delegates |
            Events |
            Exception |
            Error |
            Warning |
            Verbose |
            Lock |
            WriteLine |
            Scope |
            Assert
    }

    #endregion PSTraceSourceOptions

    
    public partial class PSTraceSource
    {
        #region PSTraceSource construction methods

        
        internal PSTraceSource(string fullName, string name, string description, bool traceHeaders)
        {
            ArgumentException.ThrowIfNullOrEmpty(fullName);

            try
            {
                FullName = fullName;
                _name = name;

                // TODO: move this to startup json file instead of using env var
                string tracingEnvVar = Environment.GetEnvironmentVariable("MshEnableTrace");

                if (string.Equals(
                        tracingEnvVar,
                        "True",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string options = this.TraceSource.Attributes["Options"];
                    if (options != null)
                    {
                        _flags = (PSTraceSourceOptions)Enum.Parse(typeof(PSTraceSourceOptions), options, true);
                    }
                }

                ShowHeaders = traceHeaders;
                Description = description;
            }
            catch (System.Xml.XmlException)
            {
                // This exception occurs when the config
                // file is malformed. Just default to Off.

                _flags = PSTraceSourceOptions.None;
            }
#if !CORECLR
            catch (System.Configuration.ConfigurationException)
            {
                // This exception occurs when the config
                // file is malformed. Just default to Off.

                _flags = PSTraceSourceOptions.None;
            }
#endif
        }

        private static bool globalTraceInitialized;

        
        internal void TraceGlobalAppDomainHeader()
        {
            // Only trace the global header if it hasn't
            // already been traced

            if (globalTraceInitialized)
            {
                return;
            }

            // AppDomain

            OutputLine(
                PSTraceSourceOptions.All,
                "Initializing tracing for AppDomain: {0}",
                AppDomain.CurrentDomain.FriendlyName);

            // Current time

            OutputLine(
                PSTraceSourceOptions.All,
                "\tCurrent time: {0}",
                DateTime.Now.ToString());

            // OS build

            OutputLine(
                PSTraceSourceOptions.All,
                "\tOS Build: {0}",
                Environment.OSVersion.ToString());

            // .NET Framework version

            OutputLine(
                PSTraceSourceOptions.All,
                "\tFramework Build: {0}\n",
                Environment.Version.ToString());

            // Mark that we have traced the global header

            globalTraceInitialized = true;
        }

        
        internal void TracerObjectHeader(
            Assembly callingAssembly)
        {
            if (_flags == PSTraceSourceOptions.None)
            {
                return;
            }

            // Write the header for the new trace object

            OutputLine(PSTraceSourceOptions.All, "Creating tracer:");

            // Category

            OutputLine(
                PSTraceSourceOptions.All,
                "\tCategory: {0}",
                this.Name);

            // Description

            OutputLine(
                PSTraceSourceOptions.All,
                "\tDescription: {0}",
                Description);

            if (callingAssembly != null)
            {
                // Assembly name

                OutputLine(
                    PSTraceSourceOptions.All,
                    "\tAssembly: {0}",
                    callingAssembly.FullName);

                // Assembly location

                OutputLine(
                    PSTraceSourceOptions.All,
                    "\tAssembly Location: {0}",
                    callingAssembly.Location);

                // Assembly File timestamp

                FileInfo assemblyFileInfo =
                    new FileInfo(callingAssembly.Location);

                OutputLine(
                    PSTraceSourceOptions.All,
                    "\tAssembly File Timestamp: {0}",
                    assemblyFileInfo.CreationTime.ToString());
            }

            StringBuilder flagBuilder = new StringBuilder();
            // Label

            flagBuilder.Append("\tFlags: ");
            flagBuilder.Append(_flags.ToString());

            // Write out the flags

            OutputLine(PSTraceSourceOptions.All, flagBuilder.ToString());
        }
        #endregion StructuredTraceSource constructor methods

        #region PSTraceSourceOptions.Scope

        internal IDisposable TraceScope(string msg)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Scope))
            {
                try
                {
                    return new ScopeTracer(this, PSTraceSourceOptions.Scope, null, null, string.Empty, msg);
                }
                catch { }
            }

            return null;
        }

        internal IDisposable TraceScope(string format, object arg1)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Scope))
            {
                try
                {
                    return new ScopeTracer(this, PSTraceSourceOptions.Scope, null, null, string.Empty, format, arg1);
                }
                catch { }
            }

            return null;
        }

        internal IDisposable TraceScope(string format, object arg1, object arg2)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Scope))
            {
                try
                {
                    return new ScopeTracer(this, PSTraceSourceOptions.Scope, null, null, string.Empty, format, arg1, arg2);
                }
                catch { }
            }

            return null;
        }

        #endregion PSTraceSourceOptions.Scope

        #region PSTraceSourceOptions.Method methods/helpers
        
        internal IDisposable TraceMethod(
            string format,
            params object[] args)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Method))
            {
                try
                {
                    // Get the name of the method that called this method
                    // 1, signifies the caller of this method, whereas 2
                    // would signify the caller of that method.

                    string methodName = GetCallingMethodNameAndParameters(1);

                    // Create the method tracer object
                    return (IDisposable)new ScopeTracer(
                        this,
                        PSTraceSourceOptions.Method,
                        methodOutputFormatter,
                        methodLeavingFormatter,
                        methodName,
                        format,
                        args);
                }
                catch
                {
                    // Eat all exceptions

                    // Do not assert here because exceptions can be
                    // raised while a thread is shutting down during
                    // normal operation.
                }
            }

            return null;
        }

        #endregion PSTraceSourceOptions.Method methods/helpers

        #region PSTraceSourceOptions.Events methods/helpers

        
        internal IDisposable TraceEventHandlers()
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Events))
            {
                try
                {
                    // Get the name of the method that called this method
                    // 1, signifies the caller of this method, whereas 2
                    // would signify the caller of that method.

                    string methodName = GetCallingMethodNameAndParameters(1);

                    // Create the scope tracer object
                    return (IDisposable)new ScopeTracer(
                        this,
                        PSTraceSourceOptions.Events,
                        eventHandlerOutputFormatter,
                        eventHandlerLeavingFormatter,
                        methodName,
                        string.Empty);
                }
                catch
                {
                    // Eat all exceptions

                    // Do not assert here because exceptions can be
                    // raised while a thread is shutting down during
                    // normal operation.
                }
            }

            return null;
        }

        
        internal IDisposable TraceEventHandlers(
            string format,
            params object[] args)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Events))
            {
                try
                {
                    // Get the name of the method that called this method
                    // 1, signifies the caller of this method, whereas 2
                    // would signify the caller of that method.

                    string methodName = GetCallingMethodNameAndParameters(1);

                    // Create the scope tracer object
                    return (IDisposable)new ScopeTracer(
                        this,
                        PSTraceSourceOptions.Events,
                        eventHandlerOutputFormatter,
                        eventHandlerLeavingFormatter,
                        methodName,
                        format,
                        args);
                }
                catch
                {
                    // Eat all exceptions

                    // Do not assert here because exceptions can be
                    // raised while a thread is shutting down during
                    // normal operation.
                }
            }

            return null;
        }
        #endregion PSTraceSourceOptions.Events methods/helpers

        #region PSTraceSourceOptions.Lock methods/helpers

        
        internal IDisposable TraceLock(string lockName)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Lock))
            {
                try
                {
                    return (IDisposable)new ScopeTracer(
                        this,
                        PSTraceSourceOptions.Lock,
                        lockEnterFormatter,
                        lockLeavingFormatter,
                        lockName);
                }
                catch
                {
                    // Eat all exceptions

                    // Do not assert here because exceptions can be
                    // raised while a thread is shutting down during
                    // normal operation.
                }
            }

            return null;
        }

        
        internal void TraceLockAcquiring(string lockName)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Lock))
            {
                TraceLockHelper(
                    lockAcquiringFormatter,
                    lockName);
            }
        }

        
        internal void TraceLockAcquired(string lockName)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Lock))
            {
                TraceLockHelper(
                    lockEnterFormatter,
                    lockName);
            }
        }

        
        internal void TraceLockReleased(string lockName)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Lock))
            {
                TraceLockHelper(
                    lockLeavingFormatter,
                    lockName);
            }
        }

        
        private void TraceLockHelper(
            string formatter,
            string lockName)
        {
            try
            {
                OutputLine(
                    PSTraceSourceOptions.Lock,
                    formatter,
                    lockName);
            }
            catch
            {
                // Eat all exceptions

                // Do not assert here because exceptions can be
                // raised while a thread is shutting down during
                // normal operation.
            }
        }
        #endregion PSTraceSourceOptions.Lock methods/helpers

        #region PSTraceSourceOptions.Error,Warning,Normal methods/helpers
        
        internal void TraceError(
            string errorMessageFormat,
            params object[] args)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Error))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.Error,
                    errorFormatter,
                    errorMessageFormat,
                    args);
            }
        }

        
        internal void TraceWarning(
            string warningMessageFormat,
            params object[] args)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Warning))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.Warning,
                    warningFormatter,
                    warningMessageFormat,
                    args);
            }
        }

        
        internal void TraceVerbose(
            string verboseMessageFormat,
            params object[] args)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.Verbose))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.Verbose,
                    verboseFormatter,
                    verboseMessageFormat,
                    args);
            }
        }

        
        internal void WriteLine(string format)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.WriteLine))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.WriteLine,
                    writeLineFormatter,
                    format,
                    Array.Empty<object>());
            }
        }

        
        internal void WriteLine(string format, object arg1)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.WriteLine))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.WriteLine,
                    writeLineFormatter,
                    format,
                    new object[] { arg1 });
            }
        }

        internal void WriteLine(string format, bool arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        internal void WriteLine(string format, byte arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        internal void WriteLine(string format, char arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        internal void WriteLine(string format, decimal arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        internal void WriteLine(string format, double arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        internal void WriteLine(string format, float arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        internal void WriteLine(string format, int arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        internal void WriteLine(string format, long arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        internal void WriteLine(string format, uint arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        internal void WriteLine(string format, ulong arg1)
        {
            WriteLine(format, (object)arg1.ToString());
        }

        
        internal void WriteLine(string format, object arg1, object arg2)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.WriteLine))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.WriteLine,
                    writeLineFormatter,
                    format,
                    new object[] { arg1, arg2 });
            }
        }

        
        internal void WriteLine(string format, object arg1, object arg2, object arg3)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.WriteLine))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.WriteLine,
                    writeLineFormatter,
                    format,
                    new object[] { arg1, arg2, arg3 });
            }
        }

        
        internal void WriteLine(string format, object arg1, object arg2, object arg3, object arg4)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.WriteLine))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.WriteLine,
                    writeLineFormatter,
                    format,
                    new object[] { arg1, arg2, arg3, arg4 });
            }
        }

        
        internal void WriteLine(string format, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.WriteLine))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.WriteLine,
                    writeLineFormatter,
                    format,
                    new object[] { arg1, arg2, arg3, arg4, arg5 });
            }
        }

        
        internal void WriteLine(string format, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.WriteLine))
            {
                FormatOutputLine(
                    PSTraceSourceOptions.WriteLine,
                    writeLineFormatter,
                    format,
                    new object[] { arg1, arg2, arg3, arg4, arg5, arg6 });
            }
        }

        
        internal void WriteLine(object arg)
        {
            if (_flags.HasFlag(PSTraceSourceOptions.WriteLine))
            {
                WriteLine("{0}", arg == null ? "null" : arg.ToString());
            }
        }

        
        private void FormatOutputLine(
            PSTraceSourceOptions flag,
            string classFormatter,
            string format,
            params object[] args)
        {
            try
            {
                // First format the class format string and the
                // user provided format string together
                StringBuilder output = new StringBuilder();

                if (classFormatter != null)
                {
                    output.Append(classFormatter);
                }

                if (format != null)
                {
                    output.AppendFormat(
                        CultureInfo.CurrentCulture,
                        format,
                        args);
                }

                // finally trace the output
                OutputLine(flag, output.ToString());
            }
            catch
            {
                // Eat all exceptions
                //
                // Do not assert here because exceptions can be
                // raised while a thread is shutting down during
                // normal operation.
            }
        }

        #endregion PSTraceSourceOptions.Error methods/helpers

        #region Class helper methods and properties

        
        private static string GetCallingMethodNameAndParameters(int skipFrames)
        {
            StringBuilder methodAndParameters = null;

            try
            {
                // Use the stack to get the method and type information
                // for the calling method

                StackFrame stackFrame = new StackFrame(++skipFrames);
                MethodBase callingMethod = stackFrame.GetMethod();

                Type declaringType = callingMethod.DeclaringType;

                // Append the class name and method name together

                methodAndParameters = new StringBuilder();

                // Note: don't use the FullName for the declaringType
                // as it is usually way too long and makes the trace
                // output hard to read.

                methodAndParameters.AppendFormat(
                    CultureInfo.CurrentCulture,
                    "{0}.{1}(",
                    declaringType.Name,
                    callingMethod.Name);

                methodAndParameters.Append(')');
            }
            catch
            {
                // Eat all exceptions

                // Do not assert here because exceptions can be
                // raised while a thread is shutting down during
                // normal operation.
            }

            return methodAndParameters.ToString();
        }

        // The default formatter for TraceError
        private const string errorFormatter =
            "ERROR: ";

        // The default formatter for TraceWarning
        private const string warningFormatter =
            "Warning: ";

        // The default formatter for TraceVerbose
        private const string verboseFormatter =
            "Verbose: ";

        // The default formatter for WriteLine
        private const string writeLineFormatter =
            "";

        // The default formatter for TraceConstructor

        private const string constructorOutputFormatter =
            "Enter Ctor {0}";

        private const string constructorLeavingFormatter =
            "Leave Ctor {0}";

        // The default formatter for TraceDispose

        private const string disposeOutputFormatter =
            "Enter Disposer {0}";

        private const string disposeLeavingFormatter =
            "Leave Disposer {0}";

        // The default formatter for TraceMethod

        private const string methodOutputFormatter =
            "Enter {0}:";

        private const string methodLeavingFormatter =
            "Leave {0}";

        // The default formatter for TraceProperty

        private const string propertyOutputFormatter =
            "Enter property {0}:";

        private const string propertyLeavingFormatter =
            "Leave property {0}";

        // The default formatter for TraceDelegateHandler

        private const string delegateHandlerOutputFormatter =
            "Enter delegate handler: {0}:";

        private const string delegateHandlerLeavingFormatter =
            "Leave delegate handler: {0}";

        // The default formatter for TraceEventHandlers

        private const string eventHandlerOutputFormatter =
            "Enter event handler: {0}:";

        private const string eventHandlerLeavingFormatter =
            "Leave event handler: {0}";

        // The default formatters for TraceException

        private const string exceptionOutputFormatter =
            "{0}: {1}\n{2}";

        private const string innermostExceptionOutputFormatter =
            "Inner-most {0}: {1}\n{2}";

        // The default formatters for TraceLock

        private const string lockEnterFormatter =
            "Enter Lock: {0}";

        private const string lockLeavingFormatter =
            "Leave Lock: {0}";

        private const string lockAcquiringFormatter =
            "Acquiring Lock: {0}";

        private static StringBuilder GetLinePrefix(PSTraceSourceOptions flag)
        {
            StringBuilder prefixBuilder = new StringBuilder();

            // Add the flag that caused this line to be traced

            prefixBuilder.AppendFormat(
                CultureInfo.CurrentCulture,
                " {0,-11} ",
                Enum.GetName(typeof(PSTraceSourceOptions), flag));
            return prefixBuilder;
        }

        private static void AddTab(StringBuilder lineBuilder)
        {
            // The Trace.IndentSize does not change at all
            // through the running of the process so there
            // are no thread issues here.
            int indentSize = Trace.IndentSize;
            int threadIndentLevel = ThreadIndentLevel;

            lineBuilder.Append(System.Management.Automation.Internal.StringUtil.Padding(indentSize * threadIndentLevel));
        }

        // used to find and blocks cyclic-loops in tracing.

        private bool _alreadyTracing = false;
        
        internal void OutputLine(
            PSTraceSourceOptions flag,
            string format,
            string arg = null)
        {
            // if already tracing something for this current TraceSource,
            // dont trace again. This will block cyclic-loops from happening.
            if (_alreadyTracing)
            {
                return;
            }

            _alreadyTracing = true;
            try
            {
                Diagnostics.Assert(
                    format != null,
                    "The format string should not be null");

                StringBuilder lineBuilder = new StringBuilder();

                if (ShowHeaders)
                {
                    // Get the line prefix string which includes things
                    // like App name, clock tick, thread ID, etc.
                    lineBuilder.Append(GetLinePrefix(flag));
                }

                // Add the spaces for the indent
                AddTab(lineBuilder);

                if (arg != null)
                {
                    lineBuilder.AppendFormat(
                        CultureInfo.CurrentCulture,
                        format,
                        arg);
                }
                else
                {
                    lineBuilder.Append(format);
                }

                this.TraceSource.TraceInformation(lineBuilder.ToString());
            }
            finally
            {
                // reset tracing for the current trace source..
                // so future traces can go through.
                _alreadyTracing = false;
            }
        }

        
        internal static int ThreadIndentLevel
        {
            get
            {
                // The first time access the ThreadLocal instance, the default int value will be used
                // to initialize the instance. The default int value is 0.
                return s_localIndentLevel.Value;
            }

            set
            {
                if (value >= 0)
                {
                    // Set the new indent level in thread local storage
                    s_localIndentLevel.Value = value;
                }
                else
                {
                    Diagnostics.Assert(value >= 0, "The indention value cannot be less than zero");
                }
            }
        }

        
        private static readonly ThreadLocal<int> s_localIndentLevel = new ThreadLocal<int>();

        
        private PSTraceSourceOptions _flags = PSTraceSourceOptions.None;

        
        public string Description { get; set; } = string.Empty;

        
        internal bool ShowHeaders { get; set; } = true;

        
        internal string FullName { get; } = string.Empty;

        private readonly string _name;

        
        internal TraceSource TraceSource
        {
            get { return _traceSource ??= new MonadTraceSource(_name); }
        }

        private TraceSource _traceSource;

        #endregion Class helper methods and properties

        #region Public members

        
        public PSTraceSourceOptions Options
        {
            get
            {
                return _flags;
            }

            set
            {
                _flags = value;
                this.TraceSource.Switch.Level = (SourceLevels)_flags;
            }
        }

        internal bool IsEnabled
        {
            get { return _flags != PSTraceSourceOptions.None; }
        }

        
        public StringDictionary Attributes
        {
            get
            {
                return TraceSource.Attributes;
            }
        }

        
        public TraceListenerCollection Listeners
        {
            get
            {
                return TraceSource.Listeners;
            }
        }

        
        public string Name
        {
            get
            {
                return _name;
            }
        }

        
        public SourceSwitch Switch
        {
            get
            {
                return TraceSource.Switch;
            }

            set
            {
                TraceSource.Switch = value;
            }
        }
        #endregion Public members

        #region TraceCatalog

        
        internal static Dictionary<string, PSTraceSource> TraceCatalog { get; } = new Dictionary<string, PSTraceSource>(StringComparer.OrdinalIgnoreCase);

        
        internal static Dictionary<string, PSTraceSource> PreConfiguredTraceSource { get; } = new Dictionary<string, PSTraceSource>(StringComparer.OrdinalIgnoreCase);

        #endregion TraceCatalog
    }

    #region ScopeTracer object/helpers
    
    internal class ScopeTracer : IDisposable
    {
        
        internal ScopeTracer(
            PSTraceSource tracer,
            PSTraceSourceOptions flag,
            string scopeOutputFormatter,
            string leavingScopeFormatter,
            string scopeName)
        {
            _tracer = tracer;

            // Call the helper

            ScopeTracerHelper(
                flag,
                scopeOutputFormatter,
                leavingScopeFormatter,
                scopeName,
                string.Empty);
        }

        
        internal ScopeTracer(
            PSTraceSource tracer,
            PSTraceSourceOptions flag,
            string scopeOutputFormatter,
            string leavingScopeFormatter,
            string scopeName,
            string format,
            params object[] args)
        {
            _tracer = tracer;

            // Call the helper

            if (format != null)
            {
                ScopeTracerHelper(
                    flag,
                    scopeOutputFormatter,
                    leavingScopeFormatter,
                    scopeName,
                    format,
                    args);
            }
            else
            {
                ScopeTracerHelper(
                    flag,
                    scopeOutputFormatter,
                    leavingScopeFormatter,
                    scopeName,
                    string.Empty);
            }
        }

        
        internal void ScopeTracerHelper(
            PSTraceSourceOptions flag,
            string scopeOutputFormatter,
            string leavingScopeFormatter,
            string scopeName,
            string format,
            params object[] args)
        {
            // Store the flags, scopeName, and the leavingScopeFormatter
            // so that it can be used in the Dispose method

            _flag = flag;
            _scopeName = scopeName;
            _leavingScopeFormatter = leavingScopeFormatter;

            // Format the string for output

            StringBuilder output = new StringBuilder();

            if (!string.IsNullOrEmpty(scopeOutputFormatter))
            {
                output.AppendFormat(
                    CultureInfo.CurrentCulture,
                    scopeOutputFormatter,
                    _scopeName);
            }

            if (!string.IsNullOrEmpty(format))
            {
                output.AppendFormat(
                    CultureInfo.CurrentCulture,
                    format,
                    args);
            }

            // Now write the trace

            _tracer.OutputLine(_flag, output.ToString());

            // Increment the current thread indent level

            PSTraceSource.ThreadIndentLevel++;
        }

        
        public void Dispose()
        {
            // Decrement the indent level in thread local storage

            PSTraceSource.ThreadIndentLevel--;

            // Trace out the scope name

            if (!string.IsNullOrEmpty(_leavingScopeFormatter))
            {
                _tracer.OutputLine(_flag, _leavingScopeFormatter, _scopeName);
            }

            GC.SuppressFinalize(this);
        }

        
        private readonly PSTraceSource _tracer;

        
        private PSTraceSourceOptions _flag;

        
        private string _scopeName;

        
        private string _leavingScopeFormatter;
    }
    #endregion ScopeTracer object/helpers

    #region PSTraceSourceAttribute
    
    [AttributeUsage(
         AttributeTargets.Field,
         AllowMultiple = false)]
    internal class TraceSourceAttribute : Attribute
    {
        
        internal TraceSourceAttribute(
            string category,
            string description)
        {
            Category = category;
            Description = description;
        }

        
        internal string Category { get; }

        
        internal string Description { get; set; }
    }
    #endregion TraceSourceAttribute

    #region MonadTraceSource

    
    internal class MonadTraceSource : TraceSource
    {
        internal MonadTraceSource(string name)
            : base(name)
        {
        }

        
        protected override string[] GetSupportedAttributes()
        {
            return new string[] { "Options" };
        }
    }
    #endregion MonadTraceSource
}
