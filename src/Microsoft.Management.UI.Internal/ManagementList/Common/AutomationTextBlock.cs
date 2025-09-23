// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Description("Provides a System.Windows.Controls.TextBlock control that is always visible in the automation tree.")]
    public class AutomationTextBlock : TextBlock
    {
        #region Structors

        
        public AutomationTextBlock()
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overides

        
        /// <returns>The <see cref="System.Windows.Automation.Peers.AutomationPeer"/> implementations for this control.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutomationTextBlockAutomationPeer(this);
        }

        #endregion
    }
}
