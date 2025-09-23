// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public sealed class SessionState
    {
        #region Constructors

        
        internal SessionState(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        
        internal SessionState(ExecutionContext context, bool createAsChild, bool linkToGlobal)
        {
            if (context == null)
                throw new InvalidOperationException("ExecutionContext");

            if (createAsChild)
            {
                _sessionState = new SessionStateInternal(context.EngineSessionState, linkToGlobal, context);
            }
            else
            {
                _sessionState = new SessionStateInternal(context);
            }

            _sessionState.PublicSessionState = this;
        }

        
        public SessionState()
        {
            ExecutionContext ecFromTLS = LocalPipeline.GetExecutionContextFromTLS();
            if (ecFromTLS == null)
                throw new InvalidOperationException("ExecutionContext");

            _sessionState = new SessionStateInternal(ecFromTLS);
            _sessionState.PublicSessionState = this;
        }

        #endregion Constructors

        #region Public methods

        
        public DriveManagementIntrinsics Drive
        {
            get { return _drive ??= new DriveManagementIntrinsics(_sessionState); }
        }

        
        public CmdletProviderManagementIntrinsics Provider
        {
            get { return _provider ??= new CmdletProviderManagementIntrinsics(_sessionState); }
        }

        
        public PathIntrinsics Path
        {
            get { return _path ??= new PathIntrinsics(_sessionState); }
        }

        
        public PSVariableIntrinsics PSVariable
        {
            get { return _variable ??= new PSVariableIntrinsics(_sessionState); }
        }

        
        public PSLanguageMode LanguageMode
        {
            get { return _sessionState.LanguageMode; }

            set { _sessionState.LanguageMode = value; }
        }

        
        public bool UseFullLanguageModeInDebugger
        {
            get { return _sessionState.UseFullLanguageModeInDebugger; }
        }

        
        public List<string> Scripts
        {
            get { return _sessionState.Scripts; }
        }

        
        public List<string> Applications
        {
            get { return _sessionState.Applications; }
        }

        
        public PSModuleInfo Module
        {
            get { return _sessionState.Module; }
        }

        
        public ProviderIntrinsics InvokeProvider
        {
            get { return _sessionState.InvokeProvider; }
        }

        
        public CommandInvocationIntrinsics InvokeCommand
        {
            get { return _sessionState.ExecutionContext.EngineIntrinsics.InvokeCommand; }
        }

        
        public static void ThrowIfNotVisible(CommandOrigin origin, object valueToCheck)
        {
            SessionStateException exception;
            if (!IsVisible(origin, valueToCheck))
            {
                PSVariable sv = valueToCheck as PSVariable;
                if (sv != null)
                {
                    exception =
                       new SessionStateException(
                           sv.Name,
                           SessionStateCategory.Variable,
                           "VariableIsPrivate",
                           SessionStateStrings.VariableIsPrivate,
                           ErrorCategory.PermissionDenied);

                    throw exception;
                }

                CommandInfo cinfo = valueToCheck as CommandInfo;
                if (cinfo != null)
                {
                    string commandName = cinfo.Name;
                    if (commandName != null)
                    {
                        // If we have a name, use it in the error message
                        exception =
                            new SessionStateException(
                                commandName,
                                SessionStateCategory.Command,
                                "NamedCommandIsPrivate",
                                SessionStateStrings.NamedCommandIsPrivate,
                                ErrorCategory.PermissionDenied);
                    }
                    else
                    {
                        exception =
                            new SessionStateException(
                                string.Empty,
                                SessionStateCategory.Command,
                                "CommandIsPrivate",
                                SessionStateStrings.CommandIsPrivate,
                                ErrorCategory.PermissionDenied);
                    }

                    throw exception;
                }

                // Catch all error for other types of resources...
                exception =
                    new SessionStateException(
                        null,
                        SessionStateCategory.Resource,
                        "ResourceIsPrivate",
                        SessionStateStrings.ResourceIsPrivate,
                        ErrorCategory.PermissionDenied);

                throw exception;
            }
        }

        
        public static bool IsVisible(CommandOrigin origin, object valueToCheck)
        {
            if (origin == CommandOrigin.Internal)
                return true;
            IHasSessionStateEntryVisibility obj = valueToCheck as IHasSessionStateEntryVisibility;
            if (obj != null)
            {
                return (obj.Visibility == SessionStateEntryVisibility.Public);
            }

            return true;
        }
        
        public static bool IsVisible(CommandOrigin origin, PSVariable variable)
        {
            if (origin == CommandOrigin.Internal)
                return true;
            if (variable == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(variable));
            }

            return (variable.Visibility == SessionStateEntryVisibility.Public);
        }
        
        public static bool IsVisible(CommandOrigin origin, CommandInfo commandInfo)
        {
            if (origin == CommandOrigin.Internal)
                return true;
            if (commandInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandInfo));
            }

            return (commandInfo.Visibility == SessionStateEntryVisibility.Public);
        }

        #endregion Public methods

        #region Internal methods

        
        internal SessionStateInternal Internal
        {
            get { return _sessionState; }
        }
        #endregion Internal methods

        #region private data

        private readonly SessionStateInternal _sessionState;
        private DriveManagementIntrinsics _drive;
        private CmdletProviderManagementIntrinsics _provider;
        private PathIntrinsics _path;
        private PSVariableIntrinsics _variable;

        #endregion private data
    }

    
    public enum SessionStateEntryVisibility
    {
        
        Public = 0,

        
        Private = 1
    }

#nullable enable
    internal interface IHasSessionStateEntryVisibility
    {
        SessionStateEntryVisibility Visibility { get; set; }
    }

    
    public enum PSLanguageMode
    {
        
        FullLanguage = 0,

        
        RestrictedLanguage = 1,

        
        NoLanguage = 2,

        
        ConstrainedLanguage = 3
    }
}
