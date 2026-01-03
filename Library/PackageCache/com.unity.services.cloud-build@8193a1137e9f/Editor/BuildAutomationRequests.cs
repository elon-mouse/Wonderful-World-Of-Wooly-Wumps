using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.Services.CloudBuild.Editor
{
    internal class Link
    {
        internal string href { get; set; }
    }

    internal class ProjectInfoLinks
    {
        [JsonProperty("self")]
        internal Link Self { get; set; }

        [JsonProperty("list_buildTargets")]
        internal Link BuildTargets { get; set; }

        [JsonProperty("latest_builds")]
        internal Link LatestBuilds { get; set; }
    }

    internal class ProjectInfo
    {
        [JsonProperty("disabled")]
        internal bool Disabled { get; set; }

        [JsonProperty("links")]
        internal ProjectInfoLinks Links;

        [JsonProperty("settings")]
        internal ProjectSettings Settings;
    }

    internal class ProjectSettings
    {
        [JsonProperty("scm")]
        internal ProjectScmSettings Scm;
    }

    internal class ProjectScmSettings
    {
        [JsonProperty("type")]
        internal string Type { get; set; }

        [JsonProperty("url")]
        internal string Url { get; set; }

        [JsonProperty("user")]
        internal string User { get; set; }

        [JsonProperty("authType")]
        internal string AuthType { get; set; }

        [JsonProperty("plasticAccessToken")]
        internal string PlasticAccessToken { get; set; }

        [JsonProperty("useEncryption")]
        internal bool UseEncryption { get; set; }

        [JsonProperty("windowsGitBinary")]
        internal string WindowsGitBinary { get; set; }
    }

    internal class RequestHeader
    {
        internal string Key;
        internal string Value;
    }

    internal class EffectiveBillingPlan
    {
        [JsonProperty("label")]
        internal string Label { get; set; }
    }

    internal class SupportedPlatform
    {
        [JsonProperty("platform")]
        internal string Platform { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("editorIcon")]
        internal string EditorIcon { get; set; }

        [JsonProperty("operatingSystems")]
        internal List<OperatingSystem> OperatingSystems { get; set; }

        [JsonProperty("credentials")]
        internal CredentialType Credentials { get; set; }
    }

    internal enum CredentialType
    {
        required,
        optional,
        none
    }

    internal class OperatingSystem
    {
        [JsonProperty("family")]
        internal string Family { get; set; }

        [JsonProperty("defaultVersion")]
        internal string DefaultVersion { get; set; }

        [JsonProperty("versions")]
        internal List<OperatingSystemVersion> Versions { get; set; }
    }

    internal class OperatingSystemVersion
    {
        [JsonProperty("value")]
        internal string Value { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("default")]
        internal bool Default { get; set; }

        [JsonProperty("deprecated")]
        internal bool Deprecated { get; set; }
    }
    class GetProjectBillingPlanResponse
    {
        [JsonProperty("effective")]
        internal EffectiveBillingPlan Effective;
    }

    class ProjectBuildTargetLinks
    {
        [JsonProperty("start_builds")]
        internal Link StartBuilds { get; set; }

        [JsonProperty("self")]
        internal Link Self { get; set; }
    }

    class ProjectBuildAttemptLinks
    {
        [JsonProperty("artifacts")]
        internal List<BuildAttemptArtifact> Artifacts { get; set; }

        [JsonProperty("log")]
        internal Link Log { get; set; }
    }

    class BuildAttemptArtifact
    {
        [JsonProperty("files")]
        internal List<BuildAttemptFiles> Files { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("key")]
        internal string Key { get; set; }

        [JsonProperty("primary")]
        internal bool Primary { get; set; }

        [JsonProperty("url")]
        internal string Url { get; set; }
    }

    class BuildAttemptFiles
    {
        [JsonProperty("filename")]
        internal string Filename { get; set; }

        [JsonProperty("size")]
        internal int Size { get; set; }

        [JsonProperty("href")]
        internal string Href { get; set; }
    }

    class ProjectBuildTarget
    {
        [JsonProperty("error")]
        internal string Error { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("enabled")]
        internal bool Enabled { get; set; }

        [JsonProperty("buildtargetid")]
        internal string Id { get; set; }

        [JsonProperty("links")]
        internal ProjectBuildTargetLinks Links { get; set; }

        [JsonProperty("credentials")]
        internal Credentials Credentials { get; set; }

        [JsonProperty("settings")]
        internal BuildTargetSettings Settings { get; set; }
    }

    class UnityVersion
    {
        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("value")]
        internal string Value { get; set; }
    }

    class BuildTargetSettings
    {
        [JsonProperty("operatingSystemSelected")]
        internal string OperatingSystemSelected { get; set; }

        [JsonProperty("operatingSystemVersion")]
        internal string OperatingSystemVersion { get; set; }

        [JsonProperty("machineTypeLabel")]
        internal string MachineTypeLabel { get; set; }
    }

    class Credentials
    {
        [JsonProperty("signing")]
        internal SigningCredentials Signing { get; set; }
    }

    class SigningCredentials
    {
        [JsonProperty("credentialid")]
        internal string CredentialId { get; set; }
    }

    class ProjectBuildAttempt
    {
        [JsonProperty("error")]
        internal string Error { get; set; }

        [JsonProperty("build")]
        internal float Build { get; set; }

        [JsonProperty("buildtargetid")]
        internal string BuildTargetId { get; set; }

        [JsonProperty("buildStatus")]
        internal string BuildStatus { get; set; }

        [JsonProperty("platform")]
        internal string Platform { get; set; }

        [JsonProperty("buildTargetName")]
        internal string BuildTargetName { get; set; }

        [JsonProperty("created")]
        internal string Created { get; set; }

        [JsonProperty("finished")]
        internal string Finished { get; set; }

        [JsonProperty("buildTimeInSeconds")]
        internal float BuildTimeInSeconds { get; set; }

        [JsonProperty("causedByUser")]
        internal string CausedByUser { get; set; }

        [JsonProperty("scmBranch")]
        internal string Branch { get; set; }

        [JsonProperty("repo")]
        internal string Repo { get; set; }

        [JsonProperty("formattedPlatformName")]
        internal string FormattedPlatformName { get; set; }

        [JsonProperty("editorIcon")]
        internal string EditorIcon { get; set; }

        [JsonProperty("requestedRevision")]
        internal string Revision { get; set; }

        [JsonProperty("links")]
        internal ProjectBuildAttemptLinks Links { get; set; }

        internal int TotalAttempts { get; set; }
        internal string AttemptRange { get; set; }
    }

    class GetApiStatusResponse
    {
        [JsonProperty("text")]
        internal string NotificationText { get; set; }

        [JsonProperty("alertType")]
        internal string NotificationAlertType { get; set; }

        [JsonProperty("billingPlan")]
        internal string BillingPlan { get; set; }
    }

    class LaunchBuildRequest
    {
        [JsonProperty("clean")]
        internal bool Clean { get; set; }

        [JsonProperty("causedBy")]
        internal string CausedBy { get; set; }

        [JsonProperty("shelvesetID")]
        internal string ShelvesetID { get; set; }
    }

    class CancelBuildRequest
    {
        [JsonProperty("clean")]
        internal bool Clean { get; set; }

        [JsonProperty("causedBy")]
        internal string CausedBy { get; set; }
    }

    class DeleteBuildsResponse
    {
        [JsonProperty("numberOfDeletedBuildArtifacts")]
        internal int NumDeletedBuilds { get; set; }
    }

    class DeleteBuildsRequest
    {
        [JsonProperty("builds")]
        internal List<DeleteBuildRequest> Builds { get; set; }
    }

    class DeleteBuildRequest
    {
        [JsonProperty("build")]
        internal int BuildNumber { get; set; }

        [JsonProperty("buildtargetid")]
        internal string BuildTargetId { get; set; }
    }

    class LaunchedBuildResponse
    {
        [JsonProperty("error")]
        internal string Error { get; set; }

        [JsonProperty("build")]
        internal float Build { get; set; }
    }

    class UvcsTokenExchangeResponse
    {
        [JsonProperty("user")]
        internal string User { get; set; }

        [JsonProperty("accessToken")]
        internal string AccessToken { get; set; }

        [JsonProperty("refreshToken")]
        internal string RefreshToken { get; set; }

        [JsonProperty("authAccessToken")]
        internal string AuthAccessToken { get; set; }
    }

    class GetCredentialsResponse
    {
        [JsonProperty("platform")]
        internal string Platform { get; set; }

        [JsonProperty("credentialid")]
        internal string CredentialId { get; set; }

        [JsonProperty("keystore")]
        internal AndroidKeyStoreResponse KeyStore { get; set; }

        [JsonProperty("label")]
        internal string Label { get; set; }

        [JsonProperty("created")]
        internal string Created { get; set; }

        [JsonProperty("certificate")]
        internal CertificateResponse Certificate { get; set; }

        [JsonProperty("provisioningProfile")]
        internal ProvisioningProfileResponse ProvisioningProfile { get; set; }

        [JsonProperty("providerName")]
        internal string ProviderName { get; set; }

        [JsonProperty("appleIdUsername")]
        internal string AppleIdUsername { get; set; }
    }

    class CertificateResponse
    {
        [JsonProperty("expiration")]
        internal string Expiration { get; set; }

        [JsonProperty("certName")]
        internal string CertName { get; set; }

        [JsonProperty("isDistribution")]
        internal bool IsDistribution { get; set; }

        [JsonProperty("issuer")]
        internal string Issuer { get; set; }

        [JsonProperty("uploaded")]
        internal string Uploaded { get; set; }

        [JsonProperty("teamId")]
        internal string TeamId { get; set; }

    }

    class ProvisioningProfileResponse
    {
        [JsonProperty("teamId")]
        internal string TeamId { get; set; }

        [JsonProperty("bundleId")]
        internal string BundleId { get; set; }

        [JsonProperty("expiration")]
        internal string Expiration { get; set; }

        [JsonProperty("isEnterpriseProfile")]
        internal bool IsEnterpriseProfile { get; set; }

        [JsonProperty("type")]
        internal string Type { get; set; }

        [JsonProperty("numDevices")]
        internal int NumDevices { get; set; }

        [JsonProperty("uploaded")]
        internal string Uploaded { get; set; }

        [JsonProperty("provisionedDevices")]
        internal List<string> ProvisionedDevices { get; set; }
    }

    class AndroidKeyStoreResponse
    {
        [JsonProperty("alias")]
        internal string Alias { get; set; }

        [JsonProperty("debug")]
        internal bool Debug { get; set; }

        [JsonProperty("expiration")]
        internal string Expiration { get; set; }
    }

    class BuildTargetStatus
    {
        [JsonProperty("build")]
        internal float Build { get; set; }

        [JsonProperty("buildtargetid")]
        internal string BuildTargetId { get; set; }

        [JsonProperty("buildStatus")]
        internal string BuildStatus { get; set; }

        [JsonProperty("buildTargetName")]
        internal string BuildTargetName { get; set; }
    }

    internal class GetConfigurationResponse
    {
        [JsonProperty("operatingSystemVersions")]
        internal List<GetOperatingSystemResponse> OperatingSystems { get; set; }

        [JsonProperty("xcodeVersions")]
        internal List<GetXCodeVersionResponse> XcodeVersions { get; set; }

        [JsonProperty("machineTypes")]
        internal List<GetMachineTypeResponse> MachineTypes { get; set; }

        [JsonProperty("androidSdkConfig")]
        internal List<GetAndroidSdkConfigResponse> AndroidSdkValues { get; set; }
    }

    internal class GetAndroidSdkConfigResponse
    {
        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("value")]
        internal string Value { get; set; }
    }

    internal class GetOperatingSystemResponse
    {
        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("value")]
        internal string Value { get; set; }

        [JsonProperty("family")]
        internal string Family { get; set; }

        [JsonProperty("version")]
        internal string Version { get; set; }

        [JsonProperty("unity_versions")]
        internal List<string> UnityVersions { get; set; }

        [JsonProperty("xcode_versions")]
        internal List<string> XcodeVersions { get; set; }

        [JsonProperty("default_xcode_version")]
        internal string DefaultXcodeVersion { get; set; }

        [JsonProperty("hidden")]
        internal bool Hidden { get; set; }

        [JsonProperty("deprecated")]
        internal bool Deprecated { get; set; }

        [JsonProperty("architectures")]
        internal List<string> Architectures { get; set; }
    }

    internal class GetXCodeVersionResponse
    {
        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("value")]
        internal string Value { get; set; }

        [JsonProperty("hidden")]
        internal bool Hidden { get; set; }

        [JsonProperty("deprecated")]
        internal bool Deprecated { get; set; }
    }

    internal class GetMachineTypeResponse
    {
        [JsonProperty("name")] internal string Name { get; set; }

        [JsonProperty("shortName")] internal string ShortName { get; set; }

        [JsonProperty("label")] internal string Label { get; set; }

        [JsonProperty("operatingSystem")] internal string OperatingSystem { get; set; }

        [JsonProperty("operatingSystemVersions")]
        internal List<string> OperatingSystemVersions { get; set; }

        [JsonProperty("enabled")] internal bool Enabled { get; set; }

        [JsonProperty("default")] internal bool Default { get; set; }

        [JsonProperty("isBestValue")] internal bool IsBestValue { get; set; }

        [JsonProperty("sku")] internal string Sku { get; set; }

        [JsonProperty("cpuCores")] internal string cpuCores { get; set; }

        [JsonProperty("memoryGB")] internal string memoryGB { get; set; }

        [JsonProperty("diskSizeGB")] internal string diskSizeGB { get; set; }
    }

    internal class GetProductsResponse
    {
        [JsonProperty("products")]
        internal List<Product> Products { get; set; }
    }

    internal class Product
    {
        [JsonProperty("name")]
        internal string Name { get; set; }

        [JsonProperty("sku")]
        internal string Sku { get; set; }

        [JsonProperty("priceCents")]
        internal int PriceCents { get; set; }
    }
}
