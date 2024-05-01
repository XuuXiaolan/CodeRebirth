using DunGen.Graph;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DunGen.Editor.Validation.Rules
{
	sealed class DoorwayRule : IValidationRule
	{
		#region Nested Types

		private sealed class DoorwayInfo
		{
			public Doorway Doorway { get; private set; }
			public GameObject TilePrefab { get; private set; }


			public DoorwayInfo(Doorway doorway, GameObject tilePrefab)
			{
				Doorway = doorway;
				TilePrefab = tilePrefab;
			}
		}

		#endregion

		private const float AngleThreshold = 0.5f;


		public void Validate(DungeonFlow flow, DungeonValidator validator)
		{
			var tiles = flow.GetUsedTileSets()
						.SelectMany(ts => ts.TileWeights.Weights.Select(w => w.Value))
						.Where(t => t != null);

			var tileDoorways = new Dictionary<GameObject, Doorway[]>();

			foreach (var tile in tiles)
				tileDoorways[tile] = tile.GetComponentsInChildren<Doorway>(true);

			CheckDoorwayCount(flow, validator);
			CheckDoorwayUpVectors(flow, validator, tileDoorways);
			CheckDoorwayForwardVectorsAndPositionAlongBounds(flow, validator, tileDoorways);
			CheckDoorwaySockets(flow, validator, tileDoorways);
		}

		private void CheckDoorwayCount(DungeonFlow flow, DungeonValidator validator)
		{
			var pathTileSets = new List<TileSet>();

			foreach (var node in flow.Nodes)
				if (node.NodeType != NodeType.Start && node.NodeType != NodeType.Goal)
					pathTileSets.AddRange(node.TileSets);

			foreach (var line in flow.Lines)
				foreach (var archetype in line.DungeonArchetypes)
					pathTileSets.AddRange(archetype.TileSets);


			var pathTiles = pathTileSets.SelectMany(ts => ts.TileWeights.Weights.Select(w => w.Value)).Where(t => t != null);

			foreach (var tile in pathTiles)
			{
				int doorwayCount = tile.GetComponentsInChildren<Doorway>(true).Count();

				if (doorwayCount < 2)
					validator.AddError("Tile '{0}' does not have enough doorways. Two doorways are required for all tiles except those that appear exclusively as a start node, goal node, or branch cap", tile, tile.name);
			}
		}

		private void CheckDoorwayUpVectors(DungeonFlow flow, DungeonValidator validator, Dictionary<GameObject, Doorway[]> tileDoorways)
		{
			var doorwaysByUpVector = new Dictionary<Vector3, List<DoorwayInfo>>();
			
			foreach(var pair in tileDoorways)
			{
				foreach(var doorway in pair.Value)
				{
					Vector3 upVector = doorway.transform.up;

					List<DoorwayInfo> doorwaySet = null;

					foreach(var existingPair in doorwaysByUpVector)
						if(Vector3.Angle(upVector, existingPair.Key) <= AngleThreshold)
							doorwaySet = existingPair.Value;

					if(doorwaySet == null)
					{
						doorwaySet = new List<DoorwayInfo>();
						doorwaysByUpVector[upVector] = doorwaySet;
					}

					doorwaySet.Add(new DoorwayInfo(doorway, pair.Key));
				}
			}

			if(doorwaysByUpVector.Count > 1)
			{
				Vector3 mostCommonUpVector = doorwaysByUpVector.OrderByDescending(x => x.Value.Count).First().Key;

				if (!UnityUtil.IsVectorAxisAligned(mostCommonUpVector))
					validator.AddError("The most common doorway up vector is not axis-aligned");

				foreach(var pair in doorwaysByUpVector)
				{
					if (pair.Key == mostCommonUpVector)
						continue;

					foreach (var info in pair.Value)
						validator.AddError("Doorway '{0}' in tile '{1}' has an invalid rotation. The most common up-vector among doorways is {2}", info.TilePrefab, info.Doorway.name, info.TilePrefab.name, mostCommonUpVector);
				}
			}
		}

		private void CheckDoorwayForwardVectorsAndPositionAlongBounds(DungeonFlow flow, DungeonValidator validator, Dictionary<GameObject, Doorway[]> tileDoorways)
		{
			foreach(var pair in tileDoorways)
			{
				var tile = pair.Key;
				var bounds = UnityUtil.CalculateObjectBounds(tile, true, false);

				foreach(var doorway in pair.Value)
				{
					doorway.ValidateTransform(out _, out bool isAxisAligned, out bool isEdgePositioned);

					if (!isAxisAligned)
						validator.AddError("Doorway '{0}' in tile '{1}' has an invalid rotation. the forward vector is not axis-aligned", tile, doorway.name, tile.name);
					else if (!isEdgePositioned)
						validator.AddWarning("Doorway '{0}' in tile '{1}' is not positioned at the edge of the tile's bounding box. This could also indicate that the doorway is facing the wrong way - a doorway should be rotated such that its local z-axis is facing away from the tile", tile, doorway.name, tile.name);
				}
			}
		}

		private void CheckDoorwaySockets(DungeonFlow flow, DungeonValidator validator, Dictionary<GameObject, Doorway[]> tileDoorways)
		{
			foreach (var pair in tileDoorways)
			{
				var tile = pair.Key;

				foreach (var doorway in pair.Value)
				{
					if (!doorway.HasSocketAssigned)
						validator.AddWarning("Doorway '{0}' in tile '{1}' has no socket assigned. The default socket will be used.", tile, doorway.name, tile.name);
				}
			}
		}
	}
}
