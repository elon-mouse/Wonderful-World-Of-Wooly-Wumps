using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Profile;

namespace Unity.Services.CloudBuild.Editor
{
    internal class BuildAutomationTargetRequestDto
    {
        [JsonProperty("name")]
        string Name { set; get; }

        [JsonProperty("createdBy")]
        string CreatedBy { set; get; }

        [JsonProperty("platform")]
        string Platform { set; get; }

        [JsonProperty("enabled")]
        bool Enabled { set; get; }

        [JsonProperty("credentials")]
        internal BuildAutomationCredentialsDto AutomationCredentials { set; get; }

        [JsonProperty("settings")]
        BuildAutomationTargetSettingsDto Settings { set; get; }

        internal BuildAutomationTargetRequestDto(BuildProfile buildProfile, BuildAutomationSettings settings)
        {
            InitializeCommonSettings(buildProfile, settings);
            if (BuildAutomationUtilities.TargetRequiresCredentials(settings.buildTarget))
            {
                AutomationCredentials = new BuildAutomationCredentialsDto
                {
                    AutomationSigning = new BuildAutomationSigningCredentialsDto
                    {
                        CredentialId = settings.credentialsId,
                    }
                };
            }
        }

        void InitializeCommonSettings(BuildProfile buildProfile, BuildAutomationSettings settings)
        {
            Platform = settings.buildTarget.ToString().ToLower();
            Name = BuildAutomationUtilities.GetBuildTargetNameFromProfile(buildProfile);
            Enabled = true;
            CreatedBy = "editor";
            Settings = new BuildAutomationTargetSettingsDto
            {
                BuildProfilePath = AssetDatabase.GetAssetPath(buildProfile),
                UnityVersion = BuildAutomationUtilities.GetUnityVersion(),
                SCM = new BuildAutomationTargetScmDto
                {
                    Type = "plastic",
                    Subdirectory = BuildAutomationUVCSConnector.GetUnityProjectSubfolder(),
                    Repo = BuildAutomationUVCSConnector.Repo,
                    Branch = BuildAutomationUVCSConnector.Branch,
                },
                Platform = new BuildAutomationTargetPlatformDto
                {
                    BundleId = PlayerSettings.applicationIdentifier, // TODO: This only works for the active profile
                    XcodeVersion = settings.xcodeVersion,
                },

                OperatingSystemSelected = settings.operatingSystemFamily,
                OperatingSystemVersion = settings.operatingSystemValue,
                MachineTypeLabel = settings.machineTypeId,
                Advanced = new BuildAutomationTargetAdvancedSettingsDto
                {
                    Xcode = new BuildAutomationTargetXcodeDto
                    {
                        UploadXcArchive = true,
                        UploadXcodeProject = true,
                    },
                    Android = new BuildAutomationTargetAndroidDto
                    {
                        AndroidSDKVersion = settings.androidSdkVersion,
                    },
                    Unity = new BuildAutomationTargetUnityDto
                    {
                        ScriptingDefineSymbols = string.Join(";", buildProfile.scriptingDefines),
                        PlayerExporter = new BuildAutomationTargetPlayerExporterDto
                        {
                            Export = true,
                        },
                        EnableLightBake = false,
                    },
                },
            };
        }
    }

    internal class BuildAutomationCredentialsDto
    {
        [JsonProperty("signing")]
        internal BuildAutomationSigningCredentialsDto AutomationSigning { set; get; }
    }

    internal class BuildAutomationSigningCredentialsDto
    {
        [JsonProperty("credentialid")]
        internal string CredentialId { set; get; }
    }

    struct BuildAutomationTargetSettingsDto
    {
        [JsonProperty("unityVersion")]
        internal string UnityVersion { set; get; }

        [JsonProperty("scm")]
        internal BuildAutomationTargetScmDto SCM { set; get; }

        [JsonProperty("platform")]
        internal BuildAutomationTargetPlatformDto Platform { set; get; }

        [JsonProperty("machineTypeLabel")]
        internal string MachineTypeLabel { set; get; }

        [JsonProperty("operatingSystemSelected")]
        internal string OperatingSystemSelected { set; get; }

        [JsonProperty("operatingSystemVersion")]
        internal string OperatingSystemVersion { set; get; }

        [JsonProperty("advanced")]
        internal BuildAutomationTargetAdvancedSettingsDto Advanced { set; get; }

        [JsonProperty("buildProfilePath")]
        internal string BuildProfilePath { set; get; }
    }

    struct BuildAutomationTargetScmDto
    {
        [JsonProperty("type")]
        internal string Type { set; get; }

        [JsonProperty("repo")]
        internal string Repo { set; get; }

        [JsonProperty("branch")]
        internal string Branch { set; get; }

        [JsonProperty("subdirectory")]
        internal string Subdirectory { set; get; }

        [JsonProperty("client")]
        internal string Client { set; get; }
    }
    struct BuildAutomationTargetPlatformDto
    {
        [JsonProperty("bundleId")]
        internal string BundleId { set; get; }

        [JsonProperty("xcodeVersion")]
        internal string XcodeVersion { set; get; }
    }

    struct BuildAutomationTargetBuildScheduleDto
    {
        [JsonProperty("isEnabled")]
        internal bool IsEnabled;

        [JsonProperty("date")]
        internal string Date;

        [JsonProperty("repeatCycle")]
        internal string RepeatCycle;

        [JsonProperty("cleanBuild")]
        internal bool CleanBuild;
    }

    struct BuildAutomationTargetAdvancedSettingsDto
    {
        [JsonProperty("xcode")]
        internal BuildAutomationTargetXcodeDto Xcode;

        [JsonProperty("android")]
        internal BuildAutomationTargetAndroidDto Android;

        [JsonProperty("unity")]
        internal BuildAutomationTargetUnityDto Unity;
    }

    struct BuildAutomationTargetCacheDto
    {
        [JsonProperty("compressionMethod")]
        internal string CompressionMethod;
    }

    struct BuildAutomationTargetXcodeDto
    {
        [JsonProperty("uploadXCArchive")]
        internal bool UploadXcArchive { set; get; }

        [JsonProperty("uploadXcodeProject")]
        internal bool UploadXcodeProject { set; get; }
    }

    struct BuildAutomationTargetAndroidDto
    {
        [JsonProperty("buildAppBundle")]
        internal bool BuildAppBundle { set; get; }

        [JsonProperty("androidSDKVersion")]
        internal string AndroidSDKVersion { set; get; }
    }

    struct BuildAutomationTargetUnityDto
    {
        [JsonProperty("scriptingDefineSymbols")]
        internal string ScriptingDefineSymbols { set; get; }

        [JsonProperty("playerExporter")]
        internal BuildAutomationTargetPlayerExporterDto PlayerExporter { set; get; }

        [JsonProperty("enableLightBake")]
        internal bool EnableLightBake { set; get; }
    }

    struct BuildAutomationTargetPlayerExporterDto
    {
        [JsonProperty("sceneList")]
        internal string[] SceneList { set; get; }

        [JsonProperty("buildOptions")]
        internal string[] BuildOptions { set; get; }

        [JsonProperty("export")]
        internal bool Export { set; get; }
    }
}
