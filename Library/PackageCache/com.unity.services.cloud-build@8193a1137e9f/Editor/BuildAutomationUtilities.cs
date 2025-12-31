using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Profile;
using DateTime = System.DateTime;

namespace Unity.Services.CloudBuild.Editor
{
    internal static class BuildAutomationUtilities
    {
        internal static DateTime ToLocalTime(string utcTimeString)
        {
            try
            {
                var utcDateTime = DateTime.ParseExact(utcTimeString, "yyyy-MM-ddTHH:mm:ss.fffZ",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                return utcDateTime.ToLocalTime();
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

        internal static string FormatSize(float fileSizeInBytes)
        {
            var sizes = new [] { "B", "KB", "MB", "GB", "TB" };
            var order = 0;
            while (fileSizeInBytes >= 1024 && order < sizes.Length - 1) {
                order++;
                fileSizeInBytes /= 1024;
            }

            return $"{fileSizeInBytes:0.#} {sizes[order]}";
        }

        internal static string GetUnityVersion()
        {
            var version = UnityEditorInternal.InternalEditorUtility.GetFullUnityVersion().Split(" ")[0].Trim();
            return version.Replace(".", "_");
        }

        internal static bool IsConnectedToUnityCloud()
        {
            return !string.IsNullOrEmpty(CloudProjectSettings.organizationKey) &&
                    !string.IsNullOrEmpty(CloudProjectSettings.projectId);
        }

        internal static string FormatTime(TimeSpan ts)
        {
            int timeValue;
            string timeUnit;
            if (ts.TotalHours < 1)
            {
                timeUnit = "minute";
                timeValue = (int)ts.TotalMinutes;
            }
            else if (ts.TotalDays < 1)
            {
                timeUnit = "hour";
                timeValue = (int)ts.TotalHours;
            }
            else
            {
                timeUnit = "day";
                timeValue = (int)ts.TotalDays;
            }

            return $"{timeValue} {timeUnit}{ (timeValue != 1 ?  "s" : "")  } ago";
        }

        internal static string IconNameFromPlatformAndTarget(string platform, bool serverSubtarget)
        {
            if (serverSubtarget)
            {
                return "BuildSettings.DedicatedServer";
            }

            if (platform == BuildTarget.Android.ToString().ToLower())
                return "BuildSettings.Android";
            if (platform == BuildTarget.EmbeddedLinux.ToString().ToLower())
                return "BuildSettings.EmbeddedLinux";
            if (platform == BuildTarget.GameCoreXboxOne.ToString().ToLower())
                return "BuildSettings.GameCoreXboxOne";
            if (platform == BuildTarget.GameCoreXboxSeries.ToString().ToLower() || platform == "gamecorescarlett")
                return "BuildSettings.GameCoreScarlett";
            if (platform == BuildTarget.iOS.ToString().ToLower())
                return "BuildSettings.iPhone";
            if (platform == BuildTarget.LinuxHeadlessSimulation.ToString().ToLower())
                return "BuildSettings.LinuxHeadlessSimulation";
            if (platform == BuildTarget.PS4.ToString().ToLower())
                return "BuildSettings.PS4";
            if (platform == BuildTarget.PS5.ToString().ToLower())
                return "BuildSettings.PS5";
            if (platform == BuildTarget.QNX.ToString().ToLower())
                return "BuildSettings.QNX";
            if (platform.Contains("standalonelinux"))
                return "BuildSettings.Linux";
            if (platform.Contains("standaloneosx"))
                return "BuildSettings.OSX";
            if (platform.Contains("standalonewindows"))
                return "BuildSettings.Windows";
            if (platform == BuildTarget.Switch.ToString().ToLower())
                return "BuildSettings.Switch";
            if (platform == BuildTarget.tvOS.ToString().ToLower())
                return "BuildSettings.tvOS";
            if (platform == BuildTarget.VisionOS.ToString().ToLower())
                return "BuildSettings.visionOS";
            if (platform == BuildTarget.WSAPlayer.ToString().ToLower())
                return "BuildSettings.Metro";
            if (platform == BuildTarget.XboxOne.ToString().ToLower())
                return "BuildSettings.XboxOne";
            if (platform == BuildTarget.WebGL.ToString().ToLower() || platform.Contains("web"))
                return "BuildSettings.WebGL";

            return "BuildSettings.Standalone";
        }

        public static string NormalizeBuildTargetNameURL(string href, string targetName)
        {
            targetName = NormalizedBuildTargetName(targetName);
            return href + "/" + targetName;
        }

        public static string NormalizedBuildTargetName(string targetName)
        {
            targetName = targetName.ToLower();

            // Replace spaces with hyphens
            targetName = targetName.Replace(" ", "-");

            // Remove all non-alphanumeric and non-hyphen characters
            targetName = Regex.Replace(targetName, @"[^a-z0-9-]", "");

            // Replace multiple hyphens with a single hyphen
            targetName = Regex.Replace(targetName, @"-+", "-");

            return targetName;
        }

        internal static string GetOSFamily(string operatingSystemValue)
        {
            if (operatingSystemValue.Contains("windows"))
            {
                return "windows";
            }
            else
            {
                return "mac";
            }
        }

        internal static string GetCredentialPlatform(BuildTarget target)
        {
            string platform = target switch
            {
                BuildTarget.Android => "android",
                BuildTarget.iOS => "ios",
                BuildTarget.StandaloneOSX => "osx",
                BuildTarget.WSAPlayer => "uwp",
                _ => "unknown"
            };

            return platform;
        }

        internal static bool TargetRequiresCredentials(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.Android => true,
                BuildTarget.iOS => true,
                BuildTarget.StandaloneOSX => true,
                BuildTarget.WSAPlayer => true,
                _ => false
            };
        }

        internal static string GetExpirationDateFromCredentials(GetCredentialsResponse credentials, BuildTarget platform)
        {
            var credentialsPlatform = GetCredentialPlatform(platform);

            return credentialsPlatform switch
            {
                "android" => credentials?.KeyStore?.Expiration,
                "ios" => credentials?.Certificate?.Expiration,
                "osx" => credentials?.Certificate?.Expiration,
                "uwp" => credentials?.KeyStore?.Expiration,
                _ => ""
            };
        }

        internal static bool IsStandalonePlatform(string platform)
        {
            return platform.Contains("standlone");
        }

        internal static string FormatPrice(int priceInCents)
        {
            var dollars = Math.Floor((float)priceInCents / 100);
            var cents = priceInCents % 100;

            return $"${dollars}.{cents:D2}";
        }

        internal static string FormatDate(string dateString)
        {
            var dateTime = DateTime.Parse(dateString);
            return dateTime.ToString(CultureInfo.CurrentCulture);
        }

        internal static string SanitizeBuildTargetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Allow only valid characters: alphanumeric, underscores, hyphens, @, dots, and spaces
            string sanitized = Regex.Replace(name, @"[^\w\-@. ]", "");

            sanitized = sanitized.Trim();

            return sanitized;
        }

        internal static string GetBuildTargetNameFromProfile(BuildProfile profile)
        {
            var targetName = $"{profile.name} - {BuildAutomationUVCSConnector.Repo}@{BuildAutomationUVCSConnector.Branch}".Trim();
            return SanitizeBuildTargetName(targetName);
        }
    }
}
