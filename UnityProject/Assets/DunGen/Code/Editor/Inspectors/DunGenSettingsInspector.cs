using UnityEditor;

namespace DunGen.Editor.Inspectors
{
	[CustomEditor(typeof(DunGenSettings))]
	public sealed class DunGenSettingsInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var obj = target as DunGenSettings;

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Default Socket", obj.DefaultSocket, typeof(DoorwaySocket), false);
			EditorGUI.EndDisabledGroup();
		}
	}
}
