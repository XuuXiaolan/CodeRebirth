using UnityEngine;
using UnityEditor;
using System.Linq;

namespace DunGen.Editor
{
	[CustomEditor(typeof(RandomPrefab))]
	public class RandomPrefabInspector : UnityEditor.Editor
	{
		#region Labels

		private static class Label
		{
			public static readonly GUIContent ZeroPosition = new GUIContent("Zero Position", "Snaps the spawned prop to this GameObject's position. Otherwise, the prefab's position will be used as an offset.");
			public static readonly GUIContent ZeroRotation =new GUIContent("Zero Rotation", "Snaps the spawned prop to this GameObject's rotation. Otherwise, the prefab's rotation will be used as an offset.");
			public static readonly GUIContent Props = new GUIContent("Prefab", "Snaps the spawned prop to this GameObject's rotation. Otherwise, the prefab's rotation will be used as an offset.");
		}

		#endregion

		private SerializedProperty zeroPosition;
		private SerializedProperty zeroRotation;
		private SerializedProperty props;


		private void OnEnable()
		{
			zeroPosition = serializedObject.FindProperty("ZeroPosition");
			zeroRotation = serializedObject.FindProperty("ZeroRotation");
			props = serializedObject.FindProperty("Props");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(zeroPosition, Label.ZeroPosition);
			EditorGUILayout.PropertyField(zeroRotation, Label.ZeroRotation);

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(props, Label.Props);

			HandlePropDragAndDrop(GUILayoutUtility.GetLastRect());

			serializedObject.ApplyModifiedProperties();
		}

		private void HandlePropDragAndDrop(Rect dragTargetRect)
		{
			var evt = Event.current;

			if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
			{
				var validGameObjects = EditorUtil.GetValidGameObjects(DragAndDrop.objectReferences, false, true);

				if (dragTargetRect.Contains(evt.mousePosition) && validGameObjects.Any())
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						Undo.RecordObject(target, "Drag Prop Prefab(s)");
						DragAndDrop.AcceptDrag();

						var randomPrefabComponent = target as RandomPrefab;

						foreach (var dragObject in validGameObjects)
							randomPrefabComponent.Props.Weights.Add(new GameObjectChance(dragObject));

						Undo.FlushUndoRecordObjects();
					}
				}
			}
		}
	}
}

