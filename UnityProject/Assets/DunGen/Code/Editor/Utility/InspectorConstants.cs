using System;
using UnityEngine;
using UnityEditor;

namespace DunGen.Editor
{
	public static class InspectorConstants
	{
		public static readonly GUIContent AdapterPriorityLabel = new GUIContent("Priority", "Determines the order of execution of this adapter in relation to others (highest to lowest)");


		#region Layout Constants

		public static readonly GUILayoutOption SmallButtonWidth        = GUILayout.Width(19);
		public static readonly GUILayoutOption SmallWidth              = GUILayout.Width(10);
		public static readonly GUILayoutOption IntFieldWidth           = GUILayout.Width(50);
		public static readonly GUILayoutOption LabelWidth              = GUILayout.Width(120);
		
		#endregion
	}
}

