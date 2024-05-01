using DunGen.Editor.Windows;
using DunGen.Tags;
using UnityEditor;
using UnityEngine;

namespace DunGen.Editor.Drawers.Tags
{
	[CustomPropertyDrawer(typeof(Tag))]
	public class TagPropertyDrawer : PropertyDrawer
	{
		#region Nested Types

		private struct MenuItemData
		{
			public SerializedProperty Property;
			public int TagID;

			public MenuItemData(SerializedProperty property, int tagID)
			{
				Property = property;
				TagID = tagID;
			}
		}

		#endregion


		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Tag tag = (Tag)EditorUtil.GetTargetObjectOfProperty(property);

			if (tag == null)
				return;

			EditorGUI.BeginProperty(position, label, property);

			var tagManager = DunGenSettings.Instance.TagManager;
			GUIContent dropdownText = new GUIContent(tag.Name ?? "Select a tag...");

			Rect rect = EditorGUI.PrefixLabel(position, label);

			if (!EditorGUI.DropdownButton(rect, dropdownText, FocusType.Passive))
				return;

			GenericMenu menu = new GenericMenu();
			
			foreach(var id in tagManager.GetTagIDs())
				menu.AddItem(new GUIContent(tagManager.TryGetNameFromID(id)), id == tag.ID, OnTagSelected, new MenuItemData(property, id));

			menu.AddItem(new GUIContent("Open Tag Editor..."), false, OnOpenTagWindow);
			menu.DropDown(rect);

			EditorGUI.EndProperty();
		}

		private void OnOpenTagWindow()
		{
			TagEditorWindow.Open();
		}

		private void OnTagSelected(object parameter)
		{
			var data = (MenuItemData)parameter;

			data.Property.FindPropertyRelative("id").intValue = data.TagID;
			data.Property.serializedObject.ApplyModifiedProperties();
		}
	}
}
