using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Services.CloudBuild.Editor.Components
{
    /*
     * This component manages the state of different options within the ToolbarMenu by automatically toggling the status
     * of an option whenever it is selected
     * It also provides a callback for whenever the status of any option changes, which returns all currently selected values
     */

    [UxmlElement]
    internal partial class BuildAutomationToolbarSelectionMenu : ToolbarMenu
    {
        readonly HashSet<string> _selectedOptions = new();
        internal Action<string[]> OnValueChanged;
        string originalText;

        internal void AddOption(string optionName, string optionValue, bool preselected = false)
        {
            originalText = text;
            if (preselected)
            {
                _selectedOptions.Add(optionValue);
            }
            menu.AppendAction(optionName, OnOptionSelected, ActionStatusCallback, optionValue);
        }

        internal void ClearOptions()
        {
            _selectedOptions.Clear();
            text = originalText;
        }

        void OnOptionSelected(DropdownMenuAction action)
        {
            if (!_selectedOptions.Add(action.userData.ToString()))
            {
                _selectedOptions.Remove(action.userData.ToString());
            }

            switch (_selectedOptions.Count)
            {
                case 0:
                    text = originalText;
                    break;
                case 1:
                    text = originalText + $": {_selectedOptions.First()}";
                    break;
                default:
                    text = originalText + $": ({_selectedOptions.Count})";
                    break;
            }

            OnValueChanged(_selectedOptions.ToArray());
        }

        DropdownMenuAction.Status ActionStatusCallback(DropdownMenuAction action)
        {
            return _selectedOptions.Contains(action.userData.ToString()) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
        }
    }
}
