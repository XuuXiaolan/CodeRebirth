using DunGen.Graph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DunGen.Editor
{
	public static class EditorUtil
	{
		/// <summary>
		/// Draws a GUI for a game object chance table. Allowing for addition/removal of rows and
		/// modification of values and weights
		/// </summary>
		/// <param name="table">The table to draw</param>
		public static void DrawGameObjectChanceTableGUI(string objectName, GameObjectChanceTable table, List<bool> showWeights, bool allowSceneObjects, bool allowAssetObjects, UnityEngine.Object owningObject)
		{
			string title = string.Format("{0} Weights ({1})", objectName, table.Weights.Count);
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
			EditorGUI.indentLevel = 1;

			int toDeleteIndex = -1;
			GUILayout.BeginVertical("box");

			for (int i = 0; i < table.Weights.Count; i++)
			{
				var w = table.Weights[i];
				GUILayout.BeginVertical("box");
				EditorGUILayout.BeginHorizontal();

				var obj = (GameObject)EditorGUILayout.ObjectField("", w.Value, typeof(GameObject), allowSceneObjects);

				if (obj != null)
				{
					bool isAsset = EditorUtility.IsPersistent(obj);

					if (allowAssetObjects && isAsset || allowSceneObjects && !isAsset)
						w.Value = obj;
				}
				else
					w.Value = null;

				if (GUILayout.Button("x", EditorStyles.miniButton, InspectorConstants.SmallButtonWidth))
					toDeleteIndex = i;

				EditorGUILayout.EndHorizontal();

				if (i > showWeights.Count - 1)
					showWeights.Add(false);

#if UNITY_5_5_OR_NEWER
				showWeights[i] = EditorGUILayout.Foldout(showWeights[i], "Weights", true);
#else
				showWeights[i] = EditorGUILayout.Foldout(showWeights[i], "Weights");
#endif

				if (showWeights[i])
				{
					w.MainPathWeight = EditorGUILayout.FloatField("Main Path", w.MainPathWeight);
					w.BranchPathWeight = EditorGUILayout.FloatField("Branch Path", w.BranchPathWeight);

					w.DepthWeightScale = EditorGUILayout.CurveField("Depth Scale", w.DepthWeightScale, Color.white, new Rect(0, 0, 1, 1));
				}

				GUILayout.EndVertical();
			}

			if (toDeleteIndex >= 0)
			{
				Undo.RecordObject(owningObject, "Delete Chance Entry");

				table.Weights.RemoveAt(toDeleteIndex);
				showWeights.RemoveAt(toDeleteIndex);

				Undo.FlushUndoRecordObjects();
			}

			if (GUILayout.Button("Add New " + objectName))
			{
				Undo.RecordObject(owningObject, "Add Chance Entry");

				table.Weights.Add(new GameObjectChance());
				showWeights.Add(false);

				Undo.FlushUndoRecordObjects();
			}

			EditorGUILayout.EndVertical();


			// Handle dragging objects into the list
			var dragTargetRect = GUILayoutUtility.GetLastRect();
			var evt = Event.current;

			if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
			{
				var validDraggingObjects = GetValidGameObjects(DragAndDrop.objectReferences, allowSceneObjects, allowAssetObjects);

				if (dragTargetRect.Contains(evt.mousePosition) && validDraggingObjects.Any())
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						Undo.RecordObject(owningObject, "Drag Objects");
						DragAndDrop.AcceptDrag();

						foreach (var dragObject in validDraggingObjects)
						{
							table.Weights.Add(new GameObjectChance(dragObject));
							showWeights.Add(false);
						}

						Undo.FlushUndoRecordObjects();
					}
				}
			}
		}

		public static IEnumerable<GameObject> GetValidGameObjects(IEnumerable<object> objects, bool allowSceneObjects, bool allowAssets)
		{
			foreach (var gameObject in objects.OfType<GameObject>())
			{
				bool isSceneObject = gameObject.scene.handle != 0;

				if ((isSceneObject && allowSceneObjects) || (!isSceneObject && allowAssets))
					yield return gameObject;
			}
		}

		/// <summary>
		/// Draws a simple GUI for an IntRange
		/// </summary>
		/// <param name="name">A descriptive label</param>
		/// <param name="range">The range to modify</param>
		public static void DrawIntRange(string name, IntRange range)
		{
			DrawIntRange(new GUIContent(name), range);
		}

		/// <summary>
		/// Draws a simple GUI for an IntRange
		/// </summary>
		/// <param name="name">A descriptive label</param>
		/// <param name="range">The range to modify</param>
		public static void DrawIntRange(GUIContent name, IntRange range)
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.PrefixLabel(name);
			GUILayout.FlexibleSpace();
			range.Min = EditorGUILayout.IntField(range.Min, InspectorConstants.IntFieldWidth);
			EditorGUILayout.LabelField("-", InspectorConstants.SmallWidth);
			range.Max = EditorGUILayout.IntField(range.Max, InspectorConstants.IntFieldWidth);

			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// Draws a min/max slider representing a float range
		/// </summary>
		/// <param name="name">A descriptive label</param>
		/// <param name="range">The range value</param>
		/// <param name="limitMin">The lowest value of the slider</param>
		/// <param name="limitMax">The highest value of the slider</param>
		public static void DrawLimitedFloatRange(string name, FloatRange range, float limitMin = 0, float limitMax = 1)
		{
			float min = range.Min;
			float max = range.Max;

			DrawLimitedFloatRange(name, ref min, ref max, limitMin, limitMax);

			range.Min = min;
			range.Max = max;
		}

		/// <summary>
		/// Draws a min/max slider representing a float range
		/// </summary>
		/// <param name="name">A descriptive label</param>
		/// <param name="min">The current minimum value</param>
		/// <param name="max">The current maximum value</param>
		/// <param name="limitMin">The lowest value of the slider</param>
		/// <param name="limitMax">The highest value of the slider</param>
		public static void DrawLimitedFloatRange(string name, ref float min, ref float max, float limitMin, float limitMax)
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField(name);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.MinMaxSlider(ref min, ref max, limitMin, limitMax);
			min = EditorGUILayout.FloatField(min, GUILayout.Width(50));
			max = EditorGUILayout.FloatField(max, GUILayout.Width(50));

			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// Draws the GUI for a list of Unity.Object. Allows users to add/remove/modify a specific type
		/// deriving from Unity.Object (such as GameObject, or a Component type)
		/// </summary>
		/// <param name="header">A descriptive header</param>
		/// <param name="objects">The object list to edit</param>
		/// <param name="allowedSelectionTypes">The types of objects that are allowed to be selected</param>
		/// <typeparam name="T">The type of object in the list</typeparam>
		public static void DrawObjectList<T>(string header, IList<T> objects, GameObjectSelectionTypes allowedSelectionTypes, UnityEngine.Object owningObject) where T : UnityEngine.Object
		{
			DrawObjectList(new GUIContent(header), objects, allowedSelectionTypes, owningObject);
		}

		/// <summary>
		/// Draws the GUI for a list of Unity.Object. Allows users to add/remove/modify a specific type
		/// deriving from Unity.Object (such as GameObject, or a Component type)
		/// </summary>
		/// <param name="header">A descriptive header</param>
		/// <param name="objects">The object list to edit</param>
		/// <param name="allowedSelectionTypes">The types of objects that are allowed to be selected</param>
		/// <typeparam name="T">The type of object in the list</typeparam>
		public static void DrawObjectList<T>(GUIContent header, IList<T> objects, GameObjectSelectionTypes allowedSelectionTypes, UnityEngine.Object owningObject) where T : UnityEngine.Object
		{
			bool allowSceneSelection = (allowedSelectionTypes & GameObjectSelectionTypes.InScene) == GameObjectSelectionTypes.InScene;
			bool allowPrefabSelection = (allowedSelectionTypes & GameObjectSelectionTypes.Prefab) == GameObjectSelectionTypes.Prefab;

			GUILayout.BeginVertical("box");
			EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
			EditorGUI.indentLevel = 0;

			int toDeleteIndex = -1;
			GUILayout.BeginVertical("box");

			for (int i = 0; i < objects.Count; i++)
			{
				T obj = objects[i];
				EditorGUILayout.BeginHorizontal();

				T tempObj = (T)EditorGUILayout.ObjectField("", obj, typeof(T), allowSceneSelection);

				if (tempObj != null)
				{
					bool isAsset = EditorUtility.IsPersistent(tempObj);

					if ((isAsset && allowPrefabSelection) || (!isAsset && allowSceneSelection))
						objects[i] = tempObj;
				}
				else
					objects[i] = null;

				if (GUILayout.Button("x", EditorStyles.miniButton, InspectorConstants.SmallButtonWidth))
					toDeleteIndex = i;

				EditorGUILayout.EndHorizontal();
			}

			if (toDeleteIndex >= 0)
			{
				Undo.RecordObject(owningObject, "Delete Object Entry");
				objects.RemoveAt(toDeleteIndex);
				Undo.FlushUndoRecordObjects();
			}

			if (GUILayout.Button("Add New"))
			{
				Undo.RecordObject(owningObject, "Add Object Entry");
				objects.Add(default(T));
				Undo.FlushUndoRecordObjects();
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndVertical();


			// Handle dragging objects into the list
			var dragTargetRect = GUILayoutUtility.GetLastRect();
			var evt = Event.current;

			if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
			{
				bool isDraggingValidObjects = false;

				foreach (var obj in DragAndDrop.objectReferences)
				{
					if (obj is T)
					{
						isDraggingValidObjects = true;
						break;
					}
				}

				if (dragTargetRect.Contains(evt.mousePosition) && isDraggingValidObjects)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						Undo.RecordObject(owningObject, "Drag Objects");
						DragAndDrop.AcceptDrag();

						foreach (var dragObject in DragAndDrop.objectReferences.OfType<T>())
							objects.Add(dragObject);

						Undo.FlushUndoRecordObjects();
					}
				}
			}
		}

		public static void DrawKey(GUIContent label, KeyManager manager, ref int keyID)
		{
			if (manager == null)
				EditorGUILayout.LabelField("<Missing Key Manager>");
			else
			{
				string[] keyNames = manager.Keys.Select(x => x.Name).ToArray();
				GUIContent[] keyLabels = keyNames.Select(x => new GUIContent(x)).ToArray();

				var key = manager.GetKeyByID(keyID);
				int nameIndex = EditorGUILayout.Popup(label, Array.IndexOf(keyNames, key.Name), keyLabels);
				keyID = manager.GetKeyByName(keyNames[nameIndex]).ID;
			}
		}

		public static void DrawKeySelection(string label, KeyManager manager, List<KeyLockPlacement> keys, bool includeRange)
		{
			if (manager == null)
				return;

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

			int toDeleteIndex = -1;
			string[] keyNames = manager.Keys.Select(x => x.Name).ToArray();

			for (int i = 0; i < keys.Count; i++)
			{
				EditorGUILayout.BeginVertical("box");

				var key = manager.GetKeyByID(keys[i].ID);

				EditorGUILayout.BeginHorizontal();

				int nameIndex = EditorGUILayout.Popup(Array.IndexOf(keyNames, key.Name), keyNames);
				keys[i].ID = manager.GetKeyByName(keyNames[nameIndex]).ID;

				if (GUILayout.Button("x", EditorStyles.miniButton, InspectorConstants.SmallButtonWidth))
					toDeleteIndex = i;

				EditorGUILayout.EndHorizontal();

				if (includeRange)
					EditorUtil.DrawIntRange("Count", keys[i].Range);

				EditorGUILayout.EndVertical();
			}

			if (toDeleteIndex > -1)
				keys.RemoveAt(toDeleteIndex);

			if (GUILayout.Button("Add"))
				keys.Add(new KeyLockPlacement() { ID = manager.Keys[0].ID });

			EditorGUILayout.EndVertical();
		}

		public static void DrawDungeonGenerator(DungeonGenerator generator, bool isRuntimeDungeon)
		{
			generator.DungeonFlow = (DungeonFlow)EditorGUILayout.ObjectField("Dungeon Flow", generator.DungeonFlow, typeof(DungeonFlow), false);

			generator.ShouldRandomizeSeed = EditorGUILayout.Toggle(new GUIContent("Randomize Seed", "If checked, a new random seed will be created every time a dungeon is generated. If unchecked, a specific seed will be used each time"), generator.ShouldRandomizeSeed);

			if (!generator.ShouldRandomizeSeed)
				generator.Seed = EditorGUILayout.IntField(new GUIContent("Seed", "The seed used to generate a dungeon layout. Generating a dungoen multiple times with the same seed will produce the exact same results each time"), generator.Seed);

			generator.MaxAttemptCount = EditorGUILayout.IntField(new GUIContent("Max Failed Attempts", "The maximum number of times DunGen is allowed to fail at generating a dungeon layout before giving up. This only applies in-editor; in a packaged build, DunGen will keep trying indefinitely"), generator.MaxAttemptCount);
			generator.LengthMultiplier = EditorGUILayout.FloatField(new GUIContent("Length Multiplier", "Used to alter the length of the dungeon without modifying the Dungeon Flow asset. 1 = normal-length, 2 = double-length, 0.5 = half-length, etc."), generator.LengthMultiplier);
			generator.IgnoreSpriteBounds = EditorGUILayout.Toggle(new GUIContent("Ignore Sprite Bounds", "When calculating bounding boxes for tiles, if this is checked, sprited will be ignored"), generator.IgnoreSpriteBounds);

			int selectedUpDirectionIndex = (int)generator.UpDirection;
			var upDirectionDisplayOptions = new GUIContent[]
			{
				new GUIContent("+X"), new GUIContent("-X"), new GUIContent("+Y"), new GUIContent("-Y"), new GUIContent("+Z"), new GUIContent("-Z")
			};

			generator.UpDirection = (AxisDirection)EditorGUILayout.Popup(new GUIContent("Up Direction", "The up direction of the dungeon. This won't actually rotate your dungeon, but it must match the expected up-vector for your dungeon layout - usually +Y for 3D and side-on 2D, -Z for top-down 2D"), selectedUpDirectionIndex, upDirectionDisplayOptions);

			if (generator.LengthMultiplier < 0)
				generator.LengthMultiplier = 0.0f;

			if (isRuntimeDungeon)
				generator.DebugRender = EditorGUILayout.Toggle("Debug Render", generator.DebugRender);

			generator.PlaceTileTriggers = EditorGUILayout.Toggle(new GUIContent("Place Tile Triggers", "Places trigger colliders around Tiles which can be used in conjunction with the DungenCharacter component to receieve events when changing rooms"), generator.PlaceTileTriggers);
			generator.TileTriggerLayer = EditorGUILayout.LayerField(new GUIContent("Trigger Layer", "The layer to place the tile root objects on if \"Place Tile Triggers\" is checked"), generator.TileTriggerLayer);

			if (isRuntimeDungeon)
			{
				generator.GenerateAsynchronously = EditorGUILayout.Toggle(new GUIContent("Generate Asynchronously", "If checked, DunGen will generate the layout without blocking Unity's main thread, allowing for things like animated loading screens to be shown"), generator.GenerateAsynchronously);

				EditorGUI.BeginDisabledGroup(!generator.GenerateAsynchronously);
				generator.MaxAsyncFrameMilliseconds = EditorGUILayout.Slider(new GUIContent("Max Frame Time", "How many milliseconds the dungeon generation is allowed to take per-frame"), generator.MaxAsyncFrameMilliseconds, 0f, 1000f);
				generator.PauseBetweenRooms = EditorGUILayout.Slider(new GUIContent("Pause Between Rooms", "If greater than zero, the dungeon generation will pause for the set time (in seconds) after placing a room; useful for visualising the generation process"), generator.PauseBetweenRooms, 0, 5);
				EditorGUI.EndDisabledGroup();
			}

			generator.OverlapThreshold = EditorGUILayout.Slider(new GUIContent("Overlap Threshold", "Maximum distance two connected tiles are allowed to overlap without being discarded. If doorways aren't exactly on the tile's axis-aligned bounding box, two tiles can overlap slighty when connected. This property can help to fix this issue"), generator.OverlapThreshold, 0.0001f, 1.0f);

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.LabelField("Constraints", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Constraints can make dungeon generation more likely to fail. Stricter constraints increase the chance of failure.", MessageType.Info);
			EditorGUILayout.Space();

			EditorGUI.BeginChangeCheck();
			float padding = EditorGUILayout.DelayedFloatField(new GUIContent("Padding", "A minimum buffer distance between two unconnected tiles"), generator.Padding);
			if (EditorGUI.EndChangeCheck())
				generator.Padding = Mathf.Max(0f, padding);

			generator.DisallowOverhangs = EditorGUILayout.Toggle(new GUIContent("Disallow Overhangs", "If checked, two tiles cannot overlap along the Up-Vector (a room cannot spawn above another room)"), generator.DisallowOverhangs);

			EditorGUI.BeginChangeCheck();
			generator.RestrictDungeonToBounds = EditorGUILayout.Toggle(new GUIContent("Restrict to Bounds?", "If checked, tiles will only be placed within the specified bounds below. May increase generation times"), generator.RestrictDungeonToBounds);

			EditorGUI.BeginDisabledGroup(!generator.RestrictDungeonToBounds);
			generator.TilePlacementBounds = EditorGUILayout.BoundsField(new GUIContent("Placement Bounds", "Tiles are not allowed to be placed outside of these bounds"), generator.TilePlacementBounds);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndVertical();

			if (EditorGUI.EndChangeCheck() && isRuntimeDungeon)
				SceneView.RepaintAll();

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.LabelField("Global Overrides", EditorStyles.boldLabel);
			EditorGUILayout.Space();


			EditorGUILayout.BeginHorizontal();
			generator.OverrideRepeatMode = EditorGUILayout.Toggle(generator.OverrideRepeatMode, GUILayout.Width(10));
			bool previousEnabled = GUI.enabled;
			GUI.enabled = generator.OverrideRepeatMode;
			generator.RepeatMode = (TileRepeatMode)EditorGUILayout.EnumPopup("Repeat Mode", generator.RepeatMode);
			GUI.enabled = previousEnabled;
			EditorGUILayout.EndHorizontal();

			DrawOverride("Allow Tile Rotation", ref generator.OverrideAllowTileRotation, ref generator.AllowTileRotation);

			EditorGUILayout.EndVertical();
		}

		public static void DrawOverride(string label, ref bool shouldOverride, ref bool value)
		{
			EditorGUILayout.BeginHorizontal();
			shouldOverride = EditorGUILayout.Toggle(shouldOverride, GUILayout.Width(10));
			bool previousEnabled = GUI.enabled;
			GUI.enabled = shouldOverride;
			value = EditorGUILayout.Toggle(label, value);
			GUI.enabled = previousEnabled;
			EditorGUILayout.EndHorizontal();
		}

		public static void ObjectField(Rect rect, SerializedProperty property, GUIContent label, Type objectType, bool allowSceneObjects, bool allowAssets)
		{
			EditorGUI.BeginChangeCheck();
			var newValue = EditorGUI.ObjectField(rect, label, property.objectReferenceValue, objectType, allowSceneObjects);

			if (EditorGUI.EndChangeCheck())
			{
				if (newValue == null)
					property.objectReferenceValue = newValue;
				else
				{
					bool isAsset = EditorUtility.IsPersistent(newValue);

					if (isAsset && allowAssets || allowSceneObjects && !isAsset)
						property.objectReferenceValue = newValue;
				}
			}
		}

		public static void ObjectFieldLayout(SerializedProperty property, GUIContent label, Type objectType, bool allowSceneObjects, bool allowAssets)
		{
			EditorGUI.BeginChangeCheck();
			var newValue = EditorGUILayout.ObjectField(label, property.objectReferenceValue, objectType, allowSceneObjects);

			if (EditorGUI.EndChangeCheck())
			{
				if (newValue == null)
					property.objectReferenceValue = newValue;
				else
				{
					bool isAsset = EditorUtility.IsPersistent(newValue);

					if (isAsset && allowAssets || allowSceneObjects && !isAsset)
						property.objectReferenceValue = newValue;
				}
			}
		}

		public static object GetTargetObjectOfProperty(SerializedProperty property)
		{
			if (property == null)
				return null;

			string path = property.propertyPath.Replace(".Array.data[", "[");
			object obj = property.serializedObject.targetObject;

			string[] elements = path.Split('.');

			foreach (var element in elements)
			{
				if (element.Contains("["))
				{
					string elementName = element.Substring(0, element.IndexOf("["));
					int index = int.Parse(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
					obj = GetValue(obj, elementName, index);
				}
				else
				{
					obj = GetValue(obj, element);
				}
			}

			return obj;
		}

		private static object GetValue(object source, string name)
		{
			if (source == null)
				return null;

			var type = source.GetType();

			while (type != null)
			{
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

				if (f != null)
					return f.GetValue(source);

				var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

				if (p != null)
					return p.GetValue(source, null);

				type = type.BaseType;
			}

			return null;
		}

		private static object GetValue(object source, string name, int index)
		{
			var enumerable = GetValue(source, name) as IEnumerable;

			if (enumerable == null)
				return null;

			var enumerator = enumerable.GetEnumerator();

			for (int i = 0; i <= index; i++)
				if (!enumerator.MoveNext())
					return null;

			return enumerator.Current;
		}
	}
}
