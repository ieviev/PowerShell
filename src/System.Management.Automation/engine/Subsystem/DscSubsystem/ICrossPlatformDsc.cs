// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace System.Management.Automation.Subsystem.DSC
{
    
    public interface ICrossPlatformDsc : ISubsystem
    {
        
        Dictionary<string, string>? ISubsystem.FunctionsToDefine => null;

        
        void LoadDefaultKeywords(Collection<Exception> errors);

        
        void ClearCache();

        
        string GetDSCResourceUsageString(DynamicKeyword keyword);

        
        bool IsSystemResourceName(string name);

        
        bool IsDefaultModuleNameForMetaConfigResource(string name);
    }
}
