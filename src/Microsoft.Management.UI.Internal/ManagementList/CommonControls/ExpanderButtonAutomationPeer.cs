// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class ExpanderButtonAutomationPeer : ToggleButtonAutomationPeer, IExpandCollapseProvider
    {
        #region Fields

        private ExpanderButton expanderButton;

        #endregion

        #region Structors

        
        public ExpanderButtonAutomationPeer(ExpanderButton owner)
            : base(owner)
        {
            this.expanderButton = owner;
        }

        #endregion

        #region Overrides

        
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }

        
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse ||
                patternInterface == PatternInterface.Toggle)
            {
                return this;
            }

            return null;
        }

        #endregion

        #region IExpandCollapseProvider Implementations

        
        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                if (this.expanderButton.IsChecked == true)
                {
                    return ExpandCollapseState.Expanded;
                }
                else
                {
                    return ExpandCollapseState.Collapsed;
                }
            }
        }

        
        void IExpandCollapseProvider.Expand()
        {
            if (!this.IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            this.expanderButton.IsChecked = true;
        }

        
        void IExpandCollapseProvider.Collapse()
        {
            if (!this.IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            this.expanderButton.IsChecked = false;
        }

        #endregion
    }
}
