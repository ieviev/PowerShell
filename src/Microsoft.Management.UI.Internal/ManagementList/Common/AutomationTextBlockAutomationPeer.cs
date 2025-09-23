// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal class AutomationTextBlockAutomationPeer : TextBlockAutomationPeer
    {
        #region Structors

        
        /// <param name="owner">The owner of the automation peer.</param>
        public AutomationTextBlockAutomationPeer(TextBlock owner)
            : base(owner)
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        
        /// <returns>This method always returns true.</returns>
        protected override bool IsControlElementCore()
        {
            return true;
        }

        
        /// <returns>The class name.</returns>
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }

        #endregion
    }
}
