// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public sealed class ProviderIntrinsics
    {
        #region Constructors

        
        private ProviderIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        
        internal ProviderIntrinsics(Cmdlet cmdlet)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            _cmdlet = cmdlet;
            Item = new ItemCmdletProviderIntrinsics(cmdlet);
            ChildItem = new ChildItemCmdletProviderIntrinsics(cmdlet);
            Content = new ContentCmdletProviderIntrinsics(cmdlet);
            Property = new PropertyCmdletProviderIntrinsics(cmdlet);
            SecurityDescriptor = new SecurityDescriptorCmdletProviderIntrinsics(cmdlet);
        }

        
        internal ProviderIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            Item = new ItemCmdletProviderIntrinsics(sessionState);
            ChildItem = new ChildItemCmdletProviderIntrinsics(sessionState);
            Content = new ContentCmdletProviderIntrinsics(sessionState);
            Property = new PropertyCmdletProviderIntrinsics(sessionState);
            SecurityDescriptor = new SecurityDescriptorCmdletProviderIntrinsics(sessionState);
        }

        #endregion Constructors

        #region Public members

        
        public ItemCmdletProviderIntrinsics Item { get; }

        
        public ChildItemCmdletProviderIntrinsics ChildItem { get; }

        
        public ContentCmdletProviderIntrinsics Content { get; }

        
        public PropertyCmdletProviderIntrinsics Property { get; }

        
        public SecurityDescriptorCmdletProviderIntrinsics SecurityDescriptor { get; }

        #endregion Public members

        #region private data

        private readonly InternalCommand _cmdlet;

        #endregion private data
    }
}
