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

        
        public AutomationImageAutomationPeer(Image owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        
        protected override bool IsControlElementCore()
        {
            return false;
        }

        #endregion
    }
}
