using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DunGen.Graph
{
	/// <summary>
	/// A helper class for building a dungeon flow with a fluent interface, removing a lot of manual work
	/// </summary>
	public sealed class DungeonFlowBuilder
	{
		/// <summary>
		/// The dungeon flow that this builder will write to
		/// </summary>
		public DungeonFlow Flow { get; private set; }

		private List<GraphLine> lines = new List<GraphLine>();
		private List<GraphNode> nodes = new List<GraphNode>();
		private float currentPosition;


		public DungeonFlowBuilder(DungeonFlow flow)
		{
			Flow = flow;
		}

		/// <summary>
		/// Adds a new line segment to the flow graph. A line segment represents one or more rooms in a path between two nodes
		/// </summary>
		/// <param name="archetype">An archetype to pull rooms from</param>
		/// <param name="length">The length of this line segment. The entire path length will be automatically normalized. Must be greater than zero</param>
		/// <param name="locks">An optional list of the locks that can be applied to this segment</param>
		/// <param name="keys">An optional list of the keys that can be placed in this segment</param>
		/// <returns></returns>
		public DungeonFlowBuilder AddLine(DungeonArchetype archetype, float length = 1f, IEnumerable<KeyLockPlacement> locks = null, IEnumerable<KeyLockPlacement> keys = null)
		{
			return AddLine(new DungeonArchetype[] { archetype }, length, locks, keys);
		}

		/// <summary>
		/// Adds a new line segment to the flow graph. A line segment represents one or more rooms in a path between two nodes
		/// </summary>
		/// <param name="archetypes">A list of archetypes to pull rooms from</param>
		/// <param name="length">The length of this line segment. The entire path length will be automatically normalized. Must be greater than zero</param>
		/// <param name="locks">An optional list of the locks that can be applied to this segment</param>
		/// <param name="keys">An optional list of the keys that can be placed in this segment</param>
		/// <returns></returns>
		public DungeonFlowBuilder AddLine(IEnumerable<DungeonArchetype> archetypes, float length = 1f, IEnumerable<KeyLockPlacement> locks = null, IEnumerable<KeyLockPlacement> keys = null)
		{
			if (length <= 0f)
				throw new ArgumentOutOfRangeException("Length must be grater than zero");

			var line = new GraphLine(Flow);
			line.Position = currentPosition;
			line.Length = length;

			if (archetypes != null && archetypes.Any())
				line.DungeonArchetypes.AddRange(archetypes);

			if (locks != null && locks.Any())
				line.Locks.AddRange(locks);
			if (keys != null && keys.Any())
				line.Keys.AddRange(keys);

			lines.Add(line);
			currentPosition += length;

			return this;
		}

		/// <summary>
		/// Continue an existing line. This will just increase the length of the preceding line segment (for use when you need to place a node mid-way through a line segment)
		/// </summary>
		/// <param name="length">The additional length to add to this line segment. The entire path length will be automatically normalized. Must be greater than zero</param>
		/// <returns></returns>
		public DungeonFlowBuilder ContinueLine(float length = 1f)
		{
			if (lines.Count == 0)
				throw new Exception("Cannot call ContinueLine(..) before AddLine(..)");

			lines.Last().Length += length;
			currentPosition += length;

			return this;
		}

		/// <summary>
		/// Adds a node to the current point in the dungeon path. A node represents a single room in the layout
		/// </summary>
		/// <param name="tileSet">A tile set to pull rooms from</param>
		/// <param name="label">An optional label for this node (only visible in the dungeon flow editor)</param>
		/// <param name="allowLocksOnEntrance">Can locks be placed on the entrance doorway to this room?</param>
		/// <param name="allowLocksOnExit">Can locks be placed on the exit doorway from this room?</param>
		/// <param name="locks">An optional list of the locks that can be applied to this node</param>
		/// <param name="keys">An optional list of the keys that can be placed in this node</param>
		/// <returns></returns>
		public DungeonFlowBuilder AddNode(TileSet tileSet, string label = null, bool allowLocksOnEntrance = false, bool allowLocksOnExit = false, IEnumerable<KeyLockPlacement> locks = null, IEnumerable<KeyLockPlacement> keys = null)
		{
			return AddNode(new TileSet[] { tileSet }, label, allowLocksOnEntrance, allowLocksOnExit, locks, keys);
		}

		/// <summary>
		/// Adds a node to the current point in the dungeon path. A node represents a single room in the layout
		/// </summary>
		/// <param name="tileSets">A list of tile sets to pull rooms from</param>
		/// <param name="label">An optional label for this node (only visible in the dungeon flow editor)</param>
		/// <param name="allowLocksOnEntrance">Can locks be placed on the entrance doorway to this room?</param>
		/// <param name="allowLocksOnExit">Can locks be placed on the exit doorway from this room?</param>
		/// <param name="locks">An optional list of the locks that can be applied to this node</param>
		/// <param name="keys">An optional list of the keys that can be placed in this node</param>
		/// <returns></returns>
		public DungeonFlowBuilder AddNode(IEnumerable<TileSet> tileSets, string label = null, bool allowLocksOnEntrance = false, bool allowLocksOnExit = false, IEnumerable<KeyLockPlacement> locks = null, IEnumerable<KeyLockPlacement> keys = null)
		{
			var node = new GraphNode(Flow);

			node.Label = (label == null) ? "Node" : label;
			node.Position = currentPosition;
			node.NodeType = NodeType.Normal;

			if (allowLocksOnEntrance)
				node.LockPlacement |= NodeLockPlacement.Entrance;
			if (allowLocksOnExit)
				node.LockPlacement |= NodeLockPlacement.Exit;

			if(tileSets != null && tileSets.Any())
				node.TileSets.AddRange(tileSets);

			if (locks != null && locks.Any())
				node.Locks.AddRange(locks);
			if (keys != null && keys.Any())
				node.Keys.AddRange(keys);

			nodes.Add(node);
			return this;
		}

		/// <summary>
		/// Finalize the dungeon flow and assign the built nodes and lines to the targetted dungeon flow asset
		/// </summary>
		/// <returns></returns>
		public DungeonFlowBuilder Complete()
		{
			if (lines.Count == 0)
				throw new Exception("DungeonFlowBuilder must have at least one line added before finalizing");
			if (nodes.Count < 2)
				throw new Exception("DungeonFlowBuilder must have at least two nodes added before finalizing");


			// Normalize flow length
			float length = currentPosition;
			currentPosition = 1.0f;

			foreach (var line in lines)
			{
				line.Position /= length;
				line.Length /= length;
			}

			foreach(var node in nodes)
				node.Position /= length;


			// Set node types
			nodes.First().NodeType = NodeType.Start;
			nodes.Last().NodeType = NodeType.Goal;


			// Assign lines and nodes to the dungeon flow
			Flow.Lines.Clear();
			Flow.Nodes.Clear();
			Flow.Lines.AddRange(lines);
			Flow.Nodes.AddRange(nodes);

			return this;
		}
	}
}
