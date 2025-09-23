// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;

using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    
    internal abstract class HelpProvider
    {
        
        internal HelpProvider(HelpSystem helpSystem)
        {
            _helpSystem = helpSystem;
        }

        private readonly HelpSystem _helpSystem;

        internal HelpSystem HelpSystem
        {
            get
            {
                return _helpSystem;
            }
        }

        #region Common Properties

        
        internal abstract string Name
        {
            get;
        }

        
        internal abstract HelpCategory HelpCategory
        {
            get;
        }

#if V2

        
        virtual internal string AssemblyName
        {
            get
            {
                return Assembly.GetExecutingAssembly().FullName;
            }
        }

        
        virtual internal string ClassName
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        
        internal PSObject ProviderInfo
        {
            get
            {
                PSObject result = new PSObject();
                result.Properties.Add(new PSNoteProperty("Name", this.Name));
                result.Properties.Add(new PSNoteProperty("Category", this.HelpCategory.ToString()));
                result.Properties.Add(new PSNoteProperty("ClassName", this.ClassName));
                result.Properties.Add(new PSNoteProperty("AssemblyName", this.AssemblyName));

                Collection<string> typeNames = new Collection<string>();
                typeNames.Add("HelpProviderInfo");
                result.TypeNames = typeNames;

                return result;
            }
        }

#endif

        #endregion

        #region Help Provider Interface

        
        internal abstract IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest);

        
        internal abstract IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent);

        
        internal virtual IEnumerable<HelpInfo> ProcessForwardedHelp(HelpInfo helpInfo, HelpRequest helpRequest)
        {
            // Win8: 508648. Remove the current provides category for resolving forward help as the current
            // help provider already process it.
            helpInfo.ForwardHelpCategory ^= this.HelpCategory;
            yield return helpInfo;
        }

        
        internal virtual void Reset()
        {
            return;
        }

        #endregion

        #region Utility functions

        
        internal void ReportHelpFileError(Exception exception, string target, string helpFile)
        {
            ErrorRecord errorRecord = new ErrorRecord(exception, "LoadHelpFileForTargetFailed", ErrorCategory.OpenError, null);
            errorRecord.ErrorDetails = new ErrorDetails(typeof(HelpProvider).Assembly, "HelpErrors", "LoadHelpFileForTargetFailed", target, helpFile, exception.Message);
            this.HelpSystem.LastErrors.Add(errorRecord);
            return;
        }

        
        internal string GetDefaultShellSearchPath()
        {
            string shellID = this.HelpSystem.ExecutionContext.ShellID;
            // Beginning in PowerShell 6.0.0.12, the $pshome is no longer registry specified, we search the application base instead.
            // We use executing assemblies location in case registry entry not found
            return Utils.GetApplicationBase(shellID) ?? Path.GetDirectoryName(Environment.ProcessPath);
        }

        
        internal Collection<string> GetSearchPaths()
        {
            Collection<string> searchPaths = this.HelpSystem.GetSearchPaths();

            Diagnostics.Assert(searchPaths != null,
                "HelpSystem returned an null search path");

            string defaultShellSearchPath = GetDefaultShellSearchPath();
            if (!searchPaths.Contains(defaultShellSearchPath))
            {
                searchPaths.Add(defaultShellSearchPath);
            }

            return searchPaths;
        }

        #endregion
    }
}
