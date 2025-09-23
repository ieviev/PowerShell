// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Description("Provides a System.Windows.Controls.Button control that is always visible in the automation tree.")]
    public class AutomationButton : Button
    {
        #region Constructors

        
        public AutomationButton()
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overides

        
        /// <returns>The <see cref="System.Windows.Automation.Peers.AutomationPeer"/> implementations for this control.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutomationButtonAutomationPeer(this);
        }

        #endregion
    }

    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal class AutomationButtonAutomationPeer : ButtonAutomationPeer
    {
        #region Constructors

        
        /// <param name="owner">The owner of the automation peer.</param>
        public AutomationButtonAutomationPeer(Button owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        
        /// <returns>This method always returns false.</returns>
        protected override bool IsControlElementCore()
        {
            return this.Owner.Visibility != Visibility.Hidden;
        }

        #endregion
    }
}
