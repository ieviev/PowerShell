// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{

    
    [Localizability(LocalizationCategory.None)]
    partial class TextBlockService
    {
        //
        // IsTextTrimmed dependency property
        //
        
        private static readonly DependencyPropertyKey IsTextTrimmedPropertyKey = DependencyProperty.RegisterAttachedReadOnly( "IsTextTrimmed", typeof(bool), typeof(TextBlockService), new PropertyMetadata( BooleanBoxes.FalseBox, IsTextTrimmedProperty_PropertyChanged) );
        
        public static readonly DependencyProperty IsTextTrimmedProperty = IsTextTrimmedPropertyKey.DependencyProperty;

        
        static public bool GetIsTextTrimmed(DependencyObject element)
        {
            return (bool) element.GetValue(IsTextTrimmedProperty);
        }

        
        static private void SetIsTextTrimmed(DependencyObject element, bool value)
        {
            element.SetValue(IsTextTrimmedPropertyKey,BooleanBoxes.Box(value));
        }

        static private void IsTextTrimmedProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            IsTextTrimmedProperty_PropertyChangedImplementation(o, e);
        }

        static partial void IsTextTrimmedProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

        //
        // IsTextTrimmedExternally dependency property
        //
        
        public static readonly DependencyProperty IsTextTrimmedExternallyProperty = DependencyProperty.RegisterAttached( "IsTextTrimmedExternally", typeof(bool), typeof(TextBlockService), new PropertyMetadata( BooleanBoxes.FalseBox, IsTextTrimmedExternallyProperty_PropertyChanged) );

        
        static public bool GetIsTextTrimmedExternally(DependencyObject element)
        {
            return (bool) element.GetValue(IsTextTrimmedExternallyProperty);
        }

        
        static public void SetIsTextTrimmedExternally(DependencyObject element, bool value)
        {
            element.SetValue(IsTextTrimmedExternallyProperty,BooleanBoxes.Box(value));
        }

        static private void IsTextTrimmedExternallyProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            IsTextTrimmedExternallyProperty_PropertyChangedImplementation(o, e);
        }

        static partial void IsTextTrimmedExternallyProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

        //
        // IsTextTrimmedMonitoringEnabled dependency property
        //
        
        public static readonly DependencyProperty IsTextTrimmedMonitoringEnabledProperty = DependencyProperty.RegisterAttached( "IsTextTrimmedMonitoringEnabled", typeof(bool), typeof(TextBlockService), new PropertyMetadata( BooleanBoxes.FalseBox, IsTextTrimmedMonitoringEnabledProperty_PropertyChanged) );

        
        static public bool GetIsTextTrimmedMonitoringEnabled(DependencyObject element)
        {
            return (bool) element.GetValue(IsTextTrimmedMonitoringEnabledProperty);
        }

        
        static public void SetIsTextTrimmedMonitoringEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsTextTrimmedMonitoringEnabledProperty,BooleanBoxes.Box(value));
        }

        static private void IsTextTrimmedMonitoringEnabledProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            IsTextTrimmedMonitoringEnabledProperty_PropertyChangedImplementation(o, e);
        }

        static partial void IsTextTrimmedMonitoringEnabledProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

        //
        // UntrimmedText dependency property
        //
        
        public static readonly DependencyProperty UntrimmedTextProperty = DependencyProperty.RegisterAttached( "UntrimmedText", typeof(string), typeof(TextBlockService), new PropertyMetadata( string.Empty, UntrimmedTextProperty_PropertyChanged) );

        
        static public string GetUntrimmedText(DependencyObject element)
        {
            return (string) element.GetValue(UntrimmedTextProperty);
        }

        
        static public void SetUntrimmedText(DependencyObject element, string value)
        {
            element.SetValue(UntrimmedTextProperty,value);
        }

        static private void UntrimmedTextProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            UntrimmedTextProperty_PropertyChangedImplementation(o, e);
        }

        static partial void UntrimmedTextProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

    }
}
#endregion
