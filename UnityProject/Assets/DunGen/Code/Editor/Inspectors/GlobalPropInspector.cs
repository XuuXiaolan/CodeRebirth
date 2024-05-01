using UnityEngine;
using UnityEditor;

namespace DunGen.Editor
{
	[CustomEditor(typeof(GlobalProp))]
	[CanEditMultipleObjects]
    public class GlobalPropInspector : UnityEditor.Editor
    {
		#region Labels

		private static class Label
		{
			public static readonly GUIContent PropGroupID = new GUIContent("Group ID", "The ID used by the dungeon flow to spawn instances of this prop");
			public static readonly GUIContent MainPathWeight = new GUIContent("Main Path", "Modifies the likelyhood that this object will be spawned while on the main path. Use 0 to disallow");
			public static readonly GUIContent BranchPathWeight = new GUIContent("Branch Path", "Modifies the likelyhood that this object will be spawned while on any of the branch paths. Use 0 to disallow");
			public static readonly GUIContent DepthWeightScale = new GUIContent("Depth Scale", "Modified the likelyhood that this obhect will be spawned based on how deep into the dungeon it is");
			public static readonly GUIContent WeightsHeader = new GUIContent("Weights");
		}

		#endregion

		private SerializedProperty propGroupID;
		private SerializedProperty mainPathWeight;
		private SerializedProperty branchPathWeight;
		private SerializedProperty depthWeightScale;


		private void OnEnable()
		{
			propGroupID = serializedObject.FindProperty("PropGroupID");
			mainPathWeight = serializedObject.FindProperty("MainPathWeight");
			branchPathWeight = serializedObject.FindProperty("BranchPathWeight");
			depthWeightScale = serializedObject.FindProperty("DepthWeightScale");
		}

		public override void OnInspectorGUI()
        {
			serializedObject.Update();

			EditorGUILayout.PropertyField(propGroupID, Label.PropGroupID);

            GUILayout.BeginVertical("box");

			EditorGUILayout.LabelField(Label.WeightsHeader, EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(mainPathWeight, Label.MainPathWeight);
			EditorGUILayout.PropertyField(branchPathWeight, Label.BranchPathWeight);

            EditorGUILayout.CurveField(depthWeightScale, Color.white, new Rect(0, 0, 1, 1), Label.DepthWeightScale);

            GUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();
        }
    }
}