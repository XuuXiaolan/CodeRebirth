using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace DunGen.Editor
{
	[CustomEditor(typeof(Tile))]
	public class TileInspector : UnityEditor.Editor
	{
		#region Labels

		private static class Label
		{
			public static readonly GUIContent AllowRotation = new GUIContent("Allow Rotation", "If checked, this tile is allowed to be rotated by the dungeon gennerator. This setting can be overriden globally in the dungeon generator settings");
			public static readonly GUIContent RepeatMode = new GUIContent("Repeat Mode", "Determines how a tile is able to repeat throughout the dungeon. This setting can be overriden globally in the dungeon generator settings");
			public static readonly GUIContent OverrideAutomaticTileBounds = new GUIContent("Override Automatic Tile Bounds", "DunGen automatically calculates a bounding volume for tiles. Check this option if you're having problems with the automatically generated bounds.");
			public static readonly GUIContent FitToTile = new GUIContent("Fit to Tile", "Uses DunGen's automatic bounds generating to try to fit the bounds to the tile.");
			public static readonly GUIContent Entrance = new GUIContent("Entrance", "If set, DunGen will always use this doorway as the entrance to this tile.");
			public static readonly GUIContent Exit = new GUIContent("Exit", "If set, DunGen will always use this doorway as the first exit from this tile");
			public static readonly GUIContent OverrideConnectionChance = new GUIContent("Override Connection Chance", "If checked, this tile will override the global connection chance set in the dungeon flow. If both tiles override the connection chance, the lowest value will be used");
			public static readonly GUIContent ConnectionChance = new GUIContent("Connection Chance", "The chance that this tile will be connected to an overlapping doorway");
			public static readonly GUIContent Tags = new GUIContent("Tags", "A set of user-defined tags that can be used with the dungeon flow to restrict tile connections or referenced in code to apply custom logic");
		}

		#endregion

		private SerializedProperty allowRotation;
		private SerializedProperty repeatMode;
		private SerializedProperty overrideAutomaticTileBounds;
		private SerializedProperty tileBoundsOverride;
		private SerializedProperty entrance;
		private SerializedProperty exit;
		private SerializedProperty overrideConnectionChance;
		private SerializedProperty connectionChance;
		private SerializedProperty tags;

		private BoxBoundsHandle overrideBoundsHandle;


		private void OnEnable()
		{
			allowRotation = serializedObject.FindProperty("AllowRotation");
			repeatMode = serializedObject.FindProperty("RepeatMode");
			overrideAutomaticTileBounds = serializedObject.FindProperty("OverrideAutomaticTileBounds");
			tileBoundsOverride = serializedObject.FindProperty("TileBoundsOverride");
			entrance = serializedObject.FindProperty("Entrance");
			exit = serializedObject.FindProperty("Exit");
			overrideConnectionChance = serializedObject.FindProperty("OverrideConnectionChance");
			connectionChance = serializedObject.FindProperty("ConnectionChance");
			tags = serializedObject.FindProperty("Tags");


#if UNITY_2017_1_OR_NEWER
			overrideBoundsHandle = new BoxBoundsHandle();
#else
			overrideBoundsHandle = new BoxBoundsHandle(0);
#endif

			overrideBoundsHandle.SetColor(Color.red);
		}

		public override void OnInspectorGUI()
		{
			var tile = (Tile)target;

			serializedObject.Update();

			EditorGUILayout.PropertyField(allowRotation, Label.AllowRotation);
			EditorGUILayout.PropertyField(repeatMode, Label.RepeatMode);


			// Tile Bounds Override
			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.PropertyField(overrideAutomaticTileBounds, Label.OverrideAutomaticTileBounds);

			EditorGUI.BeginDisabledGroup(!overrideAutomaticTileBounds.boolValue);

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(tileBoundsOverride, GUIContent.none);

			if (GUILayout.Button(Label.FitToTile))
				tileBoundsOverride.boundsValue = tile.transform.InverseTransformBounds(UnityUtil.CalculateObjectBounds(tile.gameObject, false, false));

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();


			// Connection Chance Override
			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.PropertyField(overrideConnectionChance, Label.OverrideConnectionChance);

			EditorGUI.BeginDisabledGroup(!overrideConnectionChance.boolValue);

			EditorGUILayout.Slider(connectionChance, 0f, 1f, Label.ConnectionChance);

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();


			// Entrance & Exit doorways
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.HelpBox("You can optionally designate doorways as the entrance / exit for this tile", MessageType.Info);

			EditorUtil.ObjectFieldLayout(entrance, Label.Entrance, typeof(Doorway), true, false);
			EditorUtil.ObjectFieldLayout(exit, Label.Exit, typeof(Doorway), true, false);

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(tags, Label.Tags);

			serializedObject.ApplyModifiedProperties();
		}

		private void OnSceneGUI()
		{
			if (!overrideAutomaticTileBounds.boolValue)
				return;

			var tile = (Tile)target;
			overrideBoundsHandle.center = tileBoundsOverride.boundsValue.center;
			overrideBoundsHandle.size = tileBoundsOverride.boundsValue.size;

			EditorGUI.BeginChangeCheck();

			using (new Handles.DrawingScope(tile.transform.localToWorldMatrix))
			{
				overrideBoundsHandle.DrawHandle();
			}

			if (EditorGUI.EndChangeCheck())
			{
				tileBoundsOverride.boundsValue = new Bounds(overrideBoundsHandle.center, overrideBoundsHandle.size);
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}