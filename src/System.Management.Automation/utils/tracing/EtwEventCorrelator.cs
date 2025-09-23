// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

namespace System.Management.Automation.Tracing
{
    using System;
    using System.Diagnostics.Eventing;

    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Etw")]
#nullable enable
    public interface IEtwEventCorrelator
    {
        
        Guid CurrentActivityId { get; set; }

        
        IEtwActivityReverter StartActivity(Guid relatedActivityId);

        
        IEtwActivityReverter StartActivity();
    }
#nullable restore

    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Etw")]
    public class EtwEventCorrelator :
        IEtwEventCorrelator
    {
        private readonly EventProvider _transferProvider;
        private readonly EventDescriptor _transferEvent;

        
        public EtwEventCorrelator(EventProvider transferProvider, EventDescriptor transferEvent)
        {
            ArgumentNullException.ThrowIfNull(transferProvider);

            _transferProvider = transferProvider;
            _transferEvent = transferEvent;
        }

        
        public Guid CurrentActivityId
        {
            get
            {
                return EtwActivity.GetActivityId();
            }

            set
            {
                EventProvider.SetActivityId(ref value);
            }
        }

        
        public IEtwActivityReverter StartActivity(Guid relatedActivityId)
        {
            var retActivity = new EtwActivityReverter(this, CurrentActivityId);
            CurrentActivityId = EventProvider.CreateActivityId();

            if (relatedActivityId != Guid.Empty)
            {
                var tempTransferEvent = _transferEvent;
                _transferProvider.WriteTransferEvent(in tempTransferEvent, relatedActivityId);
            }

            return retActivity;
        }

        
        public IEtwActivityReverter StartActivity()
        {
            return StartActivity(CurrentActivityId);
        }
    }
}

#endif
