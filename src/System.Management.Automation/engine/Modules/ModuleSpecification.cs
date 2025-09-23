// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Text;

using Dbg = System.Management.Automation.Diagnostics;

//
// Now define the set of commands for manipulating modules.
//

namespace Microsoft.PowerShell.Commands
{
    #region Module Specification class

    
    public class ModuleSpecification
    {
        
        public ModuleSpecification()
        {
        }

        
        public ModuleSpecification(string moduleName)
        {
            ArgumentException.ThrowIfNullOrEmpty(moduleName);

            this.Name = moduleName;

            // Alias name of miniumVersion
            this.Version = null;
            this.RequiredVersion = null;
            this.MaximumVersion = null;
            this.Guid = null;
        }

        
        public ModuleSpecification(Hashtable moduleSpecification)
        {
            ArgumentNullException.ThrowIfNull(moduleSpecification);

            var exception = ModuleSpecificationInitHelper(this, moduleSpecification);
            if (exception != null)
            {
                throw exception;
            }
        }

        
        internal static Exception ModuleSpecificationInitHelper(ModuleSpecification moduleSpecification, Hashtable hashtable)
        {
            StringBuilder badKeys = new StringBuilder();
            try
            {
                foreach (DictionaryEntry entry in hashtable)
                {
                    string field = entry.Key.ToString();

                    if (field.Equals("ModuleName", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.Name = LanguagePrimitives.ConvertTo<string>(entry.Value);
                    }
                    else if (field.Equals("ModuleVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.Version = LanguagePrimitives.ConvertTo<Version>(entry.Value);
                    }
                    else if (field.Equals("RequiredVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.RequiredVersion = LanguagePrimitives.ConvertTo<Version>(entry.Value);
                    }
                    else if (field.Equals("MaximumVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.MaximumVersion = LanguagePrimitives.ConvertTo<string>(entry.Value);
                        ModuleCmdletBase.GetMaximumVersion(moduleSpecification.MaximumVersion);
                    }
                    else if (field.Equals("GUID", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.Guid = LanguagePrimitives.ConvertTo<Guid?>(entry.Value);
                    }
                    else
                    {
                        if (badKeys.Length > 0)
                        {
                            badKeys.Append(", ");
                        }

                        badKeys.Append('\'');
                        badKeys.Append(entry.Key.ToString());
                        badKeys.Append('\'');
                    }
                }
            }
            // catch all exceptions here, we are going to report them via return value.
            // Example of caught exception: one of conversions to Version failed.
            catch (Exception e)
            {
                return e;
            }

            string message;
            if (badKeys.Length != 0)
            {
                message = StringUtil.Format(Modules.InvalidModuleSpecificationMember, "ModuleName, ModuleVersion, RequiredVersion, GUID", badKeys);
                return new ArgumentException(message);
            }

            if (string.IsNullOrEmpty(moduleSpecification.Name))
            {
                message = StringUtil.Format(Modules.RequiredModuleMissingModuleName);
                return new MissingMemberException(message);
            }

            if (moduleSpecification.RequiredVersion == null && moduleSpecification.Version == null && moduleSpecification.MaximumVersion == null)
            {
                message = StringUtil.Format(Modules.RequiredModuleMissingModuleVersion);
                return new MissingMemberException(message);
            }

            if (moduleSpecification.RequiredVersion != null && moduleSpecification.Version != null)
            {
                message = StringUtil.Format(SessionStateStrings.GetContent_TailAndHeadCannotCoexist, "ModuleVersion", "RequiredVersion");
                return new ArgumentException(message);
            }

            if (moduleSpecification.RequiredVersion != null && moduleSpecification.MaximumVersion != null)
            {
                message = StringUtil.Format(SessionStateStrings.GetContent_TailAndHeadCannotCoexist, "MaximumVersion", "RequiredVersion");
                return new ArgumentException(message);
            }

            return null;
        }

        internal string GetRequiredModuleNotFoundVersionMessage()
        {
            if (RequiredVersion is not null)
            {
                return StringUtil.Format(
                    Modules.RequiredModuleNotFoundRequiredVersion,
                    Name,
                    RequiredVersion);
            }

            bool hasVersion = Version is not null;
            bool hasMaximumVersion = MaximumVersion is not null;

            if (hasVersion && hasMaximumVersion)
            {
                return StringUtil.Format(
                    Modules.RequiredModuleNotFoundModuleAndMaximumVersion,
                    Name,
                    Version,
                    MaximumVersion);
            }

            if (hasVersion)
            {
                return StringUtil.Format(
                    Modules.RequiredModuleNotFoundModuleVersion,
                    Name,
                    Version);
            }

            if (hasMaximumVersion)
            {
                return StringUtil.Format(
                    Modules.RequiredModuleNotFoundMaximumVersion,
                    Name,
                    MaximumVersion);
            }

            return StringUtil.Format(
                Modules.RequiredModuleNotFoundWithoutVersion,
                Name);
        }

        internal ModuleSpecification(PSModuleInfo moduleInfo)
        {
            ArgumentNullException.ThrowIfNull(moduleInfo);

            this.Name = moduleInfo.Name;
            this.Version = moduleInfo.Version;
            this.Guid = moduleInfo.Guid;
        }

        
        public override string ToString()
        {
            if (Guid == null && Version == null && RequiredVersion == null && MaximumVersion == null)
            {
                return Name;
            }

            var moduleSpecBuilder = new StringBuilder();

            moduleSpecBuilder.Append("@{ ModuleName = '").Append(Name).Append('\'');

            if (Guid != null)
            {
                moduleSpecBuilder.Append("; Guid = '{").Append(Guid).Append("}' ");
            }

            if (RequiredVersion != null)
            {
                moduleSpecBuilder.Append("; RequiredVersion = '").Append(RequiredVersion).Append('\'');
            }
            else
            {
                if (Version != null)
                {
                    moduleSpecBuilder.Append("; ModuleVersion = '").Append(Version).Append('\'');
                }

                if (MaximumVersion != null)
                {
                    moduleSpecBuilder.Append("; MaximumVersion = '").Append(MaximumVersion).Append('\'');
                }
            }

            moduleSpecBuilder.Append(" }");

            return moduleSpecBuilder.ToString();
        }

        
        public static bool TryParse(string input, out ModuleSpecification result)
        {
            result = null;
            try
            {
                Hashtable hashtable;
                if (Parser.TryParseAsConstantHashtable(input, out hashtable))
                {
                    result = new ModuleSpecification(hashtable);
                    return true;
                }
            }
            catch
            {
                // Ignoring the exceptions to return false
            }

            return false;
        }

        
        internal ModuleSpecification WithNormalizedName(ExecutionContext context, string basePath)
        {
            // Save allocating a new module spec if we don't need to change anything
            if (!ModuleIntrinsics.IsModuleNamePath(Name))
            {
                return this;
            }

            return new ModuleSpecification()
            {
                Guid = Guid,
                MaximumVersion = MaximumVersion,
                Version = Version,
                RequiredVersion = RequiredVersion,
                Name = ModuleIntrinsics.NormalizeModuleName(Name, basePath, context)
            };
        }

        
        public string Name { get; internal set; }

        
        public Guid? Guid { get; internal set; }

        
        public Version Version { get; internal set; }

        
        public string MaximumVersion { get; internal set; }

        
        public Version RequiredVersion { get; internal set; }
    }

    
    internal class ModuleSpecificationComparer : IEqualityComparer<ModuleSpecification>
    {
        
        public bool Equals(ModuleSpecification x, ModuleSpecification y)
        {
            if (x == y)
            {
                return true;
            }

            return x != null && y != null
                && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                && Guid.Equals(x.Guid, y.Guid)
                && Version.Equals(x.RequiredVersion, y.RequiredVersion)
                && Version.Equals(x.Version, y.Version)
                && string.Equals(x.MaximumVersion, y.MaximumVersion);
        }

        
        public int GetHashCode(ModuleSpecification obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return HashCode.Combine(obj.Name, obj.Guid, obj.RequiredVersion, obj.Version, obj.MaximumVersion);
        }
    }

    #endregion
}
