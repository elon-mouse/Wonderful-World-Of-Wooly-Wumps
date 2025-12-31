using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.CloudBuild.Editor.Components
{
    class BuildAutomationBuilderConfig : VisualElement
    {
        const string k_Uxml = "Packages/com.unity.services.cloud-build/Editor/UXML/BuildAutomationBuilderConfig.uxml";

        const string k_OperatingSystemValueDropDown = "operating-system-dropdown";
        const string k_OperatingSystemValueBindingProperty = "m_OperatingSystemValue";
        const string k_OperatingSystemFamilyBindingProperty = "m_OperatingSystemFamily";

        const string k_XcodeDropDown = "xcode-dropdown";
        const string k_XcodeBindingProperty = "m_XcodeVersion";

        const string k_AndroidDropDown = "android-dropdown";
        const string k_AndroidBindingProperty = "m_AndroidSdkVersion";

        const string k_UnityArchitectureDropDown = "architecture-dropdown";
        const string k_UnityArchitectureBindingProperty = "m_UnityArchitecture";

        const string k_MacOsFamilyName = "mac";

        const string k_MachineTypeDropdown = "machine-type-dropdown";
        const string k_MachineTypeBindingProperty = "m_MachineTypeId";
        const string k_MachineTypeInfoButton = "machine-type-info-button";

        readonly BuildAutomationApiClient m_ApiClient;
        readonly SerializedObject m_SerializedObject;
        readonly BuildTarget m_BuildTarget;

        DropdownField m_OperatingSystemDropDown;
        DropdownField m_XcodeDropDown;
        DropdownField m_AndroidDropDown;
        DropdownField m_UnityArchitectureDropDown;
        DropdownField m_MachineTypeDropDown;

        Dictionary<string, GetOperatingSystemResponse> m_OsValueMap;
        Dictionary<string, GetMachineTypeResponse> m_MachineTypeValueMap;
        Dictionary<string, string> m_AndroidValueNameMap;
        Dictionary<string, List<GetMachineTypeResponse>> m_OSVersionMachineTypeMap;

        public BuildAutomationBuilderConfig(BuildAutomationApiClient mApiClient, SerializedObject serializedObject, BuildTarget buildTarget)
        {
            m_ApiClient = mApiClient;
            m_SerializedObject = serializedObject;
            m_BuildTarget = buildTarget;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml);
            visualTree.CloneTree(this);

            InitializeVisualElements(this);
        }

        public async Task FetchConfig(CancellationToken cancellationToken)
        {
            try
            {
                var config = await m_ApiClient.GetConfigOptions(BuildAutomationUtilities.GetUnityVersion());
                var supportedPlatforms = await m_ApiClient.GetSupportedPlatforms(BuildAutomationUtilities.GetUnityVersion());
                var supportedPlatform = supportedPlatforms.Where(p => p.Platform.ToLower() == m_BuildTarget.ToString().ToLower());

                cancellationToken.ThrowIfCancellationRequested();

                InitializeOperatingSystemsDropdownChoices(config.OperatingSystems, supportedPlatform);
                InitializeMachineTypesDropdownChoices(config.MachineTypes);
                InitializeXcodeDropdownChoices(config.XcodeVersions);
                InitializeAndroidDropdownChoices(config.AndroidSdkValues);

                OnOperatingSystemChanged(m_OperatingSystemDropDown.value);
                ApplyDropdownFormatting();
                RegisterChangeCallbacks();
            }
            catch (OperationCanceledException)
            {
                // This is expected when cancellation occurs, don't treat as error
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"Loading Build Automation configuration failed - ${e}");
            }
        }

        void InitializeVisualElements(VisualElement root)
        {
            m_OperatingSystemDropDown = root.Q<DropdownField>(k_OperatingSystemValueDropDown);
            m_OperatingSystemDropDown.BindProperty(m_SerializedObject.FindProperty(k_OperatingSystemValueBindingProperty));

            m_XcodeDropDown = root.Q<DropdownField>(k_XcodeDropDown);
            m_XcodeDropDown.BindProperty(m_SerializedObject.FindProperty(k_XcodeBindingProperty));

            m_AndroidDropDown = root.Q<DropdownField>(k_AndroidDropDown);
            m_AndroidDropDown.BindProperty(m_SerializedObject.FindProperty(k_AndroidBindingProperty));

            m_UnityArchitectureDropDown = root.Q<DropdownField>(k_UnityArchitectureDropDown);
            m_UnityArchitectureDropDown.BindProperty(m_SerializedObject.FindProperty(k_UnityArchitectureBindingProperty));

            m_MachineTypeDropDown = root.Q<DropdownField>(k_MachineTypeDropdown);
            m_MachineTypeDropDown.BindProperty(m_SerializedObject.FindProperty(k_MachineTypeBindingProperty));

            var machineTypeInfoButton = root.Q<Button>(k_MachineTypeInfoButton);
            var helpIcon = EditorGUIUtility.IconContent("_Help");
            machineTypeInfoButton.style.width = 18;
            machineTypeInfoButton.style.height = 18;
            machineTypeInfoButton.style.alignItems = new StyleEnum<Align>(Align.Center);
            var helpButtonImage = new Image {
                style = {
                    backgroundImage = new StyleBackground(helpIcon.image as Texture2D),
                    width = 16,
                    height = 16,
                }
            };
            machineTypeInfoButton.Add(helpButtonImage);
            machineTypeInfoButton.tooltip = "Open documentation about Build Automation Machine Specifications";
            machineTypeInfoButton.clicked += () =>
            {
                var url = BuildAutomationDashboardUrls.GetMachineSpecsUrl();
                Application.OpenURL(url);
            };
        }

        void ApplyDropdownFormatting()
        {
            m_MachineTypeDropDown.formatSelectedValueCallback = FormatMachineType;
            m_MachineTypeDropDown.formatListItemCallback = FormatMachineType;

            m_OperatingSystemDropDown.formatSelectedValueCallback = FormatOperatingSystem;
            m_OperatingSystemDropDown.formatListItemCallback = FormatOperatingSystem;

            m_AndroidDropDown.formatSelectedValueCallback = FormatAndroid;
            m_AndroidDropDown.formatListItemCallback = FormatAndroid;

            m_UnityArchitectureDropDown.formatSelectedValueCallback = FormatArchitecture;
            m_UnityArchitectureDropDown.formatListItemCallback = FormatArchitecture;

            m_XcodeDropDown.formatSelectedValueCallback = FormatXcode;
            m_XcodeDropDown.formatListItemCallback = FormatXcode;
        }

        void RegisterChangeCallbacks()
        {
            m_OperatingSystemDropDown.RegisterValueChangedCallback(OnOperatingSystemChanged);
        }

        void InitializeOperatingSystemsDropdownChoices(List<GetOperatingSystemResponse> operatingSystems, IEnumerable<SupportedPlatform> supportedPlatform)
        {
            var supportedOsVersions = supportedPlatform
                    .SelectMany(p => p.OperatingSystems)
                    .SelectMany(os => os.Versions)
                    .Select(v => v.Value)
                    .ToList();

            var filteredOs = operatingSystems.Where(os => !os.Hidden && supportedOsVersions.Contains(os.Value)).ToList();

            m_OsValueMap = filteredOs.ToDictionary(os => os.Value);
            m_OperatingSystemDropDown.choices = filteredOs.Select(os => os.Value).ToList();

            if (string.IsNullOrEmpty(m_OperatingSystemDropDown.value))
            {
                m_OperatingSystemDropDown.value = filteredOs.Select(os => os.Value).ToList()[0];
            }
        }

        void InitializeMachineTypesDropdownChoices(List<GetMachineTypeResponse> machineTypes)
        {
            var machineValues = machineTypes.Select(os => os.Label).ToList();
            m_MachineTypeValueMap = new Dictionary<string, GetMachineTypeResponse>();
            m_OSVersionMachineTypeMap = new Dictionary<string, List<GetMachineTypeResponse>>();
            for (var i = 0; i < machineValues.Count; i++)
            {
                m_MachineTypeValueMap.Add(machineValues[i], machineTypes[i]);

                foreach (var osVersion in machineTypes[i].OperatingSystemVersions)
                {
                    if (!m_OSVersionMachineTypeMap.TryGetValue(osVersion, out var validMachineTypesForOSVersion))
                    {
                        validMachineTypesForOSVersion = new List<GetMachineTypeResponse>();
                        m_OSVersionMachineTypeMap.Add(osVersion, validMachineTypesForOSVersion);
                    }
                    validMachineTypesForOSVersion.Add(machineTypes[i]);
                }
            }
        }

        void InitializeXcodeDropdownChoices(List<GetXCodeVersionResponse> xCodeVersions)
        {
            m_XcodeDropDown.choices = new List<string>();
        }

        void InitializeAndroidDropdownChoices(List<GetAndroidSdkConfigResponse> androidVersions)
        {
            var androidValues = androidVersions.Select(sdk => sdk.Value).ToList();
            m_AndroidDropDown.choices = androidValues;

            m_AndroidValueNameMap = new Dictionary<string, string>();
            foreach (var sdk in androidVersions)
            {
                m_AndroidValueNameMap.Add(sdk.Value, sdk.Name);
            }

            m_AndroidDropDown.enabledSelf = m_BuildTarget == BuildTarget.Android;
            if (string.IsNullOrEmpty(m_AndroidDropDown.value))
            {
                m_AndroidDropDown.value = androidValues[0];
            }
        }

        void OnOperatingSystemChanged(ChangeEvent<string> value)
        {
            OnOperatingSystemChanged(value.newValue);
        }

        void OnOperatingSystemChanged(string value)
        {
            GetOperatingSystemResponse selectedOS = null;
            if (!string.IsNullOrEmpty(value))
            {
                selectedOS = m_OsValueMap?.GetValueOrDefault(value, null);
            }

            try
            {
                m_SerializedObject.FindProperty(k_OperatingSystemFamilyBindingProperty).stringValue = selectedOS?.Family;
                m_SerializedObject.ApplyModifiedProperties();
                m_SerializedObject.Update();

                UpdateXcodeDropdown(selectedOS);
                UpdateArchitectureDropdown(selectedOS);
                UpdateMachineTypeDropdown(selectedOS);
            }
            catch (Exception)
            {
                // This is expected when cancellation occurs, don't treat as error
                return;
            }
        }

        void UpdateXcodeDropdown(GetOperatingSystemResponse selectedOS)
        {
            var osFamily = selectedOS?.Family;
            m_XcodeDropDown.enabledSelf = osFamily == k_MacOsFamilyName;

            if (selectedOS != null && selectedOS.XcodeVersions != null)
            {
                // Get all Xcode versions that are supported by this OS
                var supportedVersions = m_OsValueMap[selectedOS.Value].XcodeVersions;
                m_XcodeDropDown.choices = supportedVersions;

                // If current selection is not in filtered list, set to default
                if (!string.IsNullOrEmpty(m_XcodeDropDown.value) && !supportedVersions.Contains(m_XcodeDropDown.value))
                {
                    m_XcodeDropDown.value = selectedOS.DefaultXcodeVersion ?? "default";
                }

                if (string.IsNullOrEmpty(m_XcodeDropDown.value))
                {
                    m_XcodeDropDown.value = "default";
                }
            }
            else
            {
                m_XcodeDropDown.choices = new List<string>();
            }
        }

        void UpdateArchitectureDropdown(GetOperatingSystemResponse selectedOS)
        {
            var architectures = selectedOS?.Architectures;
            if (architectures == null || architectures.Count == 0)
            {
                m_UnityArchitectureDropDown.value = "";
                m_UnityArchitectureDropDown.enabledSelf = false;
            }
            else
            {
                m_UnityArchitectureDropDown.enabledSelf = true;
                m_UnityArchitectureDropDown.choices = architectures;
                if (string.IsNullOrEmpty(m_UnityArchitectureDropDown.value))
                {
                    m_UnityArchitectureDropDown.value = architectures[0];
                }
            }
        }

        void UpdateMachineTypeDropdown(GetOperatingSystemResponse selectedOS)
        {
            if (selectedOS == null)
            {
                // We cannot populate the machine type dropdown until an OS is selected
                m_MachineTypeDropDown.enabledSelf = false;
                return;
            }

            var selectedOSValue = selectedOS.Value;

            var validMachineTypes = m_OSVersionMachineTypeMap?.GetValueOrDefault(selectedOSValue);
            if (validMachineTypes == null || validMachineTypes.Count == 0)
            {
                // If there are no valid machine types, disable the dropdown
                m_MachineTypeDropDown.enabledSelf = false;
                return;
            }

            m_MachineTypeDropDown.enabledSelf = true;
            m_MachineTypeDropDown.choices = validMachineTypes.Select(m => m.Label).ToList();

            // check if the selected Machine Type is still valid for the new OS
            if (!string.IsNullOrEmpty(m_MachineTypeDropDown.value))
            {
                var selectedMachineType = m_MachineTypeValueMap?.GetValueOrDefault(m_MachineTypeDropDown.value, null);
                if (selectedMachineType != null && selectedMachineType.OperatingSystemVersions.Contains(selectedOSValue))
                {
                    // The selected machine type is valid for the newly selected OS, nothing to do
                    return;
                }
            }

            // If selected Machine Type is not valid for the new OS, set it to the default value
            var defaultMachineType = validMachineTypes.FirstOrDefault(m => m.Default);
            m_MachineTypeDropDown.value = defaultMachineType != null ? defaultMachineType.Label : "";
        }

        string FormatOperatingSystem(string osValue)
        {
            if (string.IsNullOrEmpty(osValue))
            {
                return "";
            }

            if (m_OsValueMap == null)
            {
                return osValue;
            }

            var os = m_OsValueMap.GetValueOrDefault(osValue, null);
            return os != null ? os.Name : osValue;
        }

        string FormatArchitecture(string architectureValue)
        {
            if (string.IsNullOrEmpty(architectureValue))
            {
                return "";
            }

            if (architectureValue == "default")
            {
                return "Default";
            }

            return Regex.Replace(architectureValue, "[a-z][A-Z]", m => m.Value[0] + "-" + m.Value[1]);
        }

        string FormatXcode(string xCodeValue)
        {
            if (string.IsNullOrEmpty(xCodeValue))
            {
                return "";
            }

            if (xCodeValue == "default")
            {
                return "Default";
            }

            if (xCodeValue == "latest")
            {
                return "Latest";
            }

            return xCodeValue.Replace("xcode4", "xcode14").Replace('_', '.')
                .Replace("xcode", "Xcode ");
        }

        string FormatAndroid(string androidValue)
        {
            if (string.IsNullOrEmpty(androidValue))
            {
                return "";
            }

            if (m_AndroidValueNameMap == null)
            {
                return androidValue;
            }

            return m_AndroidValueNameMap.GetValueOrDefault(androidValue, androidValue);;
        }

        string FormatMachineType(string machineTypeLabel)
        {
            if (string.IsNullOrEmpty(machineTypeLabel))
            {
                return "";
            }

            if (m_MachineTypeValueMap == null)
            {
                return machineTypeLabel;
            }

            var machineType = m_MachineTypeValueMap.GetValueOrDefault(machineTypeLabel, null);
            if (machineType == null)
            {
                return machineTypeLabel;
            }

            return machineType.Name.Replace("w/", "with");
        }
    }
}
