using DunGen.Tags;
using UnityEditor;
using UnityEngine;

namespace DunGen.Editor.Drawers.Tags
{
	[CustomPropertyDrawer(typeof(TagPair))]
	public class TagPairPropertyDrawer : PropertyDrawer
	{
		private static GUIStyle centeredTextStyle;


		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (centeredTextStyle == null)
			{
				centeredTextStyle = new GUIStyle(EditorStyles.boldLabel)
				{
					alignment = TextAnchor.MiddleCenter
				};
			}


			const float separatorWidth = 15f;

			EditorGUI.BeginProperty(position, label, property);

			Rect rect = EditorGUI.PrefixLabel(position, label);
			float tagWidth = (rect.width - separatorWidth) / 2f;

			Rect tagARect = new Rect(rect.x, rect.y, tagWidth, rect.height);
			Rect separatorRect = new Rect(tagARect.xMax, rect.y, separatorWidth, rect.height);
			Rect tagBRect = new Rect(separatorRect.xMax, rect.y, tagWidth, rect.height);

			EditorGUI.PropertyField(tagARect, property.FindPropertyRelative("TagA"), GUIContent.none);
			EditorGUI.LabelField(separatorRect, new GUIContent("+"), centeredTextStyle);
			EditorGUI.PropertyField(tagBRect, property.FindPropertyRelative("TagB"), GUIContent.none);

			EditorGUI.EndProperty();
		}
	}
}
