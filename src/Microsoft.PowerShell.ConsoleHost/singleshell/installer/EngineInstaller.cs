// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Reflection;

namespace Microsoft.PowerShell
{
    
    [RunInstaller(true)]
    public sealed class EngineInstaller : PSInstaller
    {
        
        public EngineInstaller()
            : base()
        {
        }

        
        internal sealed override string RegKey
        {
            get
            {
                return RegistryStrings.MonadEngineKey;
            }
        }

        private static string EngineVersion
        {
            get
            {
                return PSVersionInfo.FeatureVersionString;
            }
        }

        private Dictionary<string, object> _regValues = null;
        
        internal sealed override Dictionary<string, object> RegValues
        {
            get
            {
                if (_regValues == null)
                {
                    _regValues = new Dictionary<string, object>();
                    _regValues[RegistryStrings.MonadEngine_MonadVersion] = EngineVersion;
                    _regValues[RegistryStrings.MonadEngine_ApplicationBase] = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    _regValues[RegistryStrings.MonadEngine_ConsoleHostAssemblyName] = Assembly.GetExecutingAssembly().FullName;
                    _regValues[RegistryStrings.MonadEngine_ConsoleHostModuleName] = Assembly.GetExecutingAssembly().Location;
                    _regValues[RegistryStrings.MonadEngine_RuntimeVersion] = Assembly.GetExecutingAssembly().ImageRuntimeVersion;
                }

                return _regValues;
            }
        }
    }
}
