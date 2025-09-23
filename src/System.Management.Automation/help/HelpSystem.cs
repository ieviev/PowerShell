// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation.Language;

using Microsoft.PowerShell.Commands;

using System.Management.Automation.Runspaces;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    
    internal class HelpSystem
    {
        
        internal HelpSystem(ExecutionContext context)
        {
            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException("ExecutionContext");
            }

            _executionContext = context;

            Initialize();
        }

        private readonly ExecutionContext _executionContext;

        
        internal ExecutionContext ExecutionContext
        {
            get
            {
                return _executionContext;
            }
        }

        #region Progress Callback

        internal event EventHandler<HelpProgressEventArgs> OnProgress;

        #endregion

        #region Initialization

        
        internal void Initialize()
        {
            _verboseHelpErrors = LanguagePrimitives.IsTrue(
                _executionContext.GetVariableValue(SpecialVariables.VerboseHelpErrorsVarPath, false));
            _helpErrorTracer = new HelpErrorTracer(this);

            InitializeHelpProviders();
        }

        #endregion Initialization

        #region Help API

        
        internal IEnumerable<HelpInfo> GetHelp(HelpRequest helpRequest)
        {
            if (helpRequest == null)
                return null;

            helpRequest.Validate();

            ValidateHelpCulture();

            return this.DoGetHelp(helpRequest);
        }

        #endregion Help API

        #region Error Handling

        private readonly Collection<ErrorRecord> _lastErrors = new Collection<ErrorRecord>();

        
        internal Collection<ErrorRecord> LastErrors
        {
            get
            {
                return _lastErrors;
            }
        }

        private HelpCategory _lastHelpCategory = HelpCategory.None;

        
        internal HelpCategory LastHelpCategory
        {
            get
            {
                return _lastHelpCategory;
            }
        }

        #endregion

        #region Configuration

        private bool _verboseHelpErrors = false;

        
        internal bool VerboseHelpErrors
        {
            get
            {
                return _verboseHelpErrors;
            }
        }

        #endregion

        #region Help Engine

        // Cache of search paths that are currently active.
        // This will save a lot time when help providers do their searching
        private Collection<string> _searchPaths = null;

        
        internal Collection<string> GetSearchPaths()
        {
            // return the cache if already present.
            if (_searchPaths != null)
            {
                return _searchPaths;
            }

            _searchPaths = new Collection<string>();

            // add loaded modules paths to the search path
            if (ExecutionContext.Modules != null)
            {
                foreach (PSModuleInfo loadedModule in ExecutionContext.Modules.ModuleTable.Values)
                {
                    if (!_searchPaths.Contains(loadedModule.ModuleBase))
                    {
                        _searchPaths.Add(loadedModule.ModuleBase);
                    }
                }
            }

            return _searchPaths;
        }

        
        private IEnumerable<HelpInfo> DoGetHelp(HelpRequest helpRequest)
        {
            _lastErrors.Clear();
            // Reset SearchPaths
            _searchPaths = null;

            _lastHelpCategory = helpRequest.HelpCategory;

            if (string.IsNullOrEmpty(helpRequest.Target))
            {
                HelpInfo helpInfo = GetDefaultHelp();

                if (helpInfo != null)
                {
                    yield return helpInfo;
                }

                yield return null;
            }
            else
            {
                bool isMatchFound = false;
                if (!WildcardPattern.ContainsWildcardCharacters(helpRequest.Target))
                {
                    foreach (HelpInfo helpInfo in ExactMatchHelp(helpRequest))
                    {
                        isMatchFound = true;
                        yield return helpInfo;
                    }
                }

                if (!isMatchFound)
                {
                    foreach (HelpInfo helpInfo in SearchHelp(helpRequest))
                    {
                        isMatchFound = true;
                        yield return helpInfo;
                    }

                    if (!isMatchFound)
                    {
                        // Throwing an exception here may not be the
                        // best thing to do. Instead we can choose to
                        //    a. give a hint
                        //    b. just silently return an empty search result.
                        // Solution:
                        //    If it is an exact help target, throw an exception.
                        //    Otherwise, return empty result set.
                        if (!WildcardPattern.ContainsWildcardCharacters(helpRequest.Target) && this.LastErrors.Count == 0)
                        {
                            Exception e = new HelpNotFoundException(helpRequest.Target);
                            ErrorRecord errorRecord = new ErrorRecord(e, "HelpNotFound", ErrorCategory.ResourceUnavailable, null);
                            this.LastErrors.Add(errorRecord);
                            yield break;
                        }
                    }
                }
            }
        }

        
        internal IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest)
        {
            bool isHelpInfoFound = false;
            for (int i = 0; i < this.HelpProviders.Count; i++)
            {
                HelpProvider helpProvider = (HelpProvider)this.HelpProviders[i];
                if ((helpProvider.HelpCategory & helpRequest.HelpCategory) > 0)
                {
                    foreach (HelpInfo helpInfo in helpProvider.ExactMatchHelp(helpRequest))
                    {
                        isHelpInfoFound = true;
                        foreach (HelpInfo fwdHelpInfo in ForwardHelp(helpInfo, helpRequest))
                        {
                            yield return fwdHelpInfo;
                        }
                    }
                }

                // Bug Win7 737383: Win7 RTM shows both function and cmdlet help when there is
                // function and cmdlet with the same name. So, ignoring the ScriptCommandHelpProvider's
                // results and going to the CommandHelpProvider for further evaluation.
                if (isHelpInfoFound && helpProvider is not ScriptCommandHelpProvider)
                {
                    // once helpInfo found from a provider..no need to traverse other providers.
                    yield break;
                }
            }
        }

        
        private IEnumerable<HelpInfo> ForwardHelp(HelpInfo helpInfo, HelpRequest helpRequest)
        {
            // findout if this helpInfo needs to be processed further..
            if (helpInfo.ForwardHelpCategory == HelpCategory.None && string.IsNullOrEmpty(helpInfo.ForwardTarget))
            {
                // this helpInfo is final...so store this in result
                // and move on..
                yield return helpInfo;
            }
            else
            {
                // Find out a capable provider to process this request...
                HelpCategory forwardHelpCategory = helpInfo.ForwardHelpCategory;
                bool isHelpInfoProcessed = false;
                for (int i = 0; i < this.HelpProviders.Count; i++)
                {
                    HelpProvider helpProvider = (HelpProvider)this.HelpProviders[i];
                    if ((helpProvider.HelpCategory & forwardHelpCategory) != HelpCategory.None)
                    {
                        isHelpInfoProcessed = true;
                        // If this help info is processed by this provider already, break
                        // out of the provider loop...
                        foreach (HelpInfo fwdResult in helpProvider.ProcessForwardedHelp(helpInfo, helpRequest))
                        {
                            // Add each helpinfo to our repository
                            foreach (HelpInfo fHelpInfo in ForwardHelp(fwdResult, helpRequest))
                            {
                                yield return fHelpInfo;
                            }

                            // get out of the provider loop..
                            yield break;
                        }
                    }
                }

                if (!isHelpInfoProcessed)
                {
                    // we are here because no help provider processed the helpinfo..
                    // so add this to our repository..
                    yield return helpInfo;
                }
            }
        }

        
        private HelpInfo GetDefaultHelp()
        {
            HelpRequest helpRequest = new HelpRequest("default", HelpCategory.DefaultHelp);
            foreach (HelpInfo helpInfo in ExactMatchHelp(helpRequest))
            {
                // return just the first helpInfo object
                return helpInfo;
            }

            return null;
        }

        
        private IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest)
        {
            int countOfHelpInfosFound = 0;
            bool searchInHelpContent = false;
            bool shouldBreak = false;

            HelpProgressEventArgs progress = new HelpProgressEventArgs();

            progress.Activity = StringUtil.Format(HelpDisplayStrings.SearchingForHelpContent, helpRequest.Target);
            progress.Completed = false;
            progress.PercentComplete = 0;

            try
            {
                OnProgress(this, progress);

                // algorithm:
                // 1. Search for pattern (helpRequest.Target) in command name
                // 2. If Step 1 fails then search for pattern in help content
                do
                {
                    // we should not continue the search loop if we are
                    // searching in the help content (as this is the last step
                    // in our search algorithm).
                    if (searchInHelpContent)
                    {
                        shouldBreak = true;
                    }

                    for (int i = 0; i < this.HelpProviders.Count; i++)
                    {
                        HelpProvider helpProvider = (HelpProvider)this.HelpProviders[i];
                        if ((helpProvider.HelpCategory & helpRequest.HelpCategory) > 0)
                        {
                            foreach (HelpInfo helpInfo in helpProvider.SearchHelp(helpRequest, searchInHelpContent))
                            {
                                if (_executionContext.CurrentPipelineStopping)
                                {
                                    yield break;
                                }

                                countOfHelpInfosFound++;
                                yield return helpInfo;

                                if ((countOfHelpInfosFound >= helpRequest.MaxResults) && (helpRequest.MaxResults > 0))
                                {
                                    yield break;
                                }
                            }
                        }
                    }

                    // no need to do help content search once we have some help topics
                    // with command name search.
                    if (countOfHelpInfosFound > 0)
                    {
                        yield break;
                    }

                    // appears that we did not find any help matching command names..look for
                    // pattern in help content.
                    searchInHelpContent = true;

                    if (this.HelpProviders.Count > 0)
                    {
                        progress.PercentComplete += (100 / this.HelpProviders.Count);
                        OnProgress(this, progress);
                    }
                } while (!shouldBreak);
            }
            finally
            {
                progress.Completed = true;
                progress.PercentComplete = 100;

                OnProgress(this, progress);
            }
        }

        #endregion Help Engine

        #region Help Provider Manager

        private readonly ArrayList _helpProviders = new ArrayList();

        
        internal ArrayList HelpProviders
        {
            get
            {
                return _helpProviders;
            }
        }

        
        private void InitializeHelpProviders()
        {
            HelpProvider helpProvider = null;

            helpProvider = new AliasHelpProvider(this);
            _helpProviders.Add(helpProvider);

            helpProvider = new ScriptCommandHelpProvider(this);
            _helpProviders.Add(helpProvider);

            helpProvider = new CommandHelpProvider(this);
            _helpProviders.Add(helpProvider);

            helpProvider = new ProviderHelpProvider(this);
            _helpProviders.Add(helpProvider);

            helpProvider = new PSClassHelpProvider(this);
            _helpProviders.Add(helpProvider);

            
            helpProvider = new HelpFileHelpProvider(this);
            _helpProviders.Add(helpProvider);

            helpProvider = new DefaultHelpProvider(this);
            _helpProviders.Add(helpProvider);
        }

#if _HelpProviderReflection

        // Eventually we will publicize the provider api and initialize
        // help providers using reflection. This is not in v1 right now.
        //
        private static HelpProviderInfo[] _providerInfos = new HelpProviderInfo[]
                            { new HelpProviderInfo(string.Empty, "AliasHelpProvider", HelpCategory.Alias),
                              new HelpProviderInfo(string.Empty, "CommandHelpProvider", HelpCategory.Command),
                              new HelpProviderInfo(string.Empty, "ProviderHelpProvider", HelpCategory.Provider),
                              new HelpProviderInfo(string.Empty, "OverviewHelpProvider", HelpCategory.Overview),
                              new HelpProviderInfo(string.Empty, "GeneralHelpProvider", HelpCategory.General),
                              new HelpProviderInfo(string.Empty, "FAQHelpProvider", HelpCategory.FAQ),
                              new HelpProviderInfo(string.Empty, "GlossaryHelpProvider", HelpCategory.Glossary),
                              new HelpProviderInfo(string.Empty, "HelpFileHelpProvider", HelpCategory.HelpFile),
                              new HelpProviderInfo(string.Empty, "DefaultHelpHelpProvider", HelpCategory.DefaultHelp)
                            };

        private void InitializeHelpProviders()
        {
            for (int i = 0; i < _providerInfos.Length; i++)
            {
                HelpProvider helpProvider = GetHelpProvider(_providerInfos[i]);

                if (helpProvider != null)
                {
                    helpProvider.Initialize(this._executionContext);
                    _helpProviders.Add(helpProvider);
                }
            }
        }

        private HelpProvider GetHelpProvider(HelpProviderInfo providerInfo)
        {
            Assembly providerAssembly = null;

            if (string.IsNullOrEmpty(providerInfo.AssemblyName))
            {
                providerAssembly = Assembly.GetExecutingAssembly();
            }
            else
            {
                providerAssembly = Assembly.Load(providerInfo.AssemblyName);
            }

            try
            {
                if (providerAssembly != null)
                {
                    HelpProvider helpProvider =
                        (HelpProvider)providerAssembly.CreateInstance(providerInfo.ClassName,
                                                                     false, // don't ignore case
                                                                     BindingFlags.CreateInstance,
                                                                     null, // use default binder
                                                                     null,
                                                                     null, // use current culture
                                                                     null // no special activation attributes
                                                                    );

                    return helpProvider;
                }
            }
            catch (TargetInvocationException e)
            {
                System.Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    System.Console.WriteLine(e.InnerException.Message);
                    System.Console.WriteLine(e.InnerException.StackTrace);
                }
            }

            return null;
        }

#endif

        #endregion Help Provider Manager

        #region Help Error Tracer

        private HelpErrorTracer _helpErrorTracer;

        
        internal HelpErrorTracer HelpErrorTracer
        {
            get
            {
                return _helpErrorTracer;
            }
        }

        
        internal IDisposable Trace(string helpFile)
        {
            if (_helpErrorTracer == null)
                return null;

            return _helpErrorTracer.Trace(helpFile);
        }

        
        internal void TraceError(ErrorRecord errorRecord)
        {
            if (_helpErrorTracer == null)
                return;

            _helpErrorTracer.TraceError(errorRecord);
        }

        
        internal void TraceErrors(Collection<ErrorRecord> errorRecords)
        {
            if (_helpErrorTracer == null || errorRecords == null)
                return;

            _helpErrorTracer.TraceErrors(errorRecords);
        }

        #endregion

        #region Help MUI

        private CultureInfo _culture;

        
        private void ValidateHelpCulture()
        {
            CultureInfo culture = CultureInfo.CurrentUICulture;

            if (_culture == null)
            {
                _culture = culture;
                return;
            }

            if (_culture.Equals(culture))
            {
                return;
            }

            _culture = culture;
            ResetHelpProviders();
        }

        
        internal void ResetHelpProviders()
        {
            if (_helpProviders == null)
                return;

            for (int i = 0; i < _helpProviders.Count; i++)
            {
                HelpProvider helpProvider = (HelpProvider)_helpProviders[i];

                helpProvider.Reset();
            }

            return;
        }

        #endregion

        #region ScriptBlock Parse Tokens Caching/Clearing Functionality

        private readonly Lazy<Dictionary<Ast, Token[]>> _scriptBlockTokenCache = new Lazy<Dictionary<Ast, Token[]>>(isThreadSafe: true);

        internal Dictionary<Ast, Token[]> ScriptBlockTokenCache
        {
            get { return _scriptBlockTokenCache.Value; }
        }

        internal void ClearScriptBlockTokenCache()
        {
            if (_scriptBlockTokenCache.IsValueCreated)
            {
                _scriptBlockTokenCache.Value.Clear();
            }
        }

        #endregion
    }

    
    internal class HelpProgressEventArgs : EventArgs
    {
        internal bool Completed { get; set; }

        internal string Activity { get; set; }

        internal int PercentComplete { get; set; }
    }

    
    internal class HelpProviderInfo
    {
        internal string AssemblyName = string.Empty;
        internal string ClassName = string.Empty;
        internal HelpCategory HelpCategory = HelpCategory.None;

        
        internal HelpProviderInfo(string assemblyName, string className, HelpCategory helpCategory)
        {
            this.AssemblyName = assemblyName;
            this.ClassName = className;
            this.HelpCategory = helpCategory;
        }
    }

    
    [Flags]
    internal enum HelpCategory
    {
        
        None = 0x00,

        
        Alias = 0x01,

        
        Cmdlet = 0x02,

        
        Provider = 0x04,

        
        General = 0x10,

        
        FAQ = 0x20,

        
        Glossary = 0x40,

        
        HelpFile = 0x80,

        
        ScriptCommand = 0x100,

        
        Function = 0x200,

        
        Filter = 0x400,

        
        ExternalScript = 0x800,

        
        All = 0xFFFFF,

        
        DefaultHelp = 0x1000,

        
        Configuration = 0x4000,

        
        DscResource = 0x8000,

        
        Class = 0x10000
    }
}
