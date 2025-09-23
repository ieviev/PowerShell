// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    internal sealed class SyntaxHelpInfo : BaseCommandHelpInfo
    {
        
        private SyntaxHelpInfo(string name, string text, HelpCategory category)
            : base(category)
        {
            FullHelp = PSObject.AsPSObject(text);
            Name = name;
            Synopsis = text;
        }

        
        internal override string Name { get; } = string.Empty;

        
        internal override string Synopsis { get; } = string.Empty;

        
        internal override PSObject FullHelp { get; }

        
        internal static SyntaxHelpInfo GetHelpInfo(string name, string text, HelpCategory category)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            SyntaxHelpInfo syntaxHelpInfo = new SyntaxHelpInfo(name, text, category);

            if (string.IsNullOrEmpty(syntaxHelpInfo.Name))
                return null;

            syntaxHelpInfo.AddCommonHelpProperties();

            return syntaxHelpInfo;
        }
    }
}
