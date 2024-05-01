using UnityEditor;
using DunGen.Graph;
using UnityEngine;
using DunGen.Editor.Validation;
using System;
using UnityEditorInternal;

namespace DunGen.Editor
{
	[CustomEditor(typeof(DungeonFlow))]
	public sealed class DungeonFlowInspector : UnityEditor.Editor
	{
		#region Helpers

		private sealed class Properties
		{
			public SerializedProperty Length;
			public SerializedProperty BranchMode;
			public SerializedProperty BranchCount;
			public SerializedProperty KeyManager;
			public SerializedProperty DoorwayConnectionChance;
			public SerializedProperty TileInjectionRules;
			public SerializedProperty RestrictConnectionToSameSection;
			public SerializedProperty TileTagConnectionMode;
			public SerializedProperty TileConnectionTags;
			public SerializedProperty BranchTagPruneMode;
			public SerializedProperty BranchPruneTags;

			public ReorderableList GlobalProps;
			public ReorderableList TileConnectionTagsList;
			public ReorderableList BranchPruneTagsList;
		}

		private static class Labels
		{
			public static readonly GUIContent Validate = new GUIContent("Validate Dungeon", "Runs a set of automated checks on the integrity of the dungeon, reporting any errors that are found");
			public static readonly GUIContent Length = new GUIContent("Length", "Min and max length of the main path. This will determine how long the dungeon is");
			public static readonly GUIContent BranchMode = new GUIContent("Branch Mode", "Determines how the number of branches is computed");
			public static readonly GUIContent BranchCount = new GUIContent("Branch Count", "The total number of branches to appear accross the entire dungeon. Only used when Branch Mode is set to Global");
			public static readonly GUIContent GlobalProps = new GUIContent("Global Props");
			public static readonly GUIContent KeyManager = new GUIContent("Key Manager", "Defines which keys are available to be placed throughout the dungeon. This can be left blank if you don't want to make use of the lock & key system");
			public static readonly GUIContent DoorwayConnectionHeader = new GUIContent("Doorway Connection");
			public static readonly GUIContent DoorwayConnectionChance = new GUIContent("Connection Chance", "The percentage chance that an unconnected but overlapping set of doorways will be connected. This can be overriden on a per-tile basis");
			public static readonly GUIContent RestrictConnectionToSameSection = new GUIContent("Restrict to Same Section", "If checked, doorways will only be connected if they lie on the same line segment in the dungeon flow graph");
			public static readonly GUIContent TileInjection = new GUIContent("Special Tile Injection", "Used to inject specific tiles into the dungeon layout based on a set of rules");
			public static readonly GUIContent OpenFlowEditor = new GUIContent("Open Flow Editor", "The node graph lets you design how the dungeon should be laid out");
			public static readonly GUIContent GlobalPropGroupID = new GUIContent("Group ID", "The prop ID. This should match the ID on the GlobalProp component placed inside Tiles");
			public static readonly GUIContent GlobalPropGroupCount = new GUIContent("Count", "The number of times this prop should appear across the entire dungeon");
			public static readonly GUIContent TileConnectionTagMode = new GUIContent("Mode", "How to apply the tag rules below. NOTE: This section is ignored if the tag pair list is empty.\n    Accept: Tiles are only connected if their tags match one of the pairs in the list below.\n    Reject: Tiles will always connect unless their tags match one of the pairs in the list below.");
			public static readonly GUIContent TileConnectionTags = new GUIContent("Tag Pairs");
			public static readonly GUIContent TileConnectionRules = new GUIContent("Tile Connection Rules", "Allows us to accept or reject a connection between two tiles based on the tags each of them have.");
			public static readonly GUIContent BranchPruneMode = new GUIContent("Branch Prune Mode", "The method by which tiles at the end of a branch are pruned based on the tags below.");
			public static readonly GUIContent BranchPruneTags = new GUIContent("Branch Prune Tags", "Tiles on the end of branches will be deleted depending on which tags they have. Based on the branch prune mode");


			public static readonly string LocalBranchMode = "In Local mode, the number of branches is calculated per-tile using the Archetype's 'Branch Count' property";
			public static readonly string GlobalBranchMode = "In Global mode, the number of branches is calculated across the entire dungeon. NOTE: The number of branches might be less than the specified minimum value, but will never be more than the maximum";
		}

		#endregion

		private Properties properties;


		private void OnEnable()
		{
			properties = new Properties()
			{
				Length = serializedObject.FindProperty("Length"),
				BranchMode = serializedObject.FindProperty("BranchMode"),
				BranchCount = serializedObject.FindProperty("BranchCount"),
				KeyManager = serializedObject.FindProperty("KeyManager"),
				DoorwayConnectionChance = serializedObject.FindProperty("DoorwayConnectionChance"),
				RestrictConnectionToSameSection = serializedObject.FindProperty("RestrictConnectionToSameSection"),
				TileInjectionRules = serializedObject.FindProperty("TileInjectionRules"),
				TileTagConnectionMode = serializedObject.FindProperty("TileTagConnectionMode"),
				TileConnectionTags = serializedObject.FindProperty("TileConnectionTags"),
				BranchTagPruneMode = serializedObject.FindProperty("BranchTagPruneMode"),
				BranchPruneTags = serializedObject.FindProperty("BranchPruneTags"),


				GlobalProps = new ReorderableList(serializedObject, serializedObject.FindProperty("GlobalProps"), true, false, true, true)
				{
					drawElementCallback = (rect, index, isActive, isFocused) => DrawGlobalProp(rect, index),
					elementHeightCallback = GetGlobalPropHeight,
				},
			};

			properties.TileConnectionTagsList = new ReorderableList(serializedObject, properties.TileConnectionTags)
			{
				drawHeaderCallback = DrawTileConnectionTagsHeader,
				drawElementCallback = DrawTileConnectionTagsElement,
			};

			properties.BranchPruneTagsList = new ReorderableList(serializedObject, properties.BranchPruneTags)
			{
				drawHeaderCallback = (Rect rect) =>
				{
					EditorGUI.LabelField(rect, Labels.BranchPruneTags);
				},
				drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
				{
					EditorGUI.PropertyField(rect, properties.BranchPruneTags.GetArrayElementAtIndex(index), GUIContent.none);
				},
			};

			var flow = target as DungeonFlow;

			if (flow != null)
			{
				foreach (var line in flow.Lines)
					line.Graph = flow;
				foreach (var node in flow.Nodes)
					node.Graph = flow;
			}
		}

		private void DrawTileConnectionTagsHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, Labels.TileConnectionTags);
		}

		private void DrawTileConnectionTagsElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			EditorGUI.PropertyField(rect, properties.TileConnectionTags.GetArrayElementAtIndex(index), GUIContent.none);
		}

		private string GetCurrentBranchModeLabel()
		{
			var dungeonFlow = target as DungeonFlow;

			switch (dungeonFlow.BranchMode)
			{
				case BranchMode.Local:
					return Labels.LocalBranchMode;
				case BranchMode.Global:
					return Labels.GlobalBranchMode;

				default:
					throw new NotImplementedException(string.Format("{0}.{1} is not implemented", typeof(BranchMode).Name, dungeonFlow.BranchMode));
			}
		}

		public override void OnInspectorGUI()
		{
			var data = target as DungeonFlow;

			if (data == null)
				return;

			serializedObject.Update();

			if (GUILayout.Button(Labels.Validate))
				DungeonValidator.Instance.Validate(data);

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(properties.KeyManager, Labels.KeyManager);
			EditorGUILayout.PropertyField(properties.Length, Labels.Length);

			// Doorway Connections
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField(Labels.DoorwayConnectionHeader, EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(properties.DoorwayConnectionChance, Labels.DoorwayConnectionChance);
			EditorGUILayout.PropertyField(properties.RestrictConnectionToSameSection, Labels.RestrictConnectionToSameSection);
			EditorGUILayout.EndVertical();

			// Branch Mode
			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.HelpBox(GetCurrentBranchModeLabel(), MessageType.Info);
			EditorGUILayout.PropertyField(properties.BranchMode, Labels.BranchMode);

			EditorGUI.BeginDisabledGroup(data.BranchMode != BranchMode.Global);
			EditorGUILayout.PropertyField(properties.BranchCount, Labels.BranchCount);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			// Branch Prune Tags
			EditorGUILayout.PropertyField(properties.BranchTagPruneMode, Labels.BranchPruneMode);
			EditorGUILayout.Space();
			properties.BranchPruneTagsList.DoLayoutList();

			EditorGUILayout.EndVertical();

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			// Open Flow Editor
			if (GUILayout.Button(Labels.OpenFlowEditor))
				DungeonFlowEditorWindow.Open(data);

			// Tile Injection Rules
			DrawTileInjectionRules(data);

			EditorGUILayout.Space();

			// Global Props
			var globalProps = properties.GlobalProps.serializedProperty;
			globalProps.isExpanded = EditorGUILayout.Foldout(globalProps.isExpanded, Labels.GlobalProps, true);

			if (globalProps.isExpanded)
			{
				EditorGUI.indentLevel++;
				properties.GlobalProps.DoLayoutList();
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			// Tile Connection Rules
			properties.TileConnectionTags.isExpanded = EditorGUILayout.Foldout(properties.TileConnectionTags.isExpanded, Labels.TileConnectionRules);

			if(properties.TileConnectionTags.isExpanded)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(properties.TileTagConnectionMode, Labels.TileConnectionTagMode);
				EditorGUILayout.Space();
				properties.TileConnectionTagsList.DoLayoutList();
				EditorGUI.indentLevel--;
			}


			if (GUI.changed)
				EditorUtility.SetDirty(data);

			serializedObject.ApplyModifiedProperties();
		}

		private float GetGlobalPropHeight(int index)
		{
			return EditorGUI.GetPropertyHeight(properties.GlobalProps.serializedProperty.GetArrayElementAtIndex(index));
		}

		private void DrawGlobalProp(Rect rect, int index)
		{
			var propProperty = properties.GlobalProps.serializedProperty.GetArrayElementAtIndex(index);
			EditorGUI.PropertyField(rect, propProperty);
		}

		private void DrawTileInjectionRules(DungeonFlow data)
		{
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			properties.TileInjectionRules.isExpanded = EditorGUILayout.Foldout(properties.TileInjectionRules.isExpanded, "Special Tile Injection", true);

			if (!properties.TileInjectionRules.isExpanded)
				return;

			int indexToRemove = -1;

			EditorGUILayout.BeginVertical("box");

			for (int i = 0; i < data.TileInjectionRules.Count; i++ )
			{
				var rule = data.TileInjectionRules[i];
				EditorGUILayout.BeginVertical("box");

				EditorGUILayout.BeginHorizontal();

				rule.TileSet = EditorGUILayout.ObjectField(rule.TileSet, typeof(TileSet), false) as TileSet;

				if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)))
					indexToRemove = i;

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();

				rule.IsRequired = EditorGUILayout.ToggleLeft("Is Required?", rule.IsRequired);
				rule.CanAppearOnMainPath = EditorGUILayout.ToggleLeft("Can appear on Main Path?", rule.CanAppearOnMainPath);
				rule.CanAppearOnBranchPath = EditorGUILayout.ToggleLeft("Can appear on Branch Path?", rule.CanAppearOnBranchPath);

				EditorGUILayout.Space();

				EditorGUI.BeginDisabledGroup(data.KeyManager == null);

				rule.IsLocked = EditorGUILayout.ToggleLeft("Locked", rule.IsLocked);

				EditorGUI.BeginDisabledGroup(!rule.IsLocked);
				EditorUtil.DrawKey(new GUIContent("Lock Type"), data.KeyManager, ref rule.LockID);
				EditorGUI.EndDisabledGroup();

				EditorGUI.EndDisabledGroup();

				EditorGUILayout.Space();

				EditorUtil.DrawLimitedFloatRange("Path Depth", rule.NormalizedPathDepth);

				bool previousEnabled = GUI.enabled;
				GUI.enabled = rule.CanAppearOnBranchPath;

				EditorUtil.DrawLimitedFloatRange("Branch Depth", rule.NormalizedBranchDepth);
				GUI.enabled = previousEnabled;

				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();
			}

			if (indexToRemove > -1)
				data.TileInjectionRules.RemoveAt(indexToRemove);

			if (GUILayout.Button("Add New Rule"))
				data.TileInjectionRules.Add(new TileInjectionRule());

			EditorGUILayout.EndVertical();
		}
	}
}
