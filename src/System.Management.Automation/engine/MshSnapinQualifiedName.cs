// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    
    internal sealed class PSSnapinQualifiedName
    {
        private PSSnapinQualifiedName(string[] splitName)
        {
            Dbg.Assert(splitName != null, "splitName should not be null");
            Dbg.Assert(splitName.Length == 1 || splitName.Length == 2, "splitName should contain 1 or 2 elements");

            if (splitName.Length == 1)
            {
                _shortName = splitName[0];
            }
            else if (splitName.Length == 2)
            {
                if (!string.IsNullOrEmpty(splitName[0]))
                {
                    _psSnapinName = splitName[0];
                }

                _shortName = splitName[1];
            }
            else
            {
                // Since the provider name contained multiple slashes it is
                // a bad format.

                throw PSTraceSource.NewArgumentException("name");
            }

            // Now set the full name

            if (!string.IsNullOrEmpty(_psSnapinName))
            {
                _fullName =
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0}\\{1}",
                        _psSnapinName,
                        _shortName);
            }
            else
            {
                _fullName = _shortName;
            }
        }

        
        internal static PSSnapinQualifiedName? GetInstance(string? name)
        {
            if (name == null)
                return null;
            string[] splitName = name.Split('\\');
            if (splitName.Length == 0 || splitName.Length > 2)
                return null;
            var result = new PSSnapinQualifiedName(splitName);
            // If the shortname is empty, then return null...
            if (string.IsNullOrEmpty(result.ShortName))
            {
                return null;
            }

            return result;
        }

        
        internal string FullName
        {
            get
            {
                return _fullName;
            }
        }

        private readonly string _fullName;

        
        internal string? PSSnapInName
        {
            get
            {
                return _psSnapinName;
            }
        }

        private readonly string? _psSnapinName;

        
        internal string ShortName
        {
            get
            {
                return _shortName;
            }
        }

        private readonly string _shortName;

        
        public override string ToString()
        {
            return _fullName;
        }
    }
}
