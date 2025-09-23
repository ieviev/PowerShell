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
        
        /// <remarks>
        ///     <para>This method should only be used for advanced scenarios
        ///         or diagnostics.  Prefer using <see cref="StartActivity()"/>
        ///         or <see cref="StartActivity(Guid)"/> instead.</para>
        /// </remarks>
        Guid CurrentActivityId { get; set; }

        
        /// <param name="relatedActivityId">The ID of an existing activity to be correlated with the
        ///     new activity or <see cref="Guid.Empty"/> if correlation is not desired.</param>
        /// <returns>An object which can be used to revert the activity ID of the current thread once
        ///     the new activity yields control of the current thread.</returns>
        IEtwActivityReverter StartActivity(Guid relatedActivityId);

        
        /// <returns>An object which can be used to revert the activity ID of the current thread once
        ///     the new activity yields control of the current thread.</returns>
        IEtwActivityReverter StartActivity();
    }
#nullable restore

    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Etw")]
    public class EtwEventCorrelator :
        IEtwEventCorrelator
    {
        private readonly EventProvider _transferProvider;
        private readonly EventDescriptor _transferEvent;

        
        /// <param name="transferProvider">The <see cref="EventProvider"/> to use when logging transfer events
        ///     during activity correlation.</param>
        /// <param name="transferEvent">The <see cref="EventDescriptor"/> to use when logging transfer events
        ///     during activity correlation.</param>
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
