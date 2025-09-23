// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [Description("Provides a System.Windows.Controls.Image control that is always visible in the automation tree.")]
    public class AutomationImage : Image
    {
        #region Constructors

        
        public AutomationImage()
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overides

        
        /// <returns>The <see cref="System.Windows.Automation.Peers.AutomationPeer"/> implementations for this control.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutomationImageAutomationPeer(this);
        }

        #endregion
    }

    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal class AutomationImageAutomationPeer : ImageAutomationPeer
    {
        #region Constructors

        
        /// <param name="owner">The owner of the automation peer.</param>
        public AutomationImageAutomationPeer(Image owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        
        /// <returns>This method always returns false.</returns>
        protected override bool IsControlElementCore()
        {
            return false;
        }

        #endregion
    }
}
