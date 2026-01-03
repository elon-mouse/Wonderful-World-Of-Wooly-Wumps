using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Services.CloudBuild.Editor
{
    class BuildAutomationApiClient
    {
        const string k_BuildApi = "https://build-api.cloud.unity3d.com";

        string OrgEndpoint => $"{k_BuildApi}/api/v1/orgs/{CloudProjectSettings.organizationKey}";
        string ProjectEndpoint => $"{OrgEndpoint}/projects/{CloudProjectSettings.projectId}";

        public Task<ProjectInfo> GetProjectInfo()
        {
            return CreateJsonGetRequestAsync<ProjectInfo>(ProjectEndpoint);
        }

        public Task<List<ProjectBuildAttempt>> GetProjectBuildHistory(BuildAutomationHistoryOptions options)
        {
            var endpoint = $"{ProjectEndpoint}/buildtargets/_all/builds?{options.ToQueryParams()}";
            return CreateJsonGetRequestAsync<List<ProjectBuildAttempt>>(endpoint);
        }

        public Task<List<UnityVersion>> GetSupportedUnityVersions()
        {
            var endpoint = $"{k_BuildApi}/api/v1/versions/unity";
            return CreateJsonGetRequestAsync<List<UnityVersion>>(endpoint);
        }

        public Task<ProjectInfo> UpdateProject(ProjectInfo request)
        {
            return CreateJsonRequestAsync<ProjectInfo, ProjectInfo>(ProjectEndpoint, request,
                UnityWebRequest.kHttpVerbPUT);
        }

        public Task<List<ProjectBuildTarget>> GetProjectBuildTargets()
        {
            var endpoint = $"{ProjectEndpoint}/buildTargets";
            return CreateJsonGetRequestAsync<List<ProjectBuildTarget>>(endpoint);
        }

        public Task<ProjectBuildTarget> CreateBuildTarget(BuildAutomationTargetRequestDto request)
        {
            var endpoint = $"{ProjectEndpoint}/buildTargets/";
            return CreateJsonRequestAsync<BuildAutomationTargetRequestDto, ProjectBuildTarget>(endpoint, request,
                UnityWebRequest.kHttpVerbPOST);
        }

        public Task<ProjectBuildTarget> UpdateBuildTarget(string buildTargetId, BuildAutomationTargetRequestDto request)
        {
            var endpoint = $"{ProjectEndpoint}/buildTargets/{buildTargetId}";
            return CreateJsonRequestAsync<BuildAutomationTargetRequestDto, ProjectBuildTarget>(endpoint, request,
                UnityWebRequest.kHttpVerbPUT);
        }

        public Task<List<LaunchedBuildResponse>> StartBuild(
            ProjectBuildTarget buildTarget, LaunchBuildRequest request)
        {
            var endpoint = $"{ProjectEndpoint}/buildtargets/{buildTarget.Id}/builds";
            return CreateJsonRequestAsync<LaunchBuildRequest, List<LaunchedBuildResponse>>(endpoint, request,
                UnityWebRequest.kHttpVerbPOST);
        }

        public Task<UvcsTokenExchangeResponse> ExchangeUvcsToken()
        {
            var endpoint = $"{k_BuildApi}/api/v1/plastic/exchange";
            var encodedGenesisToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(CloudProjectSettings.accessToken));
            var plasticHeader = new RequestHeader { Key = "x-plastic", Value = encodedGenesisToken, };
            return CreateJsonRequestAsync<object, UvcsTokenExchangeResponse>(endpoint, new object(),
                UnityWebRequest.kHttpVerbPOST, plasticHeader);
        }

        public Task<GetConfigurationResponse> GetConfigOptions(string unityVersionValue)
        {
            var endpoint = $"{k_BuildApi}/api/v1/editor/config?unity_version={unityVersionValue}";
            return CreateJsonGetRequestAsync<GetConfigurationResponse>(endpoint);
        }

        public Task<List<GetCredentialsResponse>> GetCredentialsForPlatform(string platform)
        {
            var endpoint = $"{ProjectEndpoint}/credentials/signing/{platform}";
            return CreateJsonGetRequestAsync<List<GetCredentialsResponse>>(endpoint);
        }

        public Task<List<SupportedPlatform>> GetSupportedPlatforms(string unityVersion)
        {
            var endpoint = $"{k_BuildApi}/api/v1/platforms/supported?unityVersion={unityVersion}&editorOnly=true";
            return CreateJsonGetRequestAsync<List<SupportedPlatform>>(endpoint);
        }

        public async Task<bool> HasFreeTierBeenReached()
        {
            var orgId = CloudProjectSettings.organizationKey;
            var endpoint = $"{k_BuildApi}/api/v1/orgs/{orgId}/free-tier-status";
            var response = await CreateJsonGetRequestAsync<Dictionary<string, bool>>(endpoint);

            return response != null && response.TryGetValue("freeTierLimitReached", out bool limitReached) && limitReached;
        }

        async Task<T> CreateJsonGetRequestAsync<T>(string url)
        {
            var getRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            Authorize(getRequest);
            await getRequest.SendWebRequest();

            if (!WebRequestSucceeded(getRequest))
            {
                var exception = getRequest.downloadHandler is { text: not null }
                    ? new Exception(getRequest.downloadHandler.text)
                    : new Exception(getRequest.error);
                throw exception;
            }

            var deserializedObject = JsonConvert.DeserializeObject<T>(getRequest.downloadHandler.text);

            // if the Type is List<BuildAttemptArtifact> then we want to deserialize the response and add the content range to it.
            if (typeof(T) == typeof(List<ProjectBuildAttempt>)) {
                var buildAttempts = deserializedObject as List<ProjectBuildAttempt>;
                var contentRange = getRequest.GetResponseHeader("Content-Range");
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

            return deserializedObject;
        }

        async Task<TResponseType> CreateJsonRequestAsync<TRequestType, TResponseType>(string url, TRequestType request,
            string httpVerb, params RequestHeader[] optionalHeaders)
        {
            var payload = JsonConvert.SerializeObject(request, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            var webRequest = new UnityWebRequest(url, httpVerb)
            {
                downloadHandler = new DownloadHandlerBuffer(), uploadHandler = uploadHandler
            };
            webRequest.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
            foreach (var header in optionalHeaders)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }

            Authorize(webRequest);
            await webRequest.SendWebRequest();

            if (!WebRequestSucceeded(webRequest))
            {
                var exception = webRequest.downloadHandler is { text: not null }
                    ? new Exception(webRequest.downloadHandler.text)
                    : new Exception(webRequest.error);
                throw exception;
            }

            return JsonConvert.DeserializeObject<TResponseType>(webRequest.downloadHandler.text);
        }

        static bool WebRequestSucceeded(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.Success;
        }

        static void Authorize(UnityWebRequest request)
        {
            request.SetRequestHeader("AUTHORIZATION", $"Bearer {CloudProjectSettings.accessToken}");
        }
    }
}
