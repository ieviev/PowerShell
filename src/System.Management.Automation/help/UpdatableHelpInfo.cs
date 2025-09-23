// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Internal;
using System.Text;

namespace System.Management.Automation.Help
{
    
    internal class CultureSpecificUpdatableHelp
    {
        
        internal CultureSpecificUpdatableHelp(CultureInfo culture, Version version)
        {
            Debug.Assert(version != null);
            Debug.Assert(culture != null);

            Culture = culture;
            Version = version;
        }

        
        internal Version Version { get; set; }

        
        internal CultureInfo Culture { get; set; }

        
        internal static IEnumerable<string> GetCultureFallbackChain(CultureInfo culture)
        {
            // We use just names instead because comparing two CultureInfo objects
            // can fail if they are created using different means
            while (culture != null)
            {
                if (string.IsNullOrEmpty(culture.Name))
                {
                    yield break;
                }

                yield return culture.Name;

                culture = culture.Parent;
            }
        }

        
        internal bool IsCultureSupported(string cultureName)
        {
            Debug.Assert(cultureName != null, $"{nameof(cultureName)} may not be null");
            return GetCultureFallbackChain(Culture).Any(fallback => fallback == cultureName);
        }
    }

    
    internal class UpdatableHelpInfo
    {
        
        internal UpdatableHelpInfo(string unresolvedUri, CultureSpecificUpdatableHelp[] cultures)
        {
            Debug.Assert(cultures != null);

            UnresolvedUri = unresolvedUri;
            HelpContentUriCollection = new Collection<UpdatableHelpUri>();
            UpdatableHelpItems = cultures;
        }

        
        internal string UnresolvedUri { get; }

        
        internal Collection<UpdatableHelpUri> HelpContentUriCollection { get; }

        
        internal CultureSpecificUpdatableHelp[] UpdatableHelpItems { get; }

        
        internal bool IsNewerVersion(UpdatableHelpInfo helpInfo, CultureInfo culture)
        {
            Debug.Assert(helpInfo != null);

            Version v1 = helpInfo.GetCultureVersion(culture);
            Version v2 = GetCultureVersion(culture);

            Debug.Assert(v1 != null);

            if (v2 == null)
            {
                return true;
            }

            return v1 > v2;
        }

        
        internal bool IsCultureSupported(string cultureName)
        {
            Debug.Assert(cultureName != null, $"{nameof(cultureName)} may not be null");
            return UpdatableHelpItems.Any(item => item.IsCultureSupported(cultureName));
        }

        
        internal string GetSupportedCultures()
        {
            if (UpdatableHelpItems.Length == 0)
            {
                return StringUtil.Format(HelpDisplayStrings.None);
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < UpdatableHelpItems.Length; i++)
            {
                sb.Append(UpdatableHelpItems[i].Culture.Name);

                if (i != (UpdatableHelpItems.Length - 1))
                {
                    sb.Append(" | ");
                }
            }

            return sb.ToString();
        }

        
        internal Version GetCultureVersion(CultureInfo culture)
        {
            foreach (CultureSpecificUpdatableHelp updatableHelpItem in UpdatableHelpItems)
            {
                if (string.Equals(updatableHelpItem.Culture.Name, culture.Name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return updatableHelpItem.Version;
                }
            }

            return null;
        }
    }
}
