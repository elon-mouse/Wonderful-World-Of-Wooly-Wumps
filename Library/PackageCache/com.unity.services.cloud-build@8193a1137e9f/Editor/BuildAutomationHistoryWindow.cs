using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudBuild.Editor.Components;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.CloudBuild.Editor
{
    internal class BuildAutomationHistoryWindow : EditorWindow
    {
        [SerializeField] VisualTreeAsset m_VisualTreeAsset = default;

        const string k_Uss = "Packages/com.unity.services.cloud-build/Editor/USS/BuildAutomationHistory.uss";

        const string k_RefreshButton = "RefreshButton";
        const string k_ClearFiltersButton = "ClearFiltersButton";
        const string k_SettingsMenu = "SettingsMenu";
        const string k_BuildsSelectedLabel = "ItemsSelectedLabel";
        const string k_BuildsSelectedTextPlural = "Items Selected";
        const string k_BuildsSelectedTextSingular = "Item Selected";
        const string k_CancelBuildsButton = "CancelButton";
        const string k_DeleteBuildsButton = "DeleteButton";
        const string k_UpgradeButton = "UpgradeButton";
        const string k_StatusFilter = "StatusFilter";
        const string k_PlatformFilter = "PlatformFilter";
        const string k_ProfileFilter = "ProfileFilter";
        const string k_DisplayFilter = "DisplayFilter";
        const string k_SearchBar = "HistorySearch";
        const string k_FreeTierBanner = "FreeTierBanner";
        const string k_PricingLink = "PricingLink";

        const string k_OnlyMyBuildsLabel = "Only Show My Builds";
        const string k_OnlyMyBuildsValue = "onlyMyBuilds";

        const string k_ErrorProjectStateMismatch = "There is a mismatch between local and web configuration for Unity Build Automation. Please open the Unity Build Automation web dashboard and enable the current project.";

        MultiColumnListView m_MultiColumnListView;
        List<Toggle> m_buildSelectors = new();
        Toggle m_parentBuildSelector;
        List<ProjectBuildAttempt> m_selectedBuilds = new();
        CloudBuildApiClient m_apiClient { get; } = new();
        BuildAutomationHistoryOptions m_AutomationHistoryOptions = new();
        BuildAutomationApiClient m_asyncApiClient;
        Timer m_pollerTimer;
        bool m_isPolling;

        [MenuItem("Window/Unity Build Automation/Build History")]
        internal static void ShowWindow()
        {
            var wnd = GetWindow<BuildAutomationHistoryWindow>();
            wnd.titleContent = new GUIContent("Build History");
        }

        internal async void CreateGUI()
        {
            m_VisualTreeAsset.CloneTree(rootVisualElement);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_Uss);
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            m_asyncApiClient = new BuildAutomationApiClient();
            var supportedPlatforms = await m_asyncApiClient.GetSupportedPlatforms(BuildAutomationUtilities.GetUnityVersion());

            // Setup static components (page buttons, settings button, refresh button, etc..)
            SetupStaticComponents(supportedPlatforms);

            var buildHistory = await FetchBuildHistory();
            SetupBuildHistory(buildHistory);

            await SetupBillingBanner();

            // Start the poller
            StartBuildHistoryPoller();
        }

        async Task SetupBillingBanner()
        {
            var freeTierBanner = rootVisualElement.Q<VisualElement>(k_FreeTierBanner);
            var showBanner = await m_asyncApiClient.HasFreeTierBeenReached();
            if (showBanner)
            {
                freeTierBanner.style.display = DisplayStyle.Flex;
                var pricingLink = rootVisualElement.Q<Label>(k_PricingLink);
                pricingLink.RegisterCallback<ClickEvent>(evt =>
                {
                    Application.OpenURL(BuildAutomationDashboardUrls.GetPlanComparisonUrl());
                });
            }
        }

        async Task<List<ProjectBuildAttempt>> FetchBuildHistory()
        {
            var projectInfo = await m_asyncApiClient.GetProjectInfo();
            if (projectInfo.Disabled)
            {
                Debug.LogError(L10n.Tr(k_ErrorProjectStateMismatch));
                return new List<ProjectBuildAttempt>();
            }

            var buildAttempts =
                await m_asyncApiClient.GetProjectBuildHistory(m_AutomationHistoryOptions);

            return buildAttempts;
        }

        void StartBuildHistoryPoller()
        {
            m_isPolling = false;
            m_pollerTimer = new Timer(
                _ =>
                {
                    m_isPolling = true;
                    // Ensures thread-safety with Unity's main thread
                    EditorApplication.delayCall += () => {
                        RefreshBuildHistory();
                        m_isPolling = false;
                    };
                },
                null,
                TimeSpan.FromMinutes(1), // Start First poll 1 minute later
                TimeSpan.FromMinutes(1)); // Refresh every 1 minute
        }

        void StopBuildHistoryPoller()
        {
            m_pollerTimer?.Dispose();
            m_pollerTimer = null;
        }

        // OnDestroy is automatically called when build history window is closed
        void OnDestroy()
        {
            StopBuildHistoryPoller();
        }

        void OnDisable()
        {
            StopBuildHistoryPoller();
        }

        void SetupBuildHistory(List<ProjectBuildAttempt> buildAttempts)
        {
            if (buildAttempts != null && buildAttempts.Count > 0)
            {
                var firstAttempt = buildAttempts[0];
                var rangeLabel = rootVisualElement.Q<TextElement>("builds-page-info");
                rangeLabel.text = $"{firstAttempt.AttemptRange} of {firstAttempt.TotalAttempts}";
                try
                {
                    var firstItem = int.Parse(firstAttempt.AttemptRange.Split("-")[0]);
                    var pageBackButton = rootVisualElement.Q<Button>("PageBack");
                    pageBackButton.SetEnabled(firstItem != 0);
                }
                catch (Exception)
                {
                    // SWALLOW
                }

                try
                {
                    var lastItem = int.Parse(firstAttempt.AttemptRange.Split("-")[1]);
                    var pageForwardButton = rootVisualElement.Q<Button>("NextPage");
                    pageForwardButton.SetEnabled(lastItem < firstAttempt.TotalAttempts);
                }
                catch (Exception)
                {
                    // SWALLOW
                }
            }

            // For each column, set Column.makeCell to initialize each cell in the column
            // The index, platform, logs, and downloads columns all use cellTemplates instead of makeCell, since they
            // have more complex layouts
            m_MultiColumnListView.columns["profile"].makeCell = () => new Label();
            m_MultiColumnListView.columns["user"].makeCell = () => new Label();
            m_MultiColumnListView.columns["changeset"].makeCell = () => new Label();
            m_MultiColumnListView.columns["branch"].makeCell = () => new Label();
            m_MultiColumnListView.columns["startedAt"].makeCell = () => new Label();
            m_MultiColumnListView.columns["finishedAt"].makeCell = () => new Label();
            m_MultiColumnListView.columns["index"].makeHeader = () => new VisualElement();
            m_MultiColumnListView.columns["index"].bindHeader = BindIndexHeader;
            m_MultiColumnListView.columns["index"].bindCell = BindIndexCell;
            m_MultiColumnListView.columns["profile"].bindCell = BindProfileCell;
            m_MultiColumnListView.columns["platform"].bindCell = BindPlatformCell;
            m_MultiColumnListView.columns["user"].bindCell = BindUserCell;
            m_MultiColumnListView.columns["changeset"].bindCell = BindChangesetCell;
            m_MultiColumnListView.columns["branch"].bindCell = BindBranchCell;
            m_MultiColumnListView.columns["startedAt"].bindCell = BindStartedAtCell;
            m_MultiColumnListView.columns["finishedAt"].bindCell = BindFinishedAtCell;
            m_MultiColumnListView.columns["downloads"].bindCell = BindDownloadsCell;
            m_MultiColumnListView.columns["logs"].bindCell = BindLogsCell;

            m_MultiColumnListView.itemsSource = buildAttempts;

            ToggleLoadingText(false);
        }

        private void BindIndexHeader(VisualElement element)
        {
            element.AddToClassList("history-cell");
            element.style.paddingLeft = 4;
            if (element.childCount > 0)
            {
                element.Clear();
            }
            m_parentBuildSelector = new Toggle();
            m_parentBuildSelector.RegisterCallback<ChangeEvent<bool>>(OnParentSelectorClicked);
            element.Add(m_parentBuildSelector);

            var label = new Label("Status & #") { style = { marginLeft = 6 } };
            element.Add(label);
        }

        private void BindIndexCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            if (cellAttempt == null)
            {
                return;
            }
            element.AddToClassList("history-cell");

            // Initialize Build Selector
            var buildSelector = element.Q<Toggle>("buildSelector");
            buildSelector.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                OnBuildSelected(cellAttempt, evt.newValue);
            });
            m_buildSelectors.Add(buildSelector);

            // Initialize Build Status Icon
            var icon = element.Q<Image>("statusIcon");
            var iconName = BuildAutomationConstants.GetStatusIcon(cellAttempt.BuildStatus);
            var tooltip = BuildAutomationConstants.GetFormattedStatusName(cellAttempt.BuildStatus);
            var statusIcon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.unity.services.cloud-build/Icons/{iconName}.png");
            icon.style.backgroundImage = new StyleBackground(statusIcon);
            icon.tooltip = tooltip;

            // Initialize Build Status Label
            var label = element.Q<Label>("statusLabel");
            var attemptNumber = cellAttempt.Build.ToString(CultureInfo.InvariantCulture);
            label.text = "# " + attemptNumber;
            label.tooltip = $"Click to view build details for attempt {attemptNumber} of {cellAttempt.BuildTargetName}";

            var newManipulator = new Clickable(() =>
            {
                var url = BuildAutomationDashboardUrls.GetAttemptUrl(cellAttempt.BuildTargetId, attemptNumber);
                Application.OpenURL(url);
            });
            label.AddManipulator(newManipulator);
        }

        private void BindProfileCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            var label = element as Label ?? new Label();

            label.text = cellAttempt.BuildTargetName;
            label.AddToClassList("history-cell-label");
        }

        private void BindPlatformCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            if (cellAttempt == null)
            {
                return;
            }
            element.AddToClassList("history-cell");

            var platform = cellAttempt.Platform;
            var formattedPlatform = cellAttempt.FormattedPlatformName;

            // Initialize Platform Icon
            var platformIconName = $"{BuildAutomationUtilities.IconNameFromPlatformAndTarget(platform, false)}.Small";
            var platformIcon = EditorGUIUtility.IconContent(platformIconName);
            var icon = element.Q<Image>("platformIcon");
            icon.style.backgroundImage = new StyleBackground(platformIcon.image as Texture2D);
            icon.style.width = platformIcon.image.width;
            icon.style.height = platformIcon.image.height;
            icon.tooltip = platform;

            // Initialize Platform Label
            var label = element.Q<Label>("platformLabel");
            label.text = formattedPlatform;

            // Initialize Server Icon
            // TODO: Change this to "{platform server" when we have migrated prod to standaloneosx instead of standaloneosxuniversal
            var isServerBuild = platform.Contains("standalone") &&
                                cellAttempt.BuildTargetName.ToLower().Contains(" server");
            var serverIconImage = element.Q<Image>("serverIcon");
            if (isServerBuild)
            {
                serverIconImage.style.display = DisplayStyle.Flex;
                var serverIcon =
                    EditorGUIUtility.IconContent(BuildAutomationUtilities.IconNameFromPlatformAndTarget(platform, true));
                serverIconImage.style.backgroundImage = new StyleBackground(serverIcon.image as Texture2D);
                serverIconImage.style.width = serverIcon.image.width;
                serverIconImage.style.height = serverIcon.image.height;
                serverIconImage.tooltip = "Server";
            }
            else
            {
                serverIconImage.style.display = DisplayStyle.None;
            }
        }

        private void BindUserCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            var label = element as Label;
            if (label == null)
                label = new Label();
            label.AddToClassList("history-cell-label");

            label.text = string.IsNullOrEmpty(cellAttempt.CausedByUser) ? "--" : cellAttempt.CausedByUser;
        }

        private void BindChangesetCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            var label = element as Label;
            if (label == null)
                label = new Label();
            label.AddToClassList("history-cell-label");

            label.text = string.IsNullOrEmpty(cellAttempt.Revision) ? "--" : cellAttempt.Revision;
        }

        private void BindBranchCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            var label = element as Label;
            if (label == null)
                label = new Label();
            label.AddToClassList("history-cell-label");

            label.text = string.IsNullOrEmpty(cellAttempt.Branch) ? "--" : cellAttempt.Branch;
        }

        private void BindStartedAtCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            var label = element as Label;
            if (label == null)
                label = new Label();
            label.AddToClassList("history-cell-label");

            if (BuildAutomationConstants.IsBuildPending(cellAttempt.BuildStatus))
            {
                label.text = BuildAutomationConstants.GetFormattedStatusName(cellAttempt.BuildStatus);
            }
            else
            {
                var time = TimeSpan.FromSeconds(cellAttempt.BuildTimeInSeconds);
                var formattedTime = time.ToString(@"hh\:mm\:ss");
                label.text = formattedTime;
            }
        }

        private void BindFinishedAtCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            var label = element as Label;
            if (label == null)
                label = new Label();
            label.AddToClassList("history-cell-label");

            if (!BuildAutomationConstants.IsBuildFinished(cellAttempt.BuildStatus))
            {
                label.text = "N/A";
                return;
            }
            else
            {
                var finishedTime = BuildAutomationUtilities.ToLocalTime(cellAttempt.Finished);
                var timeString = BuildAutomationUtilities.FormatTime(DateTime.Now.Subtract(finishedTime));

                label.text = timeString;
                label.tooltip = finishedTime.ToString(CultureInfo.CurrentCulture);
            }
        }

        private void BindDownloadsCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            element.AddToClassList("history-cell");
            if (element.childCount > 0)
            {
                element.Clear();
            }

            if (cellAttempt.Links.Artifacts.Count == 0)
            {
                var emptyDownload = new VisualElement();
                emptyDownload.AddToClassList("history-cell");
                element.Add(emptyDownload);
            }
            else
            {
                var menu = new ToolbarMenu();
                element.Add(menu);
                var settingsIcon = CreateIcon("Download-Available", 16, 2);
                menu.Add(settingsIcon);

                foreach (var artifact in cellAttempt.Links.Artifacts)
                {
                    menu.menu.AppendAction($"{artifact.Name}", (a) =>
                    {
                        var location = EditorUtility.SaveFilePanel($"Save {artifact.Name}", "", artifact.Files[0].Filename, "");
                        Debug.Log($"Attempting to download {artifact.Name} from build attempt #{cellAttempt.Build} of {cellAttempt.BuildTargetName}");
                        m_apiClient.DownloadFile(artifact.Files[0].Href, location, OnDownloadSucceeded, OnDownloadFailed);

                        void OnDownloadFailed(Exception error)
                        {
                            Debug.LogError(error);
                        }
                        void OnDownloadSucceeded(string text)
                        {
                            Debug.Log(text);
                        }
                    });
                }
            }
        }

        private void BindLogsCell(VisualElement element, int index)
        {
            var cellAttempt = m_MultiColumnListView.itemsSource[index] as ProjectBuildAttempt;
            element.AddToClassList("history-cell");
            if (element.childCount > 0)
            {
                element.Clear();
            }

            var menu = new ToolbarMenu();
            element.Add(menu);
            var settingsIcon = CreateIcon("Download-Available", 16, 2);
            menu.Add(settingsIcon);

            menu.menu.AppendAction("View Build Log", (a) =>
            {
                OnLogClicked(BuildAutomationDashboardUrls.GetAttemptUrl(cellAttempt.BuildTargetId,
                    cellAttempt.Build + "/log"));
            });

            if (BuildAutomationConstants.IsBuildLogDownloadable(cellAttempt.BuildStatus))
            {
                menu.menu.AppendAction($"Download Build Log", (a) =>
                {
                    var logFileName = $"{cellAttempt.BuildTargetName}-{cellAttempt.Build}.txt";
                    var location = EditorUtility.SaveFilePanel($"Download Build Log", "", logFileName, "");

                    m_apiClient.GetBuildLog(
                        onSuccess: logText =>
                        { },
                        onError: exception =>
                        {
                            Debug.LogError("Error retrieving build log: " + exception.Message);
                        },
                        location,
                        cellAttempt.Build,
                        cellAttempt.BuildTargetName
                    );

                });
            }
        }

        void SetupStaticComponents(List<SupportedPlatform> supportedPlatforms)
        {
            // Cancel Build Button
            var cancelButton = rootVisualElement.Q<Button>(k_CancelBuildsButton);
            cancelButton.clicked += OnCancelClicked;

            // Delete Build Button
            var deleteButton = rootVisualElement.Q<Button>(k_DeleteBuildsButton);
            deleteButton.clicked += OnDeleteClicked;

            // Per Page Dropdown
            var buildsPerPage = new List<string> { "10", "25", "50" };
            var perPageDropdown = rootVisualElement.Q<DropdownField>("builds-per-page");
            perPageDropdown.choices = buildsPerPage;
            perPageDropdown.value = buildsPerPage[0];
            perPageDropdown.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                m_AutomationHistoryOptions.PerPage = int.Parse(evt.newValue);
                m_AutomationHistoryOptions.PageNumber = 1;
                RefreshBuildHistory();
            });

            // Back Button
            var backButton = rootVisualElement.Q<Button>("PageBack");
            var backIcon = EditorGUIUtility.IconContent("back@2x");
            backButton.style.width = 27;
            backButton.style.height = 24;
            backButton.style.alignItems = new StyleEnum<Align>(Align.Center);
            var backButtonImage = new Image {
                style = {
                    backgroundImage = new StyleBackground(backIcon.image as Texture2D),
                    width = 18,
                    height = 21,
                }
            };
            backButton.Add(backButtonImage);
            backButton.SetEnabled(false);
            backButton.clicked += () =>
            {
                if (m_AutomationHistoryOptions.PageNumber == 1) return;
                m_AutomationHistoryOptions.PageNumber--;
                RefreshBuildHistory();
            };

            // Forward Button
            var forwardButton = rootVisualElement.Q<Button>("NextPage");
            var forwardIcon = EditorGUIUtility.IconContent("forward@2x");
            forwardButton.style.width = 27;
            forwardButton.style.height = 24;
            forwardButton.style.alignItems = new StyleEnum<Align>(Align.Center);
            var forwardButtonImage = new Image {
                style = {
                    backgroundImage = new StyleBackground(forwardIcon.image as Texture2D),
                    width = 18,
                    height = 21,
                }
            };
            forwardButton.Add(forwardButtonImage);
            forwardButton.SetEnabled(false);
            forwardButton.clicked += () =>
            {
                m_AutomationHistoryOptions.PageNumber++;
                RefreshBuildHistory();
            };

            // Refresh Button
            var button = rootVisualElement.Q<Button>(k_RefreshButton);
            var refresh = EditorGUIUtility.IconContent("Refresh");
            var refreshIcon = new Image {
                style = {
                    backgroundImage = new StyleBackground(refresh.image as Texture2D),
                    width = 16,
                    height = 16,
                }
            };
            button.Add(refreshIcon);
            button.clicked += RefreshBuildHistory;

            // Clear Filters Button
            var clearFilterButton = rootVisualElement.Q<Button>(k_ClearFiltersButton);
            var clear = EditorGUIUtility.IconContent("Clear");
            var clearIcon = new Image {
                style = {
                    backgroundImage = new StyleBackground(clear.image as Texture2D),
                    width = 16,
                    height = 16,
                }
            };
            clearFilterButton.Add(clearIcon);
            clearFilterButton.clicked += ClearFilters;

            // Settings Button
            var settingsMenu = rootVisualElement.Q<ToolbarMenu>(k_SettingsMenu);
            var settingsIcon = CreateIcon("Settings", 16, 2);
            settingsMenu.Add(settingsIcon);
            settingsMenu.menu.AppendAction("Open History In Dashboard", (a) => { OpenHistoryInDashboard(); });
            settingsMenu.menu.AppendAction("Open Config In Dashboard", (a) => { OpenConfigInDashboard(); });
            settingsMenu.menu.AppendSeparator();
            settingsMenu.menu.AppendAction("Options", (a) => { OpenProjectSettings("Project/Services/Build Automation"); });

            // Upgrade Button
            var upgradeButton = rootVisualElement.Q<Button>(k_UpgradeButton);
            upgradeButton.clicked += () =>
            {
                Application.OpenURL(BuildAutomationDashboardUrls.GetUpgradeUrl());
            };

            // Status Filter
            var statusFilter = rootVisualElement.Q<BuildAutomationToolbarSelectionMenu>(k_StatusFilter);
            foreach (var status in BuildAutomationConstants.FilterableBuildStatuses)
            {
                statusFilter.AddOption(BuildAutomationConstants.GetFormattedStatusName(status), status);
            }
            statusFilter.OnValueChanged += (selectedValues) =>
            {
                m_AutomationHistoryOptions.StatusFilter = selectedValues;
                RefreshBuildHistory();
            };

            // Platform Filter
            var platformFilter = rootVisualElement.Q<BuildAutomationToolbarSelectionMenu>(k_PlatformFilter);
            foreach (var platform in supportedPlatforms)
            {
                platformFilter.AddOption(platform.Name, platform.Platform);
            }
            platformFilter.OnValueChanged += (selectedValues) =>
            {
                m_AutomationHistoryOptions.PlatformFilter = selectedValues;
                RefreshBuildHistory();
            };

            // Profile Filter
            var profiles = AssetDatabase.FindAssets("t:BuildProfile");
            var profileFilter = rootVisualElement.Q<BuildAutomationToolbarSelectionMenu>(k_ProfileFilter);
            foreach (var profileGuid in profiles)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(profileGuid);
                var profileName = Path.GetFileNameWithoutExtension(assetPath);
                profileFilter.AddOption(profileName, profileName);
            }
            profileFilter.OnValueChanged += (selectedValues) =>
            {
                m_AutomationHistoryOptions.ProfileFilter = selectedValues;
                RefreshBuildHistory();
            };

            // Display Filter
            var displayFilter = rootVisualElement.Q<BuildAutomationToolbarSelectionMenu>(k_DisplayFilter);
            displayFilter.AddOption(k_OnlyMyBuildsLabel, k_OnlyMyBuildsValue);
            displayFilter.OnValueChanged += (selectedValues) =>
            {
                m_AutomationHistoryOptions.OnlyMyBuilds = selectedValues.Contains(k_OnlyMyBuildsValue);
                RefreshBuildHistory();
            };

            // Search Bar
            var searchBar = rootVisualElement.Q<ToolbarSearchField>(k_SearchBar);
            searchBar.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (searchBar.value == m_AutomationHistoryOptions.Search) return;
                m_AutomationHistoryOptions.Search = searchBar.value;
                RefreshBuildHistory();
            });

            // History Table
            m_MultiColumnListView = rootVisualElement.Q<MultiColumnListView>();
            m_MultiColumnListView.makeNoneElement = BuildEmptyTableView;
        }

        async void RefreshBuildHistory()
        {
            ToggleLoadingText(true);

            // Don't clear the list during polling
            if (!m_isPolling)
            {
                m_MultiColumnListView.itemsSource = new List<ProjectBuildAttempt>();
            }

            var freeTierBanner = rootVisualElement.Q<VisualElement>(k_FreeTierBanner);
            // Check if user has upgraded from free plan
            if (freeTierBanner.style.display == DisplayStyle.Flex)
            {
                var showBanner = await m_asyncApiClient.HasFreeTierBeenReached();
                if (showBanner)
                {
                    // Ensure UI changes happen on the main thread
                    EditorApplication.delayCall += () =>
                    {
                        freeTierBanner.style.display = DisplayStyle.Flex;
                    };
                }
            }

            var buildAttempts = await m_asyncApiClient.GetProjectBuildHistory(m_AutomationHistoryOptions);
            if (buildAttempts != null && buildAttempts.Count > 0)
            {
                SetupBuildHistory(buildAttempts);
            }
            else
            {
                SetupBuildHistory(new List<ProjectBuildAttempt>());
            }
        }

        void ClearFilters()
        {
            m_selectedBuilds.Clear();

            UpdateBuildsSelectedText();

            foreach (var selector in m_buildSelectors)
            {
                selector.value = false;
            }

            // Early return if no filters were applied to avoid needing to refresh the build history
            if ((m_AutomationHistoryOptions.PlatformFilter == null || m_AutomationHistoryOptions.PlatformFilter.Length == 0) &&
                (m_AutomationHistoryOptions.StatusFilter == null || m_AutomationHistoryOptions.StatusFilter.Length == 0) &&
                (m_AutomationHistoryOptions.ProfileFilter == null || m_AutomationHistoryOptions.ProfileFilter.Length == 0) &&
                m_AutomationHistoryOptions.OnlyMyBuilds == false)
            {
                return;
            }

            // Reset selected filters
            var statusFilter = rootVisualElement.Q<BuildAutomationToolbarSelectionMenu>(k_StatusFilter);
            m_AutomationHistoryOptions.StatusFilter = Array.Empty<string>();

            var platformFilter = rootVisualElement.Q<BuildAutomationToolbarSelectionMenu>(k_PlatformFilter);
            m_AutomationHistoryOptions.PlatformFilter = Array.Empty<string>();

            var profileFilter = rootVisualElement.Q<BuildAutomationToolbarSelectionMenu>(k_ProfileFilter);
            m_AutomationHistoryOptions.ProfileFilter = Array.Empty<string>();

            var displayFilter = rootVisualElement.Q<BuildAutomationToolbarSelectionMenu>(k_DisplayFilter);
            m_AutomationHistoryOptions.OnlyMyBuilds = false;

            // Clearing options will reset the filter text
            statusFilter.ClearOptions();
            platformFilter.ClearOptions();
            profileFilter.ClearOptions();
            displayFilter.ClearOptions();

            RefreshBuildHistory();
        }

        VisualElement BuildEmptyTableView()
        {
            var emptyTableView = new VisualElement();
            emptyTableView.AddToClassList("empty-build-list");

            // Element to show when No Builds are Available
            var noBuildsAvailable = new VisualElement();
            emptyTableView.Add(noBuildsAvailable);

            var emptyHeader = new Label("No builds available yet");
            emptyHeader.AddToClassList("empty-build-list-header");
            noBuildsAvailable.Add(emptyHeader);

            var emptyDescription = new Label("Start a build for the build profile of your choice.");
            noBuildsAvailable.Add(emptyDescription);

            // Element to show when Fetching new Builds
            // var fetchingBuilds = new VisualElement();
            // emptyTableView.Add(fetchingBuilds);
            //
            // var fetchingLabel = new Label("Fetching build history...");
            // fetchingBuilds.Add(fetchingLabel);

            return emptyTableView;
        }

        void ToggleLoadingText(bool loading)
        {
            // TODO: This will likely change when we separate out the build history window, so leaving it unimplemented until then
            // TODO: hide/show the "no available builds"
            // TODO: show/hide "Fetching Builds"

            // rootVisualElement.Q<VisualElement>("FetchingBuildsContainer").style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;
            // rootVisualElement.Q<VisualElement>("EmptyBuildsContainer").style.display = loading ? DisplayStyle.None : DisplayStyle.Flex;
        }

        static void AddDownloadElement(VisualElement rootVisualElement, string labelText, bool active, Action onDownloadClicked)
        {
            var downloadIcon = EditorGUIUtility.IconContent("Download-Available");
            var downloadButton = new Button {
                style = {
                    backgroundImage = new StyleBackground(downloadIcon.image as Texture2D),
                    width = 16,
                    height = 16,
                    marginRight = 4,
                    borderBottomWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    backgroundColor = new Color(0,0,0,0),
                }
            };
            rootVisualElement.Add(downloadButton);
            downloadButton.clicked += onDownloadClicked;

            var label = new Label(labelText);
            rootVisualElement.Add(label);
        }

        void OnParentSelectorClicked(ChangeEvent<bool> changeEvent)
        {
            if (changeEvent.newValue)
            {
                foreach (var selector in m_buildSelectors)
                {
                    selector.value = true;
                }
            }
            else
            {
                foreach (var selector in m_buildSelectors)
                {
                    selector.value = false;
                }
            }
        }

        void OnBuildSelected(ProjectBuildAttempt selectedBuild, bool value)
        {
            if (value)
            {
                m_selectedBuilds.Add(selectedBuild);
            }
            else
            {
                m_selectedBuilds.Remove(selectedBuild);
            }

            m_parentBuildSelector.SetValueWithoutNotify(m_selectedBuilds.Count >= m_buildSelectors.Count);
            m_parentBuildSelector.showMixedValue = m_selectedBuilds.Any() && m_selectedBuilds.Count < m_buildSelectors.Count;

            UpdateBuildsSelectedText();

            var cancelButton = rootVisualElement.Q<Button>(k_CancelBuildsButton);
            var deleteButton = rootVisualElement.Q<Button>(k_DeleteBuildsButton);
            cancelButton.SetEnabled(m_selectedBuilds.Any());
            deleteButton.SetEnabled(m_selectedBuilds.Any());
        }

        void UpdateBuildsSelectedText()
        {
            var selectedLabel = rootVisualElement.Q<Label>(k_BuildsSelectedLabel);
            var selectedText = m_selectedBuilds.Count == 1
                ? k_BuildsSelectedTextSingular
                : k_BuildsSelectedTextPlural;
            selectedLabel.text = $"{m_selectedBuilds.Count} {selectedText}";
        }

        // TODO: Update this to turn it into an actual GET call with the EditorUtility.Save stuff to choose where to save the file
        void OnLogClicked(string url)
        {
            // EditorGameServiceAnalyticsSender.SendProjectSettingsBuildHistoryEvent();
            Application.OpenURL(url);
        }

        void OnCancelClicked()
        {
            if (EditorUtility.DisplayDialog($"Build Automation: Cancel {m_selectedBuilds.Count} ongoing build(s).",
                    "Are you sure you want to cancel selected ongoing builds? Any selected builds that are not in progress will be ignored.",
                    "Cancel Builds", "Close"))
            {
                rootVisualElement.Q<Button>(k_DeleteBuildsButton).SetEnabled(false);
                rootVisualElement.Q<Button>(k_CancelBuildsButton).SetEnabled(false);

                var buildsToCancel = new List<DeleteBuildRequest>();
                foreach (var build in m_selectedBuilds)
                {
                    buildsToCancel.Add(new DeleteBuildRequest
                    {
                        BuildNumber = (int)build.Build,
                        BuildTargetId = build.BuildTargetId,
                    });
                }
                m_apiClient.CancelBuilds(new DeleteBuildsRequest { Builds = buildsToCancel},
                    (deletedResponse) =>
                    {
                        Debug.Log($"Successfully Canceled {m_selectedBuilds.Count} cloud builds");
                        foreach (var selector in m_buildSelectors)
                        {
                            selector.value = false;
                        }
                        RefreshBuildHistory();
                    }, (e) =>
                    {
                        Debug.LogError(e);
                    });
            }
        }

        void OnDeleteClicked()
        {
            if (EditorUtility.DisplayDialog($"Build Automation: Delete {m_selectedBuilds.Count} build(s).",
                    "Are you sure you want to delete the selected builds?",
                    "Delete", "Close"))
            {
                rootVisualElement.Q<Button>(k_DeleteBuildsButton).SetEnabled(false);
                rootVisualElement.Q<Button>(k_CancelBuildsButton).SetEnabled(false);

                var buildsToDelete = new List<DeleteBuildRequest>();
                foreach (var build in m_selectedBuilds)
                {
                    buildsToDelete.Add(new DeleteBuildRequest
                    {
                        BuildNumber = (int)build.Build,
                        BuildTargetId = build.BuildTargetId,
                    });
                }

                m_apiClient.DeleteBuilds(new DeleteBuildsRequest { Builds = buildsToDelete },
                    (deletedResponse) =>
                    {
                        Debug.Log($"Successfully Deleted {deletedResponse.NumDeletedBuilds} cloud builds");
                        foreach (var selector in m_buildSelectors)
                        {
                            selector.value = false;
                        }
                        RefreshBuildHistory();
                    }, (e) =>
                    {
                        Debug.LogError(e);
                    });
            }
        }

        Image CreateIcon(string iconName, float width, float marginBottom)
        {
            var iconContent = EditorGUIUtility.IconContent(iconName);
            return new Image
            {
                style =
                {
                    backgroundImage = new StyleBackground(iconContent.image as Texture2D),
                    width = width,
                    height = width, // Using the same height as width for a square icon
                    marginBottom = marginBottom,
                }
            };
        }

        static void OpenHistoryInDashboard()
        {
            var url = BuildAutomationDashboardUrls.GetHistoryUrl();
            Application.OpenURL(url);
        }

        static void OpenConfigInDashboard()
        {
            var url = BuildAutomationDashboardUrls.GetConfigUrl();
            Application.OpenURL(url);
        }

        static void OpenProjectSettings(string path)
        {
            SettingsService.OpenProjectSettings(path);
        }
    }
}
