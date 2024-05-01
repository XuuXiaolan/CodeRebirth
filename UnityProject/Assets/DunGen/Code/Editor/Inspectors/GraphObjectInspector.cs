using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using DunGen.Graph;
using UnityEngine;

namespace DunGen.Editor.Inspectors
{
	[CustomEditor(typeof(GraphObjectObserver))]
	public sealed class GraphObjectInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var data = target as GraphObjectObserver;

			if (data == null)
				return;

			if (data.Node != null)
				DrawNodeGUI(data.Node);
			else if (data.Line != null)
				DrawLineGUI(data.Line);

			if (GUI.changed)
				EditorUtility.SetDirty(data.Flow);
		}

		private void DrawNodeGUI(GraphNode node)
		{
			var data = target as GraphObjectObserver;
			node.Graph = data.Flow;

			if (string.IsNullOrEmpty(node.Label))
				EditorGUILayout.LabelField("Node", EditorStyles.boldLabel);
			else
				EditorGUILayout.LabelField("Node: " + node.Label, EditorStyles.boldLabel);

			if (node.NodeType == NodeType.Normal)
			{
				node.Label = EditorGUILayout.TextField("Label", node.Label);
				//node.Position = Mathf.Clamp(EditorGUILayout.FloatField("Position", node.Position), 0, 1);
			}

			EditorUtil.DrawObjectList<TileSet>("Tile Sets", node.TileSets, GameObjectSelectionTypes.Prefab, target);

			if (data.Flow.KeyManager == null)
				return;

			EditorGUILayout.Space();
			DrawKeys(node.Graph.KeyManager, node.Keys, node.Locks, true);

#if UNITY_4 || UNITY_5 || UNITY_2017
			node.LockPlacement = (NodeLockPlacement)EditorGUILayout.EnumMaskField("Lock Placement", node.LockPlacement);
#else
			node.LockPlacement = (NodeLockPlacement)EditorGUILayout.EnumFlagsField("Lock Placement", node.LockPlacement);
#endif
		}

		private void DrawLineGUI(GraphLine line)
		{
			var data = target as GraphObjectObserver;
			line.Graph = data.Flow;

			EditorGUILayout.LabelField("Line Segment", EditorStyles.boldLabel);
			EditorUtil.DrawObjectList<DungeonArchetype>("Dungeon Archetypes", line.DungeonArchetypes, GameObjectSelectionTypes.Prefab, target);

			EditorGUILayout.Space();
			DrawKeys(line.Graph.KeyManager, line.Keys, line.Locks, false);
		}

		private void DrawKeys(KeyManager manager, List<KeyLockPlacement> keyIDs, List<KeyLockPlacement> lockIDs, bool isNode)
		{
			if (manager == null)
				return;

			if(manager == null)
				EditorGUILayout.HelpBox("Key Manager not set in Dungeon Flow", MessageType.Info);
			else if(manager.Keys.Count == 0)
				EditorGUILayout.HelpBox("Key Manager has no keys", MessageType.Info);
			else
			{
				EditorUtil.DrawKeySelection("Keys", manager, keyIDs, false);
				EditorUtil.DrawKeySelection("Locks", manager, lockIDs, !isNode);
			}
		}
	}
}
