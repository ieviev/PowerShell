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

        
        /// <param name="owner">The owner of the automation peer.</param>
        public ExpanderButtonAutomationPeer(ExpanderButton owner)
            : base(owner)
        {
            this.expanderButton = owner;
        }

        #endregion

        #region Overrides

        
        /// <returns>The class name.</returns>
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }

        
        /// <param name="patternInterface">Specifies the control pattern that is returned.</param>
        /// <returns>The control pattern for the <see cref="ExpanderButton"/> that is associated with this <see cref="ExpanderButtonAutomationPeer"/>.</returns>
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
