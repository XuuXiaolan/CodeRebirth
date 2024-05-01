using DunGen.Graph;
using UnityEditor;
using UnityEngine;

namespace DunGen.Editor.Drawers
{
	[CustomPropertyDrawer(typeof(DungeonFlow.GlobalPropSettings))]
	sealed class GlobalPropSettingsDrawer : PropertyDrawer
	{
		private const float Margin = 5f;
		private const float PaddingBetweenElements = 2f;


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var idProperty = property.FindPropertyRelative("ID");
			var countProperty = property.FindPropertyRelative("Count");

			return	EditorGUI.GetPropertyHeight(idProperty) +
					EditorGUI.GetPropertyHeight(countProperty) +
					EditorGUIUtility.standardVerticalSpacing * 2 +
					Margin * 2 +
					PaddingBetweenElements;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = GetPropertyHeight(property, label);

			var idProperty = property.FindPropertyRelative("ID");
			var countProperty = property.FindPropertyRelative("Count");

			EditorGUI.BeginProperty(position, label, property);
			position.position += new Vector2(0, Margin);
			position.height -= Margin * 2;

			var idPosition = position;
			idPosition.height = EditorGUI.GetPropertyHeight(idProperty) + EditorGUIUtility.standardVerticalSpacing;

			var countPosition = position;
			countPosition.height = EditorGUI.GetPropertyHeight(countProperty) + EditorGUIUtility.standardVerticalSpacing;
			countPosition.position += new Vector2(0f, idPosition.height + PaddingBetweenElements);

			EditorGUI.PropertyField(idPosition, idProperty);
			EditorGUI.PropertyField(countPosition, countProperty);

			EditorGUI.EndProperty();
		}
	}
}
