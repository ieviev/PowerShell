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

        
        /// <value>Name for the help provider</value>
        /// <remarks>Derived classes should set this.</remarks>
        internal abstract string Name
        {
            get;
        }

        
        /// <value>Help category for the help provider</value>
        /// <remarks>Derived classes should set this.</remarks>
        internal abstract HelpCategory HelpCategory
        {
            get;
        }

#if V2

        
        /// <value>Assembly name</value>
        virtual internal string AssemblyName
        {
            get
            {
                return Assembly.GetExecutingAssembly().FullName;
            }
        }

        
        /// <value>Class name</value>
        virtual internal string ClassName
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        
        /// <value>An mshObject that contains the providerInfo</value>
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

        
        /// <param name="helpRequest">Help request object.</param>
        /// <returns>List of HelpInfo objects retrieved.</returns>
        internal abstract IEnumerable<HelpInfo> ExactMatchHelp(HelpRequest helpRequest);

        
        /// <param name="helpRequest">Help request object.</param>
        /// <param name="searchOnlyContent">
        /// If true, searches for pattern in the help content. Individual
        /// provider can decide which content to search in.
        ///
        /// If false, searches for pattern in the command names.
        /// </param>
        /// <returns>A collection of help info objects.</returns>
        internal abstract IEnumerable<HelpInfo> SearchHelp(HelpRequest helpRequest, bool searchOnlyContent);

        
        /// <param name="helpInfo">HelpInfo passed over by another HelpProvider.</param>
        /// <param name="helpRequest">Help request object.</param>
        /// <returns></returns>
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

        
        /// <param name="exception"></param>
        /// <param name="target"></param>
        /// <param name="helpFile"></param>
        internal void ReportHelpFileError(Exception exception, string target, string helpFile)
        {
            ErrorRecord errorRecord = new ErrorRecord(exception, "LoadHelpFileForTargetFailed", ErrorCategory.OpenError, null);
            errorRecord.ErrorDetails = new ErrorDetails(typeof(HelpProvider).Assembly, "HelpErrors", "LoadHelpFileForTargetFailed", target, helpFile, exception.Message);
            this.HelpSystem.LastErrors.Add(errorRecord);
            return;
        }

        
        /// <returns>String representing base directory of the executing shell.</returns>
        internal string GetDefaultShellSearchPath()
        {
            string shellID = this.HelpSystem.ExecutionContext.ShellID;
            // Beginning in PowerShell 6.0.0.12, the $pshome is no longer registry specified, we search the application base instead.
            // We use executing assemblies location in case registry entry not found
            return Utils.GetApplicationBase(shellID) ?? Path.GetDirectoryName(Environment.ProcessPath);
        }

        
        /// <returns>A collection of string representing locations.</returns>
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
