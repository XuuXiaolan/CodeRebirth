// Name this script "RotateAtPointEditor"
using UnityEngine;
using UnityEditor;
using ModelReplacement;
using ModelReplacement.AvatarBodyUpdater;
using System.Linq;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;

[CustomEditor(typeof(ModelReplacementProject))]
[CanEditMultipleObjects]
public class ProjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ModelReplacementProject t = (ModelReplacementProject)target;


        EditorGUILayout.LabelField("Project Settings");
        EditorGUILayout.Separator();
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModelReplacementProject.AssetbundleName)));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModelReplacementProject.ProjectPath)));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModelReplacementProject.LCPath)));

        EditorGUILayout.Separator();

        if (GUILayout.Button("Build Project"))
        {
            t.BuildProject2();
        }


        EditorGUILayout.Separator();

        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

        EditorGUILayout.Separator();

        var customLabel = new GUIContent(t.GenerateNewProject ? "Generated Project Details" : "Generate New Project On Build");
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModelReplacementProject.GenerateNewProject)), customLabel);

        if (t.GenerateNewProject)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModelReplacementProject.ModGUID)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModelReplacementProject.ModName)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModelReplacementProject.ModShortDescription)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModelReplacementProject.ProjectNamespace)));
        }

        EditorGUILayout.Separator();

        Rect rect2 = EditorGUILayout.GetControlRect(false, 1);
        rect2.height = 1;
        EditorGUI.DrawRect(rect2, new Color(0.5f, 0.5f, 0.5f, 1));

        EditorGUILayout.Separator();

        if (t.ProjectPrefabs.Any())
        {
            if(t.ProjectPrefabs.Where(x => x != null).Where(x => x.isActiveAndEnabled).Any())
            {
                EditorGUILayout.LabelField("Scene Prefabs");
                EditorGUILayout.Separator();
                foreach (var pref in t.ProjectPrefabs.Where(x => x != null).Where(x => x.isActiveAndEnabled))
                {
                    EditorGUILayout.ObjectField(pref, typeof(ModelReplacementProject), true);
                }
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();
            }
            
            if (t.ProjectPrefabs.Where(x => x != null).Where(x => !x.isActiveAndEnabled).Any())
            {
                EditorGUILayout.LabelField("Asset Prefabs");
                EditorGUILayout.Separator();
                foreach (var pref in t.ProjectPrefabs.Where(x => x != null).Where(x => !x.isActiveAndEnabled))
                {
                    EditorGUILayout.ObjectField(pref, typeof(ModelReplacementProject), true);
                }
            }
        }


        serializedObject.ApplyModifiedProperties();
    }

}