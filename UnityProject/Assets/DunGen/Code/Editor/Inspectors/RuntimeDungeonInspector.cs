using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace DunGen.Editor
{
	[CustomEditor(typeof(RuntimeDungeon))]
	public sealed class RuntimeDungeonInspector : UnityEditor.Editor
	{
		private BoxBoundsHandle placementBoundsHandle;


		private void OnEnable()
		{
#if UNITY_2017_1_OR_NEWER
			placementBoundsHandle = new BoxBoundsHandle();
#else
			placementBoundsHandle = new BoxBoundsHandle(0);
#endif
			placementBoundsHandle.SetColor(Color.magenta);
		}

		public override void OnInspectorGUI()
        {
            var dungeon = (RuntimeDungeon)target;

			Undo.RecordObject(dungeon, "Inspector");

            dungeon.GenerateOnStart = EditorGUILayout.Toggle("Generate on Start", dungeon.GenerateOnStart);
			dungeon.Root = EditorGUILayout.ObjectField(new GUIContent("Root", "An optional root object for the dungeon to be parented to. If blank, a new root GameObject will be created named \"" + Constants.DefaultDungeonRootName + "\""), dungeon.Root, typeof(GameObject), true) as GameObject;

            EditorGUILayout.BeginVertical("box");
            EditorUtil.DrawDungeonGenerator(dungeon.Generator, true);
            EditorGUILayout.EndVertical();

			Undo.FlushUndoRecordObjects();

#if UNITY_2018_3_OR_NEWER
			PrefabUtility.RecordPrefabInstancePropertyModifications(dungeon);
#endif
		}

		private void OnSceneGUI()
		{
			var dungeon = (RuntimeDungeon)target;

			if (!dungeon.Generator.RestrictDungeonToBounds)
				return;

			placementBoundsHandle.center = dungeon.Generator.TilePlacementBounds.center;
			placementBoundsHandle.size = dungeon.Generator.TilePlacementBounds.size;

			EditorGUI.BeginChangeCheck();

			using (new Handles.DrawingScope(dungeon.transform.localToWorldMatrix))
			{
				placementBoundsHandle.DrawHandle();
			}

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(dungeon, "Inspector");
				dungeon.Generator.TilePlacementBounds = new Bounds(placementBoundsHandle.center, placementBoundsHandle.size);
				Undo.FlushUndoRecordObjects();
			}
		}
	}
}
