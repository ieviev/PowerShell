// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Resources;
using System.Runtime.Serialization;
using System.Reflection;
using System.Management.Automation.Language;
using System.Security.Permissions;

namespace System.Management.Automation
{
    
    public enum ErrorCategory
    {
        
        NotSpecified = 0,

        
        OpenError = 1,

        
        CloseError = 2,

        
        DeviceError = 3,

        
        DeadlockDetected = 4,

        
        InvalidArgument = 5,

        
        InvalidData = 6,

        
        InvalidOperation = 7,

        
        InvalidResult = 8,

        
        InvalidType = 9,

        
        MetadataError = 10,

        
        NotImplemented = 11,

        
        NotInstalled = 12,

        
        ObjectNotFound = 13,

        
        OperationStopped = 14,

        
        OperationTimeout = 15,

        
        SyntaxError = 16,

        
        ParserError = 17,

        
        PermissionDenied = 18,

        
        ResourceBusy = 19,

        
        ResourceExists = 20,

        
        ResourceUnavailable = 21,

        
        ReadError = 22,

        
        WriteError = 23,

        
        FromStdErr = 24,

        
        SecurityError = 25,

        
        ProtocolError = 26,

        
        ConnectionError = 27,

        
        AuthenticationError = 28,

        
        LimitsExceeded = 29,

        
        QuotaExceeded = 30,

        
        NotEnabled = 31,
    }

    
    public class ErrorCategoryInfo
    {
        #region ctor
        internal ErrorCategoryInfo(ErrorRecord errorRecord)
        {
            ArgumentNullException.ThrowIfNull(errorRecord);

            _errorRecord = errorRecord;
        }
        #endregion ctor

        #region Properties
        
        public ErrorCategory Category
        {
            get { return _errorRecord._category; }
        }

        
        public string Activity
        {
            get
            {
                if (!string.IsNullOrEmpty(_errorRecord._activityOverride))
                {
                    return _errorRecord._activityOverride;
                }

                if (_errorRecord.InvocationInfo != null
                    && (_errorRecord.InvocationInfo.MyCommand is CmdletInfo || _errorRecord.InvocationInfo.MyCommand is IScriptCommandInfo)
                    && !string.IsNullOrEmpty(_errorRecord.InvocationInfo.MyCommand.Name)
                    )
                {
                    return _errorRecord.InvocationInfo.MyCommand.Name;
                }

                return string.Empty;
            }

            set
            {
                _errorRecord._activityOverride = value;
            }
        }

        
        public string Reason
        {
            get
            {
                _reasonIsExceptionType = false;
                if (!string.IsNullOrEmpty(_errorRecord._reasonOverride))
                {
                    return _errorRecord._reasonOverride;
                }

                if (_errorRecord.Exception != null)
                {
                    _reasonIsExceptionType = true;
                    return _errorRecord.Exception.GetType().Name;
                }

                return string.Empty;
            }

            set
            {
                _errorRecord._reasonOverride = value;
            }
        }

        private bool _reasonIsExceptionType;

        
        public string TargetName
        {
            get
            {
                if (!string.IsNullOrEmpty(_errorRecord._targetNameOverride))
                {
                    return _errorRecord._targetNameOverride;
                }

                if (_errorRecord.TargetObject != null)
                {
                    string targetInString;
                    try
                    {
                        targetInString = _errorRecord.TargetObject.ToString();
                    }
                    catch (Exception)
                    {
                        targetInString = null;
                    }

                    return ErrorRecord.NotNull(targetInString);
                }

                return string.Empty;
            }

            set
            {
                _errorRecord._targetNameOverride = value;
            }
        }

        
        public string TargetType
        {
            get
            {
                if (!string.IsNullOrEmpty(_errorRecord._targetTypeOverride))
                {
                    return _errorRecord._targetTypeOverride;
                }

                if (_errorRecord.TargetObject != null)
                {
                    return _errorRecord.TargetObject.GetType().Name;
                }

                return string.Empty;
            }

            set
            {
                _errorRecord._targetTypeOverride = value;
            }
        }

        #endregion Properties

        #region Methods
        
        public string GetMessage()
        {
            

            return GetMessage(CultureInfo.CurrentUICulture);
        }

        
        public string GetMessage(CultureInfo uiCultureInfo)
        {
            // get template text
            string errorCategoryString = Category.ToString();
            if (string.IsNullOrEmpty(errorCategoryString))
            {
                // this probably indicates an invalid ErrorCategory value
                errorCategoryString = nameof(ErrorCategory.NotSpecified);
            }

            string templateText = ErrorCategoryStrings.ResourceManager.GetString(errorCategoryString, uiCultureInfo);

            if (string.IsNullOrEmpty(templateText))
            {
                // this probably indicates an invalid ErrorCategory value
                templateText = ErrorCategoryStrings.NotSpecified;
            }

            Diagnostics.Assert(!string.IsNullOrEmpty(templateText),
                "ErrorCategoryStrings.resx resource failure");

            string activityInUse = Ellipsize(uiCultureInfo, Activity);
            string targetNameInUse = Ellipsize(uiCultureInfo, TargetName);
            string targetTypeInUse = Ellipsize(uiCultureInfo, TargetType);
            // if the reason is a exception type name, we should output the whole name
            string reasonInUse = Reason;
            reasonInUse = _reasonIsExceptionType ? reasonInUse : Ellipsize(uiCultureInfo, reasonInUse);

            // assemble final string
            try
            {
                return string.Format(uiCultureInfo, templateText,
                    activityInUse,
                    targetNameInUse,
                    targetTypeInUse,
                    reasonInUse,
                    errorCategoryString);
            }
            catch (FormatException)
            {
                templateText = ErrorCategoryStrings.InvalidErrorCategory;

                return string.Format(uiCultureInfo, templateText,
                    activityInUse,
                    targetNameInUse,
                    targetTypeInUse,
                    reasonInUse,
                    errorCategoryString);
            }
        }

        
        public override string ToString()
        {
            return GetMessage(CultureInfo.CurrentUICulture);
        }
        #endregion Methods

        #region Private
        // back-reference for facade class
        private readonly ErrorRecord _errorRecord;

        
        internal static string Ellipsize(CultureInfo uiCultureInfo, string original)
        {
            if (original.Length <= 40)
            {
                return original;
            }

            // We are splitting a string > 40 chars in half, so left and right can be
            // at most 19 characters to include the ellipsis in the middle.
            const int MaxHalfWidth = 19;
            string first = original.Substring(0, MaxHalfWidth);
            string last = original.Substring(original.Length - MaxHalfWidth, MaxHalfWidth);
            return
                string.Format(uiCultureInfo, ErrorPackage.Ellipsize, first, last);
        }
        #endregion Private
    }

    
    public class ErrorDetails : ISerializable
    {
        #region Constructor
        
        public ErrorDetails(string message)
        {
            _message = message;
        }

        #region UseResourceId
        
        public ErrorDetails(
            Cmdlet cmdlet,
            string baseName,
            string resourceId,
            params object[] args)
        {
            _message = BuildMessage(cmdlet, baseName, resourceId, args);
        }
        
        public ErrorDetails(
            IResourceSupplier resourceSupplier,
            string baseName,
            string resourceId,
            params object[] args)
        {
            _message = BuildMessage(resourceSupplier, baseName, resourceId, args);
        }
        
        public ErrorDetails(
            System.Reflection.Assembly assembly,
            string baseName,
            string resourceId,
            params object[] args)
        {
            _message = BuildMessage(assembly, baseName, resourceId, args);
        }
        #endregion UseResourceId

        // deep-copy constructor
        internal ErrorDetails(ErrorDetails errorDetails)
        {
            _message = errorDetails._message;
            _recommendedAction = errorDetails._recommendedAction;
        }
        #endregion Constructor

        #region Serialization
        
        protected ErrorDetails(SerializationInfo info,
                               StreamingContext context)
        {
            _message = info.GetString("ErrorDetails_Message");
            _recommendedAction = info.GetString(
                "ErrorDetails_RecommendedAction");
        }

        
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info != null)
            {
                info.AddValue("ErrorDetails_Message", _message);
                info.AddValue("ErrorDetails_RecommendedAction",
                    _recommendedAction);
            }
        }
        #endregion Serialization

        #region Public Properties
        
        public string Message
        {
            get { return ErrorRecord.NotNull(_message); }
        }

        private readonly string _message = string.Empty;

        
        public string RecommendedAction
        {
            get
            {
                return ErrorRecord.NotNull(_recommendedAction);
            }

            set
            {
                _recommendedAction = value;
            }
        }

        private string _recommendedAction = string.Empty;
        #endregion Public Properties

        #region Internal Properties
        internal Exception TextLookupError
        {
            get { return _textLookupError; }

            set { _textLookupError = value; }
        }

        private Exception _textLookupError ;
        #endregion Internal Properties

        #region ToString
        
        public override string ToString()
        {
            return Message;
        }
        #endregion ToString

        #region Private
        private string BuildMessage(
            Cmdlet cmdlet,
            string baseName,
            string resourceId,
            params object[] args)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(baseName));
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceId));
            }

            string template = string.Empty;

            try
            {
                template = cmdlet.GetResourceString(baseName, resourceId);
            }
            catch (MissingManifestResourceException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }
            catch (ArgumentException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }

            return BuildMessage(template, baseName, resourceId, args);
        }

        private string BuildMessage(
            IResourceSupplier resourceSupplier,
            string baseName,
            string resourceId,
            params object[] args)
        {
            if (resourceSupplier == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceSupplier));
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(baseName));
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceId));
            }

            string template = string.Empty;

            try
            {
                template = resourceSupplier.GetResourceString(baseName, resourceId);
            }
            catch (MissingManifestResourceException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }
            catch (ArgumentException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }

            return BuildMessage(template, baseName, resourceId, args);
        }

        private string BuildMessage(
            System.Reflection.Assembly assembly,
            string baseName,
            string resourceId,
            params object[] args)
        {
            if (assembly == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(assembly));
            }

            if (string.IsNullOrEmpty(baseName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(baseName));
            }

            if (string.IsNullOrEmpty(resourceId))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resourceId));
            }

            string template = string.Empty;

            ResourceManager manager =
                ResourceManagerCache.GetResourceManager(
                        assembly, baseName);
            try
            {
                template = manager.GetString(
                    resourceId,
                    CultureInfo.CurrentUICulture);
            }
            catch (MissingManifestResourceException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }

            return BuildMessage(template, baseName, resourceId, args);
        }

        private string BuildMessage(
            string template,
            string baseName,
            string resourceId,
            params object[] args)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                _textLookupError = PSTraceSource.NewInvalidOperationException(
                    ErrorPackage.ErrorDetailsEmptyTemplate,
                    baseName,
                    resourceId);
                return string.Empty; // fallback to Exception.Message
            }

            try
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    template,
                    args);
            }
            catch (FormatException e)
            {
                _textLookupError = e;
                return string.Empty; // fallback to Exception.Message
            }
        }
        #endregion Private

    }

    
    public class ErrorRecord : ISerializable
    {
        #region Constructor

        private ErrorRecord()
        {
        }

        
        public ErrorRecord(
            Exception exception,
            string errorId,
            ErrorCategory errorCategory,
            object targetObject)
        {
            if (exception == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(exception));
            }

            errorId ??= string.Empty;

            // targetObject may be null
            _error = exception;
            _errorId = errorId;
            _category = errorCategory;
            _target = targetObject;
        }

        #region Serialization

        // We serialize the exception as its original type, ensuring
        // that the ErrorRecord information arrives in full, but taking
        // the risk that it cannot be serialized/deserialized at all if
        // (1) the exception type does not exist on the target machine, or
        // (2) the exception serializer/deserializer fails or is not
        // implemented/supported.
        //
        // We do not attempt to serialize TargetObject.
        //
        // We do not attempt to serialize InvocationInfo.  There is
        // potentially some useful information there, but serializing
        // InvocationInfo, Token, InternalCommand and its subclasses, and
        // CommandInfo and its subclasses is too expensive.

        
        protected ErrorRecord(SerializationInfo info,
                              StreamingContext context)
        {
            PSObject psObject = PSObject.ConstructPSObjectFromSerializationInfo(info, context);
            ConstructFromPSObjectForRemoting(psObject);
        }

        
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info != null)
            {
                PSObject psObject = RemotingEncoder.CreateEmptyPSObject();

                // for binary serialization always serialize the extended info
                ToPSObjectForRemoting(psObject, true);

                psObject.GetObjectData(info, context);
            }
        }
        #endregion Serialization

        #region Remoting

        
        private bool _isSerialized = false;

        
        internal bool IsSerialized { get => _isSerialized; }

        
        private string _serializedFullyQualifiedErrorId = null;

        
        internal string _serializedErrorCategoryMessageOverride = null;

        
        internal ErrorRecord(
            Exception exception,
            object targetObject,
            string fullyQualifiedErrorId,
            ErrorCategory errorCategory,
            string errorCategory_Activity,
            string errorCategory_Reason,
            string errorCategory_TargetName,
            string errorCategory_TargetType,
            string errorCategory_Message,
            string errorDetails_Message,
            string errorDetails_RecommendedAction)
        {
            PopulateProperties(
                exception, targetObject, fullyQualifiedErrorId, errorCategory, errorCategory_Activity,
                errorCategory_Reason, errorCategory_TargetName, errorCategory_TargetType,
                errorCategory_Message, errorDetails_Message, errorDetails_RecommendedAction, null);
        }

        private void PopulateProperties(
            Exception exception,
            object targetObject,
            string fullyQualifiedErrorId,
            ErrorCategory errorCategory,
            string errorCategory_Activity,
            string errorCategory_Reason,
            string errorCategory_TargetName,
            string errorCategory_TargetType,
            string errorCategory_Message,
            string errorDetails_Message,
            string errorDetails_RecommendedAction,
            string errorDetails_ScriptStackTrace)
        {
            if (exception == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(exception));
            }

            if (fullyQualifiedErrorId == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(fullyQualifiedErrorId));
            }

            // Mark this error record as serialized
            _isSerialized = true;
            _error = exception;
            _target = targetObject;
            _serializedFullyQualifiedErrorId = fullyQualifiedErrorId;
            _category = errorCategory;
            _activityOverride = errorCategory_Activity;
            _reasonOverride = errorCategory_Reason;
            _targetNameOverride = errorCategory_TargetName;
            _targetTypeOverride = errorCategory_TargetType;
            _serializedErrorCategoryMessageOverride = errorCategory_Message;
            if (errorDetails_Message != null)
            {
                ErrorDetails = new ErrorDetails(errorDetails_Message);
                if (errorDetails_RecommendedAction != null)
                {
                    ErrorDetails.RecommendedAction = errorDetails_RecommendedAction;
                }
            }

            _scriptStackTrace = errorDetails_ScriptStackTrace;
        }

        
        internal void ToPSObjectForRemoting(PSObject dest)
        {
            ToPSObjectForRemoting(dest, SerializeExtendedInfo);
        }

        private void ToPSObjectForRemoting(PSObject dest, bool serializeExtInfo)
        {
            RemotingEncoder.AddNoteProperty<Exception>(dest, "Exception", () => Exception);
            RemotingEncoder.AddNoteProperty<object>(dest, "TargetObject", () => TargetObject);
            RemotingEncoder.AddNoteProperty<string>(dest, "FullyQualifiedErrorId", () => FullyQualifiedErrorId);
            RemotingEncoder.AddNoteProperty<InvocationInfo>(dest, "InvocationInfo", () => InvocationInfo);
            RemotingEncoder.AddNoteProperty<int>(dest, "ErrorCategory_Category", () => (int)CategoryInfo.Category);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_Activity", () => CategoryInfo.Activity);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_Reason", () => CategoryInfo.Reason);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_TargetName", () => CategoryInfo.TargetName);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_TargetType", () => CategoryInfo.TargetType);
            RemotingEncoder.AddNoteProperty<string>(dest, "ErrorCategory_Message", () => CategoryInfo.GetMessage(CultureInfo.CurrentCulture));

            if (ErrorDetails != null)
            {
                RemotingEncoder.AddNoteProperty<string>(dest, "ErrorDetails_Message", () => ErrorDetails.Message);
                RemotingEncoder.AddNoteProperty<string>(dest, "ErrorDetails_RecommendedAction", () => ErrorDetails.RecommendedAction);
            }

            if (!serializeExtInfo || this.InvocationInfo == null)
            {
                RemotingEncoder.AddNoteProperty(dest, "SerializeExtendedInfo", () => false);
            }
            else
            {
                RemotingEncoder.AddNoteProperty(dest, "SerializeExtendedInfo", () => true);
                this.InvocationInfo.ToPSObjectForRemoting(dest);
                RemotingEncoder.AddNoteProperty<object>(dest, "PipelineIterationInfo", () => PipelineIterationInfo);
            }

            if (!string.IsNullOrEmpty(this.ScriptStackTrace))
            {
                RemotingEncoder.AddNoteProperty(dest, "ErrorDetails_ScriptStackTrace", () => this.ScriptStackTrace);
            }
        }

        
        private static object GetNoteValue(PSObject mshObject, string note)
        {
            if (mshObject.Properties[note] is PSNoteProperty p)
            {
                return p.Value;
            }
            else
            {
                return null;
            }
        }

        
        internal static ErrorRecord FromPSObjectForRemoting(PSObject serializedErrorRecord)
        {
            ErrorRecord er = new ErrorRecord();
            er.ConstructFromPSObjectForRemoting(serializedErrorRecord);
            return er;
        }

        private void ConstructFromPSObjectForRemoting(PSObject serializedErrorRecord)
        {
            if (serializedErrorRecord == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(serializedErrorRecord));
            }

            // Get Exception
            PSObject serializedException = RemotingDecoder.GetPropertyValue<PSObject>(serializedErrorRecord, "Exception");

            // Get Target object
            object targetObject = RemotingDecoder.GetPropertyValue<object>(serializedErrorRecord, "TargetObject");

            string exceptionMessage = null;
            if (serializedException != null)
            {
                PSPropertyInfo messageProperty = serializedException.Properties["Message"] as PSPropertyInfo;
                if (messageProperty != null)
                {
                    exceptionMessage = messageProperty.Value as string;
                }
            }

            // Get FullyQualifiedErrorId
            string fullyQualifiedErrorId = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "FullyQualifiedErrorId") ??
                                           "fullyQualifiedErrorId";

            // Get ErrorCategory...
            ErrorCategory errorCategory = RemotingDecoder.GetPropertyValue<ErrorCategory>(serializedErrorRecord, "errorCategory_Category");

            // Get Various ErrorCategory fileds
            string errorCategory_Activity = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_Activity");
            string errorCategory_Reason = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_Reason");
            string errorCategory_TargetName = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_TargetName");
            string errorCategory_TargetType = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_TargetType");
            string errorCategory_Message = RemotingDecoder.GetPropertyValue<string>(serializedErrorRecord, "ErrorCategory_Message");

            // Get InvocationInfo (optional property)
            PSObject invocationInfo = Microsoft.PowerShell.DeserializingTypeConverter.GetPropertyValue<PSObject>(
                serializedErrorRecord,
                "InvocationInfo",
                Microsoft.PowerShell.DeserializingTypeConverter.RehydrationFlags.MissingPropertyOk);

            // Get Error Detail (these note properties are optional, so can't right now use RemotingDecoder...)
            string errorDetails_Message =
                GetNoteValue(serializedErrorRecord, "ErrorDetails_Message") as string;

            string errorDetails_RecommendedAction =
                GetNoteValue(serializedErrorRecord, "ErrorDetails_RecommendedAction") as string;

            string errorDetails_ScriptStackTrace =
                GetNoteValue(serializedErrorRecord, "ErrorDetails_ScriptStackTrace") as string;

            RemoteException re = new RemoteException((!string.IsNullOrWhiteSpace(exceptionMessage)) ? exceptionMessage : errorCategory_Message, serializedException, invocationInfo);

            // Create ErrorRecord
            PopulateProperties(
                re,
                targetObject,
                fullyQualifiedErrorId,
                errorCategory,
                errorCategory_Activity,
                errorCategory_Reason,
                errorCategory_TargetName,
                errorCategory_TargetType,
                errorCategory_Message,
                errorDetails_Message,
                errorDetails_RecommendedAction,
                errorDetails_ScriptStackTrace
                );

            re.SetRemoteErrorRecord(this);

            //
            // Get the InvocationInfo
            //
            _serializeExtendedInfo = RemotingDecoder.GetPropertyValue<bool>(serializedErrorRecord, "SerializeExtendedInfo");

            if (_serializeExtendedInfo)
            {
                _invocationInfo = new InvocationInfo(serializedErrorRecord);

                ArrayList iterationInfo = RemotingDecoder.GetPropertyValue<ArrayList>(serializedErrorRecord, "PipelineIterationInfo");
                if (iterationInfo != null)
                {
                    _pipelineIterationInfo = new ReadOnlyCollection<int>((int[])iterationInfo.ToArray(typeof(Int32)));
                }
            }
            else
            {
                _invocationInfo = null;
            }
        }

        #endregion Remoting

        
        public ErrorRecord(ErrorRecord errorRecord,
                             Exception replaceParentContainsErrorRecordException)
        {
            if (errorRecord == null)
            {
                throw new PSArgumentNullException(nameof(errorRecord));
            }

            if (replaceParentContainsErrorRecordException != null
                && (errorRecord.Exception is ParentContainsErrorRecordException))
            {
                _error = replaceParentContainsErrorRecordException;
            }
            else
            {
                _error = errorRecord.Exception;
            }

            _target = errorRecord.TargetObject;
            _errorId = errorRecord._errorId;
            _category = errorRecord._category;
            _activityOverride = errorRecord._activityOverride;
            _reasonOverride = errorRecord._reasonOverride;
            _targetNameOverride = errorRecord._targetNameOverride;
            _targetTypeOverride = errorRecord._targetTypeOverride;
            if (errorRecord.ErrorDetails != null)
            {
                ErrorDetails = new ErrorDetails(errorRecord.ErrorDetails);
            }

            SetInvocationInfo(errorRecord._invocationInfo);
            _scriptStackTrace = errorRecord._scriptStackTrace;
            _serializedFullyQualifiedErrorId = errorRecord._serializedFullyQualifiedErrorId;
        }

        #endregion Constructor

        #region Override

        
        internal virtual ErrorRecord WrapException(Exception replaceParentContainsErrorRecordException)
        {
            return new ErrorRecord(this, replaceParentContainsErrorRecordException);
        }

        #endregion Override

        #region Public Properties

        
        public Exception Exception
        {
            get
            {
                Diagnostics.Assert(_error != null, "_error is null");
                return _error;
            }
        }

        private Exception _error ;

        
        public object TargetObject { get => _target; }

        private object _target ;

        internal void SetTargetObject(object target)
        {
            _target = target;
        }

        
        public ErrorCategoryInfo CategoryInfo { get => _categoryInfo ??= new ErrorCategoryInfo(this); }

        private ErrorCategoryInfo _categoryInfo;

        
        public string FullyQualifiedErrorId
        {
            get
            {
                if (_serializedFullyQualifiedErrorId != null)
                {
                    return _serializedFullyQualifiedErrorId;
                }

                string typeName = GetInvocationTypeName();
                string delimiter =
                    (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(_errorId))
                        ? string.Empty
                        : ",";
                return NotNull(_errorId) + delimiter + NotNull(typeName);
            }
        }

        
        public ErrorDetails ErrorDetails { get; set; }

        
        public InvocationInfo InvocationInfo { get => _invocationInfo; }

        private InvocationInfo _invocationInfo ;

        internal void SetInvocationInfo(InvocationInfo invocationInfo)
        {
            // Save the DisplayScriptPosition, if set
            IScriptExtent savedDisplayScriptPosition = null;
            if (_invocationInfo != null)
            {
                savedDisplayScriptPosition = _invocationInfo.DisplayScriptPosition;
            }

            // Assign the invocationInfo
            if (invocationInfo != null)
            {
                _invocationInfo = new InvocationInfo(invocationInfo.MyCommand, invocationInfo.ScriptPosition);
                _invocationInfo.InvocationName = invocationInfo.InvocationName;
                if (invocationInfo.MyCommand == null)
                {
                    // Pass the history id to new InvocationInfo object of command info is null since history
                    // information cannot be obtained in this case.
                    _invocationInfo.HistoryId = invocationInfo.HistoryId;
                }
            }

            // Restore the DisplayScriptPosition
            if (savedDisplayScriptPosition != null)
            {
                _invocationInfo.DisplayScriptPosition = savedDisplayScriptPosition;
            }

            LockScriptStackTrace();

            //
            // Copy a snapshot of the PipelinePositionInfo from the InvocationInfo to this ErrorRecord
            //
            if (invocationInfo != null && invocationInfo.PipelineIterationInfo != null)
            {
                int[] snapshot = (int[])invocationInfo.PipelineIterationInfo.Clone();

                _pipelineIterationInfo = new ReadOnlyCollection<int>(snapshot);
            }
        }

        // 2005/07/14-913791 "write-error output is confusing and misleading"
        internal bool PreserveInvocationInfoOnce { get; set; }

        
        public string ScriptStackTrace { get => _scriptStackTrace; }

        private string _scriptStackTrace;

        internal void LockScriptStackTrace()
        {
            if (_scriptStackTrace != null)
            {
                return;
            }

            var context = LocalPipeline.GetExecutionContextFromTLS();
            if (context != null)
            {
                StringBuilder sb = new StringBuilder();
                var callstack = context.Debugger.GetCallStack();
                bool first = true;
                foreach (var frame in callstack)
                {
                    if (!first)
                    {
                        sb.Append(Environment.NewLine);
                    }

                    first = false;
                    sb.Append(frame.ToString());
                }

                _scriptStackTrace = sb.ToString();
            }
        }

        
        public ReadOnlyCollection<int> PipelineIterationInfo { get => _pipelineIterationInfo; }

        private ReadOnlyCollection<int> _pipelineIterationInfo = Utils.EmptyReadOnlyCollection<int>();

        
        internal bool SerializeExtendedInfo
        {
            get => _serializeExtendedInfo;

            set => _serializeExtendedInfo = value;
        }

        private bool _serializeExtendedInfo = false;

        #endregion Public Properties

        #region Private
        private readonly string _errorId;

        #region Exposed by ErrorCategoryInfo
        internal ErrorCategory _category;
        internal string _activityOverride;
        internal string _reasonOverride;
        internal string _targetNameOverride;
        internal string _targetTypeOverride;
        #endregion Exposed by ErrorCategoryInfo

        internal static string NotNull(string s) => s ?? string.Empty;

        private string GetInvocationTypeName()
        {
            InvocationInfo invocationInfo = this.InvocationInfo;
            if (invocationInfo == null)
            {
                return string.Empty;
            }

            CommandInfo commandInfo = invocationInfo.MyCommand;
            if (commandInfo == null)
            {
                return string.Empty;
            }

            IScriptCommandInfo scriptInfo = commandInfo as IScriptCommandInfo;
            if (scriptInfo != null)
            {
                return commandInfo.Name;
            }

            if (!(commandInfo is CmdletInfo cmdletInfo))
            {
                return string.Empty;
            }

            return cmdletInfo.ImplementingType.FullName;
        }

        #endregion Private

        #region ToString
        
        public override string ToString()
        {
            if (ErrorDetails != null && !string.IsNullOrEmpty(ErrorDetails.Message))
            {
                return ErrorDetails.Message;
            }

            if (Exception != null)
            {
                return Exception.Message ?? Exception.ToString();
            }

            return base.ToString();
        }
        #endregion ToString

    }

    
    internal class ErrorRecord<TException> : ErrorRecord where TException : Exception
    {
        public new TException Exception { get; }

        public ErrorRecord(Exception exception, string errorId, ErrorCategory errorCategory, object targetObject) : base(exception, errorId, errorCategory, targetObject)
        {
        }
    }

    
#nullable enable
    public interface IContainsErrorRecord
    {
        
        ErrorRecord ErrorRecord { get; }
    }
#nullable restore

    
#nullable enable
    public interface IResourceSupplier
    {
        
        string GetResourceString(string baseName, string resourceId);
    }
}

#pragma warning restore 56506
