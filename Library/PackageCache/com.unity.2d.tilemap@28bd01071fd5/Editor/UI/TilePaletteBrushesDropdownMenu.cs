using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Tilemaps
{
    internal class TilePaletteBrushesDropdownMenu : AbstractGenericMenu
    {
        private const float k_BrushDropdownWidth = 150f;

        private GridBrushesDropdown m_Dropdown;
        private bool m_Active = false;

        public TilePaletteBrushesDropdownMenu(bool active)
        {
            m_Dropdown = new GridBrushesDropdown(SelectBrush, k_BrushDropdownWidth);
            m_Active = active;
        }

        public override void AddItem(string itemName, bool isChecked, System.Action action)
        {
        }

        public override void AddItem(string itemName, bool isChecked, System.Action<object> action, object data)
        {
        }

        public override void AddDisabledItem(string itemName, bool isChecked)
        {
        }

        public override void AddSeparator(string path)
        {
        }

        public override void DropDown(Rect position, VisualElement targetElement, DropdownMenuSizeMode dropdownMenuSizeMode)
        {
            if (m_Active)
                PopupWindow.Show(position, m_Dropdown);
        }

        private void SelectBrush(int i, object o)
        {
            GridPaintingState.gridBrush = GridPaletteBrushes.brushes[i];
        }
    }
}
