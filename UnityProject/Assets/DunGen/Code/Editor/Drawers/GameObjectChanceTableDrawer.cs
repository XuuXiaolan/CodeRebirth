using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DunGen.Editor.Drawers
{
	[CustomPropertyDrawer(typeof(GameObjectChanceTable))]
	sealed class GameObjectChanceTableDrawer : PropertyDrawer
	{
		private readonly Dictionary<string, ReorderableList> lists = new Dictionary<string, ReorderableList>();


		private ReorderableList GetOrCreateList(SerializedProperty property, GUIContent label)
		{
			ReorderableList list = null;

			if (lists.TryGetValue(property.propertyPath, out list))
				return list;
			else
			{
				var weightsProperty = property.FindPropertyRelative("Weights");
				var targetObject = property.serializedObject.targetObject;
				var chanceTable = (GameObjectChanceTable)fieldInfo.GetValue(targetObject);

				list = new ReorderableList(property.serializedObject, weightsProperty, true, true, true, true)
				{
					drawElementCallback = (rect, index, isActive, isFocused) => EditorGUI.PropertyField(rect, weightsProperty.GetArrayElementAtIndex(index), GUIContent.none),
					drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, label.text + " (" + weightsProperty.arraySize + ")"),
					elementHeightCallback = (index) => EditorGUI.GetPropertyHeight(weightsProperty.GetArrayElementAtIndex(index)),
					onAddCallback = (l) =>
					{
						Undo.RecordObject(targetObject, "Add GameObject Chance");
						chanceTable.Weights.Add(new GameObjectChance());
						Undo.FlushUndoRecordObjects();
					},
				};

				lists[property.propertyPath] = list;
				return list;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var list = GetOrCreateList(property, label);
			return list.GetHeight();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var list = GetOrCreateList(property, label);
			var attribute = fieldInfo.GetCustomAttributes(typeof(AcceptGameObjectTypesAttribute), true)
									 .Cast<AcceptGameObjectTypesAttribute>()
									 .FirstOrDefault();

			if (attribute != null)
				GameObjectChanceDrawer.FilterOverride = attribute.Filter;

			EditorGUI.BeginProperty(position, label, property);
			list.DoList(position);
			EditorGUI.EndProperty();

			if(attribute != null)
				GameObjectChanceDrawer.FilterOverride = null;
		}
	}
}
