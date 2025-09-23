// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation.Tracing
{
    
    [AttributeUsage(AttributeTargets.Method)]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public sealed class EtwEvent : Attribute
    {
        
        /// <param name="eventId"></param>
        public EtwEvent(long eventId)
        {
            this.EventId = eventId;
        }

        
        public long EventId { get; }
    }

    
    public delegate void CallbackNoParameter();

    
    public delegate void CallbackWithState(object state);

    
    public delegate void CallbackWithStateAndArgs(object state, System.Timers.ElapsedEventArgs args);

    
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public class EtwEventArgs : EventArgs
    {
        
        public EventDescriptor Descriptor
        {
            get;
            private set;
        }

        
        public bool Success { get; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] Payload { get; }

        
        /// <param name="descriptor">Event descriptor.</param>
        /// <param name="success">Indicate whether the event is successfully written.</param>
        /// <param name="payload">Event payload.</param>
        public EtwEventArgs(EventDescriptor descriptor, bool success, object[] payload)
        {
            this.Descriptor = descriptor;
            this.Payload = payload;
            this.Success = success;
        }
    }

    
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public abstract class EtwActivity
    {
        
        private sealed class CorrelatedCallback
        {
            private readonly CallbackNoParameter callbackNoParam;
            private readonly CallbackWithState callbackWithState;
            private readonly AsyncCallback asyncCallback;

            
            private readonly Guid parentActivityId;
            private readonly EtwActivity tracer;

            
            /// <param name="tracer"></param>
            /// <param name="callback"></param>
            public CorrelatedCallback(EtwActivity tracer, CallbackNoParameter callback)
            {
                ArgumentNullException.ThrowIfNull(callback);

                ArgumentNullException.ThrowIfNull(tracer);

                this.tracer = tracer;
                this.parentActivityId = EtwActivity.GetActivityId();
                this.callbackNoParam = callback;
            }

            
            /// <param name="tracer"></param>
            /// <param name="callback"></param>
            public CorrelatedCallback(EtwActivity tracer, CallbackWithState callback)
            {
                ArgumentNullException.ThrowIfNull(callback);

                ArgumentNullException.ThrowIfNull(tracer);

                this.tracer = tracer;
                this.parentActivityId = EtwActivity.GetActivityId();
                this.callbackWithState = callback;
            }

            
            /// <param name="tracer"></param>
            /// <param name="callback"></param>
            public CorrelatedCallback(EtwActivity tracer, AsyncCallback callback)
            {
                ArgumentNullException.ThrowIfNull(callback);

                ArgumentNullException.ThrowIfNull(tracer);

                this.tracer = tracer;
                this.parentActivityId = EtwActivity.GetActivityId();
                this.asyncCallback = callback;
            }

            
            private readonly CallbackWithStateAndArgs callbackWithStateAndArgs;

            
            /// <param name="tracer"></param>
            /// <param name="callback"></param>
            public CorrelatedCallback(EtwActivity tracer, CallbackWithStateAndArgs callback)
            {
                ArgumentNullException.ThrowIfNull(callback);

                ArgumentNullException.ThrowIfNull(tracer);

                this.tracer = tracer;
                this.parentActivityId = EtwActivity.GetActivityId();
                this.callbackWithStateAndArgs = callback;
            }

            
            public void Callback(object state, System.Timers.ElapsedEventArgs args)
            {
                Debug.Assert(callbackWithStateAndArgs != null, "callback is NULL.  There MUST always ba a valid callback!");

                Correlate();
                this.callbackWithStateAndArgs(state, args);
            }

            
            private void Correlate()
            {
                tracer.CorrelateWithActivity(this.parentActivityId);
            }

            
            public void Callback()
            {
                Debug.Assert(callbackNoParam != null, "callback is NULL.  There MUST always ba a valid callback");

                Correlate();
                this.callbackNoParam();
            }

            
            public void Callback(object state)
            {
                Debug.Assert(callbackWithState != null, "callback is NULL.  There MUST always ba a valid callback!");

                Correlate();
                this.callbackWithState(state);
            }

            
            public void Callback(IAsyncResult asyncResult)
            {
                Debug.Assert(asyncCallback != null, "callback is NULL.  There MUST always ba a valid callback!");

                Correlate();
                this.asyncCallback(asyncResult);
            }
        }

        private static readonly Dictionary<Guid, EventProvider> providers = new Dictionary<Guid, EventProvider>();
        private static readonly object syncLock = new object();

        private static readonly EventDescriptor _WriteTransferEvent = new EventDescriptor(0x1f05, 0x1, 0x11, 0x5, 0x14, 0x0, (long)0x4000000000000000);

        private EventProvider currentProvider;

        
        public static event EventHandler<EtwEventArgs> EventWritten;

        
        /// <param name="activityId"></param>
        /// <returns>True when provided activity was set, false if current activity
        /// was found to be same and set was not needed.</returns>
        public static bool SetActivityId(Guid activityId)
        {
            if (GetActivityId() != activityId)
            {
                EventProvider.SetActivityId(ref activityId);
                return true;
            }

            return false;
        }

        
        /// <returns></returns>
        public static Guid CreateActivityId()
        {
            return EventProvider.CreateActivityId();
        }

        
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        public static Guid GetActivityId()
        {
            Guid activityId = Guid.Empty;
            Interop.Windows.GetEventActivityIdControl(ref activityId);
            return activityId;
        }

        
        protected EtwActivity()
        {
        }

        
        public void CorrelateWithActivity(Guid parentActivityId)
        {
            EventProvider provider = GetProvider();
            if (!provider.IsEnabled())
                return;

            Guid activityId = CreateActivityId();
            SetActivityId(activityId);

            if (parentActivityId != Guid.Empty)
            {
                EventDescriptor transferEvent = TransferEvent;
                provider.WriteTransferEvent(in transferEvent, parentActivityId, activityId, parentActivityId);
            }
        }

        
        public bool IsEnabled
        {
            get
            {
                return GetProvider().IsEnabled();
            }
        }

        
        /// <param name="levels">Levels to check.</param>
        /// <param name="keywords">Keywords to check.</param>
        /// <returns>True, if any ETW listener is enabled else false.</returns>
        public bool IsProviderEnabled(byte levels, long keywords)
        {
            return GetProvider().IsEnabled(levels, keywords);
        }

        
        public void Correlate()
        {
            Guid parentActivity = GetActivityId();
            CorrelateWithActivity(parentActivity);
        }

        
        /// <param name="callback"></param>
        /// <returns></returns>
        public CallbackNoParameter Correlate(CallbackNoParameter callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            return new CorrelatedCallback(this, callback).Callback;
        }

        
        /// <param name="callback"></param>
        /// <returns></returns>
        public CallbackWithState Correlate(CallbackWithState callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            return new CorrelatedCallback(this, callback).Callback;
        }

        
        /// <param name="callback"></param>
        /// <returns></returns>
        public AsyncCallback Correlate(AsyncCallback callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            return new CorrelatedCallback(this, callback).Callback;
        }

        
        /// <param name="callback"></param>
        /// <returns></returns>
        public CallbackWithStateAndArgs Correlate(CallbackWithStateAndArgs callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            return new CorrelatedCallback(this, callback).Callback;
        }

        
        protected virtual Guid ProviderId
        {
            get
            {
                return 
            }
        }

        
        protected virtual EventDescriptor TransferEvent
        {
            get
            {
                return _WriteTransferEvent;
            }
        }

        
        /// <param name="ed">EventDescriptor.</param>
        /// <param name="payload">Payload.</param>
        protected void WriteEvent(EventDescriptor ed, params object[] payload)
        {
            EventProvider provider = GetProvider();

            if (!provider.IsEnabled())
                return;

            if (payload != null)
            {
                for (int i = 0; i < payload.Length; i++)
                {
                    if (payload[i] == null)
                    {
                        payload[i] = string.Empty;
                    }
                }
            }

            bool success = provider.WriteEvent(in ed, payload);
            EventWritten?.Invoke(this, new EtwEventArgs(ed, success, payload));
        }

        private EventProvider GetProvider()
        {
            if (currentProvider != null)
                return currentProvider;

            lock (syncLock)
            {
                if (currentProvider != null)
                    return currentProvider;

                if (!providers.TryGetValue(ProviderId, out currentProvider))
                {
                    currentProvider = new EventProvider(ProviderId);
                    providers[ProviderId] = currentProvider;
                }
            }

            return currentProvider;
        }
    }
}

#endif
