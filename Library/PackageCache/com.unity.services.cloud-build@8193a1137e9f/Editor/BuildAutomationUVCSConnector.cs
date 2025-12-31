using System;
using System.IO;
using System.Reflection;
using UnityEngine;

using Codice.Client.BaseCommands;
using Codice.Client.Common;
using Codice.CM.Common;
using PlasticGui;
using UnityEditor;

namespace Unity.Services.CloudBuild.Editor
{
    internal static class BuildAutomationUVCSConnector
    {
        const string k_UvcsPackageAssemblyName = "Unity.PlasticSCM.Editor";
        const string k_UvcsPackageTypeName = "Unity.PlasticSCM.Editor.UI.ShowWindow";
        const string k_UvcsShowWindowMethodName = "Plastic";
        internal static string Server => GetServer();
        internal static string Branch => GetBranch();
        internal static string Repo => GetRepo();
        internal static string CurrentUser => GetCurrentUser();

        internal static string ClientPath => GetClientPath();

        internal static bool IsUvcsConfigured()
        {
            return FindWorkspace.HasWorkspace(Application.dataPath);
        }

        internal static WorkspaceInfo GetWorkspaceInfo()
        {
            return FindWorkspace.InfoForApplicationPath(Application.dataPath, mPlasticApi);
        }

        static string GetBranch()
        {
            var selector = GetSelector();
            var branchName = selector.BranchName;

            // Remove any initial slash
            if (branchName[0] == '/' || branchName[0] == '\\')
            {
                branchName = branchName.Substring(1);
            }

            return branchName;
        }

        static string GetCurrentUser()
        {
            var server = GetServer();
            return mPlasticApi.GetCurrentUser(server)?.Name;
        }

        static string GetRepo()
        {
            var selector = GetSelector();
            var repSpec = selector?.RepSpec; // RepSpec = repo@server
            if (string.IsNullOrEmpty(repSpec) || repSpec.IndexOf('@') <= 0)
            {
                return string.Empty;
            }
            return repSpec.Substring(0,repSpec.IndexOf('@'));
        }

        static string GetServer()
        {
            var selector = GetSelector();
            var repSpec = selector?.RepSpec; // RepSpec = repo@server
            if (string.IsNullOrEmpty(repSpec) || repSpec.IndexOf('@') <= 0)
            {
                return string.Empty;
            }
            return repSpec.Substring(repSpec.IndexOf('@') + 1);
        }

        internal static void ShowWindow()
        {
            var assemblyQualifiedName = $"{k_UvcsPackageTypeName}, {k_UvcsPackageAssemblyName}";
            var packageType = Type.GetType(assemblyQualifiedName);
            var showWindowMethod = packageType?.GetMethod(
                k_UvcsShowWindowMethodName,
                BindingFlags.NonPublic | BindingFlags.Static);

            if (showWindowMethod == null)
            {
                Debug.LogError("Failed to open UVCS Window. If you have UVCS installed, you can manually open this window by going to Window -> Unity Version Control.");
            }

            showWindowMethod?.Invoke(null, new object[] {});
        }

        internal static string GetUnityProjectSubfolder()
        {
            // Get the Unity project directory (parent of the Assets folder)
            string unityProjectPath = Path.GetDirectoryName(Application.dataPath);

            var workspaceInfo = FindWorkspace.InfoForApplicationPath(Application.dataPath, mPlasticApi);

            if (workspaceInfo == null)
            {
                Debug.LogError("Workspace not found for the Unity project.");
                return null;
            }

            string workspaceRoot = workspaceInfo.ClientPath;

            if (string.IsNullOrEmpty(workspaceRoot))
            {
                Debug.LogError("Failed to determine the UVCS workspace root.");
                return null;
            }

            string relativeSubfolder = Path.GetRelativePath(workspaceRoot, unityProjectPath);

            // Return an empty string if the project is in the root
            if (relativeSubfolder == ".")
            {
                return string.Empty;
            }

            return relativeSubfolder;
        }

        static string GetClientPath()
        {
            var wkInfo = FindWorkspace.InfoForApplicationPath(
                Application.dataPath, mPlasticApi);
            return wkInfo.ClientPath;
        }

        static SelectorInformation GetSelector()
        {
            ClientHandlers.Register();
            var wkInfo = FindWorkspace.InfoForApplicationPath(
                Application.dataPath, mPlasticApi);
            return mPlasticApi.GetSelectorUserInformation(wkInfo);
        }

        internal static bool HasPendingChanges()
        {
            var workspaceInfo = GetWorkspaceInfo();
            return mPlasticApi.HasPendingChangesBeforeShelving(workspaceInfo);
        }

        internal static string ShelveChanges()
        {
            var workspaceInfo = GetWorkspaceInfo();
            var shelveId = mPlasticApi.ShelvePendingChanges(workspaceInfo, "Shelve and Build from the Editor");

            if (shelveId == -1)
            {
                Debug.LogError("Failed to shelve changes: An error occurred during the 'Shelve and Build' operation.");
                return string.Empty;
            }

            return Math.Abs(shelveId).ToString();
        }

        static class FindWorkspace
        {
            internal static bool HasWorkspace(string path)
            {
                string wkPath = PathForApplicationPath(path);

                return !string.IsNullOrEmpty(wkPath);
            }

            internal static string PathForApplicationPath(string path)
            {
                try
                {
                    return FindWorkspacePath(path, ClientConfig.Get().GetWkConfigDir());
                }
                catch (NotConfiguredClientException)
                {
                    return null;
                }
            }

            internal static WorkspaceInfo InfoForApplicationPath(string path, IPlasticAPI plasticApi)
            {
                string wkPath = PathForApplicationPath(path);
                if (string.IsNullOrEmpty(wkPath))
                    return null;

                return plasticApi.GetWorkspaceFromPath(wkPath);
            }

            static string FindWorkspacePath(string path, string wkConfigDir)
            {
                while (!string.IsNullOrEmpty(path))
                {
                    if (Directory.Exists(Path.Combine(path, wkConfigDir)))
                        return path;

                    path = Path.GetDirectoryName(path);
                }

                return null;
            }
        }

        static IPlasticAPI mPlasticApi = new PlasticAPI();
    }

}
