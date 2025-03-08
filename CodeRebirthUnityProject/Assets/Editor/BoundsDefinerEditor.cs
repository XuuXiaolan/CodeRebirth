using UnityEngine;
using UnityEditor;
using CodeRebirth.src.MiscScripts;

[CustomEditor(typeof(BoundsDefiner))]
public class BoundsDefinerEditor : Editor
{
    // Toggle for enabling/disabling bounds editing
    private bool editBoundsEnabled = false;

    public override void OnInspectorGUI()
    {
        // Draw the default inspector first.
        DrawDefaultInspector();

        // Toggle button for bounds editing.
        bool newEditBoundsEnabled = EditorGUILayout.Toggle("Edit Bounds", editBoundsEnabled);
        if (newEditBoundsEnabled != editBoundsEnabled)
        {
            editBoundsEnabled = newEditBoundsEnabled;
            // Hide the default transform gizmos when editing.
            Tools.hidden = editBoundsEnabled;
            if (editBoundsEnabled)
            {
                // Force no tool to be active.
                Tools.current = Tool.None;
            }
            else
            {
                // Restore the default tool (Move is typical).
                Tools.current = Tool.Move;
            }
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        BoundsDefiner bd = (BoundsDefiner)target;
        Transform t = bd.transform;
        // Compute the bounds center in world space (even if the object moves, this updates automatically)
        Vector3 worldCenter = t.TransformPoint(bd.bounds.center);

        // Always draw the wire cube (this ensures that if the GameObject moves, you see the updated bounds).
        Handles.color = bd.boundColor;
        // Handles.DrawWireCube(worldCenter, bd.bounds.size);

        // Only show and allow editing of the bounds when in edit mode.
        if (editBoundsEnabled)
        {
            // Ensure the default tools are hidden.
            Tools.hidden = true;
            Tools.current = Tool.None;

            // --- Adjust the bounds center ---
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldCenter = Handles.PositionHandle(worldCenter, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(bd, "Move Bounds Center");
                // Convert from world space back to local space.
                bd.bounds.center = t.InverseTransformPoint(newWorldCenter);
            }

            // --- Adjust the bounds size ---
            EditorGUI.BeginChangeCheck();
            Vector3 newSize = Handles.ScaleHandle(bd.bounds.size, worldCenter, Quaternion.identity, HandleUtility.GetHandleSize(worldCenter));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(bd, "Scale Bounds");
                bd.bounds.size = newSize;
            }

            // Force the SceneView to update so that the handles follow any GameObject movement.
            SceneView.RepaintAll();
        }
    }

    private void OnDisable()
    {
        // When this editor is closed or deselected, restore the default tools.
        Tools.hidden = false;
        Tools.current = Tool.Move;
    }
}
