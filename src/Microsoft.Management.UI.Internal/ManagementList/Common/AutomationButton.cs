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

        
        public AutomationButtonAutomationPeer(Button owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        
        protected override bool IsControlElementCore()
        {
            return this.Owner.Visibility != Visibility.Hidden;
        }

        #endregion
    }
}
