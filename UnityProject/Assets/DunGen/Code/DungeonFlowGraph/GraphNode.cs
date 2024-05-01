using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DunGen.Graph
{
    /// <summary>
    /// Possible types of node. Currently used to prevent start & goal nodes from being moved/deleted
    /// </summary>
    public enum NodeType
    {
        Normal,
        Start,
        Goal,
    }

    /// <summary>
    /// A node on the dungeon flow graph, representing a single tile
    /// </summary>
    [Serializable]
	public class GraphNode
	{
		public DungeonFlow Graph;

        /// <summary>
        /// A collection of tile sets from which one tile will be chosen at random to place at the current position in the dungeon
        /// </summary>
        public List<TileSet> TileSets = new List<TileSet>();
        /// <summary>
        /// The node type (see NodeType description)
        /// </summary>
        public NodeType NodeType;
        /// <summary>
        /// The node's normalized position on the graph
        /// </summary>
        public float Position;
        /// <summary>
        /// A descriptive label, solely for visualization
        /// </summary>
        public string Label;
		/// <summary>
		/// A list of possible keys to be placed in this area
		/// </summary>
		public List<KeyLockPlacement> Keys = new List<KeyLockPlacement>();
		/// <summary>
		/// A list of possible locked doors to be placed in this area
		/// </summary>
		public List<KeyLockPlacement> Locks = new List<KeyLockPlacement>();
        /// <summary>
        /// Where locked doors are allowed to be placed on this node
        /// </summary>
        public NodeLockPlacement LockPlacement;


		public GraphNode(DungeonFlow graph)
		{
			Graph = graph;
		}
	}
}
