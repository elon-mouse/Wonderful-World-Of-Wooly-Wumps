using UnityEditor;

namespace Unity.Services.CloudBuild.Editor
{
    static class BuildAutomationDashboardUrls
    {
        const string k_HomeFormat = "https://dashboard.unity3d.com/organizations/{0}/projects/{1}/cloud-build";
        const string k_BaseUrl = "https://dashboard.unity3d.com/organizations/{0}";
        const string k_DocsBase = "https://docs.unity.com/ugs/en-us/manual/devops/manual/build-automation";
        const string k_OrgHomeFormat = k_BaseUrl + "/cloud-build";

        public static string GetHomeUrl()
            => FillUrlWithOrganizationAndProjectIds(k_HomeFormat);

        public static string GetMachineSpecsUrl() => $"{k_DocsBase}/optimize-build-speed/choose-machine-specification";
        public static string GetSupportedVersionsUrl() => $"{k_DocsBase}/reference/supported-unity-versions";

        public static string GetSupportedPlatformsUrl() => $"{k_DocsBase}/reference/supported-platforms-on-each-builder-os";

        public static string GetUpgradeUrl() => $"https://service-store.unity.com/order/create?sku=UTY-CLD-BLD&org_id={CloudProjectSettings.organizationKey}&dashboardOrigin=build-automation-configuration";

        public static string GetPlanComparisonUrl() => "https://unity.com/products/compare-plans/unity-cloud";

        public static string GetAboutUrl()
            => FillUrlWithOrganizationAndProjectIds($"{k_HomeFormat}/history");

        public static string GetHistoryUrl()
            => FillUrlWithOrganizationAndProjectIds($"{k_HomeFormat}/history");

        public static string GetConfigUrl()
            => FillUrlWithOrganizationAndProjectIds($"{k_HomeFormat}/config");

        public static string GetUploadUrl()
            => FillUrlWithOrganizationAndProjectIds($"{k_HomeFormat}/history?upload=true");

        public static string GetCredentialsUrl()
            => FillUrlWithOrganizationAndProjectIds($"{k_OrgHomeFormat}/settings/credentials-settings");

        public static string NewCredentialsByPlatformUrl(string platform)
            => FillUrlWithOrganizationAndProjectIds($"{k_OrgHomeFormat}/settings/credentials-settings?platformId={platform}");

        public static string GetAttemptUrl(string targetid, string attemptNumber)
            => FillUrlWithOrganizationAndProjectIds($"{k_HomeFormat}/buildtargets/{targetid}/builds/{attemptNumber}");

        static string FillUrlWithOrganizationAndProjectIds(string url)
        {
            var organization = CloudProjectSettings.organizationKey;
            var filledUrl = string.Format(url, organization, CloudProjectSettings.projectId);

            return filledUrl;
        }
    }
}
