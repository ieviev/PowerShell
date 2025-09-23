// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    /// <remarks>
    /// Command Help information are stored in 'help.xml' files. Location of these files
    /// can be found from through the engine execution context.
    /// </remarks>
    internal class ScriptCommandHelpProvider : CommandHelpProvider
    {
        
        internal ScriptCommandHelpProvider(HelpSystem helpSystem)
            : base(helpSystem)
        {
        }

        #region Overrides

        
        /// <value>Help category for this provider</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return
                    HelpCategory.ExternalScript |
                    HelpCategory.Filter |
                    HelpCategory.Function |
                    HelpCategory.Configuration |
                    HelpCategory.ScriptCommand;
            }
        }

        
        /// <param name="commandName"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override CommandSearcher GetCommandSearcherForExactMatch(string commandName, ExecutionContext context)
        {
            CommandSearcher searcher = new CommandSearcher(
                commandName,
                SearchResolutionOptions.None,
                CommandTypes.Filter | CommandTypes.Function | CommandTypes.ExternalScript | CommandTypes.Configuration,
                context);

            return searcher;
        }

        
        /// <param name="pattern"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override CommandSearcher GetCommandSearcherForSearch(string pattern, ExecutionContext context)
        {
            CommandSearcher searcher =
                    new CommandSearcher(
                        pattern,
                        SearchResolutionOptions.CommandNameIsPattern | SearchResolutionOptions.ResolveFunctionPatterns,
                        CommandTypes.Filter | CommandTypes.Function | CommandTypes.ExternalScript | CommandTypes.Configuration,
                        context);

            return searcher;
        }

        #endregion
    }
}
