// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Runtime.Serialization;
using System.Text;

namespace System.Management.Automation
{
    
    public class CommandNotFoundException : RuntimeException
    {
        
        internal CommandNotFoundException(
            string commandName,
            Exception innerException,
            string errorIdAndResourceId,
            string resourceStr,
            params object[] messageArgs)
            : base(BuildMessage(commandName, resourceStr, messageArgs), innerException)
        {
            _commandName = commandName;
            _errorId = errorIdAndResourceId;
        }

        
        public CommandNotFoundException() : base() { }

        
        public CommandNotFoundException(string message) : base(message) { }

        
        public CommandNotFoundException(string message, Exception innerException) : base(message, innerException) { }

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected CommandNotFoundException(SerializationInfo info,
                                        StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #region Properties
        
        public override ErrorRecord ErrorRecord
        {
            get
            {
                _errorRecord ??= new ErrorRecord(
                    new ParentContainsErrorRecordException(this),
                    _errorId,
                    _errorCategory,
                    _commandName);

                return _errorRecord;
            }
        }

        private ErrorRecord _errorRecord;

        
        public string CommandName
        {
            get { return _commandName; }

            set { _commandName = value; }
        }

        private string _commandName = string.Empty;

        #endregion Properties

        #region Private
        private readonly string _errorId = "CommandNotFoundException";
        private readonly ErrorCategory _errorCategory = ErrorCategory.ObjectNotFound;

        private static string BuildMessage(
            string commandName,
            string resourceStr,
            params object[] messageArgs
            )
        {
            object[] a;
            if (messageArgs != null && messageArgs.Length > 0)
            {
                a = new object[messageArgs.Length + 1];
                a[0] = commandName;
                messageArgs.CopyTo(a, 1);
            }
            else
            {
                a = new object[1];
                a[0] = commandName;
            }

            return StringUtil.Format(resourceStr, a);
        }
        #endregion Private
    }
    
    public class ScriptRequiresException : RuntimeException
    {
        
        internal ScriptRequiresException(
            string commandName,
            string requiresShellId,
            string requiresShellPath,
            string errorId)
            : base(BuildMessage(commandName, requiresShellId, requiresShellPath, true))
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(commandName), "commandName is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(!string.IsNullOrEmpty(errorId), "errorId is null or empty when constructing ScriptRequiresException");
            _commandName = commandName;
            _requiresShellId = requiresShellId;
            _requiresShellPath = requiresShellPath;
            this.SetErrorId(errorId);
            this.SetTargetObject(commandName);
            this.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }
        
        internal ScriptRequiresException(
            string commandName,
            Version requiresPSVersion,
            string currentPSVersion,
            string errorId)
            : base(BuildMessage(commandName, requiresPSVersion.ToString(), currentPSVersion, false))
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(commandName), "commandName is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(requiresPSVersion != null, "requiresPSVersion is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(!string.IsNullOrEmpty(errorId), "errorId is null or empty when constructing ScriptRequiresException");
            _commandName = commandName;
            _requiresPSVersion = requiresPSVersion;
            this.SetErrorId(errorId);
            this.SetTargetObject(commandName);
            this.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }

        
        internal ScriptRequiresException(
            string commandName,
            Collection<string> missingItems,
            string errorId,
            bool forSnapins)
            : this(commandName, missingItems, errorId, forSnapins, null)
        {
        }

        
        internal ScriptRequiresException(
            string commandName,
            Collection<string> missingItems,
            string errorId,
            bool forSnapins,
            ErrorRecord errorRecord)
            : base(BuildMessage(commandName, missingItems, forSnapins), null, errorRecord)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(commandName), "commandName is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(missingItems != null && missingItems.Count > 0, "missingItems is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(!string.IsNullOrEmpty(errorId), "errorId is null or empty when constructing ScriptRequiresException");
            _commandName = commandName;
            _missingPSSnapIns = new ReadOnlyCollection<string>(missingItems);
            this.SetErrorId(errorId);
            this.SetTargetObject(commandName);
            this.SetErrorCategory(ErrorCategory.ResourceUnavailable);
        }

        
        internal ScriptRequiresException(
            string commandName,
            string errorId)
            : base(BuildMessage(commandName))
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(commandName), "commandName is null or empty when constructing ScriptRequiresException");
            Diagnostics.Assert(!string.IsNullOrEmpty(errorId), "errorId is null or empty when constructing ScriptRequiresException");
            _commandName = commandName;
            this.SetErrorId(errorId);
            this.SetTargetObject(commandName);
            this.SetErrorCategory(ErrorCategory.PermissionDenied);
        }

        
        public ScriptRequiresException() : base() { }

        
        public ScriptRequiresException(string message) : base(message) { }

        
        public ScriptRequiresException(string message, Exception innerException) : base(message, innerException) { }

        #region Serialization
        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")] 
        protected ScriptRequiresException(SerializationInfo info,
                                        StreamingContext context)
        {
            throw new NotSupportedException();
        }
        
        #endregion Serialization

        #region Properties

        
        public string CommandName
        {
            get { return _commandName; }
        }

        private readonly string _commandName = string.Empty;

        
        public Version RequiresPSVersion
        {
            get { return _requiresPSVersion; }
        }

        private readonly Version _requiresPSVersion;

        
        public ReadOnlyCollection<string> MissingPSSnapIns
        {
            get { return _missingPSSnapIns; }
        }

        private readonly ReadOnlyCollection<string> _missingPSSnapIns = new ReadOnlyCollection<string>(Array.Empty<string>());

        
        public string RequiresShellId
        {
            get { return _requiresShellId; }
        }

        private readonly string _requiresShellId;

        
        public string RequiresShellPath
        {
            get { return _requiresShellPath; }
        }

        private readonly string _requiresShellPath;

        #endregion Properties

        #region Private

        private static string BuildMessage(
            string commandName,
            Collection<string> missingItems,
            bool forSnapins)
        {
            StringBuilder sb = new StringBuilder();
            if (missingItems == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(missingItems));
            }

            foreach (string missingItem in missingItems)
            {
                sb.Append(missingItem).Append(", ");
            }

            if (sb.Length > 1)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            if (forSnapins)
            {
                return StringUtil.Format(
                    DiscoveryExceptions.RequiresMissingPSSnapIns,
                    commandName,
                    sb.ToString());
            }
            else
            {
                return StringUtil.Format(
                    DiscoveryExceptions.RequiresMissingModules,
                    commandName,
                    sb.ToString());
            }
        }

        private static string BuildMessage(
            string commandName,
            string first,
            string second,
            bool forShellId)
        {
            string resourceStr = null;

            if (forShellId)
            {
                if (string.IsNullOrEmpty(first))
                {
                    resourceStr = DiscoveryExceptions.RequiresShellIDInvalidForSingleShell;
                }
                else
                {
                    resourceStr = string.IsNullOrEmpty(second)
                            ? DiscoveryExceptions.RequiresInterpreterNotCompatibleNoPath
                            : DiscoveryExceptions.RequiresInterpreterNotCompatible;
                }
            }
            else
            {
                resourceStr = DiscoveryExceptions.RequiresPSVersionNotCompatible;
            }

            return StringUtil.Format(resourceStr, commandName, first, second);
        }

        private static string BuildMessage(string commandName)
        {
            return StringUtil.Format(DiscoveryExceptions.RequiresElevation, commandName);
        }

        #endregion Private
    }
}
