using DunGen.Graph;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DunGen
{
	public class Dungeon : MonoBehaviour
	{
		/// <summary>
		/// World-space bounding box of the entire dungeon
		/// </summary>
		public Bounds Bounds { get; protected set; }
		/// <summary>
		/// The dungeon flow asset used to generate this dungeon
		/// </summary>
		public DungeonFlow DungeonFlow
		{
			get => dungeonFlow;
			set => dungeonFlow = value;
		}
		/// <summary>
		/// Should we render debug information about the dungeon
		/// </summary>
		public bool DebugRender = false;

		public ReadOnlyCollection<Tile> AllTiles { get; private set; }
		public ReadOnlyCollection<Tile> MainPathTiles { get; private set; }
		public ReadOnlyCollection<Tile> BranchPathTiles { get; private set; }
		public ReadOnlyCollection<GameObject> Doors { get; private set; }
		public ReadOnlyCollection<DoorwayConnection> Connections { get; private set; }
		public DungeonGraph ConnectionGraph { get; private set; }

		[SerializeField]
		private DungeonFlow dungeonFlow;
		[SerializeField]
		private List<Tile> allTiles = new List<Tile>();
		[SerializeField]
		private List<Tile> mainPathTiles = new List<Tile>();
		[SerializeField]
		private List<Tile> branchPathTiles = new List<Tile>();
		[SerializeField]
		private List<GameObject> doors = new List<GameObject>();
		[SerializeField]
		private List<DoorwayConnection> connections = new List<DoorwayConnection>();


		public Dungeon()
		{
			AllTiles = new ReadOnlyCollection<Tile>(allTiles);
			MainPathTiles = new ReadOnlyCollection<Tile>(mainPathTiles);
			BranchPathTiles = new ReadOnlyCollection<Tile>(branchPathTiles);
			Doors = new ReadOnlyCollection<GameObject>(doors);
			Connections = new ReadOnlyCollection<DoorwayConnection>(connections);
		}

		private void Start()
		{
			// If there are already tiles and the connection graph isn't initialised yet,
			// this script is likely already present in the scene (from generating the dungeon in-editor).
			// We just need to finalise the dungeon info from data we already have available
			if(allTiles.Count > 0 && ConnectionGraph == null)
				FinaliseDungeonInfo();
		}

		internal void AddAdditionalDoor(Door door)
		{
			if (door != null)
				doors.Add(door.gameObject);
		}

		internal void PreGenerateDungeon(DungeonGenerator dungeonGenerator)
		{
			DungeonFlow = dungeonGenerator.DungeonFlow;
		}

		internal void PostGenerateDungeon(DungeonGenerator dungeonGenerator)
		{
			FinaliseDungeonInfo();
		}

		private void FinaliseDungeonInfo()
		{
			ConnectionGraph = new DungeonGraph(this);
			Bounds = UnityUtil.CombineBounds(allTiles.Select(x => x.Placement.Bounds).ToArray());
		}

		public void Clear()
		{
			// Destroy all tiles
			foreach (var tile in allTiles)
			{
				// Clean up any door prefabs first
				foreach (var doorway in tile.UsedDoorways)
				{
					if (doorway.UsedDoorPrefabInstance != null)
						UnityUtil.Destroy(doorway.UsedDoorPrefabInstance);
				}

				UnityUtil.Destroy(tile.gameObject);
			}

			// Destroy anything else attached to this dungeon
			for (int i = 0; i < transform.childCount; i++)
			{
				GameObject child = transform.GetChild(i).gameObject;
				UnityUtil.Destroy(child);
			}

			allTiles.Clear();
			mainPathTiles.Clear();
			branchPathTiles.Clear();
			doors.Clear();
			connections.Clear();
		}

		public Doorway GetConnectedDoorway(Doorway doorway)
		{
			foreach (var conn in connections)
				if (conn.A == doorway)
					return conn.B;
				else if (conn.B == doorway)
					return conn.A;

			return null;
		}

		public void FromProxy(DungeonProxy proxyDungeon, DungeonGenerator generator)
		{
			Clear();

			var proxyToTileMap = new Dictionary<TileProxy, Tile>();

			foreach (var tileProxy in proxyDungeon.AllTiles)
			{
				// Instantiate & re-position tile
				var tileObj = GameObject.Instantiate(tileProxy.Prefab, generator.Root.transform);
				tileObj.transform.localPosition = tileProxy.Placement.Position;
				tileObj.transform.localRotation = tileProxy.Placement.Rotation;

				// Add tile to lists
				var tile = tileObj.GetComponent<Tile>();
				tile.Dungeon = this;
				tile.Placement = new TilePlacementData(tileProxy.Placement);
				proxyToTileMap[tileProxy] = tile;
				allTiles.Add(tile);

				if (tile.Placement.IsOnMainPath)
					mainPathTiles.Add(tile);
				else
					branchPathTiles.Add(tile);

				// Place trigger volume
				if (generator.PlaceTileTriggers)
				{
					tile.AddTriggerVolume();
					tile.gameObject.layer = generator.TileTriggerLayer;
				}

				// Process doorways
				var allDoorways = tileObj.GetComponentsInChildren<Doorway>();

				foreach (var doorway in allDoorways)
				{
					doorway.Tile = tile;
					doorway.placedByGenerator = true;
					doorway.HideConditionalObjects = false;

					tile.AllDoorways.Add(doorway);
				}

				foreach (var doorwayProxy in tileProxy.UsedDoorways)
				{
					var doorway = allDoorways[doorwayProxy.Index];
					tile.UsedDoorways.Add(doorway);

					foreach (var obj in doorway.BlockerSceneObjects)
						if (obj != null)
							DestroyImmediate(obj, false);
				}

				foreach (var doorwayProxy in tileProxy.UnusedDoorways)
				{
					var doorway = allDoorways[doorwayProxy.Index];
					tile.UnusedDoorways.Add(doorway);

					foreach (var obj in doorway.ConnectorSceneObjects)
						if (obj != null)
							DestroyImmediate(obj, false);

					// If there is at least one blocker prefab, select one and spawn it as a child of the doorway
					if (doorway.BlockerPrefabWeights.HasAnyViableEntries())
					{
						GameObject blocker = GameObject.Instantiate(doorway.BlockerPrefabWeights.GetRandom(generator.RandomStream)) as GameObject;
						blocker.transform.parent = doorway.gameObject.transform;
						blocker.transform.localPosition = Vector3.zero;
						blocker.transform.localScale = Vector3.one;

						if (!doorway.AvoidRotatingBlockerPrefab)
							blocker.transform.localRotation = Quaternion.identity;
					}
				}
			}

			// Add doorway connections
			foreach(var proxyConn in proxyDungeon.Connections)
			{
				var tileA = proxyToTileMap[proxyConn.A.TileProxy];
				var tileB = proxyToTileMap[proxyConn.B.TileProxy];

				var doorA = tileA.AllDoorways[proxyConn.A.Index];
				var doorB = tileB.AllDoorways[proxyConn.B.Index];

				doorA.ConnectedDoorway = doorB;
				doorB.ConnectedDoorway = doorA;

				var conn = new DoorwayConnection(doorA, doorB);
				connections.Add(conn);

				SpawnDoorPrefab(doorA, doorB, generator.RandomStream);
			}
		}

		private void SpawnDoorPrefab(Doorway a, Doorway b, RandomStream randomStream)
		{
			// This door already has a prefab instance placed, exit early
			if (a.HasDoorPrefabInstance || b.HasDoorPrefabInstance)
				return;

			// Add door prefab
			Doorway chosenDoor;

			bool doorwayAHasEntries = a.ConnectorPrefabWeights.HasAnyViableEntries();
			bool doorwayBHasEntries = b.ConnectorPrefabWeights.HasAnyViableEntries();

			// No doorway has a prefab to place, exit early
			if (!doorwayAHasEntries && !doorwayBHasEntries)
				return;

			// If both doorways have door prefabs..
			if (doorwayAHasEntries && doorwayBHasEntries)
			{
				// ..A is selected if its priority is greater than or equal to B..
				if (a.DoorPrefabPriority >= b.DoorPrefabPriority)
					chosenDoor = a;
				// .. otherwise, B is chosen..
				else
					chosenDoor = b;
			}
			// ..if only one doorway has a prefab, use that one
			else
				chosenDoor = (doorwayAHasEntries) ? a : b;


			GameObject doorPrefab = chosenDoor.ConnectorPrefabWeights.GetRandom(randomStream);

			if (doorPrefab != null)
			{
				GameObject door = Instantiate(doorPrefab, chosenDoor.transform);
				door.transform.localPosition = Vector3.zero;

				if (!chosenDoor.AvoidRotatingDoorPrefab)
					door.transform.localRotation = Quaternion.identity;

				doors.Add(door);

				DungeonUtil.AddAndSetupDoorComponent(this, door, chosenDoor);

				a.SetUsedPrefab(door);
				b.SetUsedPrefab(door);
			}
		}

		public void OnDrawGizmos()
		{
			if (DebugRender)
				DebugDraw();
		}

		public void DebugDraw()
		{
			Color mainPathStartColour = Color.red;
			Color mainPathEndColour = Color.green;
			Color branchPathStartColour = Color.blue;
			Color branchPathEndColour = new Color(0.5f, 0, 0.5f);
			float boundsBoxOpacity = 0.75f;

			foreach (var tile in allTiles)
			{
				Bounds bounds = tile.Placement.Bounds;
				bounds.size = bounds.size * 1.01f;

				Color tileColour = (tile.Placement.IsOnMainPath) ?
									Color.Lerp(mainPathStartColour, mainPathEndColour, tile.Placement.NormalizedDepth) :
									Color.Lerp(branchPathStartColour, branchPathEndColour, tile.Placement.NormalizedDepth);

				tileColour.a = boundsBoxOpacity;
				Gizmos.color = tileColour;

				Gizmos.DrawCube(bounds.center, bounds.size);

			}
		}
	}
}
