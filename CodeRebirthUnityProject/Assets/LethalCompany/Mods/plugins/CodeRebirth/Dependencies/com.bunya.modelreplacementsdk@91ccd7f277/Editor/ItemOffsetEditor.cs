// Name this script "RotateAtPointEditor"
using UnityEngine;
using UnityEditor;
using ModelReplacement;
using ModelReplacement.AvatarBodyUpdater;
[CustomEditor(typeof(OffsetBuilder))]
[CanEditMultipleObjects]
public class OffsetBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        OffsetBuilder t = (OffsetBuilder)target;

        EditorGUILayout.LabelField("Base Model Properties");
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OffsetBuilder.rootPositionOffset)));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OffsetBuilder.rootScale)));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OffsetBuilder.UseNoPostProcessing)), new GUIContent("Remove Post Processing on Model"));
       // t.UseNoPostProcessing = EditorGUILayout.Toggle("Remove Post Processing on Model", t.UseNoPostProcessing);
        EditorGUILayout.Separator();
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Item Transform Properties");
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OffsetBuilder.itemPositonOffset)), new GUIContent("Item Position Offset"));

        // This is kinda gross, but manually setting variables without using serialized properties is pretty hard.
        /*
        EditorGUI.BeginChangeCheck();
        var check = EditorGUILayout.Vector3Field("Item Rotation Offset", t.itemRotationOffset.eulerAngles);
        // Equals avoids the rounding error leniency, Vector3Field doesn't tell us if it has changed...
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(t, "Changed rotation offset");
            t.itemRotationOffset.eulerAngles = check;
            EditorUtility.SetDirty(t);
        }
        */
        EditorGUILayout.Separator();
        Rect rect2 = EditorGUILayout.GetControlRect(false, 1);
        rect2.height = 1;
        EditorGUI.DrawRect(rect2, new Color(0.5f, 0.5f, 0.5f, 1));

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Project Properties");

        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OffsetBuilder.Project)), false);
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OffsetBuilder.assetName)));

        string buttonStr = "Build Project";
        if (t.Project == null)
        {
            buttonStr = "Build AssetBundle";
        }

        if (GUILayout.Button(buttonStr))
        {
            if (t.Project == null)
            {
                t.SaveAssetBundles();
            }
            t.Project.BuildProject2();
        }

        EditorGUILayout.Separator();
        Rect rect3 = EditorGUILayout.GetControlRect(false, 1);
        rect3.height = 1;
        EditorGUI.DrawRect(rect3, new Color(0.5f, 0.5f, 0.5f, 1));

        EditorGUILayout.Separator();

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Controls");
        if (t.playerObject != null)
        { t.renderPlayer = EditorGUILayout.Toggle("Render Preview Player", t.renderPlayer); }
        if (t.item != null) {  t.renderItem = EditorGUILayout.Toggle("Render Preview Item", t.renderItem); }
        if (GUILayout.Button("Reinitialize"))
        {
            Undo.RecordObject(t, "Reinitialized");
            t.Initialize(true);
            EditorUtility.SetDirty(t);
        }

        serializedObject.ApplyModifiedProperties();
    }


    public void OnSceneGUI()
    {
        OffsetBuilder t = (target as OffsetBuilder);
        //Transform handTransform = t.gameObject.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
        //Transform itemTransform = t.item.transform;

        Vector3 rootOff = t.rootPositionOffset;
        Vector3 rootSca = t.rootScale;

        Vector3 itemOff = t.itemPositonOffset;
        Quaternion itemRot = t.itemRotationOffset;


        EditorGUI.BeginChangeCheck();
        switch (Tools.current) {
            case Tool.Move:
                rootOff = Handles.PositionHandle(t.animator.GetBoneTransform(HumanBodyBones.Hips).position, Quaternion.identity);
                itemOff = Handles.PositionHandle(t.itemHolder.transform.position, t.itemHolder.transform.rotation * itemRot);
                break;
            case Tool.Scale:
                rootSca = Handles.ScaleHandle(rootSca, t.rootTransform.position, t.rootTransform.rotation);
                break;
            case Tool.Rotate:
                itemRot = Handles.RotationHandle(t.itemHolder.transform.rotation * itemRot, t.itemHolder.transform.position);
                break;
        }

        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(target, "Rotated RotateAt Point");
        switch (Tools.current) {
            case Tool.Move:
                t.rootPositionOffset = t.rootTransform.InverseTransformVector(rootOff - t.animator.GetBoneTransform(HumanBodyBones.Hips).position) + t.rootPositionOffset;

                t.itemHolder.transform.SetPositionAndRotation(itemOff, t.itemHolder.transform.rotation);
                t.itemPositonOffset = t.itemHolder.transform.localPosition;
                
                break;
            case Tool.Scale:
                t.rootScale = rootSca;
                break;
            case Tool.Rotate:
                t.itemRotationOffset = Quaternion.Inverse(t.itemHolder.transform.rotation) * itemRot;
                break;
        }
    }

    void OnEnable()
    {
        //folder = (Transform)target;
        Tools.hidden = true;
    }

    void OnDisable()
    {
        Tools.hidden = false;
    }
}