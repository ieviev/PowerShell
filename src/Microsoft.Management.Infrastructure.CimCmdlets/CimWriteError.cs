// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using Microsoft.Management.Infrastructure.Options;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    #region class ErrorToErrorRecord

    
    internal sealed class ErrorToErrorRecord
    {
        
        /// <param name="inner"></param>
        /// <param name="context">The context starting the operation, which generated the error.</param>
        /// <param name="cimResultContext">The CimResultContext used to provide ErrorSource, etc. info.</param>
        /// <returns></returns>
        internal static ErrorRecord ErrorRecordFromAnyException(
            InvocationContext context,
            Exception inner,
            CimResultContext cimResultContext)
        {
            Debug.Assert(inner != null, "Caller should verify inner != null");

            if (inner is CimException cimException)
            {
                return CreateFromCimException(context, cimException, cimResultContext);
            }

            if (inner is IContainsErrorRecord containsErrorRecord)
            {
                return InitializeErrorRecord(context,
                    exception: inner,
                    errorId: "CimCmdlet_" + containsErrorRecord.ErrorRecord.FullyQualifiedErrorId,
                    errorCategory: containsErrorRecord.ErrorRecord.CategoryInfo.Category,
                    cimResultContext: cimResultContext);
            }
            else
            {
                return InitializeErrorRecord(context,
                    exception: inner,
                    errorId: "CimCmdlet_" + inner.GetType().Name,
                    errorCategory: ErrorCategory.NotSpecified,
                    cimResultContext: cimResultContext);
            }
        }

        #region Helper functions
        
        /// <param name="context"></param>
        /// <param name="cimException"></param>
        /// <param name="cimResultContext">The CimResultContext used to provide ErrorSource, etc. info.</param>
        /// <returns></returns>
        internal static ErrorRecord CreateFromCimException(
            InvocationContext context,
            CimException cimException,
            CimResultContext cimResultContext)
        {
            Debug.Assert(cimException != null, "Caller should verify cimException != null");

            return InitializeErrorRecord(context, cimException, cimResultContext);
        }

        
        /// <param name="context"></param>
        /// <param name="exception"></param>
        /// <param name="errorId"></param>
        /// <param name="errorCategory"></param>
        /// <param name="cimResultContext">The CimResultContext used to provide ErrorSource, etc. info.</param>
        /// <returns></returns>
        internal static ErrorRecord InitializeErrorRecord(
            InvocationContext context,
            Exception exception,
            string errorId,
            ErrorCategory errorCategory,
            CimResultContext cimResultContext)
        {
            return InitializeErrorRecordCore(
                context,
                exception: exception,
                errorId: errorId,
                errorCategory: errorCategory,
                cimResultContext: cimResultContext);
        }

        
        /// <param name="context"></param>
        /// <param name="cimException"></param>
        /// <param name="cimResultContext">The CimResultContext used to provide ErrorSource, etc. info.</param>
        /// <returns></returns>
        internal static ErrorRecord InitializeErrorRecord(
            InvocationContext context,
            CimException cimException,
            CimResultContext cimResultContext)
        {
            ErrorRecord errorRecord = InitializeErrorRecordCore(
                context,
                exception: cimException,
                errorId: cimException.MessageId ?? "MiClientApiError_" + cimException.NativeErrorCode,
                errorCategory: ConvertCimExceptionToErrorCategory(cimException),
                cimResultContext: cimResultContext);

            if (cimException.ErrorData != null)
            {
                errorRecord.CategoryInfo.TargetName = cimException.ErrorSource;
            }

            return errorRecord;
        }

        
        /// <param name="context"></param>
        /// <param name="exception"></param>
        /// <param name="errorId"></param>
        /// <param name="errorCategory"></param>
        /// <param name="cimResultContext">The CimResultContext used to provide ErrorSource, etc. info.</param>
        /// <returns></returns>
        internal static ErrorRecord InitializeErrorRecordCore(
            InvocationContext context,
            Exception exception,
            string errorId,
            ErrorCategory errorCategory,
            CimResultContext cimResultContext)
        {
            object theTargetObject = null;
            if (cimResultContext != null)
            {
                theTargetObject = cimResultContext.ErrorSource;
            }

            if (theTargetObject == null)
            {
                if (context != null)
                {
                    if (context.TargetCimInstance != null)
                    {
                        theTargetObject = context.TargetCimInstance;
                    }
                }
            }

            ErrorRecord coreErrorRecord = new(
                exception: exception,
                errorId: errorId,
                errorCategory: errorCategory,
                targetObject: theTargetObject);

            if (context == null)
            {
                return coreErrorRecord;
            }

            System.Management.Automation.Remoting.OriginInfo originInfo = new(
                context.ComputerName,
                Guid.Empty);

            ErrorRecord errorRecord = new System.Management.Automation.Runspaces.RemotingErrorRecord(
                coreErrorRecord,
                originInfo);

            DebugHelper.WriteLogEx("Created RemotingErrorRecord.", 0);
            return errorRecord;
        }

        
        /// <param name="cimException"></param>
        /// <returns></returns>
        internal static ErrorCategory ConvertCimExceptionToErrorCategory(CimException cimException)
        {
            ErrorCategory result = ErrorCategory.NotSpecified;

            if (cimException.ErrorData != null)
            {
                result = ConvertCimErrorToErrorCategory(cimException.ErrorData);
            }

            if (result == ErrorCategory.NotSpecified)
            {
                result = ConvertCimNativeErrorCodeToErrorCategory(cimException.NativeErrorCode);
            }

            return result;
        }

        
        /// <param name="nativeErrorCode"></param>
        /// <returns></returns>
        internal static ErrorCategory ConvertCimNativeErrorCodeToErrorCategory(NativeErrorCode nativeErrorCode)
        {
            switch (nativeErrorCode)
            {
                case NativeErrorCode.Failed:
                    return ErrorCategory.NotSpecified;
                case NativeErrorCode.AccessDenied:
                    return ErrorCategory.PermissionDenied;
                case NativeErrorCode.InvalidNamespace:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.InvalidParameter:
                    return ErrorCategory.InvalidArgument;
                case NativeErrorCode.InvalidClass:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.NotFound:
                    return ErrorCategory.ObjectNotFound;
                case NativeErrorCode.NotSupported:
                    return ErrorCategory.NotImplemented;
                case NativeErrorCode.ClassHasChildren:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.ClassHasInstances:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.InvalidSuperClass:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.AlreadyExists:
                    return ErrorCategory.ResourceExists;
                case NativeErrorCode.NoSuchProperty:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.TypeMismatch:
                    return ErrorCategory.InvalidType;
                case NativeErrorCode.QueryLanguageNotSupported:
                    return ErrorCategory.NotImplemented;
                case NativeErrorCode.InvalidQuery:
                    return ErrorCategory.InvalidArgument;
                case NativeErrorCode.MethodNotAvailable:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.MethodNotFound:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.NamespaceNotEmpty:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.InvalidEnumerationContext:
                    return ErrorCategory.MetadataError;
                case NativeErrorCode.InvalidOperationTimeout:
                    return ErrorCategory.InvalidArgument;
                case NativeErrorCode.PullHasBeenAbandoned:
                    return ErrorCategory.OperationStopped;
                case NativeErrorCode.PullCannotBeAbandoned:
                    return ErrorCategory.CloseError;
                case NativeErrorCode.FilteredEnumerationNotSupported:
                    return ErrorCategory.NotImplemented;
                case NativeErrorCode.ContinuationOnErrorNotSupported:
                    return ErrorCategory.NotImplemented;
                case NativeErrorCode.ServerLimitsExceeded:
                    return ErrorCategory.ResourceBusy;
                case NativeErrorCode.ServerIsShuttingDown:
                    return ErrorCategory.ResourceUnavailable;
                default:
                    return ErrorCategory.NotSpecified;
            }
        }

        
        /// <param name="cimError"></param>
        /// <returns></returns>
        internal static ErrorCategory ConvertCimErrorToErrorCategory(CimInstance cimError)
        {
            if (cimError == null)
            {
                return ErrorCategory.NotSpecified;
            }

            CimProperty errorCategoryProperty = cimError.CimInstanceProperties[@"Error_Category"];
            if (errorCategoryProperty == null)
            {
                return ErrorCategory.NotSpecified;
            }

            ErrorCategory errorCategoryValue;
            if (!LanguagePrimitives.TryConvertTo<ErrorCategory>(errorCategoryProperty.Value, CultureInfo.InvariantCulture, out errorCategoryValue))
            {
                return ErrorCategory.NotSpecified;
            }

            return errorCategoryValue;
        }

        #endregion
    }

    #endregion

    
    internal sealed class CimWriteError : CimSyncAction
    {
        
        /// <param name="error"></param>
        public CimWriteError(CimInstance error, InvocationContext context)
        {
            this.Error = error;
            this.CimInvocationContext = context;
        }

        
        /// <param name="exception"></param>
        public CimWriteError(Exception exception, InvocationContext context, CimResultContext cimResultContext)
        {
            this.Exception = exception;
            this.CimInvocationContext = context;
            this.ResultContext = cimResultContext;
        }

        
        /// <param name="cmdlet"></param>
        public override void Execute(CmdletOperationBase cmdlet)
        {
            Debug.Assert(cmdlet != null, "Caller should verify that cmdlet != null");
            try
            {
                Exception errorException = (Error != null) ? new CimException(Error) : this.Exception;

                // PS engine takes care of handling error action
                cmdlet.WriteError(ErrorToErrorRecord.ErrorRecordFromAnyException(this.CimInvocationContext, errorException, this.ResultContext));

                // if user wants to continue, we will get here
                this.responseType = CimResponseType.Yes;
            }
            catch
            {
                this.responseType = CimResponseType.NoToAll;
                throw;
            }
            finally
            {
                // unblocking the waiting thread
                this.OnComplete();
            }
        }

        #region members

        

        internal CimInstance Error { get; }

        
        internal Exception Exception { get; }

        internal InvocationContext CimInvocationContext { get; }

        internal CimResultContext ResultContext { get; }

        #endregion
    }
}
