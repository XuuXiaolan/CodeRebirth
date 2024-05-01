using System.Linq;
using UnityEngine;
using DunGen.Graph;

namespace DunGen
{
	/// <summary>
	/// Used as a quick runtime check to ensure the dungeon is correctly set up
	/// For a more thorough validator, check the DungeonValidator class in the editor scripts
	/// </summary>
	public sealed class DungeonArchetypeValidator
	{
        public DungeonFlow Flow { get; private set; }


        public DungeonArchetypeValidator(DungeonFlow flow)
        {
            Flow = flow;
        }

        public bool IsValid()
        {
            // Ensure archetype is not NULL
            if (Flow == null)
            {
                LogError("No Dungeon Flow is assigned");
                return false;
            }

            var archetypes = Flow.GetUsedArchetypes();
            var tileSets = Flow.GetUsedTileSets();

            foreach (var line in Flow.Lines)
            {
                if (line.DungeonArchetypes.Count == 0)
                {
                    LogError("One or more line segments in your dungeon flow graph have no archetype applied. Each line segment must have at least one archetype assigned to it.");
                    return false;
                }

                foreach(var archetype in line.DungeonArchetypes)
                    if (archetype == null)
                    {
                        LogError("One or more of the archetypes in your dungeon flow graph have an unset archetype value.");
                        return false;
                    }
            }

            foreach(var node in Flow.Nodes)
                if (node.TileSets.Count == 0)
                {
                    LogError("The \"{0}\" node in your dungeon flow graph have no tile sets applied. Each node must have at least one tile set assigned to it.", node.Label);
                    return false;
                }

            foreach (var archetype in archetypes)
            {
                if (archetype == null)
                {
                    LogError("An Archetype in the Dungeon Flow has not been assigned a value");
                    return false;
                }
                else
                {
                    foreach(var tileSet in archetype.TileSets)
                        foreach (var tile in tileSet.TileWeights.Weights.Select(x => x.Value))
                        {
							if (tile == null)
								continue;

							int doorwayCount = tile.GetComponentsInChildren<Doorway>(true).Count();

							if (doorwayCount <= 1)
								LogWarning("The Tile \"{0}\" in TileSet \"{1}\" has {2} doorways. Tiles in an archetype should have more than 1 doorway.", tile.name, tileSet.name, doorwayCount);
                        }
                }
            }

            foreach (var tileSet in tileSets)
            {
                if (tileSet == null)
                {
                    LogError("A TileSet in the Dungeon Flow has not been assigned a value");
                    return false;
                }

                // Make sure each TileSet has at least one Tile
                if (tileSet.TileWeights.Weights.Count == 0)
                {
                    LogError("TileSet \"{0}\" contains no Tiles", tileSet.name);
                    return false;
                }

                // Check for NULL weights - this is non-critical so just print a warning
                foreach (var weight in tileSet.TileWeights.Weights)
                {
                    if (weight.Value == null)
                        LogWarning("TileSet \"{0}\" contains an entry with no Tile", tileSet.name);
                    if (weight.MainPathWeight <= 0 && weight.BranchPathWeight <= 0)
                        LogWarning("TileSet \"{0}\" contains an entry with an invalid weight. Both weights are below zero, resulting in no chance for this tile to spawn in the dungeon. Either MainPathWeight or BranchPathWeight can be zero, not both.", tileSet.name);
                }
            }

            return true;
        }

        private void LogError(string format, params object[] args)
        {
            Debug.LogError(string.Format("[ArchetypeValidator] Error: " + format, args));
        }

        private void LogWarning(string format, params object[] args)
        {
            Debug.LogWarning(string.Format("[ArchetypeValidator] Warning: " + format, args));
        }
	}
}
