// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    
    internal sealed class NativeCommand : InternalCommand
    {
        private NativeCommandProcessor _myCommandProcessor;

        internal NativeCommandProcessor MyCommandProcessor
        {
            get { return _myCommandProcessor; }

            set { _myCommandProcessor = value; }
        }

        
        internal override void DoStopProcessing()
        {
            try
            {
                _myCommandProcessor?.StopProcessing();
            }
            catch (Exception)
            {
            }
        }
    }
}
