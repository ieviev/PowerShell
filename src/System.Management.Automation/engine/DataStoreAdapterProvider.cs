// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Threading;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public class ProviderInfo
    {
        
        public Type ImplementingType { get; }

        
        public string HelpFile { get; } = string.Empty;

        
        private readonly SessionState _sessionState;

        private string _fullName;
        private string _cachedModuleName;

        
        public string Name { get; }

        
        internal string FullName
        {
            get
            {
                static string GetFullName(string name, string psSnapInName, string moduleName)
                {
                    string result = name;
                    if (!string.IsNullOrEmpty(psSnapInName))
                    {
                        result =
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                "{0}\\{1}",
                                psSnapInName,
                                name);
                    }

                    // After converting core snapins to load as modules, the providers will have Module property populated
                    else if (!string.IsNullOrEmpty(moduleName))
                    {
                        result =
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                "{0}\\{1}",
                                moduleName,
                                name);
                    }

                    return result;
                }

                if (_fullName != null && ModuleName.Equals(_cachedModuleName, StringComparison.Ordinal))
                {
                    return _fullName;
                }

                _cachedModuleName = ModuleName;
                return _fullName = GetFullName(Name, PSSnapInName, ModuleName);
            }
        }

        
        public PSSnapInInfo PSSnapIn { get; }

        
        internal string PSSnapInName
        {
            get
            {
                string result = null;
                if (PSSnapIn != null)
                {
                    result = PSSnapIn.Name;
                }

                return result;
            }
        }

        internal string ApplicationBase
        {
            get
            {
                string psHome = null;
                try
                {
                    psHome = Utils.DefaultPowerShellAppBase;
                }
                catch (System.Security.SecurityException)
                {
                    psHome = null;
                }

                return psHome;
            }
        }

        
        public string ModuleName
        {
            get
            {
                if (PSSnapIn != null)
                    return PSSnapIn.Name;
                if (Module != null)
                    return Module.Name;
                return string.Empty;
            }
        }

        
        public PSModuleInfo Module { get; private set; }

        internal void SetModule(PSModuleInfo module)
        {
            Module = module;
            _fullName = null;
        }

        
        public string Description { get; set; }

        
        public Provider.ProviderCapabilities Capabilities
        {
            get
            {
                if (!_capabilitiesRead)
                {
                    try
                    {
                        // Get the CmdletProvider declaration attribute

                        Type providerType = this.ImplementingType;

                        var attrs = providerType.GetCustomAttributes<CmdletProviderAttribute>(false);
                        var cmdletProviderAttributes = attrs as CmdletProviderAttribute[] ?? attrs.ToArray();

                        if (cmdletProviderAttributes.Length == 1)
                        {
                            _capabilities = cmdletProviderAttributes[0].ProviderCapabilities;
                            _capabilitiesRead = true;
                        }
                    }
                    catch (Exception) // Catch-all OK, 3rd party callout
                    {
                        // Assume no capabilities for now
                    }
                }

                return _capabilities;
            }
        }

        private ProviderCapabilities _capabilities = ProviderCapabilities.None;
        private bool _capabilitiesRead;

        
        public string Home { get; set; }

        
        public Collection<PSDriveInfo> Drives
        {
            get
            {
                return _sessionState.Drive.GetAllForProvider(FullName);
            }
        }

        
        private readonly PSDriveInfo _hiddenDrive;

        
        internal PSDriveInfo HiddenDrive
        {
            get
            {
                return _hiddenDrive;
            }
        }

        
        public override string ToString()
        {
            return FullName;
        }

#if USE_TLS
        
        private LocalDataStoreSlot instance =
            Thread.AllocateDataSlot();
#endif

        
        public bool VolumeSeparatedByColon { get; internal set; } = true;

        
        public char ItemSeparator { get; private set; }

        
        public char AltItemSeparator { get; private set; }

        
        protected ProviderInfo(ProviderInfo providerInfo)
        {
            if (providerInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(providerInfo));
            }

            Name = providerInfo.Name;
            ImplementingType = providerInfo.ImplementingType;
            _capabilities = providerInfo._capabilities;
            Description = providerInfo.Description;
            _hiddenDrive = providerInfo._hiddenDrive;
            Home = providerInfo.Home;
            HelpFile = providerInfo.HelpFile;
            PSSnapIn = providerInfo.PSSnapIn;
            _sessionState = providerInfo._sessionState;
            VolumeSeparatedByColon = providerInfo.VolumeSeparatedByColon;
            ItemSeparator = providerInfo.ItemSeparator;
            AltItemSeparator = providerInfo.AltItemSeparator;
        }

        
        internal ProviderInfo(
            SessionState sessionState,
            Type implementingType,
            string name,
            string helpFile,
            PSSnapInInfo psSnapIn)
            : this(sessionState, implementingType, name, string.Empty, string.Empty, helpFile, psSnapIn)
        {
        }

        
        internal ProviderInfo(
            SessionState sessionState,
            Type implementingType,
            string name,
            string description,
            string home,
            string helpFile,
            PSSnapInInfo psSnapIn)
        {
            // Verify parameters
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            if (implementingType == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(implementingType));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            _sessionState = sessionState;

            Name = name;
            Description = description;
            Home = home;
            ImplementingType = implementingType;
            HelpFile = helpFile;
            PSSnapIn = psSnapIn;

            // Create the hidden drive. The name doesn't really
            // matter since we are not adding this drive to a scope.

            _hiddenDrive =
                new PSDriveInfo(
                    this.FullName,
                    this,
                    string.Empty,
                    string.Empty,
                    null);

            _hiddenDrive.Hidden = true;

            // TODO:PSL
            // this is probably not right here
            if (implementingType == typeof(Microsoft.PowerShell.Commands.FileSystemProvider) && !Platform.IsWindows)
            {
                VolumeSeparatedByColon = false;
            }
        }

        
        internal bool NameEquals(string providerName)
        {
            PSSnapinQualifiedName qualifiedProviderName = PSSnapinQualifiedName.GetInstance(providerName);

            bool result = false;
            if (qualifiedProviderName != null)
            {
                // If the pssnapin name and provider name are specified, then both must match
                do // false loop
                {
                    if (!string.IsNullOrEmpty(qualifiedProviderName.PSSnapInName))
                    {
                        // After converting core snapins to load as modules, the providers will have Module property populated
                        if (!string.Equals(qualifiedProviderName.PSSnapInName, this.PSSnapInName, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(qualifiedProviderName.PSSnapInName, this.ModuleName, StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    }

                    result = string.Equals(qualifiedProviderName.ShortName, this.Name, StringComparison.OrdinalIgnoreCase);
                } while (false);
            }
            else
            {
                // If only the provider name is specified, then only the name must match
                result = string.Equals(providerName, Name, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }

        internal bool IsMatch(string providerName)
        {
            PSSnapinQualifiedName psSnapinQualifiedName = PSSnapinQualifiedName.GetInstance(providerName);

            WildcardPattern namePattern = null;

            if (psSnapinQualifiedName != null && WildcardPattern.ContainsWildcardCharacters(psSnapinQualifiedName.ShortName))
            {
                namePattern = WildcardPattern.Get(psSnapinQualifiedName.ShortName, WildcardOptions.IgnoreCase);
            }

            return IsMatch(namePattern, psSnapinQualifiedName);
        }

        internal bool IsMatch(WildcardPattern namePattern, PSSnapinQualifiedName psSnapinQualifiedName)
        {
            bool result = false;

            if (psSnapinQualifiedName == null)
            {
                result = true;
            }
            else
            {
                if (namePattern == null)
                {
                    if (string.Equals(Name, psSnapinQualifiedName.ShortName, StringComparison.OrdinalIgnoreCase) &&
                        IsPSSnapinNameMatch(psSnapinQualifiedName))
                    {
                        result = true;
                    }
                }
                else if (namePattern.IsMatch(Name) && IsPSSnapinNameMatch(psSnapinQualifiedName))
                {
                    result = true;
                }
            }

            return result;
        }

        private bool IsPSSnapinNameMatch(PSSnapinQualifiedName psSnapinQualifiedName)
        {
            bool result = false;

            if (string.IsNullOrEmpty(psSnapinQualifiedName.PSSnapInName) ||
                string.Equals(psSnapinQualifiedName.PSSnapInName, PSSnapInName, StringComparison.OrdinalIgnoreCase))
            {
                result = true;
            }

            return result;
        }

        
        internal Provider.CmdletProvider CreateInstance()
        {
            // It doesn't really seem that using thread local storage to store an
            // instance of the provider is really much of a performance gain and it
            // still causes problems with the CmdletProviderContext when piping two
            // commands together that use the same provider.
            // get-child -filter a*.txt | get-content
            // This pipeline causes problems when using a cached provider instance because
            // the CmdletProviderContext gets changed when get-content gets called.
            // When get-content finishes writing content from the first output of get-child
            // get-child gets control back and writes out a FileInfo but the WriteObject
            // from get-content gets used because the CmdletProviderContext is still from
            // that cmdlet.
            // Possible solutions are to not cache the provider instance, or to maintain
            // a CmdletProviderContext stack in ProviderBase.  Each method invocation pushes
            // the current context and the last action of the method pops back to the
            // previous context.
#if USE_TLS
            // Next see if we already have an instance in thread local storage

            object providerInstance = Thread.GetData(instance);

            if (providerInstance == null)
            {
#else
            object providerInstance = null;
#endif
            // Finally create an instance of the class
            Exception invocationException = null;

            try
            {
                providerInstance =
                    Activator.CreateInstance(this.ImplementingType);
            }
            catch (TargetInvocationException targetException)
            {
                invocationException = targetException.InnerException;
            }
            catch (MissingMethodException)
            {
            }
            catch (MemberAccessException)
            {
            }
            catch (ArgumentException)
            {
            }
#if USE_TLS
                // cache the instance in thread local storage

                Thread.SetData(instance, providerInstance);
            }
#endif

            if (providerInstance == null)
            {
                ProviderNotFoundException e = null;

                if (invocationException != null)
                {
                    e =
                        new ProviderNotFoundException(
                            this.Name,
                            SessionStateCategory.CmdletProvider,
                            "ProviderCtorException",
                            SessionStateStrings.ProviderCtorException,
                            invocationException.Message);
                }
                else
                {
                    e =
                        new ProviderNotFoundException(
                            this.Name,
                            SessionStateCategory.CmdletProvider,
                            "ProviderNotFoundInAssembly",
                            SessionStateStrings.ProviderNotFoundInAssembly);
                }

                throw e;
            }

            Provider.CmdletProvider result = providerInstance as Provider.CmdletProvider;
            ItemSeparator = result.ItemSeparator;
            AltItemSeparator = result.AltItemSeparator;

            Dbg.Diagnostics.Assert(
                result != null,
                "DiscoverProvider should verify that the class is derived from CmdletProvider so this is just validation of that");

            result.SetProviderInformation(this);
            return result;
        }

        
        internal void GetOutputTypes(string cmdletname, List<PSTypeName> listToAppend)
        {
            if (_providerOutputType == null)
            {
                _providerOutputType = new Dictionary<string, List<PSTypeName>>();
                foreach (OutputTypeAttribute outputType in ImplementingType.GetCustomAttributes<OutputTypeAttribute>(false))
                {
                    if (string.IsNullOrEmpty(outputType.ProviderCmdlet))
                    {
                        continue;
                    }

                    List<PSTypeName> l;
                    if (!_providerOutputType.TryGetValue(outputType.ProviderCmdlet, out l))
                    {
                        l = new List<PSTypeName>();
                        _providerOutputType[outputType.ProviderCmdlet] = l;
                    }

                    l.AddRange(outputType.Type);
                }
            }

            List<PSTypeName> cmdletOutputType = null;
            if (_providerOutputType.TryGetValue(cmdletname, out cmdletOutputType))
            {
                listToAppend.AddRange(cmdletOutputType);
            }
        }

        private Dictionary<string, List<PSTypeName>> _providerOutputType;

        private PSNoteProperty _noteProperty;

        internal PSNoteProperty GetNotePropertyForProviderCmdlets(string name)
        {
            if (_noteProperty == null)
            {
                Interlocked.CompareExchange(ref _noteProperty,
                                            new PSNoteProperty(name, this), null);
            }

            return _noteProperty;
        }
    }
}
