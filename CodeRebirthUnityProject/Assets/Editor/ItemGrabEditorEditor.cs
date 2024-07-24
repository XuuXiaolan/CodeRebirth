using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(ItemGrabEditor))]
public class ItemGrabEditorEditor : Editor
{
    private SerializedProperty parentObjectProp;
    private SerializedProperty itemProp;
    private SerializedProperty previewGrabProp;
    private SerializedProperty additionalRotationProp;
    private SerializedProperty positionOffsetProp;
    private SerializedProperty previewAnimationProp;
    private SerializedProperty targetTransformNameProp;

    private void OnEnable()
    {
        parentObjectProp = serializedObject.FindProperty("parentObject");
        itemProp = serializedObject.FindProperty("item");
        previewGrabProp = serializedObject.FindProperty("previewGrab");
        additionalRotationProp = serializedObject.FindProperty("additionalRotation");
        positionOffsetProp = serializedObject.FindProperty("positionOffset");
        previewAnimationProp = serializedObject.FindProperty("previewAnimation");
        targetTransformNameProp = serializedObject.FindProperty("targetTransformName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(parentObjectProp);
        EditorGUILayout.PropertyField(itemProp);
        EditorGUILayout.PropertyField(previewGrabProp);
        EditorGUILayout.PropertyField(additionalRotationProp);
        EditorGUILayout.PropertyField(positionOffsetProp);
        EditorGUILayout.PropertyField(previewAnimationProp);
        EditorGUILayout.PropertyField(targetTransformNameProp);

        if (GUILayout.Button("Set Parent Transform by Name"))
        {
            ((ItemGrabEditor)target).SetParentTransformByName(targetTransformNameProp.stringValue);
        }

        if (GUILayout.Button("Find First GrabbableObject in Scene"))
        {
            ((ItemGrabEditor)target).FindFirstGrabbableObjectInScene();
        }

        if (GUILayout.Button("Apply Default Rotation"))
        {
            ((ItemGrabEditor)target).ApplyDefaultRotation();
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Apply Default Position Offset"))
        {
            ((ItemGrabEditor)target).ApplyDefaultPositionOffset();
            SceneView.RepaintAll();
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            ((ItemGrabEditor)target).UpdateItemTransform();
            SceneView.RepaintAll();
        }

        // Automatically set the preview animation to loop if it's assigned
        if (previewAnimationProp.objectReferenceValue != null)
        {
            SetClipLooping((ItemGrabEditor)target, true);
            PlayPreviewAnimation((ItemGrabEditor)target);
        }

        if (previewGrabProp.boolValue)
        {
            EditorGUILayout.HelpBox("Preview is active. Adjust the item's transform in the scene view to match the player's hand.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Preview is inactive. Toggle 'Preview Grab' to see changes in real-time.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void PlayPreviewAnimation(ItemGrabEditor editor)
    {
        if (editor.animator != null && editor.previewAnimation != null)
        {
            // Get the animator controller
            AnimatorController animatorController = editor.animator.runtimeAnimatorController as AnimatorController;
            if (animatorController != null)
            {
                // Find the first state after the "Entry" node in the first layer
                AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
                AnimatorState firstState = null;

                foreach (ChildAnimatorState state in stateMachine.states)
                {
                    if (stateMachine.defaultState == state.state)
                    {
                        firstState = state.state;
                        break;
                    }
                }

                if (firstState != null)
                {
                    // Replace the animation clip in the state's motion
                    if (firstState.motion is AnimationClip)
                    {
                        firstState.motion = editor.previewAnimation;
                    }

                    // Play the state
                    editor.animator.Play(firstState.nameHash);
                }
                else
                {
                    Debug.LogError("No state found after the Entry node in the first layer.");
                }
            }
            else
            {
                Debug.LogError("AnimatorController not found.");
            }
        }
        else
        {
            Debug.LogError("Animator or preview animation is null.");
        }
    }

    private void SetClipLooping(ItemGrabEditor editor, bool isLooping)
    {
        if (editor.previewAnimation != null)
        {
            SerializedObject serializedClip = new SerializedObject(editor.previewAnimation);
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(editor.previewAnimation);
            clipSettings.loopTime = isLooping;
            AnimationUtility.SetAnimationClipSettings(editor.previewAnimation, clipSettings);
            serializedClip.ApplyModifiedProperties();
            Debug.Log($"Set {editor.previewAnimation.name} looping to {isLooping}");
        }
        else
        {
            Debug.LogError("Preview animation is null.");
        }
    }
}
