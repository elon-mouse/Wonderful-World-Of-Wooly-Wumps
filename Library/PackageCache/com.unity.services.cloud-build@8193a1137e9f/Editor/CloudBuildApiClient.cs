using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Services.Core.Editor;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Services.CloudBuild.Editor
{
    class CloudBuildApiClient
    {
        class CloudBuildApiConfig
        {
            [JsonProperty("build-api")]
            public string BuildApi { get; set; } = "https://build-api.cloud.unity3d.com";

            [JsonProperty("build")]
            public string Build { get; set; } = "https://dashboard.unity3d.com";
        }

        CdnConfiguredEndpoint<CloudBuildApiConfig> m_ClientConfig;

        public CloudBuildApiClient()
        {
            m_ClientConfig = new CdnConfiguredEndpoint<CloudBuildApiConfig>();
        }

        public void CancelBuilds(DeleteBuildsRequest request,
            Action<DeleteBuildsResponse> onSuccess, Action<Exception> onError)
        {
            CreateJsonRequest(GetEndPointUrl, request, UnityWebRequest.kHttpVerbPOST, onSuccess, onError);

            string GetEndPointUrl(CloudBuildApiConfig config)
            {
                return $"{config.BuildApi}/api/v1/orgs/{CloudProjectSettings.organizationKey}/projects/{CloudProjectSettings.projectId}/builds/cancel";
            }
        }

        public void DeleteBuilds(DeleteBuildsRequest request,
            Action<DeleteBuildsResponse> onSuccess, Action<Exception> onError)
        {
            CreateJsonRequest(GetEndPointUrl, request, UnityWebRequest.kHttpVerbPOST, onSuccess, onError);

            string GetEndPointUrl(CloudBuildApiConfig config)
            {
                return $"{config.BuildApi}/api/v1/orgs/{CloudProjectSettings.organizationKey}/projects/{CloudProjectSettings.projectId}/artifacts/delete";
            }
        }

        public void GetBuildLog(Action<string> onSuccess, Action<Exception> onError, string filePath, float buildId, string buildTarget)
        {
            var normalizedBuildTarget = BuildAutomationUtilities.NormalizedBuildTargetName(buildTarget);

            string GetEndPointUrl(CloudBuildApiConfig config)
            {
                return $"{config.BuildApi}/api/v1/orgs/{CloudProjectSettings.organizationKey}/projects/{CloudProjectSettings.projectId}/buildtargets/{normalizedBuildTarget}/builds/{buildId}/log";
            }

            m_ClientConfig.GetConfiguration(OnGetConfigurationCompleted);

            void OnGetConfigurationCompleted(CloudBuildApiConfig config)
            {
                var url = GetEndPointUrl(config);
                DownloadFile(url, filePath, OnDownloadSucceeded, OnDownloadFailed);
            }

            void OnDownloadSucceeded(string text)
            {
                Debug.Log("Download succeeded, saving log content.");
            }

            void OnDownloadFailed(Exception error)
            {
                Debug.LogError($"Download failed: {error.Message}");
                onError?.Invoke(error);
            }
        }

        public void DownloadFile(string url, string filePath, Action<string> onSuccess, Action<Exception> onError)
        {
            CreateGetDownloadRequest(url, filePath, onSuccess, onError);
        }

        void CreateJsonGetRequest<T>(
            Func<CloudBuildApiConfig, string> endpointConstructor, Action<T> onSuccess, Action<Exception> onError)
        {
            m_ClientConfig.GetConfiguration(OnGetConfigurationCompleted);

            void OnGetConfigurationCompleted(CloudBuildApiConfig configuration)
            {
                try
                {
                    var url = endpointConstructor(configuration);
                    var getRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
                    {
                        downloadHandler = new DownloadHandlerBuffer()
                    };
                    Authorize(getRequest);
                    getRequest.SendWebRequest().completed += CreateJsonResponseHandler(onSuccess, onError);
                }
                catch (Exception reason)
                {
                    onError?.Invoke(reason);
                }
            }
        }

        void CreateGetDownloadRequest(
            string url, string filePath, Action<string> onSuccess, Action<Exception> onError)
        {
            m_ClientConfig.GetConfiguration(OnGetConfigurationCompleted);

            void OnGetConfigurationCompleted(CloudBuildApiConfig configuration)
            {
                try
                {
                    var fileName = Path.GetFileName(filePath);
                    var getRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
                    {
                        downloadHandler = new DownloadHandlerFile(filePath)
                    };

                    Authorize(getRequest);

                    getRequest.SendWebRequest().completed += CreateFileResponseHandler(onSuccess, onError);
                    while (!getRequest.isDone)
                    {
                        EditorUtility.DisplayProgressBar("Build Automation File Download", $"Downloading {fileName} to: {filePath}", getRequest.downloadProgress);
                    }
                    EditorUtility.ClearProgressBar();
                }
                catch (Exception reason)
                {
                    onError?.Invoke(reason);
                }
            }
        }

        void CreateJsonRequest<TRequestType, TResponseType>(
            Func<CloudBuildApiConfig, string> endpointConstructor, TRequestType request, string httpVerb,
            Action<TResponseType> onSuccess, Action<Exception> onError)
        {
            m_ClientConfig.GetConfiguration(OnGetConfigurationCompleted);

            void OnGetConfigurationCompleted(CloudBuildApiConfig configuration)
            {
                try
                {
                    var url = endpointConstructor(configuration);
                    var payload = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                    var webRequest = new UnityWebRequest(url, httpVerb)
                    {
                        downloadHandler = new DownloadHandlerBuffer(),
                        uploadHandler = uploadHandler
                    };
                    webRequest.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
                    Authorize(webRequest);
                    webRequest.SendWebRequest().completed += CreateJsonResponseHandler(onSuccess, onError);
                }
                catch (Exception reason)
                {
                    onError?.Invoke(reason);
                }
            }
        }

        static Action<AsyncOperation> CreateFileResponseHandler(Action<string> onSuccess, Action<Exception> onError)
        {
            return FileResponseHandler;

            void FileResponseHandler(AsyncOperation unityOperation)
            {
                var callbackWebRequest = ((UnityWebRequestAsyncOperation)unityOperation).webRequest;
                if (WebRequestSucceeded(callbackWebRequest))
                {
                    onSuccess?.Invoke("File successfully downloaded");
                }
                else
                {
                    Debug.Log("File download failed");
                    onError?.Invoke(new Exception("File download failed"));
                }

                callbackWebRequest.Dispose();
            }
        }
        static Action<AsyncOperation> CreateJsonResponseHandler<T>(Action<T> onSuccess, Action<Exception> onError)
        {
            return JsonResponseHandler;

            void JsonResponseHandler(AsyncOperation unityOperation)
            {
                var callbackWebRequest = ((UnityWebRequestAsyncOperation)unityOperation).webRequest;
                if (WebRequestSucceeded(callbackWebRequest))
                {
                    try
                    {
                        var deserializedObject = JsonConvert.DeserializeObject<T>(
                            callbackWebRequest.downloadHandler.text);
                        // if the Type is List<BuildAttemptArtifact> then we want to deserialize the response and add the content range to it.
                        if (typeof(T) == typeof(List<ProjectBuildAttempt>)) {
                            var buildAttempts = deserializedObject as List<ProjectBuildAttempt>;
                            var contentRange = callbackWebRequest.GetResponseHeader("Content-Range");
                            var totalItems = 0;

                            try
                            {
                                totalItems = int.Parse(contentRange.Split('/')[1].Trim());
                            }
                            catch (FormatException e)
                            {
                                Debug.LogError("Could not parse string as float: " + e.Message);
                            }

                            var items = contentRange.Split(" ")[1].Split("/")[0].Trim();

                            foreach (var buildAttempt in buildAttempts)
                            {
                                buildAttempt.TotalAttempts = totalItems;
                                buildAttempt.AttemptRange = items;
                            }
                        }
                        onSuccess?.Invoke(deserializedObject);
                    }
                    catch (Exception deserializeError)
                    {
                        onError?.Invoke(deserializeError);
                    }
                }
                else
                {
                    onError?.Invoke(callbackWebRequest.downloadHandler is { text: not null }
                        ? new Exception(callbackWebRequest.downloadHandler.text)
                        : new Exception(callbackWebRequest.error));
                }

                callbackWebRequest.Dispose();
            }
        }

        static bool WebRequestSucceeded(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.Success;
#else
            return request.isDone && !request.isHttpError && !request.isNetworkError;
#endif
        }

        static void Authorize(UnityWebRequest request)
        {
            request.SetRequestHeader("AUTHORIZATION", $"Bearer {CloudProjectSettings.accessToken}");
        }
    }
}
