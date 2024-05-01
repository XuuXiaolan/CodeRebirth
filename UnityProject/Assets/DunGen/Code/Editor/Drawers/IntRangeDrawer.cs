using UnityEditor;
using UnityEngine;

namespace DunGen.Editor.Drawers
{
	[CustomPropertyDrawer(typeof(IntRange))]
	sealed class IntRangeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Find properties
			var minProperty = property.FindPropertyRelative("Min");
			var maxProperty = property.FindPropertyRelative("Max");

			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			// Calculate rectangles
			const int fieldWidth = 50;
			const int dividerWidth = 10;
			const int padding = 5;

			float xPos = position.x;
			var minRect = new Rect(xPos, position.y, fieldWidth, position.height);
			xPos += fieldWidth + padding;
			var dividerRect = new Rect(xPos, position.y, dividerWidth, position.height);
			xPos += dividerWidth + padding;
			var maxRect = new Rect(xPos, position.y, fieldWidth, position.height);

			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(minRect, minProperty, GUIContent.none);

			EditorGUI.LabelField(dividerRect, "-");

			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(maxRect, maxProperty, GUIContent.none);

			if (EditorGUI.EndChangeCheck() && maxProperty.intValue < minProperty.intValue)
				maxProperty.intValue = minProperty.intValue;

			EditorGUI.EndProperty();
		}
	}
}
