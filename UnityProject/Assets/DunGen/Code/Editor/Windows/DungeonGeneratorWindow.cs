using UnityEditor;
using UnityEngine;

namespace DunGen.Editor
{
	public sealed class DungeonGeneratorWindow : EditorWindow
	{
		private DungeonGenerator generator;
		private GameObject lastDungeon;
		private bool overwriteExisting = true;


		[MenuItem("Window/DunGen/Generate Dungeon")]
		private static void OpenWindow()
		{
			GetWindow<DungeonGeneratorWindow>(false, "New Dungeon", true);
		}

		private void OnGUI()
		{
			EditorUtil.DrawDungeonGenerator(generator, false);

			EditorGUILayout.Space();

			overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing?", overwriteExisting);

			if (GUILayout.Button("Generate"))
				GenerateDungeon();
		}

		private void OnEnable()
		{
			generator = new DungeonGenerator();
			generator.OnGenerationStatusChanged += HandleGenerationStatusChanged;
		}

		private void OnDisable()
		{
			generator.OnGenerationStatusChanged -= HandleGenerationStatusChanged;
			generator = null;
		}

		private void GenerateDungeon()
		{
			if (lastDungeon != null)
			{
				if (overwriteExisting)
					UnityUtil.Destroy(lastDungeon);
				else
					generator.DetachDungeon();
			}

			lastDungeon = new GameObject("Dungeon Layout");
			generator.Root = lastDungeon;

			Undo.RegisterCreatedObjectUndo(lastDungeon, "Create Procedural Dungeon");
			generator.GenerateAsynchronously = false;
			generator.Generate();
		}

		private void HandleGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
		{
			if (status == GenerationStatus.Failed)
			{
				UnityUtil.Destroy(lastDungeon);
				lastDungeon = generator.Root = null;
			}
		}
	}
}