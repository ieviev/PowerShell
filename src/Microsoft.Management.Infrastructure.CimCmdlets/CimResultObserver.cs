// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Globalization;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    #region AsyncResultType
    
    public enum AsyncResultType
    {
        Result,
        Exception,
        Completion
    }
    #endregion

    #region CimResultContext
    
    internal class CimResultContext
    {
        
        /// <param name="ErrorSource"></param>
        internal CimResultContext(object ErrorSource)
        {
            this.ErrorSource = ErrorSource;
        }

        
        internal object ErrorSource { get; }
    }
    #endregion

    #region AsyncResultEventArgsBase
    
    internal abstract class AsyncResultEventArgsBase : EventArgs
    {
        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="resultType"></param>
        protected AsyncResultEventArgsBase(
            CimSession session,
            IObservable<object> observable,
            AsyncResultType resultType)
        {
            this.session = session;
            this.observable = observable;
            this.resultType = resultType;
        }

        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="resultType"></param>
        /// <param name="context"></param>
        protected AsyncResultEventArgsBase(
            CimSession session,
            IObservable<object> observable,
            AsyncResultType resultType,
            CimResultContext cimResultContext)
        {
            this.session = session;
            this.observable = observable;
            this.resultType = resultType;
            this.context = cimResultContext;
        }

        public readonly CimSession session;
        public readonly IObservable<object> observable;
        public readonly AsyncResultType resultType;

        // property ErrorSource
        public readonly CimResultContext context;
    }

    #endregion

    #region AsyncResult*Args
    
    internal class AsyncResultCompleteEventArgs : AsyncResultEventArgsBase
    {
        
        /// <param name="session"><see cref="CimSession"/> object.</param>
        /// <param name="cancellationDisposable"></param>
        public AsyncResultCompleteEventArgs(
            CimSession session,
            IObservable<object> observable)
            : base(session, observable, AsyncResultType.Completion)
        {
        }
    }

    
    internal class AsyncResultObjectEventArgs : AsyncResultEventArgsBase
    {
        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="resultObject"></param>
        public AsyncResultObjectEventArgs(
            CimSession session,
            IObservable<object> observable,
            object resultObject)
            : base(session, observable, AsyncResultType.Result)
        {
            this.resultObject = resultObject;
        }

        public readonly object resultObject;
    }

    
    internal class AsyncResultErrorEventArgs : AsyncResultEventArgsBase
    {
        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="error"></param>
        public AsyncResultErrorEventArgs(
            CimSession session,
            IObservable<object> observable,
            Exception error)
            : base(session, observable, AsyncResultType.Exception)
        {
            this.error = error;
        }

        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="error"></param>
        /// <param name="context"></param>
        public AsyncResultErrorEventArgs(
            CimSession session,
            IObservable<object> observable,
            Exception error,
            CimResultContext cimResultContext)
            : base(session, observable, AsyncResultType.Exception, cimResultContext)
        {
            this.error = error;
        }

        public readonly Exception error;
    }
    #endregion

    #region CimResultObserver
    
    /// <typeparam name="T">object type</typeparam>
    internal class CimResultObserver<T> : IObserver<T>
    {
        
        public event EventHandler<AsyncResultEventArgsBase> OnNewResult;

        
        /// <param name="session"><see cref="CimSession"/> object that issued the operation.</param>
        /// <param name="observable">Operation that can be observed.</param>
        public CimResultObserver(CimSession session, IObservable<object> observable)
        {
            this.CurrentSession = session;
            this.observable = observable;
        }

        
        /// <param name="session"><see cref="CimSession"/> object that issued the operation.</param>
        /// <param name="observable">Operation that can be observed.</param>
        public CimResultObserver(CimSession session,
            IObservable<object> observable,
            CimResultContext cimResultContext)
        {
            this.CurrentSession = session;
            this.observable = observable;
            this.context = cimResultContext;
        }

        
        public virtual void OnCompleted()
        {
            // callbacks should never throw any exception to
            // protocol layer, otherwise the client process will be
            // terminated because of unhandled exception, same with
            // OnNext, OnError
            try
            {
                AsyncResultCompleteEventArgs completeArgs = new(
                    this.CurrentSession, this.observable);
                this.OnNewResult(this, completeArgs);
            }
            catch (Exception ex)
            {
                this.OnError(ex);
                DebugHelper.WriteLogEx("{0}", 0, ex);
            }
        }

        
        /// <param name="error">Error object.</param>
        public virtual void OnError(Exception error)
        {
            try
            {
                AsyncResultErrorEventArgs errorArgs = new(
                    this.CurrentSession, this.observable, error, this.context);
                this.OnNewResult(this, errorArgs);
            }
            catch (Exception ex)
            {
                // !!ignore the exception
                DebugHelper.WriteLogEx("{0}", 0, ex);
            }
        }

        
        /// <param name="value"></param>
        protected void OnNextCore(object value)
        {
            DebugHelper.WriteLogEx("value = {0}.", 1, value);
            try
            {
                AsyncResultObjectEventArgs resultArgs = new(
                    this.CurrentSession, this.observable, value);
                this.OnNewResult(this, resultArgs);
            }
            catch (Exception ex)
            {
                this.OnError(ex);
                DebugHelper.WriteLogEx("{0}", 0, ex);
            }
        }

        
        /// <param name="value">Result object.</param>
        public virtual void OnNext(T value)
        {
            DebugHelper.WriteLogEx("value = {0}.", 1, value);
            // do not allow null value
            if (value == null)
            {
                return;
            }

            this.OnNextCore(value);
        }

        #region members

        
        protected CimSession CurrentSession { get; }

        
        private readonly IObservable<object> observable;

        
        private readonly CimResultContext context;
        #endregion
    }

    
    internal class CimSubscriptionResultObserver : CimResultObserver<CimSubscriptionResult>
    {
        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public CimSubscriptionResultObserver(CimSession session, IObservable<object> observable)
            : base(session, observable)
        {
        }

        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public CimSubscriptionResultObserver(
            CimSession session,
            IObservable<object> observable,
            CimResultContext context)
            : base(session, observable, context)
        {
        }

        
        /// <param name="value"></param>
        public override void OnNext(CimSubscriptionResult value)
        {
            DebugHelper.WriteLogEx();
            base.OnNextCore(value);
        }
    }

    
    internal class CimMethodResultObserver : CimResultObserver<CimMethodResultBase>
    {
        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public CimMethodResultObserver(CimSession session, IObservable<object> observable)
            : base(session, observable)
        {
        }

        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        /// <param name="context"></param>
        public CimMethodResultObserver(
            CimSession session,
            IObservable<object> observable,
            CimResultContext context)
            : base(session, observable, context)
        {
        }

        
        /// <param name="value"></param>
        public override void OnNext(CimMethodResultBase value)
        {
            DebugHelper.WriteLogEx();
            const string PSTypeCimMethodResult = @"Microsoft.Management.Infrastructure.CimMethodResult";
            const string PSTypeCimMethodStreamedResult = @"Microsoft.Management.Infrastructure.CimMethodStreamedResult";
            const string PSTypeCimMethodResultTemplate = @"{0}#{1}#{2}";

            string resultObjectPSType = null;
            PSObject resultObject = null;
            if (value is CimMethodResult methodResult)
            {
                resultObjectPSType = PSTypeCimMethodResult;
                resultObject = new PSObject();
                foreach (CimMethodParameter param in methodResult.OutParameters)
                {
                    resultObject.Properties.Add(new PSNoteProperty(param.Name, param.Value));
                }
            }
            else
            {
                if (value is CimMethodStreamedResult methodStreamedResult)
                {
                    resultObjectPSType = PSTypeCimMethodStreamedResult;
                    resultObject = new PSObject();
                    resultObject.Properties.Add(new PSNoteProperty(@"ParameterName", methodStreamedResult.ParameterName));
                    resultObject.Properties.Add(new PSNoteProperty(@"ItemType", methodStreamedResult.ItemType));
                    resultObject.Properties.Add(new PSNoteProperty(@"ItemValue", methodStreamedResult.ItemValue));
                }
            }

            if (resultObject != null)
            {
                resultObject.Properties.Add(new PSNoteProperty(@"PSComputerName", this.CurrentSession.ComputerName));
                resultObject.TypeNames.Insert(0, resultObjectPSType);
                resultObject.TypeNames.Insert(0, string.Format(CultureInfo.InvariantCulture, PSTypeCimMethodResultTemplate, resultObjectPSType, ClassName, MethodName));
                base.OnNextCore(resultObject);
            }
        }

        
        internal string MethodName
        {
            get;
            set;
        }

        
        internal string ClassName
        {
            get;
            set;
        }
    }

    
    internal class IgnoreResultObserver : CimResultObserver<CimInstance>
    {
        
        /// <param name="session"></param>
        /// <param name="observable"></param>
        public IgnoreResultObserver(CimSession session, IObservable<object> observable)
            : base(session, observable)
        {
        }

        
        /// <param name="value"></param>
        public override void OnNext(CimInstance value)
        {
            DebugHelper.WriteLogEx();
        }
    }
    #endregion
}
