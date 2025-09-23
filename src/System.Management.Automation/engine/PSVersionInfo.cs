// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Win32;

namespace System.Management.Automation
{
    
    public static partial class PSVersionInfo
    {
        internal const string PSVersionTableName = "PSVersionTable";
        internal const string PSRemotingProtocolVersionName = "PSRemotingProtocolVersion";
        internal const string PSVersionName = "PSVersion";
        internal const string PSEditionName = "PSEdition";
        internal const string PSGitCommitIdName = "GitCommitId";
        internal const string PSCompatibleVersionsName = "PSCompatibleVersions";
        internal const string PSPlatformName = "Platform";
        internal const string PSOSName = "OS";
        internal const string SerializationVersionName = "SerializationVersion";
        internal const string WSManStackVersionName = "WSManStackVersion";
        internal const string GitCommitId = "none";
        internal const string ProductVersion = "10.0";

        private static readonly PSVersionHashTable s_psVersionTable;

        

        
        private static readonly Version s_psV1Version = new(1, 0);
        private static readonly Version s_psV2Version = new(2, 0);
        private static readonly Version s_psV3Version = new(3, 0);
        private static readonly Version s_psV4Version = new(4, 0);
        private static readonly Version s_psV5Version = new(5, 0);
        private static readonly Version s_psV51Version = new(5, 1);
        private static readonly Version s_psV6Version = new(6, 0);
        private static readonly Version s_psV7Version = new(7, 0);
        private static readonly Version s_psVersion = new(10, 0);
        private static readonly SemanticVersion s_psSemVersion;

        
        internal const string PSEditionValue = "Core";

        // Static Constructor.
        static PSVersionInfo()
        {
            // s_psVersionTable = new PSVersionHashTable(StringComparer.OrdinalIgnoreCase);

            // s_psSemVersion = Version_Label == string.Empty
            //     ? new SemanticVersion(Version_Major, Version_Minor, Version_Patch)
            //     : new SemanticVersion(Version_Major, Version_Minor, Version_Patch, Version_Label, buildLabel: null);
            // s_psVersion = (Version)s_psSemVersion;

            // s_psVersionTable[PSVersionName] = s_psSemVersion;
            // s_psVersionTable[PSEditionName] = PSEditionValue;
            // s_psVersionTable[PSGitCommitIdName] = GitCommitId;
            // s_psVersionTable[PSCompatibleVersionsName] = new Version[] { s_psV1Version, s_psV2Version, s_psV3Version, s_psV4Version, s_psV5Version, s_psV51Version, s_psV6Version, s_psV7Version };
            // s_psVersionTable[SerializationVersionName] = new Version(InternalSerializer.DefaultVersion);
            // s_psVersionTable[PSRemotingProtocolVersionName] = RemotingConstants.ProtocolVersion;
            // s_psVersionTable[WSManStackVersionName] = GetWSManStackVersion();
            // s_psVersionTable[PSPlatformName] = Environment.OSVersion.Platform.ToString();
            // s_psVersionTable[PSOSName] = Runtime.InteropServices.RuntimeInformation.OSDescription;
        }

        internal static PSVersionHashTable GetPSVersionTable()
        {
            return s_psVersionTable;
        }

        internal static Hashtable GetPSVersionTableForDownLevel()
        {
            var result = (Hashtable)s_psVersionTable.Clone();
            // Downlevel systems don't support SemanticVersion, but Version is most likely good enough anyway.
            result[PSVersionInfo.PSVersionName] = s_psVersion;
            return result;
        }

        #region Private helper methods

        // Gets the current WSMan stack version from the registry.
        private static Version GetWSManStackVersion()
        {
            Version version = null;

#if !UNIX
            try
            {
                using (RegistryKey wsManStackVersionKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\WSMAN"))
                {
                    if (wsManStackVersionKey != null)
                    {
                        object wsManStackVersionObj = wsManStackVersionKey.GetValue("ServiceStackVersion");
                        string wsManStackVersion = (wsManStackVersionObj != null) ? (string)wsManStackVersionObj : null;
                        if (!string.IsNullOrEmpty(wsManStackVersion))
                        {
                            version = new Version(wsManStackVersion.Trim());
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (System.Security.SecurityException) { }
            catch (ArgumentException) { }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
            catch (InvalidCastException) { }
#endif

            return version ?? System.Management.Automation.Remoting.Client.WSManNativeApi.WSMAN_STACK_VERSION;
        }

        #endregion

        #region Programmer APIs

        
        public static Version PSVersion
        {
            get
            {
                return s_psVersion;
            }
        }

        
        public static string PSEdition
        {
            get
            {
                return PSEditionValue;
            }
        }

        internal static Version SerializationVersion
        {
            get
            {
                return (Version)s_psVersionTable[SerializationVersionName];
            }
        }

        
        internal static string RegistryVersionKey
        {
            get
            {
                // PowerShell >=4 is compatible with PowerShell 3 and hence reg key is 3.
                return "3";
            }
        }

        internal static string GetRegistryVersionKeyForSnapinDiscovery(string majorVersion)
        {
            int tempMajorVersion = 0;
            LanguagePrimitives.TryConvertTo<int>(majorVersion, out tempMajorVersion);

            if ((tempMajorVersion >= 1) && (tempMajorVersion <= PSVersionInfo.PSVersion.Major))
            {
                // PowerShell version 3 took a dependency on CLR4 and went with:
                // SxS approach in GAC/Registry and in-place upgrade approach for
                // FileSystem.
                // For >=3.0 PowerShell, we still use "1" as the registry version key for
                // Snapin and Custom shell lookup/discovery.
                return "1";
            }

            return null;
        }

        internal static bool IsValidPSVersion(Version version)
        {
            if (version is null)
            {
                return false;
            }

            int minor = version.Minor;
            switch (version.Major)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    return minor == 0;
                case 5:
                    return minor == 0 || minor == 1;
                case 6:
                    return minor >= 0 && minor <= 2;
                case 7:
                    return minor >= 0 && minor <= s_psVersion.Minor;
            }

            return false;
        }

        internal static SemanticVersion PSCurrentVersion
        {
            get { return s_psSemVersion; }
        }

        #endregion
    }

    
    public sealed class PSVersionHashTable : Hashtable, IEnumerable
    {
        private static readonly PSVersionTableComparer s_keysComparer = new PSVersionTableComparer();

        internal PSVersionHashTable(IEqualityComparer equalityComparer) : base(equalityComparer)
        {
        }

        
        public override ICollection Keys
        {
            get
            {
                ArrayList keyList = new ArrayList(base.Keys);
                keyList.Sort(s_keysComparer);
                return keyList;
            }
        }

        private sealed class PSVersionTableComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                string xString = (string)LanguagePrimitives.ConvertTo(x, typeof(string), CultureInfo.CurrentCulture);
                string yString = (string)LanguagePrimitives.ConvertTo(y, typeof(string), CultureInfo.CurrentCulture);
                if (PSVersionInfo.PSVersionName.Equals(xString, StringComparison.OrdinalIgnoreCase))
                {
                    return -1;
                }
                else if (PSVersionInfo.PSVersionName.Equals(yString, StringComparison.OrdinalIgnoreCase))
                {
                    return 1;
                }
                else if (PSVersionInfo.PSEditionName.Equals(xString, StringComparison.OrdinalIgnoreCase))
                {
                    return -1;
                }
                else if (PSVersionInfo.PSEditionName.Equals(yString, StringComparison.OrdinalIgnoreCase))
                {
                    return 1;
                }
                else
                {
                    return string.Compare(xString, yString, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (object key in Keys)
            {
                yield return new DictionaryEntry(key, this[key]);
            }
        }
    }

    
    public sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        private const string VersionSansRegEx = @"^(?<major>\d+)(\.(?<minor>\d+))?(\.(?<patch>\d+))?$";
        private const string LabelRegEx = @"^(?<preLabel>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*)?(?:\+(?<buildLabel>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";
        private const string LabelUnitRegEx = @"^((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*)$";
        private const string BuildUnitRegEx = @"^([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*)$";
        private const string PreLabelPropertyName = "PSSemVerPreReleaseLabel";
        private const string BuildLabelPropertyName = "PSSemVerBuildLabel";
        private const string TypeNameForVersionWithLabel = "System.Version#IncludeLabel";

        private string versionString;

        
        public SemanticVersion(string version)
        {
            var v = SemanticVersion.Parse(version);

            Major = v.Major;
            Minor = v.Minor;
            Patch = v.Patch < 0 ? 0 : v.Patch;
            PreReleaseLabel = v.PreReleaseLabel;
            BuildLabel = v.BuildLabel;
        }

        
        public SemanticVersion(int major, int minor, int patch, string preReleaseLabel, string buildLabel)
            : this(major, minor, patch)
        {
            if (!string.IsNullOrEmpty(preReleaseLabel))
            {
                if (!Regex.IsMatch(preReleaseLabel, LabelUnitRegEx))
                {
                    throw new FormatException(nameof(preReleaseLabel));
                }

                PreReleaseLabel = preReleaseLabel;
            }

            if (!string.IsNullOrEmpty(buildLabel))
            {
                if (!Regex.IsMatch(buildLabel, BuildUnitRegEx))
                {
                    throw new FormatException(nameof(buildLabel));
                }

                BuildLabel = buildLabel;
            }
        }

        
        public SemanticVersion(int major, int minor, int patch, string label)
            : this(major, minor, patch)
        {
            // We presume the SemVer :
            // 1) major.minor.patch-label
            // 2) 'label' starts with letter or digit.
            if (!string.IsNullOrEmpty(label))
            {
                var match = Regex.Match(label, LabelRegEx);
                if (!match.Success)
                {
                    throw new FormatException(nameof(label));
                }

                PreReleaseLabel = match.Groups["preLabel"].Value;
                BuildLabel = match.Groups["buildLabel"].Value;
            }
        }

        
        public SemanticVersion(int major, int minor, int patch)
        {
            if (major < 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(major));
            }

            if (minor < 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(minor));
            }

            if (patch < 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(patch));
            }

            Major = major;
            Minor = minor;
            Patch = patch;
            // We presume:
            // PreReleaseLabel = null;
            // BuildLabel = null;
        }

        
        public SemanticVersion(int major, int minor) : this(major, minor, 0) { }

        
        public SemanticVersion(int major) : this(major, 0, 0) { }

        
        public SemanticVersion(Version version)
        {
            if (version == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(version));
            }

            if (version.Revision > 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(version));
            }

            Major = version.Major;
            Minor = version.Minor;
            Patch = version.Build == -1 ? 0 : version.Build;
            var psobj = new PSObject(version);
            var preLabelNote = psobj.Properties[PreLabelPropertyName];
            if (preLabelNote != null)
            {
                PreReleaseLabel = preLabelNote.Value as string;
            }

            var buildLabelNote = psobj.Properties[BuildLabelPropertyName];
            if (buildLabelNote != null)
            {
                BuildLabel = buildLabelNote.Value as string;
            }
        }

        
        public static implicit operator Version(SemanticVersion semver)
        {
            PSObject psobj;

            var result = new Version(semver.Major, semver.Minor, semver.Patch);

            if (!string.IsNullOrEmpty(semver.PreReleaseLabel) || !string.IsNullOrEmpty(semver.BuildLabel))
            {
                psobj = new PSObject(result);

                if (!string.IsNullOrEmpty(semver.PreReleaseLabel))
                {
                    psobj.Properties.Add(new PSNoteProperty(PreLabelPropertyName, semver.PreReleaseLabel));
                }

                if (!string.IsNullOrEmpty(semver.BuildLabel))
                {
                    psobj.Properties.Add(new PSNoteProperty(BuildLabelPropertyName, semver.BuildLabel));
                }

                psobj.TypeNames.Insert(0, TypeNameForVersionWithLabel);
            }

            return result;
        }

        
        public int Major { get; }

        
        public int Minor { get; }

        
        public int Patch { get; }

        
        public string PreReleaseLabel { get; }

        
        public string BuildLabel { get; }

        
        public static SemanticVersion Parse(string version)
        {
            if (version == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(version));
            }

            if (version == string.Empty)
            {
                throw new FormatException(nameof(version));
            }

            var r = new VersionResult();
            r.Init(true);
            TryParseVersion(version, ref r);

            return r._parsedVersion;
        }

        
        public static bool TryParse(string version, out SemanticVersion result)
        {
            if (version != null)
            {
                var r = new VersionResult();
                r.Init(false);

                if (TryParseVersion(version, ref r))
                {
                    result = r._parsedVersion;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static bool TryParseVersion(string version, ref VersionResult result)
        {
            if (version.EndsWith('-') || version.EndsWith('+') || version.EndsWith('.'))
            {
                result.SetFailure(ParseFailureKind.FormatException);
                return false;
            }

            string versionSansLabel = null;
            var major = 0;
            var minor = 0;
            var patch = 0;
            string preLabel = null;
            string buildLabel = null;

            // We parse the SemVer 'version' string 'major.minor.patch-PreReleaseLabel+BuildLabel'.
            var dashIndex = version.IndexOf('-');
            var plusIndex = version.IndexOf('+');

            if (dashIndex > plusIndex)
            {
                // 'PreReleaseLabel' can contains dashes.
                if (plusIndex == -1)
                {
                    // No buildLabel: buildLabel == null
                    // Format is 'major.minor.patch-PreReleaseLabel'
                    preLabel = version.Substring(dashIndex + 1);
                    versionSansLabel = version.Substring(0, dashIndex);
                }
                else
                {
                    // No PreReleaseLabel: preLabel == null
                    // Format is 'major.minor.patch+BuildLabel'
                    buildLabel = version.Substring(plusIndex + 1);
                    versionSansLabel = version.Substring(0, plusIndex);
                    dashIndex = -1;
                }
            }
            else
            {
                if (plusIndex == -1)
                {
                    // Here dashIndex == plusIndex == -1
                    // No preLabel - preLabel == null;
                    // No buildLabel - buildLabel == null;
                    // Format is 'major.minor.patch'
                    versionSansLabel = version;
                }
                else if (dashIndex == -1)
                {
                    // No PreReleaseLabel: preLabel == null
                    // Format is 'major.minor.patch+BuildLabel'
                    buildLabel = version.Substring(plusIndex + 1);
                    versionSansLabel = version.Substring(0, plusIndex);
                }
                else
                {
                    // Format is 'major.minor.patch-PreReleaseLabel+BuildLabel'
                    preLabel = version.Substring(dashIndex + 1, plusIndex - dashIndex - 1);
                    buildLabel = version.Substring(plusIndex + 1);
                    versionSansLabel = version.Substring(0, dashIndex);
                }
            }

            if ((dashIndex != -1 && string.IsNullOrEmpty(preLabel)) ||
                (plusIndex != -1 && string.IsNullOrEmpty(buildLabel)) ||
                string.IsNullOrEmpty(versionSansLabel))
            {
                // We have dash and no preReleaseLabel  or
                // we have plus and no buildLabel or
                // we have no main version part (versionSansLabel==null)
                result.SetFailure(ParseFailureKind.FormatException);
                return false;
            }

            var match = Regex.Match(versionSansLabel, VersionSansRegEx);
            if (!match.Success)
            {
                result.SetFailure(ParseFailureKind.FormatException);
                return false;
            }

            if (!int.TryParse(match.Groups["major"].Value, out major))
            {
                result.SetFailure(ParseFailureKind.FormatException);
                return false;
            }

            if (match.Groups["minor"].Success && !int.TryParse(match.Groups["minor"].Value, out minor))
            {
                result.SetFailure(ParseFailureKind.FormatException);
                return false;
            }

            if (match.Groups["patch"].Success && !int.TryParse(match.Groups["patch"].Value, out patch))
            {
                result.SetFailure(ParseFailureKind.FormatException);
                return false;
            }

            if (preLabel != null && !Regex.IsMatch(preLabel, LabelUnitRegEx) ||
               (buildLabel != null && !Regex.IsMatch(buildLabel, BuildUnitRegEx)))
            {
                result.SetFailure(ParseFailureKind.FormatException);
                return false;
            }

            result._parsedVersion = new SemanticVersion(major, minor, patch, preLabel, buildLabel);
            return true;
        }

        
        public override string ToString()
        {
            if (versionString == null)
            {
                StringBuilder result = new StringBuilder();

                result.Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);

                if (!string.IsNullOrEmpty(PreReleaseLabel))
                {
                    result.Append('-').Append(PreReleaseLabel);
                }

                if (!string.IsNullOrEmpty(BuildLabel))
                {
                    result.Append('+').Append(BuildLabel);
                }

                versionString = result.ToString();
            }

            return versionString;
        }

        
        public static int Compare(SemanticVersion versionA, SemanticVersion versionB)
        {
            if (versionA != null)
            {
                return versionA.CompareTo(versionB);
            }

            if (versionB != null)
            {
                return -1;
            }

            return 0;
        }

        
        public int CompareTo(object version)
        {
            if (version == null)
            {
                return 1;
            }

            if (!(version is SemanticVersion v))
            {
                throw PSTraceSource.NewArgumentException(nameof(version));
            }

            return CompareTo(v);
        }

        
        public int CompareTo(SemanticVersion value)
        {
            if (value is null)
                return 1;

            if (Major != value.Major)
                return Major > value.Major ? 1 : -1;

            if (Minor != value.Minor)
                return Minor > value.Minor ? 1 : -1;

            if (Patch != value.Patch)
                return Patch > value.Patch ? 1 : -1;

            // SemVer 2.0 standard requires to ignore 'BuildLabel' (Build metadata).
            return ComparePreLabel(this.PreReleaseLabel, value.PreReleaseLabel);
        }

        
        public override bool Equals(object obj)
        {
            return Equals(obj as SemanticVersion);
        }

        
        public bool Equals(SemanticVersion other)
        {
            // SemVer 2.0 standard requires to ignore 'BuildLabel' (Build metadata).
            return other != null &&
                   (Major == other.Major) && (Minor == other.Minor) && (Patch == other.Patch) &&
                   string.Equals(PreReleaseLabel, other.PreReleaseLabel, StringComparison.Ordinal);
        }

        
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        
        public static bool operator ==(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 is null)
            {
                return v2 is null;
            }

            return v1.Equals(v2);
        }

        
        public static bool operator !=(SemanticVersion v1, SemanticVersion v2)
        {
            return !(v1 == v2);
        }

        
        public static bool operator <(SemanticVersion v1, SemanticVersion v2)
        {
            return (Compare(v1, v2) < 0);
        }

        
        public static bool operator <=(SemanticVersion v1, SemanticVersion v2)
        {
            return (Compare(v1, v2) <= 0);
        }

        
        public static bool operator >(SemanticVersion v1, SemanticVersion v2)
        {
            return (Compare(v1, v2) > 0);
        }

        
        public static bool operator >=(SemanticVersion v1, SemanticVersion v2)
        {
            return (Compare(v1, v2) >= 0);
        }

        private static int ComparePreLabel(string preLabel1, string preLabel2)
        {
            // SemVer 2.0 standard p.9
            // Pre-release versions have a lower precedence than the associated normal version.
            // Comparing each dot separated identifier from left to right
            // until a difference is found as follows:
            //     identifiers consisting of only digits are compared numerically
            //     and identifiers with letters or hyphens are compared lexically in ASCII sort order.
            // Numeric identifiers always have lower precedence than non-numeric identifiers.
            // A larger set of pre-release fields has a higher precedence than a smaller set,
            // if all of the preceding identifiers are equal.
            if (string.IsNullOrEmpty(preLabel1))
            {
                return string.IsNullOrEmpty(preLabel2) ? 0 : 1;
            }

            if (string.IsNullOrEmpty(preLabel2))
            {
                return -1;
            }

            var units1 = preLabel1.Split('.');
            var units2 = preLabel2.Split('.');

            var minLength = units1.Length < units2.Length ? units1.Length : units2.Length;

            for (int i = 0; i < minLength; i++)
            {
                var ac = units1[i];
                var bc = units2[i];
                int number1, number2;
                var isNumber1 = Int32.TryParse(ac, out number1);
                var isNumber2 = Int32.TryParse(bc, out number2);

                if (isNumber1 && isNumber2)
                {
                    if (number1 != number2)
                    {
                        return number1 < number2 ? -1 : 1;
                    }
                }
                else
                {
                    if (isNumber1)
                    {
                        return -1;
                    }

                    if (isNumber2)
                    {
                        return 1;
                    }

                    int result = string.CompareOrdinal(ac, bc);
                    if (result != 0)
                    {
                        return result;
                    }
                }
            }

            return units1.Length.CompareTo(units2.Length);
        }

        internal enum ParseFailureKind
        {
            ArgumentException,
            ArgumentOutOfRangeException,
            FormatException
        }

        internal struct VersionResult
        {
            internal SemanticVersion _parsedVersion;
            internal ParseFailureKind _failure;
            internal string _exceptionArgument;
            internal bool _canThrow;

            internal void Init(bool canThrow)
            {
                _canThrow = canThrow;
            }

            internal void SetFailure(ParseFailureKind failure)
            {
                SetFailure(failure, string.Empty);
            }

            internal void SetFailure(ParseFailureKind failure, string argument)
            {
                _failure = failure;
                _exceptionArgument = argument;
                if (_canThrow)
                {
                    throw GetVersionParseException();
                }
            }

            internal Exception GetVersionParseException()
            {
                switch (_failure)
                {
                    case ParseFailureKind.ArgumentException:
                        return PSTraceSource.NewArgumentException("version");
                    case ParseFailureKind.ArgumentOutOfRangeException:
                        throw new ValidationMetadataException("ValidateRangeTooSmall",
                            null, Metadata.ValidateRangeSmallerThanMinRangeFailure,
                            _exceptionArgument, "0");
                    case ParseFailureKind.FormatException:
                        // Regenerate the FormatException as would be thrown by Int32.Parse()
                        try
                        {
                            Int32.Parse(_exceptionArgument, CultureInfo.InvariantCulture);
                        }
                        catch (FormatException e)
                        {
                            return e;
                        }
                        catch (OverflowException e)
                        {
                            return e;
                        }

                        break;
                }

                return PSTraceSource.NewArgumentException("version");
            }
        }
    }
}
