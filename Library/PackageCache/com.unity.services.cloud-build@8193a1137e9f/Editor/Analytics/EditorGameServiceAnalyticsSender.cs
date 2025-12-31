using System;
using Unity.Services.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.CloudBuild.Editor
{
    static class EditorGameServiceAnalyticsSender
    {
        static class AnalyticsComponent
        {
            public const string ProjectSettings = "Project Settings";
            public const string TopMenu = "Top Menu";
        }

        static class AnalyticsAction
        {
            public const string Configure = "Configure";
            public const string UploadBuild = "Upload Build";
            public const string ManageTarget = "Manage Target";
            public const string BuildHistory = "Build History";
            public const string Build = "Build";
            public const string BuildNotifications = "Build Notifications";
            public const string LearnMore = "Learn More";
        }

        const int k_Version = 1;
        const string k_EventName = "editorgameserviceeditor";

        static IEditorGameServiceIdentifier s_Identifier;

        static IEditorGameServiceIdentifier Identifier
        {
            get
            {
                if (s_Identifier == null)
                {
                    s_Identifier = EditorGameServiceRegistry.Instance.GetEditorGameService<BuildAutomationIdentifier>().Identifier;
                }
                return s_Identifier;
            }
        }

        internal static void SendProjectSettingsUploadBuildEvent()
        {
            SendEvent(AnalyticsComponent.ProjectSettings, AnalyticsAction.UploadBuild);
        }

        internal static void SendProjectSettingsManageTargetEvent()
        {
            SendEvent(AnalyticsComponent.ProjectSettings, AnalyticsAction.ManageTarget);
        }

        internal static void SendProjectSettingsBuildHistoryEvent()
        {
            SendEvent(AnalyticsComponent.ProjectSettings, AnalyticsAction.BuildHistory);
        }

        internal static void SendProjectSettingsBuildEvent()
        {
            SendEvent(AnalyticsComponent.ProjectSettings, AnalyticsAction.Build);
        }

        internal static void SendProjectSettingsBuildNotificationsEvent(bool enabled)
        {
            var notificationsState = enabled ? "ON" : "OFF";
            SendEvent(AnalyticsComponent.ProjectSettings, $"{AnalyticsAction.BuildNotifications} {notificationsState}");
        }

        internal static void SendProjectSettingsLearnMoreEvent()
        {
            SendEvent(AnalyticsComponent.ProjectSettings, AnalyticsAction.LearnMore);
        }

        internal static void SendTopMenuConfigureEvent()
        {
            SendEvent(AnalyticsComponent.TopMenu, AnalyticsAction.Configure);
        }

        static void SendEvent(string component, string action)
        {
            EditorAnalytics.SendEventWithLimit(k_EventName, new EditorGameServiceEvent
            {
                action = action,
                component = component,
                package = Identifier.GetKey()
            }, k_Version);
        }

        /// <remarks>Lowercase is used here for compatibility with analytics.</remarks>
        [Serializable]
        public struct EditorGameServiceEvent
        {
            public string action;
            public string component;
            public string package;
        }
    }
}
