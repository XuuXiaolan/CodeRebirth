using System.IO;
using UnityEditor;
using UnityEngine;

namespace INab.Common
{
    [CustomEditor(typeof(InteractiveEffectMask)), CanEditMultipleObjects]
    public class InteractiveEffectsMaskEditor : Editor
    {
        private InteractiveEffectMask mask;

        // Serialized properties
        private SerializedProperty type, maskSettings;
        private SerializedProperty usePreview, onlyEditorPreview;

        private void OnEnable()
        {
            mask = (InteractiveEffectMask)target;

            // Finding serialized properties
            type = serializedObject.FindProperty("type");
            maskSettings = serializedObject.FindProperty("maskSettings");
            usePreview = serializedObject.FindProperty("usePreview");
            onlyEditorPreview = serializedObject.FindProperty("onlyEditorPreview");
        }

        private InteractiveEffectMaskSettings GetDefaultMaskSettings(InteractiveEffectMask targetMask)
        {
            InteractiveEffectMaskSettings maskSettings = targetMask.maskSettings;
            if (maskSettings == null)
            {
                MonoScript monoScript = MonoScript.FromScriptableObject(this);
                string path = AssetDatabase.GetAssetPath(monoScript);
                string directory = Path.GetDirectoryName(path);

                maskSettings = AssetDatabase.LoadAssetAtPath<InteractiveEffectMaskSettings>(directory + "/Mask Settings.asset");
                targetMask.maskSettings = maskSettings;
            }

            return maskSettings;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var targetMask = (InteractiveEffectMask)target;
            GetDefaultMaskSettings(targetMask);


            // Main settings section
            DrawMainSettings();

            InteractiveEffectMaskType currentType = (InteractiveEffectMaskType)type.enumValueIndex;

            // Preview settings
            DrawPreview(currentType);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMainSettings()
        {
            EditorGUILayout.LabelField("Main Settings", EditorStyles.boldLabel);
            using (var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(type);


                EditorGUILayout.PropertyField(maskSettings);
                EditorGUILayout.PropertyField(usePreview);
                if (usePreview.boolValue)
                {
                    EditorGUILayout.PropertyField(onlyEditorPreview);
                }

            }
        }

        private void DrawPreview(InteractiveEffectMaskType currentType)
        {
            if (!usePreview.boolValue)
            {
                if (mask.HasMaskPreview)
                {
                    if (GUILayout.Button("Destroy Preview"))
                    {
                        mask.DestroyPreview();
                    }
                }
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mask Preview Settings", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawPreviewManagementButtons();
            }
        }

        private void DrawPreviewManagementButtons()
        {
            if (mask.HasMaskPreview)
            {
                if (GUILayout.Button("Update Preview"))
                {
                    mask.UpdatePreview();
                }
                if (GUILayout.Button("Destroy Preview"))
                {
                    mask.DestroyPreview();
                }
            }
            else
            {
                if (GUILayout.Button("Add Preview"))
                {
                    mask.UpdatePreview();
                }
            }
        }

        private void OnSceneGUI()
        {
            foreach (var target in Selection.gameObjects)
            {
                InteractiveEffectMask targetMask = target?.GetComponent<InteractiveEffectMask>();
                if (targetMask == null) continue;

                DrawMaskGizmos(targetMask);
            }
        }

        private void DrawMaskGizmos(InteractiveEffectMask targetMask)
        {
            InteractiveEffectMaskSettings settings = EnsureMaskSettings(targetMask);
            Transform targetTransform = targetMask.transform;
            Handles.color = settings.Color;

            InteractiveEffectMaskSettings maskSettings = GetDefaultMaskSettings(targetMask);

            // Drawing gizmos based on mask type
            switch (targetMask.Type)
            {
                case InteractiveEffectMaskType.Plane:
                    SDFHandlesUtilities.DrawArrow(targetTransform, maskSettings.NormalSize);
                    SDFHandlesUtilities.DrawPlane(targetTransform, maskSettings.PlaneSize);
                    break;
                case InteractiveEffectMaskType.Box:
                    SDFHandlesUtilities.DrawBox(targetTransform);
                    break;
                case InteractiveEffectMaskType.Ellipse:
                    SDFHandlesUtilities.DrawEllipse(targetTransform);
                    break;
            }
        }

        private InteractiveEffectMaskSettings EnsureMaskSettings(InteractiveEffectMask targetMask)
        {
            if (targetMask.maskSettings == null)
            {
                MonoScript monoScript = MonoScript.FromScriptableObject(this);
                string path = AssetDatabase.GetAssetPath(monoScript);
                string directory = Path.GetDirectoryName(path);
                return AssetDatabase.LoadAssetAtPath<InteractiveEffectMaskSettings>(directory + "/Mask Settings.asset");
            }
            return targetMask.maskSettings;
        }

    }
}