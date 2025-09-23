// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation
{
    
    internal static class SessionStateConstants
    {
        
        internal const int DefaultVariableCapacity = 4096;

        
        internal const int MaxVariablesCapacity = 32768;

        
        internal const int MinVariablesCapacity = 1024;

        
        internal const int DefaultAliasCapacity = 4096;

        
        internal const int MaxAliasCapacity = 32768;

        
        internal const int MinAliasCapacity = 1024;

        
        internal const int DefaultFunctionCapacity = 4096;

        
        internal const int MaxFunctionCapacity = 32768;

        
        internal const int MinFunctionCapacity = 1024;

        
        internal const int DefaultDriveCapacity = 4096;

        
        internal const int MaxDriveCapacity = 32768;

        
        internal const int MinDriveCapacity = 1024;

        
        internal const int DefaultErrorCapacity = 256;

        
        internal const int MaxErrorCapacity = 32768;

        
        internal const int MinErrorCapacity = 256;

        
        internal const int DefaultDictionaryCapacity = 100;

        
        internal const float DefaultHashTableLoadFactor = 0.25F;
    }

    
    internal static class SessionStateUtilities
    {
        
        internal static Collection<T> ConvertArrayToCollection<T>(T[] array)
        {
            Collection<T> result = new Collection<T>();
            if (array != null)
            {
                foreach (T element in array)
                {
                    result.Add(element);
                }
            }

            return result;
        }

        
        internal static bool CollectionContainsValue(IEnumerable collection, object value, IComparer comparer)
        {
            ArgumentNullException.ThrowIfNull(collection);

            bool result = false;

            foreach (object item in collection)
            {
                if (comparer != null)
                {
                    if (comparer.Compare(item, value) == 0)
                    {
                        result = true;
                        break;
                    }
                }
                else
                {
                    if (item.Equals(value))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        
        internal static Collection<WildcardPattern> CreateWildcardsFromStrings(
            IEnumerable<string> globPatterns,
            WildcardOptions options)
        {
            Collection<WildcardPattern> result = new Collection<WildcardPattern>();

            if (globPatterns != null)
            {
                // Loop through the patterns and construct a wildcard pattern for each one

                foreach (string pattern in globPatterns)
                {
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        result.Add(
                            WildcardPattern.Get(
                                pattern,
                                options));
                    }
                }
            }

            return result;
        }

        
        internal static bool MatchesAnyWildcardPattern(
            string text,
            IEnumerable<WildcardPattern> patterns,
            bool defaultValue)
        {
            bool result = false;
            bool patternsNonEmpty = false;

            if (patterns != null)
            {
                // Loop through each of the patterns until a match is found
                foreach (WildcardPattern pattern in patterns)
                {
                    patternsNonEmpty = true;
                    if (pattern.IsMatch(text))
                    {
                        result = true;
                        break;
                    }
                }
            }

            if (!patternsNonEmpty)
            {
                // Since no pattern was specified return the default value
                result = defaultValue;
            }

            return result;
        }

        
        internal static FileMode GetFileModeFromOpenMode(OpenMode openMode)
        {
            FileMode result = FileMode.Create;

            switch (openMode)
            {
                case OpenMode.Add:
                    result = FileMode.Append;
                    break;

                case OpenMode.New:
                    result = FileMode.CreateNew;
                    break;

                case OpenMode.Overwrite:
                    result = FileMode.Create;
                    break;
            }

            return result;
        }
    }
}

namespace Microsoft.PowerShell.Commands
{
    
    public enum OpenMode
    {
        
        Add,

        
        New,

        
        Overwrite
    }
}
