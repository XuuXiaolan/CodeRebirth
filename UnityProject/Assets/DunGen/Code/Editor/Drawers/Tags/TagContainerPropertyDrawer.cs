using DunGen.Tags;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DunGen.Editor.Drawers.Tags
{
	[CustomPropertyDrawer(typeof(TagContainer))]
	public class TagContainerPropertyDrawer : PropertyDrawer
	{
		private Dictionary<string, ReorderableList> lists = new Dictionary<string, ReorderableList>();
		private GUIContent currentLabel;
		private SerializedProperty currentProperty;


		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			currentLabel = label;
			currentProperty = property;

			EditorGUI.BeginProperty(position, label, property);
			GetOrCreateReorderableList(property).DoLayoutList();
			EditorGUI.EndProperty();
		}

		private ReorderableList GetOrCreateReorderableList(SerializedProperty property)
		{
			ReorderableList list;

			if (lists.TryGetValue(property.propertyPath, out list))
				return list;

			list = new ReorderableList(property.serializedObject, property.FindPropertyRelative("Tags"))
			{
				drawHeaderCallback = DrawListHeader,
				drawElementCallback = DrawListElement,
			};

			lists[property.propertyPath] = list;
			return list;
		}

		private void DrawListHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, currentLabel);
		}

		private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			EditorGUI.PropertyField(rect, currentProperty.FindPropertyRelative("Tags").GetArrayElementAtIndex(index), GUIContent.none);
		}
	}
}
