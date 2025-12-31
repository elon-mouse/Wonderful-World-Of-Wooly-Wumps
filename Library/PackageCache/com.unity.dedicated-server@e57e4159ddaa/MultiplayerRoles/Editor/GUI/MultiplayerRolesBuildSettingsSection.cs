using Unity.DedicatedServer.Editor.Internal;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Multiplayer.Internal;

namespace Unity.Multiplayer.Editor
{
    internal class MultiplayerRoleBuildSettingsSection : IMultiplayerBuildOptionsSection
    {
        public int Order => 100;

        public void DrawBuildOptions(BuildProfile profile)
        {
            if (!EditorMultiplayerRolesManager.EnableMultiplayerRoles || profile == null)
                return;

            if (InternalUtility.IsClassicProfile(profile))
            {
                InternalUtility.GetBuildProfileInternalData(profile, out var buildSubtarget, out var buildTarget, out _);

                EditorGUI.BeginChangeCheck();
                var target = (UnityEngine.Multiplayer.Internal.MultiplayerRoleFlags)EditorGUILayout.EnumPopup(
                    "Multiplayer Role",
                    MultiplayerRolesSettings.instance.GetMultiplayerRoleForClassicTarget(buildTarget, buildSubtarget));

                if (EditorGUI.EndChangeCheck())
                    MultiplayerRolesSettings.instance.SetMultiplayerRoleForClassicTarget(buildTarget, buildSubtarget, target);

                return;
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                var target = (UnityEngine.Multiplayer.Internal.MultiplayerRoleFlags)EditorGUILayout.EnumPopup(
                    "Multiplayer Role",
                    MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(profile));

                if (EditorGUI.EndChangeCheck())
                    MultiplayerRolesSettings.instance.SetMultiplayerRoleForBuildProfile(profile, target);
            }
        }
    }
}
