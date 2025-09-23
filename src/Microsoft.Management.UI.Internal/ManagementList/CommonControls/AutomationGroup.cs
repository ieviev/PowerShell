// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class AutomationGroup : ContentControl
    {
        
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ExtendedFrameworkElementAutomationPeer(this, AutomationControlType.Group, true);
        }
    }
}
