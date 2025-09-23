// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis; // for fxcop

namespace System.Management.Automation
{
    
    internal sealed class AliasHelpInfo : HelpInfo
    {
        
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        private AliasHelpInfo(AliasInfo aliasInfo)
        {
            _fullHelpObject = new PSObject();

            string name = (aliasInfo.ResolvedCommand == null) ? aliasInfo.UnresolvedCommandName : aliasInfo.ResolvedCommand.Name;

            this.ForwardTarget = name;
            // A Cmdlet/Function/Script etc can have alias.
            this.ForwardHelpCategory = HelpCategory.Cmdlet |
                HelpCategory.Function | HelpCategory.ExternalScript | HelpCategory.ScriptCommand | HelpCategory.Filter;

            if (!string.IsNullOrEmpty(aliasInfo.Name))
            {
                Name = aliasInfo.Name.Trim();
            }

            if (!string.IsNullOrEmpty(name))
            {
                Synopsis = name.Trim();
            }

            _fullHelpObject.TypeNames.Clear();
            _fullHelpObject.TypeNames.Add(string.Create(Globalization.CultureInfo.InvariantCulture, $"AliasHelpInfo#{Name}"));
            _fullHelpObject.TypeNames.Add("AliasHelpInfo");
            _fullHelpObject.TypeNames.Add("HelpInfo");
        }

        
        internal override string Name { get; } = string.Empty;

        
        internal override string Synopsis { get; } = string.Empty;

        
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.Alias;
            }
        }

        private readonly PSObject _fullHelpObject;

        
        internal override PSObject FullHelp
        {
            get
            {
                return _fullHelpObject;
            }
        }

        
        internal static AliasHelpInfo GetHelpInfo(AliasInfo aliasInfo)
        {
            if (aliasInfo == null)
                return null;

            if (aliasInfo.ResolvedCommand == null && aliasInfo.UnresolvedCommandName == null)
                return null;

            AliasHelpInfo aliasHelpInfo = new AliasHelpInfo(aliasInfo);

            if (string.IsNullOrEmpty(aliasHelpInfo.Name))
                return null;

            aliasHelpInfo.AddCommonHelpProperties();

            return aliasHelpInfo;
        }
    }
}
