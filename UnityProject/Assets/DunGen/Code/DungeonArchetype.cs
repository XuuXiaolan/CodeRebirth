using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen
{
	/// <summary>
	/// A description of the layout of a dungeon
	/// </summary>
	[Serializable]
	[CreateAssetMenu(fileName = "New Archetype", menuName = "DunGen/Dungeon Archetype", order = 700)]
	public sealed class DungeonArchetype : ScriptableObject
	{
        /// <summary>
        /// A collection of tile sets from which rooms will be selected to fill the dungeon
        /// </summary>
        public List<TileSet> TileSets = new List<TileSet>();
        /// <summary>
        /// A collection of tile sets that can be used at the end of branch paths
        /// </summary>
        public List<TileSet> BranchCapTileSets = new List<TileSet>();
        /// <summary>
        /// Defines how the TileSets and BranchEndTileSets are used when placing rooms at the end of a branch
        /// </summary>
        public BranchCapType BranchCapType = BranchCapType.AsWellAs;
        /// <summary>
        /// The maximum depth (in tiles) that any branch in the dungeon can be
        /// </summary>
        public IntRange BranchingDepth = new IntRange(2, 4);
        /// <summary>
        /// The maximum number of branches each room can have
        /// </summary>
        public IntRange BranchCount = new IntRange(0, 2);
        /// <summary>
        /// The chance that this archetype will produce a straight section for the main path
        /// </summary>
        public float StraightenChance = 0.0f;
        /// <summary>
        /// Should DunGen attempt to prevent this archetype from appearing more than once throughout the dungeon layout?
        /// </summary>
        public bool Unique = false;


		public bool GetHasValidBranchCapTiles()
        {
            if (BranchCapTileSets.Count == 0)
                return false;

            foreach (var tileSet in BranchCapTileSets)
                if (tileSet.TileWeights.Weights.Count > 0)
                    return true;

            return false;
        }
    }

    public enum BranchCapType : byte
    {
        InsteadOf,
        AsWellAs,
    }
}
