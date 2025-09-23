// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ExtendedFrameworkElementAutomationPeer : FrameworkElementAutomationPeer
    {
        #region Fields

        
        private AutomationControlType controlType = AutomationControlType.Custom;

        
        private bool isControlElement = true;

        #endregion

        #region Structors

        
        public ExtendedFrameworkElementAutomationPeer(FrameworkElement owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        
        public ExtendedFrameworkElementAutomationPeer(FrameworkElement owner, AutomationControlType controlType)
            : this(owner)
        {
            this.controlType = controlType;
        }

        
        public ExtendedFrameworkElementAutomationPeer(FrameworkElement owner, AutomationControlType controlType, bool isControlElement)
            : this(owner, controlType)
        {
            this.isControlElement = isControlElement;
        }

        #endregion

        #region Overrides

        
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }

        
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return this.controlType;
        }

        
        protected override bool IsControlElementCore()
        {
            return this.isControlElement;
        }

        #endregion
    }
}
