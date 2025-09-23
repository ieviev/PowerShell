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

        
        /// <param name="owner">The owner of the automation peer.</param>
        public ExtendedFrameworkElementAutomationPeer(FrameworkElement owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        
        /// <param name="owner">The owner of the automation peer.</param>
        /// <param name="controlType">The control type of the element that is associated with the automation peer.</param>
        public ExtendedFrameworkElementAutomationPeer(FrameworkElement owner, AutomationControlType controlType)
            : this(owner)
        {
            this.controlType = controlType;
        }

        
        /// <param name="owner">The owner of the automation peer.</param>
        /// <param name="controlType">The control type of the element that is associated with the automation peer.</param>
        /// <param name="isControlElement">Whether the element should show in the logical tree.</param>
        public ExtendedFrameworkElementAutomationPeer(FrameworkElement owner, AutomationControlType controlType, bool isControlElement)
            : this(owner, controlType)
        {
            this.isControlElement = isControlElement;
        }

        #endregion

        #region Overrides

        
        /// <returns>The class name.</returns>
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }

        
        /// <returns>Returns the control type of the element that is associated with the automation peer.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return this.controlType;
        }

        
        /// <returns>This method always returns true.</returns>
        protected override bool IsControlElementCore()
        {
            return this.isControlElement;
        }

        #endregion
    }
}
