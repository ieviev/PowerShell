// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    internal class HelpRequest
    {
        
        /// <param name="target"></param>
        /// <param name="helpCategory"></param>
        internal HelpRequest(string target, HelpCategory helpCategory)
        {
            Target = target;
            HelpCategory = helpCategory;
            CommandOrigin = CommandOrigin.Runspace;
        }

        
        /// <returns></returns>
        internal HelpRequest Clone()
        {
            HelpRequest helpRequest = new HelpRequest(this.Target, this.HelpCategory);

            helpRequest.Provider = this.Provider;
            helpRequest.MaxResults = this.MaxResults;
            helpRequest.Component = this.Component;
            helpRequest.Role = this.Role;
            helpRequest.Functionality = this.Functionality;
            helpRequest.ProviderContext = this.ProviderContext;
            helpRequest.CommandOrigin = CommandOrigin;

            return helpRequest;
        }

        
        internal ProviderContext ProviderContext { get; set; }

        
        /// <value></value>
        internal string Target { get; set; }

        
        /// <value></value>
        internal HelpCategory HelpCategory { get; set; } = HelpCategory.None;

        
        /// <value></value>
        internal string Provider { get; set; }

        
        /// <value></value>
        internal int MaxResults { get; set; } = -1;

        
        /// <value></value>
        internal string[] Component { get; set; }

        
        /// <value></value>
        internal string[] Role { get; set; }

        
        /// <value></value>
        internal string[] Functionality { get; set; }

        
        internal CommandOrigin CommandOrigin { get; set; }

        
        internal void Validate()
        {
            if (string.IsNullOrEmpty(Target)
                && HelpCategory == HelpCategory.None
                && string.IsNullOrEmpty(Provider)
                && Component == null
                && Role == null
                && Functionality == null
            )
            {
                Target = "default";
                HelpCategory = HelpCategory.DefaultHelp;
                return;
            }

            if (string.IsNullOrEmpty(Target))
            {
                if (!string.IsNullOrEmpty(Provider) &&
                    (HelpCategory == HelpCategory.None || HelpCategory == HelpCategory.Provider)
                )
                {
                    Target = Provider;
                }
                else
                {
                    Target = "*";
                }
            }

            // if either of component/role/functionality is specified then look in the
            // following help categories
            if ((!(Component == null && Role == null && Functionality == null)) &&
                (HelpCategory == HelpCategory.None))
            {
                HelpCategory = HelpCategory.Alias | HelpCategory.Cmdlet | HelpCategory.Function | HelpCategory.Filter | HelpCategory.ExternalScript | HelpCategory.ScriptCommand;

                return;
            }

            if ((HelpCategory & HelpCategory.Cmdlet) > 0)
            {
                HelpCategory |= HelpCategory.Alias;
            }

            if (HelpCategory == HelpCategory.None)
            {
                HelpCategory = HelpCategory.All;
            }

            HelpCategory &= ~HelpCategory.DefaultHelp;

            return;
        }
    }
}
