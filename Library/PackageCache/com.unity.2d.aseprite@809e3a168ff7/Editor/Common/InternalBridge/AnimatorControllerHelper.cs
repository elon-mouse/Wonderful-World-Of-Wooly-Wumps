using UnityEditor.Animations;

namespace UnityEditor.U2D.Aseprite.Common
{
    internal static class AnimatorControllerHelper
    {
        [Callbacks.OnOpenAsset]
        static bool OnOpenAsset(int instanceID, int line)
        {
            var controller = EditorUtility.EntityIdToObject(instanceID) as AnimatorController;
            if (controller)
            {
                EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
                return true;
            }
            return false;
        }
    }
}
