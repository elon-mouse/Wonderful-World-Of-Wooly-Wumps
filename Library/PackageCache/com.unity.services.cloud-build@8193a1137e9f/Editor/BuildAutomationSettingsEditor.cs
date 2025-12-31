using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudBuild.Editor.Components;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.CloudBuild.Editor
{
    [CustomEditor(typeof(BuildAutomationSettings))]
    internal class BuildAutomationSettingsEditor : UnityEditor.Editor
    {
        private const string k_PlayerSettingsHelpBoxAdjustmentsClass = "custom-player-settings-info-helpbox";
        const string k_Uss = "Packages/com.unity.services.cloud-build/Editor/USS/BuildProfile.uss";

        private VisualElement m_BuilderConfigurationFoldout;
        private VisualElement m_UvcsWarningBox;
        private VisualElement m_CloudWarningBox;
        private VisualElement m_ConfigFetchErrorBox;
        private VisualElement m_UnityVersionWarningBox;
        private VisualElement m_PlatformWarningBox;
        private VisualElement m_BuildAutomationLoadingLabel;
        private BuildAutomationCredentialsConfig m_BuildAutomationCredentialsConfig;
        private BuildAutomationBuilderConfig m_BuildAutomationBuilderConfig;
        private BuildAutomationApiClient m_ApiClient;

        private static CancellationTokenSource _currentCancellationTokenSource;
        private static readonly object _lock = new object();

        public override VisualElement CreateInspectorGUI()
        {
            m_ApiClient = new BuildAutomationApiClient();
            var root = new VisualElement();
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_Uss);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            if (serializedObject.targetObject is not BuildAutomationSettings buildAutomationSettings)
                throw new InvalidOperationException("Editor object is not of type BuildAutomationSettings.");

            // Loading Label - shown while async checks are being performed
            m_BuildAutomationLoadingLabel = new Label("Fetching Build Automation Settings...");
            m_BuildAutomationLoadingLabel.style.display = DisplayStyle.None;
            root.Add(m_BuildAutomationLoadingLabel);

            // Main Config Foldout - start hidden, requires async API call to determine if it should be shown
            m_BuilderConfigurationFoldout = CreateConfigFoldout(buildAutomationSettings.buildTarget);
            m_BuilderConfigurationFoldout.style.display = DisplayStyle.None;
            root.Add(m_BuilderConfigurationFoldout);

            // Warning box if not connected to Unity Cloud
            m_CloudWarningBox = CreateCloudConnectWarningBox();
            m_CloudWarningBox.style.display = BuildAutomationUtilities.IsConnectedToUnityCloud() ? DisplayStyle.None : DisplayStyle.Flex;
            root.Add(m_CloudWarningBox);

            // Warning box if not connected to Unity Version Control
            m_UvcsWarningBox = CreateUvcsWarningBox();
            m_UvcsWarningBox.style.display = BuildAutomationUVCSConnector.IsUvcsConfigured() ? DisplayStyle.None : DisplayStyle.Flex;
            root.Add(m_UvcsWarningBox);

            // Warning box if not connected to Unity Version Control
            m_ConfigFetchErrorBox = CreateConfigFetchErrorBox();
            m_ConfigFetchErrorBox.style.display = DisplayStyle.None;
            root.Add(m_ConfigFetchErrorBox);

            // Warning box if Unity Version is unsupported - start hidden, requires async API call to determine if it should be shown
            m_UnityVersionWarningBox = new HelpBox($"This version of Unity is not currently available for use with Build Automation. See the <a href=\"{BuildAutomationDashboardUrls.GetSupportedVersionsUrl()}\">Build Automation documentation</a> for more information", HelpBoxMessageType.Warning);
            m_UnityVersionWarningBox.AddToClassList(k_PlayerSettingsHelpBoxAdjustmentsClass);
            m_UnityVersionWarningBox.style.display = DisplayStyle.None;
            root.Add(m_UnityVersionWarningBox);

            // Warning box if platform is unsupported - start hidden, requires async API call to determine if it should be shown
            m_PlatformWarningBox = new HelpBox($"This Build Target is not currently available for use with Build Automation. See the <a href=\"{BuildAutomationDashboardUrls.GetSupportedPlatformsUrl()}\">Build Automation documentation</a> for more information", HelpBoxMessageType.Warning);
            m_PlatformWarningBox.AddToClassList(k_PlayerSettingsHelpBoxAdjustmentsClass);
            m_PlatformWarningBox.style.display = DisplayStyle.None;
            root.Add(m_PlatformWarningBox);

            if (CanFetchConfig())
            {
                // Kick off an async task to do final validations (Unity Version and Platform are supported by UBA)
                // If these final checks pass, fetch the config values from the UBA API and display the foldout
                lock (_lock)
                {
                    // Cancel any previous operation
                    _currentCancellationTokenSource?.Cancel();
                    _currentCancellationTokenSource?.Dispose();

                    // Create new cancellation token for the new operation
                    _currentCancellationTokenSource = new CancellationTokenSource();
                }
                FetchConfig(buildAutomationSettings, _currentCancellationTokenSource.Token);
            }

            return root;
        }

        private VisualElement CreateConfigFoldout(BuildTarget buildTarget)
        {
            var configFoldout = new Foldout
            {
                text = "Remote Builder Configuration",
                tooltip = "Settings for the remote machine used to build your project in the cloud",
                style =
                {
                    display = DisplayStyle.None,
                },
            };
            m_BuildAutomationBuilderConfig = new BuildAutomationBuilderConfig(m_ApiClient, serializedObject, buildTarget);
            configFoldout.Add(m_BuildAutomationBuilderConfig);

            m_BuildAutomationCredentialsConfig = new BuildAutomationCredentialsConfig(m_ApiClient, serializedObject);
            m_BuildAutomationCredentialsConfig.style.display = BuildAutomationUtilities.TargetRequiresCredentials(buildTarget) ? DisplayStyle.Flex : DisplayStyle.None;
            configFoldout.Add(m_BuildAutomationCredentialsConfig);

            return configFoldout;
        }

        private VisualElement CreateCloudConnectWarningBox()
        {
            var helpBox = new HelpBox($"Build Automation requires your project to be configured with Unity Cloud.", HelpBoxMessageType.Warning);
            helpBox.AddToClassList(k_PlayerSettingsHelpBoxAdjustmentsClass);
            var configureCloudButton = new Button { text = "Configure Unity Cloud" };
            configureCloudButton.clicked += () =>
            {
                SettingsService.OpenProjectSettings("Project/Services");
            };
            helpBox.Add(configureCloudButton);

            var retryButton = new Button { text = "Refresh" };
            retryButton.clicked += () =>
            {
                if (BuildAutomationUtilities.IsConnectedToUnityCloud())
                {
                    m_CloudWarningBox.style.display = DisplayStyle.None;
                    if (CanFetchConfig())
                    {
                        FetchConfig((BuildAutomationSettings)serializedObject.targetObject, CancellationToken.None);
                    }
                }
            };
            helpBox.Add(retryButton);

            return helpBox;
        }

        private VisualElement CreateUvcsWarningBox()
        {
            var helpBox = new HelpBox($"Build Automation requires your project to be configured with Unity Version Control.", HelpBoxMessageType.Warning);
            helpBox.AddToClassList(k_PlayerSettingsHelpBoxAdjustmentsClass);
            var button = new Button { text = "Configure Unity Version Control" };
            button.clicked += BuildAutomationUVCSConnector.ShowWindow;
            helpBox.Add(button);

            var retryButton = new Button { text = "Refresh" };
            retryButton.clicked += () =>
            {
                if (BuildAutomationUVCSConnector.IsUvcsConfigured())
                {
                    m_UvcsWarningBox.style.display = DisplayStyle.None;
                    if (CanFetchConfig())
                    {
                        FetchConfig((BuildAutomationSettings)serializedObject.targetObject, CancellationToken.None);
                    }
                }
            };
            helpBox.Add(retryButton);

            return helpBox;
        }

        private VisualElement CreateConfigFetchErrorBox()
        {
            var helpBox = new HelpBox($"Failed to Fetch Build Automation Configuration See console for more details.", HelpBoxMessageType.Error);
            helpBox.AddToClassList(k_PlayerSettingsHelpBoxAdjustmentsClass);
            var retryButton = new Button { text = "Retry" };
            retryButton.clicked += () =>
            {
                if (CanFetchConfig())
                {
                    m_ConfigFetchErrorBox.style.display = DisplayStyle.None;
                    FetchConfig((BuildAutomationSettings)serializedObject.targetObject, CancellationToken.None);
                }
            };
            helpBox.Add(retryButton);

            return helpBox;
        }

        private bool CanFetchConfig()
        {
            return BuildAutomationUVCSConnector.IsUvcsConfigured() && BuildAutomationUtilities.IsConnectedToUnityCloud();
        }

        async void FetchConfig(BuildAutomationSettings buildAutomationSettings, CancellationToken cancellationToken)
        {
            m_BuildAutomationLoadingLabel.style.display = DisplayStyle.Flex;
            try
            {
                var unityVersionSupported = await IsUnityVersionSupported();
                var platformSupported = await IsPlatformSupported(buildAutomationSettings.buildTarget);

                if (!unityVersionSupported)
                {
                    m_UnityVersionWarningBox.style.display = DisplayStyle.Flex;
                }

                if (!platformSupported)
                {
                    m_PlatformWarningBox.style.display = DisplayStyle.Flex;
                }

                if (unityVersionSupported && platformSupported)
                {
                    await m_BuildAutomationCredentialsConfig.FetchCredentials(cancellationToken);
                    await m_BuildAutomationBuilderConfig.FetchConfig(cancellationToken);
                    m_BuilderConfigurationFoldout.style.display = DisplayStyle.Flex;
                }
                m_BuildAutomationLoadingLabel.style.display = DisplayStyle.None;
            }
            catch (Exception e)
            {
                Debug.LogError($"Build Automation failed to fetch configuration - {e}");
                m_BuildAutomationLoadingLabel.style.display = DisplayStyle.None;
                m_ConfigFetchErrorBox.style.display = DisplayStyle.Flex;
            }
        }

        async Task<bool> IsUnityVersionSupported()
        {
            var unityVersions = await m_ApiClient.GetSupportedUnityVersions();
            var currentUnityVersion = BuildAutomationUtilities.GetUnityVersion();
            return unityVersions.Any(unityVersion => unityVersion.Value == currentUnityVersion);
        }

        async Task<bool> IsPlatformSupported(BuildTarget currentTarget)
        {
            var supportedPlatforms = await m_ApiClient.GetSupportedPlatforms(BuildAutomationUtilities.GetUnityVersion());
            return supportedPlatforms.Any(platform => platform.Platform.Equals(currentTarget.ToString(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
