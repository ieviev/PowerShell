// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace System.Management.Automation
{
    
    internal sealed class HelpFileHelpInfo : HelpInfo
    {
        
        private HelpFileHelpInfo(string name, string text, string filename)
        {
            FullHelp = PSObject.AsPSObject(text);

            Name = name;

            // Take the 5th line as synopsis. This may not be true if
            // format of help file is changed later on.
            _synopsis = GetLine(text, 5);
            if (_synopsis != null)
            {
                _synopsis = _synopsis.Trim();
            }
            else
            {
                // make sure _synopsis is never null
                _synopsis = string.Empty;
            }

            _filename = filename;
        }

        
        internal override string Name { get; } = string.Empty;

        private readonly string _filename = string.Empty;
        private readonly string _synopsis = string.Empty;
        
        internal override string Synopsis
        {
            get
            {
                return _synopsis;
            }
        }

        
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.HelpFile;
            }
        }

        
        internal override PSObject FullHelp { get; }

        
        internal static HelpFileHelpInfo GetHelpInfo(string name, string text, string filename)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            HelpFileHelpInfo helpfileHelpInfo = new HelpFileHelpInfo(name, text, filename);

            if (string.IsNullOrEmpty(helpfileHelpInfo.Name))
                return null;

            helpfileHelpInfo.AddCommonHelpProperties();

            return helpfileHelpInfo;
        }

        
        private static string GetLine(string text, int line)
        {
            StringReader reader = new StringReader(text);

            string result = null;

            for (int i = 0; i < line; i++)
            {
                result = reader.ReadLine();

                if (result == null)
                    return null;
            }

            return result;
        }

        internal override bool MatchPatternInContent(WildcardPattern pattern)
        {
            Diagnostics.Assert(pattern != null, "pattern cannot be null.");

            string helpContent = string.Empty;
            LanguagePrimitives.TryConvertTo<string>(FullHelp, out helpContent);
            return pattern.IsMatch(helpContent);
        }
    }
}
