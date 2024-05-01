using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DunGen.Editor.Drawers
{
	[CustomPropertyDrawer(typeof(GameObjectChance))]
	sealed class GameObjectChanceDrawer : PropertyDrawer
	{
		#region Labels

		private static class Label
		{
			public static readonly GUIContent WeightsFoldout = new GUIContent("Weights");
			public static readonly GUIContent MainPathWeight = new GUIContent("Main Path");
			public static readonly GUIContent BranchPathWeight = new GUIContent("Branch Path");
			public static readonly GUIContent DepthWeightScale = new GUIContent("Depth Scale");
		}

		#endregion

		public static GameObjectFilter? FilterOverride = GameObjectFilter.All;

		public const float Padding = 5;


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var mainPathWeightProperty = property.FindPropertyRelative("MainPathWeight");

			float height = EditorGUIUtility.singleLineHeight;
			height *= (mainPathWeightProperty.isExpanded) ? 5 : 2;
			height += Padding * 2;

			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var filter = FilterOverride ?? GameObjectFilter.All;
			var attribute = fieldInfo.GetCustomAttributes(typeof(AcceptGameObjectTypesAttribute), true)
									 .Cast<AcceptGameObjectTypesAttribute>()
									 .FirstOrDefault();

			if (attribute != null)
				filter = attribute.Filter;

			bool allowSceneObjects = (filter & GameObjectFilter.Scene) == GameObjectFilter.Scene;
			bool allowAssets = (filter & GameObjectFilter.Asset) == GameObjectFilter.Asset;

			// Find properties
			var valueProperty = property.FindPropertyRelative("Value");
			var mainPathWeightProperty = property.FindPropertyRelative("MainPathWeight");
			var branchPathWeightProperty = property.FindPropertyRelative("BranchPathWeight");
			var depthWeightScaleProperty = property.FindPropertyRelative("DepthWeightScale");


			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
			GUI.Box(position, GUIContent.none);

			var controlRect = new Rect(position.x + Padding, position.y + Padding, position.width - (Padding * 2), EditorGUIUtility.singleLineHeight);

			EditorUtil.ObjectField(controlRect, valueProperty, GUIContent.none, typeof(GameObject), allowSceneObjects, allowAssets);
			MoveRectToNextLine(ref controlRect);

			float foldoutOffset = EditorStyles.foldout.padding.left;
			var foldoutRect = new Rect(controlRect.x + foldoutOffset, controlRect.y, controlRect.width - foldoutOffset, controlRect.height);
			mainPathWeightProperty.isExpanded = EditorGUI.Foldout(foldoutRect, mainPathWeightProperty.isExpanded, "Weights", true);
			MoveRectToNextLine(ref controlRect);

			if (mainPathWeightProperty.isExpanded)
			{
				EditorGUI.PropertyField(controlRect, mainPathWeightProperty, Label.MainPathWeight);
				MoveRectToNextLine(ref controlRect);
				EditorGUI.PropertyField(controlRect, branchPathWeightProperty, Label.BranchPathWeight);
				MoveRectToNextLine(ref controlRect);

				EditorGUI.CurveField(controlRect, depthWeightScaleProperty, Color.white, new Rect(0, 0, 1, 1), Label.DepthWeightScale);
			}

			EditorGUI.EndProperty();
		}

		private void MoveRectToNextLine(ref Rect controlRect)
		{
			controlRect = new Rect(controlRect.x, controlRect.y + EditorGUIUtility.singleLineHeight, controlRect.width, controlRect.height);
		}
	}
}
