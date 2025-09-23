// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    public class TraceCommandBase : PSCmdlet
    {
        
        internal Collection<PSTraceSource> GetMatchingTraceSource(
            string[] patternsToMatch,
            bool writeErrorIfMatchNotFound)
        {
            Collection<string> ignored = null;
            return GetMatchingTraceSource(patternsToMatch, writeErrorIfMatchNotFound, out ignored);
        }

        
        internal Collection<PSTraceSource> GetMatchingTraceSource(
            string[] patternsToMatch,
            bool writeErrorIfMatchNotFound,
            out Collection<string> notMatched)
        {
            notMatched = new Collection<string>();

            Collection<PSTraceSource> results = new();
            foreach (string patternToMatch in patternsToMatch)
            {
                bool matchFound = false;

                if (string.IsNullOrEmpty(patternToMatch))
                {
                    notMatched.Add(patternToMatch);
                    continue;
                }

                WildcardPattern pattern =
                    WildcardPattern.Get(
                        patternToMatch,
                        WildcardOptions.IgnoreCase);

                Dictionary<string, PSTraceSource> traceCatalog = PSTraceSource.TraceCatalog;

                foreach (PSTraceSource source in traceCatalog.Values)
                {
                    // Try matching by full name

                    if (pattern.IsMatch(source.FullName))
                    {
                        matchFound = true;
                        results.Add(source);
                    }
                    // Try matching by the short name.
                    else if (pattern.IsMatch(source.Name))
                    {
                        matchFound = true;
                        results.Add(source);
                    }
                }

                if (!matchFound)
                {
                    notMatched.Add(patternToMatch);

                    // Only write an error if no match was found, the pattern doesn't
                    // contain wildcard characters, and caller wants us to.

                    if (writeErrorIfMatchNotFound &&
                        !WildcardPattern.ContainsWildcardCharacters(patternToMatch))
                    {
                        ItemNotFoundException itemNotFound =
                            new(
                                patternToMatch,
                                "TraceSourceNotFound",
                                SessionStateStrings.TraceSourceNotFound);

                        ErrorRecord errorRecord = new(itemNotFound.ErrorRecord, itemNotFound);
                        WriteError(errorRecord);
                    }
                }
            }

            return results;
        }
    }
}
