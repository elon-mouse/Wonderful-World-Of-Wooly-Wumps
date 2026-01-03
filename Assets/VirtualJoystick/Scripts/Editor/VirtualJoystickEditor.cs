using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
namespace Terresquall {

    [CustomEditor(typeof(VirtualJoystick))]
    [CanEditMultipleObjects]
    public class VirtualJoystickEditor : Editor {

        VirtualJoystick joystick;
        RectTransform rectTransform;
        Canvas rootCanvas;

        const float HANDLE_SIZE = 5f;

        private static readonly List<int> usedIDs = new List<int>();

        public float GetHandleSize() {
            if(rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return HANDLE_SIZE / 90;
            return HANDLE_SIZE;
        }

        void OnEnable() {
            joystick = target as VirtualJoystick;
            rectTransform = joystick.GetComponent<RectTransform>();
            rootCanvas = joystick.GetRootCanvas();
        }

        static VirtualJoystick[] FindAll() {
#if UNITY_2022_2_OR_NEWER
            return FindObjectsByType<VirtualJoystick>(FindObjectsSortMode.None);
#else
            return FindObjectsOfType<VirtualJoystick>();
#endif
        }

        // Does the passed joystick have an ID that is unique to itself?
        bool HasUniqueID(VirtualJoystick vj) {
            foreach(VirtualJoystick v in FindAll()) {
                if(v == vj) continue;
                if(v.ID == vj.ID) return false;
            }
            return true;
        }

        // Is a given ID value already used by another joystick?
        bool IsAvailableID(int id) {
            foreach (VirtualJoystick v in FindAll()) {
                if(v.ID == id) return false;
            }
            return true;
        }

        // Do all the joysticks have unique IDs.
        bool HasRepeatIDs() {
            usedIDs.Clear();
            foreach(VirtualJoystick vj in FindAll()) {
                if(usedIDs.Contains(vj.ID)) return true;
                usedIDs.Add(vj.ID);
            }
            return false;
        }

        // Reassign all IDs for all Joysticks.
        void ReassignAllIDs(VirtualJoystick exception = null) {
            foreach (VirtualJoystick vj in FindAll()) {
                // Ignore joysticks that are already unique.
                if(exception == vj || HasUniqueID(vj)) continue;
                ReassignThisID(vj);
            }
        }

        // Reassign the ID for this Joystick only.
        void ReassignThisID(VirtualJoystick vj) {

            // Save the action in the History.
            Undo.RecordObject(vj, "Generate Unique Joystick ID");

            // Get all joysticks so that we can check against it if the ID is valid.
            VirtualJoystick[] joysticks = FindAll();
            for(int i = 0; i < joysticks.Length; i++) {
                if(IsAvailableID(i)) {
                    vj.ID = i; // If we find an unused ID, use it.
                    EditorUtility.SetDirty(vj);
                    return;
                }
            }

            // If all of the IDs are used, we will have to use length + 1 as the ID.
            vj.ID = joysticks.Length;
            EditorUtility.SetDirty(vj);
        }

        public override void OnInspectorGUI() {
            //Checks if Joystick's Pivot is centred
            if (rectTransform != null && (Mathf.Abs(rectTransform.pivot.x - 0.5f) > 0.01f || Mathf.Abs(rectTransform.pivot.y - 0.5f) > 0.01f))
            {
                //displays warning and button to recentre pivot
                EditorGUILayout.HelpBox("Your pivot is not centred (should be 0.5, 0.5). This can cause the joystick to be unusable.", MessageType.Error);
                Debug.LogError("Your pivot is not centred (should be 0.5, 0.5). This can cause the joystick to be unusable.");
                if (GUILayout.Button("Fix: Centre Pivot"))
                {
                    Undo.RecordObject(rectTransform, "Center Pivot");
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    EditorUtility.SetDirty(rectTransform);
                }
                EditorGUILayout.Space();
            }


            // Draw a help text box if this is not attached to a Canvas.
            if (!EditorUtility.IsPersistent(target)) {
                if (!rootCanvas)
                    EditorGUILayout.HelpBox("This joystick needs to be parented to a Canvas, or it won't work!", MessageType.Error);
                else if(rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    EditorGUILayout.HelpBox("This joystick is parented to a Canvas that is not set to Screen Space - Overlay. It may be buggy or fail to work entirely.", MessageType.Error);
            }
            
            // Show this only when both input systems are used.
#if ENABLE_INPUT_SYSTEM
    #if ENABLE_LEGACY_INPUT_MANAGER
            EditorGUILayout.HelpBox("Both of Unity's Input Systems are enabled on this project. Virtual Joystick will default to using the old Input Manager to maintain compatibility with Unity Remote.", MessageType.Info);
    #endif
#endif

            // Draw all the inspector properties.
            serializedObject.Update();
            SerializedProperty property = serializedObject.GetIterator();
            bool snapsToTouch = true;
            int directions = 0;

            if (property.NextVisible(true)) {
                do {
                    // If the property name is snapsToTouch, record its value.
                    switch(property.name) {
                        case "m_Script":
                            continue;
                        case "snapsToTouch":
                            snapsToTouch = property.boolValue;
                            break;
                        case "directions":
                            directions = property.intValue;
                            break;
                        case "boundaries":
                            // If snapsToTouch is off, don't render boundaries.
                            if(!snapsToTouch) continue;
                            break;
                        case "angleOffset":
                            if(directions <= 0) continue;
                            break;
                    }

                    EditorGUI.BeginChangeCheck();

                    // Print different properties based on what the property is.
                    if(property.name == "angleOffset") {
                        float maxAngleOffset = 360f / directions / 2;
                        EditorGUILayout.Slider(property, -maxAngleOffset, maxAngleOffset, new GUIContent("Angle Offset"));
                    } else {
                        EditorGUILayout.PropertyField(property, true);
                    }

                    EditorGUI.EndChangeCheck();

                    // If the property is an ID, show a button allowing us to reassign the IDs.
                    if(property.name == "ID" && !EditorUtility.IsPersistent(target)) {
                        if(!HasUniqueID(joystick)) {
                            EditorGUILayout.HelpBox("This Virtual Joystick doesn't have a unique ID. Please assign a unique ID or click on the button below.", MessageType.Warning);
                            if(GUILayout.Button("Generate Unique Joystick ID")) {
                                ReassignThisID(joystick);
                            }
                            EditorGUILayout.Space();
                        } else if(HasRepeatIDs()) {
                            EditorGUILayout.HelpBox("At least one of your Virtual Joysticks doesn't have a unique ID. Please ensure that all of them have unique IDs, or they may not be able to collect input properly.", MessageType.Warning);
                            EditorGUILayout.Space();
                        }
                    }
                    
                } while (property.NextVisible(false));
            }

            serializedObject.ApplyModifiedProperties();

            //Increase Decrease buttons
            if(joystick) {

                if(!joystick.controlStick) {
                    EditorGUILayout.HelpBox("There is no Control Stick assigned. This joystick won't work.", MessageType.Warning);
                    return;
                }
                
                if (!joystick.controlStick.transform.IsChildOf(joystick.transform)) {
                    EditorGUILayout.HelpBox("The control stick of this joystick is not a child of this joystick.", MessageType.Warning);
                    return;
                } 

                // Add the heading for the size adjustments.
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("Size Adjustments");
                GUILayout.BeginHorizontal();

                // Create the Increase / Decrease Size buttons and code the actions.
                bool increaseSize = GUILayout.Button("Increase Size", EditorStyles.miniButtonLeft),
                     decreaseSize = GUILayout.Button("Decrease Size", EditorStyles.miniButtonRight);

                if(increaseSize || decreaseSize) {
                    // Calculate the sizes needed for the increment / decrement.
                    int gcd = Mathf.RoundToInt(FindGCD((int)rectTransform.sizeDelta.x, (int)joystick.controlStick.rectTransform.sizeDelta.x));
                    Vector2 denominator = new Vector2(gcd, gcd);

                    // Record actions for all elements.
                    RectTransform[] affected = rectTransform.GetComponentsInChildren<RectTransform>();
                    RecordSizeChangeUndo(affected);

                    // Increase / decrease size actions.
                    if(increaseSize) {
                        foreach(RectTransform r in affected)
                            r.sizeDelta += r.sizeDelta / denominator;
                    } else if(decreaseSize) {
                        foreach(RectTransform r in affected)
                            r.sizeDelta -= r.sizeDelta / denominator;
                    }
                }

                GUILayout.EndHorizontal();
                
                EditorGUI.EndChangeCheck();
            }
        }


        void OnSceneGUI() {
            VirtualJoystick vj = (VirtualJoystick)target;

            GUILayout.Space(10);
            float radius = vj.GetRadius();

            // Draw the radius of the joystick.
            Handles.color = new Color(0, 1, 0, 0.1f);
            Handles.DrawSolidArc(vj.transform.position, Vector3.forward, Vector3.right, 360, radius);
            Handles.color = new Color(0, 1, 0, 0.5f);
            Handles.DrawWireArc(vj.transform.position, Vector3.forward, Vector3.right, 360, radius, 3f);

            // Draw the deadzone.
            Handles.color = new Color(1, 0, 0, 0.2f);
            Handles.DrawSolidArc(vj.transform.position, Vector3.forward, Vector3.right, 360, radius * vj.deadzone);
            Handles.color = new Color(1, 0, 0, 0.5f);
            Handles.DrawWireArc(vj.transform.position, Vector3.forward, Vector3.right, 360, radius * vj.deadzone, 3f);

            // Draw the boundaries of the joystick.
            if (vj.GetBounds().size.sqrMagnitude > 0) {

                // Draw the lines of the bounds.
                Handles.color = Color.yellow;

                // Get the 4 points in the bounds (in pixels).
                Vector3 bottomLeft = new Vector3(vj.boundaries.x, vj.boundaries.y);
                Vector3 topLeft = new Vector3(vj.boundaries.x, vj.boundaries.y + vj.boundaries.height);
                Vector3 topRight = new Vector3(vj.boundaries.x + vj.boundaries.width, vj.boundaries.y + vj.boundaries.height);
                Vector3 bottomRight = new Vector3(vj.boundaries.x + vj.boundaries.width, vj.boundaries.y);

                // Draw the boundary lines
                Handles.DrawLine(bottomLeft, topLeft);
                Handles.DrawLine(topLeft, topRight);
                Handles.DrawLine(topRight, bottomRight);
                Handles.DrawLine(bottomRight, bottomLeft);

                // Calculate the center point of the boundaries
                Vector3 center = new Vector3(vj.boundaries.x + vj.boundaries.width / 2, vj.boundaries.y + vj.boundaries.height / 2);

                // Add a draggable handle in the center to move the boundaries
                Handles.color = Color.yellow;
                float size = GetHandleSize();
                EditorGUI.BeginChangeCheck();
#if UNITY_2022_1_OR_NEWER
                //Circle Handles
                Vector3 newCenter = Handles.FreeMoveHandle(center, size, Vector3.zero, Handles.CircleHandleCap);
#else
                Vector3 newCenter = Handles.FreeMoveHandle(center, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
#endif
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(vj, "Move Joystick Boundaries");

                    // Move the boundaries based on the handle's new position
                    float offsetX = newCenter.x - center.x;
                    float offsetY = newCenter.y - center.y;

                    vj.boundaries.x += offsetX;
                    vj.boundaries.y += offsetY;

                    EditorUtility.SetDirty(vj);
                }

                // Add draggable handles for the corners
                EditorGUI.BeginChangeCheck();
#if UNITY_2022_1_OR_NEWER
                //Circle handles
                Vector3 newBottomLeft = Handles.FreeMoveHandle(bottomLeft, size, Vector3.zero, Handles.CircleHandleCap);
                Vector3 newTopLeft = Handles.FreeMoveHandle(topLeft, size, Vector3.zero, Handles.CircleHandleCap);
                Vector3 newTopRight = Handles.FreeMoveHandle(topRight, size, Vector3.zero, Handles.CircleHandleCap);
                Vector3 newBottomRight = Handles.FreeMoveHandle(bottomRight, size, Vector3.zero, Handles.CircleHandleCap);
#else
                //Circle handles
                Vector3 newBottomLeft = Handles.FreeMoveHandle(bottomLeft, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
                Vector3 newTopLeft = Handles.FreeMoveHandle(topLeft, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
                Vector3 newTopRight = Handles.FreeMoveHandle(topRight, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
                Vector3 newBottomRight = Handles.FreeMoveHandle(bottomRight, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
#endif
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(vj, "Resize Joystick Boundaries");

                    // Determine which handle moved and apply appropriate changes
                    if (newBottomLeft != bottomLeft) {
                        // Bottom left affects x, y, width, height
                        float deltaX = newBottomLeft.x - bottomLeft.x;
                        float deltaY = newBottomLeft.y - bottomLeft.y;
                        vj.boundaries.x += deltaX;
                        vj.boundaries.y += deltaY;
                        vj.boundaries.width -= deltaX;
                        vj.boundaries.height -= deltaY;
                    } else if (newTopLeft != topLeft) {
                        // Top left affects x and width (moving left edge) and height (moving top edge)
                        float deltaX = newTopLeft.x - topLeft.x;
                        float deltaY = newTopLeft.y - topLeft.y;
                        vj.boundaries.x += deltaX;
                        vj.boundaries.width -= deltaX;
                        vj.boundaries.height += deltaY;
                    } else if (newTopRight != topRight) {
                        // Top right affects width and height
                        float deltaX = newTopRight.x - topRight.x;
                        float deltaY = newTopRight.y - topRight.y;
                        vj.boundaries.width += deltaX;
                        vj.boundaries.height += deltaY;
                    } else if (newBottomRight != bottomRight) {
                        // Bottom right affects width (moving right edge) and y, height (moving bottom edge)
                        float deltaX = newBottomRight.x - bottomRight.x;
                        float deltaY = newBottomRight.y - bottomRight.y;
                        vj.boundaries.width += deltaX;
                        vj.boundaries.y += deltaY;
                        vj.boundaries.height -= deltaY;
                    }

                    // Ensure minimum size
                    vj.boundaries.width = Mathf.Max(1, vj.boundaries.width);
                    vj.boundaries.height = Mathf.Max(1, vj.boundaries.height);

                    EditorUtility.SetDirty(vj);
                }
            }

            // Draw the direction anchors of the joystick.
            if (vj.directions > 0) {
                Handles.color = Color.blue;
                float partition = 360f / vj.directions;
                for (int i = 0; i < vj.directions; i++) {
                    Handles.DrawLine(vj.transform.position, vj.transform.position + Quaternion.Euler(0, 0, i * partition + vj.angleOffset) * Vector2.right * radius, 2f);
                }
            }
        }

        // Function to return gcd of a and b
        int GCD(int a, int b) {
            if (b == 0) return a;
            return GCD(b, a % b);
        }

        // Function to find gcd of array of numbers
        int FindGCD(params int[] numbers) {
            if (numbers.Length == 0) {
                Debug.LogError("No numbers provided");
                return 0; // or handle the error in an appropriate way
            }

            int result = numbers[0];
            for (int i = 1; i < numbers.Length; i++) {

                result = GCD(result, numbers[i]);

                if (result == 1) {
                    return 1;
                } else if (result <= 0) {
                    Debug.LogError("The size value for one or more of the Joystick elements is not more than 0");
                    // You might want to handle this error in an appropriate way
                    return 0; // or handle the error in an appropriate way
                }
            }
            return result;
        }

        void RecordSizeChangeUndo(UnityEngine.Object[] arguments) {
            for (int i = 0; i < arguments.Length; i++) {
                Undo.RecordObject(arguments[i], "Undo Virtual Joystick Size Change");
            }
        }
    }
}
