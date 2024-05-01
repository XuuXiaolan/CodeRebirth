using DunGen.Graph;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DunGen
{
	public static class DungeonUtil
	{
		/// <summary>
		/// Adds a Door component to the selected doorPrefab if one doesn't already exist
		/// </summary>
		/// <param name="dungeon">The dungeon that this door belongs to</param>
		/// <param name="doorPrefab">The door prefab on which to apply the component</param>
		/// <param name="doorway">The doorway that this door belongs to</param>
		public static void AddAndSetupDoorComponent(Dungeon dungeon, GameObject doorPrefab, Doorway doorway)
		{
			var door = doorPrefab.GetComponent<Door>();

			if (door == null)
				door = doorPrefab.AddComponent<Door>();

			door.Dungeon = dungeon;
			door.DoorwayA = doorway;
			door.DoorwayB = doorway.ConnectedDoorway;
			door.TileA = doorway.Tile;
			door.TileB = doorway.ConnectedDoorway.Tile;

			dungeon.AddAdditionalDoor(door);
		}

		public static bool HasAnyViableEntries(this List<GameObjectWeight> weights)
		{
			if (weights == null || weights.Count == 0)
				return false;

			foreach (var entry in weights)
				if (entry.GameObject != null && entry.Weight > 0f)
					return true;

			return false;
		}

		public static GameObject GetRandom(this List<GameObjectWeight> weights, RandomStream randomStream)
		{
			float totalWeight = 0f;

			foreach (var entry in weights)
				if (entry.GameObject != null)
					totalWeight += entry.Weight;

			float randomNumber = (float)(randomStream.NextDouble() * totalWeight);

			foreach (var entry in weights)
			{
				if (entry == null || entry.GameObject == null)
					continue;

				if (randomNumber < entry.Weight)
					return entry.GameObject;

				randomNumber -= entry.Weight;
			}

			return null;
		}
	}
}
