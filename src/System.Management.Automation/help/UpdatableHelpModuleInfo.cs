// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;

namespace System.Management.Automation.Help
{
    
    internal class UpdatableHelpModuleInfo
    {
#if UNIX
        internal static readonly string HelpContentZipName = "HelpContent.zip";
#else
        internal static readonly string HelpContentZipName = "HelpContent.cab";
#endif
        internal static readonly string HelpIntoXmlName = "HelpInfo.xml";

        
        internal UpdatableHelpModuleInfo(string name, Guid guid, string path, string uri)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(!string.IsNullOrEmpty(uri));

            ModuleName = name;
            _moduleGuid = guid;
            ModuleBase = path;
            HelpInfoUri = uri;
        }

        
        internal string ModuleName { get; }

        
        internal Guid ModuleGuid
        {
            get
            {
                return _moduleGuid;
            }
        }

        private readonly Guid _moduleGuid;

        
        internal string ModuleBase { get; }

        
        internal string HelpInfoUri { get; }

        
        internal string GetHelpContentName(CultureInfo culture)
        {
            Debug.Assert(culture != null);

            return ModuleName + "_" + _moduleGuid.ToString() + "_" + culture.Name + "_" + HelpContentZipName;
        }

        
        internal string GetHelpInfoName()
        {
            return ModuleName + "_" + _moduleGuid.ToString() + "_" + HelpIntoXmlName;
        }
    }
}
