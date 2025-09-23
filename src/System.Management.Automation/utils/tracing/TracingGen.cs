// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing;

namespace System.Management.Automation.Tracing
{
    
    public sealed partial class Tracer : System.Management.Automation.Tracing.EtwActivity
    {
        
        public const byte LevelCritical = 1;
        
        public const byte LevelError = 2;
        
        public const byte LevelWarning = 3;
        
        public const byte LevelInformational = 4;
        
        public const byte LevelVerbose = 5;
        
        public const long KeywordAll = 0xFFFFFFFF;

        private static readonly Guid providerId = Guid.Parse("a0c1853b-5c40-4b15-8766-3cf1c58f985a");
        private static readonly EventDescriptor WriteTransferEventEvent;
        private static readonly EventDescriptor DebugMessageEvent;
        private static readonly EventDescriptor M3PAbortingWorkflowExecutionEvent;
        private static readonly EventDescriptor M3PActivityExecutionFinishedEvent;
        private static readonly EventDescriptor M3PActivityExecutionQueuedEvent;
        private static readonly EventDescriptor M3PActivityExecutionStartedEvent;
        private static readonly EventDescriptor M3PBeginContainerParentJobExecutionEvent;
        private static readonly EventDescriptor M3PBeginCreateNewJobEvent;
        private static readonly EventDescriptor M3PBeginJobLogicEvent;
        private static readonly EventDescriptor M3PBeginProxyChildJobEventHandlerEvent;
        private static readonly EventDescriptor M3PBeginProxyJobEventHandlerEvent;
        private static readonly EventDescriptor M3PBeginProxyJobExecutionEvent;
        private static readonly EventDescriptor M3PBeginRunGarbageCollectionEvent;
        private static readonly EventDescriptor M3PBeginStartWorkflowApplicationEvent;
        private static readonly EventDescriptor M3PBeginWorkflowExecutionEvent;
        private static readonly EventDescriptor M3PCancellingWorkflowExecutionEvent;
        private static readonly EventDescriptor M3PChildWorkflowJobAdditionEvent;
        private static readonly EventDescriptor M3PEndContainerParentJobExecutionEvent;
        private static readonly EventDescriptor M3PEndCreateNewJobEvent;
        private static readonly EventDescriptor M3PEndJobLogicEvent;
        private static readonly EventDescriptor M3PEndpointDisabledEvent;
        private static readonly EventDescriptor M3PEndpointEnabledEvent;
        private static readonly EventDescriptor M3PEndpointModifiedEvent;
        private static readonly EventDescriptor M3PEndpointRegisteredEvent;
        private static readonly EventDescriptor M3PEndpointUnregisteredEvent;
        private static readonly EventDescriptor M3PEndProxyChildJobEventHandlerEvent;
        private static readonly EventDescriptor M3PEndProxyJobEventHandlerEvent;
        private static readonly EventDescriptor M3PEndProxyJobExecutionEvent;
        private static readonly EventDescriptor M3PEndRunGarbageCollectionEvent;
        private static readonly EventDescriptor M3PEndStartWorkflowApplicationEvent;
        private static readonly EventDescriptor M3PEndWorkflowExecutionEvent;
        private static readonly EventDescriptor M3PErrorImportingWorkflowFromXamlEvent;
        private static readonly EventDescriptor M3PForcedWorkflowShutdownErrorEvent;
        private static readonly EventDescriptor M3PForcedWorkflowShutdownFinishedEvent;
        private static readonly EventDescriptor M3PForcedWorkflowShutdownStartedEvent;
        private static readonly EventDescriptor M3PImportedWorkflowFromXamlEvent;
        private static readonly EventDescriptor M3PImportingWorkflowFromXamlEvent;
        private static readonly EventDescriptor M3PJobCreationCompleteEvent;
        private static readonly EventDescriptor M3PJobErrorEvent;
        private static readonly EventDescriptor M3PJobRemovedEvent;
        private static readonly EventDescriptor M3PJobRemoveErrorEvent;
        private static readonly EventDescriptor M3PJobStateChangedEvent;
        private static readonly EventDescriptor M3PLoadingWorkflowForExecutionEvent;
        private static readonly EventDescriptor M3POutOfProcessRunspaceStartedEvent;
        private static readonly EventDescriptor M3PParameterSplattingWasPerformedEvent;
        private static readonly EventDescriptor M3PParentJobCreatedEvent;
        private static readonly EventDescriptor M3PPersistenceStoreMaxSizeReachedEvent;
        private static readonly EventDescriptor M3PPersistingWorkflowEvent;
        private static readonly EventDescriptor M3PProxyJobRemoteJobAssociationEvent;
        private static readonly EventDescriptor M3PRemoveJobStartedEvent;
        private static readonly EventDescriptor M3PRunspaceAvailabilityChangedEvent;
        private static readonly EventDescriptor M3PRunspaceStateChangedEvent;
        private static readonly EventDescriptor M3PTrackingGuidContainerParentJobCorrelationEvent;
        private static readonly EventDescriptor M3PUnloadingWorkflowEvent;
        private static readonly EventDescriptor M3PWorkflowActivityExecutionFailedEvent;
        private static readonly EventDescriptor M3PWorkflowActivityValidatedEvent;
        private static readonly EventDescriptor M3PWorkflowActivityValidationFailedEvent;
        private static readonly EventDescriptor M3PWorkflowCleanupPerformedEvent;
        private static readonly EventDescriptor M3PWorkflowDeletedFromDiskEvent;
        private static readonly EventDescriptor M3PWorkflowEngineStartedEvent;
        private static readonly EventDescriptor M3PWorkflowExecutionAbortedEvent;
        private static readonly EventDescriptor M3PWorkflowExecutionCancelledEvent;
        private static readonly EventDescriptor M3PWorkflowExecutionErrorEvent;
        private static readonly EventDescriptor M3PWorkflowExecutionFinishedEvent;
        private static readonly EventDescriptor M3PWorkflowExecutionStartedEvent;
        private static readonly EventDescriptor M3PWorkflowJobCreatedEvent;
        private static readonly EventDescriptor M3PWorkflowLoadedForExecutionEvent;
        private static readonly EventDescriptor M3PWorkflowLoadedFromDiskEvent;
        private static readonly EventDescriptor M3PWorkflowManagerCheckpointEvent;
        private static readonly EventDescriptor M3PWorkflowPersistedEvent;
        private static readonly EventDescriptor M3PWorkflowPluginRequestedToShutdownEvent;
        private static readonly EventDescriptor M3PWorkflowPluginRestartedEvent;
        private static readonly EventDescriptor M3PWorkflowPluginStartedEvent;
        private static readonly EventDescriptor M3PWorkflowQuotaViolatedEvent;
        private static readonly EventDescriptor M3PWorkflowResumedEvent;
        private static readonly EventDescriptor M3PWorkflowResumingEvent;
        private static readonly EventDescriptor M3PWorkflowRunspacePoolCreatedEvent;
        private static readonly EventDescriptor M3PWorkflowStateChangedEvent;
        private static readonly EventDescriptor M3PWorkflowUnloadedEvent;
        private static readonly EventDescriptor M3PWorkflowValidationErrorEvent;
        private static readonly EventDescriptor M3PWorkflowValidationFinishedEvent;
        private static readonly EventDescriptor M3PWorkflowValidationStartedEvent;

        
        static Tracer()
        {
            unchecked
            {
                WriteTransferEventEvent = new EventDescriptor(0x1f05, 0x1, 0x11, 0x5, 0x14, 0x0, (long)0x4000000000000000);
                DebugMessageEvent = new EventDescriptor(0xc000, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PAbortingWorkflowExecutionEvent = new EventDescriptor(0xb038, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PActivityExecutionFinishedEvent = new EventDescriptor(0xb03f, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PActivityExecutionQueuedEvent = new EventDescriptor(0xb017, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PActivityExecutionStartedEvent = new EventDescriptor(0xb018, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PBeginContainerParentJobExecutionEvent = new EventDescriptor(0xb50c, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PBeginCreateNewJobEvent = new EventDescriptor(0xb503, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PBeginJobLogicEvent = new EventDescriptor(0xb506, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PBeginProxyChildJobEventHandlerEvent = new EventDescriptor(0xb512, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PBeginProxyJobEventHandlerEvent = new EventDescriptor(0xb510, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PBeginProxyJobExecutionEvent = new EventDescriptor(0xb50e, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PBeginRunGarbageCollectionEvent = new EventDescriptor(0xb514, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PBeginStartWorkflowApplicationEvent = new EventDescriptor(0xb501, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PBeginWorkflowExecutionEvent = new EventDescriptor(0xb508, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PCancellingWorkflowExecutionEvent = new EventDescriptor(0xb037, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PChildWorkflowJobAdditionEvent = new EventDescriptor(0xb50a, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PEndContainerParentJobExecutionEvent = new EventDescriptor(0xb50d, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PEndCreateNewJobEvent = new EventDescriptor(0xb504, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PEndJobLogicEvent = new EventDescriptor(0xb507, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PEndpointDisabledEvent = new EventDescriptor(0xb044, 0x1, 0x11, 0x5, 0x14, 0x9, (long)0x4000000000000200);
                M3PEndpointEnabledEvent = new EventDescriptor(0xb045, 0x1, 0x11, 0x5, 0x14, 0x9, (long)0x4000000000000200);
                M3PEndpointModifiedEvent = new EventDescriptor(0xb042, 0x1, 0x11, 0x5, 0x14, 0x9, (long)0x4000000000000200);
                M3PEndpointRegisteredEvent = new EventDescriptor(0xb041, 0x1, 0x11, 0x5, 0x14, 0x9, (long)0x4000000000000200);
                M3PEndpointUnregisteredEvent = new EventDescriptor(0xb043, 0x1, 0x11, 0x5, 0x14, 0x9, (long)0x4000000000000200);
                M3PEndProxyChildJobEventHandlerEvent = new EventDescriptor(0xb513, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PEndProxyJobEventHandlerEvent = new EventDescriptor(0xb511, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PEndProxyJobExecutionEvent = new EventDescriptor(0xb50f, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PEndRunGarbageCollectionEvent = new EventDescriptor(0xb515, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PEndStartWorkflowApplicationEvent = new EventDescriptor(0xb502, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PEndWorkflowExecutionEvent = new EventDescriptor(0xb509, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PErrorImportingWorkflowFromXamlEvent = new EventDescriptor(0xb01b, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PForcedWorkflowShutdownErrorEvent = new EventDescriptor(0xb03c, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PForcedWorkflowShutdownFinishedEvent = new EventDescriptor(0xb03b, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PForcedWorkflowShutdownStartedEvent = new EventDescriptor(0xb03a, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PImportedWorkflowFromXamlEvent = new EventDescriptor(0xb01a, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PImportingWorkflowFromXamlEvent = new EventDescriptor(0xb019, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PJobCreationCompleteEvent = new EventDescriptor(0xb032, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PJobErrorEvent = new EventDescriptor(0xb02e, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PJobRemovedEvent = new EventDescriptor(0xb033, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PJobRemoveErrorEvent = new EventDescriptor(0xb034, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PJobStateChangedEvent = new EventDescriptor(0xb02d, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PLoadingWorkflowForExecutionEvent = new EventDescriptor(0xb035, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3POutOfProcessRunspaceStartedEvent = new EventDescriptor(0xb046, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PParameterSplattingWasPerformedEvent = new EventDescriptor(0xb047, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PParentJobCreatedEvent = new EventDescriptor(0xb031, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PPersistenceStoreMaxSizeReachedEvent = new EventDescriptor(0xb516, 0x1, 0x10, 0x3, 0x0, 0x0, (long)0x8000000000000000);
                M3PPersistingWorkflowEvent = new EventDescriptor(0xb03d, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PProxyJobRemoteJobAssociationEvent = new EventDescriptor(0xb50b, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PRemoveJobStartedEvent = new EventDescriptor(0xb02c, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PRunspaceAvailabilityChangedEvent = new EventDescriptor(0xb022, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PRunspaceStateChangedEvent = new EventDescriptor(0xb023, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PTrackingGuidContainerParentJobCorrelationEvent = new EventDescriptor(0xb505, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000000);
                M3PUnloadingWorkflowEvent = new EventDescriptor(0xb039, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowActivityExecutionFailedEvent = new EventDescriptor(0xb021, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowActivityValidatedEvent = new EventDescriptor(0xb01f, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowActivityValidationFailedEvent = new EventDescriptor(0xb020, 0x1, 0x11, 0x5, 0x14, 0x8, (long)0x4000000000000200);
                M3PWorkflowCleanupPerformedEvent = new EventDescriptor(0xb028, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowDeletedFromDiskEvent = new EventDescriptor(0xb02a, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowEngineStartedEvent = new EventDescriptor(0xb048, 0x1, 0x11, 0x5, 0x14, 0x5, (long)0x4000000000000200);
                M3PWorkflowExecutionAbortedEvent = new EventDescriptor(0xb027, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowExecutionCancelledEvent = new EventDescriptor(0xb026, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowExecutionErrorEvent = new EventDescriptor(0xb040, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowExecutionFinishedEvent = new EventDescriptor(0xb036, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowExecutionStartedEvent = new EventDescriptor(0xb008, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowJobCreatedEvent = new EventDescriptor(0xb030, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowLoadedForExecutionEvent = new EventDescriptor(0xb024, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowLoadedFromDiskEvent = new EventDescriptor(0xb029, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowManagerCheckpointEvent = new EventDescriptor(0xb049, 0x1, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000000200);
                M3PWorkflowPersistedEvent = new EventDescriptor(0xb03e, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowPluginRequestedToShutdownEvent = new EventDescriptor(0xb010, 0x1, 0x11, 0x5, 0x14, 0x5, (long)0x4000000000000200);
                M3PWorkflowPluginRestartedEvent = new EventDescriptor(0xb011, 0x1, 0x11, 0x5, 0x14, 0x5, (long)0x4000000000000200);
                M3PWorkflowPluginStartedEvent = new EventDescriptor(0xb007, 0x1, 0x11, 0x5, 0x14, 0x5, (long)0x4000000000000200);
                M3PWorkflowQuotaViolatedEvent = new EventDescriptor(0xb013, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowResumedEvent = new EventDescriptor(0xb014, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowResumingEvent = new EventDescriptor(0xb012, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowRunspacePoolCreatedEvent = new EventDescriptor(0xb016, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowStateChangedEvent = new EventDescriptor(0xb009, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowUnloadedEvent = new EventDescriptor(0xb025, 0x1, 0x11, 0x5, 0x14, 0x6, (long)0x4000000000000200);
                M3PWorkflowValidationErrorEvent = new EventDescriptor(0xb01e, 0x1, 0x11, 0x5, 0x14, 0x8, (long)0x4000000000000200);
                M3PWorkflowValidationFinishedEvent = new EventDescriptor(0xb01d, 0x1, 0x11, 0x5, 0x14, 0x8, (long)0x4000000000000200);
                M3PWorkflowValidationStartedEvent = new EventDescriptor(0xb01c, 0x1, 0x11, 0x5, 0x14, 0x8, (long)0x4000000000000200);
            }
        }

        
        public Tracer() : base() { }

        
        protected override Guid ProviderId
        {
            get
            {
                return providerId;
            }
        }

        
        protected override EventDescriptor TransferEvent
        {
            get
            {
                return WriteTransferEventEvent;
            }
        }

        
        [EtwEvent(0x1f05)]
        public void WriteTransferEvent(Guid currentActivityId, Guid parentActivityId)
        {
            WriteEvent(WriteTransferEventEvent, currentActivityId, parentActivityId);
        }

        
        [EtwEvent(0xc000)]
        public void DebugMessage(string message)
        {
            WriteEvent(DebugMessageEvent, message);
        }

        
        [EtwEvent(0xb038)]
        public void AbortingWorkflowExecution(Guid workflowId, string reason)
        {
            WriteEvent(M3PAbortingWorkflowExecutionEvent, workflowId, reason);
        }

        
        [EtwEvent(0xb03f)]
        public void ActivityExecutionFinished(string activityName)
        {
            WriteEvent(M3PActivityExecutionFinishedEvent, activityName);
        }

        
        [EtwEvent(0xb017)]
        public void ActivityExecutionQueued(Guid workflowId, string activityName)
        {
            WriteEvent(M3PActivityExecutionQueuedEvent, workflowId, activityName);
        }

        
        [EtwEvent(0xb018)]
        public void ActivityExecutionStarted(string activityName, string activityTypeName)
        {
            WriteEvent(M3PActivityExecutionStartedEvent, activityName, activityTypeName);
        }

        
        [EtwEvent(0xb50c)]
        public void BeginContainerParentJobExecution(Guid containerParentJobInstanceId)
        {
            WriteEvent(M3PBeginContainerParentJobExecutionEvent, containerParentJobInstanceId);
        }

        
        [EtwEvent(0xb503)]
        public void BeginCreateNewJob(Guid trackingId)
        {
            WriteEvent(M3PBeginCreateNewJobEvent, trackingId);
        }

        
        [EtwEvent(0xb506)]
        public void BeginJobLogic(Guid workflowJobJobInstanceId)
        {
            WriteEvent(M3PBeginJobLogicEvent, workflowJobJobInstanceId);
        }

        
        [EtwEvent(0xb512)]
        public void BeginProxyChildJobEventHandler(Guid proxyChildJobInstanceId)
        {
            WriteEvent(M3PBeginProxyChildJobEventHandlerEvent, proxyChildJobInstanceId);
        }

        
        [EtwEvent(0xb510)]
        public void BeginProxyJobEventHandler(Guid proxyJobInstanceId)
        {
            WriteEvent(M3PBeginProxyJobEventHandlerEvent, proxyJobInstanceId);
        }

        
        [EtwEvent(0xb50e)]
        public void BeginProxyJobExecution(Guid proxyJobInstanceId)
        {
            WriteEvent(M3PBeginProxyJobExecutionEvent, proxyJobInstanceId);
        }

        
        [EtwEvent(0xb514)]
        public void BeginRunGarbageCollection()
        {
            WriteEvent(M3PBeginRunGarbageCollectionEvent);
        }

        
        [EtwEvent(0xb501)]
        public void BeginStartWorkflowApplication(Guid trackingId)
        {
            WriteEvent(M3PBeginStartWorkflowApplicationEvent, trackingId);
        }

        
        [EtwEvent(0xb508)]
        public void BeginWorkflowExecution(Guid workflowJobJobInstanceId)
        {
            WriteEvent(M3PBeginWorkflowExecutionEvent, workflowJobJobInstanceId);
        }

        
        [EtwEvent(0xb037)]
        public void CancellingWorkflowExecution(Guid workflowId)
        {
            WriteEvent(M3PCancellingWorkflowExecutionEvent, workflowId);
        }

        
        [EtwEvent(0xb50a)]
        public void ChildWorkflowJobAddition(Guid workflowJobInstanceId, Guid containerParentJobInstanceId)
        {
            WriteEvent(M3PChildWorkflowJobAdditionEvent, workflowJobInstanceId, containerParentJobInstanceId);
        }

        
        [EtwEvent(0xb50d)]
        public void EndContainerParentJobExecution(Guid containerParentJobInstanceId)
        {
            WriteEvent(M3PEndContainerParentJobExecutionEvent, containerParentJobInstanceId);
        }

        
        [EtwEvent(0xb504)]
        public void EndCreateNewJob(Guid trackingId)
        {
            WriteEvent(M3PEndCreateNewJobEvent, trackingId);
        }

        
        [EtwEvent(0xb507)]
        public void EndJobLogic(Guid workflowJobJobInstanceId)
        {
            WriteEvent(M3PEndJobLogicEvent, workflowJobJobInstanceId);
        }

        
        [EtwEvent(0xb044)]
        public void EndpointDisabled(string endpointName, string disabledBy)
        {
            WriteEvent(M3PEndpointDisabledEvent, endpointName, disabledBy);
        }

        
        [EtwEvent(0xb045)]
        public void EndpointEnabled(string endpointName, string enabledBy)
        {
            WriteEvent(M3PEndpointEnabledEvent, endpointName, enabledBy);
        }

        
        [EtwEvent(0xb042)]
        public void EndpointModified(string endpointName, string modifiedBy)
        {
            WriteEvent(M3PEndpointModifiedEvent, endpointName, modifiedBy);
        }

        
        [EtwEvent(0xb041)]
        public void EndpointRegistered(string endpointName, string registeredBy)
        {
            WriteEvent(M3PEndpointRegisteredEvent, endpointName, registeredBy);
        }

        
        [EtwEvent(0xb043)]
        public void EndpointUnregistered(string endpointName, string unregisteredBy)
        {
            WriteEvent(M3PEndpointUnregisteredEvent, endpointName, unregisteredBy);
        }

        
        [EtwEvent(0xb513)]
        public void EndProxyChildJobEventHandler(Guid proxyChildJobInstanceId)
        {
            WriteEvent(M3PEndProxyChildJobEventHandlerEvent, proxyChildJobInstanceId);
        }

        
        [EtwEvent(0xb511)]
        public void EndProxyJobEventHandler(Guid proxyJobInstanceId)
        {
            WriteEvent(M3PEndProxyJobEventHandlerEvent, proxyJobInstanceId);
        }

        
        [EtwEvent(0xb50f)]
        public void EndProxyJobExecution(Guid proxyJobInstanceId)
        {
            WriteEvent(M3PEndProxyJobExecutionEvent, proxyJobInstanceId);
        }

        
        [EtwEvent(0xb515)]
        public void EndRunGarbageCollection()
        {
            WriteEvent(M3PEndRunGarbageCollectionEvent);
        }

        
        [EtwEvent(0xb502)]
        public void EndStartWorkflowApplication(Guid trackingId)
        {
            WriteEvent(M3PEndStartWorkflowApplicationEvent, trackingId);
        }

        
        [EtwEvent(0xb509)]
        public void EndWorkflowExecution(Guid workflowJobJobInstanceId)
        {
            WriteEvent(M3PEndWorkflowExecutionEvent, workflowJobJobInstanceId);
        }

        
        [EtwEvent(0xb01b)]
        public void ErrorImportingWorkflowFromXaml(Guid workflowId, string errorDescription)
        {
            WriteEvent(M3PErrorImportingWorkflowFromXamlEvent, workflowId, errorDescription);
        }

        
        [EtwEvent(0xb03c)]
        public void ForcedWorkflowShutdownError(Guid workflowId, string errorDescription)
        {
            WriteEvent(M3PForcedWorkflowShutdownErrorEvent, workflowId, errorDescription);
        }

        
        [EtwEvent(0xb03b)]
        public void ForcedWorkflowShutdownFinished(Guid workflowId)
        {
            WriteEvent(M3PForcedWorkflowShutdownFinishedEvent, workflowId);
        }

        
        [EtwEvent(0xb03a)]
        public void ForcedWorkflowShutdownStarted(Guid workflowId)
        {
            WriteEvent(M3PForcedWorkflowShutdownStartedEvent, workflowId);
        }

        
        [EtwEvent(0xb01a)]
        public void ImportedWorkflowFromXaml(Guid workflowId, string xamlFile)
        {
            WriteEvent(M3PImportedWorkflowFromXamlEvent, workflowId, xamlFile);
        }

        
        [EtwEvent(0xb019)]
        public void ImportingWorkflowFromXaml(Guid workflowId, string xamlFile)
        {
            WriteEvent(M3PImportingWorkflowFromXamlEvent, workflowId, xamlFile);
        }

        
        [EtwEvent(0xb032)]
        public void JobCreationComplete(Guid jobId, Guid workflowId)
        {
            WriteEvent(M3PJobCreationCompleteEvent, jobId, workflowId);
        }

        
        [EtwEvent(0xb02e)]
        public void JobError(int jobId, Guid workflowId, string errorDescription)
        {
            WriteEvent(M3PJobErrorEvent, jobId, workflowId, errorDescription);
        }

        
        [EtwEvent(0xb033)]
        public void JobRemoved(Guid parentJobId, Guid childJobId, Guid workflowId)
        {
            WriteEvent(M3PJobRemovedEvent, parentJobId, childJobId, workflowId);
        }

        
        [EtwEvent(0xb034)]
        public void JobRemoveError(Guid parentJobId, Guid childJobId, Guid workflowId, string error)
        {
            WriteEvent(M3PJobRemoveErrorEvent, parentJobId, childJobId, workflowId, error);
        }

        
        [EtwEvent(0xb02d)]
        public void JobStateChanged(int jobId, Guid workflowId, string newState, string oldState)
        {
            WriteEvent(M3PJobStateChangedEvent, jobId, workflowId, newState, oldState);
        }

        
        [EtwEvent(0xb035)]
        public void LoadingWorkflowForExecution(Guid workflowId)
        {
            WriteEvent(M3PLoadingWorkflowForExecutionEvent, workflowId);
        }

        
        [EtwEvent(0xb046)]
        public void OutOfProcessRunspaceStarted(string command)
        {
            WriteEvent(M3POutOfProcessRunspaceStartedEvent, command);
        }

        
        [EtwEvent(0xb047)]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void ParameterSplattingWasPerformed(string parameters, string computers)
        {
            WriteEvent(M3PParameterSplattingWasPerformedEvent, parameters, computers);
        }

        
        [EtwEvent(0xb031)]
        public void ParentJobCreated(Guid jobId)
        {
            WriteEvent(M3PParentJobCreatedEvent, jobId);
        }

        
        [EtwEvent(0xb516)]
        public void PersistenceStoreMaxSizeReached()
        {
            WriteEvent(M3PPersistenceStoreMaxSizeReachedEvent);
        }

        
        [EtwEvent(0xb03d)]
        public void PersistingWorkflow(Guid workflowId, string persistPath)
        {
            WriteEvent(M3PPersistingWorkflowEvent, workflowId, persistPath);
        }

        
        [EtwEvent(0xb50b)]
        public void ProxyJobRemoteJobAssociation(Guid proxyJobInstanceId, Guid containerParentJobInstanceId)
        {
            WriteEvent(M3PProxyJobRemoteJobAssociationEvent, proxyJobInstanceId, containerParentJobInstanceId);
        }

        
        [EtwEvent(0xb02c)]
        public void RemoveJobStarted(Guid jobId)
        {
            WriteEvent(M3PRemoveJobStartedEvent, jobId);
        }

        
        [EtwEvent(0xb022)]
        public void RunspaceAvailabilityChanged(string runspaceId, string availability)
        {
            WriteEvent(M3PRunspaceAvailabilityChangedEvent, runspaceId, availability);
        }

        
        [EtwEvent(0xb023)]
        public void RunspaceStateChanged(string runspaceId, string newState, string oldState)
        {
            WriteEvent(M3PRunspaceStateChangedEvent, runspaceId, newState, oldState);
        }

        
        [EtwEvent(0xb505)]
        public void TrackingGuidContainerParentJobCorrelation(Guid trackingId, Guid containerParentJobInstanceId)
        {
            WriteEvent(M3PTrackingGuidContainerParentJobCorrelationEvent, trackingId, containerParentJobInstanceId);
        }

        
        [EtwEvent(0xb039)]
        public void UnloadingWorkflow(Guid workflowId)
        {
            WriteEvent(M3PUnloadingWorkflowEvent, workflowId);
        }

        
        [EtwEvent(0xb021)]
        public void WorkflowActivityExecutionFailed(Guid workflowId, string activityName, string failureDescription)
        {
            WriteEvent(M3PWorkflowActivityExecutionFailedEvent, workflowId, activityName, failureDescription);
        }

        
        [EtwEvent(0xb01f)]
        public void WorkflowActivityValidated(Guid workflowId, string activityDisplayName, string activityType)
        {
            WriteEvent(M3PWorkflowActivityValidatedEvent, workflowId, activityDisplayName, activityType);
        }

        
        [EtwEvent(0xb020)]
        public void WorkflowActivityValidationFailed(Guid workflowId, string activityDisplayName, string activityType)
        {
            WriteEvent(M3PWorkflowActivityValidationFailedEvent, workflowId, activityDisplayName, activityType);
        }

        
        [EtwEvent(0xb028)]
        public void WorkflowCleanupPerformed(Guid workflowId)
        {
            WriteEvent(M3PWorkflowCleanupPerformedEvent, workflowId);
        }

        
        [EtwEvent(0xb02a)]
        public void WorkflowDeletedFromDisk(Guid workflowId, string path)
        {
            WriteEvent(M3PWorkflowDeletedFromDiskEvent, workflowId, path);
        }

        
        [EtwEvent(0xb048)]
        public void WorkflowEngineStarted(string endpointName)
        {
            WriteEvent(M3PWorkflowEngineStartedEvent, endpointName);
        }

        
        [EtwEvent(0xb027)]
        public void WorkflowExecutionAborted(Guid workflowId)
        {
            WriteEvent(M3PWorkflowExecutionAbortedEvent, workflowId);
        }

        
        [EtwEvent(0xb026)]
        public void WorkflowExecutionCancelled(Guid workflowId)
        {
            WriteEvent(M3PWorkflowExecutionCancelledEvent, workflowId);
        }

        
        [EtwEvent(0xb040)]
        public void WorkflowExecutionError(Guid workflowId, string errorDescription)
        {
            WriteEvent(M3PWorkflowExecutionErrorEvent, workflowId, errorDescription);
        }

        
        [EtwEvent(0xb036)]
        public void WorkflowExecutionFinished(Guid workflowId)
        {
            WriteEvent(M3PWorkflowExecutionFinishedEvent, workflowId);
        }

        
        [EtwEvent(0xb008)]
        public void WorkflowExecutionStarted(Guid workflowId, string managedNodes)
        {
            WriteEvent(M3PWorkflowExecutionStartedEvent, workflowId, managedNodes);
        }

        
        [EtwEvent(0xb030)]
        public void WorkflowJobCreated(Guid parentJobId, Guid childJobId, Guid childWorkflowId)
        {
            WriteEvent(M3PWorkflowJobCreatedEvent, parentJobId, childJobId, childWorkflowId);
        }

        
        [EtwEvent(0xb024)]
        public void WorkflowLoadedForExecution(Guid workflowId)
        {
            WriteEvent(M3PWorkflowLoadedForExecutionEvent, workflowId);
        }

        
        [EtwEvent(0xb029)]
        public void WorkflowLoadedFromDisk(Guid workflowId, string path)
        {
            WriteEvent(M3PWorkflowLoadedFromDiskEvent, workflowId, path);
        }

        
        [EtwEvent(0xb049)]
        public void WorkflowManagerCheckpoint(string checkpointPath, string configProviderId, string userName, string path)
        {
            WriteEvent(M3PWorkflowManagerCheckpointEvent, checkpointPath, configProviderId, userName, path);
        }

        
        [EtwEvent(0xb03e)]
        public void WorkflowPersisted(Guid workflowId)
        {
            WriteEvent(M3PWorkflowPersistedEvent, workflowId);
        }

        
        [EtwEvent(0xb010)]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void WorkflowPluginRequestedToShutdown(string endpointName)
        {
            WriteEvent(M3PWorkflowPluginRequestedToShutdownEvent, endpointName);
        }

        
        [EtwEvent(0xb011)]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void WorkflowPluginRestarted(string endpointName)
        {
            WriteEvent(M3PWorkflowPluginRestartedEvent, endpointName);
        }

        
        [EtwEvent(0xb007)]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public void WorkflowPluginStarted(string endpointName, string user, string hostingMode, string protocol, string configuration)
        {
            WriteEvent(M3PWorkflowPluginStartedEvent, endpointName, user, hostingMode, protocol, configuration);
        }

        
        [EtwEvent(0xb013)]
        public void WorkflowQuotaViolated(string endpointName, string configName, string allowedValue, string valueInQuestion)
        {
            WriteEvent(M3PWorkflowQuotaViolatedEvent, endpointName, configName, allowedValue, valueInQuestion);
        }

        
        [EtwEvent(0xb014)]
        public void WorkflowResumed(Guid workflowId)
        {
            WriteEvent(M3PWorkflowResumedEvent, workflowId);
        }

        
        [EtwEvent(0xb012)]
        public void WorkflowResuming(Guid workflowId)
        {
            WriteEvent(M3PWorkflowResumingEvent, workflowId);
        }

        
        [EtwEvent(0xb016)]
        public void WorkflowRunspacePoolCreated(Guid workflowId, string managedNode)
        {
            WriteEvent(M3PWorkflowRunspacePoolCreatedEvent, workflowId, managedNode);
        }

        
        [EtwEvent(0xb009)]
        public void WorkflowStateChanged(Guid workflowId, string newState, string oldState)
        {
            WriteEvent(M3PWorkflowStateChangedEvent, workflowId, newState, oldState);
        }

        
        [EtwEvent(0xb025)]
        public void WorkflowUnloaded(Guid workflowId)
        {
            WriteEvent(M3PWorkflowUnloadedEvent, workflowId);
        }

        
        [EtwEvent(0xb01e)]
        public void WorkflowValidationError(Guid workflowId)
        {
            WriteEvent(M3PWorkflowValidationErrorEvent, workflowId);
        }

        
        [EtwEvent(0xb01d)]
        public void WorkflowValidationFinished(Guid workflowId)
        {
            WriteEvent(M3PWorkflowValidationFinishedEvent, workflowId);
        }

        
        [EtwEvent(0xb01c)]
        public void WorkflowValidationStarted(Guid workflowId)
        {
            WriteEvent(M3PWorkflowValidationStartedEvent, workflowId);
        }
    }
}

// This code was generated on 02/01/2012 19:52:32

#endif
