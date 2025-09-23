// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Microsoft.PowerShell
{
    
    internal static class UpdatesNotification
    {
        private const string UpdateCheckEnvVar = "POWERSHELL_UPDATECHECK";
        private const string LTSBuildInfoURL = "https://aka.ms/pwsh-buildinfo-lts";
        private const string StableBuildInfoURL = "https://aka.ms/pwsh-buildinfo-stable";
        private const string PreviewBuildInfoURL = "https://aka.ms/pwsh-buildinfo-preview";

        
        private static readonly string s_updateFileNameTemplate, s_updateFileNamePattern;

        
        private static readonly string s_sentinelFileName, s_doneFileNameTemplate, s_doneFileNamePattern;

        private static readonly string s_cacheDirectory;
        private static readonly EnumerationOptions s_enumOptions;
        private static readonly NotificationType s_notificationType;

        
        internal static readonly bool CanNotifyUpdates;

        static UpdatesNotification()
        {
            s_notificationType = GetNotificationType();
            CanNotifyUpdates = false;

            if (CanNotifyUpdates)
            {
                s_enumOptions = new EnumerationOptions();
                s_cacheDirectory = Path.Combine(Platform.CacheDirectory, PSVersionInfo.GitCommitId);

                // Build the template/pattern strings for the configured notification type.
                string typeNum = ((int)s_notificationType).ToString();
                s_sentinelFileName = $"_sentinel{typeNum}_";
                s_doneFileNameTemplate = $"sentinel{typeNum}-{{0}}-{{1}}-{{2}}.done";
                s_doneFileNamePattern = $"sentinel{typeNum}-*.done";
                s_updateFileNameTemplate = $"update{typeNum}_{{0}}_{{1}}";
                s_updateFileNamePattern = $"update{typeNum}_v*.*.*_????-??-??";
            }
        }

        // Maybe we shouldn't do update check and show notification when it's from a mini-shell, meaning when
        // 'ConsoleShell.Start' is not called by 'ManagedEntrance.Start'.
        // But it seems so unusual that it's probably not worth bothering. Also, a mini-shell probably should
        // just disable the update notification feature by setting the opt-out environment variable.

        internal static void ShowUpdateNotification(PSHostUserInterface hostUI)
        {
            if (!Directory.Exists(s_cacheDirectory))
            {
                return;
            }

            if (TryParseUpdateFile(
                    updateFilePath: out _,
                    out SemanticVersion lastUpdateVersion,
                    lastUpdateDate: out _)
               && lastUpdateVersion != null)
            {
                string releaseTag = lastUpdateVersion.ToString();
                string notificationMsgTemplate = s_notificationType == NotificationType.LTS
                    ? ManagedEntranceStrings.LTSUpdateNotificationMessage
                    : string.IsNullOrEmpty(lastUpdateVersion.PreReleaseLabel)
                        ? ManagedEntranceStrings.StableUpdateNotificationMessage
                        : ManagedEntranceStrings.PreviewUpdateNotificationMessage;

                string notificationColor = string.Empty;
                string resetColor = string.Empty;

                string line2Padding = string.Empty;
                string line3Padding = string.Empty;

                // We calculate how much whitespace we need to make it look nice
                if (hostUI.SupportsVirtualTerminal)
                {
                    // Swaps foreground and background colors.
                    notificationColor = "\x1B[7m";
                    resetColor = "\x1B[0m";

                    // The first line is longest, if the message changes, this needs to be updated
                    int line1Length = notificationMsgTemplate.IndexOf('\n');
                    int line2Length = notificationMsgTemplate.IndexOf('\n', line1Length + 1);
                    int line3Length = notificationMsgTemplate.IndexOf('\n', line2Length + 1);
                    line3Length -= line2Length + 1;
                    line2Length -= line1Length + 1;

                    line2Padding = line2Padding.PadRight(line1Length - line2Length + releaseTag.Length);
                    // 3 represents the extra placeholder in the template
                    line3Padding = line3Padding.PadRight(line1Length - line3Length + 3);
                }

                string notificationMsg = string.Format(CultureInfo.CurrentCulture, notificationMsgTemplate, releaseTag, notificationColor, resetColor, line2Padding, line3Padding);

                hostUI.WriteLine();
                hostUI.WriteLine(notificationMsg);
            }
        }

        internal static Task CheckForUpdates()
        {
            return Task.CompletedTask;
        }

        
        private static bool TryParseUpdateFile(
            out string updateFilePath,
            out SemanticVersion lastUpdateVersion,
            out DateTime lastUpdateDate)
        {
            updateFilePath = null;
            lastUpdateVersion = null;
            lastUpdateDate = default;

            var files = Directory.EnumerateFiles(s_cacheDirectory, s_updateFileNamePattern, s_enumOptions);
            var enumerator = files.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                // It's OK that an update file doesn't exist. This could happen when there is no new updates yet.
                return true;
            }

            updateFilePath = enumerator.Current;
            if (enumerator.MoveNext())
            {
                // More than 1 files were found that match the pattern. This is a corrupted state.
                // Theoretically, there should be only one update file at any point of time.
                updateFilePath = null;
                return false;
            }

            // OK, only found one update file for the configured notification type, which is expected.
            // Now let's parse the file name.
            string updateFileName = Path.GetFileName(updateFilePath);
            int dateStartIndex = updateFileName.LastIndexOf('_') + 1;

            if (!DateTime.TryParse(
                    updateFileName.AsSpan(dateStartIndex),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out lastUpdateDate))
            {
                updateFilePath = null;
                return false;
            }

            int versionStartIndex = updateFileName.IndexOf('_') + 2;
            int versionLength = dateStartIndex - versionStartIndex - 1;
            string versionString = updateFileName.Substring(versionStartIndex, versionLength);

            if (SemanticVersion.TryParse(versionString, out lastUpdateVersion))
            {
                return true;
            }

            updateFilePath = null;
            lastUpdateDate = default;
            return false;
        }
        
        private static NotificationType GetNotificationType()
        {
            string str = Environment.GetEnvironmentVariable(UpdateCheckEnvVar);
            if (string.IsNullOrEmpty(str))
            {
                return NotificationType.Default;
            }

            if (Enum.TryParse(str, ignoreCase: true, out NotificationType type))
            {
                return type;
            }

            return NotificationType.Default;
        }

        
        private enum NotificationType
        {
            
            Off = 0,

            
            Default = 1,

            
            LTS = 2
        }

        private sealed class Release
        {
            internal Release(string publishAt, string tagName)
            {
                PublishAt = publishAt;
                TagName = tagName;
            }

            
            internal string PublishAt { get; }

            
            internal string TagName { get; }
        }
    }
}
