using DunGen.Tags;
using UnityEditor;
using UnityEngine;

namespace DunGen.Editor.Windows
{
	public class TagEditorWindow : EditorWindow
	{
		#region Styles

		private sealed class Styles
		{
			public GUIStyle Header { get; private set; }
			public GUIStyle DeleteButton { get; private set; }


			public Styles()
			{
				Header = new GUIStyle(EditorStyles.boldLabel)
				{
					alignment = TextAnchor.LowerCenter
				};

				DeleteButton = new GUIStyle("IconButton")
				{
					alignment = TextAnchor.MiddleCenter
				};
			}
		}

		#endregion

		private const string ShowTagIDsPrefKey = "DunGen.Tags.ShowIDs";

		public bool ShowTagIDs
		{
			get { return EditorPrefs.GetBool(ShowTagIDsPrefKey, false); }
			set { EditorPrefs.SetBool(ShowTagIDsPrefKey, value); }
		}

		private TagManager tagManager;
		private int editTagID;
		private int tagToDelete;
		private bool hasTagIDChanged;
		private string newName;
		private Styles styles;
		private Vector2 scrollPosition;


		private void OnEnable()
		{
			tagManager = DunGenSettings.Instance.TagManager;
			minSize = new Vector2(250, 250);
			maxSize = new Vector2(600, 3000);

			editTagID = -1;
			tagToDelete = -1;
			hasTagIDChanged = false;
		}

		private void OnGUI()
		{
			if (styles == null)
				styles = new Styles();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(string.Format("-- Tags ({0}) --", tagManager.TagCount), styles.Header);
			ShowTagIDs = EditorGUILayout.Toggle(new GUIContent("Show IDs"), ShowTagIDs);
			EditorGUILayout.EndHorizontal();

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			var tags = tagManager.GetTagIDs();

			for (int i = 0; i < tags.Length; i++)
				DrawTag(tags[i], i);

			EditorGUILayout.EndScrollView();

			if(tagToDelete >= 0)
			{
				tagManager.RemoveTag(tagToDelete);
				tagToDelete = -1;

				ProcessChanges();
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			if (GUILayout.Button(new GUIContent("+ Add Tag")))
			{
				int newTagID = tagManager.AddTag("New Tag");

				if (newTagID >= 0)
				{
					SetEditTagID(newTagID);
					ProcessChanges();
				}
			}
		}

		private void DrawTag(int tagId, int index)
		{
			var evt = Event.current;
			string tagName = tagManager.TryGetNameFromID(tagId);

			Color previousBackgroundColour = GUI.backgroundColor;

			GUI.backgroundColor = (index % 2 == 0) ? Color.white : Color.grey;
			EditorGUILayout.BeginHorizontal("box");
			GUI.backgroundColor = previousBackgroundColour;

			if (editTagID == tagId)
			{
				string inputName = "Tag " + tagId.ToString();
				bool wasEnterPressed = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);

				GUI.SetNextControlName(inputName);
				newName = EditorGUILayout.TextField(newName);

				var rect = GUILayoutUtility.GetLastRect();
				bool hasClickedOut = evt.type == EventType.MouseDown && !rect.Contains(evt.mousePosition);


				if (hasTagIDChanged)
				{
					EditorGUI.FocusTextInControl(inputName);
					hasTagIDChanged = false;
				}

				if (wasEnterPressed || hasClickedOut)
				{
					tagManager.TryRenameTag(tagId, newName);

					SetEditTagID(-1);
					ProcessChanges();
				}
			}
			else
			{
				string labelText;

				if (ShowTagIDs)
					labelText = string.Format("[{0}] {1}", tagId, tagName);
				else
					labelText = tagName;

				EditorGUILayout.LabelField(labelText);

				var rect = GUILayoutUtility.GetLastRect();

				if (evt.type == EventType.MouseDown)
				{
					if (rect.Contains(evt.mousePosition))
					{
						SetEditTagID(tagId);
						Repaint();
					}
				}
			}

			// Delete
			const float deleteButtonSize = 20;
			if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), styles.DeleteButton, GUILayout.Width(deleteButtonSize), GUILayout.Height(deleteButtonSize)))
			{
				if (EditorUtility.DisplayDialog("Delete Tag?", "Are you sure you want to delete this tag?", "Delete", "Cancel"))
				{
					tagToDelete = tagId;
					SetEditTagID(-1);
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		private void SetEditTagID(int id)
		{
			editTagID = id;
			hasTagIDChanged = true;
			newName = tagManager.TryGetNameFromID(id);
		}

		private void ProcessChanges()
		{
			EditorUtility.SetDirty(DunGenSettings.Instance);
			Repaint();
		}

		#region Static Methods

		[MenuItem("Window/DunGen/Tags")]
		public static TagEditorWindow Open()
		{
			var window = GetWindow<TagEditorWindow>(true, "DunGen Tags", true);
			window.Show();

			return window;
		}

		#endregion
	}
}
