using UnityEditor;
using UnityEngine;

namespace DunGen.Editor
{
	[CustomEditor(typeof(KeyManager))]
	public sealed class KeyManagerInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			KeyManager keyManager = target as KeyManager;

			if (keyManager == null)
				return;

			DrawKeyListGUI(keyManager);

			if (GUI.changed)
				EditorUtility.SetDirty(keyManager);
		}

		private void DrawKeyListGUI(KeyManager manager)
		{
			int toDeleteIndex = -1;

			for (int i = 0; i < manager.Keys.Count; i++)
			{
				var key = manager.Keys[i];

				EditorGUILayout.BeginVertical("box");

				EditorGUILayout.BeginHorizontal();

				string name = EditorGUILayout.TextField("Name", key.Name);

				if (!string.IsNullOrEmpty(name))
					manager.RenameKey(i, name);

				if (GUILayout.Button("x", EditorStyles.miniButton, InspectorConstants.SmallButtonWidth))
					toDeleteIndex = i;

				EditorGUILayout.EndHorizontal();

				key.Prefab = (GameObject)EditorGUILayout.ObjectField(key.Prefab, typeof(GameObject), false);
				key.Colour = EditorGUILayout.ColorField(key.Colour);
				EditorUtil.DrawIntRange("Keys per Lock", key.KeysPerLock);

				if (key.KeysPerLock.Min < 1)
					key.KeysPerLock.Min = 1;

				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.Space();

			if (toDeleteIndex != -1)
				manager.DeleteKey(toDeleteIndex);

			if (GUILayout.Button("Add New Key"))
				manager.CreateKey();
		}
	}
}
