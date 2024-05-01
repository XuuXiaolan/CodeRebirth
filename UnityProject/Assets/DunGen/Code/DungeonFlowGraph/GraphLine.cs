using System;
using System.Collections.Generic;
using System.Linq;

namespace DunGen.Graph
{
	/// <summary>
	/// A line segment on the dugeon flow graph, representing a series of tiles forming a path through the dungeon
	/// </summary>
	[Serializable]
	public class GraphLine
	{
		public DungeonFlow Graph;

		/// <summary>
		/// A collection of dungeon archetypes, of which one will be chosen to populate this segment of the dungeon
		/// </summary>
		public List<DungeonArchetype> DungeonArchetypes = new List<DungeonArchetype>();
		/// <summary>
		/// This segment's normalized position on the graph (0-1)
		/// </summary>
		public float Position;
		/// <summary>
		/// This segment's normalized length (0-1)
		/// </summary>
		public float Length;
		/// <summary>
		/// A list of possible keys to be placed in this area
		/// </summary>
		public List<KeyLockPlacement> Keys = new List<KeyLockPlacement>();
		/// <summary>
		/// A list of possible locked doors to be placed in this area
		/// </summary>
		public List<KeyLockPlacement> Locks = new List<KeyLockPlacement>();


		public GraphLine(DungeonFlow graph)
		{
			Graph = graph;
		}

		public DungeonArchetype GetRandomArchetype(RandomStream randomStream, IList<DungeonArchetype> usedArchetypes)
		{
			var validArchetypes = DungeonArchetypes.Where(a => !a.Unique || !usedArchetypes.Contains(a));

			if (!validArchetypes.Any())
				validArchetypes = DungeonArchetypes;

			int index = randomStream.Next(0, validArchetypes.Count());
			return validArchetypes.ElementAt(index);
		}
	}
}
