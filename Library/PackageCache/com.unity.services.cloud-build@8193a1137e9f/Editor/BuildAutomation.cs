using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using System.Threading.Tasks;
using Exception = System.Exception;

namespace Unity.Services.CloudBuild.Editor
{
    internal class DialogueText
    {
        public string Title;
        public string Message;
        public string Ok;
        public string Cancel;
    }

    /// <summary>
    /// Provides a collection of methods to interact with the Build Automation Service
    /// </summary>
    public class BuildAutomation
    {
        static BuildAutomationApiClient APIClient { get; } = new();

        const string k_MessageLaunchedBuildSuccess = "Build #{0} {1} successfully added to queue";
        const string k_MessageLaunchedBuildFailedWithMsg = "Build {0} failed to start: {1}";
        const string k_CancelText = "Cancel";
        const string k_ContinueText = "Continue";
        const string k_StartBuildFailedTitle = "Failed to Start Cloud Build";

        const string k_ConfigureUvcsMessage =
            "Your project must be configured with Unity Version Control in order to start a Cloud Build.";

        const string k_ConfigureCloudMessage =
            "Your project must be configured with Unity Cloud in order to start a Cloud Build.";

        const string k_MissingSettingsMessage =
            "You must add Build Automation Settings to your Build Profile in order to start a Cloud Build.";

        const string k_UnsupportedPlatformMessage =
            "Unable to start a Cloud Build. This profile's Build Target is not currently supported by Build Automation.";

        const string k_UnsupportedUnityVersionMessage =
            "Unable to start a Cloud Build. This version of the Unity Editor is not currently supported by Build Automation.";

        const string k_InvalidSettingsMessage =
            "Configured Build Automation Settings are invalid. {0}";

        static readonly DialogueText k_PendingChangesDialogue = new()
        {
            Title = "You have Pending Changes",
            Message = "You have some pending changes. Before starting a Cloud Build, we'll create a new shelve with your outstanding changes, and build that shelveset.",
            Ok = "Shelve and Build",
            Cancel = k_CancelText,
        };

        static readonly DialogueText k_NoPendingChangesDialogue = new()
        {
            Title = "No Pending Changes",
            Message = "You have no changes in your current workspace. Starting a Cloud Build will build the latest version of your current changeset.",
            Ok = "Build Current Changeset",
            Cancel = k_CancelText,
        };

        static readonly DialogueText k_StartBuildFailedDialogue = new()
        {
            Title = k_StartBuildFailedTitle,
            Ok = k_ContinueText,
        };

        /// <summary>
        /// Starts a Cloud Build for the provided Build Profile using Build Automation
        /// </summary>
        /// <param name="buildProfile">The build profile to start a Cloud Build for</param>
        public static async void LaunchBuild(BuildProfile buildProfile)
        {
            var canBuildResults = await CanStartBuild(buildProfile);
            if (canBuildResults.FailureReason != BuildStartFailureReason.None)
            {
                DisplayBuildStartFailureDialogue(canBuildResults);
                return;
            }

            var buildAutomationSettings = GetBuildAutomationSubAsset(buildProfile);
            var hasPendingChanges = BuildAutomationUVCSConnector.HasPendingChanges();
            bool shouldContinue = hasPendingChanges ?
                ShowPendingChangesDialogue(k_PendingChangesDialogue) :
                ShowBuildDialogue(k_NoPendingChangesDialogue);

            if (!shouldContinue)
            {
                return;
            }

            var shelveSetId = "";
            if (hasPendingChanges)
            {
                shelveSetId = await Task.Run(BuildAutomationUVCSConnector.ShelveChanges);
            }

            try
            {
                EditorUtility.DisplayProgressBar("Starting Cloud Build", "Fetching Remote Project Info...", 0.1f);
                var projectInfo = await APIClient.GetProjectInfo();

                if (projectInfo.Settings.Scm?.Url != BuildAutomationUVCSConnector.Server)
                {
                    EditorUtility.DisplayProgressBar("Starting Cloud Build", "Updating Remote Project SCM Settings...", 0.2f);
                    var uvcsTokenObject = await APIClient.ExchangeUvcsToken();
                    var projectUpdateRequest = new ProjectInfo
                    {
                        Settings = new ProjectSettings
                        {
                            Scm = new ProjectScmSettings
                            {
                                Type = "plastic",
                                Url = BuildAutomationUVCSConnector.Server,
                                User = BuildAutomationUVCSConnector.CurrentUser,
                                AuthType = "SSOWorkingMode",
                                PlasticAccessToken = uvcsTokenObject.AccessToken,
                                UseEncryption = false,
                                WindowsGitBinary = "native",
                            },
                        }
                    };
                    await APIClient.UpdateProject(projectUpdateRequest);
                }

                EditorUtility.DisplayProgressBar("Starting Cloud Build", "Fetching Remote Build Targets...", 0.4f);

                var buildTargets = await APIClient.GetProjectBuildTargets();
                var foundTargetId = "";
                var expectedTargetName = BuildAutomationUtilities.GetBuildTargetNameFromProfile(buildProfile);
                foreach (var target in buildTargets)
                {
                    if (string.Equals(target.Name.Trim(), expectedTargetName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        foundTargetId = target.Id;
                        break;
                    }
                }

                var requestDto = new BuildAutomationTargetRequestDto(buildProfile, buildAutomationSettings);
                ProjectBuildTarget buildTarget;
                if (string.IsNullOrEmpty(foundTargetId))
                {
                    EditorUtility.DisplayProgressBar("Starting Cloud Build", "Creating new remote Build Target...", 0.7f);
                    buildTarget = await APIClient.CreateBuildTarget(requestDto);
                }
                else
                {
                    EditorUtility.DisplayProgressBar("Starting Cloud Build", "Updating remote Build Target...", 0.7f);
                    buildTarget = await APIClient.UpdateBuildTarget(foundTargetId, requestDto);
                }

                EditorUtility.DisplayProgressBar("Starting Cloud Build", "Launching Build...", 0.95f);

                var startedBuilds = await APIClient.StartBuild(buildTarget, new LaunchBuildRequest { Clean = false, CausedBy = "editor", ShelvesetID = shelveSetId});
                EditorUtility.ClearProgressBar();

                foreach (var build in startedBuilds)
                {
                    if (!string.IsNullOrEmpty(build.Error))
                    {
                        var message = string.Format(L10n.Tr(k_MessageLaunchedBuildFailedWithMsg), expectedTargetName, build.Error);
                        k_StartBuildFailedDialogue.Message = message;
                        ShowDialogue(k_StartBuildFailedDialogue);
                    }
                    else
                    {
                        var message = string.Format(L10n.Tr(k_MessageLaunchedBuildSuccess), build.Build, expectedTargetName);
                        BuildAutomationHistoryWindow.ShowWindow();
                        Debug.Log(message);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }

        private static async Task<BuildStartResults> CanStartBuild(BuildProfile buildProfile)
        {
            if (buildProfile == null)
            {
                throw new ArgumentNullException();
            }

            var buildStartResults = new BuildStartResults
            {
                FailureReason = BuildStartFailureReason.None,
                DialogueText = new DialogueText
                {
                    Title = k_StartBuildFailedTitle,
                    Ok = k_ContinueText,
                },
            };

            if (!BuildAutomationUtilities.IsConnectedToUnityCloud())
            {
                buildStartResults.FailureReason = BuildStartFailureReason.Cloud;
                buildStartResults.DialogueText.Message = k_ConfigureCloudMessage;
                buildStartResults.DialogueText.Ok = "Open Cloud Settings";
                buildStartResults.DialogueText.Cancel = k_CancelText;
                return buildStartResults;
            }

            if (!BuildAutomationUVCSConnector.IsUvcsConfigured())
            {
                buildStartResults.FailureReason = BuildStartFailureReason.Uvcs;
                buildStartResults.DialogueText.Message = k_ConfigureUvcsMessage;
                buildStartResults.DialogueText.Ok = "Open UVCS Settings";
                buildStartResults.DialogueText.Cancel = k_CancelText;
                return buildStartResults;
            }

            var buildAutomationSettings = GetBuildAutomationSubAsset(buildProfile);
            if (buildAutomationSettings == null)
            {
                buildStartResults.FailureReason = BuildStartFailureReason.Settings;
                buildStartResults.DialogueText.Message = k_MissingSettingsMessage;
                return buildStartResults;
            }

            var supportedUnityVersions = await APIClient.GetSupportedUnityVersions();
            var unityVersionValues = supportedUnityVersions.Select(u => u.Value);
            if (!unityVersionValues.Contains(BuildAutomationUtilities.GetUnityVersion()))
            {
                buildStartResults.FailureReason = BuildStartFailureReason.UnityVersion;
                buildStartResults.DialogueText.Message = k_UnsupportedUnityVersionMessage;
                return buildStartResults;
            }

            var supportedPlatforms = await APIClient.GetSupportedPlatforms(BuildAutomationUtilities.GetUnityVersion());
            var platformValues = supportedPlatforms.Select(p => p.Platform.ToLower());
            if (!platformValues.Contains(buildAutomationSettings.buildTarget.ToString().ToLower()))
            {
                buildStartResults.FailureReason = BuildStartFailureReason.Platform;
                buildStartResults.DialogueText.Message = k_UnsupportedPlatformMessage;
                return buildStartResults;
            }

            if (string.IsNullOrEmpty(buildAutomationSettings.operatingSystemValue))
            {
                buildStartResults.FailureReason = BuildStartFailureReason.Settings;
                buildStartResults.DialogueText.Message = string.Format(L10n.Tr(k_InvalidSettingsMessage), "Missing required field Operating System.");

                return buildStartResults;
            }

            if (string.IsNullOrEmpty(buildAutomationSettings.machineTypeId))
            {
                buildStartResults.FailureReason = BuildStartFailureReason.Settings;
                buildStartResults.DialogueText.Message = string.Format(L10n.Tr(k_InvalidSettingsMessage), "Missing required field Machine Type.");
                return buildStartResults;
            }

            if (buildAutomationSettings.operatingSystemFamily == "mac" && string.IsNullOrEmpty(buildAutomationSettings.xcodeVersion))
            {
                buildStartResults.FailureReason = BuildStartFailureReason.Settings;
                buildStartResults.DialogueText.Message = string.Format(L10n.Tr(k_InvalidSettingsMessage), "Missing required field Xcode Version.");
                return buildStartResults;
            }

            if (BuildAutomationUtilities.TargetRequiresCredentials(buildAutomationSettings.buildTarget) && string.IsNullOrEmpty(buildAutomationSettings.credentialsId))
            {
                buildStartResults.FailureReason = BuildStartFailureReason.Settings;
                buildStartResults.DialogueText.Message = string.Format(L10n.Tr(k_InvalidSettingsMessage), "Missing required field Credentials.");
                return buildStartResults;
            }

            if (buildAutomationSettings.buildTarget == BuildTarget.Android && string.IsNullOrEmpty(buildAutomationSettings.androidSdkVersion))
            {
                buildStartResults.FailureReason = BuildStartFailureReason.Settings;
                buildStartResults.DialogueText.Message = string.Format(L10n.Tr(k_InvalidSettingsMessage), "Missing required field Android SDK Version.");
                return buildStartResults;
            }

            return buildStartResults;
        }

        static bool ShowDialogue(DialogueText dialogueText)
        {
            return EditorUtility.DisplayDialog(dialogueText.Title, dialogueText.Message, dialogueText.Ok, dialogueText.Cancel);
        }

        static bool ShowBuildDialogue(DialogueText dialogueText)
        {
            return EditorUtility.DisplayDialog(dialogueText.Title, dialogueText.Message, dialogueText.Ok, dialogueText.Cancel, DialogOptOutDecisionType.ForThisMachine, "build_automation_confirm_build");
        }
        static bool ShowPendingChangesDialogue(DialogueText dialogueText)
        {
            int choice = EditorUtility.DisplayDialogComplex(
                dialogueText.Title,
                dialogueText.Message,
                dialogueText.Ok,
                dialogueText.Cancel,
                "Learn More");

            switch (choice)
            {
                case 0: // Shelve and Build pressed
                    return true;
                case 1: // Cancel pressed
                    return false;
                case 2: // "Learn More" pressed
                    Application.OpenURL("https://docs.unity.com/ugs/manual/devops/manual/build-automation/overview/build-automation-editor-package");
                    return false;
                default:
                    return false;
            }
        }

        static BuildAutomationSettings GetBuildAutomationSubAsset(BuildProfile buildProfile)
        {
            var path = AssetDatabase.GetAssetPath(buildProfile);
            var objects = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in objects)
            {
                if (obj == null)
                    continue;

                if (obj is BuildAutomationSettings settings)
                    return settings;
            }
            return null;
        }

        static void DisplayBuildStartFailureDialogue(BuildStartResults buildStartResults)
        {
            if (ShowDialogue(buildStartResults.DialogueText))
            {
                if (buildStartResults.FailureReason == BuildStartFailureReason.Uvcs)
                {
                    BuildAutomationUVCSConnector.ShowWindow();
                }
                else if (buildStartResults.FailureReason == BuildStartFailureReason.Cloud)
                {
                    SettingsService.OpenProjectSettings("Project/Services/Build Automation");
                }
            }
        }

        private class BuildStartResults
        {
            internal DialogueText DialogueText { get; set; }

            internal BuildStartFailureReason FailureReason { get; set; }
        }

        private enum BuildStartFailureReason
        {
            None,
            Uvcs,
            Cloud,
            Settings,
            UnityVersion,
            Platform,
        }
    }
}
