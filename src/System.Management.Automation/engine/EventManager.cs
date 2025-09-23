// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace System.Management.Automation
{
    
    public abstract class PSEventManager
    {
        
        private int _nextEventId = 1;

        
        protected int GetNextEventId()
        {
            return _nextEventId++;
        }

        
        public PSEventArgsCollection ReceivedEvents { get; } = new PSEventArgsCollection();

        
        public abstract List<PSEventSubscriber> Subscribers { get; }

        
        protected abstract PSEventArgs CreateEvent(string sourceIdentifier, object sender, object[] args, PSObject extraData);

        
        public PSEventArgs GenerateEvent(string sourceIdentifier, object sender, object[] args, PSObject extraData)
        {
            return this.GenerateEvent(sourceIdentifier, sender, args, extraData, false, false);
        }

        
        public PSEventArgs GenerateEvent(string sourceIdentifier, object sender, object[] args, PSObject extraData,
            bool processInCurrentThread, bool waitForCompletionInCurrentThread)
        {
            PSEventArgs newEvent = CreateEvent(sourceIdentifier, sender, args, extraData);
            ProcessNewEvent(newEvent, processInCurrentThread, waitForCompletionInCurrentThread);

            return newEvent;
        }

        
        internal abstract void AddForwardedEvent(PSEventArgs forwardedEvent);

        
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "InCurrent")]
        protected abstract void ProcessNewEvent(PSEventArgs newEvent, bool processInCurrentThread);

        
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "InCurrent")]
        protected internal virtual void ProcessNewEvent(PSEventArgs newEvent, bool processInCurrentThread,
                                                         bool waitForCompletionWhenInCurrentThread)
        {
            throw new NotImplementedException();
        }

        
        public abstract IEnumerable<PSEventSubscriber> GetEventSubscribers(string sourceIdentifier);

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public abstract PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, ScriptBlock action, bool supportEvent, bool forwardEvent);

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public abstract PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, ScriptBlock action, bool supportEvent, bool forwardEvent, int maxTriggerCount);

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public abstract PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent);

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public abstract PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent, int maxTriggerCount);

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        internal virtual PSEventSubscriber SubscribeEvent(object source,
            string eventName,
            string sourceIdentifier,
            PSObject data,
            PSEventReceivedEventHandler handlerDelegate,
            bool supportEvent,
            bool forwardEvent,
            bool shouldQueueAndProcessInExecutionThread,
            int maxTriggerCount = 0)
        {
            return SubscribeEvent(source, eventName, sourceIdentifier, data, handlerDelegate, supportEvent, forwardEvent, maxTriggerCount);
        }

        
        public abstract void UnsubscribeEvent(PSEventSubscriber subscriber);

        
        internal abstract event EventHandler<PSEventArgs> ForwardEvent;
    }

    
    internal class PSLocalEventManager : PSEventManager, IDisposable
    {
        
        internal PSLocalEventManager(ExecutionContext context)
        {
            _eventSubscribers = new Dictionary<PSEventSubscriber, Delegate>();
            _engineEventSubscribers = new Dictionary<string, List<PSEventSubscriber>>(StringComparer.OrdinalIgnoreCase);
            _actionQueue = new Queue<EventAction>();
            _context = context;
        }

        private readonly Dictionary<PSEventSubscriber, Delegate> _eventSubscribers;
        private readonly Dictionary<string, List<PSEventSubscriber>> _engineEventSubscribers;
        private readonly Queue<EventAction> _actionQueue;
        private readonly ExecutionContext _context;
        private int _nextSubscriptionId = 1;
        private readonly double _throttleLimit = 1;
        private int _throttleChecks = 0;

        // The assembly and module to hold our event registrations
        private AssemblyBuilder _eventAssembly = null;
        private ModuleBuilder _eventModule = null;
        private int _typeId = 0;

        
        public override List<PSEventSubscriber> Subscribers
        {
            get
            {
                List<PSEventSubscriber> subscribers = new List<PSEventSubscriber>();

                lock (_eventSubscribers)
                {
                    foreach (PSEventSubscriber currentSubscriber in _eventSubscribers.Keys)
                    {
                        subscribers.Add(currentSubscriber);
                    }
                }

                return subscribers;
            }
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public override PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, ScriptBlock action, bool supportEvent, bool forwardEvent)
        {
            return SubscribeEvent(source, eventName, sourceIdentifier, data, action, supportEvent, forwardEvent, 0);
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public override PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, ScriptBlock action, bool supportEvent, bool forwardEvent, int maxTriggerCount)
        {
            // Record this subscriber. This may just be a registration for engine events.
            PSEventSubscriber subscriber = new PSEventSubscriber(_context, _nextSubscriptionId++, source, eventName, sourceIdentifier, action, supportEvent, forwardEvent, maxTriggerCount);
            ProcessNewSubscriber(subscriber, source, eventName, sourceIdentifier, data, supportEvent, forwardEvent);
            subscriber.RegisterJob();

            return subscriber;
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        internal override PSEventSubscriber SubscribeEvent(object source,
            string eventName,
            string sourceIdentifier,
            PSObject data,
            PSEventReceivedEventHandler handlerDelegate,
            bool supportEvent,
            bool forwardEvent,
            bool shouldQueueAndProcessInExecutionThread,
            int maxTriggerCount = 0)
        {
            PSEventSubscriber newSubscriber = SubscribeEvent(source, eventName, sourceIdentifier, data, handlerDelegate, supportEvent, forwardEvent, maxTriggerCount);
            newSubscriber.ShouldProcessInExecutionThread = shouldQueueAndProcessInExecutionThread;
            return newSubscriber;
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public override PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent)
        {
            return SubscribeEvent(source, eventName, sourceIdentifier, data, handlerDelegate, supportEvent, forwardEvent, 0);
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public override PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent, int maxTriggerCount)
        {
            // Record this subscriber. This may just be a registration for engine events.
            PSEventSubscriber subscriber = new PSEventSubscriber(_context, _nextSubscriptionId++, source, eventName, sourceIdentifier, handlerDelegate, supportEvent, forwardEvent, maxTriggerCount);
            ProcessNewSubscriber(subscriber, source, eventName, sourceIdentifier, data, supportEvent, forwardEvent);
            subscriber.RegisterJob();

            return subscriber;
        }

        #region OnIdleProcessing

        private Timer _timer = null;
        private bool _timerInitialized = false;
        private bool _isTimerActive = false;
        
        private int _consecutiveIdleSamples = 0;

        
        private void OnElapsedEvent(object source)
        {
            var localRunspace = _context.CurrentRunspace as LocalRunspace;

            if (localRunspace == null)
            {
                // This should never happen, the context should always reference to the local runspace
                _consecutiveIdleSamples = 0;
                return;
            }

            if (localRunspace.GetCurrentlyRunningPipeline() == null)
            {
                _consecutiveIdleSamples++;
            }
            else
            {
                _consecutiveIdleSamples = 0;
            }

            if (_consecutiveIdleSamples == 4)
            {
                _consecutiveIdleSamples = 0;
                lock (_engineEventSubscribers)
                {
                    List<PSEventSubscriber> subscribers = null;
                    if (_engineEventSubscribers.TryGetValue(PSEngineEvent.OnIdle, out subscribers) && subscribers.Count > 0)
                    {
                        // We send out on-idle event and keep enabling the timer only if there still are subscribers to the on-idle event
                        GenerateEvent(PSEngineEvent.OnIdle, null, Array.Empty<object>(), null, false, false);
                        EnableTimer();
                    }
                    else
                    {
                        _isTimerActive = false;
                    }
                }
            }
            else
            {
                EnableTimer();
            }
        }

        private void InitializeTimer()
        {
            try
            {
                _timer = new Timer(OnElapsedEvent, null, Timeout.Infinite, Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                // The PSLocalEventManager is disposed, do nothing
            }
        }

        private void EnableTimer()
        {
            try
            {
                _timer.Change(100, Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                // The PSLocalEventManager is disposed, do nothing
            }
        }

        #endregion OnIdleProcessing

        private static readonly Dictionary<string, Type> s_generatedEventHandlers = new Dictionary<string, Type>();

        private void ProcessNewSubscriber(PSEventSubscriber subscriber, object source, string eventName, string sourceIdentifier, PSObject data, bool supportEvent, bool forwardEvent)
        {
            Delegate handlerDelegate = null;

            if (_eventAssembly == null)
            {
                _eventAssembly = AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName("PSEventHandler"),
                    AssemblyBuilderAccess.Run);
                _eventModule = _eventAssembly.DefineDynamicModule("PSGenericEventModule");
            }

            string engineEventSourceIdentifier = null;
            bool isOnIdleEvent = false;
            // If we are subscribing to an event on an object, generate the supporting delegate
            // for that object.
            if (source != null)
            {
                // If the identifier starts with "PowerShell.", then it will collide with engine
                // events
                if ((sourceIdentifier != null) &&
                    (sourceIdentifier.StartsWith("PowerShell.", StringComparison.OrdinalIgnoreCase)))
                {
                    string errorMessage = StringUtil.Format(EventingResources.ReservedIdentifier, sourceIdentifier);

                    throw new ArgumentException(errorMessage, nameof(sourceIdentifier));
                }

                EventInfo eventInfo = null;
                Type sourceType = source as Type ?? source.GetType();

                // Retrieve the event from the object
                const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
                eventInfo = sourceType.GetEvent(eventName, bindingFlags);

                // If we can't find the event, throw an exception
                if (eventInfo == null)
                {
                    string errorMessage = StringUtil.Format(EventingResources.CouldNotFindEvent, eventName);
                    throw new ArgumentException(errorMessage, nameof(eventName));
                }

                // Try to set the EnableRaisingEvents property if it defines one
                PropertyInfo eventProperty = sourceType.GetProperty("EnableRaisingEvents");
                if (eventProperty != null && eventProperty.CanWrite)
                {
                    try
                    {
                        object targetObject = eventProperty.SetMethod.IsStatic ? null : source;
                        eventProperty.SetValue(targetObject, true);
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException != null)
                        {
                            throw e.InnerException;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                // Get its invoke method, and register ourselves as a handler
                MethodInfo invokeMethod = eventInfo.EventHandlerType.GetMethod("Invoke");

                // We don't support non-void delegates, as the user has no way
                // to influence the result. In the .NET Framework, the only
                // events that return values are for extremely advanced scenarios:
                // System.ResolveEventHandler, and System.Reflection.ModuleResolveEventHandler.
                // For these, the Add-Type cmdlet will suffice.
                if (invokeMethod.ReturnType != typeof(void))
                {
                    string errorMessage = EventingResources.NonVoidDelegateNotSupported;
                    throw new ArgumentException(errorMessage, nameof(eventName));
                }

                // Cache generated event handlers (by type and event name) so that they don't bloat our
                // working set by recompiling event handlers for the same event.

                string eventHandlerKey = source.GetType().FullName + "|" + eventName;
                Type handlerType;

                lock (s_generatedEventHandlers)
                {
                    if (!s_generatedEventHandlers.TryGetValue(eventHandlerKey, out handlerType))
                    {
                        handlerType = GenerateEventHandler(invokeMethod);
                        s_generatedEventHandlers[eventHandlerKey] = handlerType;
                    }
                }

                // And create an instance of the type
                ConstructorInfo constructor =
                    handlerType.GetConstructor(new Type[] { typeof(PSEventManager), typeof(object), typeof(string), typeof(PSObject) });
                object handler = constructor.Invoke(new object[] { this, source, sourceIdentifier, data });
                MethodInfo eventDelegate = handlerType.GetMethod("EventDelegate", BindingFlags.Public | BindingFlags.Instance);
                handlerDelegate = eventDelegate.CreateDelegate(eventInfo.EventHandlerType, handler);

                eventInfo.AddEventHandler(source, handlerDelegate);
            }
            else
            {
                if (PSEngineEvent.EngineEvents.Contains(sourceIdentifier))
                {
                    engineEventSourceIdentifier = sourceIdentifier;
                    isOnIdleEvent = string.Equals(engineEventSourceIdentifier, PSEngineEvent.OnIdle, StringComparison.OrdinalIgnoreCase);
                }
            }

            lock (_eventSubscribers)
            {
                _eventSubscribers[subscriber] = handlerDelegate;
                if (engineEventSourceIdentifier == null)
                {
                    return;
                }

                lock (_engineEventSubscribers)
                {
                    if (isOnIdleEvent && !_timerInitialized)
                    {
                        InitializeTimer();
                        _timerInitialized = true;
                    }

                    List<PSEventSubscriber> subscribers = null;
                    if (!_engineEventSubscribers.TryGetValue(engineEventSourceIdentifier, out subscribers))
                    {
                        subscribers = new List<PSEventSubscriber>();
                        _engineEventSubscribers.Add(engineEventSourceIdentifier, subscribers);
                    }

                    subscribers.Add(subscriber);

                    // This subscriber is the only one in the idle event list, we enable the timer
                    // since the subscriber could be the first one.
                    if (isOnIdleEvent && !_isTimerActive)
                    {
                        EnableTimer();
                        _isTimerActive = true;
                    }
                }
            }
        }

        
        public override void UnsubscribeEvent(PSEventSubscriber subscriber)
        {
            UnsubscribeEvent(subscriber, false);
        }

        
        private void UnsubscribeEvent(PSEventSubscriber subscriber, bool skipDraining)
        {
            ArgumentNullException.ThrowIfNull(subscriber);

            Delegate existingSubscriber = null;
            lock (_eventSubscribers)
            {
                if (subscriber.IsBeingUnsubscribed || !_eventSubscribers.TryGetValue(subscriber, out existingSubscriber))
                {
                    // Already unsubscribed by another thread or the subscriber doesn't exist
                    return;
                }

                subscriber.IsBeingUnsubscribed = true;
            }

            if ((existingSubscriber != null) && (subscriber.SourceObject != null))
            {
                // Fire the unregistration handler
                subscriber.OnPSEventUnsubscribed(subscriber.SourceObject,
                    new PSEventUnsubscribedEventArgs(subscriber));

                EventInfo eventInfo = null;

                Type sourceType = subscriber.SourceObject as Type ?? subscriber.SourceObject.GetType();

                const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
                eventInfo = sourceType.GetEvent(subscriber.EventName, bindingFlags);

                if ((eventInfo != null) && (existingSubscriber != null))
                {
                    eventInfo.RemoveEventHandler(subscriber.SourceObject, existingSubscriber);
                }
            }

            // We don't need to drain pending actions when remove an auto-unregister subscriber
            // from ProcessPendingAction method
            if (!skipDraining)
            {
                // Drain any actions pending for this subscriber
                DrainPendingActions(subscriber);
            }

            // Stop the job
            subscriber.Action?.NotifyJobStopped();

            lock (_eventSubscribers)
            {
                _eventSubscribers.Remove(subscriber);
                if (PSEngineEvent.EngineEvents.Contains(subscriber.SourceIdentifier))
                {
                    lock (_engineEventSubscribers)
                    {
                        _engineEventSubscribers[subscriber.SourceIdentifier].Remove(subscriber);
                    }
                }
            }
        }

        
        protected override PSEventArgs CreateEvent(string sourceIdentifier, object sender, object[] args, PSObject extraData)
        {
            return new PSEventArgs(null, _context.CurrentRunspace.InstanceId, GetNextEventId(), sourceIdentifier, sender, args, extraData);
        }

        
        internal override void AddForwardedEvent(PSEventArgs forwardedEvent)
        {
            forwardedEvent.EventIdentifier = GetNextEventId();

            ProcessNewEvent(forwardedEvent, false);
        }

        
        protected override void ProcessNewEvent(PSEventArgs newEvent, bool processInCurrentThread)
        {
            ProcessNewEvent(newEvent, processInCurrentThread, false);
        }

        
        protected internal override void ProcessNewEvent(PSEventArgs newEvent,
            bool processInCurrentThread,
            bool waitForCompletionWhenInCurrentThread)
        {
            if (processInCurrentThread)
            {
                ProcessNewEventImplementation(newEvent, true);
                ManualResetEventSlim waitHandle = newEvent.EventProcessed;
                if (waitHandle != null)
                {
                    // Win8: 738767 In Win7, the processInCurrentThread parameter was used to be
                    // called processSynchronously. Even though the parameter was called "processSynchronously",
                    // the event and associated action were not processed synchronously..the event manager
                    // just added the associated action into event queue. The action can get executed at a later
                    // time depending on the throttle checks etc. In Win8, to support routing of ScriptBlock
                    // invocation to the current runspace, we took dependency on eventing infrastructure and
                    // this required ensuring the event and associated action be processed in the current thread
                    // synchronously. The below while loop was added for that (win8: 530495). However, fix for
                    // 530495 resulted in not responding for icm | % { icm } case and dynamic event/subscriptions scenarios.
                    // To overcome that, changed "processSynchronously" parameter to "processInCurrentThread" and added
                    // a new parameter "waitForCompletionWhenInCurrentThread" to trigger blocking for ScriptBlock
                    // case.
                    while (waitForCompletionWhenInCurrentThread && !waitHandle.Wait(250))
                    {
                        this.ProcessPendingActions();
                    }

                    waitHandle.Dispose();
                }
            }
            else
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((_) => ProcessNewEventImplementation(newEvent, false)));
            }
        }

        
        private void ProcessNewEventImplementation(PSEventArgs newEvent, bool processSynchronously)
        {
            // Get the subscriber(s) for this event
            bool capturedEvent = false;
            List<PSEventSubscriber> actionsHandledInCurrentThread = new List<PSEventSubscriber>();
            List<PSEventSubscriber> subscribersWithoutActionOrHandler = new List<PSEventSubscriber>();
            foreach (PSEventSubscriber subscriber in GetEventSubscribers(newEvent.SourceIdentifier, true))
            {
                newEvent.ForwardEvent = subscriber.ForwardEvent;

                // If we found a subscriber and it has an action, queue it up
                if (subscriber.Action != null)
                {
                    AddAction(new EventAction(subscriber, newEvent), processSynchronously);
                    capturedEvent = true;
                }
                else if (subscriber.HandlerDelegate != null)
                {
                    if (subscriber.ShouldProcessInExecutionThread)
                    {
                        AddAction(new EventAction(subscriber, newEvent), processSynchronously);
                    }
                    else
                    {
                        actionsHandledInCurrentThread.Add(subscriber);
                    }

                    capturedEvent = true;
                }
                else
                {
                    subscribersWithoutActionOrHandler.Add(subscriber);
                }
            }

            foreach (PSEventSubscriber subscriber in actionsHandledInCurrentThread)
            {
                subscriber.HandlerDelegate(newEvent.Sender, newEvent);
                AutoUnregisterEventIfNecessary(subscriber);
            }

            // Otherwise, add it to the queue of unprocessed items, unless we are forwarding the event
            if (!capturedEvent)
            {
                if (newEvent.ForwardEvent)
                {
                    OnForwardEvent(newEvent);
                }
                else
                {
                    lock (ReceivedEvents.SyncRoot)
                    {
                        ReceivedEvents.Add(newEvent);
                    }
                }

                foreach (PSEventSubscriber subscriber in subscribersWithoutActionOrHandler)
                {
                    AutoUnregisterEventIfNecessary(subscriber);
                }
            }
        }

        // Add an action to the event queue
        private void AddAction(EventAction action, bool processSynchronously)
        {
            if (processSynchronously)
            {
                // This mutex will get set after the event is processed.
                action.Args.EventProcessed = new ManualResetEventSlim();
            }

            lock (((System.Collections.ICollection)_actionQueue).SyncRoot)
            {
                // If the engine isn't active, pulse the pipeline.
                // When the engine starts up, it will pick up the pending events
                _actionQueue.Enqueue(action);
            }

            PulseEngine();
        }

        // PowerShell support for async notifications happen through the
        // CheckForInterrupts() method on ParseTreeNode. These are only called when
        // the engine is active (and processing,) so the Pulse() method
        // executes the equivalent of a NOP so that async events
        // can be processed when the engine is idle.
        private void PulseEngine()
        {
            try
            {
                ((LocalRunspace)_context.CurrentRunspace).Pulse();
            }
            catch (ObjectDisposedException)
            { }
        }

        
        internal void ProcessPendingActions()
        {
            // We do this check quickly outside of the lock so that the
            // main-line scenario is as fast as possible.  Also, do the real work
            // in a different method so that this method could be inlined.
            if (_actionQueue.Count == 0)
                return;

            ProcessPendingActionsImpl();
        }

        private void ProcessPendingActionsImpl()
        {
            // Now, process pending actions
            if (IsExecutingEventAction)
                return;

            try
            {
                lock (_actionProcessingLock)
                {
                    if (IsExecutingEventAction)
                        return;

                    int processed = 0;
                    _throttleChecks++;
                    EventAction nextAction;

                    while ((_throttleLimit * _throttleChecks) >= processed)
                    {
                        // Now, check for (and process) pending actions.
                        // Lock the collection so that it doesn't change from
                        // beneath us.
                        lock (((System.Collections.ICollection)_actionQueue).SyncRoot)
                        {
                            int queueCount = _actionQueue.Count;

                            // Exit if the actions have already been processed
                            if (queueCount == 0)
                                return;

                            nextAction = _actionQueue.Dequeue();
                        }

                        bool addActionBackToActionQueue = false;
                        InvokeAction(nextAction, out addActionBackToActionQueue);
                        processed++;

                        if (!addActionBackToActionQueue)
                        {
                            AutoUnregisterEventIfNecessary(nextAction.Sender);
                        }
                    }

                    if (processed > 0)
                        _throttleChecks = 0;
                }
            }
            finally
            {
                if (_actionQueue.Count > 0)
                {
                    // If we still have work remaining, sleep a bit (to give
                    // other pipelines a chance to interrupt,) and try again.
                    // This is done on another thread, since we own the runspace lock
                    // at this point if we're being called from the pipeline
                    // teardown event. That can result in starvation of
                    // foreground threads that also want to use the runspace.
                    ThreadPool.QueueUserWorkItem(new WaitCallback(
                        (_) =>
                        {
                            System.Threading.Thread.Sleep(100);
                            this.PulseEngine();
                        }));
                }
            }
        }

        
        private void AutoUnregisterEventIfNecessary(PSEventSubscriber subscriber)
        {
            bool removeSubscriber = false;
            if (subscriber.AutoUnregister)
            {
                lock (subscriber)
                {
                    subscriber.RemainingActionsToProcess--;
                    removeSubscriber = subscriber.RemainingTriggerCount == 0 &&
                                       subscriber.RemainingActionsToProcess == 0;
                }
            }

            if (removeSubscriber)
            {
                UnsubscribeEvent(subscriber, true);
            }
        }

        private readonly object _actionProcessingLock = new object();
        private EventAction _processingAction = null;

        
        internal void DrainPendingActions(PSEventSubscriber subscriber)
        {
            // We do this check quickly outside of the lock so that the
            // main-line scenario is as fast as possible.
            if (_actionQueue.Count == 0)
                return;

            // Now, process pending actions
            lock (_actionProcessingLock)
            {
                lock (((System.Collections.ICollection)_actionQueue).SyncRoot)
                {
                    int queueCount = _actionQueue.Count;

                    // Exit if the actions have already been processed
                    if (queueCount == 0)
                        return;

                    bool needToScanAgain = false;

                    do
                    {
                        EventAction[] pendingActions = _actionQueue.ToArray();
                        _actionQueue.Clear();

                        foreach (EventAction pendingAction in pendingActions)
                        {
                            // Make sure an event action can unregister itself.
                            if ((pendingAction.Sender == subscriber) &&
                                (pendingAction != _processingAction))
                            {
                                while (IsExecutingEventAction)
                                    System.Threading.Thread.Sleep(100);

                                bool addActionBackToActionQueue = false;
                                InvokeAction(pendingAction, out addActionBackToActionQueue);

                                if (addActionBackToActionQueue)
                                {
                                    needToScanAgain = true;
                                }
                            }
                            else
                            {
                                _actionQueue.Enqueue(pendingAction);
                            }
                        }
                    } while (needToScanAgain);
                }
            }
        }

        private void InvokeAction(EventAction nextAction, out bool addActionBack)
        {
            lock (_actionProcessingLock)
            {
                _processingAction = nextAction;
                addActionBack = false;

                // Invoke the action in its own session state
                SessionStateInternal oldSessionState = _context.EngineSessionState;
                if (nextAction.Sender.Action != null)
                {
                    _context.EngineSessionState = nextAction.Sender.Action.ScriptBlock.SessionStateInternal;
                }

                Runspace oldDefault = Runspace.DefaultRunspace;

                try
                {
                    // Set the engine's session state to be
                    // the session state of the action. Also update the default runspace that the
                    // scriptblock will execute under, so that scriptblocks will stay in the same
                    // runspace they were invoked from.
                    Runspace.DefaultRunspace = _context.CurrentRunspace;
                    if (nextAction.Sender.Action != null)
                    {
                        nextAction.Sender.Action.Invoke(nextAction.Sender, nextAction.Args);
                    }
                    else
                    {
                        nextAction.Sender.HandlerDelegate(nextAction.Sender, nextAction.Args);
                    }
                }
                catch (Exception e)
                {
                    // Catch-all OK. This is a third-party call-out.
                    if (e is PipelineStoppedException)
                    {
                        // Enqueue the action again, as we weren't able to process it.
                        // It is possible that the PipelineStoppedException gets generated
                        // to interrupt _our_ event, but is much more likely to get generated
                        // when somebody wants to interrupt the pipeline that we are interrupting
                        // (such as a long directory listing.)
                        AddAction(nextAction, processSynchronously: false);
                        addActionBack = true;
                    }
                }
                finally
                {
                    var eventProcessed = nextAction.Args.EventProcessed;
                    if (!addActionBack && eventProcessed != null)
                    {
                        eventProcessed.Set();
                    }

                    Runspace.DefaultRunspace = oldDefault;
                    _context.EngineSessionState = oldSessionState;
                    _processingAction = null;
                }
            }
        }

        internal bool IsExecutingEventAction
        {
            get { return (_processingAction != null); }
        }

        
        public override IEnumerable<PSEventSubscriber> GetEventSubscribers(string sourceIdentifier)
        {
            return GetEventSubscribers(sourceIdentifier, false);
        }

        
        private IEnumerable<PSEventSubscriber> GetEventSubscribers(string sourceIdentifier, bool forNewEventProcessing)
        {
            List<PSEventSubscriber> returnedSubscribers = new List<PSEventSubscriber>();
            List<PSEventSubscriber> subscribersToBeRemoved = new List<PSEventSubscriber>();

            lock (_eventSubscribers)
            {
                foreach (PSEventSubscriber currentSubscriber in _eventSubscribers.Keys)
                {
                    bool takeActionForEvent = false;
                    if (string.Equals(currentSubscriber.SourceIdentifier, sourceIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        if (forNewEventProcessing)
                        {
                            // The caller tries to process the event
                            if (!currentSubscriber.AutoUnregister || currentSubscriber.RemainingTriggerCount > 0)
                            {
                                // If we need the event filter feature, it should be added here.
                                takeActionForEvent = true;
                                returnedSubscribers.Add(currentSubscriber);
                            }
                        }
                        else
                        {
                            // The caller tries to get all subscribers for this event but it's NOT for the event processing purpose
                            returnedSubscribers.Add(currentSubscriber);
                        }

                        // Handle auto-unregister subscribers here
                        if (forNewEventProcessing && currentSubscriber.AutoUnregister && currentSubscriber.RemainingTriggerCount > 0)
                        {
                            lock (currentSubscriber)
                            {
                                currentSubscriber.RemainingTriggerCount--;
                                // For now, 'takeActionForEvent' is always True when we get to this point.
                                // But once the event filter feature is added, it could be False when we get to this point.
                                if (takeActionForEvent)
                                {
                                    currentSubscriber.RemainingActionsToProcess++;
                                }

                                // This condition can only happen after the event filter feature is introduced
                                if (currentSubscriber.RemainingTriggerCount == 0 && currentSubscriber.RemainingActionsToProcess == 0)
                                {
                                    subscribersToBeRemoved.Add(currentSubscriber);
                                }
                            }
                        }
                    }
                }
            }

            if (subscribersToBeRemoved.Count > 0)
            {
                foreach (PSEventSubscriber subscriber in subscribersToBeRemoved)
                {
                    UnsubscribeEvent(subscriber, true);
                }
            }

            return returnedSubscribers;
        }

        // Generates a type and method to handle a strongly-typed event from an object
        // The event handler itself does as little as possible, as dynamically emitted IL
        // is error prone and difficult to read. When possible, functionality enhancements
        // should go in the PSEventHandler class, from which every event handler type derives.
        private Type GenerateEventHandler(MethodInfo invokeSignature)
        {
            int parameterCount = invokeSignature.GetParameters().Length;

            // Define the type that will respond to the event. It
            // derives from PSEventHandler so that complex
            // functionality can go into its base class.
            TypeBuilder eventType =
                _eventModule.DefineType("PSEventHandler_" + _typeId, TypeAttributes.Public, typeof(PSEventHandler));
            _typeId++;

            // Retrieve the existing constructor
            ConstructorInfo existingConstructor =
                typeof(PSEventHandler).GetConstructor(
                    new Type[] { typeof(PSEventManager), typeof(object), typeof(string), typeof(PSObject) });

            // Define the new constructor
            // public TestEventHandler(PSEventManager eventManager, Object sender, string sourceIdentifier, PSObject extraData)
            // : base(eventManager, sender, sourceIdentifier, extraData)
            ConstructorBuilder eventConstructor =
                eventType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                    new Type[] { typeof(PSEventManager), typeof(object), typeof(string), typeof(PSObject) });
            ILGenerator extendedConstructor = eventConstructor.GetILGenerator();
            extendedConstructor.Emit(OpCodes.Ldarg_0);
            extendedConstructor.Emit(OpCodes.Ldarg_1);
            extendedConstructor.Emit(OpCodes.Ldarg_2);
            extendedConstructor.Emit(OpCodes.Ldarg_3);
            extendedConstructor.Emit(OpCodes.Ldarg, 4);
            extendedConstructor.Emit(OpCodes.Call, existingConstructor);
            extendedConstructor.Emit(OpCodes.Ret);

            // Go through each of the parameters in the event signature, and store their types
            Type[] parameterTypes = new Type[parameterCount];
            int parameterCounter = 0;
            foreach (ParameterInfo parameter in invokeSignature.GetParameters())
            {
                parameterTypes[parameterCounter] = parameter.ParameterType;
                parameterCounter++;
            }

            // public void EventDelegate(object sender, FileSystemEventArgs e)
            MethodBuilder eventMethod = eventType.DefineMethod("EventDelegate",
                MethodAttributes.Public, CallingConventions.Standard, invokeSignature.ReturnType, parameterTypes);

            // Create new parameters that mimic the parameters of the event method
            parameterCounter = 1;
            foreach (ParameterInfo parameter in invokeSignature.GetParameters())
            {
                ParameterBuilder builder = eventMethod.DefineParameter(
                    parameterCounter, parameter.Attributes, parameter.Name);
                parameterCounter++;
            }

            ILGenerator methodContents = eventMethod.GetILGenerator();

            // Declare a local variable of the type 'object[]' at index 0, say 'object[] args'
            methodContents.DeclareLocal(typeof(object[]));

            methodContents.Emit(OpCodes.Ldc_I4, parameterCount);
            methodContents.Emit(OpCodes.Newarr, typeof(object));

            // Store the new array to the local variable 'args'
            methodContents.Emit(OpCodes.Stloc_0);

            // Inline, this converts into a series of setting args[n] to
            // the argument at the same parameter index
            for (int counter = 1; counter <= parameterCount; counter++)
            {
                methodContents.Emit(OpCodes.Ldloc_0);
                methodContents.Emit(OpCodes.Ldc_I4, counter - 1);
                methodContents.Emit(OpCodes.Ldarg, counter);

                // Box the value type if necessary
                if (parameterTypes[counter - 1].IsValueType)
                {
                    methodContents.Emit(OpCodes.Box, parameterTypes[counter - 1]);
                }

                methodContents.Emit(OpCodes.Stelem_Ref);
            }

            // Gain access to "this"
            methodContents.Emit(OpCodes.Ldarg_0);

            // Load the "eventManager" private field, and push it onto the stack
            FieldInfo eventManagerField = typeof(PSEventHandler).GetField("eventManager",
                BindingFlags.NonPublic | BindingFlags.Instance);
            methodContents.Emit(OpCodes.Ldfld, eventManagerField);

            // Then the "sourceIdentifier" private field
            methodContents.Emit(OpCodes.Ldarg_0);
            FieldInfo identifierField = typeof(PSEventHandler).GetField("sourceIdentifier",
                BindingFlags.NonPublic | BindingFlags.Instance);
            methodContents.Emit(OpCodes.Ldfld, identifierField);

            // Then the "sender" private field
            methodContents.Emit(OpCodes.Ldarg_0);
            FieldInfo senderField = typeof(PSEventHandler).GetField("sender",
                BindingFlags.NonPublic | BindingFlags.Instance);
            methodContents.Emit(OpCodes.Ldfld, senderField);

            // Then push the args variable onto the stack
            methodContents.Emit(OpCodes.Ldloc_0);

            // Then push the "extraData" private field
            methodContents.Emit(OpCodes.Ldarg_0);
            FieldInfo extraDataField = typeof(PSEventHandler).GetField("extraData",
                BindingFlags.NonPublic | BindingFlags.Instance);
            methodContents.Emit(OpCodes.Ldfld, extraDataField);

            // Finally, invoke the method
            MethodInfo generateEventMethod = typeof(PSEventManager).GetMethod(
                nameof(PSEventManager.GenerateEvent),
                new Type[] { typeof(string), typeof(object), typeof(object[]), typeof(PSObject) });

            methodContents.Emit(OpCodes.Callvirt, generateEventMethod);

            // Discard the return value, and return
            methodContents.Emit(OpCodes.Pop);
            methodContents.Emit(OpCodes.Ret);

            return eventType.CreateTypeInfo().AsType();
        }

        
        internal override event EventHandler<PSEventArgs> ForwardEvent;

        
        protected virtual void OnForwardEvent(PSEventArgs e)
        {
            ForwardEvent?.Invoke(this, e);
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_eventSubscribers)
                {
                    _timer?.Dispose();

                    foreach (PSEventSubscriber currentSubscriber in _eventSubscribers.Keys.ToArray())
                    {
                        UnsubscribeEvent(currentSubscriber);
                    }
                }
            }
        }

        
        ~PSLocalEventManager()
        {
            Dispose(false);
        }
    }

    
    internal class PSRemoteEventManager : PSEventManager
    {
        
        private readonly string _computerName;

        
        private readonly Guid _runspaceId;

        
        internal PSRemoteEventManager(string computerName, Guid runspaceId)
        {
            _computerName = computerName;
            _runspaceId = runspaceId;
        }

        
        public override List<PSEventSubscriber> Subscribers
        {
            get
            {
                throw new NotSupportedException(EventingResources.RemoteOperationNotSupported);
            }
        }

        
        protected override PSEventArgs CreateEvent(string sourceIdentifier, object sender, object[] args, PSObject extraData)
        {
            // note that this is a local call, so we use null for the computer name
            return new PSEventArgs(null, _runspaceId, GetNextEventId(), sourceIdentifier, sender, args, extraData);
        }

        
        internal override void AddForwardedEvent(PSEventArgs forwardedEvent)
        {
            forwardedEvent.EventIdentifier = GetNextEventId();
            forwardedEvent.ForwardEvent = false;

            // The computer name will be null the first time the event is forwarded; in this case we need to override with the
            // remote computer this event manager is associated to. If the event has travelled multiple hops then we do not
            // want to override this value since we want to preserve the original computer.
            if (forwardedEvent.ComputerName == null || forwardedEvent.ComputerName.Length == 0)
            {
                forwardedEvent.ComputerName = _computerName;
                forwardedEvent.RunspaceId = _runspaceId;
            }

            ProcessNewEvent(forwardedEvent, false);
        }

        
        protected override void ProcessNewEvent(PSEventArgs newEvent, bool processInCurrentThread)
        {
            ProcessNewEvent(newEvent, processInCurrentThread, false);
        }

        
        protected internal override void ProcessNewEvent(PSEventArgs newEvent,
            bool processInCurrentThread, bool waitForCompletionInCurrentThread)
        {
            lock (ReceivedEvents.SyncRoot)
            {
                if (newEvent.ForwardEvent)
                {
                    OnForwardEvent(newEvent);
                }
                else
                {
                    ReceivedEvents.Add(newEvent);
                }
            }
        }

        
        public override IEnumerable<PSEventSubscriber> GetEventSubscribers(string sourceIdentifier)
        {
            throw new NotSupportedException(EventingResources.RemoteOperationNotSupported);
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public override PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, ScriptBlock action, bool supportEvent, bool forwardEvent)
        {
            throw new NotSupportedException(EventingResources.RemoteOperationNotSupported);
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public override PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, ScriptBlock action, bool supportEvent, bool forwardEvent, int maxTriggerCount)
        {
            throw new NotSupportedException(EventingResources.RemoteOperationNotSupported);
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public override PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent)
        {
            throw new NotSupportedException(EventingResources.RemoteOperationNotSupported);
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public override PSEventSubscriber SubscribeEvent(object source, string eventName, string sourceIdentifier, PSObject data, PSEventReceivedEventHandler handlerDelegate, bool supportEvent, bool forwardEvent, int maxTriggerCount)
        {
            throw new NotSupportedException(EventingResources.RemoteOperationNotSupported);
        }

        
        public override void UnsubscribeEvent(PSEventSubscriber subscriber)
        {
            throw new NotSupportedException(EventingResources.RemoteOperationNotSupported);
        }

        
        internal override event EventHandler<PSEventArgs> ForwardEvent;

        
        protected virtual void OnForwardEvent(PSEventArgs e)
        {
            ForwardEvent?.Invoke(this, e);
        }
    }

    
    // Note: If you generate a new engine event that happens frequently,
    // (i.e.: variable changes), the user should be required to enable
    // that engine event before they make it into the ReceivedEvents channel.
    public sealed class PSEngineEvent
    {
        private PSEngineEvent() { }

        
        public const string Exiting = "PowerShell.Exiting";

        
        public const string OnIdle = "PowerShell.OnIdle";

        
        internal const string OnScriptBlockInvoke = "PowerShell.OnScriptBlockInvoke";

        
        internal const string GetCommandInfoParameterMetadata = "PowerShell.GetCommandInfoParameterMetadata";

        
        internal static readonly HashSet<string> EngineEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Exiting, OnIdle, OnScriptBlockInvoke };
    }

    
    public class PSEventSubscriber : IEquatable<PSEventSubscriber>
    {
        
        internal PSEventSubscriber(ExecutionContext context, int id, object source,
            string eventName, string sourceIdentifier, bool supportEvent, bool forwardEvent, int maxTriggerCount)
        {
            _context = context;

            SubscriptionId = id;
            SourceObject = source;
            EventName = eventName;
            SourceIdentifier = sourceIdentifier;
            SupportEvent = supportEvent;
            ForwardEvent = forwardEvent;

            IsBeingUnsubscribed = false;
            RemainingActionsToProcess = 0;

            if (maxTriggerCount <= 0)
            {
                AutoUnregister = false;
                RemainingTriggerCount = -1;
            }
            else
            {
                AutoUnregister = true;
                RemainingTriggerCount = maxTriggerCount;
            }
        }

        
        internal PSEventSubscriber(
            ExecutionContext context,
            int id,
            object source,
            string eventName,
            string sourceIdentifier,
            ScriptBlock action,
            bool supportEvent,
            bool forwardEvent,
            int maxTriggerCount)
            : this(context, id, source, eventName, sourceIdentifier, supportEvent, forwardEvent, maxTriggerCount)
        {
            // Create the bound scriptblock, and job.
            if (action != null)
            {
                ScriptBlock newAction = CreateBoundScriptBlock(action);
                Action = new PSEventJob(context.Events, this, newAction, sourceIdentifier);
            }
        }

        internal void RegisterJob()
        {
            // Add this event subscriber to the job repository if it's not a support event.
            if (!SupportEvent)
            {
                if (this.Action != null)
                {
                    JobRepository jobRepository = ((LocalRunspace)_context.CurrentRunspace).JobRepository;
                    jobRepository.Add(Action);
                }
            }
        }

        
        internal PSEventSubscriber(
            ExecutionContext context,
            int id,
            object source,
            string eventName,
            string sourceIdentifier,
            PSEventReceivedEventHandler handlerDelegate,
            bool supportEvent,
            bool forwardEvent,
            int maxTriggerCount)
            : this(context, id, source, eventName, sourceIdentifier, supportEvent, forwardEvent, maxTriggerCount)
        {
            HandlerDelegate = handlerDelegate;
        }

        private readonly ExecutionContext _context;

        
        private ScriptBlock CreateBoundScriptBlock(ScriptBlock scriptAction)
        {
            ScriptBlock newAction = _context.Modules.CreateBoundScriptBlock(_context, scriptAction, true);

            // Create a new Error variable so that it doesn't pollute the global errors.
            PSVariable errorVariable = new PSVariable("script:Error", new ArrayList(), ScopedItemOptions.Constant);
            SessionStateInternal sessionState = newAction.SessionStateInternal;
            SessionStateScope scriptScope = sessionState.GetScopeByID("script");
            scriptScope.SetVariable(errorVariable.Name, errorVariable, false, true, sessionState, CommandOrigin.Internal);

            return newAction;
        }

        
        public int SubscriptionId { get; set; }

        
        public object SourceObject { get; }

        
        public string EventName { get; }

        
        public string SourceIdentifier { get; }

        
        public PSEventJob Action { get; }

        
        public PSEventReceivedEventHandler HandlerDelegate { get; } = null;

        
        public bool SupportEvent { get; }

        
        public bool ForwardEvent { get; }

        
        internal bool ShouldProcessInExecutionThread { get; set; }

        
        internal bool AutoUnregister { get; }

        
        internal int RemainingTriggerCount { get; set; }

        
        internal int RemainingActionsToProcess { get; set; }

        
        internal bool IsBeingUnsubscribed { get; set; }

        
        public event PSEventUnsubscribedEventHandler Unsubscribed;

        #region IComparable<PSEventSubscriber> Members

        
        public override bool Equals(object obj)
        {
            return obj is PSEventSubscriber es && Equals(es);
        }
        
        
        public bool Equals(PSEventSubscriber other)
        {
            if (other == null)
            {
                return false;
            }

            return (string.Equals(SubscriptionId, other.SubscriptionId));
        }

        
        public override int GetHashCode()
        {
            return SubscriptionId;
        }
        #endregion

        internal void OnPSEventUnsubscribed(object sender, PSEventUnsubscribedEventArgs e)
        {
            Unsubscribed?.Invoke(sender, e);
        }
    }

    
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class PSEventHandler
    {
        
        public PSEventHandler()
        {
        }

        
        public PSEventHandler(PSEventManager eventManager, object sender, string sourceIdentifier, PSObject extraData)
        {
            this.eventManager = eventManager;
            this.sender = sender;
            this.sourceIdentifier = sourceIdentifier;
            this.extraData = extraData;
        }

        
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected PSEventManager eventManager;

        
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected object sender;

        
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected string sourceIdentifier = null;

        
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected PSObject extraData = null;
    }

    
    public class ForwardedEventArgs : EventArgs
    {
        internal ForwardedEventArgs(PSObject serializedRemoteEventArgs)
        {
            SerializedRemoteEventArgs = serializedRemoteEventArgs;
        }

        
        public PSObject SerializedRemoteEventArgs { get; }

        internal static bool IsRemoteSourceEventArgs(object argument)
        {
            return Deserializer.IsDeserializedInstanceOfType(argument, typeof(EventArgs));
        }
    }

    
    internal class PSEventArgs<T> : EventArgs
    {
        
        internal T Args;

        
        public PSEventArgs(T args)
        {
            Args = args;
        }
    }

    
    public class PSEventArgs : EventArgs
    {
        
        internal PSEventArgs(string computerName, Guid runspaceId, int eventIdentifier, string sourceIdentifier, object sender, object[] originalArgs, PSObject additionalData)
        {
            // Capture the first EventArgs as SourceEventArgs
            if (originalArgs != null)
            {
                foreach (object argument in originalArgs)
                {
                    EventArgs sourceEventArgs = argument as EventArgs;
                    if (sourceEventArgs != null)
                    {
                        SourceEventArgs = sourceEventArgs;
                        break;
                    }

                    if (ForwardedEventArgs.IsRemoteSourceEventArgs(argument))
                    {
                        SourceEventArgs = new ForwardedEventArgs((PSObject)argument);
                        break;
                    }
                }
            }

            ComputerName = computerName;
            RunspaceId = runspaceId;
            EventIdentifier = eventIdentifier;
            Sender = sender;
            SourceArgs = originalArgs;
            SourceIdentifier = sourceIdentifier;
            TimeGenerated = DateTime.Now;
            MessageData = additionalData;
            ForwardEvent = false;
        }

        
        public string ComputerName { get; internal set; }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        public Guid RunspaceId { get; internal set; }

        
        public int EventIdentifier { get; internal set; }

        
        public object Sender { get; }

        
        public EventArgs SourceEventArgs { get; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] SourceArgs { get; }

        
        public string SourceIdentifier { get; }

        
        public DateTime TimeGenerated
        {
            // internal setter using during deserialization
            get; internal set;
        }

        
        public PSObject MessageData { get; }

        
        internal bool ForwardEvent { get; set; }

        
        internal ManualResetEventSlim EventProcessed { get; set; }
    }

    
    public delegate void PSEventReceivedEventHandler(object sender, PSEventArgs e);

    
    public class PSEventUnsubscribedEventArgs : EventArgs
    {
        
        internal PSEventUnsubscribedEventArgs(PSEventSubscriber eventSubscriber)
        {
            EventSubscriber = eventSubscriber;
        }

        
        public PSEventSubscriber EventSubscriber { get; internal set; }
    }

    
    public delegate void PSEventUnsubscribedEventHandler(object sender, PSEventUnsubscribedEventArgs e);

    
    public class PSEventArgsCollection : IEnumerable<PSEventArgs>
    {
        
        public event PSEventReceivedEventHandler PSEventReceived;

        private readonly List<PSEventArgs> _eventCollection = new List<PSEventArgs>();

        
        internal void Add(PSEventArgs eventToAdd)
        {
            ArgumentNullException.ThrowIfNull(eventToAdd);

            _eventCollection.Add(eventToAdd);

            OnPSEventReceived(eventToAdd.Sender, eventToAdd);
        }

        
        public int Count
        {
            get
            {
                return _eventCollection.Count;
            }
        }

        
        public void RemoveAt(int index)
        {
            _eventCollection.RemoveAt(index);
        }

        
        public PSEventArgs this[int index]
        {
            get
            {
                return _eventCollection[index];
            }
        }

        private void OnPSEventReceived(object sender, PSEventArgs e)
        {
            PSEventReceived?.Invoke(sender, e);
        }

        
        public IEnumerator<PSEventArgs> GetEnumerator()
        {
            return _eventCollection.GetEnumerator();
        }

        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _eventCollection.GetEnumerator();
        }

        
        public object SyncRoot { get; } = new object();
    }

    
    internal class EventAction
    {
        public EventAction(PSEventSubscriber sender, PSEventArgs args)
        {
            Sender = sender;
            Args = args;
        }

        
        public PSEventSubscriber Sender { get; }

        
        public PSEventArgs Args { get; }
    }

    
    public class PSEventJob : Job
    {
        
        public PSEventJob(
            PSEventManager eventManager,
            PSEventSubscriber subscriber,
            ScriptBlock action,
            string name)
            : base(action?.ToString(), name)
        {
            ArgumentNullException.ThrowIfNull(eventManager);

            ArgumentNullException.ThrowIfNull(subscriber);

            UsesResultsCollection = true;
            ScriptBlock = action;
            _eventManager = eventManager;
            _subscriber = subscriber;
        }

        private readonly PSEventManager _eventManager = null;
        private readonly PSEventSubscriber _subscriber = null;
        private int _highestErrorIndex = 0;

        
        public PSModuleInfo Module
        {
            get { return ScriptBlock.Module; }
        }

        
        public override void StopJob()
        {
            _eventManager.UnsubscribeEvent(_subscriber);
        }

        
        public override string StatusMessage { get; } = null;

        
        public override bool HasMoreData
        {
            get
            {
                return _moreData;
            }
        }

        private bool _moreData = false;

        
        public override string Location
        {
            get
            {
                return null;
            }
        }

        
        internal ScriptBlock ScriptBlock { get; }

        
        internal void Invoke(PSEventSubscriber eventSubscriber, PSEventArgs eventArgs)
        {
            if (IsFinishedState(JobStateInfo.State))
                return;

            SetJobState(JobState.Running);

            // Prepare the automatic variables
            SessionState actionState = ScriptBlock.SessionStateInternal.PublicSessionState;

            // $psEventSubscriber = The subscriber
            // that generated this event
            actionState.PSVariable.Set("eventSubscriber", eventSubscriber);
            // $psEvent = The extended event information
            actionState.PSVariable.Set("event", eventArgs);

            // $sender = $psEvent.Sender
            actionState.PSVariable.Set("sender", eventArgs.Sender);
            // $eventArgs = $psEvent.SourceEventArgs
            actionState.PSVariable.Set("eventArgs", eventArgs.SourceEventArgs);

            List<object> results = new List<object>();

            // $args = $psEventArgs.SourceArgs (for PARAM statement)
            try
            {
                Pipe outputPipe = new Pipe(results);
                ScriptBlock.InvokeWithPipe(
                    useLocalScope: false,
                    errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe,
                    dollarUnder: AutomationNull.Value,
                    input: AutomationNull.Value,
                    scriptThis: AutomationNull.Value,
                    outputPipe: outputPipe,
                    invocationInfo: null,
                    args: eventArgs.SourceArgs);
            }
            catch (Exception e)
            {
                // Catch-all OK. This is a third-party call-out.
                if (e is not PipelineStoppedException)
                {
                    LogErrorsAndOutput(results, actionState);
                    SetJobState(JobState.Failed);
                }

                throw;
            }
            

            LogErrorsAndOutput(results, actionState);
            _moreData = true;
        }

        internal void NotifyJobStopped()
        {
            SetJobState(JobState.Stopped);
            _moreData = false;
        }

        private void LogErrorsAndOutput(List<object> results, SessionState actionState)
        {
            // Add the output to the job
            for (int i = 0; i < results.Count; i++)
            {
                this.WriteObject(results[i]);
            }

            // And the errors
            Error.Clear();
            int currentErrorIndex = 0;
            var errors = (ArrayList)actionState.PSVariable.Get("error").Value;
            errors.Reverse();

            for (int i = 0; i < errors.Count; i++)
            {
                var error = (ErrorRecord)errors[i];
                if (currentErrorIndex == _highestErrorIndex)
                {
                    this.WriteError(error);
                    _highestErrorIndex++;
                }

                currentErrorIndex++;
            }
        }
    }
}
