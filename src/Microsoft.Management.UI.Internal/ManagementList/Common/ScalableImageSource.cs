// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Microsoft.Management.UI.Internal
{
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class ScalableImageSource : Freezable
    {
        #region Structors

        
        public ScalableImageSource()
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        
        protected override Freezable CreateInstanceCore()
        {
            return new ScalableImageSource();
        }

        #endregion Overrides
    }
}
