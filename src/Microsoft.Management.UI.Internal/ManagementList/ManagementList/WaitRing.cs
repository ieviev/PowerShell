// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Management.UI.Internal
{
    
    public class WaitRing : Control
    {
        
        static WaitRing()
        {
            // This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            // This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaitRing), new FrameworkPropertyMetadata(typeof(WaitRing)));
        }
    }
}
