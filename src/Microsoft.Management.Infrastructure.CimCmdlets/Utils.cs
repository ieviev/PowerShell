// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// #define LOGENABLE // uncomment this line to enable the log,
// create c:\temp\cim.log before invoking cimcmdlets

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal static class ConstValue
    {
        
        internal static readonly string[] DefaultSessionName = { @"*" };

        
        internal static readonly string NullComputerName = null;

        
        internal static readonly string[] NullComputerNames = { NullComputerName };

        
        internal static readonly string LocalhostComputerName = @"localhost";

        
        internal static readonly string DefaultNameSpace = @"root\cimv2";

        
        internal static readonly string DefaultQueryDialect = @"WQL";

        
        internal static readonly string ShowComputerNameNoteProperty = "PSShowComputerName";

        
        /// <param name="computerName"></param>
        /// <returns></returns>
        internal static bool IsDefaultComputerName(string computerName)
        {
            return string.IsNullOrEmpty(computerName);
        }

        
        /// <param name="computerNames"></param>
        /// <returns></returns>
        internal static IEnumerable<string> GetComputerNames(IEnumerable<string> computerNames)
        {
            return computerNames ?? NullComputerNames;
        }

        
        /// <param name="computerName"></param>
        /// <returns></returns>
        internal static string GetComputerName(string computerName)
        {
            return string.IsNullOrEmpty(computerName) ? NullComputerName : computerName;
        }

        
        /// <param name="nameSpace"></param>
        /// <returns></returns>
        internal static string GetNamespace(string nameSpace)
        {
            return nameSpace ?? DefaultNameSpace;
        }

        
        /// <param name="queryDialect"></param>
        /// <returns></returns>
        internal static string GetQueryDialectWithDefault(string queryDialect)
        {
            return queryDialect ?? DefaultQueryDialect;
        }
    }

    
    internal static class DebugHelper
    {
        #region private members

        
        internal static bool GenerateLog { get; set; } = true;

        
        private static bool logInitialized = false;

        internal static bool GenerateVerboseMessage { get; set; } = true;

        
        internal static readonly string logFile = @"c:\temp\Cim.log";

        
        internal static readonly string space = @"    ";

        
        internal static readonly string[] spaces = {
                                              string.Empty,
                                              space,
                                              space + space,
                                              space + space + space,
                                              space + space + space + space,
                                              space + space + space + space + space,
                                          };

        
        internal static readonly object logLock = new();

        #endregion

        #region internal strings
        internal static readonly string runspaceStateChanged = "Runspace {0} state changed to {1}";
        internal static readonly string classDumpInfo = @"Class type is {0}";
        internal static readonly string propertyDumpInfo = @"Property name {0} of type {1}, its value is {2}";
        internal static readonly string defaultPropertyType = @"It is a default property, default value is {0}";
        internal static readonly string propertyValueSet = @"This property value is set by user {0}";
        internal static readonly string addParameterSetName = @"Add parameter set {0} name to cache";
        internal static readonly string removeParameterSetName = @"Remove parameter set {0} name from cache";
        internal static readonly string currentParameterSetNameCount = @"Cache have {0} parameter set names";
        internal static readonly string currentParameterSetNameInCache = @"Cache have parameter set {0} valid {1}";
        internal static readonly string currentnonMandatoryParameterSetInCache = @"Cache have optional parameter set {0} valid {1}";
        internal static readonly string optionalParameterSetNameCount = @"Cache have {0} optional parameter set names";
        internal static readonly string finalParameterSetName = @"------Final parameter set name of the cmdlet is {0}";
        internal static readonly string addToOptionalParameterSet = @"Add to optional ParameterSetNames {0}";
        internal static readonly string startToResolveParameterSet = @"------Resolve ParameterSet Name";
        internal static readonly string reservedString = @"------";
        #endregion

        #region runtime methods
        internal static string GetSourceCodeInformation(bool withFileName, int depth)
        {
            StackTrace trace = new();
            StackFrame frame = trace.GetFrame(depth);

            return string.Create(CultureInfo.CurrentUICulture, $"{frame.GetMethod().DeclaringType.Name}::{frame.GetMethod().Name}        ");
        }
        #endregion

        
        /// <param name="message"></param>
        internal static void WriteLog(string message)
        {
            WriteLog(message, 0);
        }

        
        /// <param name="message"></param>
        internal static void WriteEmptyLine()
        {
            WriteLog(string.Empty, 0);
        }

        
        /// <param name="message"></param>
        internal static void WriteLog(string message, int indent, params object[] args)
        {
            string outMessage = string.Empty;
            FormatLogMessage(ref outMessage, message, args);
            WriteLog(outMessage, indent);
        }

        
        /// <param name="message"></param>
        /// <param name="indent"></param>
        internal static void WriteLog(string message, int indent)
        {
            WriteLogInternal(message, indent, -1);
        }

        
        /// <param name="message"></param>
        internal static void WriteLogEx(string message, int indent, params object[] args)
        {
            string outMessage = string.Empty;
            WriteLogInternal(string.Empty, 0, -1);
            FormatLogMessage(ref outMessage, message, args);
            WriteLogInternal(outMessage, indent, 3);
        }

        
        /// <param name="message"></param>
        /// <param name="indent"></param>
        internal static void WriteLogEx(string message, int indent)
        {
            WriteLogInternal(string.Empty, 0, -1);
            WriteLogInternal(message, indent, 3);
        }

        
        /// <param name="message"></param>
        /// <param name="indent"></param>
        internal static void WriteLogEx()
        {
            WriteLogInternal(string.Empty, 0, -1);
            WriteLogInternal(string.Empty, 0, 3);
        }

        
        /// <param name="message"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [Conditional("LOGENABLE")]
        private static void FormatLogMessage(ref string outMessage, string message, params object[] args)
        {
            outMessage = string.Format(CultureInfo.CurrentCulture, message, args);
        }

        
        /// <param name="message"></param>
        /// <param name="nIndent"></param>
        [Conditional("LOGENABLE")]
        private static void WriteLogInternal(string message, int indent, int depth)
        {
            if (!logInitialized)
            {
                lock (logLock)
                {
                    if (!logInitialized)
                    {
                        DebugHelper.GenerateLog = File.Exists(logFile);
                        logInitialized = true;
                    }
                }
            }

            if (GenerateLog)
            {
                if (indent < 0)
                {
                    indent = 0;
                }

                if (indent > 5)
                {
                    indent = 5;
                }

                string sourceInformation = string.Empty;
                if (depth != -1)
                {
                    sourceInformation = string.Format(
                        CultureInfo.InvariantCulture,
                        "Thread {0}#{1}:{2}:{3} {4}",
                        Environment.CurrentManagedThreadId,
                        DateTime.Now.Hour,
                        DateTime.Now.Minute,
                        DateTime.Now.Second,
                        GetSourceCodeInformation(true, depth));
                }

                lock (logLock)
                {
                    using (FileStream fs = new(logFile, FileMode.OpenOrCreate))
                    using (StreamWriter writer = new(fs))
                    {
                        writer.WriteLineAsync(spaces[indent] + sourceInformation + @"        " + message);
                    }
                }
            }
        }
    }

    
    internal static class ValidationHelper
    {
        
        /// <param name="obj"></param>
        /// <param name="argumentName"></param>
        public static void ValidateNoNullArgument(object obj, string argumentName)
        {
            ArgumentNullException.ThrowIfNull(obj, argumentName);
        }

        
        /// <param name="obj"></param>
        /// <param name="argumentName"></param>
        public static void ValidateNoNullorWhiteSpaceArgument(string obj, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(obj))
            {
                throw new ArgumentException(argumentName);
            }
        }

        
        /// <param name="parameterName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Throw if the given value is not a valid name (class name or property name).</exception>
        public static string ValidateArgumentIsValidName(string parameterName, string value)
        {
            DebugHelper.WriteLogEx();
            if (value != null)
            {
                string trimed = value.Trim();
                // The first character should be contained in set: [A-Za-z_]
                // Inner characters should be contained in set: [A-Za-z0-9_]
                Regex regex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*\z");
                if (regex.IsMatch(trimed))
                {
                    DebugHelper.WriteLogEx("A valid name: {0}={1}", 0, parameterName, value);
                    return trimed;
                }
            }

            DebugHelper.WriteLogEx("An invalid name: {0}={1}", 0, parameterName, value);
            throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, CimCmdletStrings.InvalidParameterValue, value, parameterName));
        }

        
        /// <param name="parameterName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Throw if the given value contains any invalid name (class name or property name).</exception>
        public static string[] ValidateArgumentIsValidName(string parameterName, string[] value)
        {
            if (value != null)
            {
                foreach (string propertyName in value)
                {
                    // * is wild char supported in select properties
                    if ((propertyName != null) && string.Equals(propertyName.Trim(), "*", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ValidationHelper.ValidateArgumentIsValidName(parameterName, propertyName);
                }
            }

            return value;
        }
    }
}
