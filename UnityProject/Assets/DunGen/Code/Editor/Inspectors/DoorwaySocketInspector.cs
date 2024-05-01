using UnityEditor;
using UnityEngine;

namespace DunGen.Editor
{
	[CustomEditor(typeof(DoorwaySocket))]
	public sealed class DoorwaySocketInspector : UnityEditor.Editor
	{
		#region Labels & SerializedProperties

		private static class Labels
		{
			public static readonly GUIContent Size = new GUIContent("Size", "The size of the doorway opening. Used for visualization and portal culling");
		}

		private class Properties
		{
			public SerializedProperty Size { get; private set; }

			public Properties(SerializedObject obj)
			{
				Size = obj.FindProperty("size");
			}
		}

		#endregion

		private Properties properties;


		private void OnEnable()
		{
			properties = new Properties(serializedObject);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(properties.Size, Labels.Size);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
