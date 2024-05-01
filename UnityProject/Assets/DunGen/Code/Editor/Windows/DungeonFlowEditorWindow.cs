using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using DunGen.Graph;
using UnityEngine;

namespace DunGen.Editor
{
	public sealed class DungeonFlowEditorWindow : EditorWindow
    {
        #region Layout Constants

        private const float LineThickness = 30;
        private const float HorizontalMargin = 10;
        private const float VerticalMargin = 10;
        private const float NodeWidth = 60;
        private const float MinorNodeSizeCoefficient = 0.5f;
		private const int BorderThickness = 2;
        private static readonly Color StartNodeColour = new Color(0.78f, 0.38f, 0.38f);
        private static readonly Color GoalNodeColour = new Color(0.39f, 0.69f, 0.39f);
		private static readonly Color NodeColour = Color.white;
		private static readonly Color LineColour = Color.white;
		private static readonly Color BorderColour = Color.black;

        #endregion

        #region Context Menu Command Identifiers

        private enum GraphContextCommand
        {
            Delete,
            AddNode,
            SplitLine,
        }

		#endregion

		#region Statics

		private static GUIStyle boxStyle;
		private static Texture2D whitePixel;

		#endregion

		public DungeonFlow Flow { get; private set; }

        private bool isMouseDown;
        private bool isDragging;
        private GraphNode draggingNode;
        private GraphObjectObserver inspector;
        private GraphNode contextMenuNode;
        private GraphLine contextMenuLine;
        private Vector2 contextMenuPosition;


		private bool IsInitialised()
		{
			return boxStyle != null && whitePixel != null;
		}

		private void Init()
		{
			minSize = new Vector2(470, 150);

			whitePixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
			whitePixel.SetPixel(0, 0, Color.white);
			whitePixel.Apply();

			boxStyle = new GUIStyle(GUI.skin.box);
			boxStyle.normal.background = whitePixel;

			if (Flow != null)
			{
				foreach (var node in Flow.Nodes)
					node.Graph = Flow;
				foreach (var line in Flow.Lines)
					line.Graph = Flow;
			}
		}

        public void OnGUI()
        {
			if (!IsInitialised())
				Init();

            if (Flow == null)
            {
                Flow = (DungeonFlow)EditorGUILayout.ObjectField(Flow, typeof(DungeonFlow), false);
                return;
            }

            DrawNodes();
            DrawLines();

            HandleInput();

            if (GUI.changed)
                EditorUtility.SetDirty(Flow);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private float GetNormalizedPositionOnGraph(Vector2 screenPosition)
        {
            float width = position.width - (HorizontalMargin + NodeWidth / 2) * 2;

            float linePosition = screenPosition.x - (HorizontalMargin + NodeWidth / 2);
            return Mathf.Clamp(linePosition / width, 0, 1);
        }

        private void HandleInput()
        {
            var evt = Event.current;

            // Handle left mouse button actions
            if(evt.isMouse && evt.button == 0)
            {
                switch (evt.type)
                {
                    case EventType.MouseDown:
                        {
                            var node = GetNodeAtPoint(evt.mousePosition);

                            if (node != null && node.NodeType == NodeType.Normal)
                            {
                                draggingNode = node;
                                isDragging = true;

                                Select(node);
                            }

                            isMouseDown = true;

                            evt.Use();
                            break;
                        }
                    case EventType.MouseUp:
                        {
                            if (!isDragging)
                                TrySelectGraphObject(evt.mousePosition);

                            isMouseDown = false;
                            draggingNode = null;
                            isDragging = false;

                            evt.Use();
                            break;
                        }
                    case EventType.MouseDrag:
                        {
                            if (isMouseDown && !isDragging && draggingNode != null)
                                isDragging = true;

                            if (isDragging)
                            {
                                draggingNode.Position = GetNormalizedPositionOnGraph(evt.mousePosition);
                                Repaint();
                            }

                            evt.Use();
                            break;
                        }
                }
            }
            // Handle right mouse button actions
            else if (evt.type == EventType.ContextClick)
            {
                bool hasOpenedContextMenu = false;

				for (int i = Flow.Nodes.Count - 1; i >= 0; i--)
				{
					var node = Flow.Nodes[i];

					if (GetNodeBounds(node).Contains(evt.mousePosition))
					{
						HandleNodeContextMenu(node);
						hasOpenedContextMenu = true;
						contextMenuPosition = evt.mousePosition;
						break;
					}
				}

                if (!hasOpenedContextMenu)
                {
                    foreach(var line in Flow.Lines)
                        if (GetLineBounds(line).Contains(evt.mousePosition))
                        {
                            HandleLineContextMenu(line);
                            hasOpenedContextMenu = true;
                            contextMenuPosition = evt.mousePosition;
                            break;
                        }
                }

                evt.Use();
            }
        }

        #region Node Context Menu

        private void HandleNodeContextMenu(GraphNode node)
        {
            contextMenuNode = node;
            contextMenuLine = null;

            var menu = new GenericMenu();

            if (node.NodeType == NodeType.Normal)
                menu.AddItem(new GUIContent("Delete " + (string.IsNullOrEmpty(node.Label) ? "Node" : node.Label)), false, NodeContextMenuCallback, GraphContextCommand.Delete);

            menu.ShowAsContext();
        }

        private void NodeContextMenuCallback(object obj)
        {
            GraphContextCommand cmd = (GraphContextCommand)obj;

            switch (cmd)
            {
                case GraphContextCommand.Delete:
                    if (contextMenuNode.NodeType == NodeType.Normal)
                        Flow.Nodes.Remove(contextMenuNode);
                    break;
            }
        }

        #endregion

        #region Line Context Menu

        private void HandleLineContextMenu(GraphLine line)
        {
            contextMenuLine = line;
            contextMenuNode = null;

            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Add Node Here"), false, LineContextMenuCallback, GraphContextCommand.AddNode);
            menu.AddItem(new GUIContent("Split Segment"), false, LineContextMenuCallback, GraphContextCommand.SplitLine);

            if(Flow.Lines.Count > 1)
                menu.AddItem(new GUIContent("Delete Segment"), false, LineContextMenuCallback, GraphContextCommand.Delete);

            menu.ShowAsContext();
        }

        private void LineContextMenuCallback(object obj)
        {
            GraphContextCommand cmd = (GraphContextCommand)obj;

            switch (cmd)
            {
                case GraphContextCommand.AddNode:
                    {
                        GraphNode node = new GraphNode(Flow);
                        node.Label = "New Node";
                        node.Position = GetNormalizedPositionOnGraph(contextMenuPosition);
                        Flow.Nodes.Add(node);

                        break;
                    }
                case GraphContextCommand.Delete:
                    {
                        if (Flow.Lines.Count > 1)
                        {
                            int lineIndex = Flow.Lines.IndexOf(contextMenuLine);
                            Flow.Lines.RemoveAt(lineIndex);

                            if (lineIndex == 0)
                            {
                                var replacementLine = Flow.Lines[0];
                                replacementLine.Position = 0;
                                replacementLine.Length += contextMenuLine.Length;
                            }
                            else
                            {
                                var replacementLine = Flow.Lines[lineIndex - 1];
                                replacementLine.Length += contextMenuLine.Length;
                            }
                        }

                        break;
                    }
                case GraphContextCommand.SplitLine:
                    {
                        float position = GetNormalizedPositionOnGraph(contextMenuPosition);
                        float originalLength = contextMenuLine.Length;

                        int index = Flow.Lines.IndexOf(contextMenuLine);
                        float totalLength = 0;

                        for (int i = 0; i < index; i++)
                            totalLength += Flow.Lines[i].Length;

                        contextMenuLine.Length = position - totalLength;


                        GraphLine newSegment = new GraphLine(Flow);

                        foreach (var dungeonArchetype in contextMenuLine.DungeonArchetypes)
                            newSegment.DungeonArchetypes.Add(dungeonArchetype);

                        newSegment.Position = position;
                        newSegment.Length = originalLength - contextMenuLine.Length;

                        Flow.Lines.Insert(index + 1, newSegment);

                        break;
                    }
            }
        }

        #endregion

        private bool TrySelectGraphObject(Vector2 mousePosition)
        {
            var node = GetNodeAtPoint(mousePosition);

            if (node != null)
            {
                Select(node);
                return true;
            }

            var line = GetLineAtPoint(mousePosition);

            if (line != null)
            {
                Select(line);
                return true;
            }

            return false;
        }

        private void Select(GraphNode node)
        {
            BeginSelection();
            inspector.Inspect(node);
            EndSelection();
        }

        private void Select(GraphLine line)
        {
            BeginSelection();
            inspector.Inspect(line);
            EndSelection();
        }

        private void BeginSelection()
        {
            if (inspector == null)
            {
                inspector = ScriptableObject.CreateInstance<GraphObjectObserver>();
                inspector.Flow = Flow;
            }
        }

        private void EndSelection()
        {
            Selection.activeObject = inspector;
            EditorUtility.SetDirty(inspector);
        }

        private GraphNode GetNodeAtPoint(Vector2 screenPosition)
        {
			// Loop through nodes backwards to prioritise nodes other than the Start & Goal nodes
			for (int i = Flow.Nodes.Count - 1; i >= 0; i--)
			{
				var node = Flow.Nodes[i];

				if (GetNodeBounds(node).Contains(screenPosition))
					return node;
			}

            return null;
        }

        private GraphLine GetLineAtPoint(Vector2 screenPosition)
        {
            foreach (var line in Flow.Lines)
                if (GetLineBounds(line).Contains(screenPosition))
                    return line;

            return null;
        }

        private void DrawLines()
        {
            for (int i = 0; i < Flow.Lines.Count; i++)
            {
                var line = Flow.Lines[i];
				var rect = GetLineBounds(line);

				GUI.color = BorderColour;
				GUI.Box(ExpandRectCentered(rect, BorderThickness), "", boxStyle);
				GUI.color = LineColour;
                GUI.Box(rect, "", boxStyle);
            }
        }

        private void DrawNodes()
        {
			foreach(var node in Flow.Nodes.OrderBy(x => x.NodeType == NodeType.Normal))
			{
				var rect = GetNodeBounds(node);

				GUI.color = BorderColour;
				GUI.Box(ExpandRectCentered(rect, BorderThickness), "", boxStyle);
				GUI.color = (node.NodeType == NodeType.Start) ? StartNodeColour : (node.NodeType == NodeType.Goal) ? GoalNodeColour : NodeColour;
				GUI.Box(rect, node.Label, boxStyle);
			}
		}

		private Rect ExpandRectCentered(Rect rect, int margin)
		{
			return new Rect(rect.x - margin, rect.y - margin, rect.width + margin * 2, rect.height + margin * 2);
		}

		private Rect GetLineBounds(GraphLine line)
        {
            float center = position.center.y - position.y;
            float top = center - (LineThickness / 2);
            float width = position.width - (HorizontalMargin + NodeWidth / 2) * 2;

            float left = (HorizontalMargin + NodeWidth / 2) + line.Position * width;
            return new Rect(left, top, line.Length * width, LineThickness);
        }

        private Rect GetNodeBounds(GraphNode node)
        {
            float top = VerticalMargin;
            float width = position.width - (HorizontalMargin + NodeWidth / 2) * 2;
            float height = position.height - VerticalMargin * 2;

            if (node.NodeType == NodeType.Normal)
            {
                float offset = (position.height - VerticalMargin * 2) / 4;
                top += offset;
                height -= offset * 2;
            }

            float left = (HorizontalMargin + NodeWidth / 2) + node.Position * width - NodeWidth / 2;
            return new Rect(left, top, NodeWidth, height);
        }

        #region Static Methods

        [MenuItem("Window/DunGen/Dungeon Flow Editor")]
        public static void Open()
        {
            DungeonFlowEditorWindow.Open(null);
        }

        public static void Open(DungeonFlow flow)
        {
            var window = EditorWindow.GetWindow<DungeonFlowEditorWindow>(false, "Dungeon Flow", true);
            window.Flow = flow;
        }

        #endregion
    }
}
