// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    internal class ScriptCommandHelpProvider : CommandHelpProvider
    {
        
        internal ScriptCommandHelpProvider(HelpSystem helpSystem)
            : base(helpSystem)
        {
        }

        #region Overrides

        
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

        
        internal override CommandSearcher GetCommandSearcherForExactMatch(string commandName, ExecutionContext context)
        {
            CommandSearcher searcher = new CommandSearcher(
                commandName,
                SearchResolutionOptions.None,
                CommandTypes.Filter | CommandTypes.Function | CommandTypes.ExternalScript | CommandTypes.Configuration,
                context);

            return searcher;
        }

        
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
